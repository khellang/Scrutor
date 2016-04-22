using Microsoft.Extensions.DependencyInjection;
using Scrutor.Tests;
using System;
using Xunit;

namespace Scrutor.Tests
{
    public class ScanningTests
    {
        private IServiceCollection Collection { get; } = new ServiceCollection();

        [Fact]
        public void CanFilterTypesToScan()
        {
            Collection.Scan(scan => scan.FromAssemblyOf<ITransientService>()
                .AddClasses(classes => classes.AssignableTo<ITransientService>())
                    .AsImplementedInterfaces()
                    .WithTransientLifetime());

            var services = Collection.GetDescriptors<ITransientService>();

            Assert.Equal(services, Collection);

            Assert.All(services, service =>
            {
                Assert.Equal(ServiceLifetime.Transient, service.Lifetime);
                Assert.Equal(typeof(ITransientService), service.ServiceType);
            });
        }

        [Fact]
        public void CanRegisterAsSpecificType()
        {
            Collection.Scan(scan => scan.FromAssemblyOf<ITransientService>()
                .AddClasses(classes => classes.AssignableTo<ITransientService>())
                    .As<ITransientService>());

            var services = Collection.GetDescriptors<ITransientService>();

            Assert.Equal(services, Collection);

            Assert.All(services, service =>
            {
                Assert.Equal(ServiceLifetime.Transient, service.Lifetime);
                Assert.Equal(typeof(ITransientService), service.ServiceType);
            });
        }

        [Fact]
        public void CanSpecifyLifetime()
        {
            Collection.Scan(scan => scan.FromAssemblyOf<IScopedService>()
                .AddClasses(classes => classes.AssignableTo<IScopedService>())
                    .AsImplementedInterfaces()
                    .WithScopedLifetime());

            var services = Collection.GetDescriptors<IScopedService>();

            Assert.Equal(services, Collection);

            Assert.All(services, service =>
            {
                Assert.Equal(ServiceLifetime.Scoped, service.Lifetime);
                Assert.Equal(typeof(IScopedService), service.ServiceType);
            });
        }

        [Fact]
        public void CanRegisterGenericTypes()
        {
            Collection.Scan(scan => scan.FromAssemblyOf<IScopedService>()
                .AddClasses(classes => classes.AssignableTo(typeof(IQueryHandler<,>)))
                    .AsImplementedInterfaces()
                    .WithScopedLifetime());

            var service = Collection.GetDescriptor<IQueryHandler<string, int>>();

            Assert.NotNull(service);
            Assert.Equal(ServiceLifetime.Scoped, service.Lifetime);
            Assert.Equal(typeof(QueryHandler), service.ImplementationType);
        }

        [Fact]
        public void CanScanUsingAttributes()
        {
            var interfaces = new []
                {
                    typeof(ITransientService),
                    typeof(ITransientServiceToCombine),
                    typeof(IScopedServiceToCombine),
                    typeof(ISingletonServiceToCombine),

                };
            Collection.Scan(scan => scan.FromAssemblyOf<ITransientService>()
                    .AddFromAttributes(t => t.AssignableToAny(interfaces)));


            Assert.Equal(4, Collection.Count);

            var service = Collection.GetDescriptor<ITransientService>();

            Assert.NotNull(service);
            Assert.Equal(ServiceLifetime.Transient, service.Lifetime);
            Assert.Equal(typeof(TransientService1), service.ImplementationType);
        }

        [Fact]
        public void CanFilterAttributeTypes()
        {
            Collection.Scan(scan => scan.FromAssemblyOf<ITransientService>()
                .AddFromAttributes(t => t.AssignableTo<ITransientService>()));

            Assert.Equal(Collection.Count, 1);

            var service = Collection.GetDescriptor<ITransientService>();

            Assert.NotNull(service);
            Assert.Equal(ServiceLifetime.Transient, service.Lifetime);
            Assert.Equal(typeof(TransientService1), service.ImplementationType);
        }

        [Fact]
        public void ThrowsOnWrongInheritance()
        {
            var collection = new ServiceCollection();

            var ex = Assert.Throws<InvalidOperationException>(()=>
                collection.Scan(scan => scan.FromAssemblyOf<IWrongInheritanceA>().AddFromAttributes()));

            Assert.Equal("Type \"Scrutor.Tests.WrongInheritance\" does not inherit or implement \"$Scrutor.Tests.IWrongInheritanceA\".", ex.Message);
        }

        [Fact]
        public void ThrowsOnDuplicate()
        {
            var collection = new ServiceCollection();

            var ex = Assert.Throws<InvalidOperationException>(()=>
                collection.Scan(scan => scan.FromAssemblyOf<IDuplicateInheritance>()
                    .AddFromAttributes(t => t.AssignableTo<IDuplicateInheritance>())));

            Assert.Equal("Type \"Scrutor.Tests.DuplicateInheritance\" has multiple ServiceDescriptors specified with the same service type.", ex.Message);
        }

