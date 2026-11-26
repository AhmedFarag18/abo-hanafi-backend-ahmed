using BrightMinds.Core.Enums;
using BrightMinds.Core.Interfaces;
using BrightMinds.Services.Dtos.WarehouseDtos;
using BrightMinds.Services.IServices;
using BrightMinds.Services.Services;
using BrightMinds.Services.SpecificationParams;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace BrightMinds.Api.Controllers
{
    public class SubWarehouseController : ApiBaseController
    {
        private readonly IWarehouseService _warehouseService;
        private readonly MaterialWarehouseWorkflow _warehouseWorkflow;
        private readonly IUnitOfWork _unitOfWork;

        public SubWarehouseController(IWarehouseService warehouseService, MaterialWarehouseWorkflow warehouseWorkflow, IUnitOfWork unitOfWork)
        {
            _warehouseService = warehouseService;
            _warehouseWorkflow = warehouseWorkflow;
            _unitOfWork = unitOfWork;
        }

        [HttpGet("Inventory")]
        [Authorize(Roles = "Admin,WarehouseManager,NightShiftWarehouseClerk,MorningShiftWarehouseClerk")]
        public async Task<IActionResult> GetInventory([FromQuery] WarehouseInventorySpecParams specParams)
        {
            var result = await _warehouseService.GetAllSuppliesInventoryAsync(specParams, WarehouseType.SubWarehouse);
            return StatusCode(result.StatusCode, result);
        }

        [HttpPost("Withdraw")]
        [Authorize(Roles = "Admin,WarehouseManager,NightShiftWarehouseClerk,MorningShiftWarehouseClerk")]
        public async Task<IActionResult> WithdrawFromMain([FromBody] MainInventoryWithdrawaDto withdrawlDto)
        {
            await using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                var result = await _warehouseWorkflow.WithdrawFromMainAsync(withdrawlDto);
                if (result.StatusCode != (int)HttpStatusCode.OK)
                {
                    await _unitOfWork.RollBackAsync();
                    return StatusCode(result.StatusCode, result);
                }

                await _unitOfWork.CompleteAsync();
                await _unitOfWork.CommitAsync();
                return StatusCode(result.StatusCode, result);
            }
            catch
            {
                await _unitOfWork.RollBackAsync();
                return StatusCode((int)HttpStatusCode.InternalServerError, new { statusCode = 500, message = "تعذر إتمام السحب من المخزن الرئيسي" });
            }
        }
    }
}
