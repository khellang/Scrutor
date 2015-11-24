using System;
using Microsoft.Extensions.DependencyInjection;


namespace Scrutor
{
	public static class ServiceCollectionExtensions
	{
		/// <summary>
		/// Adds registrations to the <paramref name="services"/> collection using
		/// conventions specified using the <paramref name="action"/>.
		/// </summary>
		/// <param name="services">The services to add to.</param>
		/// <param name="action">The configuration action.</param>
		/// <exception cref="System.ArgumentNullException">If either the <paramref name="services"/>
		/// or <paramref name="action"/> arguments are <c>null</c>.</exception>
		public static IServiceCollection Scan(this IServiceCollection services, Action<IAssemblySelector> action)
		{
			if (services == null)
			{
				throw new ArgumentNullException(nameof(services));
			}

			if (action == null)
			{
				throw new ArgumentNullException(nameof(action));
			}

			var selector = new AssemblySelector();

			action(selector);

			selector.Populate(services);

			return services;
		}
	}
}