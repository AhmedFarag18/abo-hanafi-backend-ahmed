using BrightMinds.Core.Interfaces;
using BrightMinds.Infrastructure.DataAccess;
using BrightMinds.Infrastructure.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace BrightMinds.Services.ApplicationServicesConfigrations
{
    public static class AddRepositories
    {
        public static IServiceCollection AddMaterialWarehouseRepositories(this IServiceCollection services)
        {
            services.AddScoped<IUnitOfWork, UnitOfWork>();
            services.AddScoped<IRawMaterialRepository, RawMaterialRepository>();
            services.AddScoped<IWarehouseSuppliesInventoryRepository, WarehouseSuppliesInventoryRepository>();
            return services;
        }
    }
}
