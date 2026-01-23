using Microsoft.CodeAnalysis;
using Scrutor.Generators.Tests.Verifiers;
using static VerifyXunit.Verifier;

namespace Scrutor.Generators.Tests;

/// <summary>
/// Tests for Scan() method chain analysis and interceptor generation.
/// </summary>
public class ScanChainGenerationTests
{
    [Fact]
    public void ScanChain_WithFromAssemblyOf_IsAnalyzed()
    {
        const string source = """
            using Microsoft.Extensions.DependencyInjection;
            using Scrutor;

            public interface IMyService { }
            public class MyService : IMyService { }

            var services = new ServiceCollection();
            services.Scan(scan => scan
                .FromAssemblyOf<MyService>()
                .AddClasses(c => c.AssignableTo<IMyService>())
                .AsImplementedInterfaces()
                .WithSingletonLifetime());
            """;

        var (diagnostics, _) = GeneratorTestHelper.RunGenerator(source);
        Assert.Empty(diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));
    }

    [Fact]
    public void ScanChain_GeneratesOnlyInterceptorsFile()
    {
        const string source = """
            using Microsoft.Extensions.DependencyInjection;
            using Scrutor;

            public interface IMyService { }
            public class MyService : IMyService { }

            var services = new ServiceCollection();
            services.Scan(scan => scan
                .FromAssemblyOf<MyService>()
                .AddClasses(c => c.AssignableTo<IMyService>())
                .AsImplementedInterfaces()
                .WithSingletonLifetime());
            """;

        var (_, sources) = GeneratorTestHelper.RunGeneratorFull(source);

        Assert.True(sources.ContainsKey("ScrutorInterceptors.g.cs"), "Should generate interceptors file");
        Assert.Single(sources);
    }

    [Fact]
    public Task ScanChain_GeneratesInterceptor_Verify()
    {
        const string source = """
            using Microsoft.Extensions.DependencyInjection;
            using Scrutor;

            public interface IMyService { }
            public class MyService : IMyService { }

            var services = new ServiceCollection();
            services.Scan(scan => scan
                .FromAssemblyOf<MyService>()
                .AddClasses(c => c.AssignableTo<IMyService>())
                .AsImplementedInterfaces()
                .WithSingletonLifetime());
            """;

        var driver = GeneratorTestHelper.CreateDriver(source);
        return Verify(driver);
    }

    [Fact]
    public Task ScanChain_WithUsingAttributes_Verify()
    {
        const string source = """
            using Microsoft.Extensions.DependencyInjection;
            using Scrutor;

            [ServiceDescriptor]
            public class MyService { }

            var services = new ServiceCollection();
            services.Scan(scan => scan
                .FromAssemblyOf<MyService>()
                .AddClasses()
                .UsingAttributes());
            """;

        var driver = GeneratorTestHelper.CreateDriver(source);
        return Verify(driver);
    }

    [Fact]
    public Task MultipleScanCalls_GenerateMultipleInterceptors_Verify()
    {
        const string source = """
            using Microsoft.Extensions.DependencyInjection;
            using Scrutor;

            public interface IService { }
            public class MyService : IService { }

            var services = new ServiceCollection();
            services.Scan(scan => scan
                .FromAssemblyOf<MyService>()
                .AddClasses()
                .AsSelf());

            services.Scan(scan => scan
                .FromAssemblyOf<MyService>()
                .AddClasses()
                .AsImplementedInterfaces());
            """;

        var driver = GeneratorTestHelper.CreateDriver(source);
        return Verify(driver);
    }

    [Fact]
    public Task ScanChain_WithUsingAttributes_MultipleServices_Verify()
    {
        const string source = """
            using Microsoft.Extensions.DependencyInjection;
            using Scrutor;

            public interface IService1 { }
            public interface IService2 { }

            [ServiceDescriptor(typeof(IService1), ServiceLifetime.Singleton)]
            public class Service1 : IService1 { }

            [ServiceDescriptor(typeof(IService2), ServiceLifetime.Scoped)]
            public class Service2 : IService2 { }

            var services = new ServiceCollection();
            services.Scan(scan => scan
                .FromAssemblyOf<Service1>()
                .AddClasses()
                .UsingAttributes());
            """;

        var driver = GeneratorTestHelper.CreateDriver(source);
        return Verify(driver);
    }

    [Fact]
    public void NoScanCalls_GeneratesNoOutput()
    {
        const string source = """
            using Scrutor;

            [ServiceDescriptor]
            public class MyService { }
            """;

        var (_, sources) = GeneratorTestHelper.RunGeneratorFull(source);
        Assert.Empty(sources);
    }

    [Fact]
    public void GenerationDisabled_ProducesNoOutput()
    {
        const string source = """
            using Microsoft.Extensions.DependencyInjection;
            using Scrutor;

            public class MyService { }

            var services = new ServiceCollection();
            services.Scan(scan => scan
                .FromAssemblyOf<MyService>()
                .AddClasses()
                .AsSelf());
            """;

        var (_, sources) = GeneratorTestHelper.RunGeneratorFull(source, enableGeneration: false);
        Assert.Empty(sources);
    }

    [Fact]
    public void Interceptor_HasCorrectStructure()
    {
        const string source = """
            using Microsoft.Extensions.DependencyInjection;
            using Scrutor;

            public class MyService { }

            var services = new ServiceCollection();
            services.Scan(scan => scan
                .FromAssemblyOf<MyService>()
                .AddClasses()
                .AsSelf());
            """;

        var (_, sources) = GeneratorTestHelper.RunGeneratorFull(source);
        var interceptorSource = sources["ScrutorInterceptors.g.cs"];

        Assert.Contains("namespace Scrutor.Generated", interceptorSource);
        Assert.Contains("file sealed class InterceptsLocationAttribute", interceptorSource);
        Assert.Contains("[global::System.CodeDom.Compiler.GeneratedCodeAttribute(\"Scrutor.Generators\"", interceptorSource);
        Assert.Contains("file static class", interceptorSource);
        Assert.Contains("InterceptsLocation", interceptorSource);
    }

    #region Type Filters

    [Fact]
    public Task TypeFilter_AssignableTo_Verify()
    {
        const string source = """
            using Microsoft.Extensions.DependencyInjection;
            using Scrutor;

            public interface IService { }
            public class ServiceA : IService { }
            public class ServiceB : IService { }
            public class NotAService { }

            var services = new ServiceCollection();
            services.Scan(scan => scan
                .FromAssemblyOf<ServiceA>()
                .AddClasses(c => c.AssignableTo<IService>())
                .AsImplementedInterfaces()
                .WithTransientLifetime());
            """;

        var driver = GeneratorTestHelper.CreateDriver(source);
        return Verify(driver);
    }

    [Fact]
    public Task TypeFilter_InNamespace_Verify()
    {
        const string source = """
            using Microsoft.Extensions.DependencyInjection;
            using Scrutor;

            namespace MyApp.Services
            {
                public interface IService { }
                public class MyService : IService { }
            }

            namespace MyApp.Other
            {
                public class OtherClass { }
            }

            var services = new ServiceCollection();
            services.Scan(scan => scan
                .FromAssemblyOf<MyApp.Services.MyService>()
                .AddClasses(c => c.InNamespaces("MyApp.Services"))
                .AsSelf());
            """;

        var driver = GeneratorTestHelper.CreateDriver(source);
        return Verify(driver);
    }

    [Fact]
    public Task TypeFilter_InExactNamespace_Verify()
    {
        const string source = """
            using Microsoft.Extensions.DependencyInjection;
            using Scrutor;

            namespace MyApp.Services
            {
                public class ServiceA { }
            }

            namespace MyApp.Services.Sub
            {
                public class ServiceB { }
            }

            var services = new ServiceCollection();
            services.Scan(scan => scan
                .FromAssemblyOf<MyApp.Services.ServiceA>()
                .AddClasses(c => c.InExactNamespaces("MyApp.Services"))
                .AsSelf());
            """;

        var driver = GeneratorTestHelper.CreateDriver(source);
        return Verify(driver);
    }

    [Fact]
    public Task TypeFilter_WithAttribute_Verify()
    {
        const string source = """
            using System;
            using Microsoft.Extensions.DependencyInjection;
            using Scrutor;

            [AttributeUsage(AttributeTargets.Class)]
            public class MyServiceAttribute : Attribute { }

            public interface IService { }

            [MyService]
            public class DecoratedService : IService { }

            public class PlainService : IService { }

            var services = new ServiceCollection();
            services.Scan(scan => scan
                .FromAssemblyOf<DecoratedService>()
                .AddClasses(c => c.WithAttribute<MyServiceAttribute>())
                .AsImplementedInterfaces());
            """;

        var driver = GeneratorTestHelper.CreateDriver(source);
        return Verify(driver);
    }

    [Fact]
    public Task TypeFilter_WithoutAttribute_Verify()
    {
        const string source = """
            using System;
            using Microsoft.Extensions.DependencyInjection;
            using Scrutor;

            [AttributeUsage(AttributeTargets.Class)]
            public class ExcludeAttribute : Attribute { }

            public interface IService { }

            [Exclude]
            public class ExcludedService : IService { }

            public class IncludedService : IService { }

            var services = new ServiceCollection();
            services.Scan(scan => scan
                .FromAssemblyOf<IncludedService>()
                .AddClasses(c => c.AssignableTo<IService>().WithoutAttribute<ExcludeAttribute>())
                .AsImplementedInterfaces());
            """;

        var driver = GeneratorTestHelper.CreateDriver(source);
        return Verify(driver);
    }

    #endregion

    #region Service Type Mappings

    [Fact]
    public Task ServiceMapping_AsSelf_Verify()
    {
        const string source = """
            using Microsoft.Extensions.DependencyInjection;
            using Scrutor;

            public interface IService { }
            public class MyService : IService { }

            var services = new ServiceCollection();
            services.Scan(scan => scan
                .FromAssemblyOf<MyService>()
                .AddClasses(c => c.AssignableTo<IService>())
                .AsSelf());
            """;

        var driver = GeneratorTestHelper.CreateDriver(source);
        return Verify(driver);
    }

    [Fact]
    public Task ServiceMapping_AsMatchingInterface_Verify()
    {
        const string source = """
            using Microsoft.Extensions.DependencyInjection;
            using Scrutor;

            public interface IMyService { }
            public interface IOtherInterface { }
            public class MyService : IMyService, IOtherInterface { }

            var services = new ServiceCollection();
            services.Scan(scan => scan
                .FromAssemblyOf<MyService>()
                .AddClasses(c => c.AssignableTo<IMyService>())
                .AsMatchingInterface());
            """;

        var driver = GeneratorTestHelper.CreateDriver(source);
        return Verify(driver);
    }

    [Fact]
    public Task ServiceMapping_AsExplicitType_Verify()
    {
        const string source = """
            using Microsoft.Extensions.DependencyInjection;
            using Scrutor;

            public interface IService { }
            public class MyService : IService { }

            var services = new ServiceCollection();
            services.Scan(scan => scan
                .FromAssemblyOf<MyService>()
                .AddClasses(c => c.AssignableTo<IService>())
                .As<IService>());
            """;

        var driver = GeneratorTestHelper.CreateDriver(source);
        return Verify(driver);
    }

    [Fact]
    public Task ServiceMapping_AsSelfWithInterfaces_Verify()
    {
        const string source = """
            using Microsoft.Extensions.DependencyInjection;
            using Scrutor;

            public interface IService { }
            public class MyService : IService { }

            var services = new ServiceCollection();
            services.Scan(scan => scan
                .FromAssemblyOf<MyService>()
                .AddClasses(c => c.AssignableTo<IService>())
                .AsSelfWithInterfaces());
            """;

        var driver = GeneratorTestHelper.CreateDriver(source);
        return Verify(driver);
    }

    #endregion

    #region Lifetimes

    [Fact]
    public Task Lifetime_Scoped_Verify()
    {
        const string source = """
            using Microsoft.Extensions.DependencyInjection;
            using Scrutor;

            public interface IService { }
            public class MyService : IService { }

            var services = new ServiceCollection();
            services.Scan(scan => scan
                .FromAssemblyOf<MyService>()
                .AddClasses(c => c.AssignableTo<IService>())
                .AsImplementedInterfaces()
                .WithScopedLifetime());
            """;

        var driver = GeneratorTestHelper.CreateDriver(source);
        return Verify(driver);
    }

    [Fact]
    public Task Lifetime_Transient_Verify()
    {
        const string source = """
            using Microsoft.Extensions.DependencyInjection;
            using Scrutor;

            public interface IService { }
            public class MyService : IService { }

            var services = new ServiceCollection();
            services.Scan(scan => scan
                .FromAssemblyOf<MyService>()
                .AddClasses(c => c.AssignableTo<IService>())
                .AsImplementedInterfaces()
                .WithTransientLifetime());
            """;

        var driver = GeneratorTestHelper.CreateDriver(source);
        return Verify(driver);
    }

    [Fact]
    public Task Lifetime_WithLifetimeMethod_Verify()
    {
        const string source = """
            using Microsoft.Extensions.DependencyInjection;
            using Scrutor;

            public interface IService { }
            public class MyService : IService { }

            var services = new ServiceCollection();
            services.Scan(scan => scan
                .FromAssemblyOf<MyService>()
                .AddClasses(c => c.AssignableTo<IService>())
                .AsImplementedInterfaces()
                .WithLifetime(ServiceLifetime.Singleton));
            """;

        var driver = GeneratorTestHelper.CreateDriver(source);
        return Verify(driver);
    }

    #endregion

    #region Keyed Services

    [Fact]
    public Task KeyedService_WithStringKey_Verify()
    {
        const string source = """
            using Microsoft.Extensions.DependencyInjection;
            using Scrutor;

            public interface IService { }
            public class MyService : IService { }

            var services = new ServiceCollection();
            services.Scan(scan => scan
                .FromAssemblyOf<MyService>()
                .AddClasses(c => c.AssignableTo<IService>())
                .AsImplementedInterfaces()
                .WithSingletonLifetime()
                .WithServiceKey("myKey"));
            """;

        var driver = GeneratorTestHelper.CreateDriver(source);
        return Verify(driver);
    }

    #endregion

    #region Registration Strategies

    [Fact]
    public Task RegistrationStrategy_Skip_Verify()
    {
        const string source = """
            using Microsoft.Extensions.DependencyInjection;
            using Scrutor;

            public interface IService { }
            public class MyService : IService { }

            var services = new ServiceCollection();
            services.Scan(scan => scan
                .FromAssemblyOf<MyService>()
                .AddClasses(c => c.AssignableTo<IService>())
                .AsImplementedInterfaces()
                .UsingRegistrationStrategy(RegistrationStrategy.Skip));
            """;

        var driver = GeneratorTestHelper.CreateDriver(source);
        return Verify(driver);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public Task EdgeCase_AbstractClassSkipped_Verify()
    {
        const string source = """
            using Microsoft.Extensions.DependencyInjection;
            using Scrutor;

            public interface IService { }
            public abstract class AbstractService : IService { }
            public class ConcreteService : AbstractService { }

            var services = new ServiceCollection();
            services.Scan(scan => scan
                .FromAssemblyOf<ConcreteService>()
                .AddClasses(c => c.AssignableTo<IService>())
                .AsImplementedInterfaces());
            """;

        var driver = GeneratorTestHelper.CreateDriver(source);
        return Verify(driver);
    }

    [Fact]
    public Task EdgeCase_MultipleImplementations_Verify()
    {
        const string source = """
            using Microsoft.Extensions.DependencyInjection;
            using Scrutor;

            public interface IRepository { }
            public class UserRepository : IRepository { }
            public class OrderRepository : IRepository { }
            public class ProductRepository : IRepository { }

            var services = new ServiceCollection();
            services.Scan(scan => scan
                .FromAssemblyOf<UserRepository>()
                .AddClasses(c => c.AssignableTo<IRepository>())
                .AsImplementedInterfaces()
                .WithScopedLifetime());
            """;

        var driver = GeneratorTestHelper.CreateDriver(source);
        return Verify(driver);
    }

    [Fact]
    public Task EdgeCase_NestedClass_Verify()
    {
        const string source = """
            using Microsoft.Extensions.DependencyInjection;
            using Scrutor;

            public interface IService { }

            public class OuterClass
            {
                public class NestedService : IService { }
            }

            var services = new ServiceCollection();
            services.Scan(scan => scan
                .FromAssemblyOf<OuterClass.NestedService>()
                .AddClasses(c => c.AssignableTo<IService>())
                .AsImplementedInterfaces());
            """;

        var driver = GeneratorTestHelper.CreateDriver(source);
        return Verify(driver);
    }

    [Fact]
    public Task EdgeCase_GenericService_Verify()
    {
        const string source = """
            using Microsoft.Extensions.DependencyInjection;
            using Scrutor;

            public interface IRepository<T> { }
            public class Repository<T> : IRepository<T> { }
            public class StringRepository : IRepository<string> { }

            var services = new ServiceCollection();
            services.Scan(scan => scan
                .FromAssemblyOf<StringRepository>()
                .AddClasses(c => c.AssignableTo(typeof(IRepository<>)))
                .AsImplementedInterfaces());
            """;

        var driver = GeneratorTestHelper.CreateDriver(source);
        return Verify(driver);
    }

    [Fact]
    public Task EdgeCase_NoMatchingTypes_Verify()
    {
        const string source = """
            using Microsoft.Extensions.DependencyInjection;
            using Scrutor;

            public interface IService { }
            public class NotImplementingService { }

            var services = new ServiceCollection();
            services.Scan(scan => scan
                .FromAssemblyOf<NotImplementingService>()
                .AddClasses(c => c.AssignableTo<IService>())
                .AsImplementedInterfaces());
            """;

        var driver = GeneratorTestHelper.CreateDriver(source);
        return Verify(driver);
    }

    [Fact]
    public Task EdgeCase_MultipleInterfaces_Verify()
    {
        const string source = """
            using Microsoft.Extensions.DependencyInjection;
            using Scrutor;

            public interface IFirst { }
            public interface ISecond { }
            public interface IThird { }
            public class MultiInterfaceService : IFirst, ISecond, IThird { }

            var services = new ServiceCollection();
            services.Scan(scan => scan
                .FromAssemblyOf<MultiInterfaceService>()
                .AddClasses(c => c.AssignableTo<IFirst>())
                .AsImplementedInterfaces()
                .WithSingletonLifetime());
            """;

        var driver = GeneratorTestHelper.CreateDriver(source);
        return Verify(driver);
    }

    #endregion

    #region Unsupported Patterns

    [Fact]
    public Task UnsupportedPattern_FromCallingAssembly_Verify()
    {
        const string source = """
            using Microsoft.Extensions.DependencyInjection;
            using Scrutor;

            var services = new ServiceCollection();
            services.Scan(scan => scan
                .FromCallingAssembly()
                .AddClasses()
                .AsSelf());
            """;

        var driver = GeneratorTestHelper.CreateDriver(source);
        return Verify(driver);
    }

    [Fact]
    public Task UnsupportedPattern_CustomWherePredicate_Verify()
    {
        const string source = """
            using Microsoft.Extensions.DependencyInjection;
            using Scrutor;

            public class MyService { }

            var services = new ServiceCollection();
            services.Scan(scan => scan
                .FromAssemblyOf<MyService>()
                .AddClasses(c => c.Where(t => t.Name.StartsWith("Service")))
                .AsSelf());
            """;

        var driver = GeneratorTestHelper.CreateDriver(source);
        return Verify(driver);
    }

    #endregion

    #region Open Generics

    [Fact]
    public Task OpenGeneric_AsImplementedInterfaces_Verify()
    {
        const string source = """
            using Microsoft.Extensions.DependencyInjection;
            using Scrutor;

            public interface IRepository<T> { }
            public class Repository<T> : IRepository<T> { }

            var services = new ServiceCollection();
            services.Scan(scan => scan
                .FromAssemblyOf<Repository<object>>()
                .AddClasses(c => c.AssignableTo(typeof(IRepository<>)))
                .AsImplementedInterfaces()
                .WithScopedLifetime());
            """;

        var driver = GeneratorTestHelper.CreateDriver(source);
        return Verify(driver);
    }

    [Fact]
    public Task OpenGeneric_AsSelf_Verify()
    {
        const string source = """
            using Microsoft.Extensions.DependencyInjection;
            using Scrutor;

            public class GenericService<T> { }

            var services = new ServiceCollection();
            services.Scan(scan => scan
                .FromAssemblyOf<GenericService<object>>()
                .AddClasses(c => c.AssignableTo(typeof(GenericService<>)))
                .AsSelf()
                .WithTransientLifetime());
            """;

        var driver = GeneratorTestHelper.CreateDriver(source);
        return Verify(driver);
    }

    #endregion

    #region Nested Classes

    [Fact]
    public Task NestedClass_AsImplementedInterfaces_Verify()
    {
        const string source = """
            using Microsoft.Extensions.DependencyInjection;
            using Scrutor;

            public class Outer
            {
                public interface INestedService { }
                public class NestedService : INestedService { }
            }

            var services = new ServiceCollection();
            services.Scan(scan => scan
                .FromAssemblyOf<Outer.NestedService>()
                .AddClasses(c => c.AssignableTo<Outer.INestedService>())
                .AsImplementedInterfaces()
                .WithSingletonLifetime());
            """;

        var driver = GeneratorTestHelper.CreateDriver(source);
        return Verify(driver);
    }

    #endregion
}
