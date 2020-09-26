using System;
using Scrutor;

namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class ServiceCollectionExtensions
    {
        /// <summary>
        /// Adds registrations to the <paramref name="services"/> collection using
        /// conventions specified using the <paramref name="action"/>.
        /// </summary>
        /// <param name="services">The services to add to.</param>
        /// <param name="action">The configuration action.</param>
        /// <exception cref="ArgumentNullException">If either the <paramref name="services"/>
        /// or <paramref name="action"/> arguments are <c>null</c>.</exception>
        public static IServiceCollection Scan(this IServiceCollection services, Action<ITypeSourceSelector> action)
        {
            Preconditions.NotNull(services, nameof(services));
            Preconditions.NotNull(action, nameof(action));

            var selector = new TypeSourceSelector();

            action(selector);

            return services.Populate(selector, RegistrationStrategy.Append);
        }

        private static IServiceCollection Populate(this IServiceCollection services, ISelector selector, RegistrationStrategy registrationStrategy)
        {
            selector.Populate(services, registrationStrategy);
            return services;
        }
    }
}
