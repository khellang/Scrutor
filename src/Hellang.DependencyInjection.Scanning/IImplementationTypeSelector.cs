using System;

namespace Microsoft.Extensions.DependencyInjection.Scanning
{
    public interface IImplementationTypeSelector : IFluentInterface
    {
        void AddAttributes();

        IServiceTypeSelector AddClasses();

        IServiceTypeSelector AddClasses(Action<IImplementationTypeFilter> action);
    }
}