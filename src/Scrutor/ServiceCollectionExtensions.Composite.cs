using System;
using System.Collections.Generic;
using System.Linq;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class ServiceCollectionExtensions
    {
        /// <summary>
        /// Registers <typeparamref name="TImplementation"/> as a composite that wraps all
        /// existing registrations of <typeparamref name="TService"/>.
        /// </summary>
        /// <typeparam name="TService"></typeparam>
        /// <typeparam name="TImplementation"></typeparam>
        /// <param name="services"></param>
        public static void AddComposite<TService, TImplementation>(this IServiceCollection services)
          where TService : class
          where TImplementation : class, TService
        {
            var wrappedDescriptors = services.Where(s => s.ServiceType == typeof(TService)).ToList();
            foreach (var descriptor in wrappedDescriptors)
                services.Remove(descriptor);

            var objectFactory = ActivatorUtilities.CreateFactory(
              typeof(TImplementation),
              new[] { typeof(IEnumerable<TService>) });

            var maxWrappedServiceLifetime = wrappedDescriptors
                .Select(d => d.Lifetime)
                .DefaultIfEmpty(ServiceLifetime.Scoped)
                .Max();

            services.Add(ServiceDescriptor.Describe(
              typeof(TService),
              s => (TService)objectFactory(s, new[] { wrappedDescriptors.Select(d => s.GetInstance(d)).Cast<TService>() }),
              maxWrappedServiceLifetime)
            );
        }
    }
}
