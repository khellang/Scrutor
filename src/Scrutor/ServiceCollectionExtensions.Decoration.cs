using Scrutor;
using System;
using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;
using System.Collections.Generic;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

[PublicAPI]
public static partial class ServiceCollectionExtensions
{
    private const string DecoratedServiceKeySuffix = "+Decorated";

    /// <summary>
    /// Decorates all registered services of type <typeparamref name="TService"/>
    /// using the specified type <typeparamref name="TDecorator"/>.
    /// </summary>
    /// <param name="services">The services to add to.</param>
    /// <exception cref="DecorationException">If no service of the type <typeparamref name="TService"/> has been registered.</exception>
    /// <exception cref="ArgumentNullException">If the <paramref name="services"/> argument is <c>null</c>.</exception>
    public static IServiceCollection Decorate<TService, TDecorator>(this IServiceCollection services)
        where TDecorator : TService
    {
        return services.Decorate<TService, TDecorator>(out _);
    }

    /// <summary>
    /// Decorates all registered services of type <typeparamref name="TService"/>
    /// using the specified type <typeparamref name="TDecorator"/>.
    /// </summary>
    /// <param name="services">The services to add to.</param>
    /// <param name="serviceKey">The <see cref="ServiceDescriptor.ServiceKey"/> of the service.</param>
    /// <exception cref="DecorationException">If no service of the type <typeparamref name="TService"/> has been registered.</exception>
    /// <exception cref="ArgumentNullException">If the <paramref name="services"/> argument is <c>null</c>.</exception>
    public static IServiceCollection DecorateKeyed<TService, TDecorator>(this IServiceCollection services, string serviceKey)
        where TDecorator : TService
    {
        Preconditions.NotNull(services, nameof(services));
        Preconditions.NotNull(serviceKey, nameof(serviceKey));

        return services.Decorate(DecorationStrategy.WithType(typeof(TService), serviceKey, typeof(TDecorator)));
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
        return services.TryDecorate<TService, TDecorator>(out _);
    }

    /// <summary>
    /// Decorates all registered services of the specified <paramref name="serviceType"/>
    /// using the specified <paramref name="decoratorType"/>.
    /// </summary>
    /// <param name="services">The services to add to.</param>
    /// <param name="serviceType">The type of services to decorate.</param>
    /// <param name="decoratorType">The type to decorate existing services with.</param>
    /// <exception cref="DecorationException">If no service of the specified <paramref name="serviceType"/> has been registered.</exception>
    /// <exception cref="ArgumentNullException">If either the <paramref name="services"/>,
    /// <paramref name="serviceType"/> or <paramref name="decoratorType"/> arguments are <c>null</c>.</exception>
    public static IServiceCollection Decorate(this IServiceCollection services, Type serviceType, Type decoratorType)
    {
        return services.Decorate(serviceType, decoratorType, out _);
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
        return services.TryDecorate(serviceType, decoratorType, out _);
    }

    /// <summary>
    /// Decorates all registered services of type <typeparamref name="TService"/>
    /// using the <paramref name="decorator"/> function.
    /// </summary>
    /// <typeparam name="TService">The type of services to decorate.</typeparam>
    /// <param name="services">The services to add to.</param>
    /// <param name="decorator">The decorator function.</param>
    /// <exception cref="DecorationException">If no service of <typeparamref name="TService"/> has been registered.</exception>
    /// <exception cref="ArgumentNullException">If either the <paramref name="services"/>
    /// or <paramref name="decorator"/> arguments are <c>null</c>.</exception>
    public static IServiceCollection Decorate<TService>(this IServiceCollection services, Func<TService, TService> decorator) where TService : notnull
    {
        return services.Decorate<TService>(decorator, out _);
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
        return services.TryDecorate<TService>(decorator, out _);
    }

    /// <summary>
    /// Decorates all registered services of type <typeparamref name="TService"/>
    /// using the <paramref name="decorator"/> function.
    /// </summary>
    /// <typeparam name="TService">The type of services to decorate.</typeparam>
    /// <param name="services">The services to add to.</param>
    /// <param name="decorator">The decorator function.</param>
    /// <exception cref="DecorationException">If no service of <typeparamref name="TService"/> has been registered.</exception>
    /// <exception cref="ArgumentNullException">If either the <paramref name="services"/>
    /// or <paramref name="decorator"/> arguments are <c>null</c>.</exception>
    public static IServiceCollection Decorate<TService>(this IServiceCollection services, Func<TService, IServiceProvider, TService> decorator) where TService : notnull
    {
        return services.Decorate<TService>(decorator, out _);
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
        return services.TryDecorate<TService>(decorator, out _);
    }

