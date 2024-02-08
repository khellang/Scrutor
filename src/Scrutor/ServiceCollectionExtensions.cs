using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;

namespace Scrutor;

internal static class ServiceCollectionExtensions
{
    public static bool HasRegistration(this IServiceCollection services, Type serviceType)
    {
        return services.Any(x => x.ServiceType == serviceType);
    }
}
