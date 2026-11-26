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

namespace BrightMinds.Services.Services
{
    public class RecipeService : IRecipeService
    {
        private readonly IUnitOfWork _unitOfWork;

        public RecipeService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        private async Task<Response> ValidateRecipeIngredientsAsync(RecipeDto recipeDto)
        {
            foreach (var ingredient in recipeDto.Ingrediants)
            {
                if (ingredient.IngredientType == IngredientType.RawMaterial)
                {
                    var rawMaterial = await _unitOfWork.RawMaterialRepository.GetAsync(ingredient.RawMaterialId ?? 0);
                    if (rawMaterial == null)
                        return new Response { StatusCode = 404, Message = "المادة الخام المرتبطة غير موجودة" };

                    if (ingredient.RawMaterialUnitId.HasValue)
                    {
                        var selectedUnit = rawMaterial.Units?.FirstOrDefault(unit => unit.Id == ingredient.RawMaterialUnitId.Value);
                        if (selectedUnit == null)
                            return new Response { StatusCode = 400, Message = "الوحدة المحددة لا تنتمي للمادة الخام" };
                        if (selectedUnit.ConversionFactor <= 0)
                            return new Response { StatusCode = 400, Message = "عامل تحويل الوحدة يجب أن يكون أكبر من صفر" };
                        if (!selectedUnit.PricePerUnit.HasValue || selectedUnit.PricePerUnit.Value <= 0)
                            return new Response { StatusCode = 400, Message = "السعر غير محدد للوحدة المختارة" };
                    }
                }
                else if (ingredient.IngredientType == IngredientType.SubRecipe)
                {
                    var subRecipe = await _unitOfWork.RecipeRepository.GetAsync(ingredient.SubRecipeId ?? 0);
                    if (subRecipe == null || subRecipe.IsDeleted)
                        return new Response { StatusCode = 404, Message = "الوصفة الفرعية غير موجودة" };
                }
            }

            return new Response { StatusCode = 200 };
        }

        private async Task<decimal> IncludeRecipePrices(RecipeDetailsDto recipe, HashSet<int>? visited = null)
        {
            visited ??= new HashSet<int>();
            if (recipe.Id > 0 && !visited.Add(recipe.Id))
                return 0m;

            decimal totalCost = 0m;
            foreach (var ingredient in recipe.Ingrediants)
            {
                if (ingredient.SubRecipeId.HasValue)
                {
                    var subRecipe = await _unitOfWork.RecipeRepository.GetWithSpecAsync(new RecipeSpecifications(null, ingredient.SubRecipeId, false));
                    if (subRecipe == null)
                    {
                        ingredient.Price = 0m;
                        continue;
                    }

                    var subDetails = subRecipe.Adapt<RecipeDetailsDto>();
                    var subTotalCost = await IncludeRecipePrices(subDetails, new HashSet<int>(visited));
                    ingredient.Price = subTotalCost * ingredient.Quantity;
                    totalCost += ingredient.Price;
                    continue;
                }

                if (!ingredient.RawMaterialId.HasValue)
                    continue;

                var rawMaterial = await _unitOfWork.RawMaterialRepository.GetAsync(ingredient.RawMaterialId.Value);
                if (rawMaterial == null)
                {
                    ingredient.Price = 0m;
                    continue;
                }

                if (ingredient.RawMaterialUnitId.HasValue)
                {
                    var selectedUnit = rawMaterial.Units?.FirstOrDefault(unit => unit.Id == ingredient.RawMaterialUnitId.Value);
                    if (selectedUnit?.PricePerUnit.HasValue == true)
                    {
                        ingredient.Price = selectedUnit.PricePerUnit.Value * ingredient.Quantity;
                        ingredient.RawMaterialUnitName = selectedUnit.Unit?.Name;
                        ingredient.RawMaterialUnitWeightInKg = selectedUnit.WeightInKg;
                        ingredient.RawMaterialUnitPrice = selectedUnit.PricePerUnit;
                        totalCost += ingredient.Price;
                        continue;
                    }
                }

                var legacyPrice = rawMaterial.PricePerInnerUnit ?? rawMaterial.PricePerUnit;
                ingredient.Price = legacyPrice * ingredient.Quantity;
                totalCost += ingredient.Price;
            }

            recipe.TotalCost = totalCost;
            return totalCost;
        }

