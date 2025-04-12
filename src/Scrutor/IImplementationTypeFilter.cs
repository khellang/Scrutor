using System;
using System.Collections.Generic;

namespace Scrutor;

public interface IImplementationTypeFilter : IFluentInterface
{
    /// <summary>
    /// Will match all types that are assignable to <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The type that should be assignable from the matching types.</typeparam>
    IImplementationTypeFilter AssignableTo<T>();

    /// <summary>
    /// Will match all types that are assignable to the specified <paramref name="type" />.
    /// </summary>
    /// <param name="type">The type that should be assignable from the matching types.</param>
    /// <exception cref="ArgumentNullException">If the <paramref name="type"/> argument is <c>null</c>.</exception>
    IImplementationTypeFilter AssignableTo(Type type);

    /// <summary>
    /// Will match all types that are assignable to any of the specified <paramref name="types" />.
    /// </summary>
    /// <param name="types">The types that should be assignable from the matching types.</param>
    /// <exception cref="ArgumentNullException">If the <paramref name="types"/> argument is <c>null</c>.</exception>
    IImplementationTypeFilter AssignableToAny(params Type[] types);

    /// <summary>
    /// Will match all types that are assignable to any of the specified <paramref name="types" />.
    /// </summary>
    /// <param name="types">The types that should be assignable from the matching types.</param>
    /// <exception cref="ArgumentNullException">If the <paramref name="types"/> argument is <c>null</c>.</exception>
    IImplementationTypeFilter AssignableToAny(IEnumerable<Type> types);

    /// <summary>
    /// Will match all types that has an attribute of type <typeparamref name="T"/> defined.
    /// </summary>
    /// <typeparam name="T">The type of attribute that needs to be defined.</typeparam>
    IImplementationTypeFilter WithAttribute<T>() where T : Attribute;

    /// <summary>
    /// Will match all types that has an attribute of <paramref name="attributeType" /> defined.
    /// </summary>
    /// <param name="attributeType">Type of the attribute.</param>
    /// <exception cref="ArgumentNullException">If the <paramref name="attributeType"/> argument is <c>null</c>.</exception>
    IImplementationTypeFilter WithAttribute(Type attributeType);

    /// <summary>
    /// Will match all types that has an attribute of type <typeparamref name="T"/> defined,
    /// and where the attribute itself matches the <paramref name="predicate"/>.
    /// </summary>
    /// <typeparam name="T">The type of attribute that needs to be defined.</typeparam>
    /// <param name="predicate">The predicate to match the attribute.</param>
    /// <exception cref="ArgumentNullException">If the <paramref name="predicate"/> argument is <c>null</c>.</exception>
    IImplementationTypeFilter WithAttribute<T>(Func<T, bool> predicate) where T : Attribute;

    /// <summary>
    /// Will match all types that doesn't have an attribute of type <typeparamref name="T"/> defined.
    /// </summary>
    /// <typeparam name="T">The type of attribute that needs to be defined.</typeparam>
    IImplementationTypeFilter WithoutAttribute<T>() where T : Attribute;

    /// <summary>
    /// Will match all types that doesn't have an attribute of <paramref name="attributeType" /> defined.
    /// </summary>
    /// <param name="attributeType">Type of the attribute.</param>
    /// <exception cref="ArgumentNullException">If the <paramref name="attributeType"/> argument is <c>null</c>.</exception>
    IImplementationTypeFilter WithoutAttribute(Type attributeType);

    /// <summary>
    /// Will match all types that doesn't have an attribute of type <typeparamref name="T"/> defined,
    /// and where the attribute itself matches the <paramref name="predicate"/>.
    /// </summary>
    /// <typeparam name="T">The type of attribute that needs to be defined.</typeparam>
    /// <param name="predicate">The predicate to match the attribute.</param>
    /// <exception cref="ArgumentNullException">If the <paramref name="predicate"/> argument is <c>null</c>.</exception>
    IImplementationTypeFilter WithoutAttribute<T>(Func<T, bool> predicate) where T : Attribute;

