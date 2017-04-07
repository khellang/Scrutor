using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Scrutor
{
    public abstract class RegistrationStrategy
    {
        public static readonly RegistrationStrategy Skip = new SkipRegistrationStrategy();

        public static readonly RegistrationStrategy Append = new AppendRegistrationStrategy();

        public static RegistrationStrategy Replace(ReplacementBehavior behavior = ReplacementBehavior.Default) => new ReplaceRegistrationStrategy(behavior);

        public abstract void Apply(IServiceCollection services, ServiceDescriptor descriptor);

        private sealed class SkipRegistrationStrategy : RegistrationStrategy
        {
            public override void Apply(IServiceCollection services, ServiceDescriptor descriptor) => services.TryAdd(descriptor);
        }

        private sealed class AppendRegistrationStrategy : RegistrationStrategy
        {
            public override void Apply(IServiceCollection services, ServiceDescriptor descriptor) => services.Add(descriptor);
        }

        private sealed class ReplaceRegistrationStrategy : RegistrationStrategy
        {
            public ReplaceStrategy(ReplacementBehavior behavior)
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

                for (var i = services.Count - 1; i >= 0; i--)
                {
                    if ((behavior.HasFlag(ReplacementBehavior.ServiceType) && services[i].ServiceType == descriptor.ServiceType)
                        || (behavior.HasFlag(ReplacementBehavior.ImplementationType) && services[i].ImplementationType == descriptor.ImplementationType))
                    {
                        services.RemoveAt(i);
                    }
                }

                services.Add(descriptor);
            }
        }
    }
}
