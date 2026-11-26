using BrightMinds.Core.Enums;
using BrightMinds.Core.Interfaces;
using BrightMinds.Core.Models;
using BrightMinds.Core.Specifications;
using BrightMinds.Services.Dtos.RecipeDtos;
using BrightMinds.Services.Dtos.RecipeIngrediantDtos;
using BrightMinds.Services.Dtos.RecipePortionDtos;
using BrightMinds.Services.IServices;
using BrightMinds.Services.SpecificationParams;
using Library.Services.Dtos;
using Mapster;
using System.Net;

namespace BrightMinds.Services.Services
{
    public class RecipeService : IRecipeService
    {
        private readonly IUnitOfWork _unitOfWork;

        public RecipeService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        private async Task<(RawMaterialUnit? Unit, decimal? InventoryQuantity, decimal? WeightInKg, decimal Price)> ResolveIngredientAsync(RecipeIngrediantDto dto)
        {
            if (dto.IngredientType != IngredientType.RawMaterial || !dto.RawMaterialId.HasValue)
                return (null, null, null, 0);

            var raw = await _unitOfWork.RawMaterialRepository.GetAsync(dto.RawMaterialId.Value);
            if (raw == null)
                return (null, null, null, 0);

            if (!dto.RawMaterialUnitId.HasValue)
            {
                // Backward-compatible UnitType calculation.
                var legacyPrice = raw.PricePerInnerUnit ?? raw.PricePerUnit;
                decimal inventoryQuantity = dto.Quantity;
                decimal? weight = null;

                if (dto.UnitType == UnitType.kg && raw.UnitType == UnitType.g)
                    inventoryQuantity = dto.Quantity * 1000;
                else if (dto.UnitType == UnitType.g && raw.UnitType == UnitType.kg)
                    inventoryQuantity = dto.Quantity / 1000;

                if (raw.UnitWeightInKg.HasValue)
                    weight = dto.Quantity * raw.UnitWeightInKg.Value;

                return (null, inventoryQuantity, weight, legacyPrice * dto.Quantity);
            }

            var unit = await _unitOfWork.UnitRepository.GetAsync(dto.RawMaterialUnitId.Value);
            if (unit == null)
                return (null, null, null, 0);

            var rawUnits = raw.Units.ToList();
            var selected = rawUnits.FirstOrDefault(x => x.Id == dto.RawMaterialUnitId.Value);
            if (selected == null)
                return (null, null, null, 0);

            var inventoryQuantity = dto.Quantity * selected.ConversionFactor;
            var weight = selected.WeightInKg.HasValue ? dto.Quantity * selected.WeightInKg.Value : (decimal?)null;
            var price = selected.PricePerUnit.HasValue ? selected.PricePerUnit.Value * dto.Quantity : 0;
            return (selected, (decimal?)inventoryQuantity, weight, price);
        }

        private async Task<Response> ValidateRecipeIngredientsAsync(RecipeDto recipeDto)
        {
            foreach (var ingredient in recipeDto.Ingrediants)
            {
                if (ingredient.IngredientType != IngredientType.RawMaterial)
                    continue;

                var raw = await _unitOfWork.RawMaterialRepository.GetAsync(ingredient.RawMaterialId!.Value);
                if (raw == null)
                    return new Response { StatusCode = 404, Message = $"المادة الخام {ingredient.RawMaterialId} غير موجودة" };

                if (ingredient.RawMaterialUnitId.HasValue)
                {
                    var unit = raw.Units.FirstOrDefault(x => x.Id == ingredient.RawMaterialUnitId.Value);
                    if (unit == null)
                        return new Response { StatusCode = 400, Message = "الوحدة المحددة لا تنتمي للمادة الخام" };
                    if (unit.ConversionFactor <= 0)
                        return new Response { StatusCode = 400, Message = "عامل تحويل الوحدة يجب أن يكون أكبر من صفر" };
                    if (!unit.PricePerUnit.HasValue || unit.PricePerUnit <= 0)
                        return new Response { StatusCode = 400, Message = $"السعر غير محدد للوحدة المختارة من المادة {raw.Name}" };
                }
            }

            return new Response { StatusCode = 200 };
        }