    /// <summary>
    /// Decorates all registered services of the specified <paramref name="serviceType"/>
    /// using the <paramref name="decorator"/> function.
    /// </summary>
    /// <param name="services">The services to add to.</param>
    /// <param name="serviceType">The type of services to decorate.</param>
    /// <param name="decorator">The decorator function.</param>
    /// <exception cref="DecorationException">If no service of the specified <paramref name="serviceType"/> has been registered.</exception>
    /// <exception cref="ArgumentNullException">If either the <paramref name="services"/>,
    /// <paramref name="serviceType"/> or <paramref name="decorator"/> arguments are <c>null</c>.</exception>
    public static IServiceCollection Decorate(this IServiceCollection services, Type serviceType, Func<object, object> decorator)
    {
        return services.Decorate(serviceType, decorator, out _);
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
        return services.TryDecorate(serviceType, decorator, out _);
    }

    /// <summary>
    /// Decorates all registered services of the specified <paramref name="serviceType"/>
    /// using the <paramref name="decorator"/> function.
    /// </summary>
    /// <param name="services">The services to add to.</param>
    /// <param name="serviceType">The type of services to decorate.</param>
    /// <param name="decorator">The decorator function.</param>
    /// <exception cref="DecorationException">If no service of the specified <paramref name="serviceType"/> has been registered.</exception>
    /// <exception cref="ArgumentNullException">If either the <paramref name="services"/>,
    /// <paramref name="serviceType"/> or <paramref name="decorator"/> arguments are <c>null</c>.</exception>
    public static IServiceCollection Decorate(this IServiceCollection services, Type serviceType, Func<object, IServiceProvider, object> decorator)
    {
        return services.Decorate(serviceType, decorator, out _);
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
        return services.TryDecorate(serviceType, decorator, out _);
    }

    /// <summary>
    /// Decorates all registered services using the specified <paramref name="strategy"/>.
    /// </summary>
    /// <param name="services">The services to add to.</param>
    /// <param name="strategy">The strategy for decorating services.</param>
    /// <exception cref="DecorationException">If no registered service matched the specified <paramref name="strategy"/>.</exception>
    public static IServiceCollection Decorate(this IServiceCollection services, DecorationStrategy strategy)
    {
        return services.Decorate(strategy, out _);
    }

    /// <summary>
    /// Decorates all registered services using the specified <paramref name="strategy"/>.
    /// </summary>
    /// <param name="services">The services to add to.</param>
    /// <param name="strategy">The strategy for decorating services.</param>
    public static bool TryDecorate(this IServiceCollection services, DecorationStrategy strategy)
    {
        return services.TryDecorate(strategy, out _);
    }


    /// <summary>
    /// Decorates all registered services of type <typeparamref name="TService"/>
    /// using the specified type <typeparamref name="TDecorator"/>.
    /// </summary>
    /// <param name="services">The services to add to.</param>
    /// <param name="decorated">A handle to the service which was decorated. Using this, the service can be retrieved from the service provider via
    /// <see cref="ServiceProviderExtensions.GetRequiredDecoratedService{TService}(IServiceProvider, DecoratedService{TService})"/>.</param>
    /// <exception cref="DecorationException">If no service of the type <typeparamref name="TService"/> has been registered.</exception>
    /// <exception cref="ArgumentNullException">If the <paramref name="services"/> argument is <c>null</c>.</exception>
    public static IServiceCollection Decorate<TService, TDecorator>(this IServiceCollection services, out DecoratedService<TService> decorated)
        where TDecorator : TService
    {
        Preconditions.NotNull(services, nameof(services));

        services = services.Decorate(typeof(TService), typeof(TDecorator), out var decoratedObj);
        decorated = decoratedObj.Downcast<TService>();
        return services;
    }

