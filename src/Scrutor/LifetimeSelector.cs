using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace Scrutor
{
    internal class LifetimeSelector : ILifetimeSelector, ISelector
    {
        public LifetimeSelector(IServiceTypeSelector serviceTypeSelector, IEnumerable<TypeMap> typeMaps)
        {
            ServiceTypeSelector = serviceTypeSelector;
            TypeMaps = typeMaps;
        }

        private IEnumerable<TypeMap> TypeMaps { get; }

        private ServiceLifetime? Lifetime { get; set; }

        private IServiceTypeSelector ServiceTypeSelector { get; }

        /// <inheritdoc />
        public IImplementationTypeSelector FromAssemblyOf<T>()
        {
            return ServiceTypeSelector.FromAssemblyOf<T>();
        }

        /// <inheritdoc />
        public IImplementationTypeSelector FromAssembliesOf(params Type[] types)
        {
            return ServiceTypeSelector.FromAssembliesOf(types);
        }

        /// <inheritdoc />
        public IImplementationTypeSelector FromAssembliesOf(IEnumerable<Type> types)
        {
            return ServiceTypeSelector.FromAssembliesOf(types);
        }

        /// <inheritdoc />
        public IImplementationTypeSelector FromAssemblies(params Assembly[] assemblies)
        {
            return ServiceTypeSelector.FromAssemblies(assemblies);
        }

        /// <inheritdoc />
        public IImplementationTypeSelector FromAssemblies(IEnumerable<Assembly> assemblies)
        {
            return ServiceTypeSelector.FromAssemblies(assemblies);
        }

        /// <inheritdoc />
        public void AddFromAttributes()
        {
            ServiceTypeSelector.AddClasses().UsingAttributes();
        }

        /// <inheritdoc />
        public void AddFromAttributes(bool publicOnly)
        {
            ServiceTypeSelector.AddClasses(publicOnly).UsingAttributes();
        }

        /// <inheritdoc />
        public IServiceTypeSelector AddClasses()
        {
            return ServiceTypeSelector.AddClasses();
        }

        /// <inheritdoc />
        public IServiceTypeSelector AddClasses(bool publicOnly)
        {
            return ServiceTypeSelector.AddClasses(publicOnly);
        }

        /// <inheritdoc />
        public IServiceTypeSelector AddClasses(Action<IImplementationTypeFilter> action)
        {
            return ServiceTypeSelector.AddClasses(action);
        }

        /// <inheritdoc />
        public IServiceTypeSelector AddClasses(Action<IImplementationTypeFilter> action, bool publicOnly)
        {
            return ServiceTypeSelector.AddClasses(action, publicOnly);
        }

        /// <inheritdoc />
        public void AddFromAttributes(Action<IImplementationTypeFilter> action)
        {
            ServiceTypeSelector.AddClasses(action).UsingAttributes();
        }

        /// <inheritdoc />
        public void AddFromAttributes(Action<IImplementationTypeFilter> action, bool publicOnly)
        {
            ServiceTypeSelector.AddClasses(action, publicOnly).UsingAttributes();
        }

        /// <inheritdoc />
        public ILifetimeSelector AsSelf()
        {
            return ServiceTypeSelector.AsSelf();
        }

        /// <inheritdoc />
        public ILifetimeSelector As<T>()
        {
            return ServiceTypeSelector.As<T>();
        }

        /// <inheritdoc />
        public ILifetimeSelector As(params Type[] types)
        {
            return ServiceTypeSelector.As(types);
        }

        /// <inheritdoc />
        public ILifetimeSelector As(IEnumerable<Type> types)
        {
            return ServiceTypeSelector.As(types);
        }

        /// <inheritdoc />
        public ILifetimeSelector AsImplementedInterfaces()
        {
            return ServiceTypeSelector.AsImplementedInterfaces();
        }

        /// <inheritdoc />
        public ILifetimeSelector AsMatchingInterface()
        {
            return ServiceTypeSelector.AsMatchingInterface();
        }

        /// <inheritdoc />
        public ILifetimeSelector AsMatchingInterface(Action<TypeInfo, IImplementationTypeFilter> action)
        {
            return ServiceTypeSelector.AsMatchingInterface(action);
        }

        /// <inheritdoc />
        public IImplementationTypeSelector UsingAttributes()
        {
            return ServiceTypeSelector.UsingAttributes();
        }

        /// <inheritdoc />
        public ILifetimeSelector As(Func<Type, IEnumerable<Type>> selector)
        {
            return ServiceTypeSelector.As(selector);
        }

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

        void ISelector.Populate(IServiceCollection services)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (!Lifetime.HasValue)
            {
                Lifetime = ServiceLifetime.Transient;
            }

            foreach (var typeMap in TypeMaps)
            {
                foreach (var serviceType in typeMap.ServiceTypes)
                {
                    var implementationType = typeMap.ImplementationType;

                    var descriptor = new ServiceDescriptor(serviceType, implementationType, Lifetime.Value);

                    services.Add(descriptor);
                }
            }
        }
    }
}
