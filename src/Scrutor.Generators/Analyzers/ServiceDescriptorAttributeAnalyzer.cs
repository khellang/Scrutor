using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Scrutor.Generators.Models;
using Scrutor.Generators.Utilities;

namespace Scrutor.Generators.Analyzers;

/// <summary>
/// Analyzes [ServiceDescriptor] and [ServiceDescriptor&lt;T&gt;] attributes on classes.
/// </summary>
internal static class ServiceDescriptorAttributeAnalyzer
{
    private const string ServiceDescriptorAttributeName = "Scrutor.ServiceDescriptorAttribute";
    private const string GenericServiceDescriptorAttributeName = "Scrutor.ServiceDescriptorAttribute`1";

    /// <summary>
    /// Analyzes a class with [ServiceDescriptor] attribute(s).
    /// </summary>
    public static IEnumerable<ServiceRegistration> Analyze(
        GeneratorAttributeSyntaxContext context,
        CancellationToken cancellationToken)
    {
        if (context.TargetSymbol is not INamedTypeSymbol classSymbol)
        {
            yield break;
        }

        if (!classSymbol.IsConcreteClass())
        {
            yield break;
        }

        var implementationTypeName = classSymbol.GetFullyQualifiedName();

        foreach (var attribute in context.Attributes)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var registration = AnalyzeAttribute(attribute, classSymbol, implementationTypeName);
            if (registration != null)
            {
                yield return registration;
            }
        }
    }

    private static ServiceRegistration? AnalyzeAttribute(
        AttributeData attribute,
        INamedTypeSymbol classSymbol,
        string implementationTypeName)
    {
        var attributeClass = attribute.AttributeClass;
        if (attributeClass == null)
        {
            return null;
        }

        // Determine the service type
        string serviceTypeName;
        var lifetime = ServiceLifetime.Transient;
        string? serviceKey = null;

        // Check if this is a generic ServiceDescriptor<T> attribute
        if (attributeClass.IsGenericType &&
            attributeClass.OriginalDefinition?.ToDisplayString() == GenericServiceDescriptorAttributeName)
        {
            // ServiceDescriptor<T> - get T as the service type
            var typeArg = attributeClass.TypeArguments.FirstOrDefault();
            if (typeArg == null)
            {
                return null;
            }
            serviceTypeName = typeArg.GetFullyQualifiedName();

            // Parse remaining constructor arguments (lifetime, key)
            ParseConstructorArguments(attribute, 0, out lifetime, out serviceKey);
        }
        else
        {
            // Regular ServiceDescriptor attribute
            var args = attribute.ConstructorArguments;

            if (args.Length == 0)
            {
                // Default: register as self with Transient lifetime
                serviceTypeName = implementationTypeName;
            }
            else
            {
                // First argument is Type? serviceType
                var serviceTypeArg = args[0];
                if (serviceTypeArg.Value is ITypeSymbol serviceType)
                {
                    serviceTypeName = serviceType.GetFullyQualifiedName();
                }
                else
                {
                    // null service type means use implementation type
                    serviceTypeName = implementationTypeName;
                }

                ParseConstructorArguments(attribute, 1, out lifetime, out serviceKey);
            }
        }

        // Get source location for diagnostics
        var location = attribute.ApplicationSyntaxReference?.GetSyntax()?.GetLocation();

        return new ServiceRegistration
        {
            ServiceTypeFullName = serviceTypeName,
            ImplementationTypeFullName = implementationTypeName,
            Lifetime = lifetime,
            ServiceKey = serviceKey,
            Strategy = RegistrationStrategyKind.Append,
            SourceLocation = location
        };
    }

    private static void ParseConstructorArguments(
        AttributeData attribute,
        int startIndex,
        out ServiceLifetime lifetime,
        out string? serviceKey)
    {
        lifetime = ServiceLifetime.Transient;
        serviceKey = null;

        var args = attribute.ConstructorArguments;

        for (var i = startIndex; i < args.Length; i++)
        {
            var arg = args[i];

            // Check for ServiceLifetime
            var typeDisplay = arg.Type?.ToDisplayString();
            if (typeDisplay == "Microsoft.Extensions.DependencyInjection.ServiceLifetime" &&
                arg.Value is int lifetimeValue)
            {
                lifetime = (ServiceLifetime)lifetimeValue;
            }
            // Check for object? serviceKey (third argument typically)
            else if (typeDisplay == "object" || typeDisplay == "string")
            {
                if (arg.Value != null)
                {
                    serviceKey = arg.Value.ToString();
                }
            }
        }
    }

    /// <summary>
    /// Gets all service types for a class including base types and interfaces when no explicit type is specified.
    /// </summary>
    public static IEnumerable<string> GetAllServiceTypes(INamedTypeSymbol classSymbol)
    {
        // Include the class itself
        yield return classSymbol.GetFullyQualifiedName();

        // Include all implemented interfaces
        foreach (var iface in classSymbol.AllInterfaces)
        {
            yield return iface.GetFullyQualifiedName();
        }

        // Include base types (except object)
        var baseType = classSymbol.BaseType;
        while (baseType != null && baseType.SpecialType != SpecialType.System_Object)
        {
            yield return baseType.GetFullyQualifiedName();
            baseType = baseType.BaseType;
        }
    }
}
