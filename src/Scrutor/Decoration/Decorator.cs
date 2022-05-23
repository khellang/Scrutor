using Microsoft.Extensions.DependencyInjection;
using System;

namespace Scrutor.Decoration
{
    internal readonly struct Decorator
    {
        private readonly IDecorationStrategy _decorationStrategy;

        public Decorator(IDecorationStrategy decorationStrategy)
            => _decorationStrategy = decorationStrategy;
        
        public static Decorator Create(Type serviceType, Type? decoratorType, Func<object, IServiceProvider, object>? decoratorFactory)
        {
            IDecorationStrategy strategy;

            if (serviceType.IsOpenGeneric())
            {
                strategy = new OpenGenericDecorationStrategy(serviceType, decoratorType, decoratorFactory);
            }
            else
            {
                strategy = new ClosedTypeDecorationStrategy(serviceType, decoratorType, decoratorFactory);
            }

            return new Decorator(strategy);
        }

        public static Decorator Create<TService>(Type? decoratorType, Func<object, IServiceProvider, object>? decoratorFactory)
            => new(new ClosedTypeDecorationStrategy(typeof(TService), decoratorType, decoratorFactory));

        public bool TryDecorate(IServiceCollection services)
        {
            var decorated = DecorateServices(services);
            return decorated != 0;
        }

        public IServiceCollection Decorate(IServiceCollection services)
        {
            var decorated = DecorateServices(services);

            if (decorated == 0)
            {
                throw new MissingTypeRegistrationException(_decorationStrategy.ServiceType);
            }

            return services;
        }

        private int DecorateServices(IServiceCollection services)
        {
            int decorated = 0;

            for (int i = services.Count - 1; i >= 0; i--)
            {
                var serviceDescriptor = services[i];

                if (IsNotAlreadyDecorated(serviceDescriptor)
                    && _decorationStrategy.CanDecorate(serviceDescriptor.ServiceType))
                {
                    var decoratedType = new DecoratedType(serviceDescriptor.ServiceType);

                    var decoratorFactory = _decorationStrategy.CreateDecorator(decoratedType);

                    // insert decorated
                    var decoratedServiceDescriptor = CreateDecoratedServiceDescriptor(serviceDescriptor, decoratedType);
                    services.Add(decoratedServiceDescriptor);

                    // replace decorator
                    services[i] = new ServiceDescriptor(serviceDescriptor.ServiceType, decoratorFactory, serviceDescriptor.Lifetime);

                    ++decorated;
                }
            }

            return decorated;
        }

        private static bool IsNotAlreadyDecorated(ServiceDescriptor serviceDescriptor) => serviceDescriptor.ServiceType is not DecoratedType;

        private static ServiceDescriptor CreateDecoratedServiceDescriptor(ServiceDescriptor serviceDescriptor, Type decoratedType) => serviceDescriptor switch
        {
            { ImplementationType: not null } => new ServiceDescriptor(decoratedType, serviceDescriptor.ImplementationType, serviceDescriptor.Lifetime),
            { ImplementationFactory: not null } => new ServiceDescriptor(decoratedType, serviceDescriptor.ImplementationFactory, serviceDescriptor.Lifetime),
            { ImplementationInstance: not null } => new ServiceDescriptor(decoratedType, serviceDescriptor.ImplementationInstance),
            _ => throw new ArgumentException($"No implementation factory or instance or type found for {serviceDescriptor.ServiceType}.", nameof(serviceDescriptor))
        };
    }
}
