using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Nest;
using Service.Services.BackgroundServices;

namespace Service.Extensions
{
    public static class ServiceExtension
    {
        public static IServiceCollection ServiceLayer(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddSingleton<IElasticClient>(new ElasticClient(new ConnectionSettings(new Uri("http://host.docker.internal:9200")).EnableApiVersioningHeader()));
            services.AddHostedService<SeedDataHostedService>();

            return services;
        }
    }
}
