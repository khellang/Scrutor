using Microsoft.Extensions.DependencyInjection;

using Scrutor.Activation;

using System;
using System.Collections;
using System.Collections.Generic;

namespace Scrutor
{
    public static class ScrutorServiceCollectionExtensions
    {
        /// <summary>
        /// Starts the scrutor flow.
        /// </summary>
        /// <param name="this">This</param>
        /// <param name="configure">Additional configuration of the created <see cref="ScrutorContext"/></param>
        /// <param name="startNew">Independently from the previous context will start a new one.</param>
        /// <remarks>
        ///     NOT THREADSAFE!
        /// </remarks>
        public static ScrutorContext Scrutor(this IServiceCollection @this, 
            Action<ScrutorContext>? configure = null, 
            bool startNew = false)
        {
            ScrutorContext? result = ScrutorContext.Current;
            if (startNew || result?.Wrapped != @this)
            {
                // Create a new one with defaults.
                result = new ScrutorContext(@this);

                // Setup defaults for a new context.
                result.UseServiceActivator(new ScrutorServiceActivator());

                ScrutorContext.Current = result;
            }

            configure?.Invoke(result);

            return result;
        }
    }

    /// <summary>
    /// A context in order to provide some data specific to the current registration flow based on Scrutor. 
    /// </summary>
    public class ScrutorContext : IServiceCollection, IDisposable
    {
        // Not suppose to be threadsafe or execution context bounded. 
        internal static ScrutorContext? Current { get; set; }

        private readonly Dictionary<string, object?> _rItems = new Dictionary<string, object?>();

        internal ScrutorContext(IServiceCollection wrappedServiceCollection)
        {
            Wrapped = Preconditions.NotNull(wrappedServiceCollection, nameof(wrappedServiceCollection));
        }

        internal IServiceCollection Wrapped { get; }

        /// <summary>
        /// Indexer in order to interact with context items.
        /// </summary>
        /// <param name="key">Key under which you suppose to store the item in the context</param>
        public object? this[string key]
        {
            get
            {
                Preconditions.NotNull(key, nameof(key));

                _rItems.TryGetValue(key, out object? result);

                return result;
            }
            set
            {
                Preconditions.NotNull(key, nameof(key));

                _rItems[key] = value;
            }
        }

        /// <summary>
        /// Invalidates <see cref="ScrutorContext.Current"/> if same.
        /// </summary>
        public void Dispose()
        {
            if (ScrutorContext.Current == this)
                ScrutorContext.Current = null;
        }

        #region IServiceCollection Wrapper Impl

        public int Count => Wrapped.Count;
        public bool IsReadOnly => Wrapped.IsReadOnly;
        public ServiceDescriptor this[int index]
        {
            get => Wrapped[index];
            set => Wrapped[index] = value;
        }

        public void Add(ServiceDescriptor item) => Wrapped.Add(item);
        public void Clear() => Wrapped.Clear();
        public bool Contains(ServiceDescriptor item) => Wrapped.Contains(item);
        public void CopyTo(ServiceDescriptor[] array, int arrayIndex) => Wrapped.CopyTo(array, arrayIndex);
        public IEnumerator<ServiceDescriptor> GetEnumerator() => Wrapped.GetEnumerator();
        public int IndexOf(ServiceDescriptor item) => Wrapped.IndexOf(item);
        public void Insert(int index, ServiceDescriptor item) => Wrapped.Insert(index, item);
        public bool Remove(ServiceDescriptor item) => Wrapped.Remove(item);
        public void RemoveAt(int index) => Wrapped.RemoveAt(index);
        IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)Wrapped).GetEnumerator();

        #endregion
    }
}
