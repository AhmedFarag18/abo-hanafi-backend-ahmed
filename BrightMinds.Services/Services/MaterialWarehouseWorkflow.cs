using BrightMinds.Core.Enums;
using BrightMinds.Core.Interfaces;
using BrightMinds.Core.Models;
using BrightMinds.Core.Specifications;
using BrightMinds.Services.Dtos.ProductDtos;
using BrightMinds.Services.Dtos.WarehouseDtos;
using BrightMinds.Services.IServices;
using System.Net;
using System.Security.Claims;

namespace BrightMinds.Services.Services
{
    /// <summary>Atomic workflows for recipe production and main-to-sub warehouse transfers.</summary>
    public sealed class MaterialWarehouseWorkflow
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IUnitConversionService _unitConversion;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public MaterialWarehouseWorkflow(IUnitOfWork unitOfWork, IUnitConversionService unitConversion, IHttpContextAccessor httpContextAccessor)
        {
            _unitOfWork = unitOfWork;
            _unitConversion = unitConversion;
            _httpContextAccessor = httpContextAccessor;
        }

        private sealed record PlannedIngredient(RawMaterial Material, decimal Quantity, decimal InventoryQuantity, decimal Cost, string UnitName, decimal? WeightInKg);

        public async Task<Response> ProduceAsync(int recipeId)
        {
            var recipe = await _unitOfWork.RecipeRepository.GetWithSpecAsync(new RecipeSpecifications(null, recipeId, false));
            if (recipe == null || recipe.IsDeleted)
                return new Response { StatusCode = (int)HttpStatusCode.NotFound, Message = "الوصفة غير موجودة" };

            var subWarehouse = await _unitOfWork.WarehouseRepository.GetByTypeAsync(WarehouseType.SubWarehouse);
            if (subWarehouse == null)
                return new Response { StatusCode = (int)HttpStatusCode.NotFound, Message = "المخزن الفرعي غير موجود" };

            var user = _httpContextAccessor.HttpContext?.User;
            var claims = user?.Claims;
            var userId = user?.FindFirstValue(ClaimTypes.NameIdentifier);
            if (claims == null || string.IsNullOrWhiteSpace(userId))
                return new Response { StatusCode = (int)HttpStatusCode.Unauthorized, Message = "يجب تسجيل الدخول أولاً" };

            var plan = new List<PlannedIngredient>();
            var path = new HashSet<int>();
            var planResult = await CollectIngredientsAsync(recipe, 1m, path, plan);
            if (planResult != null)
                return planResult;

            // Validate the entire aggregated plan before changing any stock. This also handles
            // repeated ingredients and repeated references to the same sub-recipe correctly.
            var requirements = plan.GroupBy(item => item.Material.Id)
                .Select(group => new { MaterialId = group.Key, Quantity = group.Sum(item => item.InventoryQuantity), Cost = group.Sum(item => item.Cost), Items = group.ToList() })
                .ToList();
            var inventoryByMaterial = new Dictionary<int, WarehouseSuppliesInventory>();
            foreach (var requirement in requirements)
            {
                var inventory = await _unitOfWork.WarehouseSuppliesInventoryRepository.GetFromSubWarehouseAsync(requirement.MaterialId);
                if (inventory == null)
                    return new Response { StatusCode = (int)HttpStatusCode.BadRequest, Message = $"المادة {requirement.Items[0].Material.Name} غير موجودة في المخزن الفرعي" };
                if (inventory.Quantity < requirement.Quantity)
                    return new Response { StatusCode = (int)HttpStatusCode.BadRequest, Message = $"كمية {requirement.Items[0].Material.Name} في المخزن الفرعي غير كافية (المطلوب: {requirement.Quantity}، المتاح: {inventory.Quantity})" };
                inventoryByMaterial.Add(requirement.MaterialId, inventory);
            }

            var withdrawItems = new List<WithdrawItem>();
            foreach (var requirement in requirements)
            {
                var inventory = inventoryByMaterial[requirement.MaterialId];
                inventory.Quantity -= requirement.Quantity;
                var rawMaterial = requirement.Items[0].Material;
                var containerSize = rawMaterial.ContainerSize;
                inventory.ContainerQuantity = containerSize > 0 ? inventory.Quantity / containerSize : null;
                inventory.DisplayDetails = $"{rawMaterial.Name}: استهلاك {requirement.Quantity} {inventory.UnitType}؛ الرصيد {inventory.Quantity} {inventory.UnitType}";
                if (inventory.Quantity == 0)
                    _unitOfWork.WarehouseSuppliesInventoryRepository.Delete(inventory);
                else
                    _unitOfWork.WarehouseSuppliesInventoryRepository.Update(inventory);

                // Keep one auditable history row per recipe line, including lines in sub-recipes.
                foreach (var item in requirement.Items)
                {
                    var conversionDescription = item.UnitName == item.Material.UnitType.ToString()
                        ? $"{item.Quantity} {item.UnitName}"
                        : $"{item.Quantity} {item.UnitName} = {item.InventoryQuantity} {inventory.UnitType}";
                    withdrawItems.Add(new WithdrawItem
                    {
                        Name = item.Material.Name,
                        Quantity = item.InventoryQuantity,
                        UnitType = inventory.UnitType,
                        DisplayDetails = conversionDescription
                    });
                }
            }

            var firstName = claims.FirstOrDefault(claim => claim.Type == "FirstName")?.Value;
            var lastName = claims.FirstOrDefault(claim => claim.Type == "LastName")?.Value;
            var history = new WithdrawHistory
            {
                WarehouseId = subWarehouse.Id,
                NumberOfItems = withdrawItems.Count,
                RecipientName = $"{firstName} {lastName}".Trim(),
                UserId = userId,
                Date = DateTime.UtcNow,
                WithdrawItems = withdrawItems
            };

            var product = new Product
            {
                ExpectedQuantity = recipe.ExpectedPortiansnumber,
                Name = recipe.Name,
                RecipeId = recipe.Id,
                IsFlexible = recipe.IsFlexible,
                CreationDate = DateTime.UtcNow,
                ProductionStatus = recipe.RequiresSettlement ? ProductionStatus.SettlementRequired : ProductionStatus.Waiting,
                InitialTotalWeight = recipe.RequiresSettlement ? recipe.TotalSize : null,
                Cost = plan.Sum(item => item.Cost),
                WithdrawHistory = history
            };
            if (recipe.RequiresSettlement)
                product.WithdrawHistory = history;

            await _unitOfWork.WithdrawHistoryRepository.Add(history);
            await _unitOfWork.ProductRepository.Add(product);
            return new Response { StatusCode = (int)HttpStatusCode.OK, Message = "تم تصنيع المنتج وخصم المكونات مرة واحدة بنجاح", Data = product.Adapt<ProductDetailsDto>() };
        }

