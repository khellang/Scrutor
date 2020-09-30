using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;

namespace Scrutor
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class ServiceDescriptorAttribute : Attribute
    {
        public ServiceDescriptorAttribute() : this(null) { }

        public ServiceDescriptorAttribute(ServiceLifetime lifetime) : this(null, lifetime) { }

        public ServiceDescriptorAttribute(Type? serviceType) : this(serviceType, ServiceLifetime.Transient) { }

        public ServiceDescriptorAttribute(Type? serviceType, ServiceLifetime lifetime)
        {
            ServiceType = serviceType;
            Lifetime = lifetime;
        }

        public Type? ServiceType { get; }

        public ServiceLifetime Lifetime { get; }

        public IEnumerable<Type> GetServiceTypes(Type fallbackType)
        {
            if (ServiceType is null)
            {
                yield return fallbackType;

                var fallbackTypes = fallbackType.GetBaseTypes();

                foreach (var type in fallbackTypes)
                {
                    if (type == typeof(object))
                    {
                        continue;
                    }

                    yield return type;
                }

                yield break;
            }

            if (!fallbackType.IsAssignableTo(ServiceType))
            {
                throw new InvalidOperationException($@"Type ""{fallbackType.ToFriendlyName()}"" is not assignable to ""{ServiceType.ToFriendlyName()}"".");
            }

            yield return ServiceType;
        }
    }
}
