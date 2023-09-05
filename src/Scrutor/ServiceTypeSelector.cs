using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyModel;

namespace Scrutor;

internal class ServiceTypeSelector : IServiceTypeSelector, ISelector
{
    public ServiceTypeSelector(IImplementationTypeSelector inner, ISet<Type> types)
    {
        Inner = inner;
        Types = types;
    }

    private IImplementationTypeSelector Inner { get; }

    private ISet<Type> Types { get; }

    private List<ISelector> Selectors { get; } = new();

    private RegistrationStrategy? RegistrationStrategy { get; set; }

    public ILifetimeSelector AsSelf()
    {
        return As(t => new[] { t });
    }

    public ILifetimeSelector As<T>()
    {
        return As(typeof(T));
    }

    public ILifetimeSelector As(params Type[] types)
    {
        Preconditions.NotNull(types, nameof(types));

        return As(types.AsEnumerable());
    }

    public ILifetimeSelector As(IEnumerable<Type> types)
    {
        Preconditions.NotNull(types, nameof(types));

        return AddSelector(Types.Select(t => new TypeMap(t, types)), Enumerable.Empty<TypeFactoryMap>());
    }

    public ILifetimeSelector AsImplementedInterfaces()
    {
        return AsImplementedInterfaces(_ => true);
    }

    public ILifetimeSelector AsImplementedInterfaces(Func<Type, bool> predicate)
    {
        Preconditions.NotNull(predicate, nameof(predicate));

        return As(t => GetInterfaces(t).Where(predicate));
    }

    public ILifetimeSelector AsSelfWithInterfaces()
    {
        return AsSelfWithInterfaces(_ => true);
    }

    public ILifetimeSelector AsSelfWithInterfaces(Func<Type, bool> predicate)
    {
        Preconditions.NotNull(predicate, nameof(predicate));

        return AddSelector(
            Types.Select(t => new TypeMap(t, new[] { t })),
            Types.Select(t => new TypeFactoryMap(x => x.GetRequiredService(t), Selector(t, predicate))));

        static IEnumerable<Type> Selector(Type type, Func<Type, bool> predicate)
        {
            if (type.IsGenericTypeDefinition)
            {
                // This prevents trying to register open generic types
                // with an ImplementationFactory, which is unsupported.
                return Enumerable.Empty<Type>();
            }

            return GetInterfaces(type).Where(predicate);
        }
    }

    public ILifetimeSelector AsMatchingInterface()
    {
        return AsMatchingInterface(null);
    }

    public ILifetimeSelector AsMatchingInterface(Action<Type, IImplementationTypeFilter>? action)
    {
        return As(t => t.FindMatchingInterface(action));
    }

    public ILifetimeSelector As(Func<Type, IEnumerable<Type>> selector)
    {
        Preconditions.NotNull(selector, nameof(selector));

        return AddSelector(Types.Select(t => new TypeMap(t, selector(t))), Enumerable.Empty<TypeFactoryMap>());
    }

    public IImplementationTypeSelector UsingAttributes()
    {
        var selector = new AttributeSelector(Types);

        Selectors.Add(selector);

        return this;
    }

    public IServiceTypeSelector UsingRegistrationStrategy(RegistrationStrategy registrationStrategy)
    {
        Preconditions.NotNull(registrationStrategy, nameof(registrationStrategy));

        RegistrationStrategy = registrationStrategy;
        return this;
    }

    #region Chain Methods

    public IImplementationTypeSelector FromCallingAssembly()
    {
        return Inner.FromCallingAssembly();
    }

    public IImplementationTypeSelector FromExecutingAssembly()
    {
        return Inner.FromExecutingAssembly();
    }

    public IImplementationTypeSelector FromEntryAssembly()
    {
        return Inner.FromEntryAssembly();
    }

    public IImplementationTypeSelector FromApplicationDependencies()
    {
        return Inner.FromApplicationDependencies();
    }

    public IImplementationTypeSelector FromApplicationDependencies(Func<Assembly, bool> predicate)
    {
        return Inner.FromApplicationDependencies(predicate);
    }

    public IImplementationTypeSelector FromAssemblyDependencies(Assembly assembly)
    {
        return Inner.FromAssemblyDependencies(assembly);
    }

    public IImplementationTypeSelector FromDependencyContext(DependencyContext context)
    {
        return Inner.FromDependencyContext(context);
    }

    public IImplementationTypeSelector FromDependencyContext(DependencyContext context, Func<Assembly, bool> predicate)
    {
        return Inner.FromDependencyContext(context, predicate);
    }

    public IImplementationTypeSelector FromAssemblyOf<T>()
    {
        return Inner.FromAssemblyOf<T>();
    }

    public IImplementationTypeSelector FromAssembliesOf(params Type[] types)
    {
        return Inner.FromAssembliesOf(types);
    }

    public IImplementationTypeSelector FromAssembliesOf(IEnumerable<Type> types)
    {
        return Inner.FromAssembliesOf(types);
    }

    public IImplementationTypeSelector FromAssemblies(params Assembly[] assemblies)
    {
        return Inner.FromAssemblies(assemblies);
    }

    public IImplementationTypeSelector FromAssemblies(IEnumerable<Assembly> assemblies)
    {
        return Inner.FromAssemblies(assemblies);
    }

    public IServiceTypeSelector AddClasses()
    {
        return Inner.AddClasses();
    }

    public IServiceTypeSelector AddClasses(bool publicOnly)
    {
        return Inner.AddClasses(publicOnly);
    }

    public IServiceTypeSelector AddClasses(Action<IImplementationTypeFilter> action)
    {
        return Inner.AddClasses(action);
    }

    public IServiceTypeSelector AddClasses(Action<IImplementationTypeFilter> action, bool publicOnly)
    {
        return Inner.AddClasses(action, publicOnly);
    }

    #endregion

    internal void PropagateLifetime(ServiceLifetime lifetime)
    {
        foreach (var selector in Selectors.OfType<LifetimeSelector>())
        {
            selector.Lifetime = lifetime;
        }
    }

    void ISelector.Populate(IServiceCollection services, RegistrationStrategy? registrationStrategy)
    {
        if (Selectors.Count == 0)
        {
            AsSelf();
        }

        var strategy = RegistrationStrategy ?? registrationStrategy;

        foreach (var selector in Selectors)
        {
            selector.Populate(services, strategy);
        }
    }

    private static IEnumerable<Type> GetInterfaces(Type type) =>
        type.GetInterfaces()
            .Where(x => ShouldRegister(x, type))
            .Select(x => x.GetRegistrationType(type))
            .ToList();

    private static bool ShouldRegister(Type serviceType, Type implementationType)
    {
        if (!serviceType.HasMatchingGenericArity(implementationType))
        {
            return false;
        }

        if (serviceType.IsGenericType && serviceType.GetGenericTypeDefinition() == typeof(IEnumerable<>))
        {
            return false;
        }

        if (serviceType == typeof(IEnumerable))
        {
            return false;
        }

        return true;
    }

    private ILifetimeSelector AddSelector(IEnumerable<TypeMap> types, IEnumerable<TypeFactoryMap> factories)
    {
        var selector = new LifetimeSelector(this, types, factories);

        Selectors.Add(selector);

        return selector;
    }
}
