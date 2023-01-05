using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;

namespace Scrutor;

[PublicAPI]
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class ServiceDescriptorAttribute : Attribute
{
    public ServiceDescriptorAttribute() : this(null) { }

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

        if (!fallbackType.IsBasedOn(ServiceType))
        {
            throw new InvalidOperationException($@"Type ""{fallbackType.ToFriendlyName()}"" is not assignable to ""{ServiceType.ToFriendlyName()}"".");
        }

        yield return ServiceType;
    }
}

[PublicAPI]
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public sealed class ServiceDescriptorAttribute<TService> : ServiceDescriptorAttribute
{
    public ServiceDescriptorAttribute() : base(typeof(TService)) { }

    public ServiceDescriptorAttribute(ServiceLifetime lifetime) : base(typeof(TService), lifetime) { }
}
