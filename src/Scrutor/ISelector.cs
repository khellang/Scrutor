using Microsoft.Extensions.DependencyInjection;


namespace Scrutor
{
    internal interface ISelector
    {
        void Populate(IServiceCollection services);
    }
}