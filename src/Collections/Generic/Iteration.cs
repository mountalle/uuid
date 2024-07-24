namespace System.Collections.Generic;

using Runtime.CompilerServices;

public struct Iteration
{
	private readonly object _store;
	internal unsafe delegate*<ref Iteration, bool> Move;
	public uint Cursor, Extra;

	public unsafe Iteration(object store, delegate*<ref Iteration, bool> move)
	{
		_store = store;
		Move = move;
		Cursor = Extra = 0;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public TStore As<TStore>() where TStore : class => Unsafe.As<TStore>(_store);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public ref TStore Unbox<TStore>() where TStore : struct => ref Unsafe.Unbox<TStore>(_store);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool Break()
	{
		unsafe
		{
			Move = null;
		}

		return false;
	}
}