using Microsoft.Extensions.DependencyInjection;
using Scrutor.Tests;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Xunit;

namespace Scrutor.Tests
{
    using ChildNamespace;

    public class ScanningTests : TestBase
    {
        private IServiceCollection Collection { get; } = new ServiceCollection();

        [Fact]
        public void Scan_TheseTypes()
        {
            Collection.Scan(scan => scan
                .FromTypes<TransientService1, TransientService2>()
                    .AsImplementedInterfaces(x => x != typeof(IOtherInheritance))
                    .WithSingletonLifetime());

            Assert.Equal(2, Collection.Count);

            Assert.All(Collection, x =>
            {
                Assert.Equal(ServiceLifetime.Singleton, x.Lifetime);
                Assert.Equal(typeof(ITransientService), x.ServiceType);
            });
        }

        [Fact]
        public void UsingRegistrationStrategy_None()
        {
            Collection.Scan(scan => scan
                .FromAssemblyOf<ITransientService>()
                    .AddClasses(classes => classes.AssignableTo<ITransientService>())
                        .AsImplementedInterfaces()
                        .WithTransientLifetime()
                    .AddClasses(classes => classes.AssignableTo<ITransientService>())
                        .AsImplementedInterfaces()
                        .WithSingletonLifetime());

            var services = Collection.GetDescriptors<ITransientService>();

            Assert.Equal(8, services.Count(x => x.ServiceType == typeof(ITransientService)));
        }

        [Fact]
        public void UsingRegistrationStrategy_SkipIfExists()
        {
            Collection.Scan(scan => scan
                .FromAssemblyOf<ITransientService>()
                    .AddClasses(classes => classes.AssignableTo<ITransientService>())
                        .AsImplementedInterfaces()
                        .WithTransientLifetime()
                    .AddClasses(classes => classes.AssignableTo<ITransientService>())
                        .UsingRegistrationStrategy(RegistrationStrategy.Skip)
                        .AsImplementedInterfaces()
                        .WithSingletonLifetime());

            var services = Collection.GetDescriptors<ITransientService>();

            Assert.Equal(4, services.Count(x => x.ServiceType == typeof(ITransientService)));
        }

        [Fact]
        public void UsingRegistrationStrategy_ReplaceDefault()
        {
            Collection.Scan(scan => scan
                .FromAssemblyOf<ITransientService>()
                    .AddClasses(classes => classes.AssignableTo<ITransientService>())
                        .AsImplementedInterfaces()
                        .WithTransientLifetime()
                    .AddClasses(classes => classes.AssignableTo<ITransientService>())
                        .UsingRegistrationStrategy(RegistrationStrategy.Replace())
                        .AsImplementedInterfaces()
                        .WithSingletonLifetime());

            var services = Collection.GetDescriptors<ITransientService>();

            Assert.Equal(1, services.Count(x => x.ServiceType == typeof(ITransientService)));
        }

        [Fact]
        public void UsingRegistrationStrategy_ReplaceServiceTypes()
        {
            Collection.Scan(scan => scan
                .FromAssemblyOf<ITransientService>()
                    .AddClasses(classes => classes.AssignableTo<ITransientService>())
                        .AsImplementedInterfaces()
                        .WithTransientLifetime()
                    .AddClasses(classes => classes.AssignableTo<ITransientService>())
                        .UsingRegistrationStrategy(RegistrationStrategy.Replace(ReplacementBehavior.ServiceType))
                        .AsImplementedInterfaces()
                        .WithSingletonLifetime());

            var services = Collection.GetDescriptors<ITransientService>();

            Assert.Equal(1, services.Count(x => x.ServiceType == typeof(ITransientService)));
        }

        [Fact]
        public void UsingRegistrationStrategy_ReplaceImplementationTypes()
        {
            Collection.Scan(scan => scan
                .FromAssemblyOf<ITransientService>()
                    .AddClasses(classes => classes.AssignableTo<ITransientService>())
                        .AsImplementedInterfaces()
                        .WithTransientLifetime()
                    .AddClasses(classes => classes.AssignableTo<ITransientService>())
                        .UsingRegistrationStrategy(RegistrationStrategy.Replace(ReplacementBehavior.ImplementationType))
                        .AsImplementedInterfaces()
                        .WithSingletonLifetime());

            var services = Collection.GetDescriptors<ITransientService>();

            Assert.Equal(3, services.Count(x => x.ServiceType == typeof(ITransientService)));
        }

