using System;
using System.Collections.Generic;
using System.Linq;
using Scrutor;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class ServiceCollectionExtensions
    {
        /// <summary>
        /// Decorates all registered services of type <typeparamref name="TService"/>
        /// using the specified type <typeparamref name="TDecorator"/>.
        /// </summary>
        /// <param name="services">The services to add to.</param>
        /// <exception cref="InvalidOperationException">If no service of the type <typeparamref name="TService"/> has been registered.</exception>
        /// <exception cref="ArgumentNullException">If the <paramref name="services"/> argument is <c>null</c>.</exception>
        public static IServiceCollection Decorate<TService, TDecorator>(this IServiceCollection services)
            where TDecorator : TService
        {
            Preconditions.NotNull(services, nameof(services));

            return services.DecorateDescriptors(typeof(TService), x => x.Decorate(typeof(TDecorator)));
        }

        /// <summary>
        /// Decorates all registered services of the specified <paramref name="serviceType"/>
        /// using the specified <paramref name="decoratorType"/>.
        /// </summary>
        /// <param name="services">The services to add to.</param>
        /// <param name="serviceType">The type of services to decorate.</param>
        /// <param name="decoratorType">The type to decorate existing services with.</param>
        /// <exception cref="InvalidOperationException">If no service of the specified <paramref name="serviceType"/> has been registered.</exception>
        /// <exception cref="ArgumentNullException">If either the <paramref name="services"/>,
        /// <paramref name="serviceType"/> or <paramref name="decoratorType"/> arguments are <c>null</c>.</exception>
        public static IServiceCollection Decorate(this IServiceCollection services, Type serviceType, Type decoratorType)
        {
            Preconditions.NotNull(services, nameof(services));
            Preconditions.NotNull(serviceType, nameof(serviceType));
            Preconditions.NotNull(decoratorType, nameof(decoratorType));

            if (serviceType.IsOpenGeneric() && decoratorType.IsOpenGeneric())
            {
                return services.DecorateOpenGeneric(serviceType, decoratorType);
            }

            return services.DecorateDescriptors(serviceType, x => x.Decorate(decoratorType));
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
        public static IServiceCollection Decorate<TService>(this IServiceCollection services, Func<TService, IServiceProvider, TService> decorator)
        {
            Preconditions.NotNull(services, nameof(services));
            Preconditions.NotNull(decorator, nameof(decorator));

            return services.DecorateDescriptors(typeof(TService), x => x.Decorate(decorator));
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
            Preconditions.NotNull(services, nameof(services));
            Preconditions.NotNull(decorator, nameof(decorator));

            return services.DecorateDescriptors(typeof(TService), x => x.Decorate(decorator));
        }

        /// <summary>
        /// Decorates all registered services of the specified <paramref name="serviceType"/>
        /// using the <paramref name="decorator"/> function.
        /// </summary>
        /// <param name="services">The services to add to.</param>
        /// <param name="serviceType">The type of services to decorate.</param>
        /// <param name="decorator">The decorator function.</param>
        /// <exception cref="InvalidOperationException">If no service of the specified <paramref name="serviceType"/> has been registered.</exception>
        /// <exception cref="ArgumentNullException">If either the <paramref name="services"/>,
        /// <paramref name="serviceType"/> or <paramref name="decorator"/> arguments are <c>null</c>.</exception>
        public static IServiceCollection Decorate(this IServiceCollection services, Type serviceType, Func<object, IServiceProvider, object> decorator)
        {
            Preconditions.NotNull(services, nameof(services));
            Preconditions.NotNull(serviceType, nameof(serviceType));
            Preconditions.NotNull(decorator, nameof(decorator));

            return services.DecorateDescriptors(serviceType, x => x.Decorate(decorator));
        }

        /// <summary>
        /// Decorates all registered services of the specified <paramref name="serviceType"/>
        /// using the <paramref name="decorator"/> function.
        /// </summary>
        /// <param name="services">The services to add to.</param>
        /// <param name="serviceType">The type of services to decorate.</param>
        /// <param name="decorator">The decorator function.</param>
        /// <exception cref="InvalidOperationException">If no service of the specified <paramref name="serviceType"/> has been registered.</exception>
        /// <exception cref="ArgumentNullException">If either the <paramref name="services"/>,
        /// <paramref name="serviceType"/> or <paramref name="decorator"/> arguments are <c>null</c>.</exception>
        public static IServiceCollection Decorate(this IServiceCollection services, Type serviceType, Func<object, object> decorator)
        {
            Preconditions.NotNull(services, nameof(services));
            Preconditions.NotNull(serviceType, nameof(serviceType));
            Preconditions.NotNull(decorator, nameof(decorator));

            return services.DecorateDescriptors(serviceType, x => x.Decorate(decorator));
        }

        private static IServiceCollection DecorateOpenGeneric(this IServiceCollection services, Type serviceType, Type decoratorType)
        {
            if (services.TryDecorateOpenGeneric(serviceType, decoratorType))
            {
                return services;
            }

            throw new InvalidOperationException($"Could not find any registered services for type '{serviceType.FullName}'.");
        }

        private static bool TryDecorateOpenGeneric(this IServiceCollection services, Type serviceType, Type decoratorType)
        {
            bool TryDecorate(Type[] typeArguments)
            {
                var closedServiceType = serviceType.MakeGenericType(typeArguments);
                var closedDecoratorType = decoratorType.MakeGenericType(typeArguments);

                return services.TryDecorateDescriptors(closedServiceType, x => x.Decorate(closedDecoratorType));
            }

            var arguments = services
                .Where(descriptor => descriptor.ServiceType.IsAssignableTo(serviceType))
                .Select(descriptor => descriptor.ServiceType.GenericTypeArguments)
                .ToArray();

            return arguments.Aggregate(true, (result, args) => result && TryDecorate(args));
        }

        private static IServiceCollection DecorateDescriptors(this IServiceCollection services, Type serviceType, Func<ServiceDescriptor, ServiceDescriptor> decorator)
        {
            if (services.TryDecorateDescriptors(serviceType, decorator))
            {
                return services;
            }

            throw new InvalidOperationException($"Could not find any registered services for type '{serviceType.FullName}'.");
        }

        private static bool TryDecorateDescriptors(this IServiceCollection services, Type serviceType, Func<ServiceDescriptor, ServiceDescriptor> decorator)
        {
            if (!services.TryGetDescriptors(serviceType, out var descriptors))
            {
                return false;
            }

            foreach (var descriptor in descriptors)
            {
                var index = services.IndexOf(descriptor);

                // To avoid reordering descriptors, in case a specific order is expected.
                services.Insert(index, decorator(descriptor));

                services.Remove(descriptor);
            }

            return true;
        }

        private static bool TryGetDescriptors(this IServiceCollection services, Type serviceType, out ICollection<ServiceDescriptor> descriptors)
        {
            return (descriptors = services.Where(service => service.ServiceType == serviceType).ToArray()).Any();
        }

        private static ServiceDescriptor Decorate<TService>(this ServiceDescriptor descriptor, Func<TService, IServiceProvider, TService> decorator)
        {
            return descriptor.WithFactory(provider => decorator((TService) provider.GetInstance(descriptor), provider));
        }

        private static ServiceDescriptor Decorate<TService>(this ServiceDescriptor descriptor, Func<TService, TService> decorator)
        {
            return descriptor.WithFactory(provider => decorator((TService) provider.GetInstance(descriptor)));
        }

        private static ServiceDescriptor Decorate(this ServiceDescriptor descriptor, Type decoratorType)
        {
            return descriptor.WithFactory(provider => provider.CreateInstance(decoratorType, provider.GetInstance(descriptor)));
        }

        private static ServiceDescriptor WithFactory(this ServiceDescriptor descriptor, Func<IServiceProvider, object> factory)
        {
            return ServiceDescriptor.Describe(descriptor.ServiceType, factory, descriptor.Lifetime);
        }

        private static object GetInstance(this IServiceProvider provider, ServiceDescriptor descriptor)
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

        private static object CreateInstance(this IServiceProvider provider, Type type, params object[] arguments)
        {
            return ActivatorUtilities.CreateInstance(provider, type, arguments);
        }
    }
}
