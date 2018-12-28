using System;
using System.Collections.Generic;
using System.Linq;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class ServiceCollectionExtensions
    {
        /// <summary>
        /// Registers <typeparamref name="TConcrete"/> as a composite that wraps all
        /// existing registrations of <typeparamref name="TInterface"/>.
        /// </summary>
        /// <typeparam name="TInterface"></typeparam>
        /// <typeparam name="TConcrete"></typeparam>
        /// <param name="services"></param>
        public static void AddComposite<TInterface, TConcrete>(this IServiceCollection services)
          where TInterface : class
          where TConcrete : class, TInterface
        {
            var wrappedDescriptors = services.Where(s => s.ServiceType == typeof(TInterface)).ToList();
            foreach (var descriptor in wrappedDescriptors)
                services.Remove(descriptor);

            var objectFactory = ActivatorUtilities.CreateFactory(
              typeof(TConcrete),
              new[] { typeof(IEnumerable<TInterface>) });

            var maxWrappedServiceLifetime = wrappedDescriptors
                .Select(d => d.Lifetime)
                .DefaultIfEmpty(ServiceLifetime.Scoped)
                .Max();

            services.Add(ServiceDescriptor.Describe(
              typeof(TInterface),
              s => (TInterface)objectFactory(s, new[] { wrappedDescriptors.Select(d => s.CreateInstance(d)).Cast<TInterface>() }),
              maxWrappedServiceLifetime)
            );
        }

        private static object CreateInstance(this IServiceProvider services, ServiceDescriptor descriptor)
        {
            if (descriptor.ImplementationInstance != null)
                return descriptor.ImplementationInstance;

            if (descriptor.ImplementationFactory != null)
                return descriptor.ImplementationFactory(services);

            return ActivatorUtilities.GetServiceOrCreateInstance(services, descriptor.ImplementationType);
        }
    }
}
