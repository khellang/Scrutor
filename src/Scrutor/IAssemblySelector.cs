using System;
using System.Collections.Generic;
using System.Reflection;

#if DEPENDENCY_MODEL
using Microsoft.Extensions.DependencyModel;
#endif

namespace Scrutor
{
    public interface IAssemblySelector : IFluentInterface
    {
#if NET451
        /// <summary>
        /// Will scan for types from the calling assembly.
        /// </summary>
        IImplementationTypeSelector FromCallingAssembly();

        /// <summary>
        /// Will scan for types from the currently executing assembly.
        /// </summary>
        IImplementationTypeSelector FromExecutingAssembly();
#endif

#if DEPENDENCY_MODEL
        /// <summary>
        /// Will scan for types from the entry assembly.
        /// </summary>
        IImplementationTypeSelector FromEntryAssembly();

        /// <summary>
        /// Will load and scan all runtime libraries referenced by the currently executing application.
        /// Calling this method is equivalent to calling <see cref="FromDependencyContext"/> and passing in <see cref="DependencyContext.Default"/>.
        /// </summary>
        IImplementationTypeSelector FromApplicationDependencies();

        /// <summary>
        /// Will load and scan all runtime libraries in the given <paramref name="context"/>.
        /// </summary>
        /// <param name="context">The dependency context.</param>
        IImplementationTypeSelector FromDependencyContext(DependencyContext context);
#endif

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
}