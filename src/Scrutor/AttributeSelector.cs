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
                // check whether they have the same service type
                var duplicates = attributes.GroupBy(s => s).SelectMany(grp => grp.Skip(1));
                if (duplicates.Any())
                {
                    throw new InvalidOperationException($"Type \"{type.FullName}\" has multiple ServiceDescriptors specified with the same service type.");
                }

                foreach (var attribute in attributes)
                {
                    var serviceType = GetServiceType(type, attribute);

                    var descriptor = new ServiceDescriptor(serviceType, type, attribute.Lifetime);

                    services.Add(descriptor);
                }
            }
        }

        private Type GetServiceType(Type type, ServiceDescriptorAttribute attribute)
        {
            var serviceType = attribute.ServiceType;
            if (ReferenceEquals(null, serviceType))
            {
                return type;
            }

            if (!serviceType.IsAssignableFrom(type))
            {
                throw new InvalidOperationException($"Type \"{type.FullName}\" does not inherit or implement \"${serviceType}\".");
            }

            return serviceType;

        }

    }
}
