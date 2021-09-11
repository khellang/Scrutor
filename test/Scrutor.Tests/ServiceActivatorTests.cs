using Microsoft.Extensions.DependencyInjection;

using System;
using System.Collections.Generic;

using Xunit;

using Scrutor.Activation;
using System.Linq;

namespace Scrutor.Tests
{
    public class ServiceActivatorTests : TestBase
    {
        #region Tests From Microsoft.Extensions.DependencyInjection.

        #region Fakes

        public interface IFakeService
        {
        }

        public interface IFakeOuterService
        {
            IFakeService SingleService { get; }

            IEnumerable<IFakeMultipleService> MultipleServices { get; }
        }

        public interface IFakeMultipleService : IFakeService
        {
        }

        public class PocoClass
        {
        }

        public class FakeService : IFakeService, IDisposable
        {
            public PocoClass Value { get; set; }

            public bool Disposed { get; private set; }

            public void Dispose()
            {
                if (Disposed)
                {
                    throw new ObjectDisposedException(nameof(FakeService));
                }

                Disposed = true;
            }
        }

        public class AnotherClass
        {
            public AnotherClass(IFakeService fakeService)
            {
                FakeService = fakeService;
            }

            public IFakeService FakeService { get; }
        }

        public class ClassWithPrivateCtor
        {
            private ClassWithPrivateCtor()
            {
            }
        }

        private abstract class AbstractFoo
        {
            // The constructor should be public, since that is checked as well.
            public AbstractFoo()
            {
            }
        }

        private class Bar
        {
            public Bar()
            {
                throw new InvalidOperationException("some error");
            }
        }

        private class StaticConstructorClass
        {
            static StaticConstructorClass() { }

            private StaticConstructorClass() { }
        }

        public class ClassWithProtectedConstructor
        {
            internal ClassWithProtectedConstructor()
            {
            }
        }

        public class ClassWithInternalConstructor
        {
            internal ClassWithInternalConstructor()
            {
            }
        }

        public class ClassWithMultipleMarkedCtors
        {
            [ActivatorUtilitiesConstructor]
            public ClassWithMultipleMarkedCtors(string data)
            {
            }

            [ActivatorUtilitiesConstructor]
            public ClassWithMultipleMarkedCtors(IFakeService service, string data)
            {
            }
        }

        public class ClassWithAmbiguousCtorsAndAttribute
        {
            public ClassWithAmbiguousCtorsAndAttribute(string data)
            {
                CtorUsed = "string";
            }

            [ActivatorUtilitiesConstructor]
            public ClassWithAmbiguousCtorsAndAttribute(IFakeService service, string data)
            {
                CtorUsed = "IFakeService, string";
            }

            public ClassWithAmbiguousCtorsAndAttribute(IFakeService service, IFakeOuterService service2, string data)
            {
                CtorUsed = "IFakeService, IFakeService, string";
            }

            public string CtorUsed { get; set; }
        }

        public class CreationCountFakeService
        {
            public static readonly object InstanceLock = new object();

            public CreationCountFakeService(IFakeService dependency)
            {
                InstanceCount++;
                InstanceId = InstanceCount;
            }

            public static int InstanceCount { get; set; }

            public int InstanceId { get; }
        }

        public class ClassWithThrowingCtor
        {
            public ClassWithThrowingCtor(IFakeService service)
            {
                throw new Exception(nameof(ClassWithThrowingCtor));
            }
        }

        public class ClassWithThrowingEmptyCtor
        {
            public ClassWithThrowingEmptyCtor()
            {
                throw new Exception(nameof(ClassWithThrowingEmptyCtor));
            }
        }

        public class AnotherClassAcceptingData
        {
            public AnotherClassAcceptingData(IFakeService fakeService, string one, string two)
            {
                FakeService = fakeService;
                One = one;
                Two = two;
            }

            public IFakeService FakeService { get; }

            public string One { get; }

            public string Two { get; }
        }

        public class ClassWithOptionalArgsCtor
        {
            public ClassWithOptionalArgsCtor(string whatever = "BLARGH")
            {
                Whatever = whatever;
            }

            public string Whatever { get; set; }
        }

        public class ClassWithAmbiguousCtors
        {
            public ClassWithAmbiguousCtors(string data)
            {
                CtorUsed = "string";
            }

            public ClassWithAmbiguousCtors(IFakeService service, string data)
            {
                CtorUsed = "IFakeService, string";
            }

