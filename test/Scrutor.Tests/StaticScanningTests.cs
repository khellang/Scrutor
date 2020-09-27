using System;
using Microsoft.Extensions.DependencyInjection;
using Scrutor.Tests;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Scrutor.Analyzers;
using Xunit;
using Xunit.Abstractions;
using static Scrutor.Tests.GenerationHelpers;

namespace Scrutor.Tests
{
    public class StaticScanningTests
    {
        private readonly ITestOutputHelper _testOutputHelper;

        public StaticScanningTests(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
        }

        [Fact]
        public void Should_Handle_Private_Classes_Within_Self()
        {
            using var context = new CollectibleTestAssemblyLoadContext();
            using var dependencyGenerator = new GeneratorTester(context, "DependencyProject");
            var dependency = dependencyGenerator
                .AddSources(@"
namespace DependencyProject
{
    public interface IService { }
    class Service : IService { }
}
").Compile();

            dependencyGenerator.AssertCompilationWasSuccessful();
            dependencyGenerator.Emit();

            var source = @"
using Scrutor;
using Scrutor.Static;
using Microsoft.Extensions.DependencyInjection;
using DependencyProject;

namespace TestProject
{
    public static class Program
    {
        static void Main() { }
        static IServiceCollection LoadServices()
        {
            var services = new ServiceCollection();
	        services.ScanStatic(
            z => z
			    .FromAssemblies()
			    .AddClasses(x => x.AssignableTo<IService>())
                .AsSelf()
                .AsImplementedInterfaces()
                .WithScopedLifetime()
            );
            return services;
        }
    }
}
";

            var expected = @"
using System;
using System.Reflection;
using System.Runtime.Loader;
using Microsoft.Extensions.DependencyInjection;
using Scrutor;

namespace Scrutor.Static
{
    internal static class PopulateExtensions
    {
        public static IServiceCollection Populate(IServiceCollection services, RegistrationStrategy strategy, AssemblyLoadContext context, string filePath, string memberName, int lineNumber)
        {
            switch (lineNumber)
            {
                case 15:
                    strategy.Apply(services, ServiceDescriptor.Describe(Type.GetType(""DependencyProject.Service"", true), Type.GetType(""DependencyProject.Service"", true), ServiceLifetime.Scoped));
                    strategy.Apply(services, ServiceDescriptor.Describe(typeof(DependencyProject.IService), _ => _.GetRequiredService(Type.GetType(""DependencyProject.Service"", true)) as DependencyProject.IService, ServiceLifetime.Scoped));
                    break;
            }

            return services;
        }

        private static AssemblyName _DependencyProjectVersion0000CultureneutralPublicKeyTokennull;
        private static AssemblyName DependencyProjectVersion0000CultureneutralPublicKeyTokennull => _DependencyProjectVersion0000CultureneutralPublicKeyTokennull ??= new AssemblyName(""DependencyProject, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"");
    }
}
";
            using var generator = new GeneratorTester(context);
            var result = generator
                .AddReferences(typeof(Scrutor.IFluentInterface).Assembly, typeof(ServiceCollection).Assembly, typeof(IServiceCollection).Assembly)
                .AddCompilationReference(dependency)
                .Generate<StaticScrutorGenerator>(source, "Scrutor.Static.Populate.cs");

            foreach (var tree in result)
            {
                _testOutputHelper.WriteLine(tree.GetText().ToString());
            }

            generator.AssertCompilationWasSuccessful();

            var services = StaticHelper.ExecuteStaticServiceCollectionMethod(generator.Emit(), "Program", "LoadServices");
            Assert.Equal(2, services.Count());
            Assert.Equal(1, services.Count(z => z.ImplementationFactory is not null));
            Assert.Equal(1, services.Count(z => z.ImplementationType is not null));
            Assert.Equal(2, services.Count(z => z.Lifetime == ServiceLifetime.Scoped));
        }


        [Fact]
        public void Should_Handle_Public_Accessibility()
        {
            var source = @"
using Scrutor;
using Scrutor.Static;
using Microsoft.Extensions.DependencyInjection;

public interface IService
{

}

public class Service : IService
{

}

public static class Program {
    static void Main() { }
    static IServiceCollection LoadServices()
    {
        var services = new ServiceCollection();
	    services.ScanStatic(
        z => z
			.FromAssemblies()
			.AddClasses(x => x.AssignableTo<IService>())
            .AsSelf()
            .AsImplementedInterfaces()
            .WithScopedLifetime()
        );
        return services;
    }
}
";

            var expected = @"
using System;
using System.Reflection;
using System.Runtime.Loader;
using Microsoft.Extensions.DependencyInjection;
using Scrutor;

namespace Scrutor.Static
{
    internal static class PopulateExtensions
    {
        public static IServiceCollection Populate(IServiceCollection services, RegistrationStrategy strategy, AssemblyLoadContext context, string filePath, string memberName, int lineNumber)
        {
            switch (lineNumber)
            {
                case 21:
                    strategy.Apply(services, ServiceDescriptor.Describe(typeof(Service), typeof(Service), ServiceLifetime.Scoped));
                    strategy.Apply(services, ServiceDescriptor.Describe(typeof(IService), _ => _.GetRequiredService<Service>(), ServiceLifetime.Scoped));
                    break;
            }

            return services;
        }
    }
}
";
            using var context = new CollectibleTestAssemblyLoadContext();
            using var generator = new GeneratorTester(context);
            generator.AddReferences(typeof(Scrutor.IFluentInterface).Assembly, typeof(ServiceCollection).Assembly, typeof(IServiceCollection).Assembly)
                .AssertGeneratedAsExpected<StaticScrutorGenerator>(
                    source,
                    expected,
                    "Scrutor.Static.Populate.cs"
                );

            generator.AssertCompilationWasSuccessful();

            var services = StaticHelper.ExecuteStaticServiceCollectionMethod(generator.Emit(), "Program", "LoadServices");
            Assert.Equal(2, services.Count());
            Assert.Equal(1, services.Count(z => z.ImplementationFactory is not null));
            Assert.Equal(1, services.Count(z => z.ImplementationType is not null));
            Assert.Equal(2, services.Count(z => z.Lifetime == ServiceLifetime.Scoped));
        }

        [Theory]
        [InlineData(ServiceLifetime.Scoped)]
        [InlineData(ServiceLifetime.Singleton)]
        [InlineData(ServiceLifetime.Transient)]
        public void Should_Have_Correct_Lifetime(ServiceLifetime serviceLifetime)
        {
            var source = $@"
using Scrutor;
using Scrutor.Static;
using Microsoft.Extensions.DependencyInjection;

public interface IService
{{

}}

public class Service : IService
{{

}}

public interface IServiceB
{{

}}

public class ServiceB : IServiceB
{{

}}

public static class Program {{
    static void Main() {{ }}
    static IServiceCollection LoadServices()
    {{
        var services = new ServiceCollection();
	    services.ScanStatic(
        z => z
			.FromAssemblies()
			.AddClasses(x => x.AssignableTo<IService>())
            .AsSelf()
            .AsImplementedInterfaces()
            .With{serviceLifetime}Lifetime()
        );
	    services.ScanStatic(
        z => z
			.FromAssemblies()
			.AddClasses(x => x.AssignableTo<IServiceB>(), false)
            .AsSelf()
            .AsMatchingInterface()
            .WithLifetime(ServiceLifetime.{serviceLifetime})
        );
        return services;
    }}
}}
";

            var expected = $@"
using System;
using System.Reflection;
using System.Runtime.Loader;
using Microsoft.Extensions.DependencyInjection;
using Scrutor;

namespace Scrutor.Static
{{
    internal static class PopulateExtensions
    {{
        public static IServiceCollection Populate(IServiceCollection services, RegistrationStrategy strategy, AssemblyLoadContext context, string filePath, string memberName, int lineNumber)
        {{
            switch (lineNumber)
            {{
                case 31:
                    strategy.Apply(services, ServiceDescriptor.Describe(typeof(Service), typeof(Service), ServiceLifetime.{serviceLifetime}));
                    strategy.Apply(services, ServiceDescriptor.Describe(typeof(IService), _ => _.GetRequiredService<Service>(), ServiceLifetime.{serviceLifetime}));
                    break;
                case 39:
                    strategy.Apply(services, ServiceDescriptor.Describe(typeof(ServiceB), typeof(ServiceB), ServiceLifetime.{serviceLifetime}));
                    strategy.Apply(services, ServiceDescriptor.Describe(typeof(IServiceB), _ => _.GetRequiredService<ServiceB>(), ServiceLifetime.{serviceLifetime}));
                    break;
            }}

            return services;
        }}
    }}
}}
";
            using var context = new CollectibleTestAssemblyLoadContext();
            using var generator = new GeneratorTester(context);
            generator.AddReferences(typeof(Scrutor.IFluentInterface).Assembly, typeof(ServiceCollection).Assembly, typeof(IServiceCollection).Assembly)
                .AssertGeneratedAsExpected<StaticScrutorGenerator>(
                    source,
                    expected,
                    "Scrutor.Static.Populate.cs"
                );

            generator.AssertCompilationWasSuccessful();

            var services = StaticHelper.ExecuteStaticServiceCollectionMethod(generator.Emit(), "Program", "LoadServices");
            Assert.Equal(4, services.Count());
            Assert.Equal(2, services.Count(z => z.ImplementationFactory is not null));
            Assert.Equal(2, services.Count(z => z.ImplementationType is not null));
        }

        [Fact]
        public void Should_Split_Correctly_Given_Same_Line_Number_Run()
        {
            var source = @"
using Scrutor;
using Scrutor.Static;
using Microsoft.Extensions.DependencyInjection;

public interface IService
{

}

public class Service : IService
{

}

public interface IServiceB
{

}

public class ServiceB : IServiceB
{

}
";

            var source1 = @"
using Scrutor;
using Scrutor.Static;
using Microsoft.Extensions.DependencyInjection;

public static class Program {
    static ServiceCollection Services = new ServiceCollection();
    static void Main() {}
    static IServiceCollection Method()
    {
	    Services.ScanStatic(z => z
			.FromAssemblies()
			.AddClasses(x => x.AssignableTo<IService>(), false)
            .AsSelf()
            .AsImplementedInterfaces()
            .WithSingletonLifetime()
        );

        return Services;
    }
}
";

            var source2 = @"
using Scrutor;
using Scrutor.Static;
using Microsoft.Extensions.DependencyInjection;

public static class Program2 {
    static ServiceCollection Services = new ServiceCollection();

    static IServiceCollection Method()
    {
	    Services.ScanStatic(z => z
			.FromAssemblies()
			.AddClasses(x => x.AssignableTo<IServiceB>(), false)
            .AsSelf()
            .AsImplementedInterfaces()
            .WithScopedLifetime()
        );

        return Services;
    }
}
";
            var expected = @"
using System;
using System.Reflection;
using System.Runtime.Loader;
using Microsoft.Extensions.DependencyInjection;
using Scrutor;

namespace Scrutor.Static
{
    internal static class PopulateExtensions
    {
        public static IServiceCollection Populate(IServiceCollection services, RegistrationStrategy strategy, AssemblyLoadContext context, string filePath, string memberName, int lineNumber)
        {
            switch (lineNumber)
            {
                case 11:
                    switch (filePath)
                    {
                        case ""Test1.cs"":
                            strategy.Apply(services, ServiceDescriptor.Describe(typeof(Service), typeof(Service), ServiceLifetime.Singleton));
                            strategy.Apply(services, ServiceDescriptor.Describe(typeof(IService), _ => _.GetRequiredService<Service>(), ServiceLifetime.Singleton));
                            break;
                        case ""Test2.cs"":
                            strategy.Apply(services, ServiceDescriptor.Describe(typeof(ServiceB), typeof(ServiceB), ServiceLifetime.Scoped));
                            strategy.Apply(services, ServiceDescriptor.Describe(typeof(IServiceB), _ => _.GetRequiredService<ServiceB>(), ServiceLifetime.Scoped));
                            break;
                    }

                    break;
            }

            return services;
        }
    }
}
";

            using var context = new CollectibleTestAssemblyLoadContext();
            using var generator = new GeneratorTester(context);
            generator
                .AddReferences(typeof(Scrutor.IFluentInterface).Assembly, typeof(ServiceCollection).Assembly, typeof(IServiceCollection).Assembly)
                .AssertGeneratedAsExpected<StaticScrutorGenerator>(
                    new[] {source, source1, source2},
                    new[] {expected},
                    "Scrutor.Static.Populate.cs"
                );

            generator.AssertCompilationWasSuccessful();

            var assembly = generator.Emit();
            var services1 = StaticHelper.ExecuteStaticServiceCollectionMethod(assembly, "Program", "Method");
            var services2 = StaticHelper.ExecuteStaticServiceCollectionMethod(assembly, "Program2", "Method");

            Assert.Equal(2, services1.Count());
            Assert.Equal(1, services1.Count(z => z.ImplementationFactory is not null));
            Assert.Equal(1, services1.Count(z => z.ImplementationType is not null));
            Assert.Equal(2, services1.Count(z => z.Lifetime == ServiceLifetime.Singleton));

            Assert.Equal(2, services2.Count());
            Assert.Equal(1, services2.Count(z => z.ImplementationFactory is not null));
            Assert.Equal(1, services2.Count(z => z.ImplementationType is not null));
            Assert.Equal(2, services2.Count(z => z.Lifetime == ServiceLifetime.Scoped));
        }

        static class StaticHelper
        {
            public static IServiceCollection ExecuteStaticServiceCollectionMethod(Assembly assembly, string className, string methodName)
            {
                var @class = assembly.GetTypes().FirstOrDefault(z => z.IsClass && z.Name == className)!;
                Assert.NotNull(@class);

                var method = @class.GetMethod(methodName, BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
                Assert.NotNull(method);

                return (method!.Invoke(null, Array.Empty<object>()) as IServiceCollection)!;
            }
        }
    }
}
