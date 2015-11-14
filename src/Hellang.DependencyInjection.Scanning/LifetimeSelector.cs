using System;
using System.Collections.Generic;
using Microsoft.Framework.DependencyInjection;

namespace Microsoft.Extensions.DependencyInjection.Scanning
{
    internal class LifetimeSelector : ILifetimeSelector
    {
        public LifetimeSelector(IEnumerable<Tuple<Type, IEnumerable<Type>>> types)
        {
            Types = types;
        }

        private IEnumerable<Tuple<Type, IEnumerable<Type>>> Types { get; }

        private ServiceLifetime? Lifetime { get; set; }

        public void WithSingletonLifetime()
        {
            WithLifetime(ServiceLifetime.Singleton);
        }

        public void WithScopedLifetime()
        {
            WithLifetime(ServiceLifetime.Scoped);
        }

        public void WithTransientLifetime()
        {
            WithLifetime(ServiceLifetime.Transient);
        }

        public void WithLifetime(ServiceLifetime lifetime)
        {
            Lifetime = lifetime;
        }

        internal void Populate(IServiceCollection services)
        {
            if (!Lifetime.HasValue)
            {
                Lifetime = ServiceLifetime.Transient;
            }

            foreach (var tuple in Types)
            {
                foreach (var serviceType in tuple.Item2)
                {
                    var implementationType = tuple.Item1;

                    var descriptor = new ServiceDescriptor(serviceType, implementationType, Lifetime.Value);

                    services.Add(descriptor);
                }
            }
        }
    }
}