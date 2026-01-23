using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Scrutor.Generators.Utilities;

/// <summary>
/// Extension methods for working with Roslyn symbols.
/// </summary>
internal static class SymbolExtensions
{
    /// <summary>
    /// Gets the fully qualified name of a type symbol including global prefix.
    /// </summary>
    public static string GetFullyQualifiedName(this ITypeSymbol symbol)
    {
        return symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
    }

    /// <summary>
    /// Gets the fully qualified name without the global:: prefix.
    /// </summary>
    public static string GetFullyQualifiedNameWithoutGlobal(this ITypeSymbol symbol)
    {
        var name = symbol.GetFullyQualifiedName();
        return name.StartsWith("global::") ? name.Substring(8) : name;
    }

    /// <summary>
    /// Checks if a type is a non-abstract, non-static class.
    /// </summary>
    public static bool IsConcreteClass(this ITypeSymbol symbol)
    {
        return symbol is INamedTypeSymbol namedType &&
               namedType.TypeKind == TypeKind.Class &&
               !namedType.IsAbstract &&
               !namedType.IsStatic;
    }

    /// <summary>
    /// Gets all implemented interfaces for a type.
    /// </summary>
    public static IEnumerable<INamedTypeSymbol> GetAllInterfaces(this ITypeSymbol symbol)
    {
        return symbol.AllInterfaces;
    }

    /// <summary>
    /// Checks if the type is assignable to another type.
    /// </summary>
    public static bool IsAssignableTo(this ITypeSymbol symbol, ITypeSymbol targetType, Compilation compilation)
    {
        if (compilation is CSharpCompilation csharpCompilation)
        {
            var conversion = csharpCompilation.ClassifyConversion(symbol, targetType);
            return conversion.IsIdentity || conversion.IsImplicit;
        }

        // Fallback: check if types are the same or if target is in the type hierarchy
        return SymbolEqualityComparer.Default.Equals(symbol, targetType) ||
               symbol.AllInterfaces.Any(i => SymbolEqualityComparer.Default.Equals(i, targetType));
    }

    /// <summary>
    /// Gets the namespace of the type symbol.
    /// </summary>
    public static string? GetNamespace(this ITypeSymbol symbol)
    {
        var ns = symbol.ContainingNamespace;
        if (ns == null || ns.IsGlobalNamespace)
        {
            return null;
        }

        return ns.ToDisplayString();
    }

    /// <summary>
    /// Checks if a type has a specific attribute.
    /// </summary>
    public static bool HasAttribute(this ISymbol symbol, string attributeFullName)
    {
        return symbol.GetAttributes().Any(a =>
            a.AttributeClass?.GetFullyQualifiedNameWithoutGlobal() == attributeFullName);
    }

    /// <summary>
    /// Gets an attribute by its full name.
    /// </summary>
    public static AttributeData? GetAttribute(this ISymbol symbol, string attributeFullName)
    {
        return symbol.GetAttributes().FirstOrDefault(a =>
            a.AttributeClass?.GetFullyQualifiedNameWithoutGlobal() == attributeFullName);
    }

    /// <summary>
    /// Gets all attributes matching a full name (for AllowMultiple attributes).
    /// </summary>
    public static IEnumerable<AttributeData> GetAttributes(this ISymbol symbol, string attributeFullName)
    {
        return symbol.GetAttributes().Where(a =>
            a.AttributeClass?.GetFullyQualifiedNameWithoutGlobal() == attributeFullName ||
            IsGenericAttributeOf(a.AttributeClass, attributeFullName));
    }

    private static bool IsGenericAttributeOf(INamedTypeSymbol? attributeClass, string baseName)
    {
        if (attributeClass == null) return false;

        // Check for ServiceDescriptorAttribute<T> which inherits from ServiceDescriptorAttribute
        var baseType = attributeClass.BaseType;
        while (baseType != null)
        {
            if (baseType.GetFullyQualifiedNameWithoutGlobal() == baseName)
            {
                return true;
            }
            baseType = baseType.BaseType;
        }

        return false;
    }

    /// <summary>
    /// Checks if a named type symbol represents a generic attribute (e.g., ServiceDescriptorAttribute&lt;T&gt;).
    /// </summary>
    public static bool IsGenericAttribute(this INamedTypeSymbol? symbol, string baseName)
    {
        if (symbol == null) return false;

        // Check if this is a constructed generic type
        if (symbol.IsGenericType && symbol.OriginalDefinition != null)
        {
            var originalName = symbol.OriginalDefinition.GetFullyQualifiedNameWithoutGlobal();
            // ServiceDescriptorAttribute<T> -> check if base name matches
            if (originalName.StartsWith(baseName.TrimEnd('>').Split('<')[0]))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Gets the type argument from a generic attribute.
    /// </summary>
    public static ITypeSymbol? GetGenericAttributeTypeArgument(this INamedTypeSymbol symbol)
    {
        if (symbol.TypeArguments.Length > 0)
        {
            return symbol.TypeArguments[0];
        }
        return null;
    }

    /// <summary>
    /// Gets the matching interface following the I{ClassName} convention.
    /// </summary>
    public static INamedTypeSymbol? FindMatchingInterface(this ITypeSymbol symbol)
    {
        var className = symbol.Name;
        var expectedInterfaceName = $"I{className}";

        return symbol.AllInterfaces.FirstOrDefault(i => i.Name == expectedInterfaceName);
    }

    /// <summary>
    /// Gets the fully qualified name in open generic format (e.g., global::IRepository&lt;&gt;).
    /// </summary>
    public static string GetOpenGenericName(this INamedTypeSymbol symbol)
    {
        if (!symbol.IsGenericType)
        {
            return symbol.GetFullyQualifiedName();
        }

        // Get the original definition (unbound generic)
        var definition = symbol.OriginalDefinition;
        var name = definition.ToDisplayString(new SymbolDisplayFormat(
            globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Included,
            typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
            genericsOptions: SymbolDisplayGenericsOptions.None));

        // Add the generic arity
        var arity = symbol.TypeParameters.Length > 0 ? symbol.TypeParameters.Length : symbol.TypeArguments.Length;
        if (arity > 0)
        {
            name += "<" + new string(',', arity - 1) + ">";
        }

        return name;
    }
}
