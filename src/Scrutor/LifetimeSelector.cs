using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

#if DEPENDENCY_MODEL
using Microsoft.Extensions.DependencyModel;
#endif

namespace Scrutor
{
    internal sealed class LifetimeSelector : ILifetimeSelector, ISelector
    {
        public LifetimeSelector(IServiceTypeSelector inner, IEnumerable<TypeMap> typeMaps)
        {
            Inner = inner;
            TypeMaps = typeMaps;
        }

        private IServiceTypeSelector Inner { get; }

        private IEnumerable<TypeMap> TypeMaps { get; }

        private ServiceLifetime? Lifetime { get; set; }

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
            Preconditions.IsDefined(lifetime, nameof(lifetime));

            Lifetime = lifetime;
            return this;
        }

        #region Chain Methods

#if NET451
        public IImplementationTypeSelector FromCallingAssembly()
        {
            return Inner.FromCallingAssembly();
        }

        public IImplementationTypeSelector FromExecutingAssembly()
        {
            return Inner.FromExecutingAssembly();
        }
#endif

#if DEPENDENCY_MODEL
        public IImplementationTypeSelector FromEntryAssembly()
        {
            return Inner.FromEntryAssembly();
        }

        public IImplementationTypeSelector FromApplicationDependencies()
        {
            return Inner.FromApplicationDependencies();
        }

        public IImplementationTypeSelector FromApplicationDependencies(Func<Assembly, bool> predicate)
        {
            return Inner.FromApplicationDependencies(predicate);
        }

        public IImplementationTypeSelector FromAssemblyDependencies(Assembly assembly)
        {
            return Inner.FromAssemblyDependencies(assembly);
        }

        public IImplementationTypeSelector FromDependencyContext(DependencyContext context)
        {
            return Inner.FromDependencyContext(context);
        }

        public IImplementationTypeSelector FromDependencyContext(DependencyContext context, Func<Assembly, bool> predicate)
        {
            return Inner.FromDependencyContext(context, predicate);
        }
#endif

        public IImplementationTypeSelector FromAssemblyOf<T>()
        {
            return Inner.FromAssemblyOf<T>();
        }

        public IImplementationTypeSelector FromAssembliesOf(params Type[] types)
        {
            return Inner.FromAssembliesOf(types);
        }

        public IImplementationTypeSelector FromAssembliesOf(IEnumerable<Type> types)
        {
            return Inner.FromAssembliesOf(types);
        }

        public IImplementationTypeSelector FromAssemblies(params Assembly[] assemblies)
        {
            return Inner.FromAssemblies(assemblies);
        }

        public IImplementationTypeSelector FromAssemblies(IEnumerable<Assembly> assemblies)
        {
            return Inner.FromAssemblies(assemblies);
        }

        public IServiceTypeSelector AddClasses()
        {
            return Inner.AddClasses();
        }

        public IServiceTypeSelector AddClasses(bool publicOnly)
        {
            return Inner.AddClasses(publicOnly);
        }

        public IServiceTypeSelector AddClasses(Action<IImplementationTypeFilter> action)
        {
            return Inner.AddClasses(action);
        }

        public IServiceTypeSelector AddClasses(Action<IImplementationTypeFilter> action, bool publicOnly)
        {
            return Inner.AddClasses(action, publicOnly);
        }

        public ILifetimeSelector AsSelf()
        {
            return Inner.AsSelf();
        }

        public ILifetimeSelector As<T>()
        {
            return Inner.As<T>();
        }

        public ILifetimeSelector As(params Type[] types)
        {
            return Inner.As(types);
        }

        public ILifetimeSelector As(IEnumerable<Type> types)
        {
            return Inner.As(types);
        }

        public ILifetimeSelector AsImplementedInterfaces()
        {
            return Inner.AsImplementedInterfaces();
        }

        public ILifetimeSelector AsMatchingInterface()
        {
            return Inner.AsMatchingInterface();
        }

        public ILifetimeSelector AsMatchingInterface(Action<TypeInfo, IImplementationTypeFilter> action)
        {
            return Inner.AsMatchingInterface(action);
        }

        public ILifetimeSelector As(Func<Type, IEnumerable<Type>> selector)
        {
            return Inner.As(selector);
        }

        public IImplementationTypeSelector UsingAttributes()
        {
            return Inner.UsingAttributes();
        }

        public IServiceTypeSelector UsingRegistrationStrategy(RegistrationStrategy registrationStrategy)
        {
            return Inner.UsingRegistrationStrategy(registrationStrategy);
        }

        #endregion

        void ISelector.Populate(IServiceCollection services, RegistrationStrategy strategy)
        {
            if (!Lifetime.HasValue)
            {
                Lifetime = ServiceLifetime.Transient;
            }

            strategy = strategy ?? RegistrationStrategy.Append;

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

                    strategy.Apply(services, descriptor);
                }
            }
        }
    }
}
