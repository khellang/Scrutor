using System;
using Microsoft.Extensions.Internal;

namespace Scrutor
{
    public class MissingTypeRegistrationException : InvalidOperationException
    {
        public MissingTypeRegistrationException(Type serviceType)
            : base($"Could not find any registered services for type '{TypeNameHelper.GetTypeDisplayName(serviceType)}'.")
        {
            ServiceType = serviceType;
        }

        public Type ServiceType { get; }
    }
}
