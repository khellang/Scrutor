using Scrutor;

namespace Scrutor.Static
{
    public interface ICompiledServiceTypeSelector
    {
        /// <summary>
        /// Registers each matching concrete type as itself.
        /// </summary>
        ICompiledLifetimeSelector AsSelf();

        /// <summary>
        /// Registers each matching concrete type as <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The type to register as.</typeparam>
        ICompiledLifetimeSelector As<T>();

        /// <summary>
        /// Registers each matching concrete type as all of its implemented interfaces.
        /// </summary>
        ICompiledLifetimeSelector AsImplementedInterfaces();

        /// <summary>
        /// Registers each matching concrete type as all of its implemented interfaces, by returning an instance of the main type
        /// </summary>
        ICompiledLifetimeSelector AsSelfWithInterfaces();

        /// <summary>
        /// Registers the type with the first found matching interface name.  (e.g. ClassName is matched to IClassName)
        /// </summary>
        ICompiledLifetimeSelector AsMatchingInterface();

        /// <summary>
        /// Registers each matching concrete type according to their ServiceDescriptorAttribute.
        /// </summary>
        ICompiledLifetimeSelector UsingAttributes();
    }
}
