using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace Scrutor
{
    internal class ServiceTypeSelector : ImplementationTypeSelector, IServiceTypeSelector, ISelector
    {
        public ServiceTypeSelector(IEnumerable<Type> types) : base(types)
        {
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
        public ILifetimeSelector As(Func<Type, IEnumerable<Type>> selector)
        {
            if (selector == null)
            {
                throw new ArgumentNullException(nameof(selector));
            }

            return AddSelector(Types.Select(t => new TypeMap(t, selector(t))));
        }

        /// <inheritdoc />
        public IImplementationTypeSelector UsingAttributes()
        {
            var selector = new AttributeSelector(Types);

            Selectors.Add(selector);

            return this;
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
            var selector = new LifetimeSelector(Types, types);

            Selectors.Add(selector);

            return selector;
        }

        private ILifetimeSelector AsTypeInfo(Func<TypeInfo, IEnumerable<Type>> selector)
        {
            return As(t => selector(t.GetTypeInfo()));
        }
    }
}
