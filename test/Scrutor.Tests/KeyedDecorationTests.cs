using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;
// ReSharper disable ClassNeverInstantiated.Local

namespace Scrutor.Tests;

public class KeyedDecorationTests : TestBase
{
    private interface IMyService
    {
    }


    private class MyDependency
    {
    }

    private class MyServiceImplementation : IMyService
    {
        public MyDependency Dependency { get; }

        public MyServiceImplementation(MyDependency dependency)
        {
            ArgumentNullException.ThrowIfNull(dependency);
            Dependency = dependency;
        }
    }


    private class MyDependencyToFirstDecorator
    {
    }

    private class MyServiceFirstDecorator : IMyService
    {
        public IMyService Decorated { get; }
        public MyDependencyToFirstDecorator Dependency { get; }

        public MyServiceFirstDecorator(IMyService decorated, MyDependencyToFirstDecorator dependency)
        {
            ArgumentNullException.ThrowIfNull(decorated);
            ArgumentNullException.ThrowIfNull(dependency);
            Decorated = decorated;
            Dependency = dependency;
        }
    }


    private class MyDependencyToSecondDecorator
    {
    }

    private class MyServiceSecondDecorator : IMyService
    {
        public IMyService Decorated { get; }
        public MyDependencyToSecondDecorator Dependency { get; }

        public MyServiceSecondDecorator(IMyService decorated, MyDependencyToSecondDecorator dependency)
        {
            ArgumentNullException.ThrowIfNull(decorated);
            ArgumentNullException.ThrowIfNull(dependency);
            Decorated = decorated;
            Dependency = dependency;
        }
    }

    private class MyServiceThirdDecorator : IMyService
    {
        public IMyService Decorated { get; }

        public MyServiceThirdDecorator(IMyService decorated)
        {
            ArgumentNullException.ThrowIfNull(decorated);
            Decorated = decorated;
        }
    }

    private const string serviceKey = "myServiceKey";

    private static void CreateServiceCollection(IServiceCollection services)
    {
        services
            .AddTransient<MyDependency>()
            .AddTransient<MyDependencyToFirstDecorator>()
            .AddTransient<MyDependencyToSecondDecorator>();

        services.AddKeyedTransient<IMyService, MyServiceImplementation>(serviceKey);
        services.DecorateKeyed<IMyService, MyServiceFirstDecorator>(serviceKey);
        services.DecorateKeyed<IMyService, MyServiceSecondDecorator>(serviceKey);
        services.DecorateKeyed<IMyService, MyServiceThirdDecorator>(serviceKey);
    }

    [Fact]
    public void Decorating_WithInvalidKeys_ShouldThrow()
    {
        Should.Throw<InvalidOperationException>(() =>
        {
            ConfigureProvider(services =>
            {
                services
                    .AddTransient<MyDependency>()
                    .AddTransient<MyDependencyToFirstDecorator>()
                    .AddTransient<MyDependencyToSecondDecorator>();

                services.AddKeyedScoped<IMyService, MyServiceImplementation>(serviceKey);
                services.DecorateKeyed<IMyService, MyServiceFirstDecorator>("anotherKey");
            });
        });
    }

    [Fact]
    public void ServiceCollection_KeyedDescriptor_ShouldBeSingle()
    {
        ConfigureProvider(services =>
        {
            CreateServiceCollection(services);
            ServiceDescriptor[] myServices = services.Where(x =>
                x.ServiceType == typeof(IMyService) &&
                x.ServiceKey != null &&
                x.ServiceKey.Equals(serviceKey)).ToArray();
            myServices.Length.ShouldBe(1);
        });
    }

    [Fact]
    public void ServiceProvider_ShouldBuild()
    {
        Should.NotThrow(() => ConfigureProvider(CreateServiceCollection));
    }

    [Fact]
    public void MyService_ShouldResolve()
    {
        IServiceProvider serviceProvider = ConfigureProvider(CreateServiceCollection);
        Should.NotThrow(() => serviceProvider.GetRequiredKeyedService<IMyService>(serviceKey));
    }

