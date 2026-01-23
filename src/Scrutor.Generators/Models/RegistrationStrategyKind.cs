namespace Scrutor.Generators.Models;

/// <summary>
/// Defines how service registrations should be added to the service collection.
/// </summary>
internal enum RegistrationStrategyKind
{
    /// <summary>
    /// Always add the service, even if it already exists (default).
    /// </summary>
    Append,

    /// <summary>
    /// Skip registration if the service type already exists (TryAdd).
    /// </summary>
    Skip,

    /// <summary>
    /// Replace existing registrations for the same service type.
    /// </summary>
    Replace,

    /// <summary>
    /// Replace existing registrations by implementation type.
    /// </summary>
    ReplaceByImplementation
}
