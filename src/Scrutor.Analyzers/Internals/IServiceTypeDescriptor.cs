using System;
using Microsoft.CodeAnalysis;

namespace Scrutor.Analyzers.Internals
{
    interface IServiceTypeDescriptor { }

    struct SelfServiceTypeDescriptor : IServiceTypeDescriptor { }

    struct ImplementedInterfacesServiceTypeDescriptor : IServiceTypeDescriptor { }

    struct MatchingInterfaceServiceTypeDescriptor : IServiceTypeDescriptor { }
    struct UsingAttributeServiceTypeDescriptor : IServiceTypeDescriptor { }
    struct CompiledServiceTypeDescriptor : IServiceTypeDescriptor
    {
        public INamedTypeSymbol Type { get; }

        public CompiledServiceTypeDescriptor(INamedTypeSymbol type) => Type = type;
    }
}