            public ClassWithAmbiguousCtors(IFakeService service, int data)
            {
                CtorUsed = "IFakeService, int";
            }

            public ClassWithAmbiguousCtors(IFakeService service, string data1, int data2)
            {
                FakeService = service;
                Data1 = data1;
                Data2 = data2;

                CtorUsed = "IFakeService, string, string";
            }

            public IFakeService FakeService { get; }

            public string Data1 { get; }

            public int Data2 { get; }
            public string CtorUsed { get; set; }
        }

        public class ClassWithOptionalArgsCtorWithStructs
        {
            public ConsoleColor? Color { get; }
            public ConsoleColor? ColorNull { get; }

            public int? Integer { get; }
            public int? IntegerNull { get; }

            // re-enable once https://github.com/dotnet/csharplang/issues/99 is implemented
            // see https://github.com/dotnet/runtime/issues/49069
            //public StructWithPublicDefaultConstructor StructWithConstructor { get; }

#pragma warning disable SA1129
            public ClassWithOptionalArgsCtorWithStructs(
                DateTime dateTime = new DateTime(),
                DateTime dateTimeDefault = default(DateTime),
                TimeSpan timeSpan = new TimeSpan(),
                TimeSpan timeSpanDefault = default(TimeSpan),
                DateTimeOffset dateTimeOffset = new DateTimeOffset(),
                DateTimeOffset dateTimeOffsetDefault = default(DateTimeOffset),
                Guid guid = new Guid(),
                Guid guidDefault = default(Guid),
                CustomStruct customStruct = new CustomStruct(),
                CustomStruct customStructDefault = default(CustomStruct),
                ConsoleColor? color = ConsoleColor.DarkGreen,
                ConsoleColor? colorNull = null,
                int? integer = 12,
                int? integerNull = null
            //StructWithPublicDefaultConstructor structWithConstructor = default
            )
#pragma warning restore SA1129
            {
                Color = color;
                ColorNull = colorNull;
                Integer = integer;
                IntegerNull = integerNull;
                //StructWithConstructor = structWithConstructor;
            }

            public struct CustomStruct { }
        }

        public class ClassWithStaticCtor
        {
            static ClassWithStaticCtor()
            {

            }
        }

        private class ServiceB { }



        private class TestServiceCollection : List<ServiceDescriptor>, IServiceCollection
        {
        }


        #endregion

        [Theory]
        [MemberData(nameof(_srKnownServiceActivators))]
        public void CreateInstance_WithoutDependencies(IServiceActivator activator)
        {
            ServiceProvider provider = new ServiceCollection().BuildServiceProvider();

            ServiceB serviceB = activator.CreateInstance(provider, typeof(ServiceB)) as ServiceB;

            Assert.NotNull(serviceB);
        }

        [Theory]
        [MemberData(nameof(_srKnownServiceActivators))]
        public void TypeActivatorEnablesYouToCreateAnyTypeWithServicesEvenWhenNotInIocContainer(IServiceActivator activator)
        {
            // Arrange
            var serviceCollection = new ServiceCollection()
                .AddTransient<IFakeService, FakeService>();

            var serviceProvider = serviceCollection.BuildServiceProvider();

            var anotherClass = activator.CreateInstance<AnotherClass>(serviceProvider);

            Assert.NotNull(anotherClass.FakeService);
        }

        [Theory]
        [MemberData(nameof(_srKnownServiceActivators))]
        public void TypeActivatorAcceptsAnyNumberOfAdditionalConstructorParametersToProvide(IServiceActivator activator)
        {
            // Arrange
            var serviceCollection = new ServiceCollection()
                .AddTransient<IFakeService, FakeService>();

            var serviceProvider = serviceCollection.BuildServiceProvider();

            // Act
            var anotherClass = activator.CreateInstance<AnotherClassAcceptingData>(serviceProvider, "1", "2");

            // Assert
            Assert.NotNull(anotherClass.FakeService);
            Assert.Equal("1", anotherClass.One);
            Assert.Equal("2", anotherClass.Two);

        }

        [Theory]
        [MemberData(nameof(_srKnownServiceActivators))]
        public void TypeActivatorWorksWithStaticCtor(IServiceActivator activator)
        {
            // Act
            var anotherClass = activator.CreateInstance<ClassWithStaticCtor>(provider: null);

            // Assert
            Assert.NotNull(anotherClass);

        }

