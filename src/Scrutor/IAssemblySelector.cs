using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.Extensions.DependencyModel;

namespace Scrutor;

public interface IAssemblySelector : IFluentInterface
{
    /// <summary>
    /// Will scan for types from the calling assembly.
    /// </summary>
    IImplementationTypeSelector FromCallingAssembly();

    /// <summary>
    /// Will scan for types from the currently executing assembly.
    /// </summary>
    IImplementationTypeSelector FromExecutingAssembly();

    /// <summary>
    /// Will scan for types from the entry assembly.
    /// </summary>
    IImplementationTypeSelector FromEntryAssembly();

    /// <summary>
    /// Will load and scan all runtime libraries referenced by the currently executing application.
    /// Calling this method is equivalent to calling <see cref="FromDependencyContext(DependencyContext)"/> and passing in <see cref="DependencyContext.Default"/>.
    /// </summary>
    /// <remarks>
    /// If loading <see cref="DependencyContext.Default"/> fails, this method will fall back to calling <see cref="FromAssemblyDependencies(Assembly)"/>,
    /// using the entry assembly.
    /// </remarks>
    IImplementationTypeSelector FromApplicationDependencies();

    /// <summary>
    /// Will load and scan all runtime libraries referenced by the currently executing application.
    /// Calling this method is equivalent to calling <see cref="FromDependencyContext(DependencyContext, Func{Assembly, bool})"/> and passing in <see cref="DependencyContext.Default"/>.
    /// </summary>
    /// <remarks>
    /// If loading <see cref="DependencyContext.Default"/> fails, this method will fall back to calling <see cref="FromAssemblyDependencies(Assembly)"/>,
    /// using the entry assembly.
    /// </remarks>
    /// <param name="predicate">The predicate to match assemblies.</param>
    IImplementationTypeSelector FromApplicationDependencies(Func<Assembly, bool> predicate);

    /// <summary>
    /// Will load and scan all runtime libraries referenced by the currently specified <paramref name="assembly"/>.
    /// </summary>
    /// <param name="assembly">The assembly whose dependencies should be scanned.</param>
    IImplementationTypeSelector FromAssemblyDependencies(Assembly assembly);

    /// <summary>
    /// Will load and scan all runtime libraries in the given <paramref name="context"/>.
    /// </summary>
    /// <param name="context">The dependency context.</param>
    IImplementationTypeSelector FromDependencyContext(DependencyContext context);

    /// <summary>
    /// Will load and scan all runtime libraries in the given <paramref name="context"/>.
    /// </summary>
    /// <param name="context">The dependency context.</param>
    /// <param name="predicate">The predicate to match assemblies.</param>
    IImplementationTypeSelector FromDependencyContext(DependencyContext context, Func<Assembly, bool> predicate);

    /// <summary>
    /// Will scan for types from the assembly of type <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The type in which assembly that should be scanned.</typeparam>
    IImplementationTypeSelector FromAssemblyOf<T>();

    /// <summary>
    /// Will scan for types from the assemblies of each <see cref="Type"/> in <paramref name="types"/>.
    /// </summary>
    /// <param name="types">The types in which assemblies that should be scanned.</param>
    /// <exception cref="ArgumentNullException">If the <paramref name="types"/> argument is <c>null</c>.</exception>
    IImplementationTypeSelector FromAssembliesOf(params Type[] types);

    /// <summary>
    /// Will scan for types from the assemblies of each <see cref="Type"/> in <paramref name="types"/>.
    /// </summary>
    /// <param name="types">The types in which assemblies that should be scanned.</param>
    /// <exception cref="ArgumentNullException">If the <paramref name="types"/> argument is <c>null</c>.</exception>
    IImplementationTypeSelector FromAssembliesOf(IEnumerable<Type> types);

    /// <summary>
    /// Will scan for types in each <see cref="Assembly"/> in <paramref name="assemblies"/>.
    /// </summary>
    /// <param name="assemblies">The assemblies to should be scanned.</param>
    /// <exception cref="ArgumentNullException">If the <paramref name="assemblies"/> argument is <c>null</c>.</exception>
    IImplementationTypeSelector FromAssemblies(params Assembly[] assemblies);

    /// <summary>
    /// Will scan for types in each <see cref="Assembly"/> in <paramref name="assemblies"/>.
    /// </summary>
    /// <param name="assemblies">The assemblies to should be scanned.</param>
    /// <exception cref="ArgumentNullException">If the <paramref name="assemblies"/> argument is <c>null</c>.</exception>
    IImplementationTypeSelector FromAssemblies(IEnumerable<Assembly> assemblies);
}
