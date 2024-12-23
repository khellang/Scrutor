using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyModel;

namespace Scrutor;

internal sealed class LifetimeSelector : ILifetimeSelector, ISelector
{
    public LifetimeSelector(ServiceTypeSelector inner, IEnumerable<TypeMap> typeMaps, IEnumerable<TypeFactoryMap> typeFactoryMaps)
    {
        Inner = inner;
        TypeMaps = typeMaps;
        TypeFactoryMaps = typeFactoryMaps;
    }

    private ServiceTypeSelector Inner { get; }

    private IEnumerable<TypeMap> TypeMaps { get; }

    private IEnumerable<TypeFactoryMap> TypeFactoryMaps { get; }

    public Func<Type, ServiceLifetime>? SelectorFn { get; set; }

    public IImplementationTypeSelector WithSingletonLifetime()
    {
        return WithLifetime(ServiceLifetime.Singleton);
    }

    public IImplementationTypeSelector WithScopedLifetime()
    {
        return WithLifetime(ServiceLifetime.Scoped);
    }

    public IImplementationTypeSelector WithTransientLifetime()
    {
        return WithLifetime(ServiceLifetime.Transient);
    }
    public IImplementationTypeSelector WithLifetime(ServiceLifetime lifetime)
    {
        Preconditions.IsDefined(lifetime, nameof(lifetime));

        return WithLifetime(_ => lifetime);
    }

    public IImplementationTypeSelector WithLifetime(Func<Type, ServiceLifetime> selector)
    {
        Preconditions.NotNull(selector, nameof(selector));

        Inner.PropagateLifetime(selector);

        return this;
    }

    #region Chain Methods

    [ExcludeFromCodeCoverage]
    public IImplementationTypeSelector FromCallingAssembly()
    {
        return Inner.FromCallingAssembly();
    }

    [ExcludeFromCodeCoverage]
    public IImplementationTypeSelector FromExecutingAssembly()
    {
        return Inner.FromExecutingAssembly();
    }

    [ExcludeFromCodeCoverage]
    public IImplementationTypeSelector FromEntryAssembly()
    {
        return Inner.FromEntryAssembly();
    }

    [ExcludeFromCodeCoverage]
    public IImplementationTypeSelector FromApplicationDependencies()
    {
        return Inner.FromApplicationDependencies();
    }

    [ExcludeFromCodeCoverage]
    public IImplementationTypeSelector FromApplicationDependencies(Func<Assembly, bool> predicate)
    {
        return Inner.FromApplicationDependencies(predicate);
    }

    [ExcludeFromCodeCoverage]
    public IImplementationTypeSelector FromAssemblyDependencies(Assembly assembly)
    {
        return Inner.FromAssemblyDependencies(assembly);
    }

    [ExcludeFromCodeCoverage]
    public IImplementationTypeSelector FromDependencyContext(DependencyContext context)
    {
        return Inner.FromDependencyContext(context);
    }

    [ExcludeFromCodeCoverage]
    public IImplementationTypeSelector FromDependencyContext(DependencyContext context, Func<Assembly, bool> predicate)
    {
        return Inner.FromDependencyContext(context, predicate);
    }

    [ExcludeFromCodeCoverage]
    public IImplementationTypeSelector FromAssemblyOf<T>()
    {
        return Inner.FromAssemblyOf<T>();
    }

    [ExcludeFromCodeCoverage]
    public IImplementationTypeSelector FromAssembliesOf(params Type[] types)
    {
        return Inner.FromAssembliesOf(types);
    }

    [ExcludeFromCodeCoverage]
    public IImplementationTypeSelector FromAssembliesOf(IEnumerable<Type> types)
    {
        return Inner.FromAssembliesOf(types);
    }

    [ExcludeFromCodeCoverage]
    public IImplementationTypeSelector FromAssemblies(params Assembly[] assemblies)
    {
        return Inner.FromAssemblies(assemblies);
    }

    [ExcludeFromCodeCoverage]
    public IImplementationTypeSelector FromAssemblies(IEnumerable<Assembly> assemblies)
    {
        return Inner.FromAssemblies(assemblies);
    }

    [ExcludeFromCodeCoverage]
    public IServiceTypeSelector AddClasses()
    {
        return Inner.AddClasses();
    }

    [ExcludeFromCodeCoverage]
    public IServiceTypeSelector AddClasses(bool publicOnly)
    {
        return Inner.AddClasses(publicOnly);
    }

