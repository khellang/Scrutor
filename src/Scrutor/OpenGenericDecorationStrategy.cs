using Microsoft.Extensions.DependencyInjection;
using System;

namespace Scrutor;

internal sealed class OpenGenericDecorationStrategy : DecorationStrategy
{
    public OpenGenericDecorationStrategy(Type serviceType, Type? decoratorType, Func<object, IServiceProvider, object>? decoratorFactory) : base(serviceType)
    {
        DecoratorType = decoratorType;
        DecoratorFactory = decoratorFactory;
    }

    private Type? DecoratorType { get; }

    private Func<object, IServiceProvider, object>? DecoratorFactory { get; }

    public override bool CanDecorate(Type serviceType) =>
        serviceType.IsGenericType
            && !serviceType.IsGenericTypeDefinition
            && serviceType.GetGenericTypeDefinition() == ServiceType.GetGenericTypeDefinition()
            && (DecoratorType is null || serviceType.HasCompatibleGenericArguments(DecoratorType));

    public override Func<IServiceProvider, object> CreateDecorator(ServiceDescriptor descriptor)
    {
        if (DecoratorType is not null)
        {
            var genericArguments = descriptor.ServiceType.GetGenericArguments();
            var closedDecorator = DecoratorType.MakeGenericType(genericArguments);

            return TypeDecorator(descriptor, closedDecorator);
        }

        if (DecoratorFactory is not null)
        {
            return FactoryDecorator(descriptor, DecoratorFactory);
        }

        throw new InvalidOperationException($"Both serviceType and decoratorFactory can not be null.");
    }
}
