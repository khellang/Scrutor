using System;

namespace Scrutor;

public class DuplicateTypeRegistrationException : InvalidOperationException
{
    public DuplicateTypeRegistrationException(Type serviceType)
        : base($"A service of type '{serviceType.ToFriendlyName()}' has already been registered.")
    {
        ServiceType = serviceType;
    }

    public Type ServiceType { get; }
}
