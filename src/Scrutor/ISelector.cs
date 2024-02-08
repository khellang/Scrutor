using Microsoft.Extensions.DependencyInjection;

namespace Scrutor;

public interface ISelector
{
    void Populate(IServiceCollection services, RegistrationStrategy? options);
}
