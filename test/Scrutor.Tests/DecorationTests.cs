using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using System.Linq;

namespace Scrutor.Tests;

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

        var outerDecorator = Assert.IsType<Decorator>(instance);
        var innerDecorator = Assert.IsType<Decorator>(outerDecorator.Inner);
        _ = Assert.IsType<Decorated>(innerDecorator.Inner);
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
    public void ShouldAddServiceKeyToExistingServiceDescriptor()
    {
        var services = new ServiceCollection();

        services.AddSingleton<IDecoratedService, Decorated>();

        services.Decorate<IDecoratedService, Decorator>();

        var descriptors = services.GetDescriptors<IDecoratedService>();

        Assert.Equal(2, descriptors.Length);

        var decorated = descriptors.SingleOrDefault(x => x.ServiceKey is not null);

        Assert.NotNull(decorated);
        Assert.NotNull(decorated.KeyedImplementationType);
        var key = Assert.IsType<string>(decorated.ServiceKey);
        Assert.StartsWith("IDecoratedService", key);
        Assert.EndsWith("+Decorated", key);
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
            services.AddScoped<IDisposableService, DisposableService>();
            services.Decorate<IDisposableService, DisposableServiceDecorator>();
        });

        DisposableServiceDecorator decorator;
        using (var scope = provider.CreateScope())
        {
            var disposable = scope.ServiceProvider.GetRequiredService<IDisposableService>();
            decorator = Assert.IsType<DisposableServiceDecorator>(disposable);
        }

        Assert.True(decorator.WasDisposed);
        Assert.True(decorator.Inner.WasDisposed);
    }

    [Fact]
    public void ServicesWithSameServiceTypeAreOnlyDecoratedOnce()
    {
        // See issue: https://github.com/khellang/Scrutor/issues/125

        static bool IsHandlerButNotDecorator(Type type)
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
        var inner = Assert.IsType<DecoratedService>(result.Inner);
        Assert.NotNull(inner.Dependency);
    }

    #region Individual functions tests

    [Fact]
    public void DecorationFunctionsDoDecorateRegisteredService()
    {
        var allDecorationFunctions = new Action<IServiceCollection>[]
        {
            sc => sc.Decorate<IDecoratedService, Decorator>(),
            sc => sc.TryDecorate<IDecoratedService, Decorator>(),
            sc => sc.Decorate(typeof(IDecoratedService), typeof(Decorator)),
            sc => sc.TryDecorate(typeof(IDecoratedService), typeof(Decorator)),
            sc => sc.Decorate((IDecoratedService obj, IServiceProvider sp) => new Decorator(obj)),
            sc => sc.TryDecorate((IDecoratedService obj, IServiceProvider sp) => new Decorator(obj)),
            sc => sc.Decorate((IDecoratedService obj) => new Decorator(obj)),
            sc => sc.TryDecorate((IDecoratedService obj) => new Decorator(obj)),
            sc => sc.Decorate(typeof(IDecoratedService), (object obj, IServiceProvider sp) => new Decorator((IDecoratedService)obj)),
            sc => sc.TryDecorate(typeof(IDecoratedService), (object obj, IServiceProvider sp) => new Decorator((IDecoratedService)obj)),
            sc => sc.Decorate(typeof(IDecoratedService), (object obj) => new Decorator((IDecoratedService)obj)),
            sc => sc.TryDecorate(typeof(IDecoratedService), (object obj) => new Decorator((IDecoratedService)obj))
        };

        foreach (var decorationFunction in allDecorationFunctions)
        {
            var provider = ConfigureProvider(services =>
            {
                services.AddSingleton<IDecoratedService, Decorated>();
                decorationFunction(services);
            });

            var instance = provider.GetRequiredService<IDecoratedService>();
            var decorator = Assert.IsType<Decorator>(instance);
            Assert.IsType<Decorated>(decorator.Inner);
        }
    }

    [Fact]
    public void DecorationFunctionsProvideScopedServiceProvider()
    {
        IServiceProvider actual = default;

        var decorationFunctions = new Action<IServiceCollection>[]
        {
            sc => sc.Decorate((IDecoratedService obj, IServiceProvider sp) =>
            {
                actual = sp;
                return null;
            }),
            sc => sc.TryDecorate((IDecoratedService obj, IServiceProvider sp) =>
            {
                actual = sp;
                return null;
            }),
            sc => sc.Decorate(typeof(IDecoratedService), (object obj, IServiceProvider sp) =>
            {
                actual = sp;
                return null;
            }),
            sc => sc.TryDecorate(typeof(IDecoratedService), (object obj, IServiceProvider sp) =>
            {
                actual = sp;
                return null;
            }),
        };

        foreach (var decorationMethod in decorationFunctions)
        {
            var provider = ConfigureProvider(services =>
            {
                services.AddScoped<IDecoratedService, Decorated>();
                decorationMethod(services);
            });

            using var scope = provider.CreateScope();
            var expected = scope.ServiceProvider;
            _ = scope.ServiceProvider.GetService<IDecoratedService>();
            Assert.Same(expected, actual);
        }
    }

    [Fact]
    public void DecorateThrowsDecorationExceptionWhenNoTypeRegistered()
    {
        Assert.Throws<DecorationException>(() => ConfigureProvider(services => services.Decorate<IDecoratedService, Decorator>()));
        Assert.Throws<DecorationException>(() => ConfigureProvider(services => services.Decorate(typeof(IDecoratedService), typeof(Decorator))));
        Assert.Throws<DecorationException>(() => ConfigureProvider(services => services.Decorate((IDecoratedService obj, IServiceProvider sp) => new Decorated())));
        Assert.Throws<DecorationException>(() => ConfigureProvider(services => services.Decorate((IDecoratedService sp) => new Decorated())));
        Assert.Throws<DecorationException>(() => ConfigureProvider(services => services.Decorate(typeof(IDecoratedService), (object obj, IServiceProvider sp) => new Decorated())));
        Assert.Throws<DecorationException>(() => ConfigureProvider(services => services.Decorate(typeof(IDecoratedService), (object obj) => new Decorated())));
    }

    [Fact]
    public void TryDecorateReturnsBoolResult()
    {
        var allDecorationMethods = new Func<IServiceCollection, bool>[]
        {
            sc => sc.TryDecorate<IDecoratedService, Decorator>(),
            sc => sc.TryDecorate(typeof(IDecoratedService), typeof(Decorator)),
            sc => sc.TryDecorate((IDecoratedService obj, IServiceProvider sp) => new Decorator(obj)),
            sc => sc.TryDecorate((IDecoratedService obj) => new Decorator(obj)),
            sc => sc.TryDecorate(typeof(IDecoratedService), (object obj, IServiceProvider sp) => new Decorator((IDecoratedService)obj)),
            sc => sc.TryDecorate(typeof(IDecoratedService), (object obj) => new Decorator((IDecoratedService)obj))
        };

        foreach (var decorationMethod in allDecorationMethods)
        {
            var provider = ConfigureProvider(services =>
            {
                var isDecorated = decorationMethod(services);
                Assert.False(isDecorated);

                services.AddSingleton<IDecoratedService, Decorated>();

                isDecorated = decorationMethod(services);
                Assert.True(isDecorated);
            });
        }
    }

    #endregion

    #region DI Scope test

    [Fact]
    public void DecoratedTransientServiceRetainsScope()
    {
        var provider = ConfigureProvider(services =>
        {
            services.AddTransient<IDecoratedService, Decorated>();
            services.Decorate<IDecoratedService, Decorator>();
        });

        using var scope = provider.CreateScope();
        var service1 = scope.ServiceProvider.GetRequiredService<IDecoratedService>();
        var service2 = scope.ServiceProvider.GetRequiredService<IDecoratedService>();

        Assert.NotEqual(service1, service2);
    }

    [Fact]
    public void DecoratedScopedServiceRetainsScope()
    {
        var provider = ConfigureProvider(services =>
        {
            services.AddScoped<IDecoratedService, Decorated>();
            services.Decorate<IDecoratedService, Decorator>();
        });

        object service1;

        using (var scope = provider.CreateScope())
        {
            service1 = scope.ServiceProvider.GetRequiredService<IDecoratedService>();
            var service2 = scope.ServiceProvider.GetRequiredService<IDecoratedService>();
            Assert.Same(service1, service2);
        }

        using (var scope = provider.CreateScope())
        {
            var service2 = scope.ServiceProvider.GetRequiredService<IDecoratedService>();
            Assert.NotSame(service1, service2);
        }
    }

    [Fact]
    public void DecoratedSingletonServiceRetainsScope()
    {
        var provider = ConfigureProvider(services =>
        {
            services.AddSingleton<IDecoratedService, Decorated>();
            services.Decorate<IDecoratedService, Decorator>();
        });

        object service1;

        using (var scope = provider.CreateScope())
        {
            service1 = scope.ServiceProvider.GetRequiredService<IDecoratedService>();
            var service2 = scope.ServiceProvider.GetRequiredService<IDecoratedService>();
            Assert.Same(service1, service2);
        }

        using (var scope = provider.CreateScope())
        {
            var service2 = scope.ServiceProvider.GetRequiredService<IDecoratedService>();
            Assert.Same(service1, service2);
        }
    }

    [Fact]
    public void DependentServicesRetainTheirOwnScope()
    {
        var provider = ConfigureProvider(services =>
        {
            services.AddScoped<IService, SomeRandomService>();
            services.AddTransient<DecoratedService>();
            services.Decorate<DecoratedService, Decorator2>();
        });

        using var scope = provider.CreateScope();
        var decorator1 = scope.ServiceProvider.GetRequiredService<DecoratedService>() as Decorator2;
        var decorator2 = scope.ServiceProvider.GetRequiredService<DecoratedService>() as Decorator2;

        Assert.NotEqual(decorator1, decorator2);
        Assert.NotEqual(decorator1.Inner, decorator2.Inner);
        Assert.Equal(decorator1.Inner.Dependency, decorator2.Inner.Dependency);
    }

    #endregion

    #region Mocks

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
            Inner = inner ?? throw new ArgumentNullException(nameof(inner));
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

        public void Dispose() => WasDisposed = true;
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
    { }

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

    internal sealed class MyEventHandlerDecorator<TEvent> : IEventHandler<TEvent>, IHandlerDecorator where TEvent : class, IEvent
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

    #endregion
}
