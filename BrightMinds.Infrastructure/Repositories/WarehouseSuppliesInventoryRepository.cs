using Blendd.Infrastructure.DataAccess;
using BrightMinds.Core.Enums;
using BrightMinds.Core.Interfaces;
using BrightMinds.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace BrightMinds.Infrastructure.Repositories
{
    public class WarehouseSuppliesInventoryRepository : GenericRepository<WarehouseSuppliesInventory>, IWarehouseSuppliesInventoryRepository
    {
        private readonly AppDbContext _context;

        public WarehouseSuppliesInventoryRepository(AppDbContext context) : base(context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task AddInventoryItem(WarehouseSuppliesInventory item)
        {
            ArgumentNullException.ThrowIfNull(item);

            var rawMaterialId = await _context.SupplyItems
                .Where(supplyItem => supplyItem.Id == item.SupplyItemId)
                .Select(supplyItem => (int?)supplyItem.RawMaterialId)
                .SingleOrDefaultAsync();

            if (!rawMaterialId.HasValue)
                throw new InvalidOperationException($"Supply item {item.SupplyItemId} does not exist.");

            var existingItem = await _context.WarehouseSuppliesInventory
                .Include(inventory => inventory.SupplyItem)
                .ThenInclude(supplyItem => supplyItem.RawMaterial)
                .Where(inventory => inventory.WarehouseId == item.WarehouseId &&
                                    inventory.SupplyItem.RawMaterialId == rawMaterialId.Value)
                .FirstOrDefaultAsync();

            if (existingItem == null)
            {
                await _context.WarehouseSuppliesInventory.AddAsync(item);
                return;
            }

            if (existingItem.UnitType != item.UnitType)
                throw new InvalidOperationException("Inventory quantities in different units cannot be merged without an explicit material-unit conversion.");

            existingItem.Quantity += item.Quantity;
            existingItem.ContainerQuantity = (existingItem.ContainerQuantity ?? 0m) + (item.ContainerQuantity ?? 0m);
            // Preserve a weighted average batch valuation when consolidating supply lots.
            var previousValue = existingItem.Quantity - item.Quantity;
            existingItem.Price = existingItem.Quantity == 0
                ? item.Price
                : ((previousValue * existingItem.Price) + (item.Quantity * item.Price)) / existingItem.Quantity;

            var rawMaterial = existingItem.SupplyItem?.RawMaterial;
            if (rawMaterial != null)
            {
                var unitName = rawMaterial.SubUnitName ?? rawMaterial.UnitType.ToString();
                existingItem.DisplayDetails = rawMaterial.IsWeightBased
                    ? $"{existingItem.ContainerQuantity:F2} {rawMaterial.ContainerUnitName} ({existingItem.Quantity:F3} {rawMaterial.UnitType})"
                    : $"{existingItem.ContainerQuantity:F0} {rawMaterial.ContainerUnitName} = {existingItem.Quantity:F0} {unitName}";
            }

            _context.WarehouseSuppliesInventory.Update(existingItem);
        }

        public async Task AddInventoryItems(List<WarehouseSuppliesInventory> items)
        {
            ArgumentNullException.ThrowIfNull(items);
            foreach (var item in items)
                await AddInventoryItem(item);
        }

        public async Task DeleteInventoryItemsWithSupplyId(int id)
        {
            var inventoryItems = await _context.WarehouseSuppliesInventory
                .Where(inventory => inventory.SupplyItem.SupplyId == id)
                .ToListAsync();

            if (inventoryItems.Count > 0)
                _context.WarehouseSuppliesInventory.RemoveRange(inventoryItems);
        }

        public async Task<WarehouseSuppliesInventory?> GetFromMainWarehouseWithSupplyItemId(int supplyItemId)
        {
            var mainWarehouseId = await _context.Warehouses
                .Where(warehouse => warehouse.Type == WarehouseType.MainWarehouse)
                .Select(warehouse => (int?)warehouse.Id)
                .FirstOrDefaultAsync();

            if (!mainWarehouseId.HasValue)
                return null;

            return await _context.WarehouseSuppliesInventory
                .Include(inventory => inventory.SupplyItem)
                .Where(inventory => inventory.WarehouseId == mainWarehouseId.Value && inventory.SupplyItemId == supplyItemId)
                .FirstOrDefaultAsync();
        }

        public async Task<WarehouseSuppliesInventory?> GetFromSubWarehouseAsync(int rawMaterialId)
        {
            var subWarehouseId = await _context.Warehouses
                .Where(warehouse => warehouse.Type == WarehouseType.SubWarehouse)
                .Select(warehouse => (int?)warehouse.Id)
                .FirstOrDefaultAsync();

            if (!subWarehouseId.HasValue)
                return null;

            return await _context.WarehouseSuppliesInventory
                .Include(inventory => inventory.SupplyItem)
                .ThenInclude(supplyItem => supplyItem.RawMaterial)
                .Where(inventory => inventory.WarehouseId == subWarehouseId.Value && inventory.SupplyItem.RawMaterialId == rawMaterialId)
                .FirstOrDefaultAsync();
        }

        public async Task TransfereToSubWarehouse(WarehouseSuppliesInventory item)
        {
            ArgumentNullException.ThrowIfNull(item);

            var existingItem = await _context.WarehouseSuppliesInventory
                .Where(inventory => inventory.WarehouseId == item.WarehouseId &&
                                    inventory.SupplyItemId == item.SupplyItemId &&
                                    inventory.Price == item.Price)
                .FirstOrDefaultAsync();

            if (existingItem == null)
            {
                await _context.WarehouseSuppliesInventory.AddAsync(item);
                return;
            }

            if (item.UnitType != existingItem.UnitType)
            {
                if (item.UnitType == UnitType.kg && existingItem.UnitType == UnitType.g)
                    item.Quantity *= 1000m;
                else if (item.UnitType == UnitType.g && existingItem.UnitType == UnitType.kg)
                    item.Quantity /= 1000m;
                else
                    throw new InvalidOperationException("Cannot merge inventory entries with incompatible units.");
            }

            existingItem.Quantity += item.Quantity;
            existingItem.ContainerQuantity = (existingItem.ContainerQuantity ?? 0m) + (item.ContainerQuantity ?? 0m);
        }
    }
}
