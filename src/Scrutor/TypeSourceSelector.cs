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
        private List<ISelector> Selectors { get; } = new List<ISelector>();

        /// <inheritdoc />
        public IImplementationTypeSelector FromAssemblyOf<T>()
        {
            return InternalFromAssembliesOf(new[] { typeof(T).GetTypeInfo() });
        }

#if NET451
        public IImplementationTypeSelector FromCallingAssembly()
        {
            return FromAssemblies(Assembly.GetCallingAssembly());
        }

        public IImplementationTypeSelector FromExecutingAssembly()
        {
            return FromAssemblies(Assembly.GetExecutingAssembly());
        }
#endif

#if DEPENDENCY_MODEL
        public IImplementationTypeSelector FromEntryAssembly()
        {
            return FromAssemblies(Assembly.GetEntryAssembly());
        }

        public IImplementationTypeSelector FromApplicationDependencies()
        {
            try
            {
                return FromDependencyContext(DependencyContext.Default);
            }
            catch
            {
                // Something went wrong when loading the DependencyContext, fall
                // back to loading all referenced assemblies of the entry assembly...
                return FromAssemblyDependencies(Assembly.GetEntryAssembly());
            }
        }

        public IImplementationTypeSelector FromDependencyContext(DependencyContext context)
        {
            Preconditions.NotNull(context, nameof(context));

            return FromAssemblies(context.RuntimeLibraries
                .SelectMany(library => library.GetDefaultAssemblyNames(context))
                .Select(Assembly.Load)
                .ToArray());
        }

        public IImplementationTypeSelector FromAssemblyDependencies(Assembly assembly)
        {
            Preconditions.NotNull(assembly, nameof(assembly));

            var assemblies = new List<Assembly> { assembly };

            try
            {
                var dependencyNames = assembly.GetReferencedAssemblies();

                foreach (var dependencyName in dependencyNames)
                {
                    try
                    {
                        // Try to load the referenced assembly...
                        assemblies.Add(Assembly.Load(dependencyName));
                    }
                    catch
                    {
                        // Failed to load assembly. Skip it.
                    }
                }

                return FromAssemblies(assemblies);
            }
            catch
            {
                return FromAssemblies(assemblies);
            }
        }
#endif

        public IImplementationTypeSelector FromAssembliesOf(params Type[] types)
        {
            Preconditions.NotNull(types, nameof(types));

            return InternalFromAssembliesOf(types.Select(x => x.GetTypeInfo()));
        }

        public IImplementationTypeSelector FromAssembliesOf(IEnumerable<Type> types)
        {
            Preconditions.NotNull(types, nameof(types));

            return InternalFromAssembliesOf(types.Select(t => t.GetTypeInfo()));
        }

        public IImplementationTypeSelector FromAssemblies(params Assembly[] assemblies)
        {
            Preconditions.NotNull(assemblies, nameof(assemblies));

            return InternalFromAssemblies(assemblies);
        }

        public IImplementationTypeSelector FromAssemblies(IEnumerable<Assembly> assemblies)
        {
            Preconditions.NotNull(assemblies, nameof(assemblies));

            return InternalFromAssemblies(assemblies);
        }

        public IServiceTypeSelector AddTypes(params Type[] types)
        {
            Preconditions.NotNull(types, nameof(types));

            var selector = new ImplementationTypeSelector(this, types);

            Selectors.Add(selector);

            return selector.AddClasses();
        }

        public IServiceTypeSelector AddTypes(IEnumerable<Type> types)
        {
            Preconditions.NotNull(types, nameof(types));

            var selector = new ImplementationTypeSelector(this, types);

            Selectors.Add(selector);

            return selector.AddClasses();
        }

        void ISelector.Populate(IServiceCollection services, RegistrationStrategy registrationStrategy)
        {
            foreach (var selector in Selectors)
            {
                selector.Populate(services, registrationStrategy);
            }
        }

        private IImplementationTypeSelector InternalFromAssembliesOf(IEnumerable<TypeInfo> typeInfos)
        {
            return InternalFromAssemblies(typeInfos.Select(t => t.Assembly));
        }

        private IImplementationTypeSelector InternalFromAssemblies(IEnumerable<Assembly> assemblies)
        {
            return AddSelector(assemblies.SelectMany(asm => asm.DefinedTypes).Select(x => x.AsType()));
        }

        private IImplementationTypeSelector AddSelector(IEnumerable<Type> types)
        {
            var selector = new ImplementationTypeSelector(this, types);

            Selectors.Add(selector);

            return selector;
        }
    }
}
