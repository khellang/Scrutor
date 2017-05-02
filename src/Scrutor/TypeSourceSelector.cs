using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

#if DEPENDENCY_MODEL
using Microsoft.Extensions.DependencyModel;
#endif

namespace Scrutor
{
    internal class TypeSourceSelector : ITypeSourceSelector, ISelector
    {
        protected List<ISelector> Selectors { get; } = new List<ISelector>();

        /// <inheritdoc />
        public IImplementationTypeSelector FromAssemblyOf<T>()
        {
            return InternalFromAssembliesOf(new[] { typeof(T).GetTypeInfo() });
        }

#if NET451
        /// <inheritdoc />
        public IImplementationTypeSelector FromCallingAssembly()
        {
            return FromAssemblies(Assembly.GetCallingAssembly());
        }

        /// <inheritdoc />
        public IImplementationTypeSelector FromExecutingAssembly()
        {
            return FromAssemblies(Assembly.GetExecutingAssembly());
        }
#endif

#if DEPENDENCY_MODEL
        /// <inheritdoc />
        public IImplementationTypeSelector FromEntryAssembly()
        {
            return FromAssemblies(Assembly.GetEntryAssembly());
        }

        /// <inheritdoc />
        public IImplementationTypeSelector FromApplicationDependencies()
        {
            return FromDependencyContext(DependencyContext.Default);
        }

        /// <inheritdoc />
        public IImplementationTypeSelector FromDependencyContext(DependencyContext context)
        {
            Preconditions.NotNull(context, nameof(context));

            return FromAssemblies(context.RuntimeLibraries
                .SelectMany(library => library.GetDefaultAssemblyNames(context))
                .Select(Assembly.Load)
                .ToArray());
        }
#endif

        /// <inheritdoc />
        public IImplementationTypeSelector FromAssembliesOf(params Type[] types)
        {
            if (types == null)
            {
                throw new ArgumentNullException(nameof(types));
            }

            return InternalFromAssembliesOf(types.Select(x => x.GetTypeInfo()));
        }

        /// <inheritdoc />
        public IImplementationTypeSelector FromAssembliesOf(IEnumerable<Type> types)
        {
            if (types == null)
            {
                throw new ArgumentNullException(nameof(types));
            }

            return InternalFromAssembliesOf(types.Select(t => t.GetTypeInfo()));
        }

        private IImplementationTypeSelector InternalFromAssembliesOf(IEnumerable<TypeInfo> typeInfos)
        {
            return InternalFromAssemblies(typeInfos.Select(t => t.Assembly));
        }

        /// <inheritdoc />
        public IImplementationTypeSelector FromAssemblies(params Assembly[] assemblies)
        {
            if (assemblies == null)
            {
                throw new ArgumentNullException(nameof(assemblies));
            }

            return InternalFromAssemblies(assemblies);
        }

        /// <inheritdoc />
        public IImplementationTypeSelector FromAssemblies(IEnumerable<Assembly> assemblies)
        {
            if (assemblies == null)
            {
                throw new ArgumentNullException(nameof(assemblies));
            }

            return InternalFromAssemblies(assemblies);
        }

        private IImplementationTypeSelector InternalFromAssemblies(IEnumerable<Assembly> assemblies)
        {
            return AddSelector(assemblies.SelectMany(asm => asm.DefinedTypes).Select(x => x.AsType()));
        }

        void ISelector.Populate(IServiceCollection services, RegistrationStrategy registrationStrategy)
        {
            foreach (var selector in Selectors)
            {
                selector.Populate(services, registrationStrategy);
            }
        }

        public IServiceTypeSelector AddTypes(params Type[] types)
        {
            return AddSelector(types);
        }

        public IServiceTypeSelector AddTypes(IEnumerable<Type> types)
        {
            return AddSelector(types);
        }

        private IServiceTypeSelector AddSelector(IEnumerable<Type> types)
        {
            var selector = new ServiceTypeSelector(types);

            Selectors.Add(selector);

            return selector;
        }
    }
}
