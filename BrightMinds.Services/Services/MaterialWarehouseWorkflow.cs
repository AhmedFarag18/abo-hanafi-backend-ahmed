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

        private sealed record PlannedIngredient(
            RawMaterial Material,
            decimal InputQuantity,
            string InputUnitName,
            decimal InventoryQuantity,
            UnitType InventoryUnitType,
            decimal Cost,
            decimal? WeightInKg);

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
            var planResult = await CollectIngredientsAsync(recipe, 1m, new HashSet<int>(), plan);
            if (planResult != null)
                return planResult;

            // Validate aggregated material requirements before changing stock, so duplicate lines
            // and repeated sub-recipes are counted together and no ingredient is deducted twice.
            var requirements = plan.GroupBy(item => item.Material.Id)
                .Select(group => new
                {
                    MaterialId = group.Key,
                    Quantity = group.Sum(item => item.InventoryQuantity),
                    Items = group.ToList()
                })
                .ToList();

            var inventoryByMaterial = new Dictionary<int, WarehouseSuppliesInventory>();
            foreach (var requirement in requirements)
            {
                var inventory = await _unitOfWork.WarehouseSuppliesInventoryRepository.GetFromSubWarehouseAsync(requirement.MaterialId);
                if (inventory == null)
                    return new Response { StatusCode = 400, Message = $"المادة {requirement.Items[0].Material.Name} غير موجودة في المخزن الفرعي" };

                var neededInInventoryUnit = ConvertToInventoryUnit(requirement.Quantity, requirement.Items[0].InventoryUnitType, inventory.UnitType);
                if (!neededInInventoryUnit.HasValue)
                    return new Response { StatusCode = 400, Message = $"لا يمكن تحويل الكمية إلى وحدة رصيد {requirement.Items[0].Material.Name}" };
                if (inventory.Quantity < neededInInventoryUnit.Value)
                    return new Response { StatusCode = 400, Message = $"كمية {requirement.Items[0].Material.Name} غير كافية (المطلوب: {neededInInventoryUnit.Value}، المتاح: {inventory.Quantity})" };

                inventoryByMaterial.Add(requirement.MaterialId, inventory);
            }

            // Mutations start only after every requested material passed stock validation.
            var withdrawItems = new List<WithdrawItem>();
            foreach (var requirement in requirements)
            {
                var inventory = inventoryByMaterial[requirement.MaterialId];
                var targetUnit = inventory.UnitType;
                var required = requirement.Items.Sum(item => ConvertToInventoryUnit(item.InventoryQuantity, item.InventoryUnitType, targetUnit) ?? 0m);
                inventory.Quantity -= required;
                var material = requirement.Items[0].Material;
                inventory.ContainerQuantity = material.ContainerSize > 0 ? inventory.Quantity / material.ContainerSize : null;
                inventory.DisplayDetails = $"{material.Name}: الرصيد {inventory.Quantity} {targetUnit}";

                if (inventory.Quantity == 0)
                    _unitOfWork.WarehouseSuppliesInventoryRepository.Delete(inventory);
                else
                    _unitOfWork.WarehouseSuppliesInventoryRepository.Update(inventory);

                foreach (var line in requirement.Items)
                {
                    var convertedLine = ConvertToInventoryUnit(line.InventoryQuantity, line.InventoryUnitType, targetUnit);
                    if (!convertedLine.HasValue)
                        return new Response { StatusCode = 400, Message = $"لا يمكن تسجيل تحويل المادة {line.Material.Name}" };
                    withdrawItems.Add(new WithdrawItem
                    {
                        Name = line.Material.Name,
                        Quantity = convertedLine.Value,
                        UnitType = targetUnit,
                        DisplayDetails = $"{line.InputQuantity} {line.InputUnitName} = {convertedLine.Value} {targetUnit}"
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

            await _unitOfWork.WithdrawHistoryRepository.Add(history);
            await _unitOfWork.ProductRepository.Add(product);
            return new Response
            {
                StatusCode = (int)HttpStatusCode.OK,
                Message = "تم تصنيع المنتج وخصم المكونات مرة واحدة بنجاح",
                Data = product.Adapt<ProductDetailsDto>()
            };
        }

        private decimal? ConvertToInventoryUnit(decimal quantity, UnitType source, UnitType target) =>
            _unitConversion.ConvertKnownUnitType(quantity, source, target);

        private async Task<Response?> CollectIngredientsAsync(Recipe recipe, decimal multiplier, HashSet<int> path, List<PlannedIngredient> plan)
        {
            if (!path.Add(recipe.Id))
                return new Response { StatusCode = 400, Message = "يوجد تعارض دائري في الوصفات الفرعية" };

            foreach (var ingredient in recipe.Ingrediants)
            {
                var scaledQuantity = ingredient.Quantity * multiplier;
                if (ingredient.IngredientType == IngredientType.SubRecipe)
                {
                    var subRecipe = await _unitOfWork.RecipeRepository.GetWithSpecAsync(new RecipeSpecifications(null, ingredient.SubRecipeId, false));
                    if (subRecipe == null || subRecipe.IsDeleted)
                        return new Response { StatusCode = 400, Message = "الوصفة الفرعية غير موجودة" };
                    var nestedResult = await CollectIngredientsAsync(subRecipe, scaledQuantity, path, plan);
                    if (nestedResult != null)
                        return nestedResult;
                    continue;
                }

                if (!ingredient.RawMaterialId.HasValue)
                    return new Response { StatusCode = 400, Message = "مكوّن المادة الخام غير مكتمل" };

                var material = ingredient.RawMaterial ?? await _unitOfWork.RawMaterialRepository.GetAsync(ingredient.RawMaterialId.Value);
                if (material == null)
                    return new Response { StatusCode = 404, Message = "المادة الخام غير موجودة" };

                RawMaterialUnit? selectedUnit = null;
                string inputUnitName;
                UnitType baseUnitType;
                decimal inputQuantity = scaledQuantity;
                decimal? unitWeight;
                decimal cost;

                if (ingredient.RawMaterialUnitId.HasValue)
                {
                    selectedUnit = material.Units?.FirstOrDefault(unit => unit.Id == ingredient.RawMaterialUnitId.Value);
                    if (selectedUnit == null || selectedUnit.RawMaterialId != material.Id || selectedUnit.ConversionFactor <= 0)
                        return new Response { StatusCode = 400, Message = "وحدة الوصفة لا تنتمي للمادة الخام أو عامل تحويلها غير صالح" };
                    if (!selectedUnit.PricePerUnit.HasValue || selectedUnit.PricePerUnit.Value <= 0)
                        return new Response { StatusCode = 400, Message = $"السعر غير محدد للوحدة المختارة من المادة {material.Name}" };

                    var converted = _unitConversion.ToInventoryQuantity(material, scaledQuantity, selectedUnit.Id, selectedUnit);
                    if (!converted.HasValue)
                        return new Response { StatusCode = 400, Message = $"تعذر تحويل وحدة {material.Name} إلى وحدة المخزون" };
                    inputUnitName = selectedUnit.Unit?.Name ?? material.SmallestUnitName ?? material.MiddleUnitName ?? material.PurchaseUnitName ?? "وحدة المادة";
                    baseUnitType = material.UnitType;
                    unitWeight = selectedUnit.WeightInKg;
                    cost = scaledQuantity * selectedUnit.PricePerUnit.Value;

                    var unitQuantityInInventory = ConvertToInventoryUnit(converted.Value, material.UnitType, material.UnitType);
                    if (!unitQuantityInInventory.HasValue)
                        return new Response { StatusCode = 400, Message = $"وحدة المخزون غير معروفة للمادة {material.Name}" };
                    plan.Add(new PlannedIngredient(material, inputQuantity, inputUnitName, unitQuantityInInventory.Value, baseUnitType, cost, unitWeight.HasValue ? scaledQuantity * unitWeight.Value : null));
                    continue;
                }

                var legacyConverted = _unitConversion.ConvertKnownUnitType(scaledQuantity, ingredient.UnitType, material.UnitType);
                if (!legacyConverted.HasValue)
                    return new Response { StatusCode = 400, Message = $"لا يوجد تحويل معروف من {ingredient.UnitType} إلى وحدة مخزون {material.UnitType} للمادة {material.Name}" };
                inputUnitName = ingredient.UnitType.ToString();
                baseUnitType = material.UnitType;
                unitWeight = material.UnitWeightInKg;
                var inventory = await _unitOfWork.WarehouseSuppliesInventoryRepository.GetFromSubWarehouseAsync(material.Id);
                if (inventory == null)
                    return new Response { StatusCode = 400, Message = $"المادة {material.Name} غير موجودة في المخزن الفرعي" };

                // For legacy ingredients, inventory.Price is the actual batch valuation per inventory unit.
                cost = legacyConverted.Value * inventory.Price;
                plan.Add(new PlannedIngredient(material, inputQuantity, inputUnitName, legacyConverted.Value, baseUnitType, cost, unitWeight.HasValue ? scaledQuantity * unitWeight.Value : null));
            }

            path.Remove(recipe.Id);
            return null;
        }

        public async Task<Response> WithdrawFromMainAsync(MainInventoryWithdrawaDto request)
        {
            var mainWarehouse = await _unitOfWork.WarehouseRepository.GetByTypeAsync(WarehouseType.MainWarehouse);
            var subWarehouse = await _unitOfWork.WarehouseRepository.GetByTypeAsync(WarehouseType.SubWarehouse);
            if (mainWarehouse == null || subWarehouse == null)
                return new Response { StatusCode = 404, Message = "المخزن الرئيسي أو المخزن الفرعي غير موجود" };

            var prepared = new List<(WarehouseSuppliesInventory Inventory, RawMaterial Material, decimal RequestedQuantity, decimal BaseQuantity, string UnitName)>();
            foreach (var line in request.WithdrawRequests)
            {
                var inventory = await _unitOfWork.WarehouseSuppliesInventoryRepository.GetWithSpecAsync(new WarehouseSuppliesInventorySpecifications(null, mainWarehouse.Id, line.Id, false));
                if (inventory == null || inventory.SupplyItem?.RawMaterial == null)
                    return new Response { StatusCode = 404, Message = $"العنصر {line.Id} غير موجود في المخزن الرئيسي" };

                var material = inventory.SupplyItem.RawMaterial;
                decimal requestedQuantity;
                decimal baseQuantity;
                string unitName;
                if (line.RawMaterialUnitId.HasValue)
                {
                    var selectedUnit = material.Units?.FirstOrDefault(unit => unit.Id == line.RawMaterialUnitId.Value);
                    if (selectedUnit == null || selectedUnit.RawMaterialId != material.Id)
                        return new Response { StatusCode = 400, Message = "الوحدة المحددة لا تنتمي للمادة الخام" };
                    // Warehouse withdrawal contract explicitly supports purchase/middle only.
                    var level = selectedUnit.UnitId == 0 ? string.Empty :
                        selectedUnit.Unit?.Name == material.PurchaseUnitName ? "purchase" :
                        selectedUnit.Unit?.Name == material.MiddleUnitName ? "middle" : string.Empty;
                    if (level == "")
                        return new Response { StatusCode = 400, Message = "السحب مسموح بوحدة الشراء أو الوحدة الوسطى فقط" };
                    requestedQuantity = line.Quantity ?? 0m;
                    var converted = _unitConversion.ToInventoryQuantity(material, requestedQuantity, selectedUnit.Id, selectedUnit);
                    if (!converted.HasValue)
                        return new Response { StatusCode = 400, Message = $"عامل تحويل الوحدة غير صالح للمادة {material.Name}" };
                    baseQuantity = converted.Value;
                    unitName = selectedUnit.Unit?.Name ?? level;
                }
                else if (line.Quantity.HasValue)
                {
                    requestedQuantity = line.Quantity.Value;
                    if (line.UnitLevel == "purchase")
                    {
                        baseQuantity = requestedQuantity * (material.ContainerSize > 0 ? material.ContainerSize : material.MiddleUnitsPerPurchaseUnit ?? 0m);
                        unitName = material.PurchaseUnitName ?? material.ContainerUnitName ?? "وحدة الشراء";
                    }
                    else if (line.UnitLevel == "middle")
                    {
                        var middlesPerPurchase = material.MiddleUnitsPerPurchaseUnit.GetValueOrDefault();
                        var inventoryPerPurchase = material.ContainerSize > 0 ? material.ContainerSize : middlesPerPurchase;
                        if (middlesPerPurchase <= 0 || inventoryPerPurchase <= 0)
                            return new Response { StatusCode = 400, Message = $"تحويل الوحدة الوسطى غير معرف للمادة {material.Name}" };
                        baseQuantity = requestedQuantity * inventoryPerPurchase / middlesPerPurchase;
                        unitName = material.MiddleUnitName ?? material.SubUnitName ?? "الوحدة الوسطى";
                    }
                    else
                        return new Response { StatusCode = 400, Message = "السحب مسموح بوحدة الشراء أو الوحدة الوسطى فقط" };
                }
                else
                {
                    requestedQuantity = line.ContainerQuantity ?? 0m;
                    baseQuantity = requestedQuantity * material.ContainerSize;
                    unitName = material.PurchaseUnitName ?? material.ContainerUnitName ?? "وحدة الشراء";
                }

                if (requestedQuantity <= 0 || baseQuantity <= 0)
                    return new Response { StatusCode = 400, Message = $"بيانات التحويل أو الكمية غير صالحة للمادة {material.Name}" };

                var convertedToInventoryUnit = ConvertToInventoryUnit(baseQuantity, material.UnitType, inventory.UnitType);
                if (!convertedToInventoryUnit.HasValue)
                    return new Response { StatusCode = 400, Message = $"وحدة رصيد المخزون غير متوافقة للمادة {material.Name}" };
                prepared.Add((inventory, material, requestedQuantity, convertedToInventoryUnit.Value, unitName));
            }

            foreach (var group in prepared.GroupBy(line => line.Inventory.Id))
                if (group.First().Inventory.Quantity < group.Sum(line => line.BaseQuantity))
                    return new Response { StatusCode = 400, Message = $"رصيد {group.First().Material.Name} غير كافٍ" };

            foreach (var group in prepared.GroupBy(line => line.Inventory.Id))
            {
                var inventory = group.First().Inventory;
                var material = group.First().Material;
                inventory.Quantity -= group.Sum(line => line.BaseQuantity);
                inventory.ContainerQuantity = material.ContainerSize > 0 ? inventory.Quantity / material.ContainerSize : null;
                inventory.DisplayDetails = $"{material.Name}: الرصيد {inventory.Quantity} {inventory.UnitType}";
                if (inventory.Quantity == 0)
                    _unitOfWork.WarehouseSuppliesInventoryRepository.Delete(inventory);
                else
                    _unitOfWork.WarehouseSuppliesInventoryRepository.Update(inventory);
            }

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
            return new Response { StatusCode = 200, Message = "تم السحب للمخزن الفرعي بنجاح" };
        }
    }
}
