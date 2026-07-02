using EventHub.Identity.Api;
using EventHub.Modularity;

namespace EventHub.Api;

public static class ModuleExtensions
{
    private static readonly IReadOnlyList<IModule> Modules =  [new IdentityModule()];

    public static void AddModules(this IServiceCollection services, IConfiguration configuration)
    {
        foreach (var module in Modules)
        {
            module.ConfigureServices(services, configuration);
        }
    }

    public static void UseModules(this IEndpointRouteBuilder endpoints)
    {
        foreach (var module in Modules)
        {
            module.MapEndpoints(endpoints);
        }
    }
}
