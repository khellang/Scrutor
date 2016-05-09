using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace Scrutor
{
    internal class AttributeSelector : ISelector
    {
        public AttributeSelector(IEnumerable<Type> types)
        {
            Types = types;
        }

        private IEnumerable<Type> Types { get; }

        void ISelector.Populate(IServiceCollection services)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            foreach (var type in Types)
            {
                var typeInfo = type.GetTypeInfo();

                var attributes = typeInfo.GetCustomAttributes<ServiceDescriptorAttribute>().ToArray();

                // Check if the type has multiple attributes with same ServiceType.
                var duplicates = attributes
                    .GroupBy(s => s.ServiceType)
                    .SelectMany(grp => grp.Skip(1));

                if (duplicates.Any())
                {
                    throw new InvalidOperationException($@"Type ""{type.FullName}"" has multiple ServiceDescriptor attributes with the same service type.");
                }

                foreach (var attribute in attributes)
                {
                    var serviceTypes = GetServiceTypes(type, attribute);

                    foreach (var serviceType in serviceTypes)
                    {
                        var descriptor = new ServiceDescriptor(serviceType, type, attribute.Lifetime);

                        services.Add(descriptor);
                    }
                }
            }
        }

        private static IEnumerable<Type> GetServiceTypes(Type type, ServiceDescriptorAttribute attribute)
        {
            var serviceType = attribute.ServiceType;

            if (serviceType == null)
            {
                yield return type;

                var typeInfo = type.GetTypeInfo();

                foreach (var implementedInterface in typeInfo.ImplementedInterfaces)
                {
                    yield return implementedInterface;
                }

                if (typeInfo.BaseType != null && typeInfo.BaseType != typeof(object))
                {
                    yield return typeInfo.BaseType;
                }

                yield break;
            }

            if (!serviceType.IsAssignableFrom(type))
            {
                throw new InvalidOperationException($@"Type ""{type.FullName}"" is not assignable to ""${serviceType.FullName}"".");
            }

            yield return serviceType;
        }
    }
}