        private async Task<Response?> CollectIngredientsAsync(Recipe recipe, decimal multiplier, HashSet<int> path, List<PlannedIngredient> plan)
        {
            if (!path.Add(recipe.Id))
                return new Response { StatusCode = (int)HttpStatusCode.BadRequest, Message = "يوجد تعارض دائري في الوصفات الفرعية" };

            foreach (var ingredient in recipe.Ingrediants)
            {
                var scaledQuantity = ingredient.Quantity * multiplier;
                if (ingredient.IngredientType == IngredientType.SubRecipe)
                {
                    var subRecipe = await _unitOfWork.RecipeRepository.GetWithSpecAsync(new RecipeSpecifications(null, ingredient.SubRecipeId, false));
                    if (subRecipe == null || subRecipe.IsDeleted)
                        return new Response { StatusCode = (int)HttpStatusCode.BadRequest, Message = "الوصفة الفرعية غير موجودة" };
                    var nestedResult = await CollectIngredientsAsync(subRecipe, scaledQuantity, path, plan);
                    if (nestedResult != null)
                        return nestedResult;
                    continue;
                }

                if (!ingredient.RawMaterialId.HasValue)
                    return new Response { StatusCode = (int)HttpStatusCode.BadRequest, Message = "مكوّن المادة الخام غير مكتمل" };

                var material = ingredient.RawMaterial ?? await _unitOfWork.RawMaterialRepository.GetAsync(ingredient.RawMaterialId.Value);
                if (material == null)
                    return new Response { StatusCode = (int)HttpStatusCode.NotFound, Message = "المادة الخام غير موجودة" };

                RawMaterialUnit? selectedUnit = null;
                string unitName;
                decimal? weightPerUnit;
                decimal costPerSelectedUnit;
                decimal inventoryQuantity;

                if (ingredient.RawMaterialUnitId.HasValue)
                {
                    selectedUnit = material.Units?.FirstOrDefault(unit => unit.Id == ingredient.RawMaterialUnitId.Value);
                    if (selectedUnit == null || selectedUnit.RawMaterialId != material.Id || selectedUnit.ConversionFactor <= 0)
                        return new Response { StatusCode = (int)HttpStatusCode.BadRequest, Message = "وحدة الوصفة لا تنتمي للمادة الخام أو عامل تحويلها غير صالح" };
                    if (!selectedUnit.PricePerUnit.HasValue || selectedUnit.PricePerUnit.Value <= 0)
                        return new Response { StatusCode = (int)HttpStatusCode.BadRequest, Message = $"السعر غير محدد للوحدة المختارة من المادة {material.Name}" };

                    var converted = _unitConversion.ToInventoryQuantity(material, scaledQuantity, selectedUnit.Id, selectedUnit);
                    if (!converted.HasValue)
                        return new Response { StatusCode = (int)HttpStatusCode.BadRequest, Message = $"تعذر تحويل وحدة {material.Name} إلى وحدة المخزون" };
                    inventoryQuantity = converted.Value;
                    unitName = selectedUnit.Unit?.Name ?? material.SmallestUnitName ?? material.MiddleUnitName ?? material.PurchaseUnitName ?? "وحدة المادة";
                    weightPerUnit = selectedUnit.WeightInKg;
                    costPerSelectedUnit = selectedUnit.PricePerUnit.Value;
                }
                else
                {
                    var converted = _unitConversion.ConvertKnownUnitType(scaledQuantity, ingredient.UnitType, material.UnitType);
                    if (!converted.HasValue)
                        return new Response { StatusCode = (int)HttpStatusCode.BadRequest, Message = $"لا يوجد تحويل معروف من {ingredient.UnitType} إلى وحدة مخزون {material.UnitType} للمادة {material.Name}" };
                    inventoryQuantity = converted.Value;
                    unitName = ingredient.UnitType.ToString();
                    weightPerUnit = material.UnitWeightInKg;
                    costPerSelectedUnit = material.PricePerInnerUnit ?? material.PricePerUnit;
                }

                var inventory = await _unitOfWork.WarehouseSuppliesInventoryRepository.GetFromSubWarehouseAsync(material.Id);
                if (inventory == null)
                    return new Response { StatusCode = (int)HttpStatusCode.BadRequest, Message = $"المادة {material.Name} غير موجودة في المخزن الفرعي" };

                var inventoryUnitQuantity = inventory.UnitType == material.UnitType
                    ? inventoryQuantity
                    : _unitConversion.ConvertKnownUnitType(inventoryQuantity, material.UnitType, inventory.UnitType);
                if (!inventoryUnitQuantity.HasValue)
                    return new Response { StatusCode = (int)HttpStatusCode.BadRequest, Message = $"وحدة رصيد المخزون غير متوافقة مع المادة {material.Name}" };

                var cost = selectedUnit != null
                    ? scaledQuantity * costPerSelectedUnit
                    : inventoryUnitQuantity.Value * inventory.Price;
                var itemWeight = weightPerUnit.HasValue ? scaledQuantity * weightPerUnit.Value : (decimal?)null;
                plan.Add(new PlannedIngredient(material, scaledQuantity, inventoryUnitQuantity.Value, cost, unitName, itemWeight));
            }

            path.Remove(recipe.Id);
            return null;
        }