        private async Task<decimal> CalculateTotalSizeAsync(RecipeDto recipeDto)
        {
            decimal totalSizeKg = 0m;

            foreach (var ingredient in recipeDto.Ingrediants)
            {
                if (ingredient.IngredientType == IngredientType.RawMaterial)
                {
                    var rawMaterial = await _unitOfWork.RawMaterialRepository.GetAsync(ingredient.RawMaterialId ?? 0);
                    if (rawMaterial == null)
                        continue;

                    if (ingredient.RawMaterialUnitId.HasValue)
                    {
                        var selectedUnit = rawMaterial.Units?.FirstOrDefault(unit => unit.Id == ingredient.RawMaterialUnitId.Value);
                        if (selectedUnit == null)
                            continue;

                        if (selectedUnit.WeightInKg.HasValue)
                        {
                            totalSizeKg += ingredient.Quantity * selectedUnit.WeightInKg.Value;
                            continue;
                        }

                        if (selectedUnit.ConversionFactor > 0 && rawMaterial.UnitWeightInKg.HasValue)
                        {
                            totalSizeKg += ingredient.Quantity * selectedUnit.ConversionFactor * rawMaterial.UnitWeightInKg.Value;
                            continue;
                        }
                    }

                    if (rawMaterial.UnitType == UnitType.kg)
                        totalSizeKg += ingredient.UnitType == UnitType.g ? ingredient.Quantity / 1000m : ingredient.Quantity;
                    else if (rawMaterial.UnitType == UnitType.g)
                        totalSizeKg += ingredient.UnitType == UnitType.kg ? ingredient.Quantity : ingredient.Quantity / 1000m;
                    else if (rawMaterial.UnitWeightInKg.HasValue)
                        totalSizeKg += ingredient.Quantity * rawMaterial.UnitWeightInKg.Value;
                }
                else if (ingredient.IngredientType == IngredientType.SubRecipe)
                {
                    var subRecipe = await _unitOfWork.RecipeRepository.GetAsync(ingredient.SubRecipeId ?? 0);
                    if (subRecipe != null)
                        totalSizeKg += subRecipe.TotalSize * ingredient.Quantity;
                }
            }

            return totalSizeKg;
        }

        public async Task<Response> CreateAsync(RecipeDto recipeDto)
        {
            var validation = await ValidateRecipeIngredientsAsync(recipeDto);
            if (validation.StatusCode != 200)
                return validation;

            var recipe = recipeDto.Adapt<Recipe>();
            recipe.Ingrediants.Clear();

            foreach (var dto in recipeDto.Ingrediants)
                recipe.Ingrediants.Add(dto.Adapt<RecipeIngrediant>());

            recipe.TotalCost = await IncludeRecipePrices(recipe.Adapt<RecipeDetailsDto>());
            var calculatedSizeKg = await CalculateTotalSizeAsync(recipeDto);
            var lossKg = recipeDto.NormalLossSize / 1000m;
            recipe.TotalSize = calculatedSizeKg - lossKg;
            if (recipe.TotalSize < 0)
                return new Response { StatusCode = 400, Message = "معدل الفقد الطبيعي أكبر من الوزن الكلي للوصفة" };

            recipe.CostPerKilo = recipe.TotalSize > 0 ? recipe.TotalCost / recipe.TotalSize : 0m;

            await _unitOfWork.RecipeRepository.Add(recipe);
            await _unitOfWork.CompleteAsync();

            var result = recipe.Adapt<RecipeDetailsDto>();
            await IncludeRecipePrices(result);
            return new Response { StatusCode = 201, Message = "تم انشاء الوصفة بنجاح", Data = result };
        }

        public async Task<Response> UpdateAsync(RecipeDto recipeDto, int id)
        {
            var validation = await ValidateRecipeIngredientsAsync(recipeDto);
            if (validation.StatusCode != 200)
                return validation;

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
            var calculatedSizeKg = await CalculateTotalSizeAsync(recipeDto);
            var lossKg = recipeDto.NormalLossSize / 1000m;
            recipe.TotalSize = calculatedSizeKg - lossKg;
            if (recipe.TotalSize < 0)
                return new Response { StatusCode = 400, Message = "معدل الفقد الطبيعي أكبر من الوزن الكلي للوصفة" };

            recipe.CostPerKilo = recipe.TotalSize > 0 ? recipe.TotalCost / recipe.TotalSize : 0m;

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

        public async Task<Response> AddRecipePortions(RecipePortionDto recipePortions) => throw new NotImplementedException();
        public async Task<Response> GetAllRecipesPortions(RecipePortionsSpecParams specParams) => throw new NotImplementedException();
        public async Task<Response> GetRecipePortionById(int id) => throw new NotImplementedException();
        public async Task<Response> UpdateRecipePortion(RecipePortionDto dto) => throw new NotImplementedException();
        public async Task<Response> DeleteRecipePortion(int id) => throw new NotImplementedException();
        public async Task<Response> DeleteAllRecipePortions(int recipeId) => throw new NotImplementedException();
    }
}
