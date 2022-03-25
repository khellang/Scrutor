using Scrutor;
using Scrutor.Decoration;
using System;

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

            var decoration = DecorationFactory.Create<TService>(typeof(TDecorator), null);
            return decoration.Decorate(services);
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

            var decoration = DecorationFactory.Create<TService>(typeof(TDecorator), null);
            return decoration.TryDecorate(services);
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

            var decoration = DecorationFactory.Create(serviceType, decoratorType, null);
            return decoration.Decorate(services);
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

            var decoration = DecorationFactory.Create(serviceType, decoratorType, null);
            return decoration.TryDecorate(services);
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

            var decoration = DecorationFactory.Create<TService>(null, (decorated, serviceProvider) => decorator((TService)decorated, serviceProvider));
            return decoration.Decorate(services);
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

            var decoration = DecorationFactory.Create<TService>(null, (decorated, serviceProvider) => decorator((TService)decorated, serviceProvider));
            return decoration.TryDecorate(services);
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

            var decoration = DecorationFactory.Create<TService>(null, (decorated, serviceProvider) => decorator((TService)decorated));
            return decoration.Decorate(services);
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

            var decoration = DecorationFactory.Create<TService>(null, (decorated, serviceProvider) => decorator((TService)decorated));
            return decoration.TryDecorate(services);
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

            var decoration = DecorationFactory.Create(serviceType, null, decorator);
            return decoration.Decorate(services);
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

            var decoration = DecorationFactory.Create(serviceType, null, decorator);
            return decoration.TryDecorate(services);
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

            var decoration = DecorationFactory.Create(serviceType, null, (decorated, serviceProvider) => decorator(decorated));
            return decoration.Decorate(services);
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

            var decoration = DecorationFactory.Create(serviceType, null, (decorated, serviceProvider) => decorator(decorated));
            return decoration.TryDecorate(services);
        }
    }
}
