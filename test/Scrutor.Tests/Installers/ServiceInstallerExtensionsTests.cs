using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Primitives;
using Scrutor.Installers;
using Xunit;

namespace Scrutor.Tests.Installers
{
    public class InstallerExtensionsTests
    {
        public class Foo
        {
        }

        public class FooInstaller : IServiceInstaller
        {
            public void Install(IServiceCollection services, IConfiguration configuration)
            {
                services.AddSingleton(new Foo());
            }
        }

        public class Bar
        {
        }

        public class BarInstaller : IServiceInstaller
        {
            public void Install(IServiceCollection services, IConfiguration configuration)
            {
                services.AddSingleton(new Bar());
            }
        }

        private class DummyConfiguration : IConfiguration
        {
            public IConfigurationSection GetSection(string key)
            {
                return null;
            }

            public IEnumerable<IConfigurationSection> GetChildren()
            {
                return Enumerable.Empty<IConfigurationSection>();
            }

            public IChangeToken GetReloadToken()
            {
                return null;
            }

            public string this[string key]
            {
                get { return string.Empty; }
                set { }
            }
        }

        [Fact]
        public void ShouldFindAndInstallAllInstallersFromAnAssembly()
        {
            // Arrange
            IServiceCollection expectedServices = new ServiceCollection();
            IConfiguration expectedConfiguration = new DummyConfiguration();

            // Act
            expectedServices.InstallFromAssemblyContaining<InstallerExtensionsTests>(expectedConfiguration);

            // Assert
            var provider = expectedServices.BuildServiceProvider();

            Assert.IsType<Foo>(provider.GetRequiredService<Foo>());
            Assert.IsType<Bar>(provider.GetRequiredService<Bar>());
        }
    }
}
