using Microsoft.Extensions.DependencyInjection;
using System;

namespace Scrutor.Decoration;

internal readonly struct Decorator
{
    private Decorator(IDecoratorStrategy strategy) => Strategy = strategy;

    private IDecoratorStrategy Strategy { get; }

    public static Decorator Create(Type serviceType, Type? decoratorType, Func<object, IServiceProvider, object>? decoratorFactory)
    {
        IDecoratorStrategy strategy;

        if (serviceType.IsOpenGeneric())
        {
            strategy = new OpenGenericDecoratorStrategy(serviceType, decoratorType, decoratorFactory);
        }
        else
        {
            strategy = new ClosedTypeDecoratorStrategy(serviceType, decoratorType, decoratorFactory);
        }

        return new Decorator(strategy);
    }

    public IServiceCollection Decorate(IServiceCollection services)
    {
        if (TryDecorate(services))
        {
            return services;
        }

        throw new MissingTypeRegistrationException(Strategy.ServiceType);

    }

    public bool TryDecorate(IServiceCollection services)
    {
        var decorated = false;

        for (var i = services.Count - 1; i >= 0; i--)
        {
            var serviceDescriptor = services[i];

            if (serviceDescriptor.ServiceType is DecoratedType)
            {
                continue; // Service has already been decorated.
            }

            if (!Strategy.CanDecorate(serviceDescriptor.ServiceType))
            {
                continue; // Unable to decorate using the specified strategy.
            }

            var decoratedType = new DecoratedType(serviceDescriptor.ServiceType);

            // Insert decorated
            services.Add(serviceDescriptor.WithServiceType(decoratedType));

            // Replace decorator
            services[i] = serviceDescriptor.WithImplementationFactory(Strategy.CreateDecorator(decoratedType));

            decorated = true;
        }

        return decorated;
    }
}