        [Fact]
        public void UsingRegistrationStrategy_Throw()
        {
            Assert.Throws<DuplicateTypeRegistrationException>(() =>
                Collection.Scan(scan => scan
                    .FromAssemblyOf<ITransientService>()
                    .AddClasses(classes => classes.AssignableTo<ITransientService>())
                    .AsImplementedInterfaces()
                    .WithTransientLifetime()
                    .AddClasses(classes => classes.AssignableTo<ITransientService>())
                    .UsingRegistrationStrategy(RegistrationStrategy.Throw)
                    .AsImplementedInterfaces()
                    .WithSingletonLifetime()));
        }

        [Fact]
        public void CanFilterTypesToScan()
        {
            Collection.Scan(scan => scan
                .FromAssemblyOf<ITransientService>()
                    .AddClasses(classes => classes.AssignableTo<ITransientService>())
                        .AsImplementedInterfaces(x => x != typeof(IOtherInheritance))
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
        public void LifetimeIsPropagatedToAllRegistrations()
        {
            Collection.Scan(scan => scan.FromAssemblyOf<IScopedService>()
                .AddClasses(classes => classes.AssignableTo<IScopedService>())
                .AsImplementedInterfaces()
                .AsSelf()
                .WithScopedLifetime());

            Assert.All(Collection, service => Assert.Equal(ServiceLifetime.Scoped, service.Lifetime));
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
        public void CanRegisterFullyClosedGenericWithDifferentArityThanServiceType()
        {
            Collection.Scan(scan => scan
                .FromTypes(typeof(PartiallyClosedGeneric<string>))
                .AsImplementedInterfaces()
                .WithTransientLifetime());

            var descriptor = Assert.Single(Collection);

            Assert.Equal(typeof(IPartiallyClosedGeneric<string, int>), descriptor.ServiceType);
        }

        [Fact]
        public void CanScanUsingAttributes()
        {
            var interfaces = new[]
            {
                typeof(ITransientService),
                typeof(ITransientServiceToCombine),
                typeof(IScopedServiceToCombine),
                typeof(ISingletonServiceToCombine),

            };

            Collection.Scan(scan => scan.FromAssemblyOf<ITransientService>()
                .AddClasses(t => t.AssignableToAny(interfaces))
                    .UsingAttributes());

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
                .AddClasses(t => t.AssignableTo<ITransientService>())
                    .UsingAttributes());

            Assert.Single(Collection);

            var service = Collection.GetDescriptor<ITransientService>();

            Assert.NotNull(service);
            Assert.Equal(ServiceLifetime.Transient, service.Lifetime);
            Assert.Equal(typeof(TransientService1), service.ImplementationType);
        }

        [Fact]
        public void CanFilterGenericAttributeTypes()
        {
            Collection.Scan(scan => scan.FromAssemblyOf<IGenericAttribute>()
                .AddClasses(t => t.AssignableTo<IGenericAttribute>())
                    .UsingAttributes());

            Assert.Single(Collection);

            var service = Collection.GetDescriptor<IGenericAttribute>();

            Assert.NotNull(service);
            Assert.Equal(ServiceLifetime.Transient, service.Lifetime);
            Assert.Equal(typeof(GenericAttribute), service.ImplementationType);
        }

        [Fact]
        public void CanCreateDefault()
        {
            var types = new[]
            {
                typeof(IDefault1),
                typeof(IDefault2),
                typeof(IDefault3Level1),
                typeof(IDefault3Level2)
            };

            Collection.Scan(scan => scan.FromAssemblyOf<ITransientService>()
                .AddClasses(t => t.AssignableTo<DefaultAttributes>())
                    .UsingAttributes());

            var remainingSetOfTypes = Collection
                .Select(descriptor => descriptor.ServiceType)
                .Except(types.Concat([typeof(DefaultAttributes)]))
                .ToList();

            Assert.Equal(5, Collection.Count);
            Assert.Empty(remainingSetOfTypes);
        }

        [Fact]
        public void ThrowsOnWrongInheritance()
        {
            var collection = new ServiceCollection();

            var ex = Assert.Throws<InvalidOperationException>(() =>
                collection.Scan(scan => scan.FromAssemblyOf<IWrongInheritanceA>()
                    .AddClasses()
                        .UsingAttributes()));

            Assert.Equal(@"Type ""Scrutor.Tests.WrongInheritance"" is not assignable to ""Scrutor.Tests.IWrongInheritanceA"".", ex.Message);
        }

        [Fact]
        public void ThrowsOnDuplicate()
        {
            var collection = new ServiceCollection();

            var ex = Assert.Throws<InvalidOperationException>(() =>
                collection.Scan(scan => scan.FromAssemblyOf<IDuplicateInheritance>()
                    .AddClasses(t => t.AssignableTo<IDuplicateInheritance>())
                        .UsingAttributes()));

            Assert.Equal(@"Type ""Scrutor.Tests.DuplicateInheritance"" has multiple ServiceDescriptor attributes with the same service type.", ex.Message);
        }

        [Fact]
        public void ThrowsOnDuplicateWithMixedAttributes()
        {
            var collection = new ServiceCollection();

            var ex = Assert.Throws<InvalidOperationException>(() =>
                collection.Scan(scan => scan.FromAssemblyOf<IMixedAttribute>()
                    .AddClasses(t => t.AssignableTo<IMixedAttribute>())
                        .UsingAttributes()));

            Assert.Equal(@"Type ""Scrutor.Tests.MixedAttribute"" has multiple ServiceDescriptor attributes with the same service type.", ex.Message);
        }

        [Fact]
        public void CanHandleMultipleAttributes()
        {
            Collection.Scan(scan => scan.FromAssemblyOf<ITransientServiceToCombine>()
                .AddClasses(t => t.AssignableTo<ITransientServiceToCombine>())
                    .UsingAttributes());

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

            Assert.Equal(8, Collection.Count);

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
                    .AsMatchingInterface((t, x) => x.InNamespaceOf(t))
                    .WithTransientLifetime());

            Assert.Equal(7, Collection.Count);

            var service = Collection.GetDescriptor<ITransientService>();

            Assert.NotNull(service);
            Assert.Equal(ServiceLifetime.Transient, service.Lifetime);
            Assert.Equal(typeof(TransientService), service.ImplementationType);
        }

        [Fact]
        public void ShouldRegisterOpenGenericTypes()
        {
            var genericTypes = new[]
            {
                typeof(OpenGeneric<>),
                typeof(QueryHandler<,>),
                typeof(PartiallyClosedGeneric<>)
            };

            Collection.Scan(scan => scan
                .FromTypes(genericTypes)
                    .AddClasses()
                    .AsImplementedInterfaces());

            var provider = Collection.BuildServiceProvider();

            Assert.NotNull(provider.GetService<IOpenGeneric<int>>());
            Assert.NotNull(provider.GetService<IOpenGeneric<string>>());

            Assert.NotNull(provider.GetService<IQueryHandler<string, float>>());
            Assert.NotNull(provider.GetService<IQueryHandler<double, Guid>>());

            // We don't register partially closed generic types.
            Assert.Null(provider.GetService<IPartiallyClosedGeneric<string, int>>());
        }

        [Fact]
        public void ShouldNotIncludeCompilerGeneratedTypes()
        {
            Assert.Empty(Collection.Scan(scan => scan.FromType<CompilerGenerated>()));
        }

        [Fact]
        public void ShouldNotRegisterTypesInSubNamespace()
        {
            Collection.Scan(scan => scan.FromAssembliesOf(GetType())
                .AddClasses(classes => classes.InExactNamespaceOf<ITransientService>())
                .AsSelf());

            var provider = Collection.BuildServiceProvider();

            Assert.Null(provider.GetService<ClassInChildNamespace>());
        }

        [Fact]
        public void ScanShouldCreateSeparateRegistrationsPerInterface()
        {
            Collection.Scan(scan => scan
                .FromAssemblyOf<CombinedService2>()
                .AddClasses(classes => classes.AssignableTo<CombinedService2>())
                    .AsImplementedInterfaces()
                    .WithSingletonLifetime()
                .AddClasses(classes => classes.AssignableTo<CombinedService2>())
                    .AsSelf()
                    .WithSingletonLifetime());

            Assert.Equal(5, Collection.Count);

            Assert.All(Collection, x =>
            {
                Assert.Equal(ServiceLifetime.Singleton, x.Lifetime);
                Assert.Equal(typeof(CombinedService2), x.ImplementationType);
            });
        }

        [Fact]
        public void AsSelfWithInterfacesShouldForwardRegistrationsToClass()
        {
            Collection.Scan(scan => scan
                .FromAssemblyOf<CombinedService2>()
                .AddClasses(classes => classes.AssignableTo<CombinedService2>())
                    .AsSelfWithInterfaces()
                    .WithSingletonLifetime());

            Assert.Equal(5, Collection.Count);

            var service1 = Collection.GetDescriptor<CombinedService2>();

            Assert.NotNull(service1);
            Assert.Equal(ServiceLifetime.Singleton, service1.Lifetime);
            Assert.Equal(typeof(CombinedService2), service1.ImplementationType);

            var interfaceDescriptors = Collection.Where(x => x.ImplementationType != typeof(CombinedService2)).ToList();
            Assert.Equal(4, interfaceDescriptors.Count);

            Assert.All(interfaceDescriptors, x =>
            {
                Assert.Equal(ServiceLifetime.Singleton, x.Lifetime);
                Assert.NotNull(x.ImplementationFactory);
            });
        }

        [Fact]
        public void AsSelfWithInterfacesShouldCreateTrueSingletons()
        {
            var provider = ConfigureProvider(services =>
            {
                services.Scan(scan => scan
                    .FromAssemblyOf<CombinedService2>()
                     .AddClasses(classes => classes.AssignableTo<CombinedService2>())
                        .AsSelfWithInterfaces()
                        .WithSingletonLifetime());
            });

            var instance1 = provider.GetRequiredService<CombinedService2>();
            var instance2 = provider.GetRequiredService<IDefault1>();
            var instance3 = provider.GetRequiredService<IDefault2>();
            var instance4 = provider.GetRequiredService<IDefault3Level2>();
            var instance5 = provider.GetRequiredService<IDefault3Level1>();

            Assert.Same(instance1, instance2);
            Assert.Same(instance1, instance3);
            Assert.Same(instance1, instance4);
            Assert.Same(instance1, instance5);
        }

        [Fact]
        public void AsSelfWithInterfacesShouldFilterInterfaces()
        {
            var provider = ConfigureProvider(services =>
            {
                services.Scan(scan => scan
                    .FromAssemblyOf<CombinedService2>()
                    .AddClasses(classes => classes.AssignableTo<CombinedService2>())
                    .AsSelfWithInterfaces(x => x == typeof(IDefault1) || x == typeof(CombinedService2))
                    .WithSingletonLifetime());
            });

            var instance1 = provider.GetRequiredService<CombinedService2>();
            var instance2 = provider.GetRequiredService<IDefault1>();
            var instance3 = provider.GetService<IDefault2>();
            var instance4 = provider.GetService<IDefault3Level2>();
            var instance5 = provider.GetService<IDefault3Level1>();

            Assert.Same(instance1, instance2);
            Assert.Null(instance3);
            Assert.Null(instance4);
            Assert.Null(instance5);
        }

        [Fact]
        public void AsSelfWithInterfacesHandlesOpenGenericTypes()
        {
            ConfigureProvider(services =>
            {
                services.Scan(scan => scan
                    .FromAssemblyOf<CombinedService2>()
                    .AddClasses(classes => classes.AssignableTo<IOtherInheritance>())
                    .AsSelfWithInterfaces()
                    .WithSingletonLifetime());
            });
        }

        [Fact]
        public void ShouldAllowOptInToCompilerGeneratedTypes()
        {
            var provider = ConfigureProvider(services =>
            {
                services.Scan(scan => scan
                    .FromAssemblyOf<AllowedCompilerGeneratedSubclass>()
                    .AddClasses(classes => classes
                        .WithAttribute<CompilerGeneratedAttribute>()
                        .AssignableTo<AllowedCompilerGeneratedBase>()
                    )
                    .AsSelf()
                    .WithTransientLifetime());
            });

            var compilerGeneratedSubclass = provider.GetService<AllowedCompilerGeneratedSubclass>();
            Assert.NotNull(compilerGeneratedSubclass);
        }

        [Fact]
        public void CanRegisterWithServiceKey()
        {
            Collection.Scan(scan => scan
                .FromTypes<TransientService1, TransientService2>()
                    .AsImplementedInterfaces(x => x != typeof(IOtherInheritance))
                    .WithServiceKey("my-key")
                    .WithSingletonLifetime());

            Assert.Equal(2, Collection.Count);

            Assert.All(Collection, x =>
            {
                Assert.Equal(ServiceLifetime.Singleton, x.Lifetime);
                Assert.Equal(typeof(ITransientService), x.ServiceType);
                Assert.True(x.IsKeyedService);
                Assert.Equal("my-key", x.ServiceKey);
            });
        }

        [Fact]
        public void CanRegisterWithServiceKeySelector()
        {
            Collection.Scan(scan => scan
                .FromTypes<TransientService1, TransientService2>()
                    .AsImplementedInterfaces(x => x != typeof(IOtherInheritance))
                    .WithServiceKey(type => type.Name)
                    .WithSingletonLifetime());

            Assert.Equal(2, Collection.Count);

            var service1 = Collection.First(x => x.ServiceKey as string == nameof(TransientService1));
            Assert.Equal(typeof(ITransientService), service1.ServiceType);
            Assert.Equal(ServiceLifetime.Singleton, service1.Lifetime);
            Assert.True(service1.IsKeyedService);

            var service2 = Collection.First(x => x.ServiceKey as string == nameof(TransientService2));
            Assert.Equal(typeof(ITransientService), service2.ServiceType);
            Assert.Equal(ServiceLifetime.Singleton, service2.Lifetime);
            Assert.True(service2.IsKeyedService);
        }

        [Fact]
        public void CanResolveKeyedServices()
        {
            Collection.Scan(scan => scan
                .FromTypes<TransientService1, TransientService2>()
                    .AsSelf()
                    .WithServiceKey(type => type.Name)
                    .WithTransientLifetime());

            var provider = Collection.BuildServiceProvider();

            var service1 = provider.GetRequiredKeyedService<TransientService1>(nameof(TransientService1));
            var service2 = provider.GetRequiredKeyedService<TransientService2>(nameof(TransientService2));

            Assert.NotNull(service1);
            Assert.NotNull(service2);
        }
    }

