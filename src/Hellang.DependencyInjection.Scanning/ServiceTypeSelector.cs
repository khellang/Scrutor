using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Framework.DependencyInjection;

namespace Microsoft.Extensions.DependencyInjection.Scanning
{
    internal class ServiceTypeSelector : IServiceTypeSelector
    {
        public ServiceTypeSelector(IEnumerable<Type> types)
        {
            Types = types;
            Selectors = new List<LifetimeSelector>();
        }

        private IEnumerable<Type> Types { get; }

        private List<LifetimeSelector> Selectors { get; }

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

        internal void Populate(IServiceCollection services)
        {
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
            var lifetimeSelector = new LifetimeSelector(types);

            Selectors.Add(lifetimeSelector);

            return lifetimeSelector;
        }

        private ILifetimeSelector AsTypeInfo(Func<TypeInfo, IEnumerable<Type>> selector)
        {
            return As(t => selector(t.GetTypeInfo()));
        }
    }
}