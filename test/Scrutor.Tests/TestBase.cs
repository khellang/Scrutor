using System;
using Microsoft.Extensions.DependencyInjection;

namespace Scrutor.Tests
{
    public class TestBase
    {
        protected static IServiceProvider ConfigureProvider(Action<IServiceCollection> configure)
        {
            var services = new ServiceCollection();

            configure(services);

            return services.BuildServiceProvider();
        }
    }
}
