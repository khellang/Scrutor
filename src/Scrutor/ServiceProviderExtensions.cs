using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;

namespace Scrutor;

public static class ServiceProviderExtensions
{
    /// <summary>
    /// Get all decorated services of type <typeparamref name="TService"/> from the <see cref="IServiceProvider"/>.
    /// </summary>
    /// <typeparam name="TService">The type of services which were decorated.</typeparam>
    /// <param name="serviceProvider">The <see cref="IServiceProvider"/> to retrieve the service objects from.</param>
    /// <param name="decoratedType">A handle to a decorated service, obtainable from a <see cref="Microsoft.Extensions.DependencyInjection.ServiceCollectionExtensions.Decorate{TService, TDecorator}(IServiceCollection, out DecoratedService{TService})"/>
    /// overload which omits one as an <c>out</c> parameter.</param>
    /// <returns>A service object of type <typeparamref name="TService"/>.</returns>
    public static IEnumerable<TService> GetDecoratedServices<TService>(this IServiceProvider serviceProvider, DecoratedService<TService> decoratedType)
    {
        return decoratedType.ServiceKeys.Reverse().Select(key => (TService)serviceProvider.GetRequiredKeyedService(decoratedType.ServiceType, key));
    }

    /// <summary>
    /// Get decorated service of type <typeparamref name="TService"/> from the <see cref="IServiceProvider"/>.
    /// </summary>
    /// <typeparam name="TService">The type of service which was decorated.</typeparam>
    /// <param name="serviceProvider">The <see cref="IServiceProvider"/> to retrieve the service object from.</param>
    /// <param name="decoratedType">A handle to a decorated service, obtainable from a <see cref="Microsoft.Extensions.DependencyInjection.ServiceCollectionExtensions.Decorate{TService, TDecorator}(IServiceCollection, out DecoratedService{TService})"/>
    /// overload which omits one as an <c>out</c> parameter.</param>
    /// <returns>A service object of type <typeparamref name="TService"/>.</returns>
    public static TService GetRequiredDecoratedService<TService>(this IServiceProvider serviceProvider, DecoratedService<TService> decoratedType)
    {
        return (TService)serviceProvider.GetRequiredKeyedService(decoratedType.ServiceType, decoratedType.ServiceKeys[0]);
    }
}
