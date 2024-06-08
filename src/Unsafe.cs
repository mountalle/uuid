namespace System;

using Runtime.CompilerServices;
using Runtime.InteropServices;

internal ref struct Unsafe<T> where T : struct
{
	public ref T Value;

	public uint SizeOf() => unchecked((uint)Unsafe.SizeOf<T>());

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	//public Unsafe(ref T @ref) => Value = ref @ref;
	public Unsafe(scoped ref readonly T @ref) => Value = ref Unsafe.AsRef(in @ref);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Unsafe(nint pointer)
	{
		unsafe
		{
			Value = ref Unsafe.As<byte, T>(ref *(byte*)pointer);
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Unsafe(nint pointer, int byteOffset)
	{
		unsafe
		{
			Value = ref Unsafe.As<byte, T>(ref *((byte*)pointer + byteOffset));
		}
	}

	public ref T this[int offset]
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => ref Unsafe.Add(ref Value, offset);
	}

	public ref T this[uint offset]
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => ref Unsafe.Add(ref Value, offset);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public ref TResult CastThis<TResult>(int index) where TResult : struct => ref Unsafe.As<T, TResult>(ref Unsafe.Add(ref Value, index));

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public ref TResult CastThis<TResult>(uint index) where TResult : struct => ref Unsafe.As<T, TResult>(ref Unsafe.Add(ref Value, index));

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public ref (uint next, uint prev) GetAdjes(uint index) => ref Unsafe.As<T, (uint next, uint prev)>(ref Unsafe.Add(ref Value, index));

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public ref (uint next, uint prev) GetAdjes(uint index, uint offset) => ref Unsafe.Add(ref Unsafe.As<T, (uint next, uint prev)>(ref Unsafe.Add(ref Value, index)), offset);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Span<T> CreateSpan(int start, int length) => MemoryMarshal.CreateSpan(ref Unsafe.Add(ref Value, start), length);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Span<T> CreateSpan(int length) => MemoryMarshal.CreateSpan(ref Value, length);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Span<T> CreateSpan(uint length) => MemoryMarshal.CreateSpan(ref Value, unchecked((int)length));

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public uint OrdinalOf(ref T item) => unchecked((uint)(Unsafe.ByteOffset(ref Value, ref item) / Unsafe.SizeOf<T>()));


	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Init(byte value, uint byteCount) => Unsafe.InitBlockUnaligned(ref Unsafe.As<T, byte>(ref Value), value, byteCount);

	internal nint Offset
	{
		get
		{
			unsafe
			{
				return (nint)Unsafe.AsPointer(ref Value);
			}
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void BlockCopyTo(Unsafe<T> destination, int count) => Unsafe.CopyBlockUnaligned(ref Unsafe.As<T, byte>(ref destination.Value), ref Unsafe.As<T, byte>(ref Value), unchecked((uint)(count * Unsafe.SizeOf<T>())));

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void BlockCopyTo(Unsafe<T> destination, uint count) => Unsafe.CopyBlockUnaligned(ref Unsafe.As<T, byte>(ref destination.Value), ref Unsafe.As<T, byte>(ref Value), unchecked((uint)(count * Unsafe.SizeOf<T>())));

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void BlockCopyTo(Unsafe<T> destination, uint destinationOffset, int count) => Unsafe.CopyBlockUnaligned(ref Unsafe.As<T, byte>(ref Unsafe.Add(ref destination.Value, destinationOffset)), ref Unsafe.As<T, byte>(ref Value), unchecked((uint)(count * Unsafe.SizeOf<T>())));

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void BlockCopyTo(Unsafe<T> destination, uint destinationOffset, uint count) => Unsafe.CopyBlockUnaligned(ref Unsafe.As<T, byte>(ref Unsafe.Add(ref destination.Value, destinationOffset)), ref Unsafe.As<T, byte>(ref Value), unchecked((uint)(count * Unsafe.SizeOf<T>())));

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Unsafe<TResult> As<TResult>() where TResult : unmanaged => new(ref Unsafe.As<T, TResult>(ref Value));

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Unsafe<TResult> As<TResult>(int offset) where TResult : unmanaged => new(ref Unsafe.As<T, TResult>(ref Unsafe.Add(ref Value, offset)));

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Unsafe<TResult> As<TResult>(uint offset) where TResult : unmanaged => new(ref Unsafe.As<T, TResult>(ref Unsafe.Add(ref Value, offset)));


	public void Append(T value)
	{
		Value = value;
		Value = ref Unsafe.Add(ref Value, 1);
	}

	public void Append(scoped ReadOnlySpan<T> span)
	{
		Unsafe.CopyBlockUnaligned(ref Unsafe.As<T, byte>(ref Value), ref Unsafe.As<T, byte>(ref MemoryMarshal.GetReference(span)), unchecked((uint)(span.Length * Unsafe.SizeOf<T>())));
		Value = ref Unsafe.Add(ref Value, span.Length);
	}

	public void Append((T, T) value)
	{
		Unsafe.CopyBlockUnaligned(ref Unsafe.As<T, byte>(ref Value), ref Unsafe.As<(T, T), byte>(ref value), unchecked((uint)(2 * Unsafe.SizeOf<T>())));
		Value = ref Unsafe.Add(ref Value, 2);
	}

	public void Append((T, T, T) value)
	{
		Unsafe.CopyBlockUnaligned(ref Unsafe.As<T, byte>(ref Value), ref Unsafe.As<(T, T, T), byte>(ref value), unchecked((uint)(3 * Unsafe.SizeOf<T>())));
		Value = ref Unsafe.Add(ref Value, 3);
	}

	public void Append(in (T, T, T, T, T) value)
	{
		Unsafe.CopyBlockUnaligned(ref Unsafe.As<T, byte>(ref Value), ref Unsafe.As<(T, T, T, T, T), byte>(ref Unsafe.AsRef(in value)), unchecked((uint)(5 * Unsafe.SizeOf<T>())));
		Value = ref Unsafe.Add(ref Value, 5);
	}

	public void Append(in (T, T, T, T, T, T) value)
	{
		Unsafe.CopyBlockUnaligned(ref Unsafe.As<T, byte>(ref Value), ref Unsafe.As<(T, T, T, T, T, T), byte>(ref Unsafe.AsRef(in value)), unchecked((uint)(6 * Unsafe.SizeOf<T>())));
		Value = ref Unsafe.Add(ref Value, 6);
	}


	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static implicit operator Unsafe<T>(T[] array) => new(ref MemoryMarshal.GetArrayDataReference(array));

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static implicit operator Unsafe<T>(Span<T> span) => new(ref MemoryMarshal.GetReference(span));

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static implicit operator Unsafe<T>(ReadOnlySpan<T> span) => new(ref MemoryMarshal.GetReference(span));

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static implicit operator Unsafe<T>(string? span) => new(ref MemoryMarshal.GetReference(MemoryMarshal.Cast<char, T>(span.AsSpan())));

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Unsafe<T> operator +(Unsafe<T> @unsafe, int offset) => new(ref Unsafe.Add(ref @unsafe.Value, offset));

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Unsafe<T> operator +(Unsafe<T> @unsafe, nint offset) => new(ref Unsafe.Add(ref @unsafe.Value, offset));

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Unsafe<T> operator +(Unsafe<T> @unsafe, ulong offset) => new(ref Unsafe.Add(ref @unsafe.Value, (nint)offset));

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Unsafe<T> operator +(Unsafe<T> @unsafe, uint offset) => new(ref Unsafe.Add(ref @unsafe.Value, offset));


	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Unsafe<T> operator -(Unsafe<T> @unsafe, int offset) => new(ref Unsafe.Subtract(ref @unsafe.Value, offset));

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Unsafe<T> operator -(Unsafe<T> @unsafe, uint offset) => new(ref Unsafe.Subtract(ref @unsafe.Value, offset));


	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Unsafe<T> operator ++(Unsafe<T> @unsafe) => new(ref Unsafe.Add(ref @unsafe.Value, 1));

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Unsafe<T> operator --(Unsafe<T> @unsafe) => new(ref Unsafe.Subtract(ref @unsafe.Value, 1));

	public static int operator -(Unsafe<T> left, Unsafe<T> right) => (int)(Unsafe.ByteOffset(ref right.Value, ref left.Value) / Unsafe.SizeOf<T>());

	public static Span<T> operator -(Unsafe<T> left, Span<T> right)
	{
		ref var start = ref MemoryMarshal.GetReference(right);
		return MemoryMarshal.CreateSpan(ref start, (int)(Unsafe.ByteOffset(ref start, ref left.Value) / Unsafe.SizeOf<T>()));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool operator >(Unsafe<T> left, Unsafe<T> right) => Unsafe.IsAddressGreaterThan(ref left.Value, ref right.Value);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool operator <(Unsafe<T> left, Unsafe<T> right) => Unsafe.IsAddressLessThan(ref left.Value, ref right.Value);

	public static bool operator ==(Unsafe<T> left, Unsafe<T> right) => Unsafe.AreSame(ref left.Value, ref right.Value);
	public static bool operator !=(Unsafe<T> left, Unsafe<T> right) => !Unsafe.AreSame(ref left.Value, ref right.Value);

	public static bool operator ==(Unsafe<T> left, nint right)
	{
		unsafe
		{
			return right == (nint)Unsafe.AsPointer(ref left.Value);
		}
	}

	public static bool operator !=(Unsafe<T> left, nint right)
	{
		unsafe
		{
			return right != (nint)Unsafe.AsPointer(ref left.Value);
		}
	}

	public override bool Equals(object? obj) => false;
	public override int GetHashCode() => 0;
}