using Microsoft.Extensions.DependencyInjection;

using Scrutor.Activation;

using System;

using Xunit;

using static Scrutor.Tests.ScrutorContextTests;

namespace Scrutor.Tests
{
    public class ScrutorContextTests : TestBase
    {
        #region Fakes

        public interface IDecorable1
        {
        }

        public interface IDecorable2
        {
        }

        public interface IDecorable3
        { 
        }

        public class Decorable : IDecorable1, IDecorable2, IDecorable3
        {
        }

        public interface IFakeService
        {
        }

        public class FakeService : IFakeService
        {
        }

        public interface INotRegistered
        {
        }


        public class ClassWithOptionalDependency1 : IDecorable1
        {
            public ClassWithOptionalDependency1(IDecorable1 inner, IFakeService fakeService)
                : this(inner, fakeService, null)
            {
            }

            public ClassWithOptionalDependency1(IDecorable1 inner, IFakeService fakeService, INotRegistered notRegistered)
            {
                FakeService = fakeService;

                Inner = inner;
                NotRegistered = notRegistered;
            }

            public IDecorable1 Inner { get; }

            public IFakeService FakeService { get; }

            public INotRegistered NotRegistered { get; }
        }

        public class ClassWithOptionalDependency2 : IDecorable2
        {
            public ClassWithOptionalDependency2(IDecorable2 inner, IFakeService fakeService)
                : this(inner, fakeService, null)
            {
            }

            public ClassWithOptionalDependency2(IDecorable2 inner, IFakeService fakeService, INotRegistered notRegistered)
            {
                FakeService = fakeService;
                
                Inner = inner;
                NotRegistered = notRegistered;
            }

            public IDecorable2 Inner { get; }

            public IFakeService FakeService { get; }

            public INotRegistered NotRegistered { get; }
        }

        public class ClassWithOptionalDependency3 : IDecorable3
        {
            public ClassWithOptionalDependency3(IDecorable3 inner, IFakeService fakeService)
                : this(inner, fakeService, null)
            {
            }

            public ClassWithOptionalDependency3(IDecorable3 inner, IFakeService fakeService, INotRegistered notRegistered)
            {
                FakeService = fakeService;

                Inner = inner;
                NotRegistered = notRegistered;
            }

            public IDecorable3 Inner { get; }

            public IFakeService FakeService { get; }

            public INotRegistered NotRegistered { get; }
        }

        #endregion

        [Fact]
        public void DecorationDependsFromCurrentScrutorContext()
        {
            var serviceCollection = new ServiceCollection();

            var serviceProvider = serviceCollection
                .Scrutor()
                    .AddTransient<IFakeService, FakeService>()
                    .AddTransient<IDecorable1, Decorable>()
                    .AddTransient<IDecorable2, Decorable>()
                    .AddTransient<IDecorable3, Decorable>()
                    .Decorate<IDecorable1, ClassWithOptionalDependency1>()
                    .Scrutor()
                        .UseServiceActivator(new DefaultServiceActivator())
                        .Decorate<IDecorable2, ClassWithOptionalDependency2>()
                        .Scrutor()
                            .UseServiceActivator(new ScrutorServiceActivator(useFallbacks: true))
                            .Decorate<IDecorable3, ClassWithOptionalDependency3>()        
                    .BuildServiceProvider();

            // Will throw because was registered with ScrutorServiceActivator without UseFallbacks.
            Assert.Throws<InvalidOperationException>(() => serviceProvider.GetService<IDecorable1>());

            // Will not throw since there DefaultServiceActivator
            var resolved = serviceProvider.GetService<IDecorable2>();

            Assert.IsAssignableFrom<ClassWithOptionalDependency2>(resolved);
            Assert.Null((resolved as ClassWithOptionalDependency2).NotRegistered);

            var resolved2 = serviceProvider.GetService<IDecorable3>();
            Assert.IsAssignableFrom<ClassWithOptionalDependency3>(resolved2);
            Assert.Null((resolved2 as ClassWithOptionalDependency3).NotRegistered);
        }

        [Fact]
        public void UsagesOfMultipleContexts()
        {
            var sp1 = new ServiceCollection()
                .Scrutor()
                    .AddTransient<IFakeService, FakeService>()
                    .AddTransient<IDecorable1, Decorable>()
                    .Decorate<IDecorable1, ClassWithOptionalDependency1>()
                .BuildServiceProvider();

            var sp2 = new ServiceCollection()
                .Scrutor()
                    .UseServiceActivator(new DefaultServiceActivator())
                    .AddTransient<IFakeService, FakeService>()
                    .AddTransient<IDecorable1, Decorable>()
                    .Decorate<IDecorable1, ClassWithOptionalDependency1>()
                .BuildServiceProvider();

            // Will throw because was registered with ScrutorServiceActivator without UseFallbacks.
            Assert.Throws<InvalidOperationException>(() => sp1.GetService<IDecorable1>());

            // Will not throw since there DefaultServiceActivator
            var resolved = sp2.GetService<IDecorable1>();
            Assert.IsAssignableFrom<ClassWithOptionalDependency1>(resolved);
            Assert.Null((resolved as ClassWithOptionalDependency1).NotRegistered);
        }
    }
}