        [Theory]
        [MemberData(nameof(_srKnownServiceActivators))]
        public void TypeActivatorWorksWithCtorWithOptionalArgs(IServiceActivator activator)
        {
            // Arrange
            var serviceCollection = new ServiceCollection();
            var serviceProvider = serviceCollection.BuildServiceProvider();

            // Act
            var anotherClass = activator.CreateInstance<ClassWithOptionalArgsCtor>(serviceProvider);

            // Assert
            Assert.NotNull(anotherClass);
            Assert.Equal("BLARGH", anotherClass.Whatever);
        }

        [Theory]
        [MemberData(nameof(_srKnownServiceActivators))]
        public void TypeActivatorWorksWithCtorWithOptionalArgs_WithStructDefaults(IServiceActivator activator)
        {
#if NETCOREAPP3_1_OR_GREATER
                // Arrange
                var serviceCollection = new ServiceCollection();
                var serviceProvider = serviceCollection.BuildServiceProvider();

                // Act
                var anotherClass = activator.CreateInstance<ClassWithOptionalArgsCtorWithStructs>(serviceProvider);

                // Assert
                Assert.NotNull(anotherClass);
                Assert.Equal(ConsoleColor.DarkGreen, anotherClass.Color);
                Assert.Null(anotherClass.ColorNull);
                Assert.Equal(12, anotherClass.Integer);
                Assert.Null(anotherClass.IntegerNull);
                // re-enable once https://github.com/dotnet/csharplang/issues/99 is implemented
                // see https://github.com/dotnet/runtime/issues/49069
                // Assert.Equal(ExpectStructWithPublicDefaultConstructorInvoked, anotherClass.StructWithConstructor.ConstructorInvoked);
#endif
        }

        [Theory]
        [MemberData(nameof(_srKnownServiceActivators))]
        public void TypeActivatorCanDisambiguateConstructorsWithUniqueArguments(IServiceActivator activator)
        {
            // Arrange
            var serviceCollection = new ServiceCollection()
                .AddTransient<IFakeService, FakeService>();
            var serviceProvider = serviceCollection.BuildServiceProvider();

            // Act
            var instance = activator.CreateInstance<ClassWithAmbiguousCtors>(serviceProvider, "1", 2);

            // Assert
            Assert.NotNull(instance);
            Assert.NotNull(instance.FakeService);
            Assert.Equal("1", instance.Data1);
            Assert.Equal(2, instance.Data2);
        }

        public delegate object CreateInstanceFunc(IServiceActivator serviceActivator, IServiceProvider provider, Type type, object[] args);

        public static IEnumerable<object[]> CreateInstanceFuncs
        {
            get
            {
                yield return new[] { (CreateInstanceFunc)((IServiceActivator sa, IServiceProvider sp, Type t, object[] args) => sa.CreateInstance(sp, t, args)) };
            }
        }
        public static IEnumerable<object[]> TypesWithNonPublicConstructorData =>
            CreateInstanceFuncs.Zip(
                    new[] { typeof(ClassWithPrivateCtor), typeof(ClassWithInternalConstructor), typeof(ClassWithProtectedConstructor), typeof(StaticConstructorClass) },
                    (a, b) => new object[] { a[0], b });

        [Theory]
        [MemberData(nameof(TypesWithNonPublicConstructorData))]
        public void TypeActivatorRequiresPublicConstructor(CreateInstanceFunc createFunc, Type type)
        {
            _sExecuteForAllKnownActivators(activator =>
            {
                // Arrange
                var expectedMessage = $"A suitable constructor for type '{type}' could not be located. " +
                    "Ensure the type is concrete and all parameters of a public constructor are either registered as services or passed as arguments. Also ensure no extraneous arguments are provided.";

                // Act and Assert
                var ex = Assert.Throws<InvalidOperationException>(() =>
                    createFunc(activator, provider: null, type: type, args: Array.Empty<object>()));
            });
        }

