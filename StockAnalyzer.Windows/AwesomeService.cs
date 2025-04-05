
using System;
using System.Threading.Tasks;

namespace StockAnalyzer.Windows
{
    public class AwesomeService : IAsyncDisposable
    {
        // Detect redundant Dispose() calls.
        private bool _isDisposed;
        
        public async ValueTask DisposeAsync()
        {
            await Dispose(true);
            GC.SuppressFinalize(this);
        }
        
        // Protected implementation of Dispose pattern.
        protected virtual async Task Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                _isDisposed = true;

                if (disposing)
                {
                    // Dispose managed state.
                    await Task.Delay(10);
                }
            }
        }
    }
}