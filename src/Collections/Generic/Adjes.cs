namespace System.Collections.Generic;

using Runtime.CompilerServices;

public struct Tabulae<T> where T : struct
{
	public uint Count, First, Last;

	public static int ItemSize => Unsafe.SizeOf<T>();
	public void Deconstruct(out uint cursor, out uint count) => (cursor, count) = (First, Count);
	public void Deconstruct(out uint cursor, out uint count, out uint first) => (cursor, count, first) = (First, Count, First);
}

public struct Adjes<T> where T : struct
{
	public static int ItemSize => Unsafe.SizeOf<T>();
	public uint Next, Previous;
}

