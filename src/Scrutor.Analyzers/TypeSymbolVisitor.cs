using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Scrutor.Analyzers
{
    class TypeSymbolVisitor : SymbolVisitor
    {
        public static ImmutableArray<INamedTypeSymbol> GetTypes(CSharpCompilation compilation)
        {
            var visitor = new TypeSymbolVisitor();
            visitor.VisitNamespace(compilation.GlobalNamespace);
            foreach (var symbol in compilation.References.Select(compilation.GetAssemblyOrModuleSymbol).Where(z => z != null))
                symbol?.Accept(visitor);
            return visitor.GetTypes();
        }

        public static ImmutableArray<INamedTypeSymbol> GetTypes(CSharpCompilation compilation, IEnumerable<ISymbol?> symbols)
        {
            var visitor = new TypeSymbolVisitor();
            visitor.Accept(symbols);
            return visitor.GetTypes();
        }

        private readonly List<INamedTypeSymbol> _types = new List<INamedTypeSymbol>();

        private void Accept<T>(IEnumerable<T> members)
            where T : ISymbol?
        {
            foreach (var member in members)
                member?.Accept(this);
        }

        public override void VisitNamespace(INamespaceSymbol symbol)
        {
            Accept(symbol.GetMembers());
        }

        public override void VisitAssembly(IAssemblySymbol symbol)
        {
            symbol.GlobalNamespace.Accept(this);
        }

        public override void VisitNamedType(INamedTypeSymbol symbol)
        {
            if (symbol.TypeKind == TypeKind.Class || symbol.TypeKind == TypeKind.Delegate || symbol.TypeKind == TypeKind.Struct)
            {
                if (symbol.IsAbstract || !symbol.CanBeReferencedByName) return;
                _types.Add(symbol);
            }
            Accept(symbol.GetMembers());
        }

        public ImmutableArray<INamedTypeSymbol> GetTypes() => _types.Distinct(SymbolEqualityComparer.Default).OfType<INamedTypeSymbol>().ToImmutableArray();
    }
}
