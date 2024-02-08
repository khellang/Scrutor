using System;
using System.Collections.Generic;
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
        return AddClasses(action, publicOnly: false);
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

    private IServiceTypeSelector AddSelector(IEnumerable<Type> types)
    {
        var selector = new ServiceTypeSelector(this, types.ToHashSet());

        Selectors.Add(selector);

        return selector;
    }

    private ISet<Type> GetNonAbstractClasses(bool publicOnly)
    {
        return Types.Where(t => t.IsNonAbstractClass(publicOnly)).ToHashSet();
    }
}