    /// <summary>
    /// Decorates all registered services of type <typeparamref name="TService"/>
    /// using the specified type <typeparamref name="TDecorator"/>.
    /// </summary>
    /// <param name="services">The services to add to.</param>
    /// <param name="decorated">A handle to the service which was decorated. Using this, the service can be retrieved from the service provider via
    /// <see cref="ServiceProviderExtensions.GetRequiredDecoratedService{TService}(IServiceProvider, DecoratedService{TService})"/>.</param>
    /// <exception cref="ArgumentNullException">If the <paramref name="services"/> argument is <c>null</c>.</exception>
    public static bool TryDecorate<TService, TDecorator>(this IServiceCollection services, [NotNullWhen(true)] out DecoratedService<TService>? decorated)
        where TDecorator : TService
    {
        Preconditions.NotNull(services, nameof(services));

        var success = services.TryDecorate(typeof(TService), typeof(TDecorator), out var decoratedObj);
        decorated = success ? decoratedObj!.Downcast<TService>() : null;
        return success;
    }

    /// <summary>
    /// Decorates all registered services of the specified <paramref name="serviceType"/>
    /// using the specified <paramref name="decoratorType"/>.
    /// </summary>
    /// <param name="services">The services to add to.</param>
    /// <param name="serviceType">The type of services to decorate.</param>
    /// <param name="decoratorType">The type to decorate existing services with.</param>
    /// <param name="decorated">A handle to the service which was decorated. Using this, the service can be retrieved from the service provider via
    /// <see cref="ServiceProviderExtensions.GetRequiredDecoratedService{TService}(IServiceProvider, DecoratedService{TService})"/>.</param>
    /// <exception cref="DecorationException">If no service of the specified <paramref name="serviceType"/> has been registered.</exception>
    /// <exception cref="ArgumentNullException">If either the <paramref name="services"/>,
    /// <paramref name="serviceType"/> or <paramref name="decoratorType"/> arguments are <c>null</c>.</exception>
    public static IServiceCollection Decorate(this IServiceCollection services, Type serviceType, Type decoratorType, out DecoratedService<object> decorated)
    {
        Preconditions.NotNull(services, nameof(services));
        Preconditions.NotNull(serviceType, nameof(serviceType));
        Preconditions.NotNull(decoratorType, nameof(decoratorType));

        return services.Decorate(DecorationStrategy.WithType(serviceType, serviceKey: null, decoratorType), out decorated);
    }

    /// <summary>
    /// Decorates all registered services of the specified <paramref name="serviceType"/>
    /// using the specified <paramref name="decoratorType"/>.
    /// </summary>
    /// <param name="services">The services to add to.</param>
    /// <param name="serviceType">The type of services to decorate.</param>
    /// <param name="decoratorType">The type to decorate existing services with.</param>
    /// <param name="decorated">A handle to the service which was decorated. Using this, the service can be retrieved from the service provider via
    /// <see cref="ServiceProviderExtensions.GetRequiredDecoratedService{TService}(IServiceProvider, DecoratedService{TService})"/>.</param>
    /// <exception cref="ArgumentNullException">If either the <paramref name="services"/>,
    /// <paramref name="serviceType"/> or <paramref name="decoratorType"/> arguments are <c>null</c>.</exception>
    public static bool TryDecorate(this IServiceCollection services, Type serviceType, Type decoratorType, [NotNullWhen(true)] out DecoratedService<object>? decorated)
    {
        Preconditions.NotNull(services, nameof(services));
        Preconditions.NotNull(serviceType, nameof(serviceType));
        Preconditions.NotNull(decoratorType, nameof(decoratorType));

        return services.TryDecorate(DecorationStrategy.WithType(serviceType, serviceKey: null, decoratorType), out decorated);
    }

    /// <summary>
    /// Decorates all registered services of type <typeparamref name="TService"/>
    /// using the <paramref name="decorator"/> function.
    /// </summary>
    /// <typeparam name="TService">The type of services to decorate.</typeparam>
    /// <param name="services">The services to add to.</param>
    /// <param name="decorator">The decorator function.</param>
    /// <param name="decorated">A handle to the service which was decorated. Using this, the service can be retrieved from the service provider via
    /// <see cref="ServiceProviderExtensions.GetRequiredDecoratedService{TService}(IServiceProvider, DecoratedService{TService})"/>.</param>
    /// <exception cref="DecorationException">If no service of <typeparamref name="TService"/> has been registered.</exception>
    /// <exception cref="ArgumentNullException">If either the <paramref name="services"/>
    /// or <paramref name="decorator"/> arguments are <c>null</c>.</exception>
    public static IServiceCollection Decorate<TService>(this IServiceCollection services, Func<TService, TService> decorator, out DecoratedService<TService> decorated) where TService : notnull
    {
        Preconditions.NotNull(services, nameof(services));
        Preconditions.NotNull(decorator, nameof(decorator));

        return services.Decorate<TService>((service, _) => decorator(service), out decorated);
    }

