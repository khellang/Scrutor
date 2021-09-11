using System;

namespace Scrutor.Activation
{
    /// <summary>
    /// Extensions for <see cref="IServiceActivator"/>
    /// </summary>
    public static class ServiceActivatorExtensions
    {
        /// <summary>
        /// Instantiate a type with constructor arguments provided directly and/or from an <see cref="IServiceProvider"/>
        /// </summary>
        /// <typeparam name="T">The type to activate</typeparam>
        /// <param name="this">This</param>
        /// <param name="provider">The service provider used to resolve dependencies</param>
        /// <param name="arguments"> Constructor arguments not provided by the provider.</param>
        /// <returns>An activated object of <typeparamref name="T"/></returns>
        public static T CreateInstance<T>(this IServiceActivator @this, IServiceProvider provider, params object[] arguments)
            => (T) @this.CreateInstance(provider, typeof(T), arguments);

        /// <summary>
        /// Retrieve an instance of the given type from the service provider. If one is not found then instantiate it directly.
        /// </summary>
        /// <param name="this"><see cref="IServiceActivator"/></param>
        /// <param name="provider">The service provider used to resolve dependencies</param>
        /// <param name="type">The type to activate</param>
        /// <returns>The resolved service or created instance</returns>
        public static object GetServiceOrCreateInstance(this IServiceActivator @this, IServiceProvider provider, Type type)
            => provider.GetService(type) ?? @this.CreateInstance(provider, type);

        /// <summary>
        /// Retrieve an instance of the given type from the service provider. If one is not found then instantiate it directly.
        /// </summary>
        /// <typeparam name="T">The type to activate</typeparam>
        /// <param name="this"><see cref="IServiceActivator"/></param>
        /// <param name="provider">The service provider used to resolve dependencies</param>
        /// <returns>The resolved service or created instance</returns>
        public static T GetServiceOrCreateInstance<T>(this IServiceActivator @this, IServiceProvider provider)
            => (T) @this.GetServiceOrCreateInstance(provider, typeof(T));
    }
}
