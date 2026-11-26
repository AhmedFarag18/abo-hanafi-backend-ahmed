using BrightMinds.Core.Models;
using BrightMinds.Services.Dtos.RawMaterialDtos;
using Blendd.Infrastructure.DataAccess;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BrightMinds.Api.Controllers
{
    [Authorize(Roles = "Admin,MorningShiftWarehouseClerk,NightShiftWarehouseClerk,WarehouseManager")]
    [Route("api/RawMaterials/{rawMaterialId:int}/Units")]
    [ApiController]
    public sealed class RawMaterialUnitsController : ControllerBase
    {
        private readonly AppDbContext _db;

        public RawMaterialUnitsController(AppDbContext db) => _db = db;

        [HttpGet]
        public async Task<IActionResult> GetForRawMaterial(int rawMaterialId, CancellationToken cancellationToken)
        {
            var exists = await _db.RawMaterials.AnyAsync(x => x.Id == rawMaterialId, cancellationToken);
            if (!exists)
                return NotFound(new { message = "المادة الخام غير موجودة" });

            var units = await _db.RawMaterialUnits
                .AsNoTracking()
                .Where(x => x.RawMaterialId == rawMaterialId)
                .Include(x => x.Unit)
                .OrderBy(x => x.Id)
                .Select(x => new
                {
                    x.Id,
                    x.RawMaterialId,
                    x.UnitId,
                    UnitName = x.Unit.Name,
                    x.ConversionFactor,
                    x.PricePerUnit,
                    x.WeightInKg
                })
                .ToListAsync(cancellationToken);

            return Ok(units);
        }

        /// <summary>Atomically replaces a material's selectable units; recipe-referenced units cannot be removed.</summary>
        [HttpPut]
        public async Task<IActionResult> ReplaceForRawMaterial(int rawMaterialId, [FromBody] List<RawMaterialUnitDto> units, CancellationToken cancellationToken)
        {
            if (units == null)
                return BadRequest(new { message = "قائمة الوحدات مطلوبة" });
            if (units.GroupBy(x => x.UnitId).Any(g => g.Count() > 1))
                return BadRequest(new { message = "لا يمكن تكرار نفس الوحدة للمادة الخام" });

            var rawMaterialExists = await _db.RawMaterials.AnyAsync(x => x.Id == rawMaterialId, cancellationToken);
            if (!rawMaterialExists)
                return NotFound(new { message = "المادة الخام غير موجودة" });

            var unitIds = units.Select(x => x.UnitId).Distinct().ToList();
            var existingUnitIds = await _db.Units.Where(x => unitIds.Contains(x.Id)).Select(x => x.Id).ToListAsync(cancellationToken);
            if (existingUnitIds.Count != unitIds.Count)
                return BadRequest(new { message = "توجد وحدة قياس غير موجودة" });

            var existing = await _db.RawMaterialUnits.Where(x => x.RawMaterialId == rawMaterialId).ToListAsync(cancellationToken);
            var requestedIds = units.Where(x => x.Id.HasValue).Select(x => x.Id!.Value).ToHashSet();
            if (units.Any(x => x.Id.HasValue && existing.All(old => old.Id != x.Id!.Value)))
                return BadRequest(new { message = "معرف الوحدة لا يتبع المادة الخام المحددة" });

            var removals = existing.Where(old => !requestedIds.Contains(old.Id) && units.All(x => x.Id != old.Id)).ToList();
            if (removals.Count > 0)
            {
                var removalIds = removals.Select(x => x.Id).ToList();
                var referenced = await _db.RecipeIngrediants.AnyAsync(x => x.RawMaterialUnitId.HasValue && removalIds.Contains(x.RawMaterialUnitId.Value), cancellationToken);
                if (referenced)
                    return Conflict(new { message = "لا يمكن حذف وحدة مستخدمة في وصفة" });
            }

            await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);
            _db.RawMaterialUnits.RemoveRange(removals);
            foreach (var dto in units)
            {
                var entity = dto.Id.HasValue ? existing.Single(x => x.Id == dto.Id.Value) : new RawMaterialUnit { RawMaterialId = rawMaterialId };
                entity.UnitId = dto.UnitId;
                entity.ConversionFactor = dto.ConversionFactor;
                entity.PricePerUnit = dto.PricePerUnit;
                entity.WeightInKg = dto.WeightInKg;
                if (!dto.Id.HasValue)
                    _db.RawMaterialUnits.Add(entity);
            }

            await _db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return await GetForRawMaterial(rawMaterialId, cancellationToken);
        }
    }
}
