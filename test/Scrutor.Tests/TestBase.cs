using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.Extensions.DependencyInjection;

using Scrutor.Activation;

namespace Scrutor.Tests
{
    public class TestBase : IDisposable
    {
        public static IEnumerable<object[]> _srKnownServiceActivators 
            => typeof(TypeSourceSelector).Assembly
                .GetTypes()
                .Where(t => !t.IsAbstract && typeof(IServiceActivator).IsAssignableFrom(t))
                .Select(t => new[] { Activator.CreateInstance(t, true) as IServiceActivator })
                .ToList();


        protected static ServiceProvider ConfigureProvider(Action<IServiceCollection> configure)
        {
            var services = new ServiceCollection();

            configure(services);

            return services.BuildServiceProvider();
        }

        protected static void _sExecuteForAllKnownActivators(Action<IServiceActivator> test)
        {
            foreach (IServiceActivator activator in _srKnownServiceActivators.Select(x => x[0] as IServiceActivator))
                test(activator);
        }

        public void Dispose()
        {
            ScrutorContext.Invalidate();
        }
    }
}
