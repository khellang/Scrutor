using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace Scrutor
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class ServiceDescriptorAttribute : Attribute
    {
        public ServiceDescriptorAttribute() : this(null) { }

        public ServiceDescriptorAttribute(Type serviceType) : this(serviceType, ServiceLifetime.Transient) { }

        public ServiceDescriptorAttribute(Type serviceType, ServiceLifetime lifetime)
        {
            ServiceType = serviceType;
            Lifetime = lifetime;
        }

        public Type ServiceType { get; }

        public ServiceLifetime Lifetime { get; }

        public IEnumerable<Type> GetServiceTypes(Type fallbackType)
        {
            if (ServiceType == null)
            {
                yield return fallbackType;

                var fallbackTypes = fallbackType.GetBaseTypes();

                foreach (var type in fallbackTypes)
                {
                    yield return type;
                }
            }
            else
            {
                var fallbackTypeInfo = fallbackType.GetTypeInfo();

                var serviceTypeInfo = ServiceType.GetTypeInfo();

                if (!serviceTypeInfo.IsAssignableFrom(fallbackTypeInfo))
                {
                    throw new InvalidOperationException($@"Type ""{fallbackTypeInfo.FullName}"" is not assignable to ""${serviceTypeInfo.FullName}"".");
                }

                yield return ServiceType;
            }
        }
    }
}
