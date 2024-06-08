namespace System;

using Diagnostics;
using Numerics;
using Runtime.CompilerServices;
using Runtime.InteropServices;
using Runtime.Intrinsics;
using Runtime.Intrinsics.X86;

[StructLayout(LayoutKind.Explicit)]
[DebuggerDisplay("{Lo}-{Hi}")]
public readonly struct Uuid : IEquatable<Uuid>, IComparable<Uuid>
{
	public const uint Size = 16U;

	[FieldOffset(0)] public readonly Vector128<byte> Vector;
	[FieldOffset(0)] public readonly Guid Guid;

	[FieldOffset(0)] public readonly ulong Lo;

	[FieldOffset(8)] public readonly ulong Hi;

	[FieldOffset(0)] private readonly byte _0;

	[FieldOffset(0)] private readonly uint _a;
	[FieldOffset(4)] private readonly uint _b;
	[FieldOffset(8)] private readonly uint _c;
	[FieldOffset(12)] private readonly uint _d;

	public bool IsEmpty => Lo == 0UL && Hi == 0UL;

	public byte this[int index]
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Unsafe.Add(ref Unsafe.As<ulong, byte>(ref Unsafe.AsRef(in Lo)), index & 0XF);
	}

	public int CompareTo(Uuid other)
	{
		if (Sse2.IsSupported)
		{
			var matches = (uint)Sse2.MoveMask(Sse2.CompareEqual(Vector, other.Vector));
			if (matches == ushort.MaxValue) return 0;
			var byteOffset = BitOperations.TrailingZeroCount(~matches);
			return this[byteOffset] - other[byteOffset];
		}

		return Lo == other.Lo ? Hi.CompareTo(other.Hi) : Lo > other.Lo ? 1 : -1;
	}

	public bool Equals(Uuid other) => Sse2.IsSupported ? ushort.MaxValue == (uint)Sse2.MoveMask(Sse2.CompareEqual(Vector, other.Vector)) : Lo == other.Lo && Hi == other.Hi;

	public override bool Equals(object? obj) => obj is Uuid other && Equals(other);

	public override int GetHashCode() => HashCode.Combine(_a, _b, _c, _d);

	public override string ToString() => ToString(false);

	public string ToString(bool simple)
	{
		Span<char> span = stackalloc char[44];
		Unsafe<byte> uuid = new(in _0);
	
		Unsafe<char> x = span;
		x.Append(('(', '0', 'x'));
		for (int i = 7, b, v; i > -1; i--) x.Append(((char)(((v = (b = uuid[i]) >> 4) > 9 ? 55 : 48) + v), (char)(((v = b & 15) > 9 ? 55 : 48) + v)));
		x.Append(('U', 'L', ',', ' ', '0', 'x'));
		for (int i = 15, b, v; i > 7; i--) x.Append(((char)(((v = (b = uuid[i]) >> 4) > 9 ? 55 : 48) + v), (char)(((v = b & 15) > 9 ? 55 : 48) + v)));
		x.Append(('U', 'L', ')'));
	
		return simple ? new(span[1..^1]) : new(span);
	}
	public string ToJavaScriptString(bool simple)
	{
		Span<char> span = stackalloc char[42];
		Unsafe<byte> uuid = new(in _0);
	
		Unsafe<char> x = span;
		x.Append(('(', '0', 'x'));
		for (int i = 7, b, v; i > -1; i--) x.Append(((char)(((v = (b = uuid[i]) >> 4) > 9 ? 55 : 48) + v), (char)(((v = b & 15) > 9 ? 55 : 48) + v)));
		x.Append(('n', ',', ' ', '0', 'x'));
		for (int i = 15, b, v; i > 7; i--) x.Append(((char)(((v = (b = uuid[i]) >> 4) > 9 ? 55 : 48) + v), (char)(((v = b & 15) > 9 ? 55 : 48) + v)));
		x.Append(('n', ')'));
	
		return simple ? new(span[1..^1]) : new(span);
	}
	
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Uuid NewUuid() => Unsafe.BitCast<Guid, Uuid>(Guid.NewGuid());


	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static implicit operator Uuid((ulong lo, ulong hi) tuple) => Unsafe.As<(ulong, ulong), Uuid>(ref tuple);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static implicit operator Uuid(Guid guid) => Unsafe.As<Guid, Uuid>(ref guid);

	public static bool operator ==(Uuid left, Uuid right) => Sse2.IsSupported ? ushort.MaxValue == (uint)Sse2.MoveMask(Sse2.CompareEqual(left.Vector, right.Vector)) : left.Lo == right.Lo && left.Hi == right.Hi;

	public static bool operator !=(Uuid left, Uuid right) => Sse2.IsSupported ? ushort.MaxValue != (uint)Sse2.MoveMask(Sse2.CompareEqual(left.Vector, right.Vector)) : left.Lo != right.Lo || left.Hi != right.Hi;

	public static bool operator <(Uuid left, Uuid right)
	{
		if (Sse2.IsSupported)
		{
			var matches = (uint)Sse2.MoveMask(Sse2.CompareEqual(left.Vector, right.Vector));
			if (matches == ushort.MaxValue) return false;
			var byteOffset = BitOperations.TrailingZeroCount(~matches);
			return left[byteOffset] < right[byteOffset];
		}

		return left.Lo < right.Lo || left.Hi < right.Hi && left.Lo == right.Lo;
	}

	public static bool operator >(Uuid left, Uuid right)
	{
		if (Sse2.IsSupported)
		{
			var matches = (uint)Sse2.MoveMask(Sse2.CompareEqual(left.Vector, right.Vector));
			if (matches == ushort.MaxValue) return false;
			var byteOffset = BitOperations.TrailingZeroCount(~matches);
			return left[byteOffset] > right[byteOffset];
		}

		return left.Lo > right.Lo || left.Hi > right.Hi && left.Lo == right.Lo;
	}

	public static int BinarySearch(ulong[] uuids, Uuid value)
	{
		ref var values = ref Unsafe.As<ulong, Uuid>(ref MemoryMarshal.GetArrayDataReference(uuids));
		int lo = 0, hi = (uuids.Length >> 1) - 1, offset;

		for (Uuid comparand; lo <= hi;)
			if (value.Hi == (comparand = Unsafe.Add(ref values, offset = unchecked((int)(((uint)hi + (uint)lo) >> 1)))).Hi)
				if (value.Lo == comparand.Lo) return offset;
				else if (value.Lo > comparand.Lo) lo = offset + 1;
				else hi = offset - 1;
			else if (value.Hi > comparand.Hi) lo = offset + 1;
			else hi = offset - 1;

		return ~lo;
	}

	public static int UnsafeBinarySearch<T>(T[] source, Uuid value) where T : struct
	{
		Unsafe<T> values = source;
		int lo = 0, hi = (source.Length >> 1) - 1, offset;

		for (Uuid comparand; lo <= hi;)
			if (value.Hi == (comparand = values.CastThis<Uuid>(offset = unchecked((int)(((uint)hi + (uint)lo) >> 1)))).Hi)
				if (value.Lo == comparand.Lo) return offset;
				else if (value.Lo > comparand.Lo) lo = offset + 1;
				else hi = offset - 1;
			else if (value.Hi > comparand.Hi) lo = offset + 1;
			else hi = offset - 1;

		return ~lo;
	}

	public void Deconstruct(out ulong lo, out ulong hi) => (lo, hi) = (Lo, Hi);
}