    [Fact]
    public void MyService_Hierarchy_ShouldByCorrect()
    {
        IServiceProvider serviceProvider = ConfigureProvider(CreateServiceCollection);
        IMyService myService = serviceProvider.GetRequiredKeyedService<IMyService>(serviceKey);
        myService.ShouldBeAssignableTo<MyServiceThirdDecorator>();

        MyServiceThirdDecorator thirdDecorator = (MyServiceThirdDecorator)myService;
        thirdDecorator.Decorated.ShouldBeAssignableTo<MyServiceSecondDecorator>();

        MyServiceSecondDecorator secondDecorator = (MyServiceSecondDecorator)thirdDecorator.Decorated;
        secondDecorator.Decorated.ShouldBeAssignableTo<MyServiceFirstDecorator>();

        MyServiceFirstDecorator firstDecorator = (MyServiceFirstDecorator)secondDecorator.Decorated;
        firstDecorator.Decorated.ShouldBeAssignableTo<MyServiceImplementation>();
    }
}

public class MultiKeyedDecorationTests : TestBase
{
    private interface IMyService
    {
        public string ServiceKey { get; }
    }

    private class MyImpl : IMyService
    {
        public string ServiceKey { get; }

        public MyImpl(string serviceKey)
        {
            ArgumentNullException.ThrowIfNull(serviceKey);
            ServiceKey = serviceKey;
        }
    }

    private class MyFirstDecorator : IMyService
    {
        private readonly IMyService _decorated;

        public MyFirstDecorator(IMyService decorated)
        {
            ArgumentNullException.ThrowIfNull(decorated);
            _decorated = decorated;
        }

        public string ServiceKey => _decorated.ServiceKey;
    }

    private class MySecondDecorator : IMyService
    {
        private readonly IMyService _decorated;

        public MySecondDecorator(IMyService decorated)
        {
            ArgumentNullException.ThrowIfNull(decorated);
            _decorated = decorated;
        }

        public string ServiceKey => _decorated.ServiceKey;
    }

    private const string myKey1 = "myKey1";
    private const string myKey2 = "myKey2";
    private const string myKey3 = "myKey3";

    private static void CreateServiceCollection(IServiceCollection services)
    {
        services.AddKeyedTransient<IMyService>(myKey1, (sp, key) => new MyImpl((string)key!));
        services.DecorateKeyed<IMyService, MyFirstDecorator>(myKey1);
        services.DecorateKeyed<IMyService, MySecondDecorator>(myKey1);

        services.AddKeyedTransient<IMyService>(myKey2, (sp, key) => new MyImpl((string)key!));
        services.DecorateKeyed<IMyService, MyFirstDecorator>(myKey2);
        services.DecorateKeyed<IMyService, MySecondDecorator>(myKey2);

        services.AddKeyedTransient<IMyService>(myKey3, (sp, key) => new MyImpl((string)key!));
        services.DecorateKeyed<IMyService, MyFirstDecorator>(myKey3);
    }

    [Fact]
    public void ServiceProvider_ShouldBuild()
    {
        Should.NotThrow(() => ConfigureProvider(CreateServiceCollection));
    }

    [Fact]
    public void KeyedServices_ShouldBeDecorated()
    {
        IServiceProvider serviceProvider = ConfigureProvider(CreateServiceCollection);
        IMyService service1 = serviceProvider.GetRequiredKeyedService<IMyService>(myKey1);
        IMyService service2 = serviceProvider.GetRequiredKeyedService<IMyService>(myKey2);
        IMyService service3 = serviceProvider.GetRequiredKeyedService<IMyService>(myKey3);

        service1.ShouldBeAssignableTo<MySecondDecorator>();
        service2.ShouldBeAssignableTo<MySecondDecorator>();
        service3.ShouldBeAssignableTo<MyFirstDecorator>();
    }

    [Fact]
    public void KeyedServices_ShouldHaveDifferentKeys()
    {
        IServiceProvider serviceProvider = ConfigureProvider(CreateServiceCollection);
        IMyService service1 = serviceProvider.GetRequiredKeyedService<IMyService>(myKey1);
        IMyService service2 = serviceProvider.GetRequiredKeyedService<IMyService>(myKey2);
        IMyService service3 = serviceProvider.GetRequiredKeyedService<IMyService>(myKey3);

        service1.ServiceKey.ShouldBe(myKey1);
        service2.ServiceKey.ShouldBe(myKey2);
        service3.ServiceKey.ShouldBe(myKey3);

        service1.ShouldNotBe(service2);
    }
}
