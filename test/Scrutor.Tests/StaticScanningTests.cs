using Microsoft.Extensions.DependencyInjection;
using Scrutor.Tests;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
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

        [Theory]
        [InlineData(ServiceLifetime.Scoped)]
        [InlineData(ServiceLifetime.Singleton)]
        [InlineData(ServiceLifetime.Transient)]
        public async Task Should_Have_Correct_Lifetime(ServiceLifetime serviceLifetime)
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
    static ServiceCollection Services = new ServiceCollection();
    static void Main()
    {{
	    Services.ScanStatic(z => z
			.FromAssemblies()
			.AddClasses(x => x.AssignableTo<IService>(), false)
            .AsSelf()
            .AsImplementedInterfaces()
            .With{
                    serviceLifetime
                }Lifetime()
        );
	    Services.ScanStatic(z => z
			.FromAssemblies()
			.AddClasses(x => x.AssignableTo<IServiceB>(), false)
            .AsSelf()
            .AsMatchingInterface()
            .WithLifetime(ServiceLifetime.{
                    serviceLifetime
                })
        );
    }}
}}
";

            var expected = $@"
using System;
using Microsoft.Extensions.DependencyInjection;
using Scrutor;

namespace Scrutor.Static
{{
    internal static class PopulateExtensions
    {{
        public static IServiceCollection Populate(IServiceCollection services, RegistrationStrategy strategy, string filePath, string memberName, int lineNumber)
        {{
            switch (lineNumber)
            {{
                case 30:
                    strategy.Apply(services, ServiceDescriptor.Describe(typeof(Service), typeof(Service), ServiceLifetime.{
                    serviceLifetime
                }));
                    strategy.Apply(services, ServiceDescriptor.Describe(typeof(IService), _ => _.GetRequiredService(typeof(Service)), ServiceLifetime.{
                    serviceLifetime
                }));
                    break;
                case 37:
                    strategy.Apply(services, ServiceDescriptor.Describe(typeof(ServiceB), typeof(ServiceB), ServiceLifetime.{
                    serviceLifetime
                }));
                    strategy.Apply(services, ServiceDescriptor.Describe(typeof(IServiceB), _ => _.GetRequiredService(typeof(ServiceB)), ServiceLifetime.{
                    serviceLifetime
                }));
                    break;
            }}

            return services;
        }}
    }}
}}
";


            await AssertGeneratedAsExpected<StaticScrutorGenerator>(
                new[] { typeof(Scrutor.IFluentInterface).Assembly, typeof(ServiceCollection).Assembly, typeof(IServiceCollection).Assembly },
                source,
                expected,
                "Scrutor.Static.Populate.cs"
            ).ConfigureAwait(false);
        }


        [Theory]
        [InlineData(ServiceLifetime.Scoped)]
        [InlineData(ServiceLifetime.Singleton)]
        [InlineData(ServiceLifetime.Transient)]
        public async Task Should_Have_Correct_Lifetime_Real_Code(ServiceLifetime serviceLifetime)
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
    static ServiceCollection Services = new ServiceCollection();
    static void Main() {{ }}
    static void LoadServices()
    {{
	    Services.ScanStatic(
        z => z
			.FromAssemblies()
			.AddClasses(x => x.AssignableTo<IService>())
            .AsSelf()
            .AsImplementedInterfaces()
            .With{
                    serviceLifetime
                }Lifetime()
        );
	    Services.ScanStatic(
        z => z
			.FromAssemblies()
			.AddClasses(x => x.AssignableTo<IServiceB>(), false)
            .AsSelf()
            .AsMatchingInterface()
            .WithLifetime(ServiceLifetime.{
                    serviceLifetime
                })
        );
    }}
}}
";
            var compilation = await CreateProject<StaticScrutorGenerator>(
                new[] { typeof(IFluentInterface).Assembly, typeof(ServiceCollection).Assembly, typeof(IServiceCollection).Assembly },
                source
            ).ConfigureAwait(false);

            using var context = new CollectibleTestAssemblyLoadContext();

            var extensionTree = compilation.SyntaxTrees.FirstOrDefault(z => z.GetText().ToString().Contains("switch"));
            _testOutputHelper.WriteLine(extensionTree.GetText().ToString());

            byte[] data;
            {
                using var stream = new MemoryStream();
                var emitResult = compilation!.Emit(stream, options: new EmitOptions());
                if (!emitResult.Success)
                {
                    Assert.Empty(emitResult.Diagnostics);
                }

                data = stream.ToArray();
            }

            using var assemblyStream = new MemoryStream(data);
            var assembly = context.LoadFromStream(assemblyStream);

            var extension = assembly.GetTypes().FirstOrDefault(z => z.IsClass && z.Name == "Program");
            Assert.NotNull(extension);

            var method = extension.GetMethod("LoadServices", BindingFlags.Static | BindingFlags.NonPublic);
            Assert.NotNull(method);

            var servicesfield = extension.GetField("Services", BindingFlags.Static | BindingFlags.NonPublic);
            Assert.NotNull(servicesfield);

            var services = servicesfield!.GetValue(null) as IServiceCollection;
            Assert.NotNull(services);

            method.Invoke(null, new object[] { });

            Assert.Equal(4, services.Count());
            Assert.Equal(2, services.Count(z => z.ImplementationFactory is not null));
            Assert.Equal(2, services.Count(z => z.ImplementationType is not null));
        }

        [Fact]
        public async Task Should_Split_Correctly_Given_Same_Line_Number()
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
    static void Method()
    {
	    Services.ScanStatic(z => z
			.FromAssemblies()
			.AddClasses(x => x.AssignableTo<IService>(), false)
            .AsSelf()
            .AsImplementedInterfaces()
            .WithSingletonLifetime()
        );
    }
}
";

            var source2 = @"
