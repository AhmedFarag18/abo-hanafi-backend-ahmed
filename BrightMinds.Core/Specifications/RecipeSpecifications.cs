using BrightMinds.Core.ISpecificationParams;
using BrightMinds.Core.Models;
using System.Linq.Expressions;

namespace BrightMinds.Core.Specifications
{
    public class RecipeSpecifications : BaseSpecification<Recipe>
    {
        public RecipeSpecifications(IRecipeSpecParams? specParams, int? id, bool isCount) : base(BuildCriteria(specParams, id))
        {
            AddInclude(recipe => recipe.Ingrediants);
            AddInclude(recipe => recipe.Portions);
            AddInclude(recipe => recipe.PortionCategory);
            AddIncludeString("Ingrediants.RawMaterial");
            AddIncludeString("Ingrediants.RawMaterial.Units");
            AddIncludeString("Ingrediants.RawMaterial.Units.Unit");
            AddIncludeString("Ingrediants.RawMaterialUnit");
            AddIncludeString("Ingrediants.RawMaterialUnit.Unit");
            AddIncludeString("Ingrediants.SubRecipe");

            if (!isCount && specParams != null)
            {
                ApplyPagination(specParams.PageSize * (specParams.PageIndex - 1), specParams.PageSize);
                if (string.IsNullOrEmpty(specParams.OrderBy) || specParams.OrderBy == "new")
                    AddOrderByDesc(recipe => recipe.Id);
                else
                    AddOrderBy(recipe => recipe.Id);
            }
        }

        private static Expression<Func<Recipe, bool>> BuildCriteria(IRecipeSpecParams? specParams = null, int? id = null)
        {
            return recipe => (id == null || recipe.Id == id) &&
                             !recipe.IsDeleted &&
                             (specParams == null || string.IsNullOrEmpty(specParams.SearchName) || recipe.Name.ToLower().Contains(specParams.SearchName.ToLower())) &&
                             (specParams == null || string.IsNullOrEmpty(specParams.Category) || recipe.Category == specParams.Category);
        }
    }
}
