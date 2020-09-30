using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace Scrutor.Static
{
    public interface ICompiledImplementationTypeFilter
    {
        /// <summary>
        /// Will match all types that are assignable to <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The type that should be assignable from the matching types.</typeparam>
        ICompiledImplementationTypeFilter AssignableTo<T>();

        /// <summary>
        /// Will match all types that are assignable to the specified <paramref name="type" />.
        /// </summary>
        /// <param name="type">The type that should be assignable from the matching types.</param>
        /// <exception cref="ArgumentNullException">If the <paramref name="type"/> argument is <c>null</c>.</exception>
        ICompiledImplementationTypeFilter AssignableTo(Type type);

        /// <summary>
        /// Will match all types that are assignable to any of the specified <paramref name="types" />.
        /// </summary>
        /// <param name="first">The first type that should be assignable from the matching types.</param>
        /// <param name="types">The types that should be assignable from the matching types.</param>
        /// <exception cref="ArgumentNullException">If the <paramref name="types"/> argument is <c>null</c>.</exception>
        ICompiledImplementationTypeFilter AssignableToAny(Type first, params Type[] types);

        /// <summary>
        /// Will match all types in the exact same namespace as the type <typeparamref name="T"/>
        /// </summary>
        /// <typeparam name="T">The type in the namespace to include</typeparam>
        ICompiledImplementationTypeFilter InExactNamespaceOf<T>();

        /// <summary>
        /// Will match all types in the exact same namespace as the type <paramref name="types"/>
        /// </summary>
        /// <param name="first">The first type in the namespaces to include.</param>
        /// <param name="types">The types in the namespaces to include.</param>
        /// <exception cref="ArgumentNullException">If the <paramref name="types"/> argument is <c>null</c>.</exception>
        ICompiledImplementationTypeFilter InExactNamespaceOf(Type first, params Type[] types);

        /// <summary>
        /// Will match all types in the exact same namespace as the type <paramref name="namespaces"/>
        /// </summary>
        /// <param name="first">The first namespace to include.</param>
        /// <param name="namespaces">The namespace to include.</param>
        /// <exception cref="ArgumentNullException">If the <paramref name="namespaces"/> argument is <c>null</c>.</exception>
        ICompiledImplementationTypeFilter InExactNamespaces(string first, params string[] namespaces);

        /// <summary>
        /// Will match all types in the same namespace as the type <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">A type inside the namespace to include.</typeparam>
        ICompiledImplementationTypeFilter InNamespaceOf<T>();

        /// <summary>
        /// Will match all types in any of the namespaces of the <paramref name="types" /> specified.
        /// </summary>
        /// <param name="first">The first type in the namespaces to include.</param>
        /// <param name="types">The types in the namespaces to include.</param>
        /// <exception cref="ArgumentNullException">If the <paramref name="types"/> argument is <c>null</c>.</exception>
        ICompiledImplementationTypeFilter InNamespaceOf(Type first, params Type[] types);

        /// <summary>
        /// Will match all types in any of the <paramref name="namespaces"/> specified.
        /// </summary>
        /// <param name="first">The first namespace to include.</param>
        /// <param name="namespaces">The namespaces to include.</param>
        /// <exception cref="ArgumentNullException">If the <paramref name="namespaces"/> argument is <c>null</c>.</exception>
        ICompiledImplementationTypeFilter InNamespaces(string first, params string[] namespaces);

        /// <summary>
        /// Will match all types outside of the same namespace as the type <typeparamref name="T"/>.
        /// </summary>
        ICompiledImplementationTypeFilter NotInNamespaceOf<T>();

        /// <summary>
        /// Will match all types outside of all of the namespaces of the <paramref name="types" /> specified.
        /// </summary>
        /// <param name="first">The first type in the namespaces to include.</param>
        /// <param name="types">The types in the namespaces to include.</param>
        /// <exception cref="ArgumentNullException">If the <paramref name="types"/> argument is <c>null</c>.</exception>
        ICompiledImplementationTypeFilter NotInNamespaceOf(Type first, params Type[] types);

        /// <summary>
        /// Will match all types outside of all of the <paramref name="namespaces"/> specified.
        /// </summary>
        /// <param name="first">The first namespace to include.</param>
        /// <param name="namespaces">The namespaces to include.</param>
        /// <exception cref="ArgumentNullException">If the <paramref name="namespaces"/> argument is <c>null</c>.</exception>
        ICompiledImplementationTypeFilter NotInNamespaces(string first, params string[] namespaces);

        /// <summary>
        /// Will match all types that has an attribute of type <typeparamref name="T"/> defined.
        /// </summary>
        /// <typeparam name="T">The type of attribute that needs to be defined.</typeparam>
        ICompiledImplementationTypeFilter WithAttribute<T>() where T : Attribute;

        /// <summary>
        /// Will match all types that has an attribute of <paramref name="attributeType" /> defined.
        /// </summary>
        /// <param name="attributeType">Type of the attribute.</param>
        /// <exception cref="ArgumentNullException">If the <paramref name="attributeType"/> argument is <c>null</c>.</exception>
        ICompiledImplementationTypeFilter WithAttribute(Type attributeType);

        /// <summary>
        /// Will match all types that doesn't have an attribute of type <typeparamref name="T"/> defined.
        /// </summary>
        /// <typeparam name="T">The type of attribute that needs to be defined.</typeparam>
        ICompiledImplementationTypeFilter WithoutAttribute<T>() where T : Attribute;

        /// <summary>
        /// Will match all types that doesn't have an attribute of <paramref name="attributeType" /> defined.
        /// </summary>
        /// <param name="attributeType">Type of the attribute.</param>
        /// <exception cref="ArgumentNullException">If the <paramref name="attributeType"/> argument is <c>null</c>.</exception>
        ICompiledImplementationTypeFilter WithoutAttribute(Type attributeType);
    }
}
