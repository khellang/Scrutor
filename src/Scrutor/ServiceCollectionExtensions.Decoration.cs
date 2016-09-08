using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;

namespace Scrutor
{
    public static partial class ServiceCollectionExtensions
    {
        /// <summary>
        /// Decorates all registered services of type <typeparamref name="TService"/>
        /// using the <paramref name="decorator"/> function.
        /// </summary>
        /// <typeparam name="TService">The type of services to decorate.</typeparam>
        /// <param name="services">The services to add to.</param>
        /// <param name="decorator">The decorator function.</param>
        /// <exception cref="InvalidOperationException">If no service of <typeparamref name="TService"/> has been registered.</exception>
        /// <exception cref="ArgumentNullException">If either the <paramref name="services"/>
        /// or <paramref name="decorator"/> arguments are <c>null</c>.</exception>
        public static IServiceCollection Decorate<TService>(this IServiceCollection services, Func<TService, IServiceProvider, TService> decorator)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (decorator == null)
            {
                throw new ArgumentNullException(nameof(decorator));
            }

            var descriptors = services.GetDescriptors<TService>();

            foreach (var descriptor in descriptors)
            {
                services.Replace(descriptor.Decorate(decorator));
            }

            return services;
        }

        /// <summary>
        /// Decorates all registered services of type <typeparamref name="TService"/>
        /// using the <paramref name="decorator"/> function.
        /// </summary>
        /// <typeparam name="TService">The type of services to decorate.</typeparam>
        /// <param name="services">The services to add to.</param>
        /// <param name="decorator">The decorator function.</param>
        /// <exception cref="InvalidOperationException">If no service of <typeparamref name="TService"/> has been registered.</exception>
        /// <exception cref="ArgumentNullException">If either the <paramref name="services"/>
        /// or <paramref name="decorator"/> arguments are <c>null</c>.</exception>
        public static IServiceCollection Decorate<TService>(this IServiceCollection services, Func<TService, TService> decorator)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (decorator == null)
            {
                throw new ArgumentNullException(nameof(decorator));
            }

            var descriptors = services.GetDescriptors<TService>();

            foreach (var descriptor in descriptors)
            {
                services.Replace(descriptor.Decorate(decorator));
            }

            return services;
        }

        private static List<ServiceDescriptor> GetDescriptors<TService>(this IServiceCollection services)
        {
            var descriptors = new List<ServiceDescriptor>();

            foreach (var service in services)
            {
                if (service.ServiceType == typeof(TService))
                {
                    descriptors.Add(service);
                }
            }

            if (descriptors.Count == 0)
            {
                throw new InvalidOperationException($"Could not find any registered services for type '{typeof(TService).FullName}'.");
            }

            return descriptors;
        }

        private static ServiceDescriptor Decorate<TService>(this ServiceDescriptor descriptor, Func<TService, IServiceProvider, TService> decorator)
        {
            return descriptor.WithFactory(provider => decorator((TService)descriptor.GetInstance(provider), provider));
        }

        private static ServiceDescriptor Decorate<TService>(this ServiceDescriptor descriptor, Func<TService, TService> decorator)
        {
            return descriptor.WithFactory(provider => decorator((TService)descriptor.GetInstance(provider)));
        }

        private static ServiceDescriptor WithFactory(this ServiceDescriptor descriptor, Func<IServiceProvider, object> factory)
        {
            return ServiceDescriptor.Describe(descriptor.ServiceType, factory, descriptor.Lifetime);
        }

        private static object GetInstance(this ServiceDescriptor descriptor, IServiceProvider provider)
        {
            if (descriptor.ImplementationInstance != null)
            {
                return descriptor.ImplementationInstance;
            }

            if (descriptor.ImplementationType != null)
            {
                return provider.GetServiceOrCreateInstance(descriptor.ImplementationType);
            }

            return descriptor.ImplementationFactory(provider);
        }

        private static object GetServiceOrCreateInstance(this IServiceProvider provider, Type type)
        {
            return ActivatorUtilities.GetServiceOrCreateInstance(provider, type);
        }

        private static IServiceCollection Replace(this IServiceCollection collection, ServiceDescriptor descriptor)
        {
            var registeredServiceDescriptor = collection.FirstOrDefault(s => s.ServiceType == descriptor.ServiceType);

            if (registeredServiceDescriptor != null)
            {
                collection.Remove(registeredServiceDescriptor);
            }

            collection.Add(descriptor);

            return collection;
        }
    }
}
