using System;

namespace Scrutor.Static
{
    public interface ICompiledAssemblySelector
    {
        /// <summary>
        /// Will scan for types from this assembly at compile time.
        /// </summary>
        ICompiledImplementationTypeSelector FromAssembly();

        /// <summary>
        /// Will scan for types from all metadata assembly at compile time.
        /// </summary>
        ICompiledImplementationTypeSelector FromAssemblies();

        /// <summary>
        /// Will load and scan from given types assembly
        /// </summary>
        ICompiledImplementationTypeSelector FromAssemblyDependenciesOf<T>();

        /// <summary>
        /// Will load and scan from given types assembly
        /// </summary>
        ICompiledImplementationTypeSelector FromAssemblyDependenciesOf(Type type);

        /// <summary>
        /// Will scan for types from the assembly of type <typeparamref name="T" />.
        /// </summary>
        /// <typeparam name="T">The type in which assembly that should be scanned.</typeparam>
        ICompiledImplementationTypeSelector FromAssemblyOf<T>();

        /// <summary>
        /// Will scan for types from the assembly of type.
        /// </summary>
        ICompiledImplementationTypeSelector FromAssemblyOf(Type type);
    }
}
