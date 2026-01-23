using System;
using Microsoft.CodeAnalysis;

namespace Scrutor.Generators.Models;

/// <summary>
/// Represents a service registration discovered at compile time.
/// </summary>
internal sealed class ServiceRegistration : IEquatable<ServiceRegistration>
{
    public string ServiceTypeFullName { get; init; } = string.Empty;
    public string ImplementationTypeFullName { get; init; } = string.Empty;
    public ServiceLifetime Lifetime { get; init; }
    public string? ServiceKey { get; init; }
    public RegistrationStrategyKind Strategy { get; init; } = RegistrationStrategyKind.Append;
    public Location? SourceLocation { get; init; }

    /// <summary>
    /// When true, this registration forwards to the implementation type via factory
    /// (used by AsSelfWithInterfaces for interface registrations).
    /// </summary>
    public bool ForwardsToImplementation { get; init; }

    /// <summary>
    /// When true, this is an open generic registration (e.g., IRepository&lt;&gt; -> Repository&lt;&gt;).
    /// </summary>
    public bool IsOpenGeneric { get; init; }

    public bool Equals(ServiceRegistration? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;

        return ServiceTypeFullName == other.ServiceTypeFullName &&
               ImplementationTypeFullName == other.ImplementationTypeFullName &&
               Lifetime == other.Lifetime &&
               ServiceKey == other.ServiceKey &&
               Strategy == other.Strategy &&
               ForwardsToImplementation == other.ForwardsToImplementation &&
               IsOpenGeneric == other.IsOpenGeneric;
    }

    public override bool Equals(object? obj) => Equals(obj as ServiceRegistration);

    public override int GetHashCode()
    {
        unchecked
        {
            var hash = 17;
            hash = hash * 31 + ServiceTypeFullName.GetHashCode();
            hash = hash * 31 + ImplementationTypeFullName.GetHashCode();
            hash = hash * 31 + (int)Lifetime;
            hash = hash * 31 + (ServiceKey?.GetHashCode() ?? 0);
            hash = hash * 31 + (int)Strategy;
            hash = hash * 31 + ForwardsToImplementation.GetHashCode();
            hash = hash * 31 + IsOpenGeneric.GetHashCode();
            return hash;
        }
    }
}
