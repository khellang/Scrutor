using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;

namespace Scrutor.Analyzers
{
    public static class Helpers
    {
        public static string GetFullMetadataName(ISymbol? symbol)
        {
            if (symbol == null || IsRootNamespace(symbol))
            {
                return string.Empty;
            }

            var sb = new StringBuilder(symbol.MetadataName);

            var last = symbol;

            var workingSymbol = symbol.ContainingSymbol;

            while (!IsRootNamespace(workingSymbol))
            {
                if (workingSymbol is ITypeSymbol && last is ITypeSymbol)
                {
                    sb.Insert(0, '+');
                }
                else
                {
                    sb.Insert(0, '.');
                }

                sb.Insert(0, workingSymbol.OriginalDefinition.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat).Trim());
                //sb.Insert(0, symbol.MetadataName);
                workingSymbol = workingSymbol.ContainingSymbol;
            }

            return sb.ToString();

            static bool IsRootNamespace(ISymbol symbol)
            {
                INamespaceSymbol? s = null;
                return (s = symbol as INamespaceSymbol) != null && s.IsGlobalNamespace;
            }
        }

        public static string GetGenericDisplayName(ISymbol? symbol)
        {
            if (symbol == null || IsRootNamespace(symbol))
            {
                return string.Empty;
            }

            var sb = new StringBuilder(symbol.MetadataName);
            if (symbol is INamedTypeSymbol namedTypeSymbol && (namedTypeSymbol.IsUnboundGenericType || namedTypeSymbol.IsGenericType && namedTypeSymbol.TypeArguments.All(z => z is ITypeParameterSymbol)))
            {
                sb = new StringBuilder(symbol.Name);
                sb.Append("<");
                for (var i = 1; i < namedTypeSymbol.Arity-1; i++)
                    sb.Append(",");
                sb.Append(">");
            }

            var last = symbol;

            var workingSymbol = symbol.ContainingSymbol;

            while (!IsRootNamespace(workingSymbol))
            {
                if (workingSymbol is ITypeSymbol && last is ITypeSymbol)
                {
                    sb.Insert(0, '+');
                }
                else
                {
                    sb.Insert(0, '.');
                }

                sb.Insert(0, workingSymbol.OriginalDefinition.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat).Trim());
                //sb.Insert(0, symbol.MetadataName);
                workingSymbol = workingSymbol.ContainingSymbol;
            }

            return sb.ToString();

            static bool IsRootNamespace(ISymbol symbol)
            {
                INamespaceSymbol? s = null;
                return (s = symbol as INamespaceSymbol) != null && s.IsGlobalNamespace;
            }
        }
    }
}
