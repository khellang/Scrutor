using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyModel;

namespace Scrutor;

internal class ImplementationTypeSelector : IImplementationTypeSelector, ISelector
{
    public ImplementationTypeSelector(ITypeSourceSelector inner, ISet<Type> types)
    {
        Inner = inner;
        Types = types;
    }

    private ITypeSourceSelector Inner { get; }

    private ISet<Type> Types { get; }

    private List<ISelector> Selectors { get; } = new();

    public IServiceTypeSelector AddClasses()
    {
        return AddClasses(publicOnly: true);
    }

    public IServiceTypeSelector AddClasses(bool publicOnly)
    {
        var classes = GetNonAbstractClasses(publicOnly);

        return AddSelector(classes);
    }

    public IServiceTypeSelector AddClasses(Action<IImplementationTypeFilter> action)
    {
        return AddClasses(action, publicOnly: true);
    }

    public IServiceTypeSelector AddClasses(Action<IImplementationTypeFilter> action, bool publicOnly)
    {
        Preconditions.NotNull(action, nameof(action));

        var classes = GetNonAbstractClasses(publicOnly);

        var filter = new ImplementationTypeFilter(classes);

        action(filter);

        return AddSelector(filter.Types);
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

    #endregion

    void ISelector.Populate(IServiceCollection services, RegistrationStrategy? registrationStrategy)
    {
        if (Selectors.Count == 0)
        {
            AddClasses();
        }

        foreach (var selector in Selectors)
        {
            selector.Populate(services, registrationStrategy);
        }
    }

    private IServiceTypeSelector AddSelector(ISet<Type> types)
    {
        var selector = new ServiceTypeSelector(this, types);

        Selectors.Add(selector);

        return selector;
    }

    private ISet<Type> GetNonAbstractClasses(bool publicOnly)
    {
        return Types.Where(t => t.IsNonAbstractClass(publicOnly)).ToHashSet();
    }
}
