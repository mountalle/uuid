namespace System.Collections;

using Diagnostics;
using Runtime.ConstrainedExecution;
using Runtime.InteropServices;
using Text;

[DebuggerDisplay("Count: {Count} of {_keyIntStart}")]
internal sealed class PascalOrdinalMap : CriticalFinalizerObject, IEnumerable<(string key, uint ordinal)>, IDisposable
{
	private const uint LongLength = 32, IntLength = LongLength << 1, ByteLength = LongLength << 3, MinCapacity = 1, MaxCapacity = uint.MaxValue >> 2;

	private nint _handle;
	private uint _ordinalCount, _keyCount, _keyIntStart, _trashByteStart;

	public PascalOrdinalMap(uint capacity)
	{
		if (capacity is < MinCapacity or > MaxCapacity) throw new ArgumentOutOfRangeException(nameof(capacity));
		var trashByteLength = InlinedTrashBin.BytesFor(capacity);
		_handle = Marshal.AllocHGlobal((nint)((_trashByteStart = (capacity * (IntLength + 1U)) << 2) + trashByteLength));
		_ordinalCount = _keyCount = 0;
		_keyIntStart = capacity;
		Trash.Clear(trashByteLength);
	}

	public uint Count => _ordinalCount;
	private ref InlinedTrashBin Trash => ref new Unsafe<byte>(_handle).As<InlinedTrashBin>(_trashByteStart).Value;

	public bool TryGetIndex(string name, out uint index)
	{
		if (name.Length is 0 or >= (int)ByteLength) goto Fail;
		Span<byte> span = stackalloc byte[(name.Length << 1) + 1];
		int length = Encoding.UTF8.GetBytes(name, span[1..]), bytes = length + 1, reminder = bytes & 7;
		if (reminder != 0) bytes += 8 - reminder;
		if (bytes > ByteLength) goto Fail;

		Unsafe<byte> origin = span;
		origin.Value = unchecked((byte)length);

		var ordinal = OrdinalOf(origin.As<ulong>(), bytes >> 3);
		if (ordinal > -1)
		{
			index = new Unsafe<uint>(_handle)[ordinal];
			return true;
		}
		Fail:
		index = uint.MaxValue;
		return false;
	}


	public bool Contains(string name)
	{
		if (name.Length is 0 or >= (int)ByteLength) return false;
		Span<byte> span = stackalloc byte[(name.Length << 1) + 1];
		int length = Encoding.UTF8.GetBytes(name, span[1..]), bytes = length + 1, reminder = bytes & 7;
		if (reminder != 0) bytes += 8 - reminder;
		if (bytes > ByteLength) return false;

		Unsafe<byte> origin = span;
		origin.Value = unchecked((byte)length);

		return OrdinalOf(origin.As<ulong>(), bytes >> 3) > -1;
	}


	private uint AcquireOrdinal(uint index, out bool exists)
	{
		var len = _keyCount;
		if (index >= len) goto Fail;

		var ordinals = new Unsafe<uint>(_handle);
		for (var i = 0U; i < len; i++)
			if (ordinals[i] == index)
			{
				exists = true;
				return i;
			}

		Fail:
		exists = false;
		return uint.MaxValue;
	}

	public uint this[string name]
	{
		get
		{
			if (name.Length == 0) throw new ArgumentException("Non empty value expected.", nameof(name));
			if (name.Length >= (int)ByteLength) throw new ArgumentOutOfRangeException(nameof(name));
			Span<byte> span = stackalloc byte[(name.Length << 1) + 1];
			int length = Encoding.UTF8.GetBytes(name, span[1..]), bytes = length + 1, reminder = bytes & 7;
			if (reminder != 0) bytes += 8 - reminder;
			if (bytes > ByteLength) throw new ArgumentOutOfRangeException(nameof(name), bytes, $"Byte length is greater then expected {ByteLength} bytes.");

			Unsafe<byte> origin = span;
			origin.Value = unchecked((byte)length);

			var value = origin.As<ulong>();
			var ordinal = OrdinalOf(value, bytes >> 3);
			var ordinals = new Unsafe<uint>(_handle);
			if (ordinal < 0) Add(value, (uint)(ordinal = ~ordinal));
			return ordinals[ordinal];
		}
	}

	public void Clear(bool eraseValues = false)
	{
		if (Trash.Count != 0) Trash.Clear(InlinedTrashBin.BytesFor(_keyIntStart));
		_keyCount = _ordinalCount = 0;
	}


	public bool RemoveWithIndex(uint index)
	{
		if (AcquireOrdinal(index, out var exists) is var ordinal && exists)
		{
			RemoveWithOrdinal(ordinal);
			return true;
		}
		return false;
	}

	public bool RemoveWithKey(string key)
	{
		if (key.Length is 0 or >= (int)ByteLength) return false;
		Span<byte> span = stackalloc byte[(key.Length << 1) + 1];
		int length = Encoding.UTF8.GetBytes(key, span[1..]), bytes = length + 1, reminder = bytes & 7;
		if (reminder != 0) bytes += 8 - reminder;
		if (bytes > ByteLength) return false;

		Unsafe<byte> origin = span;
		origin.Value = unchecked((byte)length);

		var value = origin.As<ulong>();
		var ordinal = OrdinalOf(value, bytes >> 3);
		if (ordinal < 0) return false;
		RemoveWithOrdinal((uint)ordinal);
		return true;
	}

