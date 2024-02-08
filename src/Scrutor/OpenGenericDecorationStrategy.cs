using System;

namespace Scrutor;

public class OpenGenericDecorationStrategy : DecorationStrategy
{
    public OpenGenericDecorationStrategy(Type serviceType, string? serviceKey, Type? decoratorType, Func<object, IServiceProvider, object>? decoratorFactory) : base(serviceType, serviceKey)
    {
        DecoratorType = decoratorType;
        DecoratorFactory = decoratorFactory;
    }

    private Type? DecoratorType { get; }

    private Func<object, IServiceProvider, object>? DecoratorFactory { get; }

    protected override bool CanDecorate(Type serviceType) =>
        serviceType.IsGenericType
            && !serviceType.IsGenericTypeDefinition
            && serviceType.GetGenericTypeDefinition() == ServiceType.GetGenericTypeDefinition()
            && (DecoratorType is null || serviceType.HasCompatibleGenericArguments(DecoratorType));

    public override Func<IServiceProvider, object?, object> CreateDecorator(Type serviceType, string serviceKey)
    {
        if (DecoratorType is not null)
        {
            var genericArguments = serviceType.GetGenericArguments();
            var closedDecorator = DecoratorType.MakeGenericType(genericArguments);

            return TypeDecorator(serviceType, serviceKey, closedDecorator);
        }

        if (DecoratorFactory is not null)
        {
            return FactoryDecorator(serviceType, serviceKey, DecoratorFactory);
        }

        throw new InvalidOperationException($"Both {nameof(DecoratorType)} and {nameof(DecoratorFactory)} can not be null.");
    }
}