    // ReSharper disable UnusedTypeParameter

    public interface ITransientService { }

    [ServiceDescriptor(typeof(ITransientService))]
    public class TransientService1 : ITransientService { }

    public class TransientService2 : ITransientService, IOtherInheritance { }

    public class TransientService : ITransientService, IEnumerable<string>
    {
        public IEnumerator<string> GetEnumerator()
        {
            throw new NotImplementedException();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException();
        }
    }

    public interface IScopedService { }

    public class ScopedService1 : IScopedService { }

    public class ScopedService2 : IScopedService { }

    public interface IQueryHandler<TQuery, TResult> { }

    public class QueryHandler : IQueryHandler<string, int> { }

    public interface IOpenGeneric<T> : IOtherInheritance { }

    public class OpenGeneric<T> : IOpenGeneric<T> { }

    public interface IPartiallyClosedGeneric<T1, T2> { }

    public class PartiallyClosedGeneric<T> : IPartiallyClosedGeneric<T, int> { }

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

    public interface IDefault1 { }

    public interface IDefault2 { }

    public interface IDefault3Level1 { }

    public interface IDefault3Level2 : IDefault3Level1 { }

    [ServiceDescriptor]
    public class DefaultAttributes : IDefault3Level2, IDefault1, IDefault2 { }

    [CompilerGenerated]
    public class CompilerGenerated { }

    public class CombinedService2 : IDefault1, IDefault2, IDefault3Level2 { }

    public interface IGenericAttribute { }

    [ServiceDescriptor<IGenericAttribute>]
    public class GenericAttribute : IGenericAttribute { }

    public interface IMixedAttribute { }

    [ServiceDescriptor(typeof(IMixedAttribute), ServiceLifetime.Scoped)]
    [ServiceDescriptor<IMixedAttribute>(ServiceLifetime.Singleton)]
    public class MixedAttribute : IMixedAttribute { }

    public abstract class AllowedCompilerGeneratedBase { }

    [CompilerGenerated]
    public class AllowedCompilerGeneratedSubclass : AllowedCompilerGeneratedBase { }
}

namespace Scrutor.Tests.ChildNamespace
{
    public class ClassInChildNamespace { }
}

namespace UnwantedNamespace
{
    public class TransientService : ITransientService
    {
    }
}
