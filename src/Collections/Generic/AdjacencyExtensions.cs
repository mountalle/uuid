namespace System.Collections.Generic;

using Runtime.CompilerServices;

public static class AdjacencyExtensions
{
	public static uint Append<T>(this ref (T[] array, uint cursor) source)
	{
		var ordinal = source.cursor++;
		if (ordinal >= source.array.Length) Array.Resize(ref source.array, unchecked((int)(ordinal << 1)));
		return ordinal;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static ref Adjes<T> At<T>(this ref Adjes<T> source, uint ordinal) where T : struct => ref Unsafe.As<T, Adjes<T>>(ref Unsafe.Add(ref Unsafe.As<Adjes<T>, T>(ref source), ordinal));

	//public static ref T Origin<T>(this T[] source) => ref System.Runtime.InteropServices.MemoryMarshal.GetArrayDataReference(source);
	public static void SinglyLink<T>(this ref Tabulae<T> source, ref uint next, uint ordinal) where T : struct
	{
		if (Interlocked.Increment(ref source.Count) == 1) source.First = source.Last = ordinal;
		else
		{
			Unsafe.Add(ref next, source.Last) = source.Last = ordinal;
		}
	}

	public static void DoublyLink<T>(this ref Tabulae<T> source, ref Adjes<T> adjes, uint ordinal) where T : struct
	{
		if (Interlocked.Increment(ref source.Count) == 1) source.First = source.Last = ordinal;
		else
		{
			adjes.At(ordinal).Previous = source.Last;
			adjes.At(source.Last).Next = source.Last = ordinal;
		}
	}

	public static void DoublyUnlink<T>(this ref Tabulae<T> source, ref Adjes<T> adjes, uint ordinal) where T : struct
	{
		switch (Interlocked.Decrement(ref source.Count))
		{
			case uint.MaxValue:
				source.Count = 0;
				throw new IndexOutOfRangeException();
			case 0: return;
			default:
				if (ordinal == source.First) source.First = adjes.At(ordinal).Next;
				else if (ordinal == source.Last) source.Last = adjes.At(ordinal).Previous;
				else
				{
					ref var itemAdjes = ref adjes.At(ordinal);
					adjes.At(itemAdjes.Previous).Next = itemAdjes.Next;
					adjes.At(itemAdjes.Next).Previous = itemAdjes.Previous;
				}

				return;
		}
	}

	public static uint DoublyPop<T>(ref this Tabulae<T> source, ref Adjes<T> adjes) where T : struct
	{
		switch (source.Count)
		{
			case 0:
				return uint.MaxValue;
			case 1:
				Interlocked.Decrement(ref source.Count);
				return source.First;
			default:
				Interlocked.Decrement(ref source.Count);
				var last = source.Last;
				source.Last = adjes.At(last).Previous;
				return last;
		}
	}
}