    [ExcludeFromCodeCoverage]
    public IServiceTypeSelector AddClasses(Action<IImplementationTypeFilter> action)
    {
        return Inner.AddClasses(action);
    }

    [ExcludeFromCodeCoverage]
    public IServiceTypeSelector AddClasses(Action<IImplementationTypeFilter> action, bool publicOnly)
    {
        return Inner.AddClasses(action, publicOnly);
    }

    [ExcludeFromCodeCoverage]
    public ILifetimeSelector AsSelf()
    {
        return Inner.AsSelf();
    }

    [ExcludeFromCodeCoverage]
    public ILifetimeSelector As<T>()
    {
        return Inner.As<T>();
    }

    [ExcludeFromCodeCoverage]
    public ILifetimeSelector As(params Type[] types)
    {
        return Inner.As(types);
    }

    [ExcludeFromCodeCoverage]
    public ILifetimeSelector As(IEnumerable<Type> types)
    {
        return Inner.As(types);
    }

    [ExcludeFromCodeCoverage]
    public ILifetimeSelector AsImplementedInterfaces()
    {
        return Inner.AsImplementedInterfaces();
    }

    [ExcludeFromCodeCoverage]
    public ILifetimeSelector AsImplementedInterfaces(Func<Type, bool> predicate)
    {
        return Inner.AsImplementedInterfaces(predicate);
    }

    [ExcludeFromCodeCoverage]
    public ILifetimeSelector AsSelfWithInterfaces()
    {
        return Inner.AsSelfWithInterfaces();
    }

    [ExcludeFromCodeCoverage]
    public ILifetimeSelector AsSelfWithInterfaces(Func<Type, bool> predicate)
    {
        return Inner.AsSelfWithInterfaces(predicate);
    }

    [ExcludeFromCodeCoverage]
    public ILifetimeSelector AsMatchingInterface()
    {
        return Inner.AsMatchingInterface();
    }

    [ExcludeFromCodeCoverage]
    public ILifetimeSelector AsMatchingInterface(Action<Type, IImplementationTypeFilter> action)
    {
        return Inner.AsMatchingInterface(action);
    }

    [ExcludeFromCodeCoverage]
    public ILifetimeSelector As(Func<Type, IEnumerable<Type>> selector)
    {
        return Inner.As(selector);
    }

    [ExcludeFromCodeCoverage]
    public IImplementationTypeSelector UsingAttributes()
    {
        return Inner.UsingAttributes();
    }

    [ExcludeFromCodeCoverage]
    public IServiceTypeSelector UsingRegistrationStrategy(RegistrationStrategy registrationStrategy)
    {
        return Inner.UsingRegistrationStrategy(registrationStrategy);
    }

    #endregion

    void ISelector.Populate(IServiceCollection services, RegistrationStrategy? strategy)
    {
        strategy ??= RegistrationStrategy.Append;

        var lifetimes = new Dictionary<Type, ServiceLifetime>();

        foreach (var typeMap in TypeMaps)
        {
            foreach (var serviceType in typeMap.ServiceTypes)
            {
                var implementationType = typeMap.ImplementationType;

                if (!implementationType.IsBasedOn(serviceType))
                {
                    throw new InvalidOperationException($@"Type ""{implementationType.ToFriendlyName()}"" is not assignable to ""${serviceType.ToFriendlyName()}"".");
                }

                var lifetime = GetOrAddLifetime(lifetimes, implementationType);

                var descriptor = new ServiceDescriptor(serviceType, implementationType, lifetime);

                strategy.Apply(services, descriptor);
            }
        }

        foreach (var typeFactoryMap in TypeFactoryMaps)
        {
            foreach (var serviceType in typeFactoryMap.ServiceTypes)
            {
                var lifetime = GetOrAddLifetime(lifetimes, typeFactoryMap.ImplementationType);

                var descriptor = new ServiceDescriptor(serviceType, typeFactoryMap.ImplementationFactory, lifetime);

                strategy.Apply(services, descriptor);
            }
        }
    }

    private ServiceLifetime GetOrAddLifetime(Dictionary<Type, ServiceLifetime> lifetimes, Type implementationType)
    {
        if (lifetimes.TryGetValue(implementationType, out var lifetime))
        {
            return lifetime;
        }

        lifetime = SelectorFn?.Invoke(implementationType) ?? ServiceLifetime.Transient;

        lifetimes[implementationType] = lifetime;

        return lifetime;
    }
}
