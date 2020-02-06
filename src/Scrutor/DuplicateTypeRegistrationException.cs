using System;
using Microsoft.Extensions.Internal;

namespace Scrutor
{
    public class DuplicateTypeRegistrationException : InvalidOperationException
    {
        public DuplicateTypeRegistrationException(Type serviceType)
            : base($"A service of type '{TypeNameHelper.GetTypeDisplayName(serviceType)}' has already been registered.")
        {
            ServiceType = serviceType;
        }

        public Type ServiceType { get; }
    }
}
