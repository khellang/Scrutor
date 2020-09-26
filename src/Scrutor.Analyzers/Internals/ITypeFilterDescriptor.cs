using System;
using Microsoft.CodeAnalysis;

namespace Scrutor.Analyzers.Internals
{
    interface ITypeFilterDescriptor
    {
    }

    struct NamespaceFilterDescriptor : ITypeFilterDescriptor
    {
        public string Namespace { get; }
        public NamespaceFilter Filter { get; }

        public NamespaceFilterDescriptor(NamespaceFilter filter, string @namespace)
        {
            Filter = filter;
            Namespace = @namespace;
        }
    }

    struct AttributeFilterDescriptor : ITypeFilterDescriptor
    {
        public Type Attribute { get; }

        public AttributeFilterDescriptor(Type attribute) => Attribute = attribute;
    }
    interface ICompiledTypeFilterDescriptor : ITypeFilterDescriptor
    {
        INamedTypeSymbol Type { get; }
    }
    struct CompiledAttributeFilterDescriptor : ITypeFilterDescriptor
    {
        public INamedTypeSymbol Attribute { get; }

        public CompiledAttributeFilterDescriptor(INamedTypeSymbol attribute) => Attribute = attribute;
    }
    struct CompiledAssignableToTypeFilterDescriptor : ICompiledTypeFilterDescriptor
    {
        public INamedTypeSymbol Type { get; }

        public CompiledAssignableToTypeFilterDescriptor(INamedTypeSymbol type) => Type = type;
    }
    struct CompiledAssignableToAnyTypeFilterDescriptor : ICompiledTypeFilterDescriptor
    {
        public INamedTypeSymbol Type { get; }

        public CompiledAssignableToAnyTypeFilterDescriptor(INamedTypeSymbol type) => Type = type;
    }
}
