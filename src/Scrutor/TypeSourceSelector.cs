using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyModel;

namespace Scrutor;

public class TypeSourceSelector : ITypeSourceSelector, ISelector
{
    private static Assembly EntryAssembly => Assembly.GetEntryAssembly()
                                             ?? throw new InvalidOperationException("Could not get entry assembly.");

    private List<ISelector> Selectors { get; } = new();

    /// <inheritdoc />
    public IImplementationTypeSelector FromAssemblyOf<T>()
    {
        return InternalFromAssembliesOf(new[] { typeof(T) });
    }

    public IImplementationTypeSelector FromCallingAssembly()
    {
        return FromAssemblies(Assembly.GetCallingAssembly());
    }

    public IImplementationTypeSelector FromExecutingAssembly()
    {
        return FromAssemblies(Assembly.GetExecutingAssembly());
    }

    public IImplementationTypeSelector FromEntryAssembly()
    {
        return FromAssemblies(EntryAssembly);
    }

    public IImplementationTypeSelector FromApplicationDependencies()
    {
        return FromApplicationDependencies(_ => true);
    }

    public IImplementationTypeSelector FromApplicationDependencies(Func<Assembly, bool> predicate)
    {
        try
        {
            var context = DependencyContext.Default;
            if (context is null)
            {
                return FromAssemblyDependencies(EntryAssembly);
            }

            return FromDependencyContext(context, predicate);
        }
        catch
        {
            // Something went wrong when loading the DependencyContext, fall
            // back to loading all referenced assemblies of the entry assembly...
            return FromAssemblyDependencies(EntryAssembly);
        }
    }

    public IImplementationTypeSelector FromDependencyContext(DependencyContext context)
    {
        return FromDependencyContext(context, _ => true);
    }

    public IImplementationTypeSelector FromDependencyContext(DependencyContext context, Func<Assembly, bool> predicate)
    {
        Preconditions.NotNull(context, nameof(context));
        Preconditions.NotNull(predicate, nameof(predicate));

        var assemblyNames = context.RuntimeLibraries
            .SelectMany(library => library.GetDefaultAssemblyNames(context))
            .ToHashSet();

        var assemblies = LoadAssemblies(assemblyNames);

        return InternalFromAssemblies(assemblies.Where(predicate));
    }

    public IImplementationTypeSelector FromAssemblyDependencies(Assembly assembly)
    {
        Preconditions.NotNull(assembly, nameof(assembly));

        try
        {
            var dependencyNames = assembly
                .GetReferencedAssemblies()
                .ToHashSet();

            var assemblies = LoadAssemblies(dependencyNames);

            assemblies.Add(assembly);

            return InternalFromAssemblies(assemblies);
        }
        catch
        {
            return FromAssemblies(assembly);
        }
    }

    public IImplementationTypeSelector FromAssembliesOf(params Type[] types)
    {
        Preconditions.NotNull(types, nameof(types));

        return InternalFromAssembliesOf(types);
    }

    public IImplementationTypeSelector FromAssembliesOf(IEnumerable<Type> types)
    {
        Preconditions.NotNull(types, nameof(types));

        return InternalFromAssembliesOf(types);
    }

    public IImplementationTypeSelector FromAssemblies(params Assembly[] assemblies)
    {
        Preconditions.NotNull(assemblies, nameof(assemblies));

        return InternalFromAssemblies(assemblies);
    }

    public IImplementationTypeSelector FromAssemblies(IEnumerable<Assembly> assemblies)
    {
        Preconditions.NotNull(assemblies, nameof(assemblies));

        return InternalFromAssemblies(assemblies);
    }

    public IServiceTypeSelector FromTypes(params Type[] types) => FromTypes(types.AsEnumerable());

    public IServiceTypeSelector FromTypes(IEnumerable<Type> types)
    {
        Preconditions.NotNull(types, nameof(types));

        return AddSelector(types).AddClasses();
    }

    public void Populate(IServiceCollection services, RegistrationStrategy? registrationStrategy)
    {
        foreach (var selector in Selectors)
        {
            selector.Populate(services, registrationStrategy);
        }
    }

    private IImplementationTypeSelector InternalFromAssembliesOf(IEnumerable<Type> types)
    {
        return InternalFromAssemblies(types.Select(t => t.Assembly));
    }

    private IImplementationTypeSelector InternalFromAssemblies(IEnumerable<Assembly> assemblies)
    {
        return AddSelector(assemblies.SelectMany(asm => asm.GetTypes()));
    }

    private static ISet<Assembly> LoadAssemblies(ISet<AssemblyName> assemblyNames)
    {
        var assemblies = new HashSet<Assembly>();

        foreach (var assemblyName in assemblyNames)
        {
            try
            {
                // Try to load the referenced assembly...
                assemblies.Add(Assembly.Load(assemblyName));
            }
            catch
            {
                // Failed to load assembly. Skip it.
            }
        }

        return assemblies;
    }

    private IImplementationTypeSelector AddSelector(IEnumerable<Type> types)
    {
        var selector = new ImplementationTypeSelector(this, types.ToHashSet());

        Selectors.Add(selector);

        return selector;
    }
}
