using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Scrutor.Tests
{
    public class DecorationTests
    {
        private IServiceCollection Collection { get; } = new ServiceCollection();

        [Fact]
        public void CanDecorateType()
        {
            Collection.AddSingleton<IDecoratedService, Decorated>();

            Collection.Decorate<IDecoratedService>(inner => new Decorator(inner));

            var provider = Collection.BuildServiceProvider();

            var instance = provider.GetRequiredService<IDecoratedService>();

            var decorator = Assert.IsType<Decorator>(instance);

            Assert.IsType<Decorated>(decorator.Inner);
        }

        [Fact]
        public void CanDecorateMultipleLevels()
        {
            Collection.AddSingleton<IDecoratedService, Decorated>();

            Collection.Decorate<IDecoratedService>(inner => new Decorator(inner));
            Collection.Decorate<IDecoratedService>(inner => new Decorator(inner));

            var provider = Collection.BuildServiceProvider();

            var instance = provider.GetRequiredService<IDecoratedService>();

            var decorator = Assert.IsType<Decorator>(instance);
            var outerDecorator = Assert.IsType<Decorator>(decorator.Inner);

            Assert.IsType<Decorated>(outerDecorator.Inner);
        }

        [Fact]
        public void CanDecorateDifferentServices()
        {
            Collection.AddSingleton<IDecoratedService, Decorated>();
            Collection.AddSingleton<IDecoratedService, OtherDecorated>();

            Collection.Decorate<IDecoratedService>(inner => new Decorator(inner));

            var provider = Collection.BuildServiceProvider();

            var instances = provider
                .GetRequiredService<IEnumerable<IDecoratedService>>()
                .ToArray();

            Assert.Equal(2, instances.Length);
            Assert.All(instances, x => Assert.IsType<Decorator>(x));
        }

        [Fact]
        public void ShouldReplaceExistingServiceDescriptor()
        {
            Collection.AddSingleton<IDecoratedService, Decorated>();

            Collection.Decorate<IDecoratedService>(inner => new Decorator(inner));

            var descriptor = Collection.GetDescriptor<IDecoratedService>();

            Assert.Equal(typeof(IDecoratedService), descriptor.ServiceType);
            Assert.NotNull(descriptor.ImplementationFactory);
        }

        [Fact]
        public void CanInjectServicesIntoDecoratedType()
        {
            Collection.AddSingleton<IService, SomeRandomService>();
            Collection.AddSingleton<IDecoratedService, Decorated>();

            Collection.Decorate<IDecoratedService>(inner => new Decorator(inner));

            var provider = Collection.BuildServiceProvider();

            var validator = provider.GetRequiredService<IService>();

            var instance = provider.GetRequiredService<IDecoratedService>();

            var decorator = Assert.IsType<Decorator>(instance);
            var decorated = Assert.IsType<Decorated>(decorator.Inner);

            Assert.Same(validator, decorated.InjectedService);
        }

        [Fact]
        public void CanInjectServicesIntoDecoratingType()
        {
            Collection.AddSingleton<IService, SomeRandomService>();
            Collection.AddSingleton<IDecoratedService, Decorated>();

            Collection.Decorate<IDecoratedService>((inner, provider) => new Decorator(inner, provider.GetRequiredService<IService>()));

            var serviceProvider = Collection.BuildServiceProvider();

            var validator = serviceProvider.GetRequiredService<IService>();

            var instance = serviceProvider.GetRequiredService<IDecoratedService>();

            var decorator = Assert.IsType<Decorator>(instance);

            Assert.Same(validator, decorator.InjectedService);
        }

        public interface IDecoratedService { }

        public interface IService {}

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