	private void Add(Unsafe<ulong> key, uint ordinal)
	{
		var count = _ordinalCount++;
		var source = new Unsafe<uint>(_handle);
		if (Trash.TryRestore(out var index)) goto Insert;

		index = _keyCount++;
		if (index >= _keyIntStart) // inflate
		{
			var capacity = index << 1;
			var neu = Marshal.AllocHGlobal((nint)((_trashByteStart = (capacity * (IntLength + 1U)) << 2) + InlinedTrashBin.BytesFor(capacity)));
			var destination = new Unsafe<uint>(neu);
			// copy ordinals
			if (ordinal != 0)
			{
				source.BlockCopyTo(destination, ordinal);
				if (ordinal != count) (source + ordinal).BlockCopyTo(destination, ordinal + 1U, count - ordinal);
			}
			else source.BlockCopyTo(destination, 1U, count);

			// copy keys
			(source + _keyIntStart).BlockCopyTo(destination, capacity, index * IntLength);
			Marshal.FreeHGlobal(Interlocked.Exchange(ref _handle, neu));
			Trash.Clear(InlinedTrashBin.BytesFor(_keyIntStart = capacity));
			source = destination;
			goto SetValue;
		}

		Insert:
		if (ordinal != count) (source + ordinal).BlockCopyTo(source, ordinal + 1, count - ordinal);

		SetValue:
		key.BlockCopyTo((source + _keyIntStart + index * IntLength).As<ulong>(), LongLength);
		source[ordinal] = index;
	}

	private void RemoveWithOrdinal(uint ordinal)
	{
		var item = new Unsafe<uint>(_handle) + ordinal;
		var index = item.Value;
		if (index == _keyCount - 1) _keyCount--;
		else Trash[index] = true;
		var last = --_ordinalCount;
		if (ordinal != last) (item + 1).BlockCopyTo(item, last - ordinal);
	}

	private int OrdinalOf(Unsafe<ulong> value, int length)
	{
		uint lo = 0, hi = _ordinalCount, ordinal;
		if (hi == 0) return -1;
		var ordinals = new Unsafe<uint>(_handle);
		var keys = ordinals.As<ulong>(_keyIntStart);
		ulong v, c;

		//var query = AsString(value.As<byte>());

		for (hi--; lo <= hi;)
		{
			var comparand = keys + ordinals[ordinal = hi + (lo >> 1)] * LongLength;
			//string existing = AsString((keys + ordinals[ordinal]* LongLength).As<byte>());
			for (var i = 0; i < length; i++)
				if ((v = value[i]) > (c = comparand[i]))
				{
					lo = ordinal + 1;
					goto Next;
				}
				else if (v < c)
				{
					if (ordinal == 0) return -1;
					hi = ordinal - 1;
					goto Next;
				}
			return unchecked((int)ordinal);
			Next: ;
		}

		return ~unchecked((int)lo);
	}

	~PascalOrdinalMap()
	{
		var handle = Interlocked.Exchange(ref _handle, 0);
		if (handle != 0) Marshal.FreeHGlobal(_handle);
	}

	// ReSharper disable once NotDisposedResourceIsReturned
	IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable<(string key, uint ordinal)>)this).GetEnumerator();

	public void Dispose()
	{
		var handle = Interlocked.Exchange(ref _handle, 0);
		if (handle != 0) Marshal.FreeHGlobal(_handle);
	}

	// private static string AsString(Unsafe<byte> key)
	// {
	// 	var length = key.Value;
	// 	var bytes = key.CreateSpan((int)ByteLength);
	// 	return Encoding.UTF8.GetString(bytes.Slice(1, bytes[0]));
	// }

	IEnumerator<(string key, uint ordinal)> IEnumerable<(string key, uint ordinal)>.GetEnumerator()
	{
		var origin = new Unsafe<uint>(_handle);
		var ordinals = origin.CreateSpan(_ordinalCount).ToArray();
		var keys = (origin + _keyIntStart).As<byte>().CreateSpan(_keyCount * ByteLength).ToArray();

		return Enumerate(ordinals, keys);
	}

	private IEnumerator<(string key, uint ordinal)> Enumerate(uint[] ordinals, byte[] keys)
	{
		foreach (var index in ordinals)
		{
			var bytes = keys.AsSpan((int)(index * ByteLength), (int)ByteLength);
			var key = Encoding.UTF8.GetString(bytes.Slice(1, bytes[0]));
			yield return (key, index);
		}
	}

	public Iterator<(string key, uint ordinal)> GetEnumerator()
	{
		if (Count == 0) return default;
		unsafe
		{
			return new Iterator<(string key, uint ordinal)>(new Iteration(this, &Next), &Select);

			static bool Next(ref Iteration iteration)
			{
				return iteration.As<PascalOrdinalMap>().Count > iteration.Cursor++;
			}

			static (string key, uint ordinal) Select(ref Iteration iteration)
			{
				var me = iteration.As<PascalOrdinalMap>();
				var origin = new Unsafe<uint>(me._handle);
				var ordinal = origin[iteration.Cursor];
				var value = (origin + me._keyIntStart + ordinal * IntLength).As<byte>();
				var key = Encoding.UTF8.GetString(value.CreateSpan(1, value.Value));
				return (key, ordinal);
			}
		}
	}
}