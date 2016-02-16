using System.Linq;
using Microsoft.Extensions.DependencyInjection;

namespace Scrutor.Tests
{
    internal static class ServiceCollectionExtensions
    {
        public static ServiceDescriptor GetDescriptor<T>(this IServiceCollection services)
        {
            return services.GetDescriptors<T>().Single();
        }

        public static ServiceDescriptor[] GetDescriptors<T>(this IServiceCollection services)
        {
            return services.Where(x => x.ServiceType == typeof(T)).ToArray();
        }
    }
}
