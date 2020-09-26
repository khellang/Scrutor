using Microsoft.Extensions.DependencyInjection;

namespace Scrutor.Static
{
    public interface ICompiledLifetimeSelector : ICompiledServiceTypeSelector
    {
        /// <summary>
        /// Registers each matching concrete type with <see cref="ServiceLifetime.Singleton"/> lifetime.
        /// </summary>
        void WithSingletonLifetime();

        /// <summary>
        /// Registers each matching concrete type with <see cref="ServiceLifetime.Scoped"/> lifetime.
        /// </summary>
        void WithScopedLifetime();

        /// <summary>
        /// Registers each matching concrete type with <see cref="ServiceLifetime.Transient"/> lifetime.
        /// </summary>
        void WithTransientLifetime();

        /// <summary>
        /// Registers each matching concrete type with the specified <paramref name="lifetime"/>.
        /// </summary>
        void WithLifetime(ServiceLifetime lifetime);
    }
}
