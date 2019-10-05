using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Scrutor;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class ServiceCollectionExtensions
    {
        /// <summary>
        /// Overrides the dependencies for every service
        /// registered in the <paramref name="addServices"/> <see cref="Action"/>.
        /// </summary>
        /// <param name="services">The services to add to.</param>
        /// <param name="addServices">The action in which you configure the services.</param>
        /// <param name="dependenciesTypes">The types of the dependencies.</param>
        /// <exception cref="ArgumentNullException">
        /// If either the <paramref name="services"/>, <paramref name="addServices"/>
        /// or <paramref name="dependenciesTypes"/> arguments are <c>null</c>.
        /// </exception>
        public static IServiceCollection AddWithDependencies(
            this IServiceCollection services,
            Action addServices,
            params Type[] dependenciesTypes
        )
        {
            Preconditions.NotNull(services, nameof(services));
            Preconditions.NotNull(addServices, nameof(addServices));
            Preconditions.NotNull(dependenciesTypes, nameof(dependenciesTypes));

            services.AddWithDependencies(
                addServices,
                (serviceProvider, injectionContext) => dependenciesTypes.Select(serviceProvider.GetRequiredService).ToArray()
            );

            return services;
        }

        /// <summary>
        /// Overrides the dependencies for every service
        /// registered in the <paramref name="addServices"/> <see cref="Action"/>.
        /// </summary>
        /// <param name="services">The services to add to.</param>
        /// <param name="addServices">The action in which you configure the services.</param>
        /// <param name="dependenciesFactory">A factory that returns instances of the dependencies.</param>
        /// <exception cref="ArgumentNullException">
        /// If either the <paramref name="services"/>, <paramref name="addServices"/>
        /// or <paramref name="dependenciesFactory"/> arguments are <c>null</c>.
        /// </exception>
        public static IServiceCollection AddWithDependencies(
            this IServiceCollection services,
            Action addServices,
            Func<IServiceProvider, object[]> dependenciesFactory
        )
        {
            Preconditions.NotNull(services, nameof(services));
            Preconditions.NotNull(addServices, nameof(addServices));
            Preconditions.NotNull(dependenciesFactory, nameof(dependenciesFactory));

            services.AddWithDependencies(
                addServices,
                (serviceProvider, injectionContext) => dependenciesFactory(serviceProvider)
            );

            return services;
        }

        /// <summary>
        /// Overrides the dependencies for every service
        /// registered in the <paramref name="addServices"/> <see cref="Action"/>.
        /// </summary>
        /// <param name="services">The services to add to.</param>
        /// <param name="addServices">The action in which you configure the services.</param>
        /// <param name="dependenciesFactory">
        /// A factory with an <see cref="IInjectionContext"/>
        /// (Which provides the type of the event the dependencies are being injected)
        /// that returns instances of the dependencies.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// If either the <paramref name="services"/>, <paramref name="addServices"/>
        /// or <paramref name="dependenciesFactory"/> arguments are <c>null</c>.
        /// </exception>
        public static IServiceCollection AddWithDependencies(
            this IServiceCollection services,
            Action addServices,
            Func<IServiceProvider, IInjectionContext, object[]> dependenciesFactory
        )
        {
            Preconditions.NotNull(services, nameof(services));
            Preconditions.NotNull(addServices, nameof(addServices));
            Preconditions.NotNull(dependenciesFactory, nameof(dependenciesFactory));

            var addedServices = GetAddedServices(services, addServices);

            foreach (var addedService in addedServices)
                services.AddWithDependencies(addedService, dependenciesFactory);

            return services;
        }

        private static void AddWithDependencies(
            this IServiceCollection services,
            ServiceDescriptor serviceDescriptor,
            Func<IServiceProvider, IInjectionContext, object[]> dependenciesFactory
        )
        {
            var injectionContext = new InjectionContext(serviceDescriptor.ServiceType);

            if (serviceDescriptor.ImplementationType != null)
            {
                services.ReplaceServiceFactory(serviceDescriptor, serviceProvider =>
                {
                    var factoryServiceProvider = new InjectionContextAwareServiceProvider(
                        serviceProvider,
                        injectionContext,
                        dependenciesFactory
                    );

                    return factoryServiceProvider.GetService(serviceDescriptor.ImplementationType);
                });
            }
            else if (serviceDescriptor.ImplementationFactory != null)
            {
                services.ReplaceServiceFactory(serviceDescriptor, serviceProvider =>
                {
                    var factoryServiceProvider = new InjectionContextAwareServiceProvider(
                        serviceProvider,
                        injectionContext,
                        dependenciesFactory
                    );

                    var service = serviceDescriptor.ImplementationFactory(factoryServiceProvider);

                    return service;
                });
            }
        }

        private static IEnumerable<ServiceDescriptor> GetAddedServices(IServiceCollection services, Action addServices)
        {
            var originalServices = services.ToArray();
            addServices();
            var addedServices = services.Where(x => originalServices.All(y => y != x)).ToArray();

            return addedServices;
        }

        private static void ReplaceServiceFactory(
            this IServiceCollection services,
            ServiceDescriptor serviceDescriptor,
            Func<IServiceProvider, object> factory
        )
        {
            services.Replace(new ServiceDescriptor(serviceDescriptor.ServiceType, factory, serviceDescriptor.Lifetime));
        }
    }
}
