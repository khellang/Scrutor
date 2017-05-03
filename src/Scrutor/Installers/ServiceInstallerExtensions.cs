using System;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Scrutor.Installers
{
    public static class ServiceInstallerExtensions
    {
        /// <summary>
        ///     Scan the assembly for the given type for installers <see cref="IServiceInstaller" /> and installs
        ///     registrations into the services collection <see cref="IServiceCollection" />
        /// </summary>
        /// <typeparam name="T">The type contained in the assembly</typeparam>
        /// <param name="services">The service collection</param>
        /// <param name="configuration">The configuration</param>
        /// <returns></returns>
        public static IServiceCollection InstallFromAssemblyContaining<T>(this IServiceCollection services,
            IConfiguration configuration)
        {
            return services.InstallFromAssembly(typeof(T).GetTypeInfo().Assembly, configuration);
        }

        /// <summary>
        ///     Scan the assembly for for installers <see cref="IServiceInstaller" /> and installs
        ///     registrations into the services collection <see cref="IServiceCollection" />
        /// </summary>
        /// <param name="services">The service collection</param>
        /// <param name="assembly">The assembly to scan</param>
        /// <param name="configuration">The configuration</param>
        /// <returns></returns>
        public static IServiceCollection InstallFromAssembly(this IServiceCollection services, Assembly assembly,
            IConfiguration configuration)
        {
            var installers = assembly.ExportedTypes
                .Where(IsInstaller)
                .Select(Activator.CreateInstance)
                .Cast<IServiceInstaller>();

            foreach (var installer in installers)
                services.Install(installer, configuration);

            return services;
        }

        /// <summary>
        ///     Installs the provided installer registrations into the service collection <see cref="IServiceCollection" />
        /// </summary>
        /// <param name="services">The services collection</param>
        /// <param name="installer">The installer to install</param>
        /// <param name="configuration">The configuration</param>
        /// <returns></returns>
        public static IServiceCollection Install(this IServiceCollection services, IServiceInstaller installer,
            IConfiguration configuration)
        {
            installer.Install(services, configuration);
            return services;
        }

        private static bool IsInstaller(Type type)
        {
            var typeInfo = type.GetTypeInfo();
            return typeof(IServiceInstaller).GetTypeInfo().IsAssignableFrom(typeInfo) && typeInfo.IsClass &&
                   !typeInfo.IsAbstract;
        }
    }
}