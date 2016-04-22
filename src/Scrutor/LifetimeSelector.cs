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

        public IImplementationTypeSelector FromAssemblyOf<T>()
        {
            return ServiceTypeSelector.FromAssemblyOf<T>();
        }

        public IImplementationTypeSelector FromAssembliesOf(params Type[] types)
        {
            return ServiceTypeSelector.FromAssembliesOf(types);
        }

        public IImplementationTypeSelector FromAssembliesOf(IEnumerable<Type> types)
        {
            return ServiceTypeSelector.FromAssembliesOf(types);
        }

        public IImplementationTypeSelector FromAssemblies(params Assembly[] assemblies)
        {
            return ServiceTypeSelector.FromAssemblies(assemblies);
        }

        public IImplementationTypeSelector FromAssemblies(IEnumerable<Assembly> assemblies)
        {
            return ServiceTypeSelector.FromAssemblies(assemblies);
        }

        public void AddFromAttributes()
        {
            ServiceTypeSelector.AddClasses().UsingAttributes();
        }

        public void AddFromAttributes(bool publicOnly)
        {
            ServiceTypeSelector.AddClasses(publicOnly).UsingAttributes();
        }

        public IServiceTypeSelector AddClasses()
        {
            return ServiceTypeSelector.AddClasses();
        }

        public IServiceTypeSelector AddClasses(bool publicOnly)
        {
            return ServiceTypeSelector.AddClasses(publicOnly);
        }

        public IServiceTypeSelector AddClasses(Action<IImplementationTypeFilter> action)
        {
            return ServiceTypeSelector.AddClasses(action);
        }

        public IServiceTypeSelector AddClasses(Action<IImplementationTypeFilter> action, bool publicOnly)
        {
            return ServiceTypeSelector.AddClasses(action, publicOnly);
        }

        public void AddFromAttributes(Action<IImplementationTypeFilter> action)
        {
            ServiceTypeSelector.AddClasses(action).UsingAttributes();
        }

        public void AddFromAttributes(Action<IImplementationTypeFilter> action, bool publicOnly)
        {
            ServiceTypeSelector.AddClasses(action, publicOnly).UsingAttributes();
        }

        public ILifetimeSelector AsSelf()
        {
            return ServiceTypeSelector.AsSelf();
        }

        public ILifetimeSelector As<T>()
        {
            return ServiceTypeSelector.As<T>();
        }

        public ILifetimeSelector As(params Type[] types)
        {
            return ServiceTypeSelector.As(types);
        }

        public ILifetimeSelector As(IEnumerable<Type> types)
        {
            return ServiceTypeSelector.As(types);
        }

        public ILifetimeSelector AsImplementedInterfaces()
        {
            return ServiceTypeSelector.AsImplementedInterfaces();
        }

        public ILifetimeSelector AsMatchingInterface()
        {
            return ServiceTypeSelector.AsMatchingInterface();
        }

        public ILifetimeSelector AsMatchingInterface(Action<TypeInfo, IImplementationTypeFilter> action)
        {
            return ServiceTypeSelector.AsMatchingInterface(action);
        }

        public IImplementationTypeSelector UsingAttributes()
        {
            return ServiceTypeSelector.UsingAttributes();
        }

        public ILifetimeSelector As(Func<Type, IEnumerable<Type>> selector)
        {
            return ServiceTypeSelector.As(selector);
        }

        public IImplementationTypeSelector WithSingletonLifetime()
        {
            return WithLifetime(ServiceLifetime.Singleton);
        }

        public IImplementationTypeSelector WithScopedLifetime()
        {
            return WithLifetime(ServiceLifetime.Scoped);
        }

        public IImplementationTypeSelector WithTransientLifetime()
        {
            return WithLifetime(ServiceLifetime.Transient);
        }

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
