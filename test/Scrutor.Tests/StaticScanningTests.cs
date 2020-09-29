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
            Assert.Equal(1, services.Count());
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
            Assert.Equal(1, services.Count());
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
                .Generate<StaticScrutorGenerator>(source);
                // .AssertGeneratedAsExpected<StaticScrutorGenerator>(
                //     source,
                //     expected,
                //     "Scrutor.Static.Populate.cs"
                // );

            generator.AssertGenerationWasSuccessful();

            var services = StaticHelper.ExecuteStaticServiceCollectionMethod(generator.Emit(), "Program", "LoadServices");
            Assert.Equal(count, services.Count());
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

            Assert.NotEmpty(generator.GeneratorDiagnostics);
            Assert.Contains(generator.GeneratorDiagnostics, z => z.Id == "SCTR0001");
        }

        [Fact]
        public void Should_Report_Diagnostic_Not_Give_A_Compiled_Type()
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

            Assert.NotEmpty(generator.GeneratorDiagnostics);
            Assert.Contains(generator.GeneratorDiagnostics, z => z.Id == "SCTR0002");
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
