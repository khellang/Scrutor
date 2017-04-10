using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;

namespace Scrutor
{
    internal class ImplementationTypeSelector : TypeSourceSelector, IImplementationTypeSelector, ISelector
    {
        public ImplementationTypeSelector(IEnumerable<Type> types)
        {
            Types = types;
        }

        protected IEnumerable<Type> Types { get; }

        public void AddFromAttributes()
        {
            AddFromAttributes(publicOnly: false);
        }

        public void AddFromAttributes(bool publicOnly)
        {
            var classes = GetNonAbstractClasses(publicOnly);

            Selectors.Add(new AttributeSelector(classes));
        }

        public void AddFromAttributes(Action<IImplementationTypeFilter> action)
        {
            AddFromAttributes(action, publicOnly: false);
        }

        public void AddFromAttributes(Action<IImplementationTypeFilter> action, bool publicOnly)
        {
            if (action == null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            var classes = GetNonAbstractClasses(publicOnly);

            var filter = new ImplementationTypeFilter(classes);

            action(filter);

            var selector = new AttributeSelector(filter.Types);

            Selectors.Add(selector);
        }

        public IServiceTypeSelector AddClasses()
        {
            return AddClasses(publicOnly: false);
        }

        public IServiceTypeSelector AddClasses(bool publicOnly)
        {
            var classes = GetNonAbstractClasses(publicOnly);

            return AddSelector(classes);
        }

        public IServiceTypeSelector AddClasses(Action<IImplementationTypeFilter> action)
        {
            return AddClasses(action, publicOnly: false);
        }

        public IServiceTypeSelector AddClasses(Action<IImplementationTypeFilter> action, bool publicOnly)
        {
            if (action == null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            var classes = GetNonAbstractClasses(publicOnly);

            var filter = new ImplementationTypeFilter(classes);

            action(filter);

            return AddSelector(filter.Types);
        }

        void ISelector.Populate(IServiceCollection services, RegistrationStrategy registrationStrategy)
        {
            if (Selectors.Count == 0)
            {
                AddClasses();
            }

            foreach (var selector in Selectors)
            {
                selector.Populate(services, registrationStrategy);
            }
        }

        private IServiceTypeSelector AddSelector(IEnumerable<Type> types)
        {
            var selector = new ServiceTypeSelector(types);

            Selectors.Add(selector);

            return selector;
        }

        private IEnumerable<Type> GetNonAbstractClasses(bool publicOnly)
        {
            return Types.Where(t => t.IsNonAbstractClass(publicOnly));
        }
    }
}
