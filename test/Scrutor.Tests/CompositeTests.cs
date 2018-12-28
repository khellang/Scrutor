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

                services.AddComposite<IService, Composite>();
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
                services.AddComposite<IService, Composite>();
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

                services.AddComposite<IService, CompositeWithOtherDependency>();
            });

            var instance = provider.GetRequiredService<IService>();

            var composite = Assert.IsType<CompositeWithOtherDependency>(instance);

            Assert.Equal("Prefix: one,two", composite.GetMessage());
            Assert.IsType<Service1>(composite.ConcreteServices[0]);
            Assert.IsType<Service2>(composite.ConcreteServices[1]);
        }

        [Fact]
        public void CanComposeMultipleLifetimeScopes()
        {
            var services = new ServiceCollection();
            services.AddScoped<IService, Service1>();
            services.AddTransient<IService, Service2>();

            services.AddComposite<IService, Composite>();

            var descriptor = services.GetDescriptor<IService>();
            Assert.Equal(ServiceLifetime.Transient, descriptor.Lifetime);
        }

        interface IService
        {
            string GetMessage();
        }

        class Service1 : IService
        {
            public string GetMessage() => "one";
        }

        class Service2 : IService
        {
            public string GetMessage() => "two";
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
