using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace Scrutor
{
    internal class ServiceTypeSelector : IServiceTypeSelector, ISelector
    {
        public ServiceTypeSelector(IImplementationTypeSelector implementationTypeSelector, IEnumerable<Type> types)
        {
            ImplementationTypeSelector = implementationTypeSelector;
            Types = types;
            Selectors = new List<ISelector>();
        }

        private IEnumerable<Type> Types { get; }

        private List<ISelector> Selectors { get; }

        private IImplementationTypeSelector ImplementationTypeSelector { get; }

        public IImplementationTypeSelector FromAssemblyOf<T>()
        {
            return ImplementationTypeSelector.FromAssemblyOf<T>();
        }

        public IImplementationTypeSelector FromAssembliesOf(params Type[] types)
        {
            return ImplementationTypeSelector.FromAssembliesOf(types);
        }

        public IImplementationTypeSelector FromAssembliesOf(IEnumerable<Type> types)
        {
            return ImplementationTypeSelector.FromAssembliesOf(types);
        }

        public IImplementationTypeSelector FromAssemblies(params Assembly[] assemblies)
        {
            return ImplementationTypeSelector.FromAssemblies(assemblies);
        }

        public IImplementationTypeSelector FromAssemblies(IEnumerable<Assembly> assemblies)
        {
            return ImplementationTypeSelector.FromAssemblies(assemblies);
        }

        public void AddFromAttributes()
        {
            ImplementationTypeSelector.AddClasses().UsingAttributes();
        }

        public void AddFromAttributes(bool publicOnly)
        {
            ImplementationTypeSelector.AddClasses(publicOnly).UsingAttributes();
        }

        public IServiceTypeSelector AddClasses()
        {
            return ImplementationTypeSelector.AddClasses();
        }

        public IServiceTypeSelector AddClasses(bool publicOnly)
        {
            return ImplementationTypeSelector.AddClasses(publicOnly);
        }

        public IServiceTypeSelector AddClasses(Action<IImplementationTypeFilter> action)
        {
            return ImplementationTypeSelector.AddClasses(action);
        }

        public IServiceTypeSelector AddClasses(Action<IImplementationTypeFilter> action, bool publicOnly)
        {
            return ImplementationTypeSelector.AddClasses(action, publicOnly);
        }

        public void AddFromAttributes(Action<IImplementationTypeFilter> action)
        {
            ImplementationTypeSelector.AddClasses(action).UsingAttributes();
        }

        public void AddFromAttributes(Action<IImplementationTypeFilter> action, bool publicOnly)
        {
            ImplementationTypeSelector.AddClasses(action, publicOnly).UsingAttributes();
        }

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
            if (types == null)
            {
                throw new ArgumentNullException(nameof(types));
            }

            return As(types.AsEnumerable());
        }

        public ILifetimeSelector As(IEnumerable<Type> types)
        {
            if (types == null)
            {
                throw new ArgumentNullException(nameof(types));
            }

            return AddSelector(Types.Select(t => Tuple.Create(t, types)));
        }

        public ILifetimeSelector AsImplementedInterfaces()
        {
            return AsTypeInfo(t => t.ImplementedInterfaces);
        }

        public ILifetimeSelector As(Func<Type, IEnumerable<Type>> selector)
        {
            if (selector == null)
            {
                throw new ArgumentNullException(nameof(selector));
            }

            return AddSelector(Types.Select(t => Tuple.Create(t, selector(t))));
        }

        void ISelector.Populate(IServiceCollection services)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (Selectors.Count == 0)
            {
                AsSelf();
            }

            foreach (var selector in Selectors)
            {
                selector.Populate(services);
            }
        }

        private ILifetimeSelector AddSelector(IEnumerable<Tuple<Type, IEnumerable<Type>>> types)
        {
            var lifetimeSelector = new LifetimeSelector(this, types);

            Selectors.Add(lifetimeSelector);

            return lifetimeSelector;
        }

        private ILifetimeSelector AsTypeInfo(Func<TypeInfo, IEnumerable<Type>> selector)
        {
            return As(t => selector(t.GetTypeInfo()));
        }

        public ILifetimeSelector AsMatchingInterface()
        {
            return AsMatchingInterface(null);
        }

        public ILifetimeSelector AsMatchingInterface(Action<TypeInfo, IImplementationTypeFilter> action)
        {
            return AsTypeInfo(t => t.FindMatchingInterface(action));
        }

        public IImplementationTypeSelector UsingAttributes()
        {
            var selector = new AttributeSelector(Types);

            Selectors.Add(selector);

            return ImplementationTypeSelector;
        }
    }
}
