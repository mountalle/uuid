namespace System.Collections.Generic;

public class UuidMap<T>
{
	protected Buffer Buff;


	public UuidMap(uint capacity)
	{
		if (capacity < 1) throw new ArgumentOutOfRangeException(nameof(capacity));
		Buff = new(capacity);
	}

	public UuidMap(int capacity)
	{
		if (capacity is < 1) throw new ArgumentOutOfRangeException(nameof(capacity));
		Buff = new(unchecked((uint)capacity));
	}

	protected bool TryGetOrdinal(Uuid uuid, out uint ordinal)
	{
		Fail:
		ordinal = 0;
		return false;
	}

	public ref T this[Uuid uuid]
	{
		get { throw new NotImplementedException(); }
	}

	public bool ContainsKey(Uuid uuid)
	{
		throw new NotImplementedException();
	}

	public bool Add(Uuid uuid, T value)
	{
		if (Buff.Trash.TryRestore(out var index)) goto Insert;
		Insert:
		return true;
	}

	protected struct Buffer
	{
		public uint[] Array;
		public uint Cursor, Capacity;
		public (T[] array, uint cursor) Values;

		public Buffer(uint capacity)
		{
			Array = new uint[capacity * 5 + InlinedTrashBin.IntsFor(Capacity = capacity)];
			Values = (new T[capacity], 0);
			Cursor = 0;
		}

		public Unsafe<uint> Ordinals => new(Array);
		public Unsafe<Uuid> Keys => new Unsafe<uint>(Array, Capacity).As<Uuid>();

		public ref InlinedTrashBin Trash => ref new Unsafe<uint>(Array, Capacity * 5).As<InlinedTrashBin>().Value;
	}

	public ref struct Pair
	{
		internal UuidMap<T> Map;
		public Uuid Key;
		public ref T Value;
	}
}