    /// <summary>
    /// Will match all types in the same namespace as the type <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">A type inside the namespace to include.</typeparam>
    IImplementationTypeFilter InNamespaceOf<T>();

    /// <summary>
    /// Will match all types in any of the namespaces of the <paramref name="types" /> specified.
    /// </summary>
    /// <param name="types">The types in the namespaces to include.</param>
    /// <exception cref="ArgumentNullException">If the <paramref name="types"/> argument is <c>null</c>.</exception>
    IImplementationTypeFilter InNamespaceOf(params Type[] types);

    /// <summary>
    /// Will match all types in any of the <paramref name="namespaces"/> specified.
    /// </summary>
    /// <param name="namespaces">The namespaces to include.</param>
    /// <exception cref="ArgumentNullException">If the <paramref name="namespaces"/> argument is <c>null</c>.</exception>
    IImplementationTypeFilter InNamespaces(params string[] namespaces);

    /// <summary>
    /// Will match all types in the exact same namespace as the type <typeparamref name="T"/>
    /// </summary>
    /// <typeparam name="T">The type in the namespace to include</typeparam>
    IImplementationTypeFilter InExactNamespaceOf<T>();

    /// <summary>
    /// Will match all types in the exact same namespace as the type <paramref name="types"/>
    /// </summary>
    /// <param name="types">The types in the namespaces to include.</param>
    /// <exception cref="ArgumentNullException">If the <paramref name="types"/> argument is <c>null</c>.</exception>
    IImplementationTypeFilter InExactNamespaceOf(params Type[] types);

    /// <summary>
    /// Will match all types in the exact same namespace as the type <paramref name="namespaces"/>
    /// </summary>
    /// <param name="namespaces">The namespace to include.</param>
    /// <exception cref="ArgumentNullException">If the <paramref name="namespaces"/> argument is <c>null</c>.</exception>
    IImplementationTypeFilter InExactNamespaces(params string[] namespaces);

    /// <summary>
    /// Will match all types in any of the <paramref name="namespaces"/> specified.
    /// </summary>
    /// <param name="namespaces">The namespaces to include.</param>
    /// <exception cref="ArgumentNullException">If the <paramref name="namespaces"/> argument is <c>null</c>.</exception>
    IImplementationTypeFilter InNamespaces(IEnumerable<string> namespaces);

    /// <summary>
    /// Will match all types outside of the same namespace as the type <typeparamref name="T"/>.
    /// </summary>
    IImplementationTypeFilter NotInNamespaceOf<T>();

    /// <summary>
    /// Will match all types outside of all of the namespaces of the <paramref name="types" /> specified.
    /// </summary>
    /// <param name="types">The types in the namespaces to include.</param>
    /// <exception cref="ArgumentNullException">If the <paramref name="types"/> argument is <c>null</c>.</exception>
    IImplementationTypeFilter NotInNamespaceOf(params Type[] types);

    /// <summary>
    /// Will match all types outside of all of the <paramref name="namespaces"/> specified.
    /// </summary>
    /// <param name="namespaces">The namespaces to include.</param>
    /// <exception cref="ArgumentNullException">If the <paramref name="namespaces"/> argument is <c>null</c>.</exception>
    IImplementationTypeFilter NotInNamespaces(params string[] namespaces);

    /// <summary>
    /// Will match all types outside of all of the <paramref name="namespaces"/> specified.
    /// </summary>
    /// <param name="namespaces">The namespaces to include.</param>
    /// <exception cref="ArgumentNullException">If the <paramref name="namespaces"/> argument is <c>null</c>.</exception>
    IImplementationTypeFilter NotInNamespaces(IEnumerable<string> namespaces);

    /// <summary>
    /// Will match types based on the specified <paramref name="predicate"/>.
    /// </summary>
    /// <param name="predicate">The predicate to match types.</param>
    /// <exception cref="ArgumentNullException">If the <paramref name="predicate" /> argument is <c>null</c>.</exception>
    IImplementationTypeFilter Where(Func<Type, bool> predicate);
}
