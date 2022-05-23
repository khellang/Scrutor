using System;

namespace Scrutor.Decoration
{
    internal sealed class ClosedTypeDecoratorStrategy : IDecoratorStrategy
    {
        private readonly Type _serviceType;
        private readonly Type? _decoratorType;
        private readonly Func<object, IServiceProvider, object>? _decoratorFactory;

        public ClosedTypeDecoratorStrategy(Type serviceType, Type? decoratorType, Func<object, IServiceProvider, object>? decoratorFactory)
        {
            _serviceType = serviceType;
            _decoratorType = decoratorType;
            _decoratorFactory = decoratorFactory;
        }

        public Type ServiceType => _serviceType;

        public bool CanDecorate(Type serviceType) => _serviceType == serviceType;

        public Func<IServiceProvider, object> CreateDecorator(Type serviceType)
        {
            if (_decoratorType is not null)
            {
                return DecoratorInstanceFactory.Default(serviceType, _decoratorType);
            }

            if (_decoratorFactory is not null)
            {
                return DecoratorInstanceFactory.Custom(serviceType, _decoratorFactory);
            }

            throw new InvalidOperationException($"Both serviceType and decoratorFactory can not be null.");
        } 
    }
}