    /// <summary>
    /// Decorates all registered services of type <typeparamref name="TService"/>
    /// using the <paramref name="decorator"/> function.
    /// </summary>
    /// <typeparam name="TService">The type of services to decorate.</typeparam>
    /// <param name="services">The services to add to.</param>
    /// <param name="decorator">The decorator function.</param>
    /// <param name="decorated">A handle to the service which was decorated. Using this, the service can be retrieved from the service provider via
    /// <see cref="ServiceProviderExtensions.GetRequiredDecoratedService{TService}(IServiceProvider, DecoratedService{TService})"/>.</param>
    /// <exception cref="ArgumentNullException">If either the <paramref name="services"/>
    /// or <paramref name="decorator"/> arguments are <c>null</c>.</exception>
    public static bool TryDecorate<TService>(this IServiceCollection services, Func<TService, TService> decorator, [NotNullWhen(true)] out DecoratedService<TService>? decorated) where TService : notnull
    {
        Preconditions.NotNull(services, nameof(services));
        Preconditions.NotNull(decorator, nameof(decorator));

        return services.TryDecorate<TService>((service, _) => decorator(service), out decorated);
    }

    /// <summary>
    /// Decorates all registered services of type <typeparamref name="TService"/>
    /// using the <paramref name="decorator"/> function.
    /// </summary>
    /// <typeparam name="TService">The type of services to decorate.</typeparam>
    /// <param name="services">The services to add to.</param>
    /// <param name="decorator">The decorator function.</param>
    /// <param name="decorated">A handle to the service which was decorated. Using this, the service can be retrieved from the service provider via
    /// <see cref="ServiceProviderExtensions.GetRequiredDecoratedService{TService}(IServiceProvider, DecoratedService{TService})"/>.</param>
    /// <exception cref="DecorationException">If no service of <typeparamref name="TService"/> has been registered.</exception>
    /// <exception cref="ArgumentNullException">If either the <paramref name="services"/>
    /// or <paramref name="decorator"/> arguments are <c>null</c>.</exception>
    public static IServiceCollection Decorate<TService>(this IServiceCollection services, Func<TService, IServiceProvider, TService> decorator, out DecoratedService<TService> decorated) where TService : notnull
    {
        Preconditions.NotNull(services, nameof(services));
        Preconditions.NotNull(decorator, nameof(decorator));

        services = services.Decorate(typeof(TService), (service, provider) => decorator((TService)service, provider), out var decoratedObj);
        decorated = decoratedObj.Downcast<TService>();
        return services;
    }

    /// <summary>
    /// Decorates all registered services of type <typeparamref name="TService"/>
    /// using the <paramref name="decorator"/> function.
    /// </summary>
    /// <typeparam name="TService">The type of services to decorate.</typeparam>
    /// <param name="services">The services to add to.</param>
    /// <param name="decorator">The decorator function.</param>
    /// <param name="decorated">A handle to the service which was decorated. Using this, the service can be retrieved from the service provider via
    /// <see cref="ServiceProviderExtensions.GetRequiredDecoratedService{TService}(IServiceProvider, DecoratedService{TService})"/>.</param>
    /// <exception cref="ArgumentNullException">If either the <paramref name="services"/>
    /// or <paramref name="decorator"/> arguments are <c>null</c>.</exception>
    public static bool TryDecorate<TService>(this IServiceCollection services, Func<TService, IServiceProvider, TService> decorator, [NotNullWhen(true)] out DecoratedService<TService>? decorated) where TService : notnull
    {
        Preconditions.NotNull(services, nameof(services));
        Preconditions.NotNull(decorator, nameof(decorator));

        var success = services.TryDecorate(typeof(TService), (service, provider) => decorator((TService)service, provider), out var decoratedObj);
        decorated = success ? decoratedObj!.Downcast<TService>() : null;
        return success;
    }

