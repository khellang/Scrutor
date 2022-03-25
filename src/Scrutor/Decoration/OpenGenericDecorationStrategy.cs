using Microsoft.Extensions.DependencyInjection;
using System;

namespace Scrutor.Decoration
{
    internal class OpenGenericDecorationStrategy : IDecorationStrategy
    {
        private readonly Type _serviceType;
        private readonly Type _decoratorType;

        public OpenGenericDecorationStrategy(Type serviceType, Type decoratorType)
        {
            _serviceType = serviceType;
            _decoratorType = decoratorType;
        }

        public Type ServiceType => _serviceType;

        public bool CanDecorate(Type serviceType)
        {
            var canHandle = serviceType.IsGenericType
                && (!serviceType.IsGenericTypeDefinition)
                && _serviceType.GetGenericTypeDefinition() == serviceType.GetGenericTypeDefinition()
                && HasCompatibleGenericArguments(serviceType);

            return canHandle;
        }

        public Func<IServiceProvider, object> CreateDecorator(ServiceDescriptor descriptor)
        {
            var genericArguments = descriptor.ServiceType.GetGenericArguments();
            var closedDecorator = _decoratorType.MakeGenericType(genericArguments);

            return DecoratorInstanceFactory.Default(descriptor, closedDecorator);
        }

        private bool HasCompatibleGenericArguments(Type serviceType)
        {
            var canHandle = false;

            if (_decoratorType is null)
            {
                canHandle = true;
            }
            else
            {
                var genericArguments = serviceType.GetGenericArguments();

                try
                {
                    _ = _decoratorType.MakeGenericType(genericArguments);
                    canHandle = true;
                }
                catch (ArgumentException)
                {
                }
            }

            return canHandle;
        }
    }
}