        [Theory]
        [MemberData(nameof(_srKnownServiceActivators))]
        public void TypeActivatorRequiresAllArgumentsCanBeAccepted(IServiceActivator activator)
        {
            // Arrange
            var expectedMessage = $"A suitable constructor for type '{typeof(AnotherClassAcceptingData).FullName}' could not be located. " +
                "Ensure the type is concrete and all parameters of a public constructor are either registered as services or passed as arguments. Also ensure no extraneous arguments are provided.";
            var serviceCollection = new TestServiceCollection()
                .AddTransient<IFakeService, FakeService>();
            var serviceProvider = serviceCollection.BuildServiceProvider();

            var ex1 = Assert.Throws<InvalidOperationException>(() =>
                activator.CreateInstance<AnotherClassAcceptingData>(serviceProvider, "1", "2", "3"));
            var ex2 = Assert.Throws<InvalidOperationException>(() =>
                activator.CreateInstance<AnotherClassAcceptingData>( serviceProvider, 1, 2));
        }

        [Theory]
        [MemberData(nameof(_srKnownServiceActivators))]
        public void TypeActivatorRethrowsOriginalExceptionFromConstructor(IServiceActivator activator)
        {
            // Act
            var ex1 = Assert.Throws<Exception>(() =>
                activator.CreateInstance<ClassWithThrowingEmptyCtor>(provider: null));

            var ex2 = Assert.Throws<Exception>(() =>
                activator.CreateInstance<ClassWithThrowingCtor>(provider: null, arguments: new[] { new FakeService() }));

            // Assert
            Assert.Equal(nameof(ClassWithThrowingEmptyCtor), ex1.Message);
            Assert.Equal(nameof(ClassWithThrowingCtor), ex2.Message);
        }


        [Theory]
        [InlineData("", "string")]
        [InlineData(5, "IFakeService, int")]
        public void TypeActivatorCreateInstanceUsesFirstMathchedConstructor(object value, string ctor)
        {
            _sExecuteForAllKnownActivators(activator =>
            {
                // Arrange
                var serviceCollection = new TestServiceCollection();
                serviceCollection.AddSingleton<IFakeService, FakeService>();
                var serviceProvider = serviceCollection.BuildServiceProvider();
                var type = typeof(ClassWithAmbiguousCtors);

                // Act
                var instance = activator.CreateInstance(serviceProvider, type, value);

                // Assert
                Assert.Equal(ctor, ((ClassWithAmbiguousCtors)instance).CtorUsed);
            });
        }

        [Theory]
        [MemberData(nameof(_srKnownServiceActivators))]
        public void TypeActivatorUsesMarkedConstructor(IServiceActivator activator)
        {
            // Arrange
            var serviceCollection = new TestServiceCollection();
            serviceCollection.AddSingleton<IFakeService, FakeService>();
            var serviceProvider = serviceCollection.BuildServiceProvider();

            // Act
            var instance = activator.CreateInstance<ClassWithAmbiguousCtorsAndAttribute>(serviceProvider, "hello");

            // Assert
            Assert.Equal("IFakeService, string", instance.CtorUsed);
        }

        [Theory]
        [MemberData(nameof(_srKnownServiceActivators))]
        public void TypeActivatorThrowsOnMultipleMarkedCtors(IServiceActivator activator)
        {
            // Act
            var exception = Assert.Throws<InvalidOperationException>(() => activator.CreateInstance<ClassWithMultipleMarkedCtors>( null, "hello"));

            // Assert
            Assert.Equal("Multiple constructors were marked with ActivatorUtilitiesConstructorAttribute.", exception.Message);
        }

        [Theory]
        [MemberData(nameof(_srKnownServiceActivators))]
        public void TypeActivatorThrowsWhenMarkedCtorDoesntAcceptArguments(IServiceActivator activator)
        {
            // Act
            var exception = Assert.Throws<InvalidOperationException>(() => activator.CreateInstance<ClassWithAmbiguousCtorsAndAttribute>( null, 0, "hello"));

            // Assert
            Assert.Equal("Constructor marked with ActivatorUtilitiesConstructorAttribute does not accept all given argument types.", exception.Message);
        }

        [Theory]
        [MemberData(nameof(_srKnownServiceActivators))]
        public void GetServiceOrCreateInstanceRegisteredServiceTransient(IServiceActivator activator)
        {
            // Reset the count because test order is not guaranteed
            lock (CreationCountFakeService.InstanceLock)
            {
                CreationCountFakeService.InstanceCount = 0;

                var serviceCollection = new TestServiceCollection()
                    .AddTransient<IFakeService, FakeService>()
                    .AddTransient<CreationCountFakeService>();

                var serviceProvider = serviceCollection.BuildServiceProvider();

                var service = activator.GetServiceOrCreateInstance<CreationCountFakeService>(serviceProvider);
                Assert.NotNull(service);
                Assert.Equal(1, service.InstanceId);
                Assert.Equal(1, CreationCountFakeService.InstanceCount);

                service = ActivatorUtilities.GetServiceOrCreateInstance<CreationCountFakeService>(serviceProvider);
                Assert.NotNull(service);
                Assert.Equal(2, service.InstanceId);
                Assert.Equal(2, CreationCountFakeService.InstanceCount);
            }
        }

