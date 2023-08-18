using System;

namespace Scrutor;

internal sealed class ClosedTypeDecorationStrategy : DecorationStrategy
{
    public ClosedTypeDecorationStrategy(Type serviceType, Type? decoratorType, Func<object, IServiceProvider, object>? decoratorFactory) : base(serviceType)
    {
        DecoratorType = decoratorType;
        DecoratorFactory = decoratorFactory;
    }

    private Type? DecoratorType { get; }

    private Func<object, IServiceProvider, object>? DecoratorFactory { get; }

    public override bool CanDecorate(Type serviceType) => ServiceType == serviceType;

    public override Func<IServiceProvider, object> CreateDecorator(Type serviceType)
    {
        if (DecoratorType is not null)
        {
            return TypeDecorator(serviceType, DecoratorType);
        }

        if (DecoratorFactory is not null)
        {
            return FactoryDecorator(serviceType, DecoratorFactory);
        }

        throw new InvalidOperationException($"Both serviceType and decoratorFactory can not be null.");
    }
}
