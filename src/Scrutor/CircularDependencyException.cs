using System;

namespace Scrutor
{
    public class CircularDependencyException : InvalidOperationException
    {
        public CircularDependencyException(Type creatingServiceType, Type requestedServiceType)
            : base(
                $"A circular dependency was detected for the service of type '{creatingServiceType.Name}'." +
                $"\r\n" +
                $"{creatingServiceType.Name} -> {requestedServiceType}"
            )
        {
        }
    }
}
