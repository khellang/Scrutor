using Microsoft.Extensions.DependencyInjection;
using System;

namespace Scrutor.Activation
{
    /// <summary>
    /// Implementation of the <see cref="IServiceProvider"/> based on Microsoft.Extensions.DependencyInjection package <see cref="ActivatorUtilities"/>
    /// </summary>
    public class DefaultServiceActivator : IServiceActivator
    {
        /// <summary>
        /// <see cref="IServiceActivator.CreateInstance(IServiceProvider, Type, object[])"/>
        /// </summary>
        public object CreateInstance(IServiceProvider provider, Type type, params object[] arguments) => ActivatorUtilities.CreateInstance(provider, type, arguments);
    }
}
