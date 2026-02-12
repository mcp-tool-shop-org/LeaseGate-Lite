using System.Threading;

namespace LeaseGateLite.Daemon;

public sealed class SingleInstanceGuard : IDisposable
{
    private readonly Mutex _mutex;

    public bool Acquired { get; }

    public SingleInstanceGuard(string name)
    {
        _mutex = new Mutex(true, name, out var createdNew);
        Acquired = createdNew;
    }

    public void Dispose()
    {
        if (Acquired)
        {
            _mutex.ReleaseMutex();
        }

        _mutex.Dispose();
    }
}
