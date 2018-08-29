using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyModel;

namespace Scrutor
{
    internal class ServiceTypeSelector : IServiceTypeSelector, ISelector
    {
        public ServiceTypeSelector(IImplementationTypeSelector inner, IEnumerable<Type> types)
        {
            Inner = inner;
            Types = types;
        }

        private IImplementationTypeSelector Inner { get; }

        private IEnumerable<Type> Types { get; }

        private List<ISelector> Selectors { get; } = new List<ISelector>();

        private RegistrationStrategy RegistrationStrategy { get; set; }

        public ILifetimeSelector AsSelf()
        {
            return As(t => new[] { t });
        }

        public ILifetimeSelector As<T>()
        {
            return As(typeof(T));
        }

        public ILifetimeSelector As(params Type[] types)
        {
            Preconditions.NotNull(types, nameof(types));

            return As(types.AsEnumerable());
        }

        public ILifetimeSelector As(IEnumerable<Type> types)
        {
            Preconditions.NotNull(types, nameof(types));

            return AddSelector(Types.Select(t => new TypeMap(t, types)), Enumerable.Empty<TypeFactoryMap>());
        }

        public ILifetimeSelector AsImplementedInterfaces()
        {
            return AsTypeInfo(t => t.ImplementedInterfaces
                .Where(x => x.HasMatchingGenericArity(t))
                .Select(x => x.GetRegistrationType(t)));
        }

        public ILifetimeSelector AsSelfWithInterfaces()
        {
            Func<TypeInfo, IEnumerable<Type>> selector = info =>
                info.ImplementedInterfaces
                    .Where(x => x.HasMatchingGenericArity(info))
                    .Select(x => x.GetRegistrationType(info));

            return AddSelector(
                Types.Select(t => new TypeMap(t, new[] { t })),
                Types.Select(t => new TypeFactoryMap(x => x.GetRequiredService(t), selector(t.GetTypeInfo()))));
        }

        public ILifetimeSelector AsMatchingInterface()
        {
            return AsMatchingInterface(null);
        }

        public ILifetimeSelector AsMatchingInterface(Action<TypeInfo, IImplementationTypeFilter> action)
        {
            return AsTypeInfo(t => t.FindMatchingInterface(action));
        }

        public ILifetimeSelector As(Func<Type, IEnumerable<Type>> selector)
        {
            Preconditions.NotNull(selector, nameof(selector));

            return AddSelector(Types.Select(t => new TypeMap(t, selector(t))), Enumerable.Empty<TypeFactoryMap>());
        }

        public IImplementationTypeSelector UsingAttributes()
        {
            var selector = new AttributeSelector(Types);

            Selectors.Add(selector);

            return this;
        }

        public IServiceTypeSelector UsingRegistrationStrategy(RegistrationStrategy registrationStrategy)
        {
            Preconditions.NotNull(registrationStrategy, nameof(registrationStrategy));

            RegistrationStrategy = registrationStrategy;
            return this;
        }

        #region Chain Methods

        public IImplementationTypeSelector FromCallingAssembly()
        {
            return Inner.FromCallingAssembly();
        }

        public IImplementationTypeSelector FromExecutingAssembly()
        {
            return Inner.FromExecutingAssembly();
        }

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

        #endregion

        void ISelector.Populate(IServiceCollection services, RegistrationStrategy registrationStrategy)
        {
            if (Selectors.Count == 0)
            {
                AsSelf();
            }

            var strategy = RegistrationStrategy ?? registrationStrategy;

            foreach (var selector in Selectors)
            {
                selector.Populate(services, strategy);
            }
        }

        private ILifetimeSelector AddSelector(IEnumerable<TypeMap> types, IEnumerable<TypeFactoryMap> factories)
        {
            var selector = new LifetimeSelector(this, types, factories);

            Selectors.Add(selector);

            return selector;
        }

        private ILifetimeSelector AsTypeInfo(Func<TypeInfo, IEnumerable<Type>> selector)
        {
            return As(t => selector(t.GetTypeInfo()));
        }
    }
}
