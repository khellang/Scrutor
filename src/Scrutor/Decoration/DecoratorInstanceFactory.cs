using Microsoft.Extensions.DependencyInjection;
using System;

namespace Scrutor.Decoration
{
    internal static class DecoratorInstanceFactory
    {
        internal static Func<IServiceProvider, object> Default(Type decorated, Type decorator) =>
            (serviceProvider) =>
            {
                var instanceToDecorate = serviceProvider.GetRequiredService(decorated);
                return ActivatorUtilities.CreateInstance(serviceProvider, decorator, instanceToDecorate);
            };

        internal static Func<IServiceProvider, object> Custom(Type decorated, Func<object, IServiceProvider, object> creationFactory) =>
            (serviceProvider) =>
            {
                var instanceToDecorate = serviceProvider.GetRequiredService(decorated);
                return creationFactory(instanceToDecorate, serviceProvider);
            };
    }
}
