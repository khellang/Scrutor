using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Framework.DependencyInjection;

namespace Microsoft.Extensions.DependencyInjection.Scanning
{
    internal class AssemblySelector : IAssemblySelector
    {
        public AssemblySelector()
        {
            Selectors = new List<ImplementationTypeSelector>();
        }

        private List<ImplementationTypeSelector> Selectors { get; }

        public IImplementationTypeSelector FromAssemblyOf<T>()
        {
            return FromAssembliesOf(typeof(T));
        }

        public IImplementationTypeSelector FromAssembliesOf(params Type[] types)
        {
            if (types == null)
            {
                throw new ArgumentNullException(nameof(types));
            }

            return FromAssembliesOf(types.AsEnumerable());
        }

        public IImplementationTypeSelector FromAssembliesOf(IEnumerable<Type> types)
        {
            if (types == null)
            {
                throw new ArgumentNullException(nameof(types));
            }

            return FromAssemblies(types.Select(t => t.GetTypeInfo().Assembly));
        }

        public IImplementationTypeSelector FromAssemblies(params Assembly[] assemblies)
        {
            if (assemblies == null)
            {
                throw new ArgumentNullException(nameof(assemblies));
            }

            return FromAssemblies(assemblies.AsEnumerable());
        }

        public IImplementationTypeSelector FromAssemblies(IEnumerable<Assembly> assemblies)
        {
            if (assemblies == null)
            {
                throw new ArgumentNullException(nameof(assemblies));
            }

            return AddSelector(assemblies.SelectMany(asm => asm.ExportedTypes));
        }

        internal void Populate(IServiceCollection services)
        {
            foreach (var selector in Selectors)
            {
                selector.Populate(services);
            }
        }

        private IImplementationTypeSelector AddSelector(IEnumerable<Type> types)
        {
            var selector = new ImplementationTypeSelector(types);

            Selectors.Add(selector);

            return selector;
        }
    }
}