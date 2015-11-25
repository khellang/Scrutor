using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

namespace Scrutor.Tests
{
    public class ScanningTests
    {
        public ScanningTests(ITestOutputHelper output)
        {
            Output = output;
        }

        public ITestOutputHelper Output { get; }

        [Fact]
        public void CanFilterTypesToScan()
        {
            var collection = new ServiceCollection();

            collection.Scan(scan => scan.FromAssemblyOf<ITransientService>()
                .AddClasses(classes => classes.AssignableTo<ITransientService>())
                    .AsImplementedInterfaces()
                    .WithTransientLifetime());

            Output.WriteLine(string.Join(", ", collection));

            var services = collection.GetDescriptors<ITransientService>();

            Assert.Equal(services, collection);

            Assert.All(services, service =>
            {
                Assert.Equal(ServiceLifetime.Transient, service.Lifetime);
                Assert.Equal(typeof(ITransientService), service.ServiceType);
            });
        }

        [Fact]
        public void CanRegisterAsSpecificType()
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
        public void CanSpecifyLifetime()
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
        public void CanRegisterGenericTypes()
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

        [Fact]
        public void CanScanUsingAttributes()
        {
            var collection = new ServiceCollection();

            collection.Scan(scan => scan.FromAssemblyOf<ITransientService>().AddFromAttributes());

            var service = collection.GetDescriptor<ITransientService>();

            Assert.NotNull(service);
            Assert.Equal(ServiceLifetime.Transient, service.Lifetime);
            Assert.Equal(typeof(TransientService1), service.ImplementationType);
        }
    }

    public interface ITransientService { }

    [ServiceDescriptor(typeof(ITransientService))]
    public class TransientService1 : ITransientService { }

    public class TransientService2 : ITransientService { }

    public interface IScopedService { }

    public class ScopedService1 : IScopedService { }

    public class ScopedService2 : IScopedService { }

    public interface IQueryHandler<TQuery, TResult> { }

    public class QueryHandler : IQueryHandler<string, int> { }
}