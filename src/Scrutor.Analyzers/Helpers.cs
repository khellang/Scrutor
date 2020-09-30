using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

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
            if (symbol is INamedTypeSymbol namedTypeSymbol && (namedTypeSymbol.IsOpenGenericType() || namedTypeSymbol.IsGenericType))
            {
                sb = new StringBuilder(symbol.Name);
                if (namedTypeSymbol.IsOpenGenericType())
                {
                    sb.Append("<");
                    for (var i = 1; i < namedTypeSymbol.Arity - 1; i++)
                        sb.Append(",");
                    sb.Append(">");
                }
                else
                {
                    sb.Append("<");
                    for (var index = 0; index < namedTypeSymbol.TypeArguments.Length; index++)
                    {
                        var argument = namedTypeSymbol.TypeArguments[index];
                        sb.Append(GetGenericDisplayName(argument));
                        if (index < namedTypeSymbol.TypeArguments.Length -1)
                        sb.Append(",");
                    }

                    sb.Append(">");
                }
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

        private static Regex SpecialCharacterRemover = new Regex("[^\\w\\d]", RegexOptions.Compiled);
        public static string AssemblyVariableName(IAssemblySymbol symbol) => SpecialCharacterRemover.Replace(symbol.Identity.GetDisplayName(true), "");

        public static IEnumerable<INamedTypeSymbol> GetBaseTypes(CSharpCompilation compilation, INamedTypeSymbol namedTypeSymbol)
        {
            while (namedTypeSymbol.BaseType != null)
            {
                if (SymbolEqualityComparer.Default.Equals(namedTypeSymbol.BaseType, compilation.ObjectType)) yield break;
                yield return namedTypeSymbol.BaseType;
                namedTypeSymbol = namedTypeSymbol.BaseType;
            }
        }

        public static TypeSyntax? ExtractSyntaxFromMethod(
            InvocationExpressionSyntax expression,
            NameSyntax name
        )
        {
            if (name is GenericNameSyntax genericNameSyntax)
            {
                if (genericNameSyntax.TypeArgumentList.Arguments.Count == 1)
                {
                    return genericNameSyntax.TypeArgumentList.Arguments[0];
                }
            }

            if (name is SimpleNameSyntax)
            {
                if (expression.ArgumentList.Arguments.Count == 1 && expression.ArgumentList.Arguments[0].Expression is TypeOfExpressionSyntax typeOfExpression)
                {
                    return typeOfExpression.Type;
                }
            }

            return null;
        }
    }
}
