using System;
using Microsoft.Extensions.DependencyInjection;

namespace Scrutor;

public interface ILifetimeSelector : IServiceTypeSelector
{
    /// <summary>
    /// Registers each matching concrete type with <see cref="ServiceLifetime.Singleton"/> lifetime.
    /// </summary>
    IImplementationTypeSelector WithSingletonLifetime();

    /// <summary>
    /// Registers each matching concrete type with <see cref="ServiceLifetime.Scoped"/> lifetime.
    /// </summary>
    IImplementationTypeSelector WithScopedLifetime();

    /// <summary>
    /// Registers each matching concrete type with <see cref="ServiceLifetime.Transient"/> lifetime.
    /// </summary>
    IImplementationTypeSelector WithTransientLifetime();

    /// <summary>
    /// Registers each matching concrete type with the specified <paramref name="lifetime"/>.
    /// </summary>
    IImplementationTypeSelector WithLifetime(ServiceLifetime lifetime);

    /// <summary>
    /// Registers each matching concrete type with a lifetime based on the provided <paramref name="selector"/>.
    /// </summary>
    IImplementationTypeSelector WithLifetime(Func<Type, ServiceLifetime> selector);
}
