using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Scrutor.Tests
{

    public class DependenciesTests : TestBase
    {
        [Fact]
        public void CanInjectDesiredDependencyUsingDependenciesTypes()
        {
            var serviceProvider = ConfigureProvider(services =>
            {
                services.AddTransient<IDependency1<int>, Dependency1<int>>();
                services.AddTransient<IDependency1<string>, Dependency1<string>>();

                services.AddWithDependencies(() =>
                {
                    services.AddTransient<Dependant2>();
                }, typeof(IDependency1<string>));
            });

            var dependant = serviceProvider.GetRequiredService<Dependant2>();

            Assert.Same(dependant.Dependency1.GetType(), typeof(Dependency1<string>));
        }

        [Fact]
        public void CanInjectDesiredDependencyUsingDependenciesFactory()
        {
            var serviceProvider = ConfigureProvider(services =>
            {
                services.AddTransient<IDependency1<int>, Dependency1<int>>();
                services.AddTransient<IDependency1<string>, Dependency1<string>>();

                services.AddWithDependencies(() =>
                {
                    services.AddTransient<Dependant2>();
                }, provider => new object[] { provider.GetRequiredService<IDependency1<string>>() });
            });

            var dependant = serviceProvider.GetRequiredService<Dependant2>();

            Assert.Same(dependant.Dependency1.GetType(), typeof(Dependency1<string>));
        }

        [Fact]
        public void CanInjectDesiredDependencyUsingDependenciesFactoryWithInjectionContext()
        {
            var serviceProvider = ConfigureProvider(services =>
            {
                services.AddTransient<IDependency1<Dependant2>, Dependency1<Dependant2>>();

                services.AddWithDependencies(() =>
                {
                    services.AddTransient<Dependant2>();
                },
                (provider, injectionContext) => new object[]
                {
                    provider.GetRequiredService(
                        typeof(IDependency1<>).MakeGenericType(injectionContext.CreatingServiceType)
                    )
                });
            });

            var dependant = serviceProvider.GetRequiredService<Dependant2>();

            Assert.Same(dependant.Dependency1.GetType(), typeof(Dependency1<Dependant2>));
        }

        [Fact]
        public void CanInjectMultipleDependenciesDependencyUsingDependenciesTypes()
        {
            var serviceProvider = ConfigureProvider(services =>
            {
                services.AddTransient<IDependency1<int>, Dependency1<int>>();
                services.AddTransient<IDependency1<string>, Dependency1<string>>();
                services.AddTransient<IDependency2<int>, Dependency2<int>>();
                services.AddTransient<IDependency2<string>, Dependency2<string>>();

                services.AddWithDependencies(() =>
                {
                    services.AddTransient<Dependant2>();
                }, typeof(IDependency1<string>), typeof(IDependency2<string>));
            });

            var dependant = serviceProvider.GetRequiredService<Dependant2>();

            Assert.Same(dependant.Dependency1.GetType(), typeof(Dependency1<string>));
            Assert.Same(dependant.Dependency2.GetType(), typeof(Dependency2<string>));
        }

        [Fact]
        public void CanInjectMultipleDependenciesDependencyUsingDependenciesFactory()
        {
            var serviceProvider = ConfigureProvider(services =>
            {
                services.AddTransient<IDependency1<int>, Dependency1<int>>();
                services.AddTransient<IDependency1<string>, Dependency1<string>>();
                services.AddTransient<IDependency2<int>, Dependency2<int>>();
                services.AddTransient<IDependency2<string>, Dependency2<string>>();

                services.AddWithDependencies(() =>
                {
                    services.AddTransient<Dependant2>();
                },
                provider => new object[]
                {
                    provider.GetRequiredService<IDependency1<string>>(),
                    provider.GetRequiredService<IDependency2<string>>()
                });
            });

            var dependant = serviceProvider.GetRequiredService<Dependant2>();

            Assert.Same(dependant.Dependency1.GetType(), typeof(Dependency1<string>));
            Assert.Same(dependant.Dependency2.GetType(), typeof(Dependency2<string>));
        }

        [Fact]
        public void CanInjectMultipleDependenciesUsingDependenciesFactoryWithInjectionContext()
        {
            var serviceProvider = ConfigureProvider(services =>
            {
                services.AddTransient<IDependency1<Dependant2>, Dependency1<Dependant2>>();
                services.AddTransient<IDependency2<Dependant2>, Dependency2<Dependant2>>();

                services.AddWithDependencies(() =>
                {
                    services.AddTransient<Dependant2>();
                },
                (provider, injectionContext) => new object[]
                {
                    provider.GetRequiredService(
                        typeof(IDependency1<>).MakeGenericType(injectionContext.CreatingServiceType)
                    ),
                    provider.GetRequiredService(
                        typeof(IDependency2<>).MakeGenericType(injectionContext.CreatingServiceType)
                    )
                });
            });

            var dependant = serviceProvider.GetRequiredService<Dependant2>();

            Assert.Same(dependant.Dependency1.GetType(), typeof(Dependency1<Dependant2>));
            Assert.Same(dependant.Dependency2.GetType(), typeof(Dependency2<Dependant2>));
        }

        [Fact]
        public void CanInjectDesiredDependenciesWithMultipleLevels()
        {
            var serviceProvider = ConfigureProvider(services =>
            {
                services.AddTransient<IDependant2, Dependant2>();
                services.AddTransient<Dependency1<int>>();

                services.AddWithDependencies(() =>
                {
                    services.AddTransient<AlternativeDependant2>();
                }, typeof(Dependency1<int>));

                services.AddWithDependencies(() =>
                {
                    services.AddTransient<Dependant1>();
                },
                provider => new object[]
                {
                    provider.GetRequiredService<AlternativeDependant2>()
                });
            });

            var dependant1 = serviceProvider.GetRequiredService<Dependant1>();

            Assert.Same(dependant1.Dependant2.GetType(), typeof(AlternativeDependant2));
            Assert.Same(dependant1.Dependant2.Dependency1.GetType(), typeof(Dependency1<int>));
        }

        [Fact]
        public void CanDetectCircularDependencyWithNestedCalls()
        {
            var serviceProvider = ConfigureProvider(services =>
            {
                services.AddWithDependencies(() =>
                {
                    services.AddTransient<Dependant1>();

                    services.AddWithDependencies(() =>
                    {
                        services.AddTransient<AlternativeDependant2>();
                    });
                },
                provider => new object[]
                {
                    provider.GetRequiredService<AlternativeDependant2>()
                });
            });

            Assert.Throws<CircularDependencyException>(() =>
            {
                serviceProvider.GetRequiredService<Dependant1>();
            });
        }

        [Fact]
        public void CanDetectCircularDependency()
        {
            var serviceProvider = ConfigureProvider(services =>
            {
                services.AddWithDependencies(() =>
                {
                    services.AddTransient<Dependant1>();

                    services.AddTransient<AlternativeDependant2>();
                },
                provider => new object[]
                {
                    provider.GetRequiredService<AlternativeDependant2>()
                });
            });

            Assert.Throws<CircularDependencyException>(() =>
            {
                serviceProvider.GetRequiredService<Dependant1>();
            });
        }

        private interface IDependency1 { }

        private interface IDependency1<T> : IDependency1 { }

        private class Dependency1<T> : IDependency1<T> { }

        private interface IDependency2 { }

        private interface IDependency2<T> : IDependency2 { }

        private class Dependency2<T> : IDependency2<T> { }

        private interface IDependant2
        {
            IDependency1 Dependency1 { get; }
        }

        private class Dependant2 : IDependant2
        {
            public IDependency1 Dependency1 { get; }
            public IDependency2 Dependency2 { get; }

            public Dependant2(IDependency1 dependency1, IDependency2 dependency2 = null)
            {
                Dependency1 = dependency1;
                Dependency2 = dependency2;
            }
        }

        private class AlternativeDependant2 : IDependant2
        {
            public IDependency1 Dependency1 { get; }

            public AlternativeDependant2(IDependency1 dependency1)
            {
                Dependency1 = dependency1;
            }
        }

        private class Dependant1
        {
            public IDependant2 Dependant2 { get; }

            public Dependant1(IDependant2 dependant2)
            {
                Dependant2 = dependant2;
            }
        }
    }

}
