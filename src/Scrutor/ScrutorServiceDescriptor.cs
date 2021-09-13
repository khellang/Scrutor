using Microsoft.Extensions.DependencyInjection;

using Scrutor.Activation;

using System;

namespace Scrutor
{
    /// <summary>
    /// Scrutor specific <see cref="ServiceDescriptor"/>
    /// </summary>
    public class ScrutorServiceDescriptor : ServiceDescriptor
    {
        public ScrutorServiceDescriptor(Type serviceType, object instance) 
            : base(serviceType, instance)
        {
        }

        public ScrutorServiceDescriptor(Type serviceType, Type implementationType, ServiceLifetime lifetime)
            : base(serviceType, implementationType, lifetime)
        {
        }

        public ScrutorServiceDescriptor(Type serviceType, Func<IServiceProvider, object> factory, ServiceLifetime lifetime) 
            : base(serviceType, factory, lifetime)
        {
        }

        /// <summary>
        /// Bound <see cref="IServiceActivator"/>
        /// </summary>
        public IServiceActivator? ServiceActivator { get; internal set; }
    }
}
