using Microsoft.Framework.DependencyInjection;

namespace Microsoft.Extensions.DependencyInjection.Scanning
{
    internal interface ISelector
    {
        void Populate(IServiceCollection services);
    }
}