    /// <summary>
    /// Decorates all registered services of the specified <paramref name="serviceType"/>
    /// using the <paramref name="decorator"/> function.
    /// </summary>
    /// <param name="services">The services to add to.</param>
    /// <param name="serviceType">The type of services to decorate.</param>
    /// <param name="decorator">The decorator function.</param>
    /// <param name="decorated">A handle to the service which was decorated. Using this, the service can be retrieved from the service provider via
    /// <see cref="ServiceProviderExtensions.GetRequiredDecoratedService{TService}(IServiceProvider, DecoratedService{TService})"/>.</param>
    /// <exception cref="DecorationException">If no service of the specified <paramref name="serviceType"/> has been registered.</exception>
    /// <exception cref="ArgumentNullException">If either the <paramref name="services"/>,
    /// <paramref name="serviceType"/> or <paramref name="decorator"/> arguments are <c>null</c>.</exception>
    public static IServiceCollection Decorate(this IServiceCollection services, Type serviceType, Func<object, object> decorator, out DecoratedService<object> decorated)
    {
        Preconditions.NotNull(services, nameof(services));
        Preconditions.NotNull(serviceType, nameof(serviceType));
        Preconditions.NotNull(decorator, nameof(decorator));

        return services.Decorate(serviceType, (decorated, _) => decorator(decorated), out decorated);
    }

    /// <summary>
    /// Decorates all registered services of the specified <paramref name="serviceType"/>
    /// using the <paramref name="decorator"/> function.
    /// </summary>
    /// <param name="services">The services to add to.</param>
    /// <param name="serviceType">The type of services to decorate.</param>
    /// <param name="decorator">The decorator function.</param>
    /// <param name="decorated">A handle to the service which was decorated. Using this, the service can be retrieved from the service provider via
    /// <see cref="ServiceProviderExtensions.GetRequiredDecoratedService{TService}(IServiceProvider, DecoratedService{TService})"/>.</param>
    /// <exception cref="ArgumentNullException">If either the <paramref name="services"/>,
    /// <paramref name="serviceType"/> or <paramref name="decorator"/> arguments are <c>null</c>.</exception>
    public static bool TryDecorate(this IServiceCollection services, Type serviceType, Func<object, object> decorator, [NotNullWhen(true)] out DecoratedService<object>? decorated)
    {
        Preconditions.NotNull(services, nameof(services));
        Preconditions.NotNull(serviceType, nameof(serviceType));
        Preconditions.NotNull(decorator, nameof(decorator));

        return services.TryDecorate(serviceType, (decorated, _) => decorator(decorated), out decorated);
    }

    /// <summary>
    /// Decorates all registered services of the specified <paramref name="serviceType"/>
    /// using the <paramref name="decorator"/> function.
    /// </summary>
    /// <param name="services">The services to add to.</param>
    /// <param name="serviceType">The type of services to decorate.</param>
    /// <param name="decorator">The decorator function.</param>
    /// <param name="decorated">A handle to the service which was decorated. Using this, the service can be retrieved from the service provider via
    /// <see cref="ServiceProviderExtensions.GetRequiredDecoratedService{TService}(IServiceProvider, DecoratedService{TService})"/>.</param>
    /// <exception cref="DecorationException">If no service of the specified <paramref name="serviceType"/> has been registered.</exception>
    /// <exception cref="ArgumentNullException">If either the <paramref name="services"/>,
    /// <paramref name="serviceType"/> or <paramref name="decorator"/> arguments are <c>null</c>.</exception>
    public static IServiceCollection Decorate(this IServiceCollection services, Type serviceType, Func<object, IServiceProvider, object> decorator, out DecoratedService<object> decorated)
    {
        Preconditions.NotNull(services, nameof(services));
        Preconditions.NotNull(serviceType, nameof(serviceType));
        Preconditions.NotNull(decorator, nameof(decorator));

        return services.Decorate(DecorationStrategy.WithFactory(serviceType, serviceKey: null, decorator), out decorated);
    }

