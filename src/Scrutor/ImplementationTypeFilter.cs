using System;
using System.Collections.Generic;
using System.Linq;

namespace Scrutor;

internal class ImplementationTypeFilter : IImplementationTypeFilter
{
    public ImplementationTypeFilter(IEnumerable<Type> types)
    {
        Types = types;
    }

    internal IEnumerable<Type> Types { get; private set; }

    public IImplementationTypeFilter AssignableTo<T>()
    {
        return AssignableTo(typeof(T));
    }

    public IImplementationTypeFilter AssignableTo(Type type)
    {
        Preconditions.NotNull(type, nameof(type));

        return AssignableToAny(type);
    }

    public IImplementationTypeFilter AssignableToAny(params Type[] types)
    {
        Preconditions.NotNull(types, nameof(types));

        return AssignableToAny(types.AsEnumerable());
    }

    public IImplementationTypeFilter AssignableToAny(IEnumerable<Type> types)
    {
        Preconditions.NotNull(types, nameof(types));

        return Where(t => types.Any(t.IsBasedOn));
    }

    public IImplementationTypeFilter WithAttribute<T>() where T : Attribute
    {
        return WithAttribute(typeof(T));
    }

    public IImplementationTypeFilter WithAttribute(Type attributeType)
    {
        Preconditions.NotNull(attributeType, nameof(attributeType));

        return Where(t => t.HasAttribute(attributeType));
    }

    public IImplementationTypeFilter WithAttribute<T>(Func<T, bool> predicate) where T : Attribute
    {
        Preconditions.NotNull(predicate, nameof(predicate));

        return Where(t => t.HasAttribute(predicate));
    }

    public IImplementationTypeFilter WithoutAttribute<T>() where T : Attribute
    {
        return WithoutAttribute(typeof(T));
    }

    public IImplementationTypeFilter WithoutAttribute(Type attributeType)
    {
        Preconditions.NotNull(attributeType, nameof(attributeType));

        return Where(t => !t.HasAttribute(attributeType));
    }

    public IImplementationTypeFilter WithoutAttribute<T>(Func<T, bool> predicate) where T : Attribute
    {
        Preconditions.NotNull(predicate, nameof(predicate));

        return Where(t => !t.HasAttribute(predicate));
    }

    public IImplementationTypeFilter InNamespaceOf<T>()
    {
        return InNamespaceOf(typeof(T));
    }

    public IImplementationTypeFilter InNamespaceOf(params Type[] types)
    {
        Preconditions.NotNull(types, nameof(types));

        return InNamespaces(types.Select(t => t.Namespace ?? string.Empty));
    }

    public IImplementationTypeFilter InNamespaces(params string[] namespaces)
    {
        Preconditions.NotNull(namespaces, nameof(namespaces));

        return InNamespaces(namespaces.AsEnumerable());
    }

    public IImplementationTypeFilter InExactNamespaceOf<T>()
    {
        return InExactNamespaceOf(typeof(T));
    }

    public IImplementationTypeFilter InExactNamespaceOf(params Type[] types)
    {
        Preconditions.NotNull(types, nameof(types));
        return Where(t => types.Any(x => t.IsInExactNamespace(x.Namespace ?? string.Empty)));
    }

    public IImplementationTypeFilter InExactNamespaces(params string[] namespaces)
    {
        Preconditions.NotNull(namespaces, nameof(namespaces));

        return Where(t => namespaces.Any(t.IsInExactNamespace));
    }

    public IImplementationTypeFilter InNamespaces(IEnumerable<string> namespaces)
    {
        Preconditions.NotNull(namespaces, nameof(namespaces));

        return Where(t => namespaces.Any(t.IsInNamespace));
    }

    public IImplementationTypeFilter NotInNamespaceOf<T>()
    {
        return NotInNamespaceOf(typeof(T));
    }

    public IImplementationTypeFilter NotInNamespaceOf(params Type[] types)
    {
        Preconditions.NotNull(types, nameof(types));

        return NotInNamespaces(types.Select(t => t.Namespace ?? string.Empty));
    }

    public IImplementationTypeFilter NotInNamespaces(params string[] namespaces)
    {
        Preconditions.NotNull(namespaces, nameof(namespaces));

        return NotInNamespaces(namespaces.AsEnumerable());
    }

    public IImplementationTypeFilter NotInNamespaces(IEnumerable<string> namespaces)
    {
        Preconditions.NotNull(namespaces, nameof(namespaces));

        return Where(t => namespaces.All(ns => !t.IsInNamespace(ns)));
    }

    public IImplementationTypeFilter Where(Func<Type, bool> predicate)
    {
        Preconditions.NotNull(predicate, nameof(predicate));

        Types = Types.Where(predicate);
        return this;
    }
}
