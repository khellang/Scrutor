using System;

namespace Scrutor;

internal sealed class ClosedTypeDecorationStrategy : DecorationStrategy
{
    public ClosedTypeDecorationStrategy(Type serviceType, string? serviceKey, Type? decoratorType, Func<object, IServiceProvider, object>? decoratorFactory) : base(serviceType, serviceKey)
    {
        DecoratorType = decoratorType;
        DecoratorFactory = decoratorFactory;
    }

    private Type? DecoratorType { get; }

    private Func<object, IServiceProvider, object>? DecoratorFactory { get; }

    protected override bool CanDecorate(Type serviceType) => ServiceType == serviceType;

    public override Func<IServiceProvider, object?, object> CreateDecorator(Type serviceType, string serviceKey)
    {
        if (DecoratorType is not null)
        {
            return TypeDecorator(serviceType, serviceKey, DecoratorType);
        }

        if (DecoratorFactory is not null)
        {
            return FactoryDecorator(serviceType, serviceKey, DecoratorFactory);
        }

        throw new InvalidOperationException($"Both {nameof(DecoratorType)} and {nameof(DecoratorFactory)} can not be null.");
    }
}
