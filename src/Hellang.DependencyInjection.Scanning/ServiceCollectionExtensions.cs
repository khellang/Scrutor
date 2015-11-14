using System;
using Microsoft.Framework.DependencyInjection;

namespace Microsoft.Extensions.DependencyInjection.Scanning
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection Scan(this IServiceCollection services, Action<IAssemblySelector> action)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (action == null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            var selector = new AssemblySelector();

            action(selector);

            selector.Populate(services);

            return services;
        }
    }
}