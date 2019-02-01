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
        /// <param name="serviceLifetime"></param>
        public static void AddComposite<TService, TImplementation>(this IServiceCollection services, ServiceLifetime serviceLifetime)
          where TService : class
          where TImplementation : class, TService
        {
            var wrappedDescriptors = services.Where(s => typeof(TService).IsAssignableFrom(s.ServiceType)).ToList();
            foreach (var descriptor in wrappedDescriptors)
                services.Remove(descriptor);

            var objectFactory = ActivatorUtilities.CreateFactory(
              typeof(TImplementation),
              new[] { typeof(IEnumerable<TService>) });

            services.Add(ServiceDescriptor.Describe(
              typeof(TService),
              s => (TService)objectFactory(s, new[] { wrappedDescriptors.Select(d => s.GetInstance(d)).Cast<TService>() }),
              serviceLifetime)
            );
        }

        public static void AddCompositeTransient<TService, TImplementation>(this IServiceCollection services)
          where TService : class
          where TImplementation : class, TService
         => services.AddComposite<TService, TImplementation>(ServiceLifetime.Transient);

        public static void AddCompositeScoped<TService, TImplementation>(this IServiceCollection services)
          where TService : class
          where TImplementation : class, TService
         => services.AddComposite<TService, TImplementation>(ServiceLifetime.Scoped);

        public static void AddCompositeSingleton<TService, TImplementation>(this IServiceCollection services)
          where TService : class
          where TImplementation : class, TService
         => services.AddComposite<TService, TImplementation>(ServiceLifetime.Singleton);
    }
}
