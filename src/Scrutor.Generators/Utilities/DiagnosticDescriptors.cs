using Microsoft.CodeAnalysis;

namespace Scrutor.Generators.Utilities;

/// <summary>
/// Diagnostic descriptors for Scrutor source generator.
/// </summary>
internal static class DiagnosticDescriptors
{
    private const string Category = "Scrutor.Generators";

    /// <summary>
    /// SCRUT001: Unsupported pattern detected in Scan() configuration.
    /// </summary>
    public static readonly DiagnosticDescriptor UnsupportedPattern = new(
        id: "SCRUT001",
        title: "Unsupported Scrutor pattern",
        messageFormat: "The Scan() configuration uses an unsupported pattern for AOT generation: {0}. This registration will be skipped in generated code.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Some Scrutor patterns like delegate-based type selectors or dynamic assembly loading cannot be resolved at compile time.");

    /// <summary>
    /// SCRUT002: ServiceDescriptor attribute has invalid configuration.
    /// </summary>
    public static readonly DiagnosticDescriptor InvalidServiceDescriptor = new(
        id: "SCRUT002",
        title: "Invalid ServiceDescriptor configuration",
        messageFormat: "The [ServiceDescriptor] attribute on '{0}' has an invalid configuration: {1}",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "The ServiceDescriptor attribute must have valid service and implementation types.");

    /// <summary>
    /// SCRUT003: Implementation type is not assignable to service type.
    /// </summary>
    public static readonly DiagnosticDescriptor TypeMismatch = new(
        id: "SCRUT003",
        title: "Type mismatch in service registration",
        messageFormat: "Type '{0}' is not assignable to service type '{1}'",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "The implementation type must be assignable to the specified service type.");

    /// <summary>
    /// SCRUT004: Duplicate service registration detected.
    /// </summary>
    public static readonly DiagnosticDescriptor DuplicateRegistration = new(
        id: "SCRUT004",
        title: "Duplicate service registration",
        messageFormat: "Duplicate [ServiceDescriptor] for service type '{0}' with key '{1}' on type '{2}'",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Multiple ServiceDescriptor attributes specify the same service type and key.");

    /// <summary>
    /// SCRUT005: Generation was successful.
    /// </summary>
    public static readonly DiagnosticDescriptor GenerationInfo = new(
        id: "SCRUT005",
        title: "Scrutor generation info",
        messageFormat: "{0}",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "Informational message about Scrutor source generation.");
}
