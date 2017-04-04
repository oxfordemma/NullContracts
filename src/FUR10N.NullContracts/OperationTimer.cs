using System;
using System.Diagnostics;

namespace FUR10N.NullContracts
{
    public class OperationTimer : IDisposable
    {
#if DEBUG
        private readonly Stopwatch sw;

        private readonly Action<long> callback;
#endif

        public OperationTimer(Action<long> callback)
        {
#if DEBUG
            this.callback = callback;

            sw = new Stopwatch();
            sw.Start();
#endif
        }

        public void Dispose()
        {
#if DEBUG
            sw.Stop();
            callback(sw.ElapsedMilliseconds);
#endif
        }
    }
}
