using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Scrutor
{
    public class SelectorOptions
    {
        public bool UseTry { get; set; }
        public bool ReplaceServiceType { get; set; }

        public void MergeOptions(SelectorOptions otherOptions)
        {
            UseTry = UseTry || otherOptions.UseTry;
            ReplaceServiceType = ReplaceServiceType || otherOptions.ReplaceServiceType;
        }

        public void ApplyType(IServiceCollection services, ServiceDescriptor descriptor)
        {
            if (ReplaceServiceType)
            {
                for (var i = services.Count - 1; i >=0; i--)
                {
                    if (services[i].ServiceType == descriptor.ServiceType)
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