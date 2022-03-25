using Microsoft.Extensions.DependencyInjection;
using System;

namespace Scrutor.Decoration
{
    internal sealed class Decoration
    {
        private readonly IDecorationStrategy _decorationStrategy;

        public Decoration(IDecorationStrategy decorationStrategy)
            => _decorationStrategy = decorationStrategy;

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

                if (_decorationStrategy.CanDecorate(serviceDescriptor.ServiceType))
                {
                    var decoratorFactory = _decorationStrategy.CreateDecorator(serviceDescriptor);

                    // replace decorator
                    services[i] = new ServiceDescriptor(serviceDescriptor.ServiceType, decoratorFactory, serviceDescriptor.Lifetime);

                    ++decorated;
                }
            }

            return decorated;
        }
    }
}
