using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Scrutor
{
    public class SelectorOptions
    {
        public bool UseTry { get; set; }
        public bool ReplaceServiceType { get; set; }
        public bool ReplaceImplementationType { get; set; }

        public void MergeOptions(SelectorOptions otherOptions)
        {
            UseTry = UseTry || otherOptions.UseTry;
            ReplaceServiceType = ReplaceServiceType || otherOptions.ReplaceServiceType;
            ReplaceImplementationType = ReplaceImplementationType || otherOptions.ReplaceImplementationType;
        }

        public void ApplyType(IServiceCollection services, ServiceDescriptor descriptor)
        {
            if (ReplaceServiceType || ReplaceImplementationType)
            {
                for (var i = services.Count - 1; i >=0; i--)
                {
                    if ((ReplaceServiceType && services[i].ServiceType == descriptor.ServiceType)
                        || (ReplaceImplementationType && services[i].ImplementationType == descriptor.ImplementationType))
                    {
                        services.RemoveAt(i);
                    }
                }
                services.Add(descriptor);
            }
            else if (UseTry)
            {
                services.TryAdd(descriptor);
            }
            else
            {
                services.Add(descriptor);
            }
        }
    }
}