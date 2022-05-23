using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
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
        /// <exception cref="MissingTypeRegistrationException">If no service of the type <typeparamref name="TService"/> has been registered.</exception>
        /// <exception cref="ArgumentNullException">If the <paramref name="services"/> argument is <c>null</c>.</exception>
        public static IServiceCollection Decorate<TService, TDecorator>(this IServiceCollection services)
            where TDecorator : TService
        {
            Preconditions.NotNull(services, nameof(services));

            if (typeof(TDecorator).IsInterface)
            {
                return DecorateUsingInterface<TService, TDecorator>(services);
            }
            else
            {
                return services.DecorateDescriptors(typeof(TService), x => x.Decorate(typeof(TDecorator)));
            }
        }

        private static IServiceCollection DecorateUsingInterface<TService, TDecorator>(IServiceCollection services) where TDecorator : TService
        {
            if (typeof(TDecorator).IsGenericType)
            {
                var decoratorDescriptor = services.Where(service => HasSameTypeDefinition(service.ServiceType, typeof(TDecorator))).FirstOrDefault();
                if (decoratorDescriptor == null)
                    throw new MissingTypeRegistrationException(typeof(TDecorator).IsGenericType ? typeof(TDecorator).GetGenericTypeDefinition() : typeof(TDecorator));

                return services.DecorateDescriptors(typeof(TService), x => x.Decorate(decoratorDescriptor.ImplementationType.MakeGenericType(typeof(TDecorator).GetGenericArguments().First())));
            }
            else
            {
                var decoratorDescriptor = services.Where(service => service.ServiceType == typeof(TDecorator)).FirstOrDefault();
                if (decoratorDescriptor == null)
                    throw new MissingTypeRegistrationException(typeof(TDecorator).IsGenericType ? typeof(TDecorator).GetGenericTypeDefinition() : typeof(TDecorator));

                return services.DecorateDescriptors(typeof(TService), x => x.Decorate(decoratorDescriptor.ImplementationType));
            }

        }

        /// <summary>
        /// Decorates all registered services of type <typeparamref name="TService"/>
        /// using the specified type <typeparamref name="TDecorator"/>.
        /// </summary>
        /// <param name="services">The services to add to.</param>
        /// <exception cref="ArgumentNullException">If the <paramref name="services"/> argument is <c>null</c>.</exception>
        public static bool TryDecorate<TService, TDecorator>(this IServiceCollection services)
            where TDecorator : TService
        {
            Preconditions.NotNull(services, nameof(services));

            return services.TryDecorateDescriptors(typeof(TService), out _, x => x.Decorate(typeof(TDecorator)));
        }

        /// <summary>
        /// Decorates all registered services of the specified <paramref name="serviceType"/>
        /// using the specified <paramref name="decoratorType"/>.
        /// </summary>
        /// <param name="services">The services to add to.</param>
        /// <param name="serviceType">The type of services to decorate.</param>
        /// <param name="decoratorType">The type to decorate existing services with.</param>
        /// <exception cref="MissingTypeRegistrationException">If no service of the specified <paramref name="serviceType"/> has been registered.</exception>
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
        /// Decorates all registered services of the specified <paramref name="serviceType"/>
        /// using the specified <paramref name="decoratorType"/>.
        /// </summary>
        /// <param name="services">The services to add to.</param>
        /// <param name="serviceType">The type of services to decorate.</param>
        /// <param name="decoratorType">The type to decorate existing services with.</param>
        /// <exception cref="ArgumentNullException">If either the <paramref name="services"/>,
        /// <paramref name="serviceType"/> or <paramref name="decoratorType"/> arguments are <c>null</c>.</exception>
        public static bool TryDecorate(this IServiceCollection services, Type serviceType, Type decoratorType)
        {
            Preconditions.NotNull(services, nameof(services));
            Preconditions.NotNull(serviceType, nameof(serviceType));
            Preconditions.NotNull(decoratorType, nameof(decoratorType));

            if (serviceType.IsOpenGeneric() && decoratorType.IsOpenGeneric())
            {
                return services.TryDecorateOpenGeneric(serviceType, decoratorType, out _);
            }

            return services.TryDecorateDescriptors(serviceType, out _, x => x.Decorate(decoratorType));
        }

        /// <summary>
        /// Decorates all registered services of type <typeparamref name="TService"/>
        /// using the <paramref name="decorator"/> function.
        /// </summary>
        /// <typeparam name="TService">The type of services to decorate.</typeparam>
        /// <param name="services">The services to add to.</param>
        /// <param name="decorator">The decorator function.</param>
        /// <exception cref="MissingTypeRegistrationException">If no service of <typeparamref name="TService"/> has been registered.</exception>
        /// <exception cref="ArgumentNullException">If either the <paramref name="services"/>
        /// or <paramref name="decorator"/> arguments are <c>null</c>.</exception>
        public static IServiceCollection Decorate<TService>(this IServiceCollection services, Func<TService, IServiceProvider, TService> decorator) where TService : notnull
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
        /// <exception cref="ArgumentNullException">If either the <paramref name="services"/>
        /// or <paramref name="decorator"/> arguments are <c>null</c>.</exception>
        public static bool TryDecorate<TService>(this IServiceCollection services, Func<TService, IServiceProvider, TService> decorator) where TService : notnull
        {
            Preconditions.NotNull(services, nameof(services));
            Preconditions.NotNull(decorator, nameof(decorator));

            return services.TryDecorateDescriptors(typeof(TService), out _, x => x.Decorate(decorator));
        }

        /// <summary>
        /// Decorates all registered services of type <typeparamref name="TService"/>
        /// using the <paramref name="decorator"/> function.
        /// </summary>
        /// <typeparam name="TService">The type of services to decorate.</typeparam>
        /// <param name="services">The services to add to.</param>
        /// <param name="decorator">The decorator function.</param>
        /// <exception cref="MissingTypeRegistrationException">If no service of <typeparamref name="TService"/> has been registered.</exception>
        /// <exception cref="ArgumentNullException">If either the <paramref name="services"/>
        /// or <paramref name="decorator"/> arguments are <c>null</c>.</exception>
        public static IServiceCollection Decorate<TService>(this IServiceCollection services, Func<TService, TService> decorator) where TService : notnull
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
        /// <exception cref="ArgumentNullException">If either the <paramref name="services"/>
        /// or <paramref name="decorator"/> arguments are <c>null</c>.</exception>
        public static bool TryDecorate<TService>(this IServiceCollection services, Func<TService, TService> decorator) where TService : notnull
        {
            Preconditions.NotNull(services, nameof(services));
            Preconditions.NotNull(decorator, nameof(decorator));

            return services.TryDecorateDescriptors(typeof(TService), out _, x => x.Decorate(decorator));
        }

        /// <summary>
        /// Decorates all registered services of the specified <paramref name="serviceType"/>
        /// using the <paramref name="decorator"/> function.
        /// </summary>
        /// <param name="services">The services to add to.</param>
        /// <param name="serviceType">The type of services to decorate.</param>
        /// <param name="decorator">The decorator function.</param>
        /// <exception cref="MissingTypeRegistrationException">If no service of the specified <paramref name="serviceType"/> has been registered.</exception>
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
        /// <exception cref="ArgumentNullException">If either the <paramref name="services"/>,
        /// <paramref name="serviceType"/> or <paramref name="decorator"/> arguments are <c>null</c>.</exception>
        public static bool TryDecorate(this IServiceCollection services, Type serviceType, Func<object, IServiceProvider, object> decorator)
        {
            Preconditions.NotNull(services, nameof(services));
            Preconditions.NotNull(serviceType, nameof(serviceType));
            Preconditions.NotNull(decorator, nameof(decorator));

            return services.TryDecorateDescriptors(serviceType, out _, x => x.Decorate(decorator));
        }

        /// <summary>
        /// Decorates all registered services of the specified <paramref name="serviceType"/>
        /// using the <paramref name="decorator"/> function.
        /// </summary>
        /// <param name="services">The services to add to.</param>
        /// <param name="serviceType">The type of services to decorate.</param>
        /// <param name="decorator">The decorator function.</param>
        /// <exception cref="MissingTypeRegistrationException">If no service of the specified <paramref name="serviceType"/> has been registered.</exception>
        /// <exception cref="ArgumentNullException">If either the <paramref name="services"/>,
        /// <paramref name="serviceType"/> or <paramref name="decorator"/> arguments are <c>null</c>.</exception>
        public static IServiceCollection Decorate(this IServiceCollection services, Type serviceType, Func<object, object> decorator)
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
        /// <exception cref="ArgumentNullException">If either the <paramref name="services"/>,
        /// <paramref name="serviceType"/> or <paramref name="decorator"/> arguments are <c>null</c>.</exception>
        public static bool TryDecorate(this IServiceCollection services, Type serviceType, Func<object, object> decorator)
        {
            Preconditions.NotNull(services, nameof(services));
            Preconditions.NotNull(serviceType, nameof(serviceType));
            Preconditions.NotNull(decorator, nameof(decorator));

            return services.TryDecorateDescriptors(serviceType, out _, x => x.Decorate(decorator));
        }

        private static IServiceCollection DecorateOpenGeneric(this IServiceCollection services, Type serviceType, Type decoratorType)
        {
            if (services.TryDecorateOpenGeneric(serviceType, decoratorType, out var error))
            {
                return services;
            }

            throw error;
        }

        private static bool HasSameTypeDefinition(Type t1, Type t2)
        {
            return t1.IsGenericType && t2.IsGenericType && t1.GetGenericTypeDefinition() == t2.GetGenericTypeDefinition();
        }

        private static bool TryDecorateOpenGeneric(this IServiceCollection services, Type serviceType, Type decoratorType, [NotNullWhen(false)] out Exception? error)
        {
            var closedGenericServiceTypes = services
                .Where(x => !x.ServiceType.IsGenericTypeDefinition)
                .Where(x => HasSameTypeDefinition(x.ServiceType, serviceType))
                .Select(x => x.ServiceType)
                .Distinct()
                .ToList();

            if (closedGenericServiceTypes.Count == 0)
            {
                error = new MissingTypeRegistrationException(serviceType);
                return false;
            }

            foreach (var closedGenericServiceType in closedGenericServiceTypes)
            {
                var arguments = closedGenericServiceType.GetGenericArguments();

                var closedServiceType = serviceType.MakeGenericType(arguments);
                try
                {
                    var closedDecoratorType = decoratorType.MakeGenericType(arguments);
                    if (!services.TryDecorateDescriptors(closedServiceType, out error, x => x.Decorate(closedDecoratorType)))
                    {
                        return false;
                    }
                }
                catch (ArgumentException) { }
            }

            error = default;
            return true;
        }

        private static IServiceCollection DecorateDescriptors(this IServiceCollection services, Type serviceType, Func<ServiceDescriptor, ServiceDescriptor> decorator)
        {
            if (services.TryDecorateDescriptors(serviceType, out var error, decorator))
            {
                return services;
            }

            throw error;
        }

        private static bool TryDecorateDescriptors(this IServiceCollection services, Type serviceType, [NotNullWhen(false)] out Exception? error, Func<ServiceDescriptor, ServiceDescriptor> decorator)
        {
            if (!services.TryGetDescriptors(serviceType, out var descriptors))
            {
                error = new MissingTypeRegistrationException(serviceType);
                return false;
            }

            foreach (var (position, value) in descriptors)
            {
                // To avoid reordering descriptors, in case a specific order is expected.
                services[position] = decorator(value);
            }

            error = default;
            return true;
        }

        private static bool TryGetDescriptors(this IServiceCollection services, Type serviceType, out IReadOnlyList<(int Position, ServiceDescriptor Value)> descriptors)
        {
            descriptors = EnumerateDescriptors().ToArray();
            return descriptors.Count != 0;

            IEnumerable<(int Position, ServiceDescriptor Value)> EnumerateDescriptors()
            {
                for (var i = 0; i < services.Count; ++i)
                {
                    var descriptor = services[i];
                    if (descriptor.ServiceType == serviceType)
                    {
                        yield return (i, descriptor);
                    }
                }
            }
        }

        private static ServiceDescriptor Decorate<TService>(this ServiceDescriptor descriptor, Func<TService, IServiceProvider, TService> decorator) where TService : notnull
        {
            return descriptor.WithFactory(provider => decorator((TService)provider.GetInstance(descriptor), provider));
        }

        private static ServiceDescriptor Decorate<TService>(this ServiceDescriptor descriptor, Func<TService, TService> decorator) where TService : notnull
        {
            return descriptor.WithFactory(provider => decorator((TService)provider.GetInstance(descriptor)));
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

            // Not suppose to be abstract. 
            var implementationType = descriptor.ImplementationType;
            if (implementationType != null)
            {
                if (implementationType == descriptor.ServiceType)
                {
                    // Since implementationType is equal to ServiceType we need explicitly create an implementation type through reflections in order to avoid infinite recursion.
                    // Should not cause issue with singletons, since singleton will be a decorator and after this fact we can don't care about lifecycle of decorable service (for sure, if IDisposable of decorator disposes underlying type:))
                    return provider.CreateInstance(implementationType);
                }

                return provider.GetServiceOrCreateInstance(implementationType);
            }

            if (descriptor.ImplementationFactory != null)
            {
                return descriptor.ImplementationFactory(provider);
            }

            throw new InvalidOperationException($"No implementation factory or instance or type found for {descriptor.ServiceType}.");
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

