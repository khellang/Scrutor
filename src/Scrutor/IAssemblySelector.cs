using System;
using System.Collections.Generic;
using System.Reflection;

namespace Scrutor
{
    public interface IAssemblySelector : IFluentInterface
    {
        /// <summary>
        /// Will scan for types from the assembly of type <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The type in which assembly that should be scanned.</typeparam>
        /// <exception cref="ArgumentNullException">If the <paramref name="types"/> argument is <c>null</c>.</exception>
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