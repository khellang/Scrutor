using System;

namespace Scrutor.Decoration
{
    internal sealed class OpenGenericDecorationStrategy : IDecorationStrategy
    {
        private readonly Type _serviceType;
        private readonly Type? _decoratorType;
        private readonly Func<object, IServiceProvider, object>? _decoratorFactory;

        public OpenGenericDecorationStrategy(Type serviceType, Type? decoratorType, Func<object, IServiceProvider, object>? decoratorFactory)
        {
            _serviceType = serviceType;
            _decoratorType = decoratorType;
            _decoratorFactory = decoratorFactory;
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

        public Func<IServiceProvider, object> CreateDecorator(Type serviceType)
        {
            if (_decoratorType is not null)
            {
                var genericArguments = serviceType.GetGenericArguments();
                var closedDecorator = _decoratorType.MakeGenericType(genericArguments);

                return DecoratorInstanceFactory.Default(serviceType, closedDecorator);
            }

            if (_decoratorFactory is not null)
            {
                return DecoratorInstanceFactory.Custom(serviceType, _decoratorFactory);
            }

            throw new InvalidOperationException($"Both serviceType and decoratorFactory can not be null.");
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
