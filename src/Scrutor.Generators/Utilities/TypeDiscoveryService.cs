using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Scrutor.Generators.Models;

namespace Scrutor.Generators.Utilities;

/// <summary>
/// Discovers types in the compilation that match the scan configuration.
/// </summary>
internal static class TypeDiscoveryService
{
    /// <summary>
    /// Discovers service registrations based on the scan configuration.
    /// </summary>
    public static List<ServiceRegistration> DiscoverRegistrations(
        ScanConfiguration config,
        Compilation compilation)
    {
        var registrations = new List<ServiceRegistration>();

        if (config.HasUnsupportedPatterns || config.UsesAttributes)
        {
            return registrations;
        }

        // Get all types from the specified assemblies
        var candidateTypes = GetCandidateTypes(config, compilation);

        // Apply type filters
        var filteredTypes = ApplyTypeFilters(candidateTypes, config, compilation);

        // Generate registrations based on service type mappings
        foreach (var implType in filteredTypes)
        {
            var isOpenGeneric = implType.IsGenericType && implType.TypeParameters.Length > 0;
            var serviceTypes = GetServiceTypesWithForwarding(implType, config, compilation, isOpenGeneric);

            var implTypeName = isOpenGeneric
                ? implType.GetOpenGenericName()
                : implType.GetFullyQualifiedName();

            foreach (var (serviceType, forwardsToImpl) in serviceTypes)
            {
                registrations.Add(new ServiceRegistration
                {
                    ServiceTypeFullName = serviceType,
                    ImplementationTypeFullName = implTypeName,
                    Lifetime = config.Lifetime,
                    Strategy = config.Strategy,
                    ServiceKey = config.ServiceKey,
                    ForwardsToImplementation = forwardsToImpl,
                    IsOpenGeneric = isOpenGeneric
                });
            }
        }

        return registrations;
    }

    private static IEnumerable<INamedTypeSymbol> GetCandidateTypes(
        ScanConfiguration config,
        Compilation compilation)
    {
        var assemblies = new HashSet<IAssemblySymbol>(SymbolEqualityComparer.Default);

        // If no assembly sources specified, use the compilation's assembly
        if (config.AssemblySourceTypes.Count == 0)
        {
            assemblies.Add(compilation.Assembly);
        }
        else
        {
            // Find assemblies containing the specified types
            foreach (var typeFullName in config.AssemblySourceTypes)
            {
                var typeName = typeFullName.TrimStart("global::".ToCharArray());
                var typeSymbol = GetTypeByName(typeName, compilation);
                if (typeSymbol?.ContainingAssembly != null)
                {
                    assemblies.Add(typeSymbol.ContainingAssembly);
                }
            }
        }

        // Collect all named types from the assemblies
        foreach (var assembly in assemblies)
        {
            foreach (var type in GetAllTypes(assembly.GlobalNamespace))
            {
                // Skip abstract classes, interfaces, and non-class types
                if (type.IsAbstract || type.TypeKind != TypeKind.Class)
                {
                    continue;
                }

                // Check accessibility
                if (!config.IncludeNonPublic && type.DeclaredAccessibility != Accessibility.Public)
                {
                    continue;
                }

                yield return type;
            }
        }
    }

    private static IEnumerable<INamedTypeSymbol> GetAllTypes(INamespaceSymbol ns)
    {
        foreach (var type in ns.GetTypeMembers())
        {
            yield return type;

            // Include nested types
            foreach (var nested in GetNestedTypes(type))
            {
                yield return nested;
            }
        }

        foreach (var childNs in ns.GetNamespaceMembers())
        {
            foreach (var type in GetAllTypes(childNs))
            {
                yield return type;
            }
        }
    }

    private static IEnumerable<INamedTypeSymbol> GetNestedTypes(INamedTypeSymbol type)
    {
        foreach (var nested in type.GetTypeMembers())
        {
            yield return nested;

            foreach (var deepNested in GetNestedTypes(nested))
            {
                yield return deepNested;
            }
        }
    }

