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

        public void Populate(IServiceCollection services)
        {
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
                    var serviceType = GetServiceType(type, attribute);

                    var descriptor = new ServiceDescriptor(serviceType, type, attribute.Lifetime);

                    services.Add(descriptor);
                }
            }
        }

        private static Type GetServiceType(Type type, ServiceDescriptorAttribute attribute)
        {
            var serviceType = attribute.ServiceType;

            if (serviceType == null)
            {
                return type;
            }

            if (!serviceType.IsAssignableFrom(type))
            {
                throw new InvalidOperationException($@"Type ""{type.FullName}"" is not assignable to ""${serviceType.FullName}"".");
            }

            return serviceType;
        }
    }
}
