using System.Collections.Generic;

namespace Scrutor.Generators.Models;

/// <summary>
/// Represents a parsed Scan() method chain configuration.
/// </summary>
internal sealed class ScanConfiguration
{
    /// <summary>
    /// The assembly source types to scan from (e.g., types from FromAssemblyOf&lt;T&gt;()).
    /// </summary>
    public List<string> AssemblySourceTypes { get; } = new();

    /// <summary>
    /// Type filters applied via AddClasses (assignable to types, namespace filters, etc.).
    /// </summary>
    public List<TypeFilter> TypeFilters { get; } = new();

    /// <summary>
    /// Service type selectors (AsImplementedInterfaces, AsSelf, As&lt;T&gt;).
    /// </summary>
    public List<ServiceTypeMapping> ServiceTypeMappings { get; } = new();

    /// <summary>
    /// The lifetime for registrations.
    /// </summary>
    public ServiceLifetime Lifetime { get; set; } = ServiceLifetime.Transient;

    /// <summary>
    /// The registration strategy.
    /// </summary>
    public RegistrationStrategyKind Strategy { get; set; } = RegistrationStrategyKind.Append;

    /// <summary>
    /// Service key for keyed services.
    /// </summary>
    public string? ServiceKey { get; set; }

    /// <summary>
    /// Whether UsingAttributes() was called.
    /// </summary>
    public bool UsesAttributes { get; set; }

    /// <summary>
    /// Whether to include non-public classes.
    /// </summary>
    public bool IncludeNonPublic { get; set; }

    /// <summary>
    /// Whether this configuration uses unsupported patterns.
    /// </summary>
    public bool HasUnsupportedPatterns { get; set; }

    /// <summary>
    /// Description of the unsupported pattern for diagnostic messages.
    /// </summary>
    public string? UnsupportedPatternDescription { get; set; }
}

/// <summary>
/// Represents a type filter from AddClasses().
/// </summary>
internal sealed class TypeFilter
{
    public TypeFilterKind Kind { get; set; }
    public string? TypeFullName { get; set; }
    public string? Namespace { get; set; }
}

/// <summary>
/// Kinds of type filters.
/// </summary>
internal enum TypeFilterKind
{
    All,
    AssignableTo,
    InNamespace,
    InExactNamespace,
    WithAttribute,
    WithoutAttribute
}

/// <summary>
/// Represents how implementation types map to service types.
/// </summary>
internal sealed class ServiceTypeMapping
{
    public ServiceTypeMappingKind Kind { get; set; }
    public string? ExplicitTypeFullName { get; set; }
}

/// <summary>
/// Kinds of service type mappings.
/// </summary>
internal enum ServiceTypeMappingKind
{
    AsSelf,
    AsImplementedInterfaces,
    AsMatchingInterface,
    AsExplicitType,
    /// <summary>
    /// AsSelfWithInterfaces - registers self, then forwards interfaces to self via factory.
    /// </summary>
    AsSelfWithInterfaces
}
