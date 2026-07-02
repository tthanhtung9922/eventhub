using EventHub.Identity.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EventHub.Identity.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Add infrastructure services here, e.g., database context, repositories, etc.
        string? connectionString = configuration.GetConnectionString("IdentityDb");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("Connection string 'IdentityDb' is not configured.");
        }

        services.AddDbContext<IdentityDbContext>(options => options.UseNpgsql(connectionString));

        return services;
    }
}
