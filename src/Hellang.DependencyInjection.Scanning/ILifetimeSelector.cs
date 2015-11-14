using Microsoft.Framework.DependencyInjection;

namespace Microsoft.Extensions.DependencyInjection.Scanning
{
    public interface ILifetimeSelector : IFluentInterface
    {
        void WithSingletonLifetime();

        void WithScopedLifetime();

        void WithTransientLifetime();

        void WithLifetime(ServiceLifetime lifetime);
    }
}