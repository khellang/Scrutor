using System;
using System.Collections.Generic;

namespace Scrutor
{
    /// <summary>
    /// A class used to dispose decorated services
    /// </summary>
    public class DecoratedServiceDisposer : IDisposable
    {
        private bool _disposed;
        private IDisposable _disposable;

        public void Set(IDisposable disposable)
        {
            _disposable = disposable;
        }

        public void Dispose()
        { 
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _disposable.Dispose();
        }
    }
}
