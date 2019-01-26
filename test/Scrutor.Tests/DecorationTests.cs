using System;
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
        public void CanDecorateExistingInstance()
        {
            var existing = new Decorated();

            var provider = ConfigureProvider(services =>
            {
                services.AddSingleton<IDecoratedService>(existing);

                services.Decorate<IDecoratedService, Decorator>();
            });

            var instance = provider.GetRequiredService<IDecoratedService>();

            var decorator = Assert.IsType<Decorator>(instance);
            var decorated = Assert.IsType<Decorated>(decorator.Inner);

            Assert.Same(existing, decorated);
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

        [Fact]
        public void DisposableServicesAreDisposed()
        {
            var provider = ConfigureProvider(services =>
            {
                services.AddTransient<IDisposableService, DisposableService>();

                services.Decorate<IDisposableService, DisposableServiceDecorator>();
            });

            var disposable = provider.GetRequiredService<IDisposableService>();

            var decorator = Assert.IsType<DisposableServiceDecorator>(disposable);

            provider.Dispose();

            Assert.True(decorator.WasDisposed);
            Assert.True(decorator.Inner.WasDisposed);
        }

        [Fact]
        public void DecoratingNonRegisteredServiceThrows()
        {
            Assert.Throws<MissingTypeRegistrationException>(() => ConfigureProvider(services => services.Decorate<IDecoratedService, Decorator>()));
        }

        [Fact]
        public void CheckLifetimeOfDecorateSingleton()
        {
            var services = new ServiceCollection();

            services.AddSingleton<IDecoratedService, Decorated>();
            services.DecorateSingleton<IDecoratedService, Decorator>();

            var descriptor = services.GetDescriptor<IDecoratedService>();

            Assert.Equal(ServiceLifetime.Singleton, descriptor.Lifetime);
        }

        [Fact]
        public void CheckLifetimeOfDecorateScoped()
        {
            var services = new ServiceCollection();

            services.AddSingleton<IDecoratedService, Decorated>();
            services.DecorateScoped<IDecoratedService, Decorator>();

            var descriptor = services.GetDescriptor<IDecoratedService>();

            Assert.Equal(ServiceLifetime.Scoped, descriptor.Lifetime);
        }

        [Fact]
        public void CheckLifetimeOfDecorateTransient()
        {
            var services = new ServiceCollection();

            services.AddSingleton<IDecoratedService, Decorated>();
            services.DecorateTransient<IDecoratedService, Decorator>();

            var descriptor = services.GetDescriptor<IDecoratedService>();

            Assert.Equal(ServiceLifetime.Transient, descriptor.Lifetime);
        }

        [Fact]
        public void IsSameLifetimeOfDecoratorAndDecorated()
        {
            var services = new ServiceCollection();

            services.AddSingleton<IDecoratedService, Decorated>();

            var decoratedDescriptor = services.GetDescriptor<IDecoratedService>();

            services.Decorate<IDecoratedService, Decorator>();

            var decoratorDescriptor = services.GetDescriptor<IDecoratedService>();

            Assert.Equal(decoratedDescriptor.Lifetime, decoratorDescriptor.Lifetime);
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

        private interface IDisposableService : IDisposable
        {
            bool WasDisposed { get; }
        }

        private class DisposableService : IDisposableService
        {
            public bool WasDisposed { get; private set; }

            public virtual void Dispose()
            {
                WasDisposed = true;
            }
        }

        private class DisposableServiceDecorator : IDisposableService
        {
            public DisposableServiceDecorator(IDisposableService inner)
            {
                Inner = inner ?? throw new ArgumentNullException(nameof(inner));
            }

            public IDisposableService Inner { get; }

            public bool WasDisposed { get; private set; }

            public void Dispose()
            {
                Inner.Dispose();
                WasDisposed = true;
            }
        }
    }
}
