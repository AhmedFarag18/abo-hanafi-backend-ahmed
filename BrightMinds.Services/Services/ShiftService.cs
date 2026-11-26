using BrightMinds.Core.Enums;
using BrightMinds.Core.Interfaces;
using BrightMinds.Core.Models;
using BrightMinds.Core.Specifications;
using BrightMinds.Services.Dtos.ShiftDtos;
using BrightMinds.Services.IServices;
using BrightMinds.Services.SpecificationParams;
using Library.Services.Dtos;
using Mapster;
using Microsoft.AspNetCore.Http;
using System.Net;
using System.Security.Claims;

namespace BrightMinds.Services.Services
{
    public class ShiftService : IShiftService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IHttpContextAccessor _contextAccessor;

        public ShiftService(IUnitOfWork unitOfWork, IHttpContextAccessor contextAccessor)
        {
            _unitOfWork = unitOfWork;
            _contextAccessor = contextAccessor;
        }

        public async Task<Response> CreateShift()
        {
            var user = _contextAccessor.HttpContext?.User;
            var userId = user?.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId))
                return new Response { StatusCode = (int)HttpStatusCode.Unauthorized, Message = "لازم تسجل دخول الاول" };

            var lastShift = await _unitOfWork.ShiftRepository.GetLatestShiftForUser(userId);
            if (lastShift?.Status == ShiftStatus.Opened)
                return new Response { StatusCode = (int)HttpStatusCode.BadRequest, Message = "لازم تقفل اخر شيفت ليك، لو مش عارف تقفله كلم مسؤول النظام" };

            var shift = new Shift
            {
                UserId = userId,
                EmployeeName = $"{user?.FindFirst("FirstName")?.Value} {user?.FindFirst("LastName")?.Value}".Trim()
            };
            await _unitOfWork.ShiftRepository.Add(shift);
            await _unitOfWork.CompleteAsync();

            var remainingItems = await GetRemainingItemsFromLastClosedShift();
            if (remainingItems.Count > 0)
            {
                var salesWarehouse = await _unitOfWork.WarehouseRepository.GetByTypeAsync(WarehouseType.SalesWarehouse);
                if (salesWarehouse == null)
                    return new Response { StatusCode = (int)HttpStatusCode.NotFound, Message = "مخزن البيع غير موجود، لا يمكن ترحيل المتبقي" };

                await _unitOfWork.WithdrawHistoryRepository.Add(new WithdrawHistory
                {
                    WarehouseId = salesWarehouse.Id,
                    RecipientName = shift.EmployeeName,
                    UserId = userId,
                    ChefShiftId = shift.Id,
                    Date = DateTime.UtcNow,
                    NumberOfItems = remainingItems.Count,
                    WithdrawItems = remainingItems
                });
                await _unitOfWork.CompleteAsync();
            }

            return new Response { StatusCode = 200, Message = "تم بدأ الشيفت بنجاح", Data = new { Id = shift.Id } };
        }

        public async Task<Response> CloseShift()
        {
            var context = _contextAccessor.HttpContext;
            var shift = context?.GetActiveShift();
            if (shift == null)
                return new Response { StatusCode = 404, Message = "لازم تبدأ شيفت الاول" };

            var userId = context?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId))
                return new Response { StatusCode = 401, Message = "سجل دخول الاول" };
            if (shift.UserId != userId)
                return new Response { StatusCode = 401, Message = "الموظف الى بدأ الشيفت لازم هو الي ينهيه، لو بتواجهك اى مشكلة كلم مدير النظام" };
            if (shift.Status != ShiftStatus.Opened)
                return new Response { StatusCode = 400, Message = shift.Status == ShiftStatus.ForcedClosedByAdmin ? "الشيفت تم غلقه بالفعل من قبل مسؤول النظام" : "تم غلق الشيفت بالفعل" };

            var histories = await _unitOfWork.WithdrawHistoryRepository.GetAllWithSpecAsync(new WithdrawHistorySpecifications(shift));
            var unrecorded = histories.SelectMany(history => history.WithdrawItems).Where(item => !item.ConsumedQuantity.HasValue).ToList();
            if (unrecorded.Count > 0)
            {
                var names = string.Join("، ", unrecorded.Select(item => $"{item.Name} (الكمية: {item.Quantity})"));
                return new Response { StatusCode = 400, Message = $"لا يمكن إغلاق الشيفت قبل تسجيل الاستهلاك للعناصر التالية: {names}" };
            }

            shift.EndTimeStamp = DateTime.UtcNow;
            shift.Status = ShiftStatus.Closed;
            shift.TotalHours = (shift.EndTimeStamp - shift.StartTimeStamp).Value.TotalHours;
            _unitOfWork.ShiftRepository.Update(shift);
            await _unitOfWork.CompleteAsync();
            return new Response { StatusCode = 200, Message = "تم غلق الشيفت بنجاح" };
        }

        public async Task<Response> GetAllShifts(ShiftSpecParams specParams)
        {
            var data = await _unitOfWork.ShiftRepository.GetAllWithSpecAsync(new ShiftSpecifications(specParams, false));
            var count = await _unitOfWork.ShiftRepository.GetCountWithSpecAsync(new ShiftSpecifications(specParams, true));
            return new Response { StatusCode = 200, Message = "تم استعادة بيانات الشيفتات بنجاح", Data = new Pagination(specParams.PageSize, specParams.PageIndex, count, data.Adapt<List<ShiftDetailsDto>>()) };
        }

        public async Task<Response> GetShiftById(int id)
        {
            var shift = await _unitOfWork.ShiftRepository.GetAsync(id);
            return shift == null
                ? new Response { StatusCode = 404, Message = "الشيفت مش موجود" }
                : new Response { StatusCode = 200, Message = "تم استرجاع بيانات الشيفت بنجاح", Data = shift.Adapt<ShiftDetailsDto>() };
        }

        public async Task<Response> ForceCloseShift(int id)
        {
            var shift = await _unitOfWork.ShiftRepository.GetAsync(id);
            if (shift == null)
                return new Response { StatusCode = 404, Message = "الشيفت مش موجود" };
            if (shift.Status != ShiftStatus.Opened)
                return new Response { StatusCode = 400, Message = "تم غلق الشيفت بالفعل" };
            shift.EndTimeStamp = DateTime.UtcNow;
            shift.Status = ShiftStatus.ForcedClosedByAdmin;
            shift.TotalHours = (shift.EndTimeStamp - shift.StartTimeStamp).Value.TotalHours;
            _unitOfWork.ShiftRepository.Update(shift);
            await _unitOfWork.CompleteAsync();
            return new Response { StatusCode = 200, Message = "تم غلق الشيفت بنجاح" };
        }

        public async Task<Response> GetLastestShift()
        {
            var userId = _contextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId))
                return new Response { StatusCode = 401, Message = "Unauthorized User" };
            var shift = await _unitOfWork.ShiftRepository.GetLatestShiftForUser(userId);
            return shift == null
                ? new Response { StatusCode = 404, Message = "المستخدم معملش اى شيفت قبل كدا" }
                : new Response { StatusCode = 200, Message = "تم استعادة بيانات احدث شيفت بنجاح", Data = shift.Adapt<ShiftDetailsDto>() };
        }

        private async Task<List<WithdrawItem>> GetRemainingItemsFromLastClosedShift()
        {
            var closedShifts = await _unitOfWork.ShiftRepository.GetAllWithSpecAsync(
                new BaseSpecification<Shift>(shift => (shift.Status == ShiftStatus.Closed || shift.Status == ShiftStatus.ForcedClosedByAdmin) && shift.EndTimeStamp != null));
            var last = closedShifts.OrderByDescending(shift => shift.EndTimeStamp).FirstOrDefault();
            if (last == null)
                return new List<WithdrawItem>();

            var histories = await _unitOfWork.WithdrawHistoryRepository.GetAllWithSpecAsync(new WithdrawHistorySpecifications(last));
            return histories.SelectMany(history => history.WithdrawItems)
                .Where(item => item.ConsumedQuantity.HasValue && item.ConsumedQuantity.Value < item.Quantity)
                .Select(item => new WithdrawItem
                {
                    Name = item.Name,
                    Quantity = item.Quantity - item.ConsumedQuantity!.Value,
                    ContainerQuantity = item.ContainerQuantity,
                    UnitType = item.UnitType,
                    DisplayDetails = item.DisplayDetails + " (متبقي من شيفت سابق)",
                    ConsumedQuantity = 0,
                    SourceWithdrawItemId = item.Id
                }).ToList();
        }

        public async Task<Response> RecordConsumption(int shiftId, List<ConsumptionItemDto> consumptionList)
        {
            var shift = await _unitOfWork.ShiftRepository.GetAsync(shiftId);
            if (shift == null)
                return new Response { StatusCode = 404, Message = "الشيفت غير موجود" };

            var userId = _contextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId) || shift.UserId != userId)
                return new Response { StatusCode = 401, Message = "غير مصرح لك بتعديل هذا الشيفت" };
            if (shift.Status != ShiftStatus.Opened)
                return new Response { StatusCode = 400, Message = "لا يمكن تعديل استهلاك شيفت مغلق" };
            if (consumptionList == null || consumptionList.Count == 0)
                return new Response { StatusCode = 400, Message = "يجب إرسال عنصر استهلاك واحد على الأقل" };
            if (consumptionList.Any(item => item.ConsumedQuantity < 0))
                return new Response { StatusCode = 400, Message = "الكمية المستهلكة لا يمكن أن تكون سالبة" };
            if (consumptionList.GroupBy(item => item.WithdrawItemId).Any(group => group.Count() > 1))
                return new Response { StatusCode = 400, Message = "لا يمكن تكرار عنصر السحب في طلب الاستهلاك" };

            var histories = await _unitOfWork.WithdrawHistoryRepository.GetAllWithSpecAsync(new WithdrawHistorySpecifications(shift));
            var itemsById = histories.SelectMany(history => history.WithdrawItems).ToDictionary(item => item.Id);

            // Validate the entire batch before mutating tracked entities, so a bad item
            // cannot leave earlier items changed in the current request scope.
            foreach (var consumption in consumptionList)
            {
                if (!itemsById.TryGetValue(consumption.WithdrawItemId, out var item))
                    return new Response { StatusCode = 404, Message = $"العنصر {consumption.WithdrawItemId} غير موجود في هذا الشيفت" };
                if (consumption.ConsumedQuantity > item.Quantity)
                    return new Response { StatusCode = 400, Message = $"الكمية المستهلكة لا يمكن أن تتجاوز الكمية الأصلية للعنصر {item.Name}" };
            }

            foreach (var consumption in consumptionList)
                itemsById[consumption.WithdrawItemId].ConsumedQuantity = consumption.ConsumedQuantity;

            await _unitOfWork.CompleteAsync();
            return new Response { StatusCode = 200, Message = "تم تسجيل الاستهلاك بنجاح" };
        }
    }
}
