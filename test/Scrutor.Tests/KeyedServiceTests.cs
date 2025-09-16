using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using Xunit;

namespace Scrutor.Tests;

public class KeyedServiceTests : TestBase
{
    private IServiceCollection Collection { get; } = new ServiceCollection();

    [Fact]
    public void CanRegisterKeyedServiceWithStringKey()
    {
        Collection.Scan(scan => scan
            .FromTypes(typeof(KeyedTransientService))
            .UsingAttributes());

        Assert.Single(Collection);

        var service = Collection.Single();
        Assert.Equal(typeof(IKeyedTestService), service.ServiceType);
        Assert.Equal(typeof(KeyedTransientService), service.KeyedImplementationType);
        Assert.Equal("test-key", service.ServiceKey);
        Assert.Equal(ServiceLifetime.Transient, service.Lifetime);
        Assert.True(service.IsKeyedService);
    }

    [Fact]
    public void CanRegisterKeyedServiceWithGenericAttribute()
    {
        Collection.Scan(scan => scan
            .FromTypes(typeof(GenericKeyedService))
            .UsingAttributes());

        Assert.Single(Collection);

        var service = Collection.Single();
        Assert.Equal(typeof(IKeyedTestService), service.ServiceType);
        Assert.Equal(typeof(GenericKeyedService), service.KeyedImplementationType);
        Assert.Equal("generic-key", service.ServiceKey);
        Assert.Equal(ServiceLifetime.Scoped, service.Lifetime);
        Assert.True(service.IsKeyedService);
    }

    [Fact]
    public void CanRegisterMultipleKeyedServicesOnSameType()
    {
        Collection.Scan(scan => scan
            .FromTypes(typeof(MultipleKeyedService))
            .UsingAttributes());

        Assert.Equal(2, Collection.Count);

        var services = Collection.ToArray();
        
        var service1 = services.First(s => s.ServiceKey?.ToString() == "key1");
        Assert.Equal(typeof(IKeyedTestService), service1.ServiceType);
        Assert.Equal(typeof(MultipleKeyedService), service1.KeyedImplementationType);
        Assert.Equal(ServiceLifetime.Transient, service1.Lifetime);

        var service2 = services.First(s => s.ServiceKey?.ToString() == "key2");
        Assert.Equal(typeof(IKeyedTestService), service2.ServiceType);
        Assert.Equal(typeof(MultipleKeyedService), service2.KeyedImplementationType);
        Assert.Equal(ServiceLifetime.Singleton, service2.Lifetime);
    }

    [Fact]
    public void CanRegisterMixedKeyedAndNonKeyedServices()
    {
        Collection.Scan(scan => scan
            .FromTypes(typeof(MixedKeyedService))
            .UsingAttributes());

        Assert.Equal(2, Collection.Count);

        var keyedService = Collection.First(s => s.IsKeyedService);
        Assert.Equal(typeof(IKeyedTestService), keyedService.ServiceType);
        Assert.Equal(typeof(MixedKeyedService), keyedService.KeyedImplementationType);
        Assert.Equal("mixed-key", keyedService.ServiceKey);
        Assert.Equal(ServiceLifetime.Scoped, keyedService.Lifetime);

        var nonKeyedService = Collection.First(s => !s.IsKeyedService);
        Assert.Equal(typeof(IKeyedTestService), nonKeyedService.ServiceType);
        Assert.Equal(typeof(MixedKeyedService), nonKeyedService.ImplementationType);
        Assert.Null(nonKeyedService.ServiceKey);
        Assert.Equal(ServiceLifetime.Transient, nonKeyedService.Lifetime);
    }

    [Fact]
    public void CanResolveKeyedServices()
    {
        var provider = ConfigureProvider(services =>
        {
            services.Scan(scan => scan
                .FromTypes(typeof(KeyedTransientService), typeof(GenericKeyedService), typeof(MultipleKeyedService))
                .UsingAttributes());
        });

        var keyedTransient = provider.GetRequiredKeyedService<IKeyedTestService>("test-key");
        Assert.IsType<KeyedTransientService>(keyedTransient);

        using var scope = provider.CreateScope();
        var genericKeyed = scope.ServiceProvider.GetRequiredKeyedService<IKeyedTestService>("generic-key");
        Assert.IsType<GenericKeyedService>(genericKeyed);

        var multipleKeyed1 = provider.GetRequiredKeyedService<IKeyedTestService>("key1");
        Assert.IsType<MultipleKeyedService>(multipleKeyed1);

        var multipleKeyed2 = provider.GetRequiredKeyedService<IKeyedTestService>("key2");
        Assert.IsType<MultipleKeyedService>(multipleKeyed2);

        // Verify they are different instances for transient services
        var anotherKeyedTransient = provider.GetRequiredKeyedService<IKeyedTestService>("test-key");
        Assert.NotSame(keyedTransient, anotherKeyedTransient);

        // Verify singleton behavior
        var anotherMultipleKeyed2 = provider.GetRequiredKeyedService<IKeyedTestService>("key2");
        Assert.Same(multipleKeyed2, anotherMultipleKeyed2);
    }