        public async Task<Response> WithdrawFromMainAsync(MainInventoryWithdrawaDto request)
        {
            var mainWarehouse = await _unitOfWork.WarehouseRepository.GetByTypeAsync(WarehouseType.MainWarehouse);
            var subWarehouse = await _unitOfWork.WarehouseRepository.GetByTypeAsync(WarehouseType.SubWarehouse);
            if (mainWarehouse == null || subWarehouse == null)
                return new Response { StatusCode = (int)HttpStatusCode.NotFound, Message = "المخزن الرئيسي أو المخزن الفرعي غير موجود" };

            var prepared = new List<(WarehouseSuppliesInventory Inventory, RawMaterial Material, decimal RequestedQuantity, decimal BaseQuantity, string UnitName)>();
            foreach (var line in request.WithdrawRequests)
            {
                var inventory = await _unitOfWork.WarehouseSuppliesInventoryRepository
                    .GetWithSpecAsync(new WarehouseSuppliesInventorySpecifications(null, mainWarehouse.Id, line.Id, false));
                if (inventory == null || inventory.SupplyItem?.RawMaterial == null)
                    return new Response { StatusCode = (int)HttpStatusCode.NotFound, Message = $"العنصر {line.Id} غير موجود في المخزن الرئيسي" };

                var material = inventory.SupplyItem.RawMaterial;
                decimal requestedQuantity;
                decimal basePerRequestedUnit;
                string unitName;
                if (line.Quantity.HasValue)
                {
                    requestedQuantity = line.Quantity.Value;
                    if (line.UnitLevel == "purchase")
                    {
                        basePerRequestedUnit = material.ContainerSize > 0 ? material.ContainerSize : material.MiddleUnitsPerPurchaseUnit ?? 0m;
                        unitName = material.PurchaseUnitName ?? material.ContainerUnitName ?? "وحدة الشراء";
                    }
                    else if (line.UnitLevel == "middle")
                    {
                        var unitsPerPurchase = material.MiddleUnitsPerPurchaseUnit.GetValueOrDefault();
                        var basePerPurchase = material.ContainerSize > 0 ? material.ContainerSize : unitsPerPurchase;
                        if (unitsPerPurchase <= 0)
                            return new Response { StatusCode = (int)HttpStatusCode.BadRequest, Message = $"تحويل الوحدة الوسطى غير معرف للمادة {material.Name}" };
                        basePerRequestedUnit = basePerPurchase / unitsPerPurchase;
                        unitName = material.MiddleUnitName ?? material.SubUnitName ?? "الوحدة الوسطى";
                    }
                    else
                        return new Response { StatusCode = (int)HttpStatusCode.BadRequest, Message = "السحب مسموح بوحدة الشراء أو الوحدة الوسطى فقط" };
                }
                else
                {
                    requestedQuantity = line.ContainerQuantity ?? 0m;
                    basePerRequestedUnit = material.ContainerSize;
                    unitName = material.PurchaseUnitName ?? material.ContainerUnitName ?? "وحدة الشراء";
                }

                if (requestedQuantity <= 0 || basePerRequestedUnit <= 0)
                    return new Response { StatusCode = (int)HttpStatusCode.BadRequest, Message = $"بيانات التحويل أو الكمية غير صالحة للمادة {material.Name}" };

                var baseQuantity = requestedQuantity * basePerRequestedUnit;
                prepared.Add((inventory, material, requestedQuantity, baseQuantity, unitName));
            }

            foreach (var group in prepared.GroupBy(line => line.Inventory.Id))
            {
                var totalRequired = group.Sum(line => line.BaseQuantity);
                if (group.First().Inventory.Quantity < totalRequired)
                    return new Response { StatusCode = (int)HttpStatusCode.BadRequest, Message = $"رصيد {group.First().Material.Name} غير كافٍ" };
            }

            foreach (var group in prepared.GroupBy(line => line.Inventory.Id))
            {
                var inventory = group.First().Inventory;
                var material = group.First().Material;
                var deducted = group.Sum(line => line.BaseQuantity);
                inventory.Quantity -= deducted;
                inventory.ContainerQuantity = material.ContainerSize > 0 ? inventory.Quantity / material.ContainerSize : null;
                inventory.DisplayDetails = $"{inventory.Quantity} {inventory.UnitType}";
                if (inventory.Quantity == 0)
                    _unitOfWork.WarehouseSuppliesInventoryRepository.Delete(inventory);
                else
                    _unitOfWork.WarehouseSuppliesInventoryRepository.Update(inventory);
            }

            var claims = _httpContextAccessor.HttpContext?.User?.Claims;
            var history = new WithdrawHistory
            {
                WarehouseId = mainWarehouse.Id,
                RecipientName = request.RecipientName,
                UserId = _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier),
                Date = DateTime.UtcNow,
                NumberOfItems = prepared.Count
            };
            foreach (var line in prepared)
            {
                var existingSubStock = await _unitOfWork.WarehouseSuppliesInventoryRepository.GetFromSubWarehouseAsync(line.Material.Id);
                var subItem = new WarehouseSuppliesInventory
                {
                    WarehouseId = subWarehouse.Id,
                    SupplyItemId = line.Inventory.SupplyItemId,
                    ContainerQuantity = line.Material.ContainerSize > 0 ? line.BaseQuantity / line.Material.ContainerSize : null,
                    Quantity = line.BaseQuantity,
                    Price = line.Inventory.Price,
                    UnitType = line.Inventory.UnitType,
                    DisplayDetails = $"{line.RequestedQuantity} {line.UnitName} = {line.BaseQuantity} {line.Inventory.UnitType}"
                };
                await _unitOfWork.WarehouseSuppliesInventoryRepository.AddInventoryItem(subItem);
                history.WithdrawItems.Add(new WithdrawItem
                {
                    Name = line.Material.Name,
                    Quantity = line.BaseQuantity,
                    ContainerQuantity = line.Material.ContainerSize > 0 ? line.BaseQuantity / line.Material.ContainerSize : null,
                    UnitType = line.Inventory.UnitType,
                    DisplayDetails = $"{line.RequestedQuantity} {line.UnitName} = {line.BaseQuantity} {line.Inventory.UnitType}"
                });
            }

            await _unitOfWork.WithdrawHistoryRepository.Add(history);
            return new Response { StatusCode = (int)HttpStatusCode.OK, Message = "تم السحب للمخزن الفرعي بنجاح" };
        }
    }
}
