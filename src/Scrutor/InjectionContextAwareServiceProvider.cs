using System;
using Microsoft.Extensions.DependencyInjection;

namespace Scrutor
{
    internal struct InjectionContextAwareServiceProvider : IServiceProvider
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IInjectionContext _injectionContext;
        private readonly Func<IServiceProvider, IInjectionContext, object[]> _dependenciesFactory;

        public InjectionContextAwareServiceProvider(
            IServiceProvider serviceProvider,
            IInjectionContext injectionContext,
            Func<IServiceProvider, IInjectionContext, object[]> dependenciesFactory
        )
        {
            _serviceProvider = new SubServiceProvider(serviceProvider, injectionContext);
            _injectionContext = injectionContext;
            _dependenciesFactory = dependenciesFactory;
        }

        public object GetService(Type serviceType)
        {
            return ActivatorUtilities.CreateInstance(
                _serviceProvider,
                serviceType,
                _dependenciesFactory(_serviceProvider, _injectionContext)
            );
        }

        private struct SubServiceProvider : IServiceProvider
        {
            private readonly IServiceProvider _serviceProvider;
            private readonly IInjectionContext _injectionContext;

            public SubServiceProvider(
                IServiceProvider serviceProvider,
                IInjectionContext injectionContext
            )
            {
                _serviceProvider = serviceProvider;
                _injectionContext = injectionContext;
            }

            public object GetService(Type serviceType)
            {
                if (serviceType == _injectionContext.CreatingServiceType)
                    throw new CircularDependencyException(_injectionContext.CreatingServiceType, serviceType);

                return _serviceProvider.GetService(serviceType);
            }
        }
    }
}
