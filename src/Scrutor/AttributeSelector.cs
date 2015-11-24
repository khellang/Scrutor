using System;
using System.Collections.Generic;
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
            foreach (var type in Types)
            {
                var typeInfo = type.GetTypeInfo();

                var attribute = typeInfo.GetCustomAttribute<ServiceDescriptorAttribute>();

                if (attribute == null)
                {
                    continue;
                }

                var serviceType = attribute.ServiceType ?? type;

                var descriptor = new ServiceDescriptor(serviceType, type, attribute.Lifetime);

                services.Add(descriptor);
            }
        }
    }
}