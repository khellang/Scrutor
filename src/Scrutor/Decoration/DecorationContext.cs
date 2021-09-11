using Scrutor.Activation;

using System;

namespace Scrutor.Decoration
{
    /// <summary>
    /// A context in order to provide data specific to the current decoration flow. 
    /// </summary>
    public class DecorationContext : IDisposable
    {
        private readonly DecorationContext _rPrev;

        internal DecorationContext(DecorationContext prev)
        {
            _rPrev = Preconditions.NotNull(prev, nameof(prev));

            Current = this;
        }

        // No sense to implement a real execution friendly context, since not suppose to be changed from multiple threads, just static will be enough for my mind...
        internal static DecorationContext? Current { get; private set; }

        /// <summary>
        /// The <see cref="ServiceActivator"/> that will be used in the current decoration flow
        /// </summary>
        public IServiceActivator ServiceActivator { get; } = new ScrutorServiceActivator();

        /// <summary>
        /// Restores the captured decoration context or <see langword="null"/>
        /// </summary>
        public void Dispose()
        {
            Current = _rPrev;
        }
    }
}
