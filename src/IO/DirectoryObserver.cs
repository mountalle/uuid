namespace System.IO;

using Runtime.CompilerServices;

public sealed class DirectoryObserver : IDisposable
{
	private const int Timeout = 381; // to avoid multiple onChanged events with the same fs entity
	public DirectoryInfo Directory { get; }
	private FileSystemWatcher? _watcher;
	private Dictionary<string, (int timestamp, FileSystemEventArgs e)>? _map;
	private int _timestamp;

	public event FileSystemEventHandler? Changed;

	public DirectoryObserver(DirectoryInfo directory, string? filter = null, bool recursive = false)
	{
		Directory = directory;
		_map = new();
		_watcher = new(directory.FullName) { NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName, IncludeSubdirectories = recursive, };
		_watcher.Changed += OnWatcher;
		_watcher.Created += OnWatcher;
		_watcher.Deleted += OnWatcher;
		_watcher.Renamed += OnWatcher;
		if (filter is not null) _watcher.Filter = filter;
		_watcher.EnableRaisingEvents = true;
	}

	private void OnWatcher(object sender, FileSystemEventArgs e)
	{
		var key = e.FullPath;
		if (Ignore(key) || _map is not { } map) return;

		// renaming from *.tmp means rewritten content
		if (e is RenamedEventArgs renamedEventArgs && Ignore(renamedEventArgs.OldFullPath))
			e = new FileSystemEventArgs(WatcherChangeTypes.Changed, Directory.FullName, e.Name);

		lock (map) map[key] = (Environment.TickCount, e);

		ThreadPool.QueueUserWorkItem(Tick, map, false);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static bool Ignore(string path) => path.EndsWith(".tmp") || path.EndsWith(".TMP");

	private void Tick(Dictionary<string, (int timestamp, FileSystemEventArgs e)> map)
	{
		if (map.Count == 0) return;
		var ts = Environment.TickCount;
		if (ts - _timestamp < Timeout)
		{
			ThreadPool.QueueUserWorkItem(Tick, map, false);
			return;
		}
		var list = new TinyList<FileSystemEventArgs>();
		lock (map)
		{
			_timestamp = ts;
			foreach (var x in map.ToList())
				if (ts - x.Value.timestamp > Timeout)
				{
					list.Add(x.Value.e);
					map.Remove(x.Key);
				}
		}
		if (list.IsEmpty)
		{
			if (map.Count > 0) ThreadPool.QueueUserWorkItem(Tick, map, false);
			return;
		}
		foreach (var e in list.Span) Changed?.Invoke(this, e);
		if (map.Count > 0) ThreadPool.QueueUserWorkItem(Tick, map, false);
	}


	public void Dispose()
	{
		Interlocked.Exchange(ref _watcher, null)?.Dispose();
		_map = null;
	}
}