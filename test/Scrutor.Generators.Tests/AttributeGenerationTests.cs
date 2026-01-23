using Scrutor.Generators.Tests.Verifiers;

namespace Scrutor.Generators.Tests;

/// <summary>
/// Tests for [ServiceDescriptor] attribute detection through Scan() calls.
/// </summary>
public class AttributeGenerationTests
{
    [Fact]
    public Task UsingAttributes_WithExplicitServiceType_Verify()
    {
        const string source = """
            using Scrutor;
            using Microsoft.Extensions.DependencyInjection;

            public interface IMyService { }

            [ServiceDescriptor(typeof(IMyService))]
            public class MyService : IMyService { }

            public class Startup
            {
                public void ConfigureServices(IServiceCollection services)
                {
                    services.Scan(scan => scan
                        .FromAssemblyOf<Startup>()
                        .AddClasses()
                        .UsingAttributes());
                }
            }
            """;

        var driver = GeneratorTestHelper.CreateDriver(source);
        return Verify(driver);
    }

    [Fact]
    public Task UsingAttributes_WithSingletonLifetime_Verify()
    {
        const string source = """
            using Scrutor;
            using Microsoft.Extensions.DependencyInjection;

            public interface IMyService { }

            [ServiceDescriptor(typeof(IMyService), ServiceLifetime.Singleton)]
            public class MyService : IMyService { }

            public class Startup
            {
                public void ConfigureServices(IServiceCollection services)
                {
                    services.Scan(scan => scan
                        .FromAssemblyOf<Startup>()
                        .AddClasses()
                        .UsingAttributes());
                }
            }
            """;

        var driver = GeneratorTestHelper.CreateDriver(source);
        return Verify(driver);
    }

    [Fact]
    public Task UsingAttributes_WithScopedLifetime_Verify()
    {
        const string source = """
            using Scrutor;
            using Microsoft.Extensions.DependencyInjection;

            public interface IMyService { }

            [ServiceDescriptor(typeof(IMyService), ServiceLifetime.Scoped)]
            public class MyService : IMyService { }

            public class Startup
            {
                public void ConfigureServices(IServiceCollection services)
                {
                    services.Scan(scan => scan
                        .FromAssemblyOf<Startup>()
                        .AddClasses()
                        .UsingAttributes());
                }
            }
            """;

        var driver = GeneratorTestHelper.CreateDriver(source);
        return Verify(driver);
    }

    [Fact]
    public Task UsingAttributes_MultipleAttributes_Verify()
    {
        const string source = """
            using Scrutor;
            using Microsoft.Extensions.DependencyInjection;

            public interface IService1 { }
            public interface IService2 { }

            [ServiceDescriptor(typeof(IService1), ServiceLifetime.Singleton)]
            [ServiceDescriptor(typeof(IService2), ServiceLifetime.Scoped)]
            public class MultiService : IService1, IService2 { }

            public class Startup
            {
                public void ConfigureServices(IServiceCollection services)
                {
                    services.Scan(scan => scan
                        .FromAssemblyOf<Startup>()
                        .AddClasses()
                        .UsingAttributes());
                }
            }
            """;

        var driver = GeneratorTestHelper.CreateDriver(source);
        return Verify(driver);
    }

    [Fact]
    public Task UsingAttributes_NoAttributes_Verify()
    {
        const string source = """
            using Scrutor;
            using Microsoft.Extensions.DependencyInjection;

            public interface IMyService { }
            public class MyService : IMyService { }

            public class Startup
            {
                public void ConfigureServices(IServiceCollection services)
                {
                    services.Scan(scan => scan
                        .FromAssemblyOf<Startup>()
                        .AddClasses()
                        .UsingAttributes());
                }
            }
            """;

        var driver = GeneratorTestHelper.CreateDriver(source);
        return Verify(driver);
    }

    [Fact]
    public void GenerationDisabled_ProducesNoOutput()
    {
        const string source = """
            using Scrutor;
            using Microsoft.Extensions.DependencyInjection;

            public interface IMyService { }

            [ServiceDescriptor(typeof(IMyService))]
            public class MyService : IMyService { }

            public class Startup
            {
                public void ConfigureServices(IServiceCollection services)
                {
                    services.Scan(scan => scan
                        .FromAssemblyOf<Startup>()
                        .AddClasses()
                        .UsingAttributes());
                }
            }
            """;

        var (_, sources) = GeneratorTestHelper.RunGeneratorFull(source, enableGeneration: false);

        // Should produce no generated source
        Assert.Empty(sources);
    }

    [Fact]
    public Task UsingAttributes_AbstractClassIgnored_Verify()
    {
        const string source = """
            using Scrutor;
            using Microsoft.Extensions.DependencyInjection;

            public interface IMyService { }

            [ServiceDescriptor(typeof(IMyService))]
            public abstract class MyAbstractService : IMyService { }

            [ServiceDescriptor(typeof(IMyService))]
            public class MyConcreteService : IMyService { }

            public class Startup
            {
                public void ConfigureServices(IServiceCollection services)
                {
                    services.Scan(scan => scan
                        .FromAssemblyOf<Startup>()
                        .AddClasses()
                        .UsingAttributes());
                }
            }
            """;

        var driver = GeneratorTestHelper.CreateDriver(source);
        return Verify(driver);
    }

    [Fact]
    public Task UsingAttributes_DefaultConstructor_RegistersAsSelf_Verify()
    {
        const string source = """
            using Scrutor;
            using Microsoft.Extensions.DependencyInjection;

            [ServiceDescriptor]
            public class MyService { }

            public class Startup
            {
                public void ConfigureServices(IServiceCollection services)
                {
                    services.Scan(scan => scan
                        .FromAssemblyOf<Startup>()
                        .AddClasses()
                        .UsingAttributes());
                }
            }
            """;

        var driver = GeneratorTestHelper.CreateDriver(source);
        return Verify(driver);
    }

    [Fact]
    public Task UsingAttributes_WithNamespace_Verify()
    {
        const string source = """
            using Scrutor;
            using Microsoft.Extensions.DependencyInjection;

            namespace MyApp.Services
            {
                public interface IMyService { }

                [ServiceDescriptor(typeof(IMyService))]
                public class MyService : IMyService { }
            }

            public class Startup
            {
                public void ConfigureServices(IServiceCollection services)
                {
                    services.Scan(scan => scan
                        .FromAssemblyOf<Startup>()
                        .AddClasses()
                        .UsingAttributes());
                }
            }
            """;

        var driver = GeneratorTestHelper.CreateDriver(source);
        return Verify(driver);
    }
}
