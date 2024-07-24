namespace System.Collections.Generic;

using Runtime.CompilerServices;

public ref struct Iterator<T>
{
	private Iteration _iteration;
	private readonly unsafe delegate*<ref Iteration, T> _select;

	public unsafe Iterator(Iteration iteration, delegate*<ref Iteration, T> select)
	{
		_iteration = iteration;
		_select = select;
	}

	public bool MoveNext()
	{
		unsafe
		{
			var move = _iteration.Move;
			return move != null && move(ref _iteration);
		}
	}


	public T Current
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get
		{
			unsafe
			{
				return _select(ref _iteration);
			}
		}
	}
}

public ref struct RefIterator<T>
{
	private Iteration _iteration;
	private readonly unsafe delegate*<ref Iteration, ref T> _select;

	public unsafe RefIterator(Iteration iteration, delegate*<ref Iteration, ref T> select)
	{
		_iteration = iteration;
		_select = select;
	}

	public bool MoveNext()
	{
		unsafe
		{
			var move = _iteration.Move;
			return move != null && move(ref _iteration);
		}
	}


	public ref T Current
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get
		{
			unsafe
			{
#pragma warning disable CS9084 
				return ref _select(ref _iteration);
			}
		}
	}
}