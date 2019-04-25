using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Scrutor.Tests
{
    public class OpenGenericAggregationTests: TestBase
    {
        [Fact]
        public void CanAggregateOpenGenericBasedOnClass()
        {
            var provider = ConfigureProvider(services =>
            {
                services.AddTransient<Validator<Entity1>, Entity1ValidatorA>();
                services.AddTransient<Validator<Entity1>, Entity1ValidatorB>();
                services.Aggregate(typeof(Validator<>), typeof(CompositeValidator<>));
            });

            var instance = provider.GetRequiredService<Validator<Entity1>>();

            var aggregator = Assert.IsType<CompositeValidator<Entity1>>(instance);
            Assert.Collection(aggregator.Validators,
                validator => Assert.IsType<Entity1ValidatorA>(validator),
                validator => Assert.IsType<Entity1ValidatorB>(validator));
        }

        [Fact]
        public void CanAggregateOpenGenericBasedOnInterface()
        {
            var provider = ConfigureProvider(services =>
            {
                services.AddTransient<IValidator<Entity1>, Entity1ValidatorA>();
                services.AddTransient<IValidator<Entity1>, Entity1ValidatorB>();
                services.Aggregate(typeof(IValidator<>), typeof(CompositeValidator<>));
            });

            var instance = provider.GetRequiredService<IValidator<Entity1>>();

            var aggregator = Assert.IsType<CompositeValidator<Entity1>>(instance);
            Assert.Collection(aggregator.Validators,
                validator => Assert.IsType<Entity1ValidatorA>(validator),
                validator => Assert.IsType<Entity1ValidatorB>(validator));
        }

        [Fact]
        public void CanAggregateOpenGenericOfProperTypeArgument()
        {
            var provider = ConfigureProvider(services =>
            {
                services.AddTransient<IValidator<Entity1>, Entity1ValidatorA>();
                services.AddTransient<IValidator<Entity1>, Entity1ValidatorB>();
                services.AddTransient<IValidator<Entity2>, Entity2Validator>();
                services.Aggregate(typeof(IValidator<>), typeof(CompositeValidator<>));
            });

            var instance = provider.GetRequiredService<IValidator<Entity1>>();

            var aggregator = Assert.IsType<CompositeValidator<Entity1>>(instance);
            Assert.All(aggregator.Validators, Assert.IsNotType<Entity2Validator>);
        }

        [Fact]
        public void CanAggregateOpenGenericsWithClosedGenerics()
        {
            var provider = ConfigureProvider(services =>
            {
                services.AddTransient<IValidator<Entity1>, Entity1ValidatorA>();
                services.AddTransient<IValidator<Entity1>, Entity1ValidatorB>();
                services.AddTransient(typeof(IValidator<>), typeof(Validator<>));
                services.Aggregate(typeof(IValidator<>), typeof(CompositeValidator<>));
            });

            var instance = provider.GetRequiredService<IValidator<Entity1>>();

            var aggregator = Assert.IsType<CompositeValidator<Entity1>>(instance);
            Assert.Collection(aggregator.Validators,
                validator => Assert.IsType<Entity1ValidatorA>(validator),
                validator => Assert.IsType<Entity1ValidatorB>(validator),
                validator => Assert.IsType<Validator<Entity1>>(validator));
        }

        //[Fact]
        //public void CanAggregateClosedGenericsOnly()
        //{
        //    var provider = ConfigureProvider(services =>
        //    {
        //        services.AddTransient(typeof(IValidator<>), typeof(Validator<>));
        //        services.AddTransient(typeof(IValidator<>), typeof(OtherValidator<>));
        //        services.Aggregate(typeof(IValidator<>), typeof(CompositeValidator<>));
        //    });

        //    var instance = provider.GetRequiredService<IValidator<Entity1>>();

        //    var aggregator = Assert.IsType<CompositeValidator<Entity1>>(instance);
        //    Assert.Collection(aggregator.Validators,
        //        validator => Assert.IsType<Validator<Entity1>>(validator),
        //        validator => Assert.IsType<OtherValidator<Entity1>>(validator));
        //}

        [Fact]
        public void AggregatingNonRegisteredOpenGenericServiceThrows()
        {
            Assert.Throws<MissingTypeRegistrationException>(() => ConfigureProvider(services => services.Aggregate(typeof(IValidator<>), typeof(CompositeValidator<>))));
        }

        public interface IValidator<T> { }

        private class Validator<T> : IValidator<T> { }

        private class OtherValidator<T> : IValidator<T> { }

        private class Entity1 { }

        private class Entity2 { }

        private class Entity1ValidatorA : Validator<Entity1>
        {
        }

        private class Entity1ValidatorB : Validator<Entity1>
        {
        }

        private class Entity2Validator : Validator<Entity2>
        {
        }

        private class CompositeValidator<T> : Validator<T>
        {
            public CompositeValidator(IEnumerable<IValidator<T>> validators)
            {
                Validators = validators;
            }

            public IEnumerable<IValidator<T>> Validators { get; }
        }
    }
}
