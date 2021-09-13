using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using System.Linq;
using static Scrutor.Tests.DecorationTests;

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
        public void ServicesWithSameServiceTypeAreOnlyDecoratedOnce()
        {
            // See issue: https://github.com/khellang/Scrutor/issues/125

            bool IsHandlerButNotDecorator(Type type)
            {
                var isHandlerDecorator = false;

                var isHandler = type.GetInterfaces().Any(i =>
                    i.IsGenericType &&
                    i.GetGenericTypeDefinition() == typeof(IEventHandler<>)
                );

                if (isHandler)
                {
                    isHandlerDecorator = type.GetInterfaces().Any(i => i == typeof(IHandlerDecorator));
                }

                return isHandler && !isHandlerDecorator;
            }

            var provider = ConfigureProvider(services =>
            {
                // This should end up with 3 registrations of type IEventHandler<MyEvent>.
                services.Scan(s =>
                    s.FromAssemblyOf<DecorationTests>()
                        .AddClasses(c => c.Where(IsHandlerButNotDecorator))
                        .AsImplementedInterfaces()
                        .WithTransientLifetime());

                // This should not decorate each registration 3 times.
                services.Decorate(typeof(IEventHandler<>), typeof(MyEventHandlerDecorator<>));
            });

            var instances = provider.GetRequiredService<IEnumerable<IEventHandler<MyEvent>>>().ToList();

            Assert.Equal(3, instances.Count);

            Assert.All(instances, instance =>
            {
                var decorator = Assert.IsType<MyEventHandlerDecorator<MyEvent>>(instance);

                // The inner handler should not be a decorator.
                Assert.IsNotType<MyEventHandlerDecorator<MyEvent>>(decorator.Handler);

                // The return call count should only be 1, we've only called Handle on one decorator.
                // If there were nested decorators, this would return a higher call count as it
                // would increment at each level.
                Assert.Equal(1, decorator.Handle(new MyEvent()));
            });
        }

        [Fact]
        public void DecoratingNonRegisteredServiceThrows()
        {
            Assert.Throws<MissingTypeRegistrationException>(() => ConfigureProvider(services => services.Decorate<IDecoratedService, Decorator>()));
        }

        [Fact]
        public void Issue148_Decorate_IsAbleToDecorateConcreateTypes()
        {
            var sp = ConfigureProvider(sc =>
            {
                sc
                    .AddTransient<IService, SomeRandomService>()
                    .AddTransient<DecoratedService>()
                    .Decorate<DecoratedService, Decorator2>();
            });
            
            var result = sp.GetService<DecoratedService>() as Decorator2;
            
            Assert.NotNull(result);  
            Assert.NotNull(result.Inner);
            Assert.NotNull(result.Inner.Dependency);
        }

        public interface IDecoratedService { }

        public class DecoratedService
        {
            public DecoratedService(IService dependency)
            {
                Dependency = dependency;
            }

            public IService Dependency { get; }
        }

        public class Decorator2 : DecoratedService
        {
            public Decorator2(DecoratedService decoratedService)
                : base(null)
            {
                Inner = decoratedService;
            }

            public DecoratedService Inner { get; }
        }

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

        public interface IEvent
        {
        }

        public interface IEventHandler<in TEvent> where TEvent : class, IEvent
        {
            int Handle(TEvent @event);
        }

        public interface IHandlerDecorator
        {
        }

        public sealed class MyEvent : IEvent
        {}

        internal sealed class MyEvent1Handler : IEventHandler<MyEvent>
        {
            private int _callCount;

            public int Handle(MyEvent @event)
            {
                return _callCount++;
            }
        }

        internal sealed class MyEvent2Handler : IEventHandler<MyEvent>
        {
            private int _callCount;

            public int Handle(MyEvent @event)
            {
                return _callCount++;
            }
        }

        internal sealed class MyEvent3Handler : IEventHandler<MyEvent>
        {
            private int _callCount;

            public int Handle(MyEvent @event)
            {
                return _callCount++;
            }
        }

        internal sealed class MyEventHandlerDecorator<TEvent> : IEventHandler<TEvent>, IHandlerDecorator where TEvent: class, IEvent
        {
            public readonly IEventHandler<TEvent> Handler;

            public MyEventHandlerDecorator(IEventHandler<TEvent> handler)
            {
                Handler = handler;
            }

            public int Handle(TEvent @event)
            {
                return Handler.Handle(@event) + 1;
            }
        }
    }
}
