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

        /// <inheritdoc />
        public IImplementationTypeSelector FromAssemblyOf<T>()
        {
            return ImplementationTypeSelector.FromAssemblyOf<T>();
        }

        /// <inheritdoc />
        public IImplementationTypeSelector FromAssembliesOf(params Type[] types)
        {
            return ImplementationTypeSelector.FromAssembliesOf(types);
        }

        /// <inheritdoc />
        public IImplementationTypeSelector FromAssembliesOf(IEnumerable<Type> types)
        {
            return ImplementationTypeSelector.FromAssembliesOf(types);
        }

        /// <inheritdoc />
        public IImplementationTypeSelector FromAssemblies(params Assembly[] assemblies)
        {
            return ImplementationTypeSelector.FromAssemblies(assemblies);
        }

        /// <inheritdoc />
        public IImplementationTypeSelector FromAssemblies(IEnumerable<Assembly> assemblies)
        {
            return ImplementationTypeSelector.FromAssemblies(assemblies);
        }

        /// <inheritdoc />
        public void AddFromAttributes()
        {
            ImplementationTypeSelector.AddClasses().UsingAttributes();
        }

        /// <inheritdoc />
        public void AddFromAttributes(bool publicOnly)
        {
            ImplementationTypeSelector.AddClasses(publicOnly).UsingAttributes();
        }

        /// <inheritdoc />
        public IServiceTypeSelector AddClasses()
        {
            return ImplementationTypeSelector.AddClasses();
        }

        /// <inheritdoc />
        public IServiceTypeSelector AddClasses(bool publicOnly)
        {
            return ImplementationTypeSelector.AddClasses(publicOnly);
        }

        /// <inheritdoc />
        public IServiceTypeSelector AddClasses(Action<IImplementationTypeFilter> action)
        {
            return ImplementationTypeSelector.AddClasses(action);
        }

        /// <inheritdoc />
        public IServiceTypeSelector AddClasses(Action<IImplementationTypeFilter> action, bool publicOnly)
        {
            return ImplementationTypeSelector.AddClasses(action, publicOnly);
        }

        /// <inheritdoc />
        public void AddFromAttributes(Action<IImplementationTypeFilter> action)
        {
            ImplementationTypeSelector.AddClasses(action).UsingAttributes();
        }

        /// <inheritdoc />
        public void AddFromAttributes(Action<IImplementationTypeFilter> action, bool publicOnly)
        {
            ImplementationTypeSelector.AddClasses(action, publicOnly).UsingAttributes();
        }

        /// <inheritdoc />
        public ILifetimeSelector AsSelf()
        {
            return As(t => new[] { t });
        }

        /// <inheritdoc />
        public ILifetimeSelector As<T>()
        {
            return As(typeof(T));
        }

        /// <inheritdoc />
        public ILifetimeSelector As(params Type[] types)
        {
            if (types == null)
            {
                throw new ArgumentNullException(nameof(types));
            }

            return As(types.AsEnumerable());
        }

        /// <inheritdoc />
        public ILifetimeSelector As(IEnumerable<Type> types)
        {
            if (types == null)
            {
                throw new ArgumentNullException(nameof(types));
            }

            return AddSelector(Types.Select(t => new TypeMap(t, types)));
        }

        /// <inheritdoc />
        public ILifetimeSelector AsImplementedInterfaces()
        {
            return AsTypeInfo(t => t.ImplementedInterfaces);
        }

        /// <inheritdoc />
        public ILifetimeSelector As(Func<Type, IEnumerable<Type>> selector)
        {
            if (selector == null)
            {
                throw new ArgumentNullException(nameof(selector));
            }

            return AddSelector(Types.Select(t => new TypeMap(t, selector(t))));
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

        private ILifetimeSelector AddSelector(IEnumerable<TypeMap> types)
        {
            var lifetimeSelector = new LifetimeSelector(this, types);

            Selectors.Add(lifetimeSelector);

            return lifetimeSelector;
        }

        private ILifetimeSelector AsTypeInfo(Func<TypeInfo, IEnumerable<Type>> selector)
        {
            return As(t => selector(t.GetTypeInfo()));
        }

        /// <inheritdoc />
        public ILifetimeSelector AsMatchingInterface()
        {
            return AsMatchingInterface(null);
        }

        /// <inheritdoc />
        public ILifetimeSelector AsMatchingInterface(Action<TypeInfo, IImplementationTypeFilter> action)
        {
            return AsTypeInfo(t => t.FindMatchingInterface(action));
        }

        /// <inheritdoc />
        public IImplementationTypeSelector UsingAttributes()
        {
            var selector = new AttributeSelector(Types);

            Selectors.Add(selector);

            return ImplementationTypeSelector;
        }
    }
}
