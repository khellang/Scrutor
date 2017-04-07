using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Scrutor
{
    public class SelectorOptions
    {
        public RegistrationStrategy RegistrationStrategy { get; set; }

        public void MergeOptions(SelectorOptions otherOptions)
        {
            RegistrationStrategy = RegistrationStrategy ??  otherOptions.RegistrationStrategy;
        }

        public void ApplyType(IServiceCollection services, ServiceDescriptor descriptor)
        {
            (RegistrationStrategy ?? RegistrationStrategy.Append).Apply(services, descriptor);
        }
    }
}