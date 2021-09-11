using Microsoft.Extensions.DependencyInjection;

using System;

namespace Scrutor.Activation
{
    /// <summary>
    /// API in order to create services based on <see cref="IServiceCollection"/>
    /// </summary>
    public interface IServiceActivator
    {
        /// <summary>
        /// Instantiate a type with constructor arguments provided directly and/or from an <see cref="IServiceProvider"/>
        /// </summary>
        /// <param name="provider">The service provider used to resolve dependencies</param>
        /// <param name="type">The type to activate</param>
        /// <param name="arguments"> Constructor arguments not provided by the provider.</param>
        /// <returns>An activated object of <paramref name="type"/></returns>
        object CreateInstance(IServiceProvider provider, Type type, params object[] arguments);
    }
}