        [Theory]
        [MemberData(nameof(_srKnownServiceActivators))]
        public void GetServiceOrCreateInstanceRegisteredServiceSingleton(IServiceActivator activator)
        {
            lock (CreationCountFakeService.InstanceLock)
            {
                // Arrange
                // Reset the count because test order is not guaranteed
                CreationCountFakeService.InstanceCount = 0;

                var serviceCollection = new TestServiceCollection()
                    .AddTransient<IFakeService, FakeService>()
                    .AddSingleton<CreationCountFakeService>();
                var serviceProvider = serviceCollection.BuildServiceProvider();

                // Act and Assert
                var service = activator.GetServiceOrCreateInstance<CreationCountFakeService>(serviceProvider);
                Assert.NotNull(service);
                Assert.Equal(1, service.InstanceId);
                Assert.Equal(1, CreationCountFakeService.InstanceCount);

                service = ActivatorUtilities.GetServiceOrCreateInstance<CreationCountFakeService>(serviceProvider);
                Assert.NotNull(service);
                Assert.Equal(1, service.InstanceId);
                Assert.Equal(1, CreationCountFakeService.InstanceCount);
            }
        }

        [Theory]
        [MemberData(nameof(_srKnownServiceActivators))]
        public void GetServiceOrCreateInstanceUnregisteredService(IServiceActivator activator)
        {
            lock (CreationCountFakeService.InstanceLock)
            {
                // Arrange
                // Reset the count because test order is not guaranteed
                CreationCountFakeService.InstanceCount = 0;

                var serviceCollection = new TestServiceCollection()
                    .AddTransient<IFakeService, FakeService>();
                var serviceProvider = serviceCollection.BuildServiceProvider();

                // Act and Assert
                var service = (CreationCountFakeService)activator.GetServiceOrCreateInstance(
                    serviceProvider,
                    typeof(CreationCountFakeService));
                Assert.NotNull(service);
                Assert.Equal(1, service.InstanceId);
                Assert.Equal(1, CreationCountFakeService.InstanceCount);

                service = activator.GetServiceOrCreateInstance<CreationCountFakeService>(serviceProvider);
                Assert.NotNull(service);
                Assert.Equal(2, service.InstanceId);
                Assert.Equal(2, CreationCountFakeService.InstanceCount);
            }
        }

        [Theory]
        [MemberData(nameof(_srKnownServiceActivators))]
        public void UnRegisteredServiceAsConstructorParameterThrowsException(IServiceActivator activator)
        {
            var serviceCollection = new TestServiceCollection()
                .AddSingleton<CreationCountFakeService>();
            var serviceProvider = serviceCollection.BuildServiceProvider();

            var ex = Assert.Throws<InvalidOperationException>(() =>
                activator.CreateInstance<CreationCountFakeService>(serviceProvider));
            Assert.Equal($"Unable to resolve service for type '{typeof(IFakeService)}' while attempting" +
                $" to activate '{typeof(CreationCountFakeService)}'.",
                ex.Message);
        }

        [Theory]
        [MemberData(nameof(_srKnownServiceActivators))]
        public void CreateInstance_WithAbstractTypeAndPublicConstructor_ThrowsCorrectException(IServiceActivator activator)
        {
            // Act & Assert
            var ex = Assert.Throws<InvalidOperationException>(() => activator.CreateInstance(default(IServiceProvider), typeof(AbstractFoo)));
        }

        [Theory]
        [MemberData(nameof(_srKnownServiceActivators))]
        public void CreateInstance_CapturesInnerException_OfTargetInvocationException(IServiceActivator activator)
        {
            // Act & Assert
            var ex = Assert.Throws<InvalidOperationException>(() => activator.CreateInstance(default(IServiceProvider), typeof(Bar)));
            var msg = "some error";
            Assert.Equal(msg, ex.Message);
        }

        #endregion

    }
}