    private static IEnumerable<INamedTypeSymbol> ApplyTypeFilters(
        IEnumerable<INamedTypeSymbol> types,
        ScanConfiguration config,
        Compilation compilation)
    {
        var result = types;

        foreach (var filter in config.TypeFilters)
        {
            result = filter.Kind switch
            {
                TypeFilterKind.AssignableTo => FilterAssignableTo(result, filter.TypeFullName, compilation),
                TypeFilterKind.InNamespace => FilterInNamespace(result, filter.Namespace, prefixMatch: true),
                TypeFilterKind.InExactNamespace => FilterInNamespace(result, filter.Namespace, prefixMatch: false),
                TypeFilterKind.WithAttribute => FilterWithAttribute(result, filter.TypeFullName, compilation),
                TypeFilterKind.WithoutAttribute => FilterWithoutAttribute(result, filter.TypeFullName, compilation),
                _ => result
            };
        }

        return result;
    }

    private static IEnumerable<INamedTypeSymbol> FilterAssignableTo(
        IEnumerable<INamedTypeSymbol> types,
        string? baseTypeFullName,
        Compilation compilation)
    {
        if (string.IsNullOrEmpty(baseTypeFullName))
        {
            return types;
        }

        var typeName = baseTypeFullName!.TrimStart("global::".ToCharArray());
        var baseType = GetTypeByName(typeName, compilation);
        if (baseType == null)
        {
            return types;
        }

        return types.Where(t => IsAssignableTo(t, baseType));
    }

    private static string ConvertToMetadataName(string displayName)
    {
        var name = displayName;

        // Handle generic types: "IRepository<T>" or "IRepository<T, U>" -> "IRepository`1" or "IRepository`2"
        var genericStart = name.IndexOf('<');
        if (genericStart >= 0)
        {
            var baseName = name.Substring(0, genericStart);
            var genericPart = name.Substring(genericStart);

            // Count type parameters by counting commas + 1
            var arity = 1 + genericPart.Count(c => c == ',');

            name = $"{baseName}`{arity}";
        }

        return name;
    }

    /// <summary>
    /// Tries multiple metadata name formats to find a type.
    /// Handles nested classes (Outer.Nested -> Outer+Nested) and generics.
    /// </summary>
    private static INamedTypeSymbol? GetTypeByName(string displayName, Compilation compilation)
    {
        var metadataName = ConvertToMetadataName(displayName);

        // Try the converted name first
        var type = compilation.GetTypeByMetadataName(metadataName);
        if (type != null) return type;

        // Try original name
        type = compilation.GetTypeByMetadataName(displayName);
        if (type != null) return type;

        // Try converting dots to plus for nested types (from right to left)
        var name = metadataName;
        var lastDot = name.LastIndexOf('.');
        while (lastDot > 0)
        {
            // Replace the last dot with a plus (nested type separator)
            name = name.Substring(0, lastDot) + "+" + name.Substring(lastDot + 1);
            type = compilation.GetTypeByMetadataName(name);
            if (type != null) return type;

            lastDot = name.LastIndexOf('.');
        }

        return null;
    }

    private static bool IsAssignableTo(INamedTypeSymbol type, INamedTypeSymbol baseType)
    {
        // Check if it's the same type
        if (SymbolEqualityComparer.Default.Equals(type, baseType))
        {
            return true;
        }

        // Check interfaces
        if (baseType.TypeKind == TypeKind.Interface)
        {
            return type.AllInterfaces.Any(i => SymbolEqualityComparer.Default.Equals(i, baseType) ||
                (baseType.IsGenericType && i.IsGenericType &&
                 SymbolEqualityComparer.Default.Equals(i.OriginalDefinition, baseType.OriginalDefinition)));
        }

        // Check base types
        var current = type.BaseType;
        while (current != null)
        {
            if (SymbolEqualityComparer.Default.Equals(current, baseType) ||
                (baseType.IsGenericType && current.IsGenericType &&
                 SymbolEqualityComparer.Default.Equals(current.OriginalDefinition, baseType.OriginalDefinition)))
            {
                return true;
            }
            current = current.BaseType;
        }

        return false;
    }

    private static IEnumerable<INamedTypeSymbol> FilterInNamespace(
        IEnumerable<INamedTypeSymbol> types,
        string? ns,
        bool prefixMatch)
    {
        if (string.IsNullOrEmpty(ns))
        {
            return types;
        }

        return types.Where(t =>
        {
            var typeNs = t.ContainingNamespace?.ToDisplayString() ?? "";
            if (prefixMatch)
            {
                return typeNs.StartsWith(ns!) || typeNs == ns;
            }
            return typeNs == ns;
        });
    }

