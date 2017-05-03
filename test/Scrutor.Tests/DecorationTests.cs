using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using System.Linq;

namespace Scrutor.Tests
{
    public class DecorationTests : TestBase
    {
        [Fact]
        public void CanDecorateType()
        {
            var provider = ConfigureProvider(services =>
            {
                services.AddSingleton<IDecoratedService, Decorated>();

                services.Decorate<IDecoratedService, Decorator>();
            });

            var instance = provider.GetRequiredService<IDecoratedService>();

            var decorator = Assert.IsType<Decorator>(instance);

            Assert.IsType<Decorated>(decorator.Inner);
        }

        [Fact]
        public void CanDecorateMultipleLevels()
        {
            var provider = ConfigureProvider(services =>
            {
                services.AddSingleton<IDecoratedService, Decorated>();

                services.Decorate<IDecoratedService, Decorator>();
                services.Decorate<IDecoratedService, Decorator>();
            });

            var instance = provider.GetRequiredService<IDecoratedService>();

            var decorator = Assert.IsType<Decorator>(instance);
            var outerDecorator = Assert.IsType<Decorator>(decorator.Inner);

            Assert.IsType<Decorated>(outerDecorator.Inner);
        }

        [Fact]
        public void CanDecorateDifferentServices()
        {
            var provider = ConfigureProvider(services =>
            {
                services.AddSingleton<IDecoratedService, Decorated>();
                services.AddSingleton<IDecoratedService, OtherDecorated>();

                services.Decorate<IDecoratedService, Decorator>();
            });

            var instances = provider
                .GetRequiredService<IEnumerable<IDecoratedService>>()
                .ToArray();

            Assert.Equal(2, instances.Length);
            Assert.All(instances, x => Assert.IsType<Decorator>(x));
        }

        [Fact]
        public void ShouldReplaceExistingServiceDescriptor()
        {
            var services = new ServiceCollection();

            services.AddSingleton<IDecoratedService, Decorated>();

            services.Decorate<IDecoratedService, Decorator>();

            var descriptor = services.GetDescriptor<IDecoratedService>();

            Assert.Equal(typeof(IDecoratedService), descriptor.ServiceType);
            Assert.NotNull(descriptor.ImplementationFactory);
        }

        [Fact]
        public void CanInjectServicesIntoDecoratedType()
        {
            var provider = ConfigureProvider(services =>
            {
                services.AddSingleton<IService, SomeRandomService>();
                services.AddSingleton<IDecoratedService, Decorated>();

                services.Decorate<IDecoratedService, Decorator>();
            });

            var validator = provider.GetRequiredService<IService>();

            var instance = provider.GetRequiredService<IDecoratedService>();

            var decorator = Assert.IsType<Decorator>(instance);
            var decorated = Assert.IsType<Decorated>(decorator.Inner);

            Assert.Same(validator, decorated.InjectedService);
        }

        [Fact]
        public void CanInjectServicesIntoDecoratingType()
        {
            var serviceProvider = ConfigureProvider(services =>
            {
                services.AddSingleton<IService, SomeRandomService>();
                services.AddSingleton<IDecoratedService, Decorated>();

                services.Decorate<IDecoratedService, Decorator>();
            });

            var validator = serviceProvider.GetRequiredService<IService>();

            var instance = serviceProvider.GetRequiredService<IDecoratedService>();

            var decorator = Assert.IsType<Decorator>(instance);

            Assert.Same(validator, decorator.InjectedService);
        }

        public interface IDecoratedService { }

        public interface IService { }

        private class SomeRandomService : IService { }

        public class Decorated : IDecoratedService
        {
            public Decorated(IService injectedService = null)
            {
                InjectedService = injectedService;
            }

            public IService InjectedService { get; }
        }

        public class Decorator : IDecoratedService
        {
            public Decorator(IDecoratedService inner, IService injectedService = null)
            {
                Inner = inner;
                InjectedService = injectedService;
            }

            public IDecoratedService Inner { get; }

            public IService InjectedService { get; }
        }

        public class OtherDecorated : IDecoratedService { }
    }
}
