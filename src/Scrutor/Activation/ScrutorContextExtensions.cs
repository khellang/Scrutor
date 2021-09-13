namespace Scrutor.Activation
{
    public static class ScrutorContextExtensions
    {
        private const string SERVICE_ACTIVATOR = nameof(SERVICE_ACTIVATOR);

        /// <summary>
        /// Gets the bound <see cref="IServiceActivator"/>
        /// </summary>
        /// <param name="this">This</param>
        public static IServiceActivator? GetServiceActivator(this ScrutorContext @this)
            => @this[SERVICE_ACTIVATOR] as IServiceActivator;

        public static IServiceActivator GetServiceActivatorOrDefault(this ScrutorContext? @this)
        {
            IServiceActivator? result = @this?.GetServiceActivator();
            if (result == null)
            {
                result = new DefaultServiceActivator();

                // Memorize in order to prevent additional allocations
                @this?.UseServiceActivator(result);
            }

            return result;
        }

        /// <summary>
        /// Sets the <see cref="IServiceActivator"/> in the current context
        /// </summary>
        /// <param name="this">This</param>
        /// <param name="serviceActivator"><see cref="IServiceActivator"/> to use in the scrutor flow</param>
        /// <returns></returns>
        public static ScrutorContext UseServiceActivator(this ScrutorContext @this, IServiceActivator serviceActivator)
        {
            @this[SERVICE_ACTIVATOR] = Preconditions.NotNull(serviceActivator, nameof(serviceActivator));

            return @this;
        }
    }
}
