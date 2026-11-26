using BrightMinds.Services.IServices;
using BrightMinds.Services.Services;
using Microsoft.Extensions.DependencyInjection;

namespace BrightMinds.Services.ApplicationServicesConfigrations
{
    public static class AddServices
    {
        public static IServiceCollection AddMaterialWarehouseWorkflows(this IServiceCollection services)
        {
            services.AddScoped<MaterialWarehouseWorkflow>();
            return services;
        }
    }
}
