using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.Framework.DependencyInjection;

namespace Microsoft.Extensions.DependencyInjection.Scanning
{
    internal class LifetimeSelector : ILifetimeSelector
    {
        public LifetimeSelector(IServiceTypeSelector serviceTypeSelector, IEnumerable<Tuple<Type, IEnumerable<Type>>> types)
        {
            ServiceTypeSelector = serviceTypeSelector;
            Types = types;
        }

        private IEnumerable<Tuple<Type, IEnumerable<Type>>> Types { get; }

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
            ServiceTypeSelector.AddFromAttributes();
        }

        public IServiceTypeSelector AddClasses()
        {
            return ServiceTypeSelector.AddClasses();
        }

        public IServiceTypeSelector AddClasses(Action<IImplementationTypeFilter> action)
        {
            return ServiceTypeSelector.AddClasses(action);
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