    [Fact]
    public void KeyedServicesAreIsolatedFromNonKeyedServices()
    {
        var provider = ConfigureProvider(services =>
        {
            services.Scan(scan => scan
                .FromTypes(typeof(MixedKeyedService))
                .UsingAttributes());
        });

        using var scope = provider.CreateScope();
        var keyedService = scope.ServiceProvider.GetRequiredKeyedService<IKeyedTestService>("mixed-key");
        var nonKeyedService = provider.GetRequiredService<IKeyedTestService>();

        Assert.IsType<MixedKeyedService>(keyedService);
        Assert.IsType<MixedKeyedService>(nonKeyedService);
        Assert.NotSame(keyedService, nonKeyedService);
    }

    [Fact]
    public void CanRegisterKeyedServiceWithObjectKey()
    {
        Collection.Scan(scan => scan
            .FromTypes(typeof(ObjectKeyedService))
            .UsingAttributes());

        Assert.Single(Collection);

        var service = Collection.Single();
        Assert.Equal(typeof(IKeyedTestService), service.ServiceType);
        Assert.Equal(typeof(ObjectKeyedService), service.KeyedImplementationType);
        Assert.Equal(42, service.ServiceKey);
        Assert.True(service.IsKeyedService);
    }

    [Fact]
    public void CanResolveKeyedServiceWithObjectKey()
    {
        var provider = ConfigureProvider(services =>
        {
            services.Scan(scan => scan
                .FromTypes(typeof(ObjectKeyedService))
                .UsingAttributes());
        });

        var keyedService = provider.GetRequiredKeyedService<IKeyedTestService>(42);
        Assert.IsType<ObjectKeyedService>(keyedService);
    }

    [Fact]
    public void CanRegisterKeyedServiceWithEnumKey()
    {
        Collection.Scan(scan => scan
            .FromTypes(typeof(EnumKeyedService))
            .UsingAttributes());

        Assert.Single(Collection);

        var service = Collection.Single();
        Assert.Equal(typeof(IKeyedTestService), service.ServiceType);
        Assert.Equal(typeof(EnumKeyedService), service.KeyedImplementationType);
        Assert.Equal(TestEnum.Value1, service.ServiceKey);
        Assert.True(service.IsKeyedService);
    }

    [Fact]
    public void CanRegisterKeyedServiceWithDifferentServiceTypes()
    {
        Collection.Scan(scan => scan
            .FromTypes(typeof(MultiServiceKeyedService))
            .UsingAttributes());

        Assert.Equal(2, Collection.Count);

        var keyedService = Collection.First(s => s.ServiceType == typeof(IKeyedTestService));
        Assert.Equal(typeof(MultiServiceKeyedService), keyedService.KeyedImplementationType);
        Assert.Equal("service-key", keyedService.ServiceKey);

        var otherKeyedService = Collection.First(s => s.ServiceType == typeof(IOtherKeyedTestService));
        Assert.Equal(typeof(MultiServiceKeyedService), otherKeyedService.KeyedImplementationType);
        Assert.Equal("other-key", otherKeyedService.ServiceKey);
    }

    [Fact]
    public void ThrowsWhenResolvingNonExistentKeyedService()
    {
        var provider = ConfigureProvider(services =>
        {
            services.Scan(scan => scan
                .FromTypes(typeof(KeyedTransientService))
                .UsingAttributes());
        });

        Assert.Throws<InvalidOperationException>(() => 
            provider.GetRequiredKeyedService<IKeyedTestService>("non-existent-key"));
    }

