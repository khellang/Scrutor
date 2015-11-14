using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Framework.DependencyInjection;

namespace Microsoft.Extensions.DependencyInjection.Scanning
{
    internal class ImplementationTypeSelector : IImplementationTypeSelector
    {
        public ImplementationTypeSelector(IEnumerable<Type> types)
        {
            Types = types;
            Selectors = new List<ServiceTypeSelector>();
        }

        private IEnumerable<Type> Types { get; }

        private List<ServiceTypeSelector> Selectors { get; }

        public IServiceTypeSelector AddClasses()
        {
            return AddSelector(Types.Where(t => t.IsNonAbstractClass()));
        }

        public IServiceTypeSelector AddClasses(Action<IImplementationTypeFilter> action)
        {
            if (action == null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            return AddFilter(Types.Where(t => t.IsNonAbstractClass()), action);
        }

        internal void Populate(IServiceCollection services)
        {
            foreach (var selector in Selectors)
            {
                selector.Populate(services);
            }
        }

        private IServiceTypeSelector AddFilter(IEnumerable<Type> types, Action<IImplementationTypeFilter> action)
        {
            var filter = new ImplementationTypeFilter(types);

            action(filter);

            return AddSelector(filter.Types);
        }

        private IServiceTypeSelector AddSelector(IEnumerable<Type> types)
        {
            var selector = new ServiceTypeSelector(types);

            Selectors.Add(selector);

            return selector;
        }
    }
}