        [Fact]
        public void CanHandleMultipleAttributes()
        {
            Collection.Scan(scan => scan.FromAssemblyOf<ITransientServiceToCombine>()
                .AddFromAttributes(t => t.AssignableTo<ITransientServiceToCombine>()));

            var transientService = Collection.GetDescriptor<ITransientServiceToCombine>();

            Assert.NotNull(transientService);
            Assert.Equal(ServiceLifetime.Transient, transientService.Lifetime);
            Assert.Equal(typeof(CombinedService), transientService.ImplementationType);

            var scopedService = Collection.GetDescriptor<IScopedServiceToCombine>();

            Assert.NotNull(scopedService);
            Assert.Equal(ServiceLifetime.Scoped, scopedService.Lifetime);
            Assert.Equal(typeof(CombinedService), scopedService.ImplementationType);

            var singletonService = Collection.GetDescriptor<ISingletonServiceToCombine>();

            Assert.NotNull(singletonService);
            Assert.Equal(ServiceLifetime.Singleton, singletonService.Lifetime);
            Assert.Equal(typeof(CombinedService), singletonService.ImplementationType);
        }

        [Fact]
        public void AutoRegisterAsMatchingInterface()
        {
            Collection.Scan(scan => scan.FromAssemblyOf<ITransientService>()
                .AddClasses()
                    .AsMatchingInterface()
                    .WithTransientLifetime());

            Assert.Equal(3, Collection.Count);

            var services = Collection.GetDescriptors<ITransientService>();

            Assert.NotNull(services);
            Assert.All(services, s =>
            {
                Assert.Equal(ServiceLifetime.Transient, s.Lifetime);
                Assert.Equal(typeof(ITransientService), s.ServiceType);
            });
        }

        [Fact]
        public void AutoRegisterAsMatchingInterfaceSameNamespaceOnly()
        {
            Collection.Scan(scan => scan.FromAssemblyOf<ITransientService>()
                .AddClasses()
                    .AsMatchingInterface((t, x) => x.InNamespaces(t.Namespace))
                    .WithTransientLifetime());

            Assert.Equal(2, Collection.Count);

            var service = Collection.GetDescriptor<ITransientService>();

            Assert.NotNull(service);
            Assert.Equal(ServiceLifetime.Transient, service.Lifetime);
            Assert.Equal(typeof(TransientService), service.ImplementationType);
        }

        [Fact]
        public void ShouldNotRegisterGenericTypesWithMissingParameters()
        {
            Collection.Scan(scan => scan
                .FromAssemblyOf<ITransientService>()
                    .AddClasses(x => x.AssignableTo(typeof(BaseQueryHandler<>)))
                    .AsImplementedInterfaces());

            // This would throw if generic type definitions were registered.
            Collection.BuildServiceProvider();
        }
    }

    public interface ITransientService { }

    [ServiceDescriptor(typeof(ITransientService))]
    public class TransientService1 : ITransientService { }

    public class TransientService2 : ITransientService { }

    public class TransientService : ITransientService { }

    public interface IScopedService { }

    public class ScopedService1 : IScopedService { }

    public class ScopedService2 : IScopedService { }

    public interface IQueryHandler<TQuery, TResult> { }

    public class QueryHandler : IQueryHandler<string, int> { }

    public class BaseQueryHandler<T> : IQueryHandler<T, int> { }

    public interface ITransientServiceToCombine { }

    public interface IScopedServiceToCombine { }

    public interface ISingletonServiceToCombine { }

    [ServiceDescriptor(typeof(ITransientServiceToCombine))]
    [ServiceDescriptor(typeof(IScopedServiceToCombine), ServiceLifetime.Scoped)]
    [ServiceDescriptor(typeof(ISingletonServiceToCombine), ServiceLifetime.Singleton)]
    public class CombinedService : ITransientServiceToCombine, IScopedServiceToCombine, ISingletonServiceToCombine { }

    public interface IWrongInheritanceA { }
    public interface IWrongInheritanceB { }

    [ServiceDescriptor(typeof(IWrongInheritanceA))]
    public class WrongInheritance : IWrongInheritanceB { }


    public interface IDuplicateInheritance { }
    public interface IOtherInheritance { }

    [ServiceDescriptor(typeof(IOtherInheritance))]
    [ServiceDescriptor(typeof(IDuplicateInheritance))]
    [ServiceDescriptor(typeof(IDuplicateInheritance))]
    public class DuplicateInheritance : IDuplicateInheritance, IOtherInheritance { }
}

namespace UnwantedNamespace
{
    public class TransientService : ITransientService
    {
    }
}
