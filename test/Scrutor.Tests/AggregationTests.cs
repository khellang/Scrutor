using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Scrutor.Tests
{
    public class AggregationTests: TestBase
    {
        [Fact]
        public void CanAggregateType()
        {
            var provider = ConfigureProvider(services =>
            {
                services.AddTransient<ICompositeService, LeafServiceA>();
                services.AddTransient<ICompositeService, LeafServiceA>();
                services.Aggregate<ICompositeService, CompositeService>();
            });

            var instance = provider.GetRequiredService<ICompositeService>();

            var aggregator = Assert.IsType<CompositeService>(instance);

            Assert.Equal(2, aggregator.AggregatedServices.Count());
            Assert.All(aggregator.AggregatedServices, injectedService => Assert.IsType<LeafServiceA>(injectedService));
        }

        [Fact]
        public void CanAggregateDifferentServices()
        {
            var provider = ConfigureProvider(services =>
            {
                services.AddTransient<ICompositeService, LeafServiceA>();
                services.AddTransient<ICompositeService, LeafServiceB>();
                services.Aggregate<ICompositeService, CompositeService>();
            });

            var instance = provider.GetRequiredService<ICompositeService>();

            var aggregator = Assert.IsType<CompositeService>(instance);

            Assert.Collection(aggregator.AggregatedServices,
                injectedService => Assert.IsType<LeafServiceA>(injectedService),
                injectedService => Assert.IsType<LeafServiceB>(injectedService));
        }

        [Fact]
        public void CanAggregateExistingInstances()
        {
            var existingA = new LeafServiceA();
            var existingB = new LeafServiceB();

            var provider = ConfigureProvider(services =>
            {
                services.AddSingleton<ICompositeService>(existingA);
                services.AddSingleton<ICompositeService>(existingB);
                services.Aggregate<ICompositeService, CompositeService>();
            });

            var instance = provider.GetRequiredService<ICompositeService>();

            var aggregator = Assert.IsType<CompositeService>(instance);

            Assert.Collection(aggregator.AggregatedServices,
                injectedService => Assert.Same(existingA, injectedService),
                injectedService => Assert.Same(existingB, injectedService));
        }

        [Fact]
        public void CanInjectServicesIntoAggregatedType()
        {
            var provider = ConfigureProvider(services =>
            {
                services.AddSingleton<IService, SomeRandomService>();
                services.AddTransient<ICompositeService, LeafServiceA>();
                services.AddTransient<ICompositeService, LeafServiceB>();
                services.Aggregate<ICompositeService, CompositeService>();
            });

            var validator = provider.GetRequiredService<IService>();

            var instance = provider.GetRequiredService<ICompositeService>();

            var aggregator = Assert.IsType<CompositeService>(instance);

            Assert.Collection(aggregator.AggregatedServices,
                injectedService =>
                {
                    var leafA = Assert.IsType<LeafServiceA>(injectedService);
                    Assert.Same(validator, leafA.InjectedService);
                },
                injectedService =>
                {
                    var leafB = Assert.IsType<LeafServiceB>(injectedService);
                    Assert.Same(validator, leafB.InjectedService);
                });
        }

        [Fact]
        public void CanInjectServicesIntoAggregatingType()
        {
            var provider = ConfigureProvider(services =>
            {
                services.AddSingleton<IService, SomeRandomService>();
                services.AddTransient<ICompositeService, LeafServiceA>();
                services.AddTransient<ICompositeService, LeafServiceB>();
                services.Aggregate<ICompositeService, CompositeService>();
            });

            var validator = provider.GetRequiredService<IService>();

            var instance = provider.GetRequiredService<ICompositeService>();

            var aggregator = Assert.IsType<CompositeService>(instance);

            Assert.Same(validator, aggregator.InjectedService);
        }

        [Fact]
        public void DecoratingNonRegisteredServiceThrows()
        {
            Assert.Throws<MissingTypeRegistrationException>(() => ConfigureProvider(services => services.Decorate<ICompositeService, CompositeService>()));
        }

        public interface IService { }

        private class SomeRandomService : IService { }

        public interface ICompositeService { }

        private class LeafServiceA : ICompositeService
        {
            public LeafServiceA(IService injectedService = null)
            {
                InjectedService = injectedService;
            }

            public IService InjectedService { get; }
        }

        private class LeafServiceB : ICompositeService
        {
            public LeafServiceB(IService injectedService = null)
            {
                InjectedService = injectedService;
            }

            public IService InjectedService { get; }
        }

        private class CompositeService : ICompositeService
        {
            public CompositeService(IEnumerable<ICompositeService> aggregatedServices, IService injectedService = null)
            {
                AggregatedServices = aggregatedServices;
                InjectedService = injectedService;
            }

            public IEnumerable<ICompositeService> AggregatedServices { get; }
            public IService InjectedService { get; }
        }
    }
}
