namespace Scrutor.Generators.Models;

/// <summary>
/// Mirrors Microsoft.Extensions.DependencyInjection.ServiceLifetime for source generation.
/// </summary>
internal enum ServiceLifetime
{
    Singleton = 0,
    Scoped = 1,
    Transient = 2
}
