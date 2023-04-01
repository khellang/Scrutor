using System;

namespace Scrutor;

[Flags]
public enum ReplacementBehavior
{
    /// <summary>
    /// Replace existing services by service type.
    /// </summary>
    Default = 0,

    /// <summary>
    /// Replace existing services by service type (default).
    /// </summary>
    ServiceType = 1,

    /// <summary>
    /// Replace existing services by implementation type.
    /// </summary>
    ImplementationType = 2,

    /// <summary>
    /// Replace existing services with the same service or implementation type.
    /// </summary>
    Either = ServiceType | ImplementationType,

    /// <summary>
    /// Replace existing services with the same service and implementation type.
    /// </summary>
    Both = 4
}
