using Microsoft.Framework.DependencyInjection;
using Xunit;

namespace Microsoft.Extensions.DependencyInjection.Scanning.Tests
{
    public static class ScanningTests
    {
        [Fact]
        public static void CanFilterTypesToScan()
        {
            var collection = new ServiceCollection();

            collection.Scan(scan => scan.FromAssemblyOf<ITransientService>()
                .AddClasses(classes => classes.AssignableTo<ITransientService>()));

            var service1 = collection.GetDescriptor<TransientService1>();
            var service2 = collection.GetDescriptor<TransientService2>();

            var services = new[] { service1, service2 };

            Assert.Equal(services, collection);

            Assert.All(services, service =>
            {
                Assert.Equal(ServiceLifetime.Transient, service.Lifetime);
                Assert.Equal(service.ImplementationType, service.ServiceType);
            });
        }

        [Fact]
        public static void CanRegisterAsSpecificType()
        {
            var collection = new ServiceCollection();

            collection.Scan(scan => scan.FromAssemblyOf<ITransientService>()
                .AddClasses(classes => classes.AssignableTo<ITransientService>())
                    .As<ITransientService>());

            var services = collection.GetDescriptors<ITransientService>();

            Assert.Equal(services, collection);

            Assert.All(services, service =>
            {
                Assert.Equal(ServiceLifetime.Transient, service.Lifetime);
                Assert.Equal(typeof(ITransientService), service.ServiceType);
            });
        }

        [Fact]
        public static void CanSpecifyLifetime()
        {
            var collection = new ServiceCollection();

            collection.Scan(scan => scan.FromAssemblyOf<IScopedService>()
                .AddClasses(classes => classes.AssignableTo<IScopedService>())
                    .AsImplementedInterfaces()
                    .WithScopedLifetime());

            var services = collection.GetDescriptors<IScopedService>();

            Assert.Equal(services, collection);

            Assert.All(services, service =>
            {
                Assert.Equal(ServiceLifetime.Scoped, service.Lifetime);
                Assert.Equal(typeof(IScopedService), service.ServiceType);
            });
        }

        [Fact]
        public static void CanRegisterGenericTypes()
        {
            var collection = new ServiceCollection();

            collection.Scan(scan => scan.FromAssemblyOf<IScopedService>()
                .AddClasses(classes => classes.AssignableTo(typeof(IQueryHandler<,>)))
                    .AsImplementedInterfaces()
                    .WithScopedLifetime());

            var service = collection.GetDescriptor<IQueryHandler<string, int>>();

            Assert.NotNull(service);
            Assert.Equal(ServiceLifetime.Scoped, service.Lifetime);
            Assert.Equal(typeof(QueryHandler), service.ImplementationType);
        }
    }

    public interface ITransientService { }

    public class TransientService1 : ITransientService { }

    public class TransientService2 : ITransientService { }

    public interface IScopedService { }

    public class ScopedService1 : IScopedService { }

    public class ScopedService2 : IScopedService { }

    public interface IQueryHandler<TQuery, TResult> { }

    public class QueryHandler : IQueryHandler<string, int> { }
}