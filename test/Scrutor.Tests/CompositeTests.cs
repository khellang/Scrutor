using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using System.Linq;

namespace Scrutor.Tests
{
    public class CompositeTests : TestBase
    {
        [Fact]
        public void CanComposeTypes()
        {
            var provider = ConfigureProvider(services =>
            {
                services.AddSingleton<IService, Service1>();
                services.AddSingleton<IService, Service2>();

                services.AddComposite<IService, Composite>(ServiceLifetime.Transient);
            });

            var instance = provider.GetRequiredService<IService>();

            var composite = Assert.IsType<Composite>(instance);

            Assert.Equal("one,two", composite.GetMessage());
            Assert.IsType<Service1>(composite.ConcreteServices[0]);
            Assert.IsType<Service2>(composite.ConcreteServices[1]);
        }

        [Fact]
        public void CanComposeWhenNoTypesRegistered()
        {
            var provider = ConfigureProvider(services =>
            {
                services.AddComposite<IService, Composite>(ServiceLifetime.Transient);
            });

            var instance = provider.GetRequiredService<IService>();

            var composite = Assert.IsType<Composite>(instance);

            Assert.Equal(string.Empty, composite.GetMessage());
            Assert.Equal(0, composite.ConcreteServices.Count);
        }

        [Fact]
        public void CompositeTypeCanHaveOtherInjectedDependencies()
        {
            var provider = ConfigureProvider(services =>
            {
                services.AddSingleton<IService, Service1>();
                services.AddSingleton<IService, Service2>();
                services.AddSingleton<IOtherDependency, OtherDependency>();

                services.AddComposite<IService, CompositeWithOtherDependency>(ServiceLifetime.Transient);
            });

            var instance = provider.GetRequiredService<IService>();

            var composite = Assert.IsType<CompositeWithOtherDependency>(instance);

            Assert.Equal("Prefix: one,two", composite.GetMessage());
            Assert.IsType<Service1>(composite.ConcreteServices[0]);
            Assert.IsType<Service2>(composite.ConcreteServices[1]);
        }

        [Fact]
        public void CanComposeExtensionsOfComposedInterface()
        {
            var provider = ConfigureProvider(services =>
            {
                services.AddSingleton<IService, Service1>();
                services.AddSingleton<IExtendedService, ExtendedService1>();

                services.AddComposite<IService, Composite>(ServiceLifetime.Transient);
            });

            var instance = provider.GetRequiredService<IService>();

            var composite = Assert.IsType<Composite>(instance);

            Assert.Equal("one,extended", composite.GetMessage());
            Assert.IsType<Service1>(composite.ConcreteServices[0]);
            Assert.IsType<ExtendedService1>(composite.ConcreteServices[1]);
        }

        interface IService
        {
            string GetMessage();
        }

        interface IExtendedService : IService
        {
            string GetAnotherMessage();
        }


        class Service1 : IService
        {
            public string GetMessage() => "one";
        }

        class Service2 : IService
        {
            public string GetMessage() => "two";
        }

        class ExtendedService1 : IExtendedService
        {
            public string GetAnotherMessage() => "extended";
            public string GetMessage() => "extended";
        }

        class Composite : IService
        {
            public Composite(IEnumerable<IService> concreteServices)
            {
                this.ConcreteServices = concreteServices.ToList();
            }

            public IList<IService> ConcreteServices { get; }

            public virtual string GetMessage() => string.Join(",",
                this.ConcreteServices.Select(s => s.GetMessage()));
        }

        interface IOtherDependency
        {
            string GetPrefix();
        }

        class OtherDependency : IOtherDependency
        {
            public string GetPrefix() => "Prefix:";
        }

        class CompositeWithOtherDependency : Composite
        {
            private readonly IOtherDependency otherDependency;

            public CompositeWithOtherDependency(IEnumerable<IService> concreteServices, IOtherDependency otherDependency)
                : base(concreteServices)
            {
                this.otherDependency = otherDependency;
            }

            public override string GetMessage() => $"{this.otherDependency.GetPrefix()} {base.GetMessage()}";
        }
    }
}
