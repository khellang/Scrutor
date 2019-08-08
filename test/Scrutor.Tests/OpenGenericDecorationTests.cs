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
        public void CanDecorateOpenGenericTypeBasedOnGrandparentInterface()
        {
            var provider = ConfigureProvider(services =>
            {
                services.AddSingleton<ISpecializedQueryHandler, MySpecializedQueryHandler>();
                services.AddSingleton<IQueryHandler<MyQuery, MyResult>, MySpecializedQueryHandler>();
                services.Decorate(typeof(IQueryHandler<,>), typeof(LoggingQueryHandler<,>));
            });

            var instance = provider.GetRequiredService<IQueryHandler<MyQuery, MyResult>>();

            var loggingDecorator = Assert.IsType<LoggingQueryHandler<MyQuery, MyResult>>(instance);
            Assert.IsType<MySpecializedQueryHandler>(loggingDecorator.Inner);
        }

        [Fact]
        public void DecoratingOpenGenericTypeBasedOnGrandparentInterfaceDoesNotDecorateParentInterface()
        {
            var provider = ConfigureProvider(services =>
            {
                services.AddSingleton<ISpecializedQueryHandler, MySpecializedQueryHandler>();
                services.AddSingleton<IQueryHandler<MyQuery, MyResult>, MySpecializedQueryHandler>();
                services.Decorate(typeof(IQueryHandler<,>), typeof(LoggingQueryHandler<,>));
            });

            var instance = provider.GetRequiredService<ISpecializedQueryHandler>();

            Assert.IsType<MySpecializedQueryHandler>(instance);
        }

        [Fact]
        public void OpenGenericDecoratorsSkipOpenGenericServiceRegistrations()
        {
            var provider = ConfigureProvider(services =>
            {
                services.Scan(x =>
                    x.FromAssemblyOf<Message>()
                        .AddClasses(classes => classes
                            .AssignableTo(typeof(IMessageProcessor<>)))
                        .AsImplementedInterfaces()
                        .WithTransientLifetime());

                services.Decorate(typeof(IMessageProcessor<>), typeof(GenericDecorator<>));
            });

            var processor = provider.GetRequiredService<IMessageProcessor<Message>>();

            var decorator = Assert.IsType<GenericDecorator<Message>>(processor);

            Assert.IsType<MessageProcessor>(decorator.Decoratee);
        }
    }

    // ReSharper disable UnusedTypeParameter

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

    public interface ISpecializedQueryHandler : IQueryHandler<MyQuery, MyResult> { }

    public class MySpecializedQueryHandler : ISpecializedQueryHandler { }

    public interface IMessageProcessor<T> { }

    public class Message { }

    public class MessageProcessor : IMessageProcessor<Message> { }

    public class GenericDecorator<T> : IMessageProcessor<T>
    {
        public GenericDecorator(IMessageProcessor<T> decoratee)
        {
            Decoratee = decoratee;
        }

        public IMessageProcessor<T> Decoratee { get; }
    }
}
