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
