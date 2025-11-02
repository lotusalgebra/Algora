using Microsoft.Extensions.DependencyInjection;
using Algora.Application.Interfaces;
//using Algora.Infrastructure.Persistence.Repositories;

namespace Algora.Infrastructure.Persistence;

public static class RepositoryRegistration
{
    public static IServiceCollection AddRepositoryLayer(this IServiceCollection services)
    {
        //services.AddScoped<IOrderRepository, OrderRepository>();
        //services.AddScoped<ICustomerRepository, CustomerRepository>();
        return services;
    }
}
