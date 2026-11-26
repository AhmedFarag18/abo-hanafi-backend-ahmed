using BrightMinds.Api.Response;
using BrightMinds.Core.Interfaces;
using BrightMinds.Services.Dtos.ProductDtos;
using BrightMinds.Services.Services;
using Mapster;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace BrightMinds.Api.Controllers
{
    [Authorize(Roles = "Admin,OperationChef")]
    public class OperationWarehouseController : ApiBaseController
    {
        private readonly MaterialWarehouseWorkflow _warehouseWorkflow;
        private readonly IUnitOfWork _unitOfWork;

        public OperationWarehouseController(MaterialWarehouseWorkflow warehouseWorkflow, IUnitOfWork unitOfWork)
        {
            _warehouseWorkflow = warehouseWorkflow;
            _unitOfWork = unitOfWork;
        }

        [HttpPost("Produce/{recipeId}")]
        public async Task<IActionResult> ProduceRecipie([FromRoute] int recipeId)
        {
            await using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                var result = await _warehouseWorkflow.ProduceAsync(recipeId);
                if (result.StatusCode != (int)HttpStatusCode.OK)
                {
                    await _unitOfWork.RollBackAsync();
                    return StatusCode(result.StatusCode, result);
                }

                await _unitOfWork.CompleteAsync();
                await _unitOfWork.CommitAsync();
                result.Data = result.Data.Adapt<ProductDetailsDto>();
                return StatusCode(result.StatusCode, result);
            }
            catch
            {
                await _unitOfWork.RollBackAsync();
                return StatusCode((int)HttpStatusCode.InternalServerError, new ApiResponse((int)HttpStatusCode.InternalServerError, message: "حدث خطأ أثناء إنتاج الوصفة"));
            }
        }
    }
}
