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
    /// Replace existing services by either service- or implementation type.
    /// </summary>
    All = ServiceType | ImplementationType
}
