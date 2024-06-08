namespace System.Threading;

public sealed class Latch
{
	private long _flags;
	private int _activeReaders, _awaitingWriters;

	private const long W = 1L << 32;
	private readonly object _x , _s ;

	public Latch() => (_x, _s) = (this, new());
	public Latch(object s) => (_x, _s) = (this, s);
	public Latch(object x, object s) => (_x, _s) = (x, s);


	public XLock ExclusiveLock()
	{
		if (Interlocked.Add(ref _flags, W) != W)
		{
			Monitor.Enter(_x);
			if (Volatile.Read(ref _flags) != W)
			{
				Interlocked.Increment(ref _awaitingWriters);
				Monitor.Wait(_x);
				Interlocked.Decrement(ref _awaitingWriters);
			}
			else Monitor.Exit(_x);
		}
		return new XLock(this);
	}

	public SLock SharedLock()
	{
		if (Interlocked.Increment(ref _flags) > W)
		{
			Monitor.Enter(_s);
			if (Volatile.Read(ref _flags) > W) Monitor.Wait(_s);
			else Monitor.Exit(_s);
		}

		Interlocked.Increment(ref _activeReaders);
		return new SLock(this);
	}


	public ref struct XLock
	{
		private readonly Latch _latch;

		internal XLock(Latch latch) => _latch = latch;

		public void Dispose()
		{
			var flags = Interlocked.Add(ref _latch._flags, -W);
			if (flags == 0L) return;
			if (flags >= W)
			{
				var exclusive = _latch._x;
				while (Volatile.Read(ref _latch._awaitingWriters) == 0 && Volatile.Read(ref _latch._flags) >= W) Thread.Yield();
				Monitor.Enter(exclusive);
				if (Volatile.Read(ref _latch._flags) >= W)
				{
					Monitor.Pulse(exclusive);
					Monitor.Exit(exclusive);
					return;
				}
				Monitor.Exit(exclusive);

			}
			var shared = _latch._s;
			Monitor.Enter(shared);
			Monitor.PulseAll(shared);
			Monitor.Exit(shared);
		}
	}

	public ref struct SLock
	{
		private readonly Latch _latch;

		internal SLock(Latch latch) => _latch = latch;

		public void Dispose()
		{
			var flags = Interlocked.Decrement(ref _latch._flags);
			if (Interlocked.Decrement(ref _latch._activeReaders) == 0L)
				if (flags >= W)
				{
					var exclusive = _latch._x;
					while (Volatile.Read(ref _latch._awaitingWriters) == 0 && Volatile.Read(ref _latch._flags) >= W) Thread.Yield();
					Monitor.Enter(exclusive);
					if (Volatile.Read(ref _latch._flags) >= W) Monitor.Pulse(exclusive);
					Monitor.Exit(exclusive);
				}
		}
	}
}