
using System.Linq;
using Microsoft.Framework.DependencyInjection;

namespace Scrutor.Tests
{
    internal static class ServiceCollectionExtensions
    {
        internal static ServiceDescriptor GetDescriptor<T>(this IServiceCollection services)
        {
            return services.GetDescriptors<T>().SingleOrDefault();
        }

        internal static ServiceDescriptor[] GetDescriptors<T>(this IServiceCollection services)
        {
            return services.Where(x => x.ServiceType == typeof(T)).ToArray();
        }
    }
}