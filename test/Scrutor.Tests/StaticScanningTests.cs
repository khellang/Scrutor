using System;
using System.Collections.Generic;
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
using Scrutor.Analyzers.Internals;
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
        public void Should_Handle_Public_Types()
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
			.AddClasses(x => x.AssignableTo(typeof(IService)))
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
            using var generator = new GeneratorTester(context)
                .Output(_testOutputHelper);
            generator.AddReferences(typeof(Scrutor.IFluentInterface).Assembly, typeof(ServiceCollection).Assembly, typeof(IServiceCollection).Assembly)
                .AssertGeneratedAsExpected<StaticScrutorGenerator>(
                    source,
                    expected,
                    "Scrutor.Static.Populate.cs"
                );

            generator.AssertCompilationWasSuccessful();
            generator.AssertGenerationWasSuccessful();

            var services = StaticHelper.ExecuteStaticServiceCollectionMethod(generator.Emit(), "Program", "LoadServices");
            Assert.Equal(2, services.Count());
            Assert.Equal(1, services.Count(z => z.ImplementationFactory is not null));
            Assert.Equal(1, services.Count(z => z.ImplementationType is not null));
            Assert.Equal(2, services.Count(z => z.Lifetime == ServiceLifetime.Scoped));
        }

        [Fact]
        public void Should_Handle_Private_Types()
        {
            using var context = new CollectibleTestAssemblyLoadContext();

            var dependencies = new List<CSharpCompilation>();
            using var rootGenerator = new GeneratorTester(context, "RootDependencyProject")
                .Output(_testOutputHelper);
            var root = rootGenerator
                .AddSources(@"
namespace RootDependencyProject
{
    public interface IService { }
    class Service : IService { }
}
").Compile();
            rootGenerator.AssertCompilationWasSuccessful();
            rootGenerator.AssertGenerationWasSuccessful();
            rootGenerator.Emit();
            dependencies.Add(root);

            var source = @"
using Scrutor;
using Scrutor.Static;
using Microsoft.Extensions.DependencyInjection;
using RootDependencyProject;

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
                .WithSingletonLifetime()
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
                    strategy.Apply(services, ServiceDescriptor.Describe(context.LoadFromAssemblyName(RootDependencyProjectVersion0000CultureneutralPublicKeyTokennull).GetType(""RootDependencyProject.Service""), context.LoadFromAssemblyName(RootDependencyProjectVersion0000CultureneutralPublicKeyTokennull).GetType(""RootDependencyProject.Service""), ServiceLifetime.Singleton));
                    strategy.Apply(services, ServiceDescriptor.Describe(typeof(RootDependencyProject.IService), _ => _.GetRequiredService(context.LoadFromAssemblyName(RootDependencyProjectVersion0000CultureneutralPublicKeyTokennull).GetType(""RootDependencyProject.Service"")) as RootDependencyProject.IService, ServiceLifetime.Singleton));
                    break;
            }

            return services;
        }

        private static AssemblyName _RootDependencyProjectVersion0000CultureneutralPublicKeyTokennull;
        private static AssemblyName RootDependencyProjectVersion0000CultureneutralPublicKeyTokennull => _RootDependencyProjectVersion0000CultureneutralPublicKeyTokennull ??= new AssemblyName(""RootDependencyProject, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"");
    }
}
";

            using var generator = new GeneratorTester(context)
                .Output(_testOutputHelper);
            generator
                .AddReferences(typeof(Scrutor.IFluentInterface).Assembly, typeof(ServiceCollection).Assembly, typeof(IServiceCollection).Assembly)
                .AddCompilationReference(dependencies).AssertGeneratedAsExpected<StaticScrutorGenerator>(
                    source,
                    expected,
                    "Scrutor.Static.Populate.cs"
                );

            generator.AssertCompilationWasSuccessful();
            generator.AssertGenerationWasSuccessful();

            var services = StaticHelper.ExecuteStaticServiceCollectionMethod(generator.Emit(), "Program", "LoadServices");
            Assert.Equal(2, services.Count());
            Assert.Equal(1, services.Count(z => z.ImplementationFactory is not null));
            Assert.Equal(1, services.Count(z => z.ImplementationType is not null));
            Assert.Equal(2, services.Count(z => z.Lifetime == ServiceLifetime.Singleton));
        }

        [Fact]
        public void Should_Handle_Public_Open_Generic_Types()
        {
            var source = @"
using Scrutor;
using Scrutor.Static;
using Microsoft.Extensions.DependencyInjection;

public interface IService<T>
{

}

public class Service<T> : IService<T>
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
			.AddClasses(x => x.AssignableTo(typeof(IService<>)))
            .AsSelfWithInterfaces()
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
                    strategy.Apply(services, ServiceDescriptor.Describe(typeof(Service<>), typeof(Service<>), ServiceLifetime.Scoped));
                    strategy.Apply(services, ServiceDescriptor.Describe(typeof(IService<>), typeof(Service<>), ServiceLifetime.Scoped));
                    break;
            }

            return services;
        }
    }
}
";
            using var context = new CollectibleTestAssemblyLoadContext();
            using var generator = new GeneratorTester(context)
                .Output(_testOutputHelper);
            generator
                .AddReferences(typeof(Scrutor.IFluentInterface).Assembly, typeof(ServiceCollection).Assembly, typeof(IServiceCollection).Assembly)
                .AssertGeneratedAsExpected<StaticScrutorGenerator>(
                    source,
                    expected,
                    "Scrutor.Static.Populate.cs"
                );

            generator.AssertCompilationWasSuccessful();
            generator.AssertGenerationWasSuccessful();

            var services = StaticHelper.ExecuteStaticServiceCollectionMethod(generator.Emit(), "Program", "LoadServices");
            Assert.Equal(2, services.Count());
            Assert.Equal(0, services.Count(z => z.ImplementationFactory is not null));
            Assert.Equal(2, services.Count(z => z.ImplementationType is not null));
            Assert.Equal(2, services.Count(z => z.Lifetime == ServiceLifetime.Scoped));
        }

        [Fact]
        public void Should_Handle_Private_Open_Generic_Types()
        {
            using var context = new CollectibleTestAssemblyLoadContext();

            var dependencies = new List<CSharpCompilation>();
            using var rootGenerator = new GeneratorTester(context, "RootDependencyProject");
            var root = rootGenerator
                .AddSources(@"
namespace RootDependencyProject
{
    public interface IService<T> { }
    class Service<T> : IService<T> { }
}
").Compile();
            rootGenerator.AssertCompilationWasSuccessful();
            rootGenerator.AssertGenerationWasSuccessful();
            rootGenerator.Emit();
            dependencies.Add(root);


            var source = @"
using Scrutor;
using Scrutor.Static;
using Microsoft.Extensions.DependencyInjection;
using RootDependencyProject;

public static class Program {
    static void Main() { }
    static IServiceCollection LoadServices()
    {
        var services = new ServiceCollection();
	    services.ScanStatic(
        z => z
			.FromAssemblies()
			.AddClasses(x => x.AssignableTo(typeof(IService<>)))
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
                case 12:
                    strategy.Apply(services, ServiceDescriptor.Describe(context.LoadFromAssemblyName(RootDependencyProjectVersion0000CultureneutralPublicKeyTokennull).GetType(""RootDependencyProject.Service`1""), context.LoadFromAssemblyName(RootDependencyProjectVersion0000CultureneutralPublicKeyTokennull).GetType(""RootDependencyProject.Service`1""), ServiceLifetime.Scoped));
                    strategy.Apply(services, ServiceDescriptor.Describe(typeof(RootDependencyProject.IService<>), context.LoadFromAssemblyName(RootDependencyProjectVersion0000CultureneutralPublicKeyTokennull).GetType(""RootDependencyProject.Service`1""), ServiceLifetime.Scoped));
                    break;
            }

            return services;
        }

        private static AssemblyName _RootDependencyProjectVersion0000CultureneutralPublicKeyTokennull;
        private static AssemblyName RootDependencyProjectVersion0000CultureneutralPublicKeyTokennull => _RootDependencyProjectVersion0000CultureneutralPublicKeyTokennull ??= new AssemblyName(""RootDependencyProject, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"");
    }
}
";
            using var generator = new GeneratorTester(context)
                .Output(_testOutputHelper);

            generator
                .AddReferences(typeof(Scrutor.IFluentInterface).Assembly, typeof(ServiceCollection).Assembly, typeof(IServiceCollection).Assembly)
                .AddCompilationReference(dependencies)
                .AssertGeneratedAsExpected<StaticScrutorGenerator>(
                    source,
                    expected,
                    "Scrutor.Static.Populate.cs"
                );

            generator.AssertCompilationWasSuccessful();
            generator.AssertGenerationWasSuccessful();

            var services = StaticHelper.ExecuteStaticServiceCollectionMethod(generator.Emit(), "Program", "LoadServices");
            Assert.Equal(2, services.Count());
            Assert.Equal(0, services.Count(z => z.ImplementationFactory is not null));
            Assert.Equal(2, services.Count(z => z.ImplementationType is not null));
            Assert.Equal(2, services.Count(z => z.Lifetime == ServiceLifetime.Scoped));
        }

        [Fact]
        public void Should_Handle_Public_Closed_Generic_Types()
        {
            using var context = new CollectibleTestAssemblyLoadContext();

            var dependencies = new List<CSharpCompilation>();
            using var rootGenerator = new GeneratorTester(context, "RootDependencyProject")
                .Output(_testOutputHelper);
            var root = rootGenerator
                .AddSources(@"
namespace RootDependencyProject
{
    public interface IRequest<T> { }
    public interface IRequestHandler<T, R> where T : IRequest<R> { }
    public class Request : IRequest<Response> { }

    public class Response { }
    public class RequestHandler : IRequestHandler<Request, Response> { }
}
").Compile();
            rootGenerator.AssertCompilationWasSuccessful();
            rootGenerator.AssertGenerationWasSuccessful();
            rootGenerator.Emit();
            dependencies.Add(root);

            var source = @"
using Scrutor;
using Scrutor.Static;
using Microsoft.Extensions.DependencyInjection;
using RootDependencyProject;

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
			    .AddClasses(x => x.AssignableTo(typeof(IRequestHandler<,>)))
                .AsImplementedInterfaces()
                .WithSingletonLifetime()
            );
            return services;
        }
    }
}
";

            var expected = @"using System;
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
                    strategy.Apply(services, ServiceDescriptor.Describe(typeof(RootDependencyProject.IRequestHandler<RootDependencyProject.Request, RootDependencyProject.Response>), typeof(RootDependencyProject.RequestHandler), ServiceLifetime.Singleton));
                    break;
            }

            return services;
        }
    }
}";

            using var generator = new GeneratorTester(context)
                .Output(_testOutputHelper);
            generator
                .AddReferences(typeof(Scrutor.IFluentInterface).Assembly, typeof(ServiceCollection).Assembly, typeof(IServiceCollection).Assembly)
                .AddCompilationReference(dependencies)
                .AssertGeneratedAsExpected<StaticScrutorGenerator>(
                    source,
                    expected,
                    "Scrutor.Static.Populate.cs"
                );

            generator.AssertCompilationWasSuccessful();
            generator.AssertGenerationWasSuccessful();

            var services = StaticHelper.ExecuteStaticServiceCollectionMethod(generator.Emit(), "Program", "LoadServices");
            Assert.Single(services);
            Assert.Equal(1, services.Count(z => z.ImplementationType is not null));
            Assert.Equal(1, services.Count(z => z.Lifetime == ServiceLifetime.Singleton));
        }

        [Fact]
        public void Should_Handle_Private_Closed_Generic_Types()
        {
            using var context = new CollectibleTestAssemblyLoadContext();

            var dependencies = new List<CSharpCompilation>();
            using var rootGenerator = new GeneratorTester(context, "RootDependencyProject")
                .Output(_testOutputHelper);
            var root = rootGenerator
                .AddSources(@"
namespace RootDependencyProject
{
    public interface IRequest<T> { }
    public interface IRequestHandler<T, R> where T : IRequest<R> { }
    class Request : IRequest<Response> { }

    class Response { }
    class RequestHandler : IRequestHandler<Request, Response> { }
}
").Compile();
            rootGenerator.AssertCompilationWasSuccessful();
            rootGenerator.AssertGenerationWasSuccessful();
            rootGenerator.Emit();
            dependencies.Add(root);

            var source = @"
using Scrutor;
using Scrutor.Static;
using Microsoft.Extensions.DependencyInjection;
using RootDependencyProject;

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
			    .AddClasses(x => x.AssignableTo(typeof(IRequestHandler<,>)))
                .AsImplementedInterfaces()
                .WithSingletonLifetime()
            );
            return services;
        }
    }
}
";

            var expected = @"using System;
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
                    strategy.Apply(services, ServiceDescriptor.Describe(typeof(RootDependencyProject.IRequestHandler<, >).MakeGenericType(context.LoadFromAssemblyName(RootDependencyProjectVersion0000CultureneutralPublicKeyTokennull).GetType(""RootDependencyProject.Request""), context.LoadFromAssemblyName(RootDependencyProjectVersion0000CultureneutralPublicKeyTokennull).GetType(""RootDependencyProject.Response"")), context.LoadFromAssemblyName(RootDependencyProjectVersion0000CultureneutralPublicKeyTokennull).GetType(""RootDependencyProject.RequestHandler""), ServiceLifetime.Singleton));
                    break;
            }

            return services;
        }

        private static AssemblyName _RootDependencyProjectVersion0000CultureneutralPublicKeyTokennull;
        private static AssemblyName RootDependencyProjectVersion0000CultureneutralPublicKeyTokennull => _RootDependencyProjectVersion0000CultureneutralPublicKeyTokennull ??= new AssemblyName(""RootDependencyProject, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"");
    }
}";

            using var generator = new GeneratorTester(context)
                .Output(_testOutputHelper);
            generator
                .AddReferences(typeof(Scrutor.IFluentInterface).Assembly, typeof(ServiceCollection).Assembly, typeof(IServiceCollection).Assembly)
                .AddCompilationReference(dependencies)
                .AssertGeneratedAsExpected<StaticScrutorGenerator>(
                    source,
                    expected,
                    "Scrutor.Static.Populate.cs"
                );

            generator.AssertCompilationWasSuccessful();
            generator.AssertGenerationWasSuccessful();

            var services = StaticHelper.ExecuteStaticServiceCollectionMethod(generator.Emit(), "Program", "LoadServices");
            Assert.Single(services);
            Assert.Equal(1, services.Count(z => z.ImplementationType is not null));
            Assert.Equal(1, services.Count(z => z.Lifetime == ServiceLifetime.Singleton));
        }

        [Fact]
        public void Should_Ignore_Abstract_Classes()
        {
            var source = @"
using Scrutor;
using Scrutor.Static;
using Microsoft.Extensions.DependencyInjection;

public interface IService { }
public class Service : IService { }
public abstract class ServiceB : IService { }

public static class Program {
    static void Main() { }
    static IServiceCollection LoadServices()
    {
        var services = new ServiceCollection();
	    services.ScanStatic(
        z => z
			.FromAssemblies()
			.AddClasses(x => x.AssignableTo(typeof(IService)))
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
                case 15:
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
            using var generator = new GeneratorTester(context)
                .Output(_testOutputHelper);
            generator.AddReferences(typeof(Scrutor.IFluentInterface).Assembly, typeof(ServiceCollection).Assembly, typeof(IServiceCollection).Assembly)
                .AssertGeneratedAsExpected<StaticScrutorGenerator>(
                    source,
                    expected,
                    "Scrutor.Static.Populate.cs"
                );

            generator.AssertCompilationWasSuccessful();
            generator.AssertGenerationWasSuccessful();

            var services = StaticHelper.ExecuteStaticServiceCollectionMethod(generator.Emit(), "Program", "LoadServices");
            Assert.Equal(2, services.Count());
            Assert.Equal(1, services.Count(z => z.ImplementationFactory is not null));
            Assert.Equal(1, services.Count(z => z.ImplementationType is not null));
            Assert.Equal(2, services.Count(z => z.Lifetime == ServiceLifetime.Scoped));
        }

        [Fact]
        public void Should_Using_Support_As_Type()
        {
            var source = @"
using Scrutor;
using Scrutor.Static;
using Microsoft.Extensions.DependencyInjection;

public interface IService { }
public class Service : IService { }
public abstract class ServiceB : IService { }

public static class Program {
    static void Main() { }
    static IServiceCollection LoadServices()
    {
        var services = new ServiceCollection();
	    services.ScanStatic(
        z => z
			.FromAssemblies()
			.AddClasses(x => x.AssignableTo(typeof(IService)))
            .As<IService>()
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
                case 15:
                    strategy.Apply(services, ServiceDescriptor.Describe(typeof(IService), typeof(Service), ServiceLifetime.Scoped));
                    break;
            }

            return services;
        }
    }
}
";
            using var context = new CollectibleTestAssemblyLoadContext();
            using var generator = new GeneratorTester(context)
                .Output(_testOutputHelper);
            generator.AddReferences(typeof(Scrutor.IFluentInterface).Assembly, typeof(ServiceCollection).Assembly, typeof(IServiceCollection).Assembly)
                .AssertGeneratedAsExpected<StaticScrutorGenerator>(
                    source,
                    expected,
                    "Scrutor.Static.Populate.cs"
                );

            generator.AssertCompilationWasSuccessful();
            generator.AssertGenerationWasSuccessful();

            var services = StaticHelper.ExecuteStaticServiceCollectionMethod(generator.Emit(), "Program", "LoadServices");
            Assert.Single(services);
            Assert.Equal(1, services.Count(z => z.ImplementationType is not null));
            Assert.Equal(1, services.Count(z => z.Lifetime == ServiceLifetime.Scoped));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        [InlineData(4)]
        [InlineData(5)]
        public void Should_Handle_Private_Generic_Classes_Within_Multiple_Dependencies(int dependencyCount)
        {
            using var context = new CollectibleTestAssemblyLoadContext();

            var dependencies = new List<CSharpCompilation>();
            using var rootGenerator = new GeneratorTester(context, "RootDependencyProject")
                .Output(_testOutputHelper);
            var root = rootGenerator
                .AddSources(@"
namespace RootDependencyProject
{
    public interface IRequest<T> { }
    public interface IRequestHandler<T, R> where T : IRequest<R> { }
}
").Compile();
            rootGenerator.AssertCompilationWasSuccessful();
            rootGenerator.AssertGenerationWasSuccessful();
            rootGenerator.Emit();
            dependencies.Add(root);

            for (var i = 0; i < dependencyCount; i++)
            {
                using var dependencyGenerator = new GeneratorTester(context, $"Dependency{i}Project");
                var dependency = dependencyGenerator
                    .AddCompilationReference(root)
                    .AddSources($@"
using RootDependencyProject;

namespace Dependency{1}Project
{{    
    {(i % 2 == 0 ? "public" : "")} class Request{i} : IRequest<Response{i}> {{ }}
    {(i % 2 == 0 ? "public" : "")} class Response{i} {{ }}
    {(i % 2 == 0 ? "public" : "")} class RequestHandler{i} : IRequestHandler<Request{i}, Response{i}>  {{ }}
}}
").Compile();
                dependencies.Add(dependency);
                dependency.EmitInto(context);
            }


            var source = @"
using Scrutor;
using Scrutor.Static;
using Microsoft.Extensions.DependencyInjection;
using RootDependencyProject;

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
			    .AddClasses(x => x.AssignableTo(typeof(IRequestHandler<,>)))
                .AsImplementedInterfaces()
                .WithSingletonLifetime()
            );
            return services;
        }
    }
}
";

            using var generator = new GeneratorTester(context)
                .Output(_testOutputHelper);
            var result = generator
                .AddReferences(typeof(Scrutor.IFluentInterface).Assembly, typeof(ServiceCollection).Assembly, typeof(IServiceCollection).Assembly)
                .AddCompilationReference(dependencies)
                .AddSources(source)
                .Generate<StaticScrutorGenerator>();

            foreach (var tree in result)
            {
                _testOutputHelper.WriteLine(tree.GetText().ToString());
            }

            generator.AssertCompilationWasSuccessful();
            generator.AssertGenerationWasSuccessful();

            var services = StaticHelper.ExecuteStaticServiceCollectionMethod(generator.Emit(), "Program", "LoadServices");
            Assert.Equal(dependencyCount, services.Count());
            Assert.Equal(dependencyCount, services.Count(z => z.ImplementationType is not null));
            Assert.Equal(dependencyCount, services.Count(z => z.Lifetime == ServiceLifetime.Singleton));
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
            dependencyGenerator.AssertGenerationWasSuccessful();
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
            using var generator = new GeneratorTester(context)
                .Output(_testOutputHelper);
            generator
                .AddReferences(typeof(Scrutor.IFluentInterface).Assembly, typeof(ServiceCollection).Assembly, typeof(IServiceCollection).Assembly)
                .AddCompilationReference(dependency)
                .Generate<StaticScrutorGenerator>(source, "Scrutor.Static.Populate.cs");

            generator.AssertCompilationWasSuccessful();
            generator.AssertGenerationWasSuccessful();

            var services = StaticHelper.ExecuteStaticServiceCollectionMethod(generator.Emit(), "Program", "LoadServices");
            Assert.Equal(2, services.Count());
            Assert.Equal(1, services.Count(z => z.ImplementationFactory is not null));
            Assert.Equal(1, services.Count(z => z.ImplementationType is not null));
            Assert.Equal(2, services.Count(z => z.Lifetime == ServiceLifetime.Scoped));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        [InlineData(4)]
        [InlineData(5)]
        public void Should_Handle_Private_Classes_Within_Multiple_Dependencies(int dependencyCount)
        {
            using var context = new CollectibleTestAssemblyLoadContext();

            var dependencies = new List<CSharpCompilation>();
            using var rootGenerator = new GeneratorTester(context, "RootDependencyProject");
            var root = rootGenerator
                .AddSources(@"
namespace RootDependencyProject
{
    public interface IService { }
}
").Compile();
            rootGenerator.AssertCompilationWasSuccessful();
            rootGenerator.AssertGenerationWasSuccessful();
            rootGenerator.Emit();
            dependencies.Add(root);

            for (var i = 0; i < dependencyCount; i++)
            {
                using var dependencyGenerator = new GeneratorTester(context, $"Dependency{i}Project");
                var dependency = dependencyGenerator
                    .AddCompilationReference(root)
                    .AddSources($@"
namespace Dependency{1}Project
{{
    class Service{i} : RootDependencyProject.IService {{ }}
}}
").Compile();
                dependencies.Add(dependency);
                dependency.EmitInto(context);
            }


            var source = @"
using Scrutor;
using Scrutor.Static;
using Microsoft.Extensions.DependencyInjection;
using RootDependencyProject;

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
                .AsImplementedInterfaces()
                .WithSingletonLifetime()
            );
            return services;
        }
    }
}
";

            using var generator = new GeneratorTester(context)
                .Output(_testOutputHelper);
            var result = generator
                .AddReferences(typeof(Scrutor.IFluentInterface).Assembly, typeof(ServiceCollection).Assembly, typeof(IServiceCollection).Assembly)
                .AddCompilationReference(dependencies)
                .AddSources(source)
                .Generate<StaticScrutorGenerator>();

            foreach (var tree in result)
            {
                _testOutputHelper.WriteLine(tree.GetText().ToString());
            }

            generator.AssertCompilationWasSuccessful();
            generator.AssertGenerationWasSuccessful();

            var services = StaticHelper.ExecuteStaticServiceCollectionMethod(generator.Emit(), "Program", "LoadServices");
            Assert.Equal(dependencyCount, services.Count());
            Assert.Equal(dependencyCount, services.Count(z => z.ImplementationType is not null));
            Assert.Equal(dependencyCount, services.Count(z => z.Lifetime == ServiceLifetime.Singleton));
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
            using var generator = new GeneratorTester(context)
                .Output(_testOutputHelper);
            generator.AddReferences(typeof(Scrutor.IFluentInterface).Assembly, typeof(ServiceCollection).Assembly, typeof(IServiceCollection).Assembly)
                .AssertGeneratedAsExpected<StaticScrutorGenerator>(
                    source,
                    expected,
                    "Scrutor.Static.Populate.cs"
                );

            generator.AssertCompilationWasSuccessful();
            generator.AssertGenerationWasSuccessful();

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
			.AddClasses(x => x.AssignableTo(typeof(IService)), false)
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
            using var generator = new GeneratorTester(context)
                .Output(_testOutputHelper);
            generator
                .AddReferences(typeof(Scrutor.IFluentInterface).Assembly, typeof(ServiceCollection).Assembly, typeof(IServiceCollection).Assembly)
                .AssertGeneratedAsExpected<StaticScrutorGenerator>(
                    new[] {source, source1, source2},
                    new[] {expected},
                    "Scrutor.Static.Populate.cs"
                );

            generator.AssertCompilationWasSuccessful();
            generator.AssertGenerationWasSuccessful();

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

        [Fact]
        public void Should_Filter_AssignableTo()
        {
            var source = @"
using Scrutor;
using Scrutor.Static;
using Microsoft.Extensions.DependencyInjection;

public interface IService { }
public interface IServiceB { }
public class Service : IService, IServiceB { }
public class ServiceA : IService { }

public static class Program {
    static void Main() { }
    static IServiceCollection LoadServices()
    {
        var services = new ServiceCollection();
	    services.ScanStatic(
        z => z
			.FromAssemblies()
			.AddClasses(x => x.AssignableTo(typeof(IService)).AssignableTo<IServiceB>())
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
                case 16:
                    strategy.Apply(services, ServiceDescriptor.Describe(typeof(Service), typeof(Service), ServiceLifetime.Scoped));
                    strategy.Apply(services, ServiceDescriptor.Describe(typeof(IService), _ => _.GetRequiredService<Service>(), ServiceLifetime.Scoped));
                    strategy.Apply(services, ServiceDescriptor.Describe(typeof(IServiceB), _ => _.GetRequiredService<Service>(), ServiceLifetime.Scoped));
                    break;
            }

            return services;
        }
    }
}
";
            using var context = new CollectibleTestAssemblyLoadContext();
            using var generator = new GeneratorTester(context)
                .Output(_testOutputHelper);
            generator.AddReferences(typeof(Scrutor.IFluentInterface).Assembly, typeof(ServiceCollection).Assembly, typeof(IServiceCollection).Assembly)
                .AssertGeneratedAsExpected<StaticScrutorGenerator>(
                    source,
                    expected,
                    "Scrutor.Static.Populate.cs"
                );

            generator.AssertCompilationWasSuccessful();
            generator.AssertGenerationWasSuccessful();

            var services = StaticHelper.ExecuteStaticServiceCollectionMethod(generator.Emit(), "Program", "LoadServices");
            Assert.Equal(3, services.Count());
            Assert.Equal(2, services.Count(z => z.ImplementationFactory is not null));
            Assert.Equal(1, services.Count(z => z.ImplementationType is not null));
            Assert.Equal(3, services.Count(z => z.Lifetime == ServiceLifetime.Scoped));
        }

        [Fact]
        public void Should_Filter_AssignableToAny()
        {
            var source = @"
using Scrutor;
using Scrutor.Static;
using Microsoft.Extensions.DependencyInjection;

public interface IService { }
public interface IServiceB { }
public class Service : IService, IServiceB { }
public class ServiceA : IService { }
public class ServiceB : IService { }

public static class Program {
    static void Main() { }
    static IServiceCollection LoadServices()
    {
        var services = new ServiceCollection();
	    services.ScanStatic(
        z => z
			.FromAssemblies()
			.AddClasses(x => x.AssignableToAny(typeof(IService), typeof(IServiceB)))
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
                case 17:
                    strategy.Apply(services, ServiceDescriptor.Describe(typeof(Service), typeof(Service), ServiceLifetime.Scoped));
                    strategy.Apply(services, ServiceDescriptor.Describe(typeof(IService), _ => _.GetRequiredService<Service>(), ServiceLifetime.Scoped));
                    strategy.Apply(services, ServiceDescriptor.Describe(typeof(IServiceB), _ => _.GetRequiredService<Service>(), ServiceLifetime.Scoped));
                    strategy.Apply(services, ServiceDescriptor.Describe(typeof(ServiceA), typeof(ServiceA), ServiceLifetime.Scoped));
                    strategy.Apply(services, ServiceDescriptor.Describe(typeof(IService), _ => _.GetRequiredService<ServiceA>(), ServiceLifetime.Scoped));
                    strategy.Apply(services, ServiceDescriptor.Describe(typeof(ServiceB), typeof(ServiceB), ServiceLifetime.Scoped));
                    strategy.Apply(services, ServiceDescriptor.Describe(typeof(IService), _ => _.GetRequiredService<ServiceB>(), ServiceLifetime.Scoped));
                    break;
            }

            return services;
        }
    }
}
";
            using var context = new CollectibleTestAssemblyLoadContext();
            using var generator = new GeneratorTester(context)
                .Output(_testOutputHelper);
            generator.AddReferences(typeof(Scrutor.IFluentInterface).Assembly, typeof(ServiceCollection).Assembly, typeof(IServiceCollection).Assembly)
                .AssertGeneratedAsExpected<StaticScrutorGenerator>(
                    source,
                    expected,
                    "Scrutor.Static.Populate.cs"
                );

            generator.AssertCompilationWasSuccessful();
            generator.AssertGenerationWasSuccessful();

            var services = StaticHelper.ExecuteStaticServiceCollectionMethod(generator.Emit(), "Program", "LoadServices");
            Assert.Equal(7, services.Count());
            Assert.Equal(4, services.Count(z => z.ImplementationFactory is not null));
            Assert.Equal(3, services.Count(z => z.ImplementationType is not null));
            Assert.Equal(7, services.Count(z => z.Lifetime == ServiceLifetime.Scoped));
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Should_Filter_WithAttribute(bool useTypeof)
        {
            var source = $@"
using System;
using Scrutor;
using Scrutor.Static;
using Microsoft.Extensions.DependencyInjection;

public class MyAttribute : Attribute {{ }}
public interface IService {{ }}
public interface IServiceB {{ }}
public class Service : IService, IServiceB {{ }}
[MyAttribute]
public class ServiceA : IService {{ }}
public class ServiceB : IService {{ }}

public static class Program {{
    static void Main() {{ }}
    static IServiceCollection LoadServices()
    {{
        var services = new ServiceCollection();
	    services.ScanStatic(
        z => z
			.FromAssemblies()
			.AddClasses(x => x.WithAttribute{(useTypeof ? "(typeof(MyAttribute))" : "<MyAttribute>()")})
            .AsSelf()
            .AsImplementedInterfaces()
            .WithScopedLifetime()
        );
        return services;
    }}
}}
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
                case 20:
                    strategy.Apply(services, ServiceDescriptor.Describe(typeof(ServiceA), typeof(ServiceA), ServiceLifetime.Scoped));
                    strategy.Apply(services, ServiceDescriptor.Describe(typeof(IService), _ => _.GetRequiredService<ServiceA>(), ServiceLifetime.Scoped));
                    break;
            }

            return services;
        }
    }
}
";
            using var context = new CollectibleTestAssemblyLoadContext();
            using var generator = new GeneratorTester(context)
                .Output(_testOutputHelper);
            generator.AddReferences(typeof(Scrutor.IFluentInterface).Assembly, typeof(ServiceCollection).Assembly, typeof(IServiceCollection).Assembly)
                .AssertGeneratedAsExpected<StaticScrutorGenerator>(
                    source,
                    expected,
                    "Scrutor.Static.Populate.cs"
                );

            generator.AssertCompilationWasSuccessful();
            generator.AssertGenerationWasSuccessful();

            var services = StaticHelper.ExecuteStaticServiceCollectionMethod(generator.Emit(), "Program", "LoadServices");
            Assert.Equal(2, services.Count());
            Assert.Equal(1, services.Count(z => z.ImplementationFactory is not null));
            Assert.Equal(1, services.Count(z => z.ImplementationType is not null));
            Assert.Equal(2, services.Count(z => z.Lifetime == ServiceLifetime.Scoped));
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Should_Filter_WithoutAttribute(bool useTypeof)
        {
            var source = $@"
using System;
using Scrutor;
using Scrutor.Static;
using Microsoft.Extensions.DependencyInjection;

public class MyAttribute : Attribute {{ }}
public interface IService {{ }}
public interface IServiceB {{ }}
public class Service : IService, IServiceB {{ }}
[MyAttribute]
public class ServiceA : IService {{ }}
public class ServiceB : IService {{ }}

public static class Program {{
    static void Main() {{ }}
    static IServiceCollection LoadServices()
    {{
        var services = new ServiceCollection();
	    services.ScanStatic(
        z => z
			.FromAssemblies()
			.AddClasses(x => x.AssignableTo(typeof(IService)).WithoutAttribute{(useTypeof ? "(typeof(MyAttribute))" : "<MyAttribute>()")})
            .AsSelf()
            .AsImplementedInterfaces()
            .WithScopedLifetime()
        );
        return services;
    }}
}}
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
                case 20:
                    strategy.Apply(services, ServiceDescriptor.Describe(typeof(Service), typeof(Service), ServiceLifetime.Scoped));
                    strategy.Apply(services, ServiceDescriptor.Describe(typeof(IService), _ => _.GetRequiredService<Service>(), ServiceLifetime.Scoped));
                    strategy.Apply(services, ServiceDescriptor.Describe(typeof(IServiceB), _ => _.GetRequiredService<Service>(), ServiceLifetime.Scoped));
                    strategy.Apply(services, ServiceDescriptor.Describe(typeof(ServiceB), typeof(ServiceB), ServiceLifetime.Scoped));
                    strategy.Apply(services, ServiceDescriptor.Describe(typeof(IService), _ => _.GetRequiredService<ServiceB>(), ServiceLifetime.Scoped));
                    break;
            }

            return services;
        }
    }
}
";
            using var context = new CollectibleTestAssemblyLoadContext();
            using var generator = new GeneratorTester(context)
                .Output(_testOutputHelper);
            generator.AddReferences(typeof(Scrutor.IFluentInterface).Assembly, typeof(ServiceCollection).Assembly, typeof(IServiceCollection).Assembly)
                .AssertGeneratedAsExpected<StaticScrutorGenerator>(
                    source,
                    expected,
                    "Scrutor.Static.Populate.cs"
                );

            generator.AssertCompilationWasSuccessful();
            generator.AssertGenerationWasSuccessful();

            var services = StaticHelper.ExecuteStaticServiceCollectionMethod(generator.Emit(), "Program", "LoadServices");
            Assert.Equal(5, services.Count());
            Assert.Equal(3, services.Count(z => z.ImplementationFactory is not null));
            Assert.Equal(2, services.Count(z => z.ImplementationType is not null));
            Assert.Equal(5, services.Count(z => z.Lifetime == ServiceLifetime.Scoped));
        }

        [Fact]
        public void Should_Support_ServiceDescriptorAttributes()
        {
            var source = $@"
using System;
using Scrutor;
using Scrutor.Static;
using Microsoft.Extensions.DependencyInjection;

public interface IService {{ }}
public interface IServiceB {{ }}
[ServiceDescriptor(typeof(IServiceB), ServiceLifetime.Scoped)]
public class Service : IService, IServiceB {{ }}
[ServiceDescriptor(ServiceLifetime.Transient)]
public class ServiceA : IService {{ }}
[ServiceDescriptor]
public class ServiceB : IService, IServiceB {{ }}

public static class Program {{
    static void Main() {{ }}
    static IServiceCollection LoadServices()
    {{
        var services = new ServiceCollection();
	    services.ScanStatic(
        z => z
			.FromAssemblies()
			.AddClasses(x => x.AssignableToAny(typeof(IService), typeof(IServiceB)))
            .UsingAttributes()
            .WithSingletonLifetime()
        );
        return services;
    }}
}}
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
                    strategy.Apply(services, ServiceDescriptor.Describe(typeof(IServiceB), typeof(Service), ServiceLifetime.Scoped));
                    strategy.Apply(services, ServiceDescriptor.Describe(typeof(ServiceA), typeof(ServiceA), ServiceLifetime.Transient));
                    strategy.Apply(services, ServiceDescriptor.Describe(typeof(IService), _ => _.GetRequiredService<ServiceA>(), ServiceLifetime.Transient));
                    strategy.Apply(services, ServiceDescriptor.Describe(typeof(ServiceB), typeof(ServiceB), ServiceLifetime.Singleton));
                    strategy.Apply(services, ServiceDescriptor.Describe(typeof(IService), _ => _.GetRequiredService<ServiceB>(), ServiceLifetime.Singleton));
                    strategy.Apply(services, ServiceDescriptor.Describe(typeof(IServiceB), _ => _.GetRequiredService<ServiceB>(), ServiceLifetime.Singleton));
                    break;
            }

            return services;
        }
    }
}
";
            using var context = new CollectibleTestAssemblyLoadContext();
            using var generator = new GeneratorTester(context)
                .Output(_testOutputHelper);
            generator.AddReferences(typeof(Scrutor.IFluentInterface).Assembly, typeof(ServiceCollection).Assembly, typeof(IServiceCollection).Assembly)
                .AssertGeneratedAsExpected<StaticScrutorGenerator>(
                    source,
                    expected,
                    "Scrutor.Static.Populate.cs"
                );

                generator.AssertGenerationWasSuccessful();
            generator.AssertCompilationWasSuccessful();

            var services = StaticHelper.ExecuteStaticServiceCollectionMethod(generator.Emit(), "Program", "LoadServices");
            Assert.Equal(6, services.Count());
            Assert.Equal(3, services.Count(z => z.ImplementationFactory is not null));
            Assert.Equal(3, services.Count(z => z.ImplementationType is not null));
            Assert.Equal(1, services.Count(z => z.Lifetime == ServiceLifetime.Scoped));
            Assert.Equal(2, services.Count(z => z.Lifetime == ServiceLifetime.Transient));
            Assert.Equal(3, services.Count(z => z.Lifetime == ServiceLifetime.Singleton));
        }

        [Theory]
        [InlineData(NamespaceFilter.Exact, "TestProject.A", 5, false, false)]
        [InlineData(NamespaceFilter.Exact, "TestProject.A.IService", 5, true, false)]
        [InlineData(NamespaceFilter.Exact, "TestProject.A.IService", 5, true, true)]
        [InlineData(NamespaceFilter.In, "TestProject.A", 7, false, false)]
        [InlineData(NamespaceFilter.In, "TestProject.A.IService", 7, true, false)]
        [InlineData(NamespaceFilter.In, "TestProject.A.IService", 7, true, true)]
        [InlineData(NamespaceFilter.NotIn, "TestProject.A.C", 7, false, false)]
        [InlineData(NamespaceFilter.NotIn, "TestProject.A.C.ServiceC", 7, true, false)]
        [InlineData(NamespaceFilter.NotIn, "TestProject.A.C.ServiceC", 7, true, true)]
        public void Should_Filter_Namespaces(NamespaceFilter filter, string namespaceFilterValue, int count, bool usingClass, bool usingTypeof)
        {
            var source = $@"
using Scrutor;
using Scrutor.Static;
using Microsoft.Extensions.DependencyInjection;

namespace TestProject.A
{{
    public interface IService {{ }}
    public class Service : IService, TestProject.B.IServiceB {{ }}
    public class ServiceA : IService {{ }}
}}

namespace TestProject.A.C
{{
    public class ServiceC : IService {{ }}
}}

namespace TestProject.B
{{
    public interface IServiceB {{ }}
    public class ServiceB : TestProject.A.IService {{ }}
}}

public static class Program {{
    static void Main() {{ }}
    static IServiceCollection LoadServices()
    {{
        var services = new ServiceCollection();
	    services.ScanStatic(
        z => z
			.FromAssemblies()
			.AddClasses(x => x.{
                (usingClass, usingTypeof, filter) switch {
                    (false, false, NamespaceFilter.Exact) => $"InExactNamespaces(\"{namespaceFilterValue}\")",
                    (false, false, NamespaceFilter.In) => $"InNamespaces(\"{namespaceFilterValue}\")",
                    (false, false, NamespaceFilter.NotIn) => $"InNamespaces(\"TestProject\").NotInNamespaces(\"{namespaceFilterValue}\")",
                    (true, false, NamespaceFilter.Exact) => $"InExactNamespaceOf(typeof({namespaceFilterValue}))",
                    (true, false, NamespaceFilter.In) => $"InNamespaceOf(typeof({namespaceFilterValue}))",
                    (true, false, NamespaceFilter.NotIn) => $"InNamespaces(\"TestProject\").NotInNamespaceOf(typeof({namespaceFilterValue}))",
                    (true, true, NamespaceFilter.Exact) => $"InExactNamespaceOf<{namespaceFilterValue}>()",
                    (true, true, NamespaceFilter.In) => $"InNamespaceOf<{namespaceFilterValue}>()",
                    (true, true, NamespaceFilter.NotIn) => $"InNamespaces(\"TestProject\").NotInNamespaceOf<{namespaceFilterValue}>()",
                    _ => "ERROR"}})
            .AsSelf()
            .AsImplementedInterfaces()
            .WithScopedLifetime()
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
                case 29:
                    {
                filter switch {
                    NamespaceFilter.Exact => @"strategy.Apply(services, ServiceDescriptor.Describe(typeof(TestProject.A.Service), typeof(TestProject.A.Service), ServiceLifetime.Scoped));
                    strategy.Apply(services, ServiceDescriptor.Describe(typeof(TestProject.A.IService), _ => _.GetRequiredService<TestProject.A.Service>(), ServiceLifetime.Scoped));
                    strategy.Apply(services, ServiceDescriptor.Describe(typeof(TestProject.B.IServiceB), _ => _.GetRequiredService<TestProject.A.Service>(), ServiceLifetime.Scoped));
                    strategy.Apply(services, ServiceDescriptor.Describe(typeof(TestProject.A.ServiceA), typeof(TestProject.A.ServiceA), ServiceLifetime.Scoped));
                    strategy.Apply(services, ServiceDescriptor.Describe(typeof(TestProject.A.IService), _ => _.GetRequiredService<TestProject.A.ServiceA>(), ServiceLifetime.Scoped));",
                    NamespaceFilter.In => @"strategy.Apply(services, ServiceDescriptor.Describe(typeof(TestProject.A.Service), typeof(TestProject.A.Service), ServiceLifetime.Scoped));
                    strategy.Apply(services, ServiceDescriptor.Describe(typeof(TestProject.A.IService), _ => _.GetRequiredService<TestProject.A.Service>(), ServiceLifetime.Scoped));
                    strategy.Apply(services, ServiceDescriptor.Describe(typeof(TestProject.B.IServiceB), _ => _.GetRequiredService<TestProject.A.Service>(), ServiceLifetime.Scoped));
                    strategy.Apply(services, ServiceDescriptor.Describe(typeof(TestProject.A.ServiceA), typeof(TestProject.A.ServiceA), ServiceLifetime.Scoped));
                    strategy.Apply(services, ServiceDescriptor.Describe(typeof(TestProject.A.IService), _ => _.GetRequiredService<TestProject.A.ServiceA>(), ServiceLifetime.Scoped));
                    strategy.Apply(services, ServiceDescriptor.Describe(typeof(TestProject.A.C.ServiceC), typeof(TestProject.A.C.ServiceC), ServiceLifetime.Scoped));
                    strategy.Apply(services, ServiceDescriptor.Describe(typeof(TestProject.A.IService), _ => _.GetRequiredService<TestProject.A.C.ServiceC>(), ServiceLifetime.Scoped));",
                    NamespaceFilter.NotIn => @"strategy.Apply(services, ServiceDescriptor.Describe(typeof(TestProject.A.Service), typeof(TestProject.A.Service), ServiceLifetime.Scoped));
                    strategy.Apply(services, ServiceDescriptor.Describe(typeof(TestProject.A.IService), _ => _.GetRequiredService<TestProject.A.Service>(), ServiceLifetime.Scoped));
                    strategy.Apply(services, ServiceDescriptor.Describe(typeof(TestProject.B.IServiceB), _ => _.GetRequiredService<TestProject.A.Service>(), ServiceLifetime.Scoped));
                    strategy.Apply(services, ServiceDescriptor.Describe(typeof(TestProject.A.ServiceA), typeof(TestProject.A.ServiceA), ServiceLifetime.Scoped));
                    strategy.Apply(services, ServiceDescriptor.Describe(typeof(TestProject.A.IService), _ => _.GetRequiredService<TestProject.A.ServiceA>(), ServiceLifetime.Scoped));
                    strategy.Apply(services, ServiceDescriptor.Describe(typeof(TestProject.B.ServiceB), typeof(TestProject.B.ServiceB), ServiceLifetime.Scoped));
                    strategy.Apply(services, ServiceDescriptor.Describe(typeof(TestProject.A.IService), _ => _.GetRequiredService<TestProject.B.ServiceB>(), ServiceLifetime.Scoped));"
                }}
                    break;
            }}

            return services;
        }}
    }}
}}
";
            using var context = new CollectibleTestAssemblyLoadContext();
            using var generator = new GeneratorTester(context)
                .Output(_testOutputHelper);
            generator.AddReferences(typeof(Scrutor.IFluentInterface).Assembly, typeof(ServiceCollection).Assembly, typeof(IServiceCollection).Assembly)
                .AssertGeneratedAsExpected<StaticScrutorGenerator>(
                    source,
                    expected,
                    "Scrutor.Static.Populate.cs"
                );

            generator.AssertGenerationWasSuccessful();

            var services = StaticHelper.ExecuteStaticServiceCollectionMethod(generator.Emit(), "Program", "LoadServices");
            Assert.Equal(count, services.Count());
        }

        [Theory]
        [InlineData(NamespaceFilter.Exact, "TestProject.A", "TestProject.B", 7, false)]
        [InlineData(NamespaceFilter.Exact, "TestProject.A.ServiceA", "TestProject.B.ServiceB", 7, true)]
        [InlineData(NamespaceFilter.In, "TestProject.A", "TestProject.B", 9, false)]
        [InlineData(NamespaceFilter.In, "TestProject.A.ServiceA", "TestProject.B.ServiceB", 9, true)]
        [InlineData(NamespaceFilter.NotIn, "TestProject.A.C", "TestProject.B", 5, false)]
        [InlineData(NamespaceFilter.NotIn, "TestProject.A.C.ServiceC", "TestProject.B.ServiceB", 5, true)]
        public void Should_Filter_Multiple_Namespaces(NamespaceFilter filter, string namespaceFilterValue, string namespaceFilterValueSecond, int count, bool usingClass)
        {
            var source = $@"
using Scrutor;
using Scrutor.Static;
using Microsoft.Extensions.DependencyInjection;

namespace TestProject.A
{{
    public interface IService {{ }}
    public class Service : IService, TestProject.B.IServiceB {{ }}
    public class ServiceA : IService {{ }}
}}

namespace TestProject.A.C
{{
    public class ServiceC : IService {{ }}
}}

namespace TestProject.B
{{
    public interface IServiceB {{ }}
    public class ServiceB : TestProject.A.IService {{ }}
}}

public static class Program {{
    static void Main() {{ }}
    static IServiceCollection LoadServices()
    {{
        var services = new ServiceCollection();
	    services.ScanStatic(
        z => z
			.FromAssemblies()
			.AddClasses(x => x.{
                (usingClass, filter) switch {
                    (false, NamespaceFilter.Exact) => $"InExactNamespaces(\"{namespaceFilterValue}\", \"{namespaceFilterValueSecond}\")",
                    (false, NamespaceFilter.In) => $"InNamespaces(\"{namespaceFilterValue}\", \"{namespaceFilterValueSecond}\")",
                    (false, NamespaceFilter.NotIn) => $"InNamespaces(\"TestProject\").NotInNamespaces(\"{namespaceFilterValue}\", \"{namespaceFilterValueSecond}\")",
                    (true, NamespaceFilter.Exact) => $"InExactNamespaceOf(typeof({namespaceFilterValue}), typeof({namespaceFilterValueSecond}))",
                    (true, NamespaceFilter.In) => $"InNamespaceOf(typeof({namespaceFilterValue}), typeof({namespaceFilterValueSecond}))",
                    (true, NamespaceFilter.NotIn) => $"InNamespaces(\"TestProject\").NotInNamespaceOf(typeof({namespaceFilterValue}), typeof({namespaceFilterValueSecond}))",
                    _ => "ERROR"}})
            .AsSelf()
            .AsImplementedInterfaces()
            .WithScopedLifetime()
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
                case 29:
                    {
                filter switch {
                    NamespaceFilter.Exact => @"strategy.Apply(services, ServiceDescriptor.Describe(typeof(TestProject.A.Service), typeof(TestProject.A.Service), ServiceLifetime.Scoped));
                    strategy.Apply(services, ServiceDescriptor.Describe(typeof(TestProject.A.IService), _ => _.GetRequiredService<TestProject.A.Service>(), ServiceLifetime.Scoped));
                    strategy.Apply(services, ServiceDescriptor.Describe(typeof(TestProject.B.IServiceB), _ => _.GetRequiredService<TestProject.A.Service>(), ServiceLifetime.Scoped));
                    strategy.Apply(services, ServiceDescriptor.Describe(typeof(TestProject.A.ServiceA), typeof(TestProject.A.ServiceA), ServiceLifetime.Scoped));
                    strategy.Apply(services, ServiceDescriptor.Describe(typeof(TestProject.A.IService), _ => _.GetRequiredService<TestProject.A.ServiceA>(), ServiceLifetime.Scoped));
                    strategy.Apply(services, ServiceDescriptor.Describe(typeof(TestProject.B.ServiceB), typeof(TestProject.B.ServiceB), ServiceLifetime.Scoped));
                    strategy.Apply(services, ServiceDescriptor.Describe(typeof(TestProject.A.IService), _ => _.GetRequiredService<TestProject.B.ServiceB>(), ServiceLifetime.Scoped));",
                    NamespaceFilter.In => @"strategy.Apply(services, ServiceDescriptor.Describe(typeof(TestProject.A.Service), typeof(TestProject.A.Service), ServiceLifetime.Scoped));
                    strategy.Apply(services, ServiceDescriptor.Describe(typeof(TestProject.A.IService), _ => _.GetRequiredService<TestProject.A.Service>(), ServiceLifetime.Scoped));
                    strategy.Apply(services, ServiceDescriptor.Describe(typeof(TestProject.B.IServiceB), _ => _.GetRequiredService<TestProject.A.Service>(), ServiceLifetime.Scoped));
                    strategy.Apply(services, ServiceDescriptor.Describe(typeof(TestProject.A.ServiceA), typeof(TestProject.A.ServiceA), ServiceLifetime.Scoped));
                    strategy.Apply(services, ServiceDescriptor.Describe(typeof(TestProject.A.IService), _ => _.GetRequiredService<TestProject.A.ServiceA>(), ServiceLifetime.Scoped));
                    strategy.Apply(services, ServiceDescriptor.Describe(typeof(TestProject.A.C.ServiceC), typeof(TestProject.A.C.ServiceC), ServiceLifetime.Scoped));
                    strategy.Apply(services, ServiceDescriptor.Describe(typeof(TestProject.A.IService), _ => _.GetRequiredService<TestProject.A.C.ServiceC>(), ServiceLifetime.Scoped));
                    strategy.Apply(services, ServiceDescriptor.Describe(typeof(TestProject.B.ServiceB), typeof(TestProject.B.ServiceB), ServiceLifetime.Scoped));
                    strategy.Apply(services, ServiceDescriptor.Describe(typeof(TestProject.A.IService), _ => _.GetRequiredService<TestProject.B.ServiceB>(), ServiceLifetime.Scoped));",
                    NamespaceFilter.NotIn => @"strategy.Apply(services, ServiceDescriptor.Describe(typeof(TestProject.A.Service), typeof(TestProject.A.Service), ServiceLifetime.Scoped));
                    strategy.Apply(services, ServiceDescriptor.Describe(typeof(TestProject.A.IService), _ => _.GetRequiredService<TestProject.A.Service>(), ServiceLifetime.Scoped));
                    strategy.Apply(services, ServiceDescriptor.Describe(typeof(TestProject.B.IServiceB), _ => _.GetRequiredService<TestProject.A.Service>(), ServiceLifetime.Scoped));
                    strategy.Apply(services, ServiceDescriptor.Describe(typeof(TestProject.A.ServiceA), typeof(TestProject.A.ServiceA), ServiceLifetime.Scoped));
                    strategy.Apply(services, ServiceDescriptor.Describe(typeof(TestProject.A.IService), _ => _.GetRequiredService<TestProject.A.ServiceA>(), ServiceLifetime.Scoped));"
                }}
                    break;
            }}

            return services;
        }}
    }}
}}
";
            using var context = new CollectibleTestAssemblyLoadContext();
            using var generator = new GeneratorTester(context)
                .Output(_testOutputHelper);
            generator.AddReferences(typeof(Scrutor.IFluentInterface).Assembly, typeof(ServiceCollection).Assembly, typeof(IServiceCollection).Assembly)
                .AssertGeneratedAsExpected<StaticScrutorGenerator>(
                    source,
                    expected,
                    "Scrutor.Static.Populate.cs"
                );

            generator.AssertGenerationWasSuccessful();

            var services = StaticHelper.ExecuteStaticServiceCollectionMethod(generator.Emit(), "Program", "LoadServices");
            Assert.Equal(count, services.Count());
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Should_Select_Specific_Assemblies_Using_FromAssemblyOf(bool useTypeof)
        {
            using var context = new CollectibleTestAssemblyLoadContext();

            var dependencies = new List<CSharpCompilation>();

            static CSharpCompilation CreateServiceDependency(CollectibleTestAssemblyLoadContext context, string suffix, params CSharpCompilation[] dependencies)
            {
                using var generator = new GeneratorTester(context, "DependencyProject" + suffix);
                var root = generator
                    .AddCompilationReference(dependencies)
                    .AddSources($@"
namespace DependencyProject{suffix}
{{
    public interface IService{suffix} {{ }}
    public class Service{suffix} : IService{suffix} {{ }}
}}
").Compile();
                generator.AssertCompilationWasSuccessful();
                generator.AssertGenerationWasSuccessful();
                generator.Emit();
                return root;
            }

            var dependencyA = CreateServiceDependency(context, "A");
            var dependencyB = CreateServiceDependency(context, "B");
            var dependencyC = CreateServiceDependency(context, "C", dependencyA);
            var dependencyD = CreateServiceDependency(context, "D", dependencyC);
            dependencies.Add(dependencyA);
            dependencies.Add(dependencyB);
            dependencies.Add(dependencyC);
            dependencies.Add(dependencyD);

            var source = $@"
using Scrutor;
using Scrutor.Static;
using Microsoft.Extensions.DependencyInjection;
using DependencyProjectA;
using DependencyProjectB;
using DependencyProjectC;
using DependencyProjectD;

namespace TestProject
{{
    public static class Program
    {{
        static void Main() {{ }}
        static IServiceCollection LoadServices()
        {{
            var services = new ServiceCollection();
	        services.ScanStatic(
            z => z
                .FromAssemblyOf{(useTypeof ? $"(typeof(IServiceB))" : $"<IServiceB>()")}
                .AddClasses()
                .AsSelf()
                .AsImplementedInterfaces()
                .WithSingletonLifetime()
            );
            return services;
        }}
    }}
}}
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
                case 18:
                    strategy.Apply(services, ServiceDescriptor.Describe(typeof(DependencyProjectB.ServiceB), typeof(DependencyProjectB.ServiceB), ServiceLifetime.Singleton));
                    strategy.Apply(services, ServiceDescriptor.Describe(typeof(DependencyProjectB.IServiceB), _ => _.GetRequiredService<DependencyProjectB.ServiceB>(), ServiceLifetime.Singleton));
                    break;
            }

            return services;
        }
    }
}
";

            using var generator = new GeneratorTester(context)
                .Output(_testOutputHelper);
            generator
                .AddReferences(typeof(Scrutor.IFluentInterface).Assembly, typeof(ServiceCollection).Assembly, typeof(IServiceCollection).Assembly)
                .AddCompilationReference(dependencies)
                .AssertGeneratedAsExpected<StaticScrutorGenerator>(
                    source,
                    expected,
                    "Scrutor.Static.Populate.cs"
                );

            foreach (var tree in generator.GeneratorSyntaxTrees)
            {
                _testOutputHelper.WriteLine(tree.GetText().ToString());
            }

            generator.AssertGenerationWasSuccessful();

            var services = StaticHelper.ExecuteStaticServiceCollectionMethod(generator.Emit(), "Program", "LoadServices");
            Assert.Equal(1, services.Count(z => z.ImplementationFactory is not null));
            Assert.Equal(1, services.Count(z => z.ImplementationType is not null));
            Assert.Equal(2, services.Count(z => z.Lifetime == ServiceLifetime.Singleton));
        }

        [Theory]
        [InlineData("ServiceA", false, 2)]
        [InlineData("ServiceB", false, 0)]
        [InlineData("ServiceC", false, 1)]
        [InlineData("ServiceA", true, 2)]
        [InlineData("ServiceB", true, 0)]
        [InlineData("ServiceC", true, 1)]
        public void Should_Select_Specific_Assemblies_Using_FromAssemblyDependenciesOf(string className, bool useTypeof, int expectedCount)
        {
            using var context = new CollectibleTestAssemblyLoadContext();

            var dependencies = new List<CSharpCompilation>();

            static CSharpCompilation CreateRoot(CollectibleTestAssemblyLoadContext context, params CSharpCompilation[] dependencies)
            {
                using var generator = new GeneratorTester(context, "RootDependencyProject");
                var dep = generator
                    .AddCompilationReference(dependencies)
                    .AddSources($@"
namespace RootDependencyProject
{{
    public interface IService {{ }}
}}
").Compile();
                generator.AssertCompilationWasSuccessful();
                generator.AssertGenerationWasSuccessful();
                generator.Emit();
                return dep;
            }

            static CSharpCompilation CreateServiceDependency(CollectibleTestAssemblyLoadContext context, string suffix, params CSharpCompilation[] dependencies)
            {
                using var generator = new GeneratorTester(context, "DependencyProject" + suffix);
                var additionalCode = dependencies
                    .Where(z => z.AssemblyName?.StartsWith("DependencyProject") == true)
                    .Select(z =>
                        $"class HardReference{z.AssemblyName?.Substring(z.AssemblyName.Length - 1)} : {z.AssemblyName + ".Service" + z.AssemblyName?.Substring(z.AssemblyName.Length - 1)} {{ }}");
                var dep = generator
                    .AddCompilationReference(dependencies)
                    .AddSources($@"
namespace DependencyProject{suffix}
{{
    {string.Join("\n", additionalCode)}
    public class Service{suffix} : RootDependencyProject.IService {{ }}
}}
").Compile();
                generator.AssertCompilationWasSuccessful();
                generator.AssertGenerationWasSuccessful();
                generator.Emit();
                return dep;
            }

            var root = CreateRoot(context);
            var dependencyA = CreateServiceDependency(context, "A", root);
            var dependencyB = CreateServiceDependency(context, "B", root);
            var dependencyC = CreateServiceDependency(context, "C", dependencyA, root);
            var dependencyD = CreateServiceDependency(context, "D", dependencyA, dependencyC, root);
            dependencies.Add(root);
            dependencies.Add(dependencyA);
            dependencies.Add(dependencyB);
            dependencies.Add(dependencyC);
            dependencies.Add(dependencyD);

            var source = $@"

using Scrutor;
using Scrutor.Static;
using Microsoft.Extensions.DependencyInjection;
using RootDependencyProject;
using DependencyProjectA;
using DependencyProjectB;
using DependencyProjectC;
using DependencyProjectD;

namespace TestProject
{{
    public static class Program
    {{
        static void Main() {{ }}
        static IServiceCollection LoadServices()
        {{
            var services = new ServiceCollection();
	        services.ScanStatic(
            z => z
			    .FromAssemblyDependenciesOf{(useTypeof ? $"(typeof({className}))" : $"<{className}>()")}
                .AddClasses(x => x.AssignableTo(typeof(IService)), true)
                .AsSelf()
                .AsImplementedInterfaces()
                .WithSingletonLifetime()
            );
            return services;
        }}
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
                case 20:{className switch {
                "ServiceA" => @"
                    strategy.Apply(services, ServiceDescriptor.Describe(typeof(DependencyProjectC.ServiceC), typeof(DependencyProjectC.ServiceC), ServiceLifetime.Singleton));
                    strategy.Apply(services, ServiceDescriptor.Describe(typeof(RootDependencyProject.IService), _ => _.GetRequiredService<DependencyProjectC.ServiceC>(), ServiceLifetime.Singleton));
                    strategy.Apply(services, ServiceDescriptor.Describe(typeof(DependencyProjectD.ServiceD), typeof(DependencyProjectD.ServiceD), ServiceLifetime.Singleton));
                    strategy.Apply(services, ServiceDescriptor.Describe(typeof(RootDependencyProject.IService), _ => _.GetRequiredService<DependencyProjectD.ServiceD>(), ServiceLifetime.Singleton));",
                "ServiceB" => @"",
                "ServiceC" => @"
                    strategy.Apply(services, ServiceDescriptor.Describe(typeof(DependencyProjectD.ServiceD), typeof(DependencyProjectD.ServiceD), ServiceLifetime.Singleton));
                    strategy.Apply(services, ServiceDescriptor.Describe(typeof(RootDependencyProject.IService), _ => _.GetRequiredService<DependencyProjectD.ServiceD>(), ServiceLifetime.Singleton));",
                _ => ""
            }}
                    break;
            }}

            return services;
        }}
    }}
}}
";

            using var generator = new GeneratorTester(context)
                .Output(_testOutputHelper);
            generator
                .AddReferences(typeof(Scrutor.IFluentInterface).Assembly, typeof(ServiceCollection).Assembly, typeof(IServiceCollection).Assembly)
                .AddCompilationReference(dependencies)
                .AssertGeneratedAsExpected<StaticScrutorGenerator>(
                    source,
                    expected,
                    "Scrutor.Static.Populate.cs"
                );

            foreach (var tree in generator.GeneratorSyntaxTrees)
            {
                _testOutputHelper.WriteLine(tree.GetText().ToString());
            }

            generator.AssertGenerationWasSuccessful();

            var services = StaticHelper.ExecuteStaticServiceCollectionMethod(generator.Emit(), "Program", "LoadServices");
            Assert.Equal(expectedCount, services.Count(z => z.ImplementationFactory is not null));
            Assert.Equal(expectedCount, services.Count(z => z.ImplementationType is not null));
            Assert.Equal(expectedCount * 2, services.Count(z => z.Lifetime == ServiceLifetime.Singleton));
        }

        [Fact]
        public void Should_Report_Diagnostic_When_Not_Using_Expressions()
        {
            var source = @"
using Scrutor;
using Scrutor.Static;
using Microsoft.Extensions.DependencyInjection;

public interface IService { }
public class Service : IService { }

public static class Program {
    static void Main() { }
    static IServiceCollection LoadServices()
    {
        var services = new ServiceCollection();
	    services.ScanStatic(
        z => { 
               z.FromAssemblies()
			    .AddClasses(x => x.AssignableTo(typeof(IService)))
                .AsSelf()
                .AsImplementedInterfaces()
                .WithScopedLifetime();
        });
        return services;
    }
}
";

            using var context = new CollectibleTestAssemblyLoadContext();
            using var generator = new GeneratorTester(context)
                .Output(_testOutputHelper);
            generator.AddReferences(typeof(Scrutor.IFluentInterface).Assembly, typeof(ServiceCollection).Assembly, typeof(IServiceCollection).Assembly)
                .Generate<StaticScrutorGenerator>(source);

            generator.AssertCompilationWasSuccessful();

            Assert.NotEmpty(generator.GeneratorDiagnostics);
            Assert.Contains(generator.GeneratorDiagnostics, z => z.Id == Diagnostics.MustBeAnExpression.Id);
        }

        [Fact]
        public void Should_Report_Diagnostic_Not_Given_A_Compiled_Type()
        {
            var source = @"
using Scrutor;
using Scrutor.Static;
using Microsoft.Extensions.DependencyInjection;

public interface IService { }
public class Service : IService { }

public static class Program {
    static void Main() { }
    static IServiceCollection LoadServices()
    {
        var type = typeof(IService);
        var services = new ServiceCollection();
	    services.ScanStatic(z => z.FromAssemblies()
			  .AddClasses(x => x.AssignableTo(type))
              .AsSelf()
              .AsImplementedInterfaces()
              .WithScopedLifetime());
        return services;
    }
}
";

            using var context = new CollectibleTestAssemblyLoadContext();
            using var generator = new GeneratorTester(context)
                .Output(_testOutputHelper);
            generator.AddReferences(typeof(Scrutor.IFluentInterface).Assembly, typeof(ServiceCollection).Assembly, typeof(IServiceCollection).Assembly)
                .Generate<StaticScrutorGenerator>(source);

            generator.AssertCompilationWasSuccessful();

            Assert.NotEmpty(generator.GeneratorDiagnostics);
            Assert.Contains(generator.GeneratorDiagnostics, z => z.Id == Diagnostics.MustBeTypeOf.Id);
        }

        [Fact]
        public void Should_Report_Diagnostic_Not_Given_A_Static_Namespace()
        {
            var source = @"
using Scrutor;
using Scrutor.Static;
using Microsoft.Extensions.DependencyInjection;

public interface IService { }
public class Service : IService { }

public static class Program {
    static void Main() { }
    static IServiceCollection LoadServices()
    {
        var ns = ""MyNamespace""; 
        var services = new ServiceCollection();
	    services.ScanStatic(z => z.FromAssemblies()
			  .AddClasses(x => x.InNamespaces(ns))
              .AsSelf()
              .AsImplementedInterfaces()
              .WithScopedLifetime());
        return services;
    }
}
";

            using var context = new CollectibleTestAssemblyLoadContext();
            using var generator = new GeneratorTester(context)
                .Output(_testOutputHelper);
            generator.AddReferences(typeof(Scrutor.IFluentInterface).Assembly, typeof(ServiceCollection).Assembly, typeof(IServiceCollection).Assembly)
                .Generate<StaticScrutorGenerator>(source);

            generator.AssertCompilationWasSuccessful();

            Assert.NotEmpty(generator.GeneratorDiagnostics);
            Assert.Contains(generator.GeneratorDiagnostics, z => z.Id == Diagnostics.NamespaceMustBeAString.Id);
        }

        [Fact]
        public void Should_Report_Diagnostic_For_Duplicate_ServiceDescriptorAttributes()
        {
            var source = $@"
using System;
using Scrutor;
using Scrutor.Static;
using Microsoft.Extensions.DependencyInjection;

public interface IService {{ }}
[ServiceDescriptor(typeof(IService), ServiceLifetime.Scoped)]
[ServiceDescriptor(typeof(IService), ServiceLifetime.Singleton)]
public class Service : IService {{ }}

public static class Program {{
    static void Main() {{ }}
    static IServiceCollection LoadServices()
    {{
        var services = new ServiceCollection();
	    services.ScanStatic(
        z => z
			.FromAssemblies()
			.AddClasses(x => x.AssignableTo(typeof(IService)))
            .UsingAttributes()
            .WithSingletonLifetime()
        );
        return services;
    }}
}}
";

            using var context = new CollectibleTestAssemblyLoadContext();
            using var generator = new GeneratorTester(context)
                .Output(_testOutputHelper);
            generator.AddReferences(typeof(Scrutor.IFluentInterface).Assembly, typeof(ServiceCollection).Assembly, typeof(IServiceCollection).Assembly)
                .Generate<StaticScrutorGenerator>(source);

            generator.AssertCompilationWasSuccessful();

            Assert.Single(generator.GeneratorDiagnostics);
            Assert.Contains(generator.GeneratorDiagnostics, z => z.Id == Diagnostics.DuplicateServiceDescriptorAttribute.Id && z.Location.GetLineSpan().StartLinePosition.Line == 8);
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
