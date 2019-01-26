using System;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Scrutor.Tests
{
    public class OpenGenericDecorationTests : TestBase
    {
        [Fact]
        public void CanDecorateOpenGenericTypeBasedOnClass()
        {
            var provider = ConfigureProvider(services =>
            {
                services.AddSingleton<QueryHandler<MyQuery, MyResult>, MyQueryHandler>();
                services.Decorate(typeof(QueryHandler<,>), typeof(LoggingQueryHandler<,>));
                services.Decorate(typeof(QueryHandler<,>), typeof(TelemetryQueryHandler<,>));
            });

            var instance = provider.GetRequiredService<QueryHandler<MyQuery, MyResult>>();

            var telemetryDecorator = Assert.IsType<TelemetryQueryHandler<MyQuery, MyResult>>(instance);
            var loggingDecorator = Assert.IsType<LoggingQueryHandler<MyQuery, MyResult>>(telemetryDecorator.Inner);
            Assert.IsType<MyQueryHandler>(loggingDecorator.Inner);
        }


        [Fact]
        public void CanDecorateOpenGenericTypeBasedOnInterface()
        {
            var provider = ConfigureProvider(services =>
            {
                services.AddSingleton<IQueryHandler<MyQuery,MyResult>, MyQueryHandler>();
                services.Decorate(typeof(IQueryHandler<,>), typeof(LoggingQueryHandler<,>));
                services.Decorate(typeof(IQueryHandler<,>), typeof(TelemetryQueryHandler<,>));
            });

            var instance = provider.GetRequiredService<IQueryHandler<MyQuery, MyResult>>();

            var telemetryDecorator = Assert.IsType<TelemetryQueryHandler<MyQuery, MyResult>>(instance);
            var loggingDecorator = Assert.IsType<LoggingQueryHandler<MyQuery, MyResult>>(telemetryDecorator.Inner);
            Assert.IsType<MyQueryHandler>(loggingDecorator.Inner);
        }

        [Fact]
        public void DecoratingNonRegisteredOpenGenericServiceThrows()
        {
            Assert.Throws<MissingTypeRegistrationException>(() => ConfigureProvider(services => services.Decorate(typeof(IQueryHandler<,>), typeof(QueryHandler<,>))));
        }

        [Fact]
        public void CheckLifetimeOfDecorateSingleton()
        {
            var services = new ServiceCollection();
            
            services.AddSingleton<IQueryHandler<MyQuery, MyResult>, MyQueryHandler>();
            services.DecorateSingleton(typeof(IQueryHandler<,>), typeof(LoggingQueryHandler<,>));

            var descriptor = services.GetDescriptor<IQueryHandler<MyQuery, MyResult>>();

            Assert.Equal(ServiceLifetime.Singleton, descriptor.Lifetime);
        }

        [Fact]
        public void CheckLifetimeOfDecorateScoped()
        {
            var services = new ServiceCollection();

            services.AddSingleton<IQueryHandler<MyQuery, MyResult>, MyQueryHandler>();
            services.DecorateScoped(typeof(IQueryHandler<,>), typeof(LoggingQueryHandler<,>));

            var descriptor = services.GetDescriptor<IQueryHandler<MyQuery, MyResult>>();

            Assert.Equal(ServiceLifetime.Scoped, descriptor.Lifetime);
        }

        [Fact]
        public void CheckLifetimeOfDecorateTransient()
        {
            var services = new ServiceCollection();

            services.AddSingleton<IQueryHandler<MyQuery, MyResult>, MyQueryHandler>();
            services.DecorateTransient(typeof(IQueryHandler<,>), typeof(LoggingQueryHandler<,>));

            var descriptor = services.GetDescriptor<IQueryHandler<MyQuery, MyResult>>();

            Assert.Equal(ServiceLifetime.Transient, descriptor.Lifetime);
        }

        [Fact]
        public void IsSameLifetimeOfDecoratorAndDecorated()
        {
            var services = new ServiceCollection();

            services.AddSingleton<IQueryHandler<MyQuery, MyResult>, MyQueryHandler>();

            var decoratedDescriptor = services.GetDescriptor<IQueryHandler<MyQuery, MyResult>>();

            services.Decorate(typeof(IQueryHandler<,>), typeof(LoggingQueryHandler<,>));

            var decoratorDescriptor = services.GetDescriptor<IQueryHandler<MyQuery, MyResult>>();

            Assert.Equal(decoratedDescriptor.Lifetime, decoratorDescriptor.Lifetime);
        }
    }

    public class MyQuery { }

    public class MyResult { }

    public class MyQueryHandler : QueryHandler<MyQuery, MyResult> { }

    public class QueryHandler<TQuery, TResult> : IQueryHandler<TQuery, TResult> { }

    public class LoggingQueryHandler<TQuery, TResult> : DecoratorQueryHandler<TQuery, TResult>
    {
        public LoggingQueryHandler(IQueryHandler<TQuery, TResult> inner) : base(inner) { }
    }

    public class TelemetryQueryHandler<TQuery, TResult> : DecoratorQueryHandler<TQuery, TResult>
    {
        public TelemetryQueryHandler(IQueryHandler<TQuery, TResult> inner) : base(inner) { }
    }

    public class DecoratorQueryHandler<TQuery, TResult> : QueryHandler<TQuery, TResult>, IDecoratorQueryHandler<TQuery, TResult>
    {
        public DecoratorQueryHandler(IQueryHandler<TQuery, TResult> inner)
        {
            Inner = inner;
        }

        public IQueryHandler<TQuery, TResult> Inner { get; }
    }

    public interface IDecoratorQueryHandler<TQuery, TResult> : IQueryHandler<TQuery, TResult>
    {
        IQueryHandler<TQuery, TResult> Inner { get; }
    }
}
