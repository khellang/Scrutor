using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Scrutor.Installers
{
    /// <summary>
    ///     Encapsulates and partitions registration logic so related registrations can be grouped and installed together
    ///     All implementations must have a default public constructor
    /// </summary>
    public interface IServiceInstaller
    {
        /// <summary>
        ///     Installs registrations into the services collection. <see cref="IServiceCollection"></see>
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configuration">The configuration</param>
        void Install(IServiceCollection services, IConfiguration configuration);
    }
}
