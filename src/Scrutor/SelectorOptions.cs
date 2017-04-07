using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Scrutor
{
    public class SelectorOptions
    {
        public RegistrationBehavior? RegistrationBehavior { get; set; }
        public ReplacementStrategy? ReplacementStrategy { get; set; }

        public void MergeOptions(SelectorOptions otherOptions)
        {
            RegistrationBehavior = RegistrationBehavior ??  otherOptions.RegistrationBehavior;
            if (otherOptions.ReplacementStrategy.HasValue && ReplacementStrategy.HasValue)
            {
                ReplacementStrategy |= otherOptions.ReplacementStrategy;
            }
            else
            {
                ReplacementStrategy = ReplacementStrategy ?? otherOptions.ReplacementStrategy;
            }
        }

        public void ApplyType(IServiceCollection services, ServiceDescriptor descriptor)
        {
            switch (RegistrationBehavior ?? Scrutor.RegistrationBehavior.Append)
            {
                case Scrutor.RegistrationBehavior.Replace:
                {
                    var strategy = ReplacementStrategy ?? Scrutor.ReplacementStrategy.ServiceType;
                    if (strategy == Scrutor.ReplacementStrategy.Default)
                    {
                        strategy = Scrutor.ReplacementStrategy.ServiceType;
                    }
                    for (var i = services.Count - 1; i >= 0; i--)
                    {
                        if ((strategy.HasFlag(Scrutor.ReplacementStrategy.ServiceType) && services[i].ServiceType == descriptor.ServiceType)
                            || (strategy.HasFlag(Scrutor.ReplacementStrategy.ImplementationType) && services[i].ImplementationType == descriptor.ImplementationType))
                        {
                            services.RemoveAt(i);
                        }
                    }
                    services.Add(descriptor);
                    break;
                }
                case Scrutor.RegistrationBehavior.SkipIfExists:
                {
                    services.TryAdd(descriptor);
                    break;
                }
                case Scrutor.RegistrationBehavior.Append:
                {
                    services.Add(descriptor);
                    break;
                }
            }
        }
    }
}