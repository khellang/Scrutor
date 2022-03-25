using Microsoft.Extensions.DependencyInjection;
using System;

namespace Scrutor.Decoration
{
    internal static class DecoratorInstanceFactory
    {
        internal static Func<IServiceProvider, object> Default(ServiceDescriptor descriptor, Type decorator) =>
            (serviceProvider) =>
            {
                var instanceToDecorate = GetInstance(serviceProvider, descriptor);
                return ActivatorUtilities.CreateInstance(serviceProvider, decorator, instanceToDecorate);
            };

        internal static Func<IServiceProvider, object> Custom(ServiceDescriptor descriptor, Func<object, IServiceProvider, object> creationFactory) =>
            (serviceProvider) =>
            {
                var instanceToDecorate = GetInstance(serviceProvider, descriptor);
                return creationFactory(instanceToDecorate, serviceProvider);
            };

        private static object GetInstance(IServiceProvider provider, ServiceDescriptor descriptor)
        {
            if (descriptor.ImplementationInstance != null)
            {
                return descriptor.ImplementationInstance;
            }

            var implementationType = descriptor.ImplementationType;
            if (implementationType != null)
            {
                return ActivatorUtilities.CreateInstance(provider, implementationType);
            }

            if (descriptor.ImplementationFactory != null)
            {
                return descriptor.ImplementationFactory(provider);
            }

            throw new InvalidOperationException($"No implementation factory or instance or type found for {descriptor.ServiceType}.");
        }
    }
}