    [Fact]
    public void CanRegisterKeyedServiceWithNullServiceType()
    {
        Collection.Scan(scan => scan
            .FromTypes(typeof(KeyedServiceWithNullServiceType))
            .UsingAttributes());

        // Should register for the implementation type and all its interfaces
        Assert.Equal(2, Collection.Count); // IKeyedTestService and KeyedServiceWithNullServiceType itself

        var services = Collection.ToArray();
        Assert.All(services, s => 
        {
            Assert.Equal("null-service-type-key", s.ServiceKey);
            Assert.True(s.IsKeyedService);
        });
    }


    [Fact]
    public void AllowsSameServiceTypeWithDifferentKeys()
    {
        Collection.Scan(scan => scan
            .FromTypes(typeof(SameServiceTypeDifferentKeys))
            .UsingAttributes());

        Assert.Equal(2, Collection.Count);

        var service1 = Collection.First(s => s.ServiceKey?.ToString() == "key1");
        var service2 = Collection.First(s => s.ServiceKey?.ToString() == "key2");

        Assert.Equal(typeof(IKeyedTestService), service1.ServiceType);
        Assert.Equal(typeof(IKeyedTestService), service2.ServiceType);
        Assert.NotEqual(service1.ServiceKey, service2.ServiceKey);
    }

    [Fact]
    public void CanRegisterServiceWithNullKey()
    {
        Collection.Scan(scan => scan
            .FromTypes(typeof(NullKeyedService))
            .UsingAttributes());

        Assert.Single(Collection);

        var service = Collection.Single();
        Assert.Equal(typeof(IKeyedTestService), service.ServiceType);
        Assert.Equal(typeof(NullKeyedService), service.ImplementationType);
        Assert.Null(service.ServiceKey);
        Assert.False(service.IsKeyedService);
    }

    [Fact]
    public void CanResolveServiceWithNullKey()
    {
        var provider = ConfigureProvider(services =>
        {
            services.Scan(scan => scan
                .FromTypes(typeof(NullKeyedService))
                .UsingAttributes());
        });

        var service = provider.GetRequiredService<IKeyedTestService>();
        Assert.IsType<NullKeyedService>(service);
    }
}

// Test interfaces and classes for keyed services
public interface IKeyedTestService { }
public interface IOtherKeyedTestService { }

[ServiceDescriptor(typeof(IKeyedTestService), ServiceLifetime.Transient, "test-key")]
public class KeyedTransientService : IKeyedTestService { }

[ServiceDescriptor<IKeyedTestService>(ServiceLifetime.Scoped, "generic-key")]
public class GenericKeyedService : IKeyedTestService { }

[ServiceDescriptor(typeof(IKeyedTestService), ServiceLifetime.Transient, "key1")]
[ServiceDescriptor(typeof(IKeyedTestService), ServiceLifetime.Singleton, "key2")]
public class MultipleKeyedService : IKeyedTestService { }

[ServiceDescriptor(typeof(IKeyedTestService), ServiceLifetime.Scoped, "mixed-key")]
[ServiceDescriptor(typeof(IKeyedTestService), ServiceLifetime.Transient)]
public class MixedKeyedService : IKeyedTestService { }

[ServiceDescriptor(typeof(IKeyedTestService), ServiceLifetime.Transient, 42)]
public class ObjectKeyedService : IKeyedTestService { }

public enum TestEnum
{
    Value1,
    Value2
}

[ServiceDescriptor(typeof(IKeyedTestService), ServiceLifetime.Transient, TestEnum.Value1)]
public class EnumKeyedService : IKeyedTestService { }

[ServiceDescriptor(typeof(IKeyedTestService), ServiceLifetime.Transient, "service-key")]
[ServiceDescriptor(typeof(IOtherKeyedTestService), ServiceLifetime.Scoped, "other-key")]
public class MultiServiceKeyedService : IKeyedTestService, IOtherKeyedTestService { }

[ServiceDescriptor(null, ServiceLifetime.Transient, "null-service-type-key")]
public class KeyedServiceWithNullServiceType : IKeyedTestService { }


[ServiceDescriptor(typeof(IKeyedTestService), ServiceLifetime.Transient, "key1")]
[ServiceDescriptor(typeof(IKeyedTestService), ServiceLifetime.Scoped, "key2")]
public class SameServiceTypeDifferentKeys : IKeyedTestService { }

[ServiceDescriptor(typeof(IKeyedTestService), ServiceLifetime.Transient, null)]
public class NullKeyedService : IKeyedTestService { }