    private static IEnumerable<INamedTypeSymbol> FilterWithAttribute(
        IEnumerable<INamedTypeSymbol> types,
        string? attributeTypeFullName,
        Compilation compilation)
    {
        if (string.IsNullOrEmpty(attributeTypeFullName))
        {
            return types;
        }

        var typeName = attributeTypeFullName!.TrimStart("global::".ToCharArray());
        var attributeType = compilation.GetTypeByMetadataName(typeName);
        if (attributeType == null)
        {
            return types;
        }

        return types.Where(t => t.GetAttributes().Any(a =>
            SymbolEqualityComparer.Default.Equals(a.AttributeClass, attributeType) ||
            (attributeType.IsGenericType && a.AttributeClass?.IsGenericType == true &&
             SymbolEqualityComparer.Default.Equals(a.AttributeClass.OriginalDefinition, attributeType.OriginalDefinition))));
    }

    private static IEnumerable<INamedTypeSymbol> FilterWithoutAttribute(
        IEnumerable<INamedTypeSymbol> types,
        string? attributeTypeFullName,
        Compilation compilation)
    {
        if (string.IsNullOrEmpty(attributeTypeFullName))
        {
            return types;
        }

        var typeName = attributeTypeFullName!.TrimStart("global::".ToCharArray());
        var attributeType = compilation.GetTypeByMetadataName(typeName);
        if (attributeType == null)
        {
            return types;
        }

        return types.Where(t => !t.GetAttributes().Any(a =>
            SymbolEqualityComparer.Default.Equals(a.AttributeClass, attributeType)));
    }

    private static IEnumerable<(string serviceType, bool forwardsToImpl)> GetServiceTypesWithForwarding(
        INamedTypeSymbol implType,
        ScanConfiguration config,
        Compilation compilation,
        bool isOpenGeneric)
    {
        var serviceTypes = new List<(string serviceType, bool forwardsToImpl)>();

        string GetTypeName(INamedTypeSymbol type) =>
            isOpenGeneric ? type.GetOpenGenericName() : type.GetFullyQualifiedName();

        // If no mappings specified, default to AsSelf
        if (config.ServiceTypeMappings.Count == 0)
        {
            serviceTypes.Add((GetTypeName(implType), false));
            return serviceTypes;
        }

        foreach (var mapping in config.ServiceTypeMappings)
        {
            switch (mapping.Kind)
            {
                case ServiceTypeMappingKind.AsSelf:
                    serviceTypes.Add((GetTypeName(implType), false));
                    break;

                case ServiceTypeMappingKind.AsImplementedInterfaces:
                    foreach (var iface in implType.Interfaces)
                    {
                        // Skip system interfaces
                        var ifaceNs = iface.ContainingNamespace?.ToDisplayString() ?? "";
                        if (ifaceNs.StartsWith("System"))
                        {
                            continue;
                        }
                        serviceTypes.Add((GetTypeName(iface), false));
                    }
                    break;

                case ServiceTypeMappingKind.AsMatchingInterface:
                    var matchingInterface = FindMatchingInterface(implType);
                    if (matchingInterface != null)
                    {
                        serviceTypes.Add((GetTypeName(matchingInterface), false));
                    }
                    break;

                case ServiceTypeMappingKind.AsExplicitType:
                    if (!string.IsNullOrEmpty(mapping.ExplicitTypeFullName))
                    {
                        serviceTypes.Add((mapping.ExplicitTypeFullName!, false));
                    }
                    break;

                case ServiceTypeMappingKind.AsSelfWithInterfaces:
                    // First, register as self (direct registration)
                    serviceTypes.Add((GetTypeName(implType), false));

                    // Then, register interfaces with forwarding to self
                    // Note: For open generics, forwarding via factory is not supported
                    if (!isOpenGeneric)
                    {
                        foreach (var iface in implType.Interfaces)
                        {
                            // Skip system interfaces
                            var ifaceNs = iface.ContainingNamespace?.ToDisplayString() ?? "";
                            if (ifaceNs.StartsWith("System"))
                            {
                                continue;
                            }
                            serviceTypes.Add((GetTypeName(iface), true));
                        }
                    }
                    break;
            }
        }

        return serviceTypes.Distinct();
    }

    private static INamedTypeSymbol? FindMatchingInterface(INamedTypeSymbol type)
    {
        // Look for interface with matching name (e.g., MyService -> IMyService)
        var expectedName = "I" + type.Name;
        return type.Interfaces.FirstOrDefault(i => i.Name == expectedName);
    }
}
