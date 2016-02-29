using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace Scrutor
{
    internal class ImplementationTypeSelector : IImplementationTypeSelector, ISelector
    {
        public ImplementationTypeSelector(IAssemblySelector assemblySelector, IEnumerable<Type> types)
        {
            AssemblySelector = assemblySelector;
            Types = types;
            Selectors = new List<ISelector>();
        }

        private IEnumerable<Type> Types { get; }

        private List<ISelector> Selectors { get; }

        private IAssemblySelector AssemblySelector { get; }

        public IImplementationTypeSelector FromAssemblyOf<T>()
        {
            return AssemblySelector.FromAssemblyOf<T>();
        }

        public IImplementationTypeSelector FromAssembliesOf(params Type[] types)
        {
            return AssemblySelector.FromAssembliesOf(types);
        }

        public IImplementationTypeSelector FromAssembliesOf(IEnumerable<Type> types)
        {
            return AssemblySelector.FromAssembliesOf(types);
        }

        public IImplementationTypeSelector FromAssemblies(params Assembly[] assemblies)
        {
            return AssemblySelector.FromAssemblies(assemblies);
        }

        public IImplementationTypeSelector FromAssemblies(IEnumerable<Assembly> assemblies)
        {
            return AssemblySelector.FromAssemblies(assemblies);
        }

        public void AddFromAttributes()
        {
            AddFromAttributes(publicOnly: false);
        }

        public void AddFromAttributes(bool publicOnly)
        {
            Selectors.Add(new AttributeSelector(Types.Where(t => t.IsNonAbstractClass(publicOnly))));
        }

        public IServiceTypeSelector AddClasses()
        {
            return AddClasses(publicOnly: false);
        }

        public IServiceTypeSelector AddClasses(bool publicOnly)
        {
            return AddSelector(Types.Where(t => t.IsNonAbstractClass(publicOnly)));
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

            return AddFilter(Types.Where(t => t.IsNonAbstractClass(publicOnly)), action);
        }

        public void Populate(IServiceCollection services)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

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
            var selector = new ServiceTypeSelector(this, types);

            Selectors.Add(selector);

            return selector;
        }
    }
}
