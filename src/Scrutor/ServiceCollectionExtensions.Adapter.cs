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
        /// Adapts all registered services of type <typeparam name="TAdaptee"/>
        /// using the specified type <typeparam name="TAdapter"/>.
        /// returning as Adapted service of type <typeparam name="TTarget"/>
        /// </summary>
        /// <param name="services">The services to add to.</param>
        /// <exception cref="MissingTypeRegistrationException">If no service of the type <typeparamref name="TAdaptee"/> has been registered.</exception>
        /// <exception cref="ArgumentNullException">If the <paramref name="services"/> argument is <c>null</c>.</exception>
        public static IServiceCollection Adapt<TAdaptee, TAdapter, TTarget>(this IServiceCollection services)
            where TAdapter : TTarget
        {
            Preconditions.NotNull(services, nameof(services));

            return services.AdaptDescriptors(typeof(TAdaptee), typeof(TTarget), (adaptee,target) => adaptee.Adapt(target, typeof(TAdapter)));
        }

        /// <summary>
        /// Adapts all registered services of type <typeparamref name="TAdaptee"/>
        /// using the specified type <typeparamref name="TAdapter"/>.
        /// </summary>
        /// <param name="services">The services to add to.</param>
        /// <exception cref="ArgumentNullException">If the <paramref name="services"/> argument is <c>null</c>.</exception>
        public static bool TryAdapt<TAdaptee, TAdapter, TTarget>(this IServiceCollection services)
            where TAdapter : TTarget
        { 
            Preconditions.NotNull(services, nameof(services));

            return services.TryAdaptDescriptors(typeof(TAdaptee), typeof(TTarget), (adaptee,target) => adaptee.Adapt(target, typeof(TAdapter)));
        }

        /// <summary>
        /// Adapts all registered services of the specified <paramref name="serviceType"/>
        /// using the specified <paramref name="adaptorType"/>.
        /// </summary>
        /// <param name="services">The services to add to.</param>
        /// <param name="serviceType">The type of services to Adapt.</param>
        /// <param name="adaptorType">The type to Adapt existing services with.</param>
        /// <param name="targetType">The resulting service type made available</param>
        /// <exception cref="MissingTypeRegistrationException">If no service of the specified <paramref name="serviceType"/> has been registered.</exception>
        /// <exception cref="ArgumentNullException">If either the <paramref name="services"/>,
        /// <paramref name="serviceType"/> or <paramref name="adaptorType"/> arguments are <c>null</c>.</exception>
        public static IServiceCollection Adapt(this IServiceCollection services, Type serviceType, Type adaptorType, Type targetType)
        {
            Preconditions.NotNull(services, nameof(services));
            Preconditions.NotNull(serviceType, nameof(serviceType));
            Preconditions.NotNull(adaptorType, nameof(adaptorType));

            if (serviceType.IsOpenGeneric() && adaptorType.IsOpenGeneric())
            {
                return services.AdaptOpenGeneric(serviceType, adaptorType, targetType);
            }

            return services.AdaptDescriptors(serviceType, targetType, (adaptee,target) => adaptee.Adapt(target, adaptorType));
        }

        /// <summary>
        /// Adapts all registered services of the specified <paramref name="serviceType"/>
        /// using the specified <paramref name="adaptorType"/>.
        /// </summary>
        /// <param name="services">The services to add to.</param>
        /// <param name="serviceType">The type of services to Adapt.</param>
        /// <param name="adaptorType">The type to Adapt existing services with.</param>
        /// <param name="targetType"></param>
        /// <exception cref="ArgumentNullException">If either the <paramref name="services"/>,
        /// <paramref name="serviceType"/> or <paramref name="adaptorType"/> arguments are <c>null</c>.</exception>
        public static bool TryAdapt(this IServiceCollection services, Type serviceType, Type adaptorType, Type targetType)
        {
            Preconditions.NotNull(services, nameof(services));
            Preconditions.NotNull(serviceType, nameof(serviceType));
            Preconditions.NotNull(adaptorType, nameof(adaptorType));

            if (serviceType.IsOpenGeneric() && adaptorType.IsOpenGeneric())
            {
                return services.TryAdaptOpenGeneric(serviceType, adaptorType, targetType);
            }

            return services.TryAdaptDescriptors(serviceType, targetType, (adaptee,target) => adaptee.Adapt(target, adaptorType));
        }

        /// <summary>
        /// Adapts all registered services of type <typeparamref name="TService"/>
        /// using the <paramref name="decorator"/> function.
        /// </summary>
        /// <typeparam name="TService">The type of services to Adapt.</typeparam>
        /// <typeparam name="TTarget"></typeparam>
        /// <param name="services">The services to add to.</param>
        /// <param name="decorator">The decorator function.</param>
        /// <exception cref="MissingTypeRegistrationException">If no service of <typeparamref name="TService"/> has been registered.</exception>
        /// <exception cref="ArgumentNullException">If either the <paramref name="services"/>
        /// or <paramref name="decorator"/> arguments are <c>null</c>.</exception>
        public static IServiceCollection Adapt<TService,TTarget>(this IServiceCollection services, Func<TService, IServiceProvider, TTarget> decorator)
        {
            Preconditions.NotNull(services, nameof(services));
            Preconditions.NotNull(decorator, nameof(decorator));

            return services.AdaptDescriptors(typeof(TService), typeof(TTarget), (adaptee,target) => adaptee.Adapt(target, typeof(TTarget)));
        }

        /// <summary>
        /// Adapts all registered services of type <typeparamref name="TService"/>
        /// using the <paramref name="decorator"/> function.
        /// </summary>
        /// <typeparam name="TService">The type of services to Adapt.</typeparam>
        /// <typeparam name="TTarget"></typeparam>
        /// <param name="services">The services to add to.</param>
        /// <param name="decorator">The decorator function.</param>
        /// <exception cref="ArgumentNullException">If either the <paramref name="services"/>
        /// or <paramref name="decorator"/> arguments are <c>null</c>.</exception>
        public static bool TryAdapt<TService,TTarget>(this IServiceCollection services, Func<TService, IServiceProvider, TTarget> decorator)
        {
            Preconditions.NotNull(services, nameof(services));
            Preconditions.NotNull(decorator, nameof(decorator));

            return services.TryAdaptDescriptors(typeof(TService), typeof(TTarget), (adaptee,target) => adaptee.Adapt(target, typeof(TTarget)));
        }

        /// <summary>
        /// Adapts all registered services of type <typeparamref name="TService"/>
        /// using the <paramref name="decorator"/> function.
        /// </summary>
        /// <typeparam name="TService">The type of services to Adapt.</typeparam>
        /// <typeparam name="TTarget"></typeparam>
        /// <param name="services">The services to add to.</param>
        /// <param name="decorator">The decorator function.</param>
        /// <exception cref="MissingTypeRegistrationException">If no service of <typeparamref name="TService"/> has been registered.</exception>
        /// <exception cref="ArgumentNullException">If either the <paramref name="services"/>
        /// or <paramref name="decorator"/> arguments are <c>null</c>.</exception>
        public static IServiceCollection Adapt<TService, TTarget>(this IServiceCollection services, Func<TService, TTarget> decorator)
        {
            Preconditions.NotNull(services, nameof(services));
            Preconditions.NotNull(decorator, nameof(decorator));

            return services.AdaptDescriptors(typeof(TService), typeof(TTarget), (adaptee,target) => adaptee.Adapt(target, typeof(TTarget)));
        }

        /// <summary>
        /// Adapts all registered services of type <typeparamref name="TService"/>
        /// using the <paramref name="decorator"/> function.
        /// </summary>
        /// <typeparam name="TService">The type of services to Adapt.</typeparam>
        /// <typeparam name="TTarget"></typeparam>
        /// <param name="services">The services to add to.</param>
        /// <param name="decorator">The decorator function.</param>
        /// <exception cref="ArgumentNullException">If either the <paramref name="services"/>
        /// or <paramref name="decorator"/> arguments are <c>null</c>.</exception>
        public static bool TryAdapt<TService, TTarget>(this IServiceCollection services, Func<TService, TService> decorator)
        {
            Preconditions.NotNull(services, nameof(services));
            Preconditions.NotNull(decorator, nameof(decorator));

            return services.TryAdaptDescriptors(typeof(TService), typeof(TTarget), (adaptee,target) => adaptee.Adapt(target, typeof(TTarget)));
        }

        /// <summary>
        /// Adapts all registered services of the specified <paramref name="serviceType"/>
        /// using the <paramref name="decorator"/> function.
        /// </summary>
        /// <param name="services">The services to add to.</param>
        /// <param name="serviceType">The type of services to Adapt.</param>
        /// <param name="targetType"></param>
        /// <param name="decorator">The decorator function.</param>
        /// <exception cref="MissingTypeRegistrationException">If no service of the specified <paramref name="serviceType"/> has been registered.</exception>
        /// <exception cref="ArgumentNullException">If either the <paramref name="services"/>,
        /// <paramref name="serviceType"/> or <paramref name="decorator"/> arguments are <c>null</c>.</exception>
        public static IServiceCollection Adapt(this IServiceCollection services, Type serviceType, Type targetType, Func<object, IServiceProvider, object> decorator)
        {
            Preconditions.NotNull(services, nameof(services));
            Preconditions.NotNull(serviceType, nameof(serviceType));
            Preconditions.NotNull(decorator, nameof(decorator));

            return services.AdaptDescriptors(serviceType, targetType, (adaptee,target) => adaptee.Adapt(target, targetType));
        }

        /// <summary>
        /// Adapts all registered services of the specified <paramref name="serviceType"/>
        /// using the <paramref name="decorator"/> function.
        /// </summary>
        /// <param name="services">The services to add to.</param>
        /// <param name="serviceType">The type of services to Adapt.</param>
        /// <param name="targetType"></param>
        /// <param name="decorator">The decorator function.</param>
        /// <exception cref="ArgumentNullException">If either the <paramref name="services"/>,
        /// <paramref name="serviceType"/> or <paramref name="decorator"/> arguments are <c>null</c>.</exception>
        public static bool TryAdapt(this IServiceCollection services, Type serviceType, Type targetType, Func<object, IServiceProvider, object> decorator)
        {
            Preconditions.NotNull(services, nameof(services));
            Preconditions.NotNull(serviceType, nameof(serviceType));
            Preconditions.NotNull(decorator, nameof(decorator));

            return services.TryAdaptDescriptors(serviceType, targetType, (adaptee,target) => adaptee.Adapt(target, targetType));
        }

        /// <summary>
        /// Adapts all registered services of the specified <paramref name="serviceType"/>
        /// using the <paramref name="decorator"/> function.
        /// </summary>
        /// <param name="services">The services to add to.</param>
        /// <param name="serviceType">The type of services to Adapt.</param>
        /// <param name="targetType"></param>
        /// <param name="decorator">The decorator function.</param>
        /// <exception cref="MissingTypeRegistrationException">If no service of the specified <paramref name="serviceType"/> has been registered.</exception>
        /// <exception cref="ArgumentNullException">If either the <paramref name="services"/>,
        /// <paramref name="serviceType"/> or <paramref name="decorator"/> arguments are <c>null</c>.</exception>
        public static IServiceCollection Adapt(this IServiceCollection services, Type serviceType, Type targetType, Func<object, object> decorator)
        {
            Preconditions.NotNull(services, nameof(services));
            Preconditions.NotNull(serviceType, nameof(serviceType));
            Preconditions.NotNull(decorator, nameof(decorator));

            return services.AdaptDescriptors(serviceType, targetType, (adaptee,target) => adaptee.Adapt(target, targetType));
        }

        /// <summary>
        /// Adapts all registered services of the specified <paramref name="serviceType"/>
        /// using the <paramref name="decorator"/> function.
        /// </summary>
        /// <param name="services">The services to add to.</param>
        /// <param name="serviceType">The type of services to Adapt.</param>
        /// <param name="targetType"></param>
        /// <param name="decorator">The decorator function.</param>
        /// <exception cref="ArgumentNullException">If either the <paramref name="services"/>,
        /// <paramref name="serviceType"/> or <paramref name="decorator"/> arguments are <c>null</c>.</exception>
        public static bool TryAdapt(this IServiceCollection services, Type serviceType, Type targetType, Func<object, object> decorator)
        {
            Preconditions.NotNull(services, nameof(services));
            Preconditions.NotNull(serviceType, nameof(serviceType));
            Preconditions.NotNull(decorator, nameof(decorator));

            return services.TryAdaptDescriptors(serviceType, targetType, (adaptee,target) => adaptee.Adapt(target, targetType));
        }

        private static IServiceCollection AdaptOpenGeneric(this IServiceCollection services, Type serviceType, Type adaptorType, Type targetType)
        {
            if (services.TryAdaptOpenGeneric(serviceType, adaptorType, targetType))
            {
                return services;
            }

            throw new MissingTypeRegistrationException(serviceType);
        }

        private static bool TryAdaptOpenGeneric(this IServiceCollection services, Type serviceType, Type adaptorType, Type targetType)
        {
            bool TryAdapt(Type[] typeArguments)
            {
                var closedServiceType = serviceType.MakeGenericType(typeArguments);
                var closedAdaptorType = adaptorType.MakeGenericType(typeArguments);
                var closedtargetServiceType = targetType.MakeGenericType(typeArguments);

                return services.TryAdaptDescriptors(closedServiceType, closedtargetServiceType, (adaptee,target) => adaptee.Adapt(target, closedtargetServiceType));
            }

            var arguments = services
                .Where(descriptor => IsSameGenericType(descriptor.ServiceType, serviceType))
                .Select(descriptor => descriptor.ServiceType.GenericTypeArguments)
                .ToArray();

            if (arguments.Length == 0)
            {
                return false;
            }

            return arguments.Aggregate(true, (result, args) => result && TryAdapt(args));
        }

        private static IServiceCollection AdaptDescriptors(this IServiceCollection services, Type adapteeServiceType, Type targetServiceType, Func<ServiceDescriptor, ServiceDescriptor, ServiceDescriptor> decorator)
        {
            if (services.TryAdaptDescriptors(adapteeServiceType, targetServiceType, decorator))
            {
                return services;
            }

            throw new MissingTypeRegistrationException(adapteeServiceType);
        }

        private static bool TryAdaptDescriptors(this IServiceCollection services, Type adapteeServiceType, Type targetServiceType, Func<ServiceDescriptor, ServiceDescriptor,  ServiceDescriptor> decorator)
        {
            if (!services.TryGetDescriptors(adapteeServiceType, out var adapteeServiceDescriptors))
            {
                return false;
            }

            if (!services.TryGetDescriptors(targetServiceType, out var targetDescriptors))
            {
                return false;
            }

            if (targetDescriptors.Count > 1)
            {
                //Currently only handle 1 Adapter, in reality only the first would be applied as the descriptors
                //would be altered after the first anyway.
                return false;
            }

            var targetDescriptor = targetDescriptors.First();

            foreach (var adapteeDescriptor in adapteeServiceDescriptors)
            {
                var index = services.IndexOf(adapteeDescriptor);

                // To avoid reordering descriptors, in case a specific order is expected.
                services[index] = decorator(adapteeDescriptor, targetDescriptor);
            }

            return true;
        }

        private static ServiceDescriptor Adapt<TService>(this ServiceDescriptor descriptor, Func<TService, IServiceProvider, TService> decorator)
        {
            return descriptor.WithFactory(provider => decorator((TService) provider.GetInstance(descriptor), provider));
        }

        private static ServiceDescriptor Adapt<TService>(this ServiceDescriptor descriptor, Func<TService, TService> decorator)
        {
            return descriptor.WithFactory(provider => decorator((TService) provider.GetInstance(descriptor)));
        }

        private static ServiceDescriptor Adapt(this ServiceDescriptor descriptor, ServiceDescriptor targetServiceDescriptor, Type adaptorType)
        {
            return targetServiceDescriptor.WithFactory(provider => provider.CreateInstance(adaptorType, provider.GetInstance(descriptor)));
        }
        
    }
}
