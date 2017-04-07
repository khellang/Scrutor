using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;

namespace Scrutor
{
    internal class LifetimeSelector : ServiceTypeSelector, ILifetimeSelector, ISelector
    {
        public LifetimeSelector(IEnumerable<Type> types, IEnumerable<TypeMap> typeMaps) : base(types)
        {
            TypeMaps = typeMaps;
        }

        private IEnumerable<TypeMap> TypeMaps { get; }

        private ServiceLifetime? Lifetime { get; set; }

        /// <inheritdoc />
        public IImplementationTypeSelector WithSingletonLifetime()
        {
            return WithLifetime(ServiceLifetime.Singleton);
        }

        /// <inheritdoc />
        public IImplementationTypeSelector WithScopedLifetime()
        {
            return WithLifetime(ServiceLifetime.Scoped);
        }

        /// <inheritdoc />
        public IImplementationTypeSelector WithTransientLifetime()
        {
            return WithLifetime(ServiceLifetime.Transient);
        }

        /// <inheritdoc />
        public IImplementationTypeSelector WithLifetime(ServiceLifetime lifetime)
        {
            Lifetime = lifetime;
            return this;
        }

        void ISelector.Populate(IServiceCollection services, RegistrationStrategy registrationStrategy)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (!Lifetime.HasValue)
            {
                Lifetime = ServiceLifetime.Transient;
            }

            registrationStrategy = registrationStrategy ?? RegistrationStrategy.Append;

            foreach (var typeMap in TypeMaps)
            {
                foreach (var serviceType in typeMap.ServiceTypes)
                {
                    var implementationType = typeMap.ImplementationType;

                    if (!implementationType.IsAssignableTo(serviceType))
                    {
                        throw new InvalidOperationException($@"Type ""{implementationType.FullName}"" is not assignable to ""${serviceType.FullName}"".");
                    }

                    var descriptor = new ServiceDescriptor(serviceType, implementationType, Lifetime.Value);

                    registrationStrategy.Apply(services, descriptor);
                }
            }
        }
    }
}