using Scrutor;
using Scrutor.Static;
using Microsoft.Extensions.DependencyInjection;

public static class Program2 {
    static ServiceCollection Services = new ServiceCollection();

    static void Method()
    {
	    Services.ScanStatic(z => z
			.FromAssemblies()
			.AddClasses(x => x.AssignableTo<IServiceB>(), false)
            .AsSelf()
            .AsImplementedInterfaces()
            .WithScopedLifetime()
        );
    }
}
";

            var expected = @"
using System;
using Microsoft.Extensions.DependencyInjection;
using Scrutor;

namespace Scrutor.Static
{
    internal static class PopulateExtensions
    {
        public static IServiceCollection Populate(IServiceCollection services, RegistrationStrategy strategy, string filePath, string memberName, int lineNumber)
        {
            switch (lineNumber)
            {
                case 11:
                    switch (filePath)
                    {
                        case ""Test1.cs"":
                            strategy.Apply(services, ServiceDescriptor.Describe(typeof(Service), typeof(Service), ServiceLifetime.Singleton));
                            strategy.Apply(services, ServiceDescriptor.Describe(typeof(IService), _ => _.GetRequiredService(typeof(Service)), ServiceLifetime.Singleton));
                            break;
                        case ""Test2.cs"":
                            strategy.Apply(services, ServiceDescriptor.Describe(typeof(ServiceB), typeof(ServiceB), ServiceLifetime.Scoped));
                            strategy.Apply(services, ServiceDescriptor.Describe(typeof(IServiceB), _ => _.GetRequiredService(typeof(ServiceB)), ServiceLifetime.Scoped));
                            break;
                    }

                    break;
            }

            return services;
        }
    }
}
";


            await AssertGeneratedAsExpected<StaticScrutorGenerator>(
                new[] { typeof(Scrutor.IFluentInterface).Assembly, typeof(ServiceCollection).Assembly, typeof(IServiceCollection).Assembly },
                new[] { source, source1, source2 },
                new[] { expected },
                "Scrutor.Static.Populate.cs"
            ).ConfigureAwait(false);
        }


        [Fact]
        public async Task Should_Split_Correctly_Given_Same_Line_Number_Run()
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
    static void Method()
    {
	    Services.ScanStatic(z => z
			.FromAssemblies()
			.AddClasses(x => x.AssignableTo<IService>(), false)
            .AsSelf()
            .AsImplementedInterfaces()
            .WithSingletonLifetime()
        );
    }
}
";

            var source2 = @"
using Scrutor;
using Scrutor.Static;
using Microsoft.Extensions.DependencyInjection;

public static class Program2 {
    static ServiceCollection Services = new ServiceCollection();

    static void Method()
    {
	    Services.ScanStatic(z => z
			.FromAssemblies()
			.AddClasses(x => x.AssignableTo<IServiceB>(), false)
            .AsSelf()
            .AsImplementedInterfaces()
            .WithScopedLifetime()
        );
    }
}
";
            var compilation = await CreateProject<StaticScrutorGenerator>(
                new[] { typeof(IFluentInterface).Assembly, typeof(ServiceCollection).Assembly, typeof(IServiceCollection).Assembly },
                source, source1, source2
            ).ConfigureAwait(false);

            using var context = new CollectibleTestAssemblyLoadContext();

            var extensionTree = compilation.SyntaxTrees.FirstOrDefault(z => z.GetText().ToString().Contains("switch"));

            byte[] data;
            {
                using var stream = new MemoryStream();
                var emitResult = compilation!.Emit(stream, options: new EmitOptions());
                if (!emitResult.Success)
                {
                    Assert.Empty(emitResult.Diagnostics);
                }

                data = stream.ToArray();
            }

            using var assemblyStream = new MemoryStream(data);
            var assembly = context.LoadFromStream(assemblyStream);

            var program1 = assembly.GetTypes().FirstOrDefault(z => z.IsClass && z.Name == "Program");
            Assert.NotNull(program1);

            var method1 = program1.GetMethod("Method", BindingFlags.Static | BindingFlags.NonPublic);
            Assert.NotNull(method1);

            var servicesField1 = program1.GetField("Services", BindingFlags.Static | BindingFlags.NonPublic);
            Assert.NotNull(servicesField1);

            var services1 = servicesField1!.GetValue(null) as IServiceCollection;
            Assert.NotNull(services1);

            var program2 = assembly.GetTypes().FirstOrDefault(z => z.IsClass && z.Name == "Program2");
            Assert.NotNull(program2);

            var method2 = program2.GetMethod("Method", BindingFlags.Static | BindingFlags.NonPublic);
            Assert.NotNull(method2);

            var servicesField2 = program2.GetField("Services", BindingFlags.Static | BindingFlags.NonPublic);
            Assert.NotNull(servicesField2);

            var services2 = servicesField2!.GetValue(null) as IServiceCollection;
            Assert.NotNull(services2);

            method1.Invoke(null, new object[] {  });
            method2.Invoke(null, new object[] {  });

            Assert.Equal(2, services1.Count());
            Assert.Equal(1, services1.Count(z => z.ImplementationFactory is not null));
            Assert.Equal(1, services1.Count(z => z.ImplementationType is not null));
            Assert.Equal(2, services1.Count(z => z.Lifetime == ServiceLifetime.Singleton));

            Assert.Equal(2, services2.Count());
            Assert.Equal(1, services2.Count(z => z.ImplementationFactory is not null));
            Assert.Equal(1, services2.Count(z => z.ImplementationType is not null));
            Assert.Equal(2, services2.Count(z => z.Lifetime == ServiceLifetime.Scoped));
        }
    }
}
