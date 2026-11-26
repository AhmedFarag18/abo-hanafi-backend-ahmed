using BrightMinds.Core.Enums;
using BrightMinds.Core.Models;
using BrightMinds.Services.Dtos.RawMaterialDtos;
using Mapster;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BrightMinds.Services.ApplicationServicesConfigrations
{
    public static class AddProfiles
    {
        public static IServiceCollection AddApplicationProfiles(this IServiceCollection services, IConfiguration configuration)
        {
            var baseUrl = configuration["ApiBaseUrl"];
            TypeAdapterConfig<RawMaterial, RawMaterialDetailsDto>
                .NewConfig()
                .Map(dest => dest.UnitType, src => src.UnitType.ToString())
                .Map(dest => dest.IsWeightBased, src => src.IsWeightBased)
                .Map(dest => dest.TotalWeightPerContainer, src => src.TotalWeightPerContainer)
                .Map(dest => dest.CategoryName, src => src.Category != null ? src.Category.Name : null)
                .Map(dest => dest.PriceDisplayUnit, src => src.UnitType.ToString());

            TypeAdapterConfig<Category, CategoryDetailsDto>.NewConfig();
            TypeAdapterConfig<Category, CategoryDto>.NewConfig();

            TypeAdapterConfig<RecipeIngrediant, RecipeIngrediantDetailsDto>
                .NewConfig()
                .Map(dest => dest.RawMaterialName, src => src.RawMaterial != null ? src.RawMaterial.Name : null)
                .Map(dest => dest.SubRecipeName, src => src.SubRecipe != null ? src.SubRecipe.Name : null)
                .Map(dest => dest.RawMaterialUnitName, src => src.RawMaterialUnit != null && src.RawMaterialUnit.Unit != null ? src.RawMaterialUnit.Unit.Name : null);

            TypeAdapterConfig<WarehouseSuppliesInventory, WarehouseInventoryItemDetailsDto>.NewConfig()
                .Map(dest => dest.InventoryItemName, src => src.SupplyItem.RawMaterial.Name)
                .Map(dest => dest.Price, src => src.Price)
                .Map(dest => dest.Quantity, src => src.Quantity)
                .Map(dest => dest.UnitType, src => src.UnitType)
                .Map(dest => dest.CategoryId, src => src.SupplyItem.RawMaterial.CategoryId)
                .Map(dest => dest.CategoryName, src => src.SupplyItem.RawMaterial.Category.Name)
                .Map(dest => dest.ContainerQuantity, src => src.ContainerQuantity)
                .Map(dest => dest.ContainerUnitName, src => src.SupplyItem.RawMaterial.ContainerUnitName)
                .Map(dest => dest.DisplayDetails, src => src.DisplayDetails)
                .Map(dest => dest.SubUnitName, src => src.SupplyItem.RawMaterial.SubUnitName)
                .Map(dest => dest.TotalWeightInKg, src => src.SupplyItem.RawMaterial.UnitWeightInKg.HasValue ? src.Quantity * src.SupplyItem.RawMaterial.UnitWeightInKg.Value : (decimal?)null)
                .Map(dest => dest.HandlingType, src => src.SupplyItem.RawMaterial.HandlingType)
                .Map(dest => dest.ContainerSize, src => src.SupplyItem.RawMaterial.ContainerSize)
                .Map(dest => dest.ContainerPrice, src => src.Price * src.SupplyItem.RawMaterial.ContainerSize);

            TypeAdapterConfig<SupplyItem, WarehouseSuppliesInventory>.NewConfig()
                .Map(dest => dest.SupplyItemId, src => src.Id)
                .Map(dest => dest.Quantity, src => src.Quantity)
                .Map(dest => dest.Price, src => src.UnitPrice)
                .Ignore(dest => dest.Id);

            TypeAdapterConfig<RecipeIngrediant, ProductIngredientSnapshoot>.NewConfig()
                .Map(dest => dest.Name, src => src.RawMaterial.Name)
                .Map(dest => dest.Quantity, src => src.Quantity)
                .Map(dest => dest.UnitType, src => src.UnitType)
                .Ignore(dest => dest.Id)
                .Ignore(dest => dest.ProductId)
                .Ignore(dest => dest.Price);

            TypeAdapterConfig<WarehouseProductsInventory, WarehouseProductDetailsDto>.NewConfig()
                .Map(dest => dest.Id, src => src.Id)
                .Map(dest => dest.Name, src => src.ProductPortion.Name)
                .Map(dest => dest.Quantity, src => src.Quantity)
                .Map(dest => dest.SizeIngrams, src => src.Size);

            TypeAdapterConfig<RequestedItemDto, RequestedSalesWarehouseInventoryItem>.NewConfig()
                .Map(dest => dest.InventoryItemId, src => src.InventoryItemId)
                .Map(dest => dest.Quantity, src => src.Quantity);

            TypeAdapterConfig<SalesWarehouseWithdrawRequest, SalesWarehouseWithdrawRequestDetailsDto>.NewConfig()
                .Map(dest => dest.Date, src => src.CreationDate);

            TypeAdapterConfig<RequestedSalesWarehouseInventoryItem, RequestedItemDetailsDto>.NewConfig()
                .Map(dest => dest.InventoryItemId, src => src.InventoryItemId)
                .Map(dest => dest.Quantity, src => src.Quantity)
                .Map(dest => dest.Name, src => src.ProductPortion.Name)
                .Map(dest => dest.Size, src => src.ProductPortion.SizeInGrams);

            TypeAdapterConfig<AppUser, UserDto>.NewConfig()
                .Map(dest => dest.DisplayName, src => src.FirstName + " " + src.LastName);

            TypeAdapterConfig<BaseUnit, BaseUnitDetailsDto>.NewConfig()
                .Map(dest => dest.CategoryName, src => src.Category.Name);

            TypeAdapterConfig<SupplyItem, SupplyItemDetailsDto>.NewConfig()
                .Map(dest => dest.ContainerQuantity, src => src.ContainerQuantity)
                .Map(dest => dest.Quantity, src => src.Quantity)
                .Map(dest => dest.DisplayDetails, src => src.DisplayDetails)
                .Map(dest => dest.TotalPrice, src => src.Quantity * src.UnitPrice)
                .Map(dest => dest.UnitType, src => src.UnitType)
                .Map(dest => dest.RawMaterialId, src => src.RawMaterialId)
                .Map(dest => dest.SupplyId, src => src.SupplyId)
                .Map(dest => dest.ReceivedBy, src => src.ReceivedBy)
                .Map(dest => dest.OriginalQuantity, src => src.OriginalQuantity)
                .Map(dest => dest.OriginalPrice, src => src.OriginalPrice);

            TypeAdapterConfig<Recipe, RecipeDetailsDto>.NewConfig()
                .Map(dest => dest.PortionCategoryName, src => src.PortionCategory != null ? src.PortionCategory.Name : null);

            TypeAdapterConfig<FinalProduct, FinalProductDetailsDto>.NewConfig()
                .Map(dest => dest.CategoryName, src => src.Category != null ? src.Category.Name : null)
                .Map(dest => dest.SubCategoryName, src => src.SubCategory != null ? src.SubCategory.Name : null);

            TypeAdapterConfig<Product, ProductDetailsDto>.NewConfig()
                .Map(dest => dest.SettledByUserName, src => src.SettledByUser != null ? $"{src.SettledByUser.FirstName} {src.SettledByUser.LastName}" : null);

            return services;
        }
    }
}
