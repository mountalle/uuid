namespace System.Collections.Generic;

using Diagnostics;
using Runtime.CompilerServices;
using Runtime.InteropServices;

[DebuggerDisplay("{ToDebugString()}")]
public class AdjacencyStore<T> where T : struct
{
	private T[] _array;
	private uint _cursor;
	protected Tabulae<T> Alive, Dead;

	public AdjacencyStore(uint capacity)
	{
		if (capacity == 0) throw new ArgumentOutOfRangeException(nameof(capacity), capacity, "Expected > 0.");
		_array = new T[capacity];
	}

	public void Clean(bool zerofy = false)
	{
		if (zerofy) Unsafe.InitBlockUnaligned(ref Unsafe.As<T, byte>(ref MemoryMarshal.GetArrayDataReference(_array)), 0, _cursor * (uint)Unsafe.SizeOf<T>());
		Alive = Dead = default;
		_cursor = 0;
	}

	[DebuggerBrowsable(DebuggerBrowsableState.Never)]
	public ref T Origin
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => ref MemoryMarshal.GetArrayDataReference(_array);
	}

	public ref T this[uint ordinal]
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get
		{
			if (ordinal < Alive.Count) return ref _array[ordinal];
			throw new ArgumentOutOfRangeException();
		}
	}

	public uint Count
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Alive.Count;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Deconstruct(out uint cursor, out uint count) => Alive.Deconstruct(out cursor, out count);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Deconstruct(out uint cursor, out uint count, out uint first) => Alive.Deconstruct(out cursor, out count, out first);

	public ref T New(ref Adjes<T> adjes, out uint ordinal)
	{
		if ((ordinal = Dead.DoublyPop(ref adjes)) is uint.MaxValue)
			if ((ordinal = _cursor++) >= _array.Length)
			{
				var neu = new T[ordinal << 1];
				Unsafe.CopyBlockUnaligned(ref Unsafe.As<T, byte>(ref MemoryMarshal.GetArrayDataReference(neu)), ref Unsafe.As<T, byte>(ref MemoryMarshal.GetArrayDataReference(_array)), ordinal);
				_array = neu;
			}

		Alive.DoublyLink(ref adjes, ordinal);
		return ref _array[ordinal];
	}

	public void Invalidate(ref Adjes<T> adjes, uint ordinal)
	{
		Alive.DoublyUnlink(ref adjes, ordinal);
		Dead.DoublyLink(ref adjes, ordinal);
	}

	internal string ToDebugString() => $"Count: {Alive.Count} of {_array.Length} (-{Dead.Count})";
}