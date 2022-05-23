using System;

namespace Scrutor.Decoration;

internal sealed class ClosedTypeDecoratorStrategy : IDecoratorStrategy
{
    public ClosedTypeDecoratorStrategy(Type serviceType, Type? decoratorType, Func<object, IServiceProvider, object>? decoratorFactory)
    {
        ServiceType = serviceType;
        DecoratorType = decoratorType;
        DecoratorFactory = decoratorFactory;
    }

    public Type ServiceType { get; }
        
    private Type? DecoratorType { get; }

    private Func<object, IServiceProvider, object>? DecoratorFactory { get; }

    public bool CanDecorate(Type serviceType) => ServiceType == serviceType;

    public Func<IServiceProvider, object> CreateDecorator(Type serviceType)
    {
        if (DecoratorType is not null)
        {
            return DecoratorInstanceFactory.Default(serviceType, DecoratorType);
        }

        if (DecoratorFactory is not null)
        {
            return DecoratorInstanceFactory.Custom(serviceType, DecoratorFactory);
        }

        throw new InvalidOperationException($"Both serviceType and decoratorFactory can not be null.");
    } 
}