    /// <summary>
    /// Decorates all registered services of the specified <paramref name="serviceType"/>
    /// using the <paramref name="decorator"/> function.
    /// </summary>
    /// <param name="services">The services to add to.</param>
    /// <param name="serviceType">The type of services to decorate.</param>
    /// <param name="decorator">The decorator function.</param>
    /// <param name="decorated">A handle to the service which was decorated. Using this, the service can be retrieved from the service provider via
    /// <see cref="ServiceProviderExtensions.GetRequiredDecoratedService{TService}(IServiceProvider, DecoratedService{TService})"/>.</param>
    /// <exception cref="ArgumentNullException">If either the <paramref name="services"/>,
    /// <paramref name="serviceType"/> or <paramref name="decorator"/> arguments are <c>null</c>.</exception>
    public static bool TryDecorate(this IServiceCollection services, Type serviceType, Func<object, IServiceProvider, object> decorator, [NotNullWhen(true)] out DecoratedService<object>? decorated)
    {
        Preconditions.NotNull(services, nameof(services));
        Preconditions.NotNull(serviceType, nameof(serviceType));
        Preconditions.NotNull(decorator, nameof(decorator));

        return services.TryDecorate(DecorationStrategy.WithFactory(serviceType, serviceKey: null, decorator), out decorated);
    }

    /// <summary>
    /// Decorates all registered services using the specified <paramref name="strategy"/>.
    /// </summary>
    /// <param name="services">The services to add to.</param>
    /// <param name="strategy">The strategy for decorating services.</param>
    /// <param name="decorated">A handle to the service which was decorated. Using this, the service can be retrieved from the service provider via
    /// <see cref="ServiceProviderExtensions.GetRequiredDecoratedService{TService}(IServiceProvider, DecoratedService{TService})"/>.</param>
    /// <exception cref="DecorationException">If no registered service matched the specified <paramref name="strategy"/>.</exception>
    public static IServiceCollection Decorate(this IServiceCollection services, DecorationStrategy strategy, out DecoratedService<object> decorated)
    {
        if (services.TryDecorate(strategy, out decorated!))
        {
            return services;
        }

        throw new DecorationException(strategy);
    }

    /// <summary>
    /// Decorates all registered services using the specified <paramref name="strategy"/>.
    /// </summary>
    /// <param name="services">The services to add to.</param>
    /// <param name="strategy">The strategy for decorating services.</param>
    /// <param name="decorated">A handle to the service which was decorated. Using this, the service can be retrieved from the service provider via
    /// <see cref="ServiceProviderExtensions.GetRequiredDecoratedService{TService}(IServiceProvider, DecoratedService{TService})"/>.</param>
    public static bool TryDecorate(this IServiceCollection services, DecorationStrategy strategy, [NotNullWhen(true)] out DecoratedService<object>? decorated)
    {
        Preconditions.NotNull(services, nameof(services));
        Preconditions.NotNull(strategy, nameof(strategy));

        var decoratedKeys = new List<string>();

        for (var i = services.Count - 1; i >= 0; i--)
        {
            var serviceDescriptor = services[i];

            if (serviceDescriptor.IsDecorated() || !strategy.CanDecorate(serviceDescriptor))
            {
                continue;
            }

            var serviceKey = GetDecoratorKey(serviceDescriptor);
            if (serviceKey is null)
            {
                decorated = null;
                return false;
            }

            // Insert decorated
            services.Add(serviceDescriptor.WithServiceKey(serviceKey));
            decoratedKeys.Add(serviceKey);

            // Replace decorator
            services[i] = serviceDescriptor.WithImplementationFactory(strategy.CreateDecorator(serviceDescriptor.ServiceType, serviceKey));
        }

        decorated = new DecoratedService<object>(strategy.ServiceType, decoratedKeys);
        return decoratedKeys.Count > 0;
    }


    /// <summary>
    /// Returns <c>true</c> if the specified service is decorated.
    /// </summary>
    /// <param name="descriptor">The service descriptor.</param>
    public static bool IsDecorated(this ServiceDescriptor descriptor) =>
        descriptor.ServiceKey is string stringKey
            && stringKey.EndsWith(DecoratedServiceKeySuffix, StringComparison.Ordinal);

    private static string? GetDecoratorKey(ServiceDescriptor descriptor)
    {
        var uniqueId = Guid.NewGuid().ToString("n");

        if (descriptor.ServiceKey is null)
        {
            return $"{descriptor.ServiceType.Name}+{uniqueId}{DecoratedServiceKeySuffix}";
        }

        if (descriptor.ServiceKey is string stringKey)
        {
            return $"{stringKey}+{uniqueId}{DecoratedServiceKeySuffix}";
        }

        return null;
    }
}
