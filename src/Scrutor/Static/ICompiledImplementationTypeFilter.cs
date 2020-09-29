using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace Scrutor.Static
{
    public interface ICompiledImplementationTypeFilter
    {
        ICompiledImplementationTypeFilter AssignableTo<T>();
        ICompiledImplementationTypeFilter AssignableTo(Type type);
        ICompiledImplementationTypeFilter AssignableToAny(Type first, params Type[] types);
        ICompiledImplementationTypeFilter InExactNamespaceOf<T>();
        ICompiledImplementationTypeFilter InExactNamespaceOf(Type first);
        ICompiledImplementationTypeFilter InExactNamespaces(string first, params string[] namespaces);
        ICompiledImplementationTypeFilter InNamespaceOf<T>();
        ICompiledImplementationTypeFilter InNamespaceOf(Type first);
        ICompiledImplementationTypeFilter InNamespaces(string first, params string[] namespaces);
        ICompiledImplementationTypeFilter NotInNamespaceOf<T>();
        ICompiledImplementationTypeFilter NotInNamespaceOf(Type first);
        ICompiledImplementationTypeFilter NotInNamespaces(string first, params string[] namespaces);
        ICompiledImplementationTypeFilter WithAttribute<T>();
        ICompiledImplementationTypeFilter WithoutAttribute<T>();
    }
}
