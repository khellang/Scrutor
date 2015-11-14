using System;
using System.Collections.Generic;

namespace Microsoft.Extensions.DependencyInjection.Scanning
{
    public interface IImplementationTypeFilter : IFluentInterface
    {
        IImplementationTypeFilter AssignableTo<T>();

        IImplementationTypeFilter AssignableTo(Type type);

        IImplementationTypeFilter AssignableToAny(params Type[] types);

        IImplementationTypeFilter AssignableToAny(IEnumerable<Type> types);

        IImplementationTypeFilter WithAttribute<T>() where T : Attribute;

        IImplementationTypeFilter WithAttribute(Type attributeType);

        IImplementationTypeFilter WithAttribute<T>(Func<T, bool> predicate) where T : Attribute;

        IImplementationTypeFilter WithoutAttribute<T>() where T : Attribute;

        IImplementationTypeFilter WithoutAttribute(Type attributeType);

        IImplementationTypeFilter WithoutAttribute<T>(Func<T, bool> predicate) where T : Attribute;

        IImplementationTypeFilter InNamespaceOf<T>();

        IImplementationTypeFilter InNamespaceOf(params Type[] types);

        IImplementationTypeFilter InNamespaces(params string[] namespaces);

        IImplementationTypeFilter InNamespaces(IEnumerable<string> namespaces);

        IImplementationTypeFilter NotInNamespaceOf<T>();

        IImplementationTypeFilter NotInNamespaceOf(params Type[] types);

        IImplementationTypeFilter NotInNamespaces(params string[] namespaces);

        IImplementationTypeFilter NotInNamespaces(IEnumerable<string> namespaces);

        IImplementationTypeFilter Where(Func<Type, bool> predicate);
    }
}