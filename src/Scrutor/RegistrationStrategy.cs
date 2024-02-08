using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Scrutor;

public abstract class RegistrationStrategy
{
    /// <summary>
    /// Skips registrations for services that already exists.
    /// </summary>
    public static readonly RegistrationStrategy Skip = new SkipRegistrationStrategy();

    /// <summary>
    /// Appends a new registration for existing services.
    /// </summary>
    public static readonly RegistrationStrategy Append = new AppendRegistrationStrategy();

    /// <summary>
    /// Throws when trying to register an existing service.
    /// </summary>
    public static readonly RegistrationStrategy Throw = new ThrowRegistrationStrategy();

    /// <summary>
    /// Replaces existing service registrations using <see cref="ReplacementBehavior.Default"/>.
    /// </summary>
    public static RegistrationStrategy Replace()
    {
        return Replace(ReplacementBehavior.Default);
    }

    /// <summary>
    /// Replaces existing service registrations based on the specified <see cref="ReplacementBehavior"/>.
    /// </summary>
    /// <param name="behavior">The behavior to use when replacing services.</param>
    public static RegistrationStrategy Replace(ReplacementBehavior behavior)
    {
        return new ReplaceRegistrationStrategy(behavior);
    }

    /// <summary>
    /// Applies the the <see cref="ServiceDescriptor"/> to the <see cref="IServiceCollection"/>.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="descriptor">The descriptor to apply.</param>
    public abstract void Apply(IServiceCollection services, ServiceDescriptor descriptor);

    private sealed class SkipRegistrationStrategy : RegistrationStrategy
    {
        public override void Apply(IServiceCollection services, ServiceDescriptor descriptor) => services.TryAdd(descriptor);
    }

    private sealed class AppendRegistrationStrategy : RegistrationStrategy
    {
        public override void Apply(IServiceCollection services, ServiceDescriptor descriptor) => services.Add(descriptor);
    }

    private sealed class ThrowRegistrationStrategy : RegistrationStrategy
    {
        public override void Apply(IServiceCollection services, ServiceDescriptor descriptor)
        {
            if (services.HasRegistration(descriptor.ServiceType))
            {
                throw new DuplicateTypeRegistrationException(descriptor.ServiceType);
            }

            services.Add(descriptor);
        }
    }

    private sealed class ReplaceRegistrationStrategy : RegistrationStrategy
    {
        public ReplaceRegistrationStrategy(ReplacementBehavior behavior)
        {
            Behavior = behavior;
        }

        private ReplacementBehavior Behavior { get; }

        public override void Apply(IServiceCollection services, ServiceDescriptor descriptor)
        {
            var behavior = Behavior;

            if (behavior == ReplacementBehavior.Default)
            {
                behavior = ReplacementBehavior.ServiceType;
            }

            if (behavior.HasFlag(ReplacementBehavior.ServiceType))
            {
                for (var i = services.Count - 1; i >= 0; i--)
                {
                    if (services[i].ServiceType == descriptor.ServiceType)
                    {
                        services.RemoveAt(i);
                    }
                }
            }

            if (behavior.HasFlag(ReplacementBehavior.ImplementationType))
            {
                for (var i = services.Count - 1; i >= 0; i--)
                {
                    if (services[i].ImplementationType == descriptor.ImplementationType)
                    {
                        services.RemoveAt(i);
                    }
                }
            }

            services.Add(descriptor);
        }
    }
}
