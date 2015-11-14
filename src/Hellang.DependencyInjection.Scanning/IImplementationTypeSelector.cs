using System;

namespace Microsoft.Extensions.DependencyInjection.Scanning
{
    public interface IImplementationTypeSelector : IFluentInterface
    {
        IServiceTypeSelector AddClasses();

        IServiceTypeSelector AddClasses(Action<IImplementationTypeFilter> action);
    }
}