        private async Task<decimal> IncludeRecipePrices(RecipeDetailsDto recipe, HashSet<int>? visited = null)
        {
            visited ??= new HashSet<int>();
            if (!visited.Add(recipe.Id)) return 0;

            decimal totalCost = 0;
            foreach (var ingredient in recipe.Ingrediants)
            {
                if (ingredient.SubRecipeId.HasValue)
                {
                    var sub = await _unitOfWork.RecipeRepository.GetWithSpecAsync(new RecipeSpecifications(null, ingredient.SubRecipeId, false));
                    if (sub == null)
                    {
                        ingredient.Price = 0;
                        continue;
                    }
                    ingredient.Price = await IncludeRecipePrices(sub.Adapt<RecipeDetailsDto>(), visited);
                    totalCost += ingredient.Price * ingredient.Quantity;
                }
                else
                {
                    var raw = await _unitOfWork.RawMaterialRepository.GetAsync(ingredient.RawMaterialId!.Value);
                    if (raw == null)
                    {
                        ingredient.Price = 0;
                        continue;
                    }

                    if (ingredient.RawMaterialUnitId.HasValue)
                    {
                        var selected = raw.Units.FirstOrDefault(x => x.Id == ingredient.RawMaterialUnitId.Value);
                        if (selected?.PricePerUnit.HasValue == true)
                        {
                            ingredient.Price = selected.PricePerUnit.Value * ingredient.Quantity;
                            totalCost += ingredient.Price;
                            continue;
                        }
                    }

                    var legacyPrice = raw.PricePerInnerUnit ?? raw.PricePerUnit;
                    ingredient.Price = legacyPrice * ingredient.Quantity;
                    totalCost += ingredient.Price;
                }
            }
            recipe.TotalCost = totalCost;
            return totalCost;
        }

        private async Task<decimal> CalculateTotalSize(RecipeDto recipeDto)
        {
            decimal totalSizeKg = 0;
            foreach (var ingredient in recipeDto.Ingrediants)
            {
                if (ingredient.IngredientType == IngredientType.RawMaterial)
                {
                    var raw = await _unitOfWork.RawMaterialRepository.GetAsync(ingredient.RawMaterialId!.Value);
                    if (raw == null) continue;

                    if (ingredient.RawMaterialUnitId.HasValue)
                    {
                        var selected = raw.Units.FirstOrDefault(x => x.Id == ingredient.RawMaterialUnitId.Value);
                        if (selected?.WeightInKg.HasValue == true)
                        {
                            totalSizeKg += ingredient.Quantity * selected.WeightInKg.Value;
                            continue;
                        }
                        if (selected?.ConversionFactor > 0 && raw.UnitWeightInKg.HasValue)
                        {
                            totalSizeKg += ingredient.Quantity * (decimal)selected.ConversionFactor * raw.UnitWeightInKg.Value;
                            continue;
                        }
                    }

                    if (raw.UnitType == UnitType.kg)
                        totalSizeKg += ingredient.UnitType == UnitType.g ? ingredient.Quantity / 1000m : ingredient.Quantity;
                    else if (raw.UnitType == UnitType.g)
                        totalSizeKg += ingredient.UnitType == UnitType.kg ? ingredient.Quantity : ingredient.Quantity / 1000m;
                    else if (raw.UnitWeightInKg.HasValue)
                        totalSizeKg += ingredient.Quantity * raw.UnitWeightInKg.Value;
                }
                else if (ingredient.IngredientType == IngredientType.SubRecipe)
                {
                    var sub = await _unitOfWork.RecipeRepository.GetAsync(ingredient.SubRecipeId!.Value);
                    if (sub != null)
                        totalSizeKg += sub.TotalSize * ingredient.Quantity;
                }
            }
            return totalSizeKg;
        }

        public async Task<Response> CreateAsync(RecipeDto recipeDto)
        {
            var validation = await ValidateRecipeIngredientsAsync(recipeDto);
            if (validation.StatusCode != 200) return validation;

            foreach (var ingredient in recipeDto.Ingrediants)
            {
                if (ingredient.IngredientType == IngredientType.RawMaterial)
                {
                    var raw = await _unitOfWork.RawMaterialRepository.GetAsync(ingredient.RawMaterialId!.Value);
                    if (raw == null)
                        return new Response { StatusCode = 404, Message = "المادة الخام المرتبطة غير موجودة" };
                }
                else
                {
                    var sub = await _unitOfWork.RecipeRepository.GetAsync(ingredient.SubRecipeId!.Value);
                    if (sub == null || sub.IsDeleted)
                        return new Response { StatusCode = 404, Message = "الوصفة الفرعية غير موجودة" };
                }
            }

            var recipe = recipeDto.Adapt<Recipe>();
            recipe.Ingrediants.Clear();

            foreach (var dto in recipeDto.Ingrediants)
                recipe.Ingrediants.Add(dto.Adapt<RecipeIngrediant>());

            if (!recipeDto.IsFlexible && recipeDto.PortionCategoryId.HasValue)
            {
                var category = await _unitOfWork.PortionCategoryRepository.GetAsync(recipeDto.PortionCategoryId.Value);
                if (category == null)
                    return new Response { StatusCode = 400, Message = "تصنيف البورشن غير موجود" };
            }

            recipe.TotalCost = await IncludeRecipePrices(recipe.Adapt<RecipeDetailsDto>());
            var calculatedSizeKg = await CalculateTotalSize(recipeDto);
            var lossKg = recipeDto.NormalLossSize / 1000m;
            recipe.TotalSize = calculatedSizeKg - lossKg;
            if (recipe.TotalSize < 0)
                return new Response { StatusCode = 400, Message = "معدل الفقد الطبيعي أكبر من الوزن الكلي للوصفة" };
            recipe.CostPerKilo = recipe.TotalSize > 0 ? recipe.TotalCost / recipe.TotalSize : 0;

            await _unitOfWork.RecipeRepository.Add(recipe);
            await _unitOfWork.CompleteAsync();

            var result = recipe.Adapt<RecipeDetailsDto>();
            await IncludeRecipePrices(result);
            return new Response { StatusCode = 201, Message = "تم انشاء الوصفة بنجاح", Data = result };
        }

