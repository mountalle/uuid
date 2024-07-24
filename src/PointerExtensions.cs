namespace System;

using Runtime.CompilerServices;

public static class PointerExtensions
{
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static ref T At<T>(this nint source) where T : unmanaged
	{
		unsafe
		{
			return ref *(T*)source;
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static ref T At<T>(this nuint source) where T : unmanaged
	{
		unsafe
		{
			return ref *(T*)source;
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static ref T At<T>(this nint source, int offset) where T : unmanaged
	{
		unsafe
		{
			return ref *((T*)source + offset);
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static ref T At<T>(this nint source, uint offset) where T : unmanaged
	{
		unsafe
		{
			return ref *((T*)source + offset);
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static ref T At<T>(this nuint source, int offset) where T : unmanaged
	{
		unsafe
		{
			return ref *((T*)source + offset);
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static ref T At<T>(this nuint source, uint offset) where T : unmanaged
	{
		unsafe
		{
			return ref *((T*)source + offset);
		}
	}
}