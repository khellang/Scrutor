using System;

namespace Scrutor.Decoration;

internal sealed class OpenGenericDecoratorStrategy : IDecoratorStrategy
{
    public OpenGenericDecoratorStrategy(Type serviceType, Type? decoratorType, Func<object, IServiceProvider, object>? decoratorFactory)
    {
        ServiceType = serviceType;
        DecoratorType = decoratorType;
        DecoratorFactory = decoratorFactory;
    }

    public Type ServiceType { get; }
        
    private Type? DecoratorType { get; }

    private Func<object, IServiceProvider, object>? DecoratorFactory { get; }

    public bool CanDecorate(Type serviceType) =>
        serviceType.IsGenericType
            && !serviceType.IsGenericTypeDefinition
            && serviceType.GetGenericTypeDefinition() == ServiceType.GetGenericTypeDefinition()
            && (DecoratorType is null || serviceType.HasCompatibleGenericArguments(DecoratorType));

    public Func<IServiceProvider, object> CreateDecorator(Type serviceType)
    {
        if (DecoratorType is not null)
        {
            var genericArguments = serviceType.GetGenericArguments();
            var closedDecorator = DecoratorType.MakeGenericType(genericArguments);

            return DecoratorInstanceFactory.Default(serviceType, closedDecorator);
        }

        if (DecoratorFactory is not null)
        {
            return DecoratorInstanceFactory.Custom(serviceType, DecoratorFactory);
        }

        throw new InvalidOperationException($"Both serviceType and decoratorFactory can not be null.");
    }
}