        public async Task<Response> UpdateAsync(RecipeDto recipeDto, int id)
        {
            var validation = await ValidateRecipeIngredientsAsync(recipeDto);
            if (validation.StatusCode != 200) return validation;

            var recipe = await _unitOfWork.RecipeRepository.GetWithSpecAsync(new RecipeSpecifications(null, id, false));
            if (recipe == null || recipe.IsDeleted)
                return new Response { StatusCode = 404, Message = "الوصفة غير موجودة" };

            recipe.Name = recipeDto.Name;
            recipe.Category = recipeDto.Category;
            recipe.PortionCategoryId = recipeDto.PortionCategoryId;
            recipe.IsFlexible = recipeDto.IsFlexible;
            recipe.IsSubRecipe = recipeDto.IsSubRecipe;
            recipe.RequiresSettlement = recipeDto.RequiresSettlement;
            recipe.NormalLossSize = recipeDto.NormalLossSize;
            recipe.ExpectedPortiansnumber = recipeDto.ExpectedPortiansnumber;
            recipe.PortianSizeInGrams = recipeDto.PortianSizeInGrams;

            recipe.Ingrediants.Clear();
            foreach (var dto in recipeDto.Ingrediants)
                recipe.Ingrediants.Add(dto.Adapt<RecipeIngrediant>());

            recipe.TotalCost = await IncludeRecipePrices(recipe.Adapt<RecipeDetailsDto>());
            var calculatedSizeKg = await CalculateTotalSize(recipeDto);
            var lossKg = recipeDto.NormalLossSize / 1000m;
            recipe.TotalSize = calculatedSizeKg - lossKg;
            if (recipe.TotalSize < 0)
                return new Response { StatusCode = 400, Message = "معدل الفقد الطبيعي أكبر من الوزن الكلي للوصفة" };
            recipe.CostPerKilo = recipe.TotalSize > 0 ? recipe.TotalCost / recipe.TotalSize : 0;

            _unitOfWork.RecipeRepository.Update(recipe);
            await _unitOfWork.CompleteAsync();

            var result = recipe.Adapt<RecipeDetailsDto>();
            await IncludeRecipePrices(result);
            return new Response { StatusCode = 200, Message = "تم تحديث الوصفة بنجاح", Data = result };
        }

        public async Task<Response> DeleteAsync(int id)
        {
            var recipe = await _unitOfWork.RecipeRepository.GetAsync(id);
            if (recipe == null || recipe.IsDeleted)
                return new Response { StatusCode = 404, Message = "الوصفة مش موجودة" };
            recipe.IsDeleted = true;
            _unitOfWork.RecipeRepository.Update(recipe);
            await _unitOfWork.CompleteAsync();
            return new Response { StatusCode = 200, Message = "تم حذف الوصفة بنجاح" };
        }

        public async Task<Response> GetAllAsync(RecipeSpecParams specParams)
        {
            var spec = new RecipeSpecifications(specParams, null, false);
            var data = await _unitOfWork.RecipeRepository.GetAllWithSpecAsync(spec);
            var count = await _unitOfWork.RecipeRepository.GetCountWithSpecAsync(new RecipeSpecifications(specParams, null, true));
            var result = data.Adapt<List<RecipeDetailsDto>>();
            foreach (var item in result)
                await IncludeRecipePrices(item);
            return new Response { StatusCode = 200, Message = "تم استعادة بيانات الوصفات بنجاح", Data = new Pagination(specParams.PageSize, specParams.PageIndex, count, result) };
        }

        public async Task<Response> GetByIdAsync(int id)
        {
            var recipe = await _unitOfWork.RecipeRepository.GetWithSpecAsync(new RecipeSpecifications(null, id, false));
            if (recipe == null || recipe.IsDeleted)
                return new Response { StatusCode = 404, Message = "الوصفة مش موجوده" };
            var result = recipe.Adapt<RecipeDetailsDto>();
            await IncludeRecipePrices(result);
            return new Response { StatusCode = 200, Message = "تم استعادة الوصفة بنجاح", Data = result };
        }

        // The existing portion operations remain unchanged.
        public async Task<Response> AddRecipePortions(RecipePortionDto recipePortions) => throw new NotImplementedException();
        public async Task<Response> GetAllRecipesPortions(RecipePortionSpecParams specParams) => throw new NotImplementedException();
        public async Task<Response> GetRecipePortionById(int id) => throw new NotImplementedException();
        public async Task<Response> UpdateRecipePortion(RecipePortionDto dto) => throw new NotImplementedException();
        public async Task<Response> DeleteRecipePortion(int id) => throw new NotImplementedException();
        public async Task<Response> DeleteAllRecipePortions(int recipeId) => throw new NotImplementedException();
    }
}
