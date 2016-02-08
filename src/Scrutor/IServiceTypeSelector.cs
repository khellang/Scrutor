using System;
using System.Collections.Generic;
using System.Reflection;

namespace Scrutor
{
    public interface IServiceTypeSelector : IImplementationTypeSelector
    {
        /// <summary>
        /// Registers each matching concrete type as itself.
        /// </summary>
        ILifetimeSelector AsSelf();

        /// <summary>
        /// Registers each matching concrete type as <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The type to register as.</typeparam>
        ILifetimeSelector As<T>();

        /// <summary>
        /// Registers each matching concrete type as each of the specified <paramref name="types" />.
        /// </summary>
        /// <param name="types">The types to register as.</param>
        /// <exception cref="ArgumentNullException">If the <paramref name="types"/> argument is <c>null</c>.</exception>
        ILifetimeSelector As(params Type[] types);

        /// <summary>
        /// Registers each matching concrete type as each of the specified <paramref name="types" />.
        /// </summary>
        /// <param name="types">The types to register as.</param>
        /// <exception cref="ArgumentNullException">If the <paramref name="types"/> argument is <c>null</c>.</exception>
        ILifetimeSelector As(IEnumerable<Type> types);

        /// <summary>
        /// Registers each matching concrete type as all of its implemented interfaces.
        /// </summary>
        ILifetimeSelector AsImplementedInterfaces();

        /// <summary>
        /// Registers each matching concrete type as each of the types returned
        /// from the <paramref name="selector"/> function.
        /// </summary>
        /// <param name="selector">A function to select service types based on implementation types.</param>
        /// <exception cref="ArgumentNullException">If the <paramref name="selector"/> argument is <c>null</c>.</exception>
        ILifetimeSelector As(Func<Type, IEnumerable<Type>> selector);

        /// <summary>
        /// Registers the type with the first found matching interface name.  (e.g. ClassName is matched to IClassName)
        /// </summary>
        /// <returns></returns>
        ILifetimeSelector WithMatchingInterface();

        /// <summary>
        /// Registers the type with the first found matching interface name.  (e.g. ClassName is matched to IClassName) 
        /// </summary>
        /// <param name="action">Filter for matching the Type to an implementing interface</param>
        /// <returns></returns>
        ILifetimeSelector WithMatchingInterface(Action<TypeInfo, IImplementationTypeFilter> action);
    }
}