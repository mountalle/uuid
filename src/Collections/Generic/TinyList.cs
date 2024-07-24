namespace System.Collections.Generic;

using Collections;
using Diagnostics;
using Runtime.CompilerServices;
using Runtime.InteropServices;

[DebuggerDisplay("Count: {Count}")]
[StructLayout(LayoutKind.Sequential)]
public struct TinyList<T> : IReadOnlyCollection<T>
{
	private const int Capacity = 8;

	private int _count;
	private T[]? _buffer;
	private Ray16 _ray;

	[InlineArray(Capacity)]
	private struct Ray16
	{
		public T Value;
	}

	public bool IsEmpty => _count == 0;

	public Span<T> Span
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => _buffer is null ? MemoryMarshal.CreateSpan(ref _ray.Value, _count) : new Span<T>(_buffer, 0, _count);
	}

	public int Count => _count;

	/// <summary>
	/// First element of list.
	/// </summary>
	/// <exception cref="IndexOutOfRangeException">If list is empty.</exception>
	public ref T First
	{
		get
		{
			if (_count < 1) throw new IndexOutOfRangeException();
			return ref _buffer is null ? ref MemoryMarshal.CreateSpan(ref _ray.Value, 1)[0] : ref _buffer[0];
		}
	}

	/// <summary>
	/// Topmost element on stack.
	/// </summary>
	// <exception cref="IndexOutOfRangeException">If list is empty.</exception>
	public ref T Last
	{
		get
		{
			var index = _count - 1;
			if (index < 0) throw new IndexOutOfRangeException();
			return ref _buffer is null ? ref MemoryMarshal.CreateSpan(ref _ray.Value, _count)[index] : ref _buffer[index];
		}
	}

	/// <summary>
	/// Appends the list. Use <see cref="Inflate"/> for value types.
	/// </summary>
	/// <param name="item">Item to append.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Add(T item) => Inflate() = item;

	public void Add(ReadOnlySpan<T> span)
	{
		if (_count + span.Length < Capacity) span.CopyTo(MemoryMarshal.CreateSpan(ref _ray.Value, Capacity));
		else
		{
			if (_buffer is null) MemoryMarshal.CreateSpan(ref _ray.Value, Capacity).CopyTo(_buffer = new T[_count + span.Length + Capacity]);
			else if (_buffer.Length <= _count + span.Length) Array.Resize(ref _buffer, _count + span.Length + Capacity);
			span.CopyTo(new Span<T>(_buffer, _count, span.Length));
		}

		_count += span.Length;
	}

	/// <summary>
	/// Inflates the list.
	/// </summary>
	public ref T Inflate()
	{
		var index = _count++;
		if (index < Capacity) return ref MemoryMarshal.CreateSpan(ref _ray.Value, _count)[index];

		if (_buffer is null) MemoryMarshal.CreateSpan(ref _ray.Value, Capacity).CopyTo(_buffer = new T[Capacity << 1]);
		else if (_buffer.Length == index) Array.Resize(ref _buffer, index + Capacity);

		return ref _buffer[index];
	}

	/// <summary>
	/// Deflates the list.
	/// </summary>
	/// <exception cref="IndexOutOfRangeException">If list is empty.</exception>
	public void Deflate()
	{
		if (--_count < 0) throw new IndexOutOfRangeException();
	}

	/// <summary>
	/// Deflates the list.
	/// </summary>
	/// <exception cref="IndexOutOfRangeException">If list is empty.</exception>
	public ref T Pop()
	{
		var index = --_count;
		if (index >= 0) return ref _buffer is null ? ref MemoryMarshal.CreateSpan(ref _ray.Value, _count)[index] : ref _buffer[index];
		_count = 0;
		throw new IndexOutOfRangeException();
	}

	/// <summary>
	/// Zeroes list length. Does nothing with stored items. Thus follow <c>foreach(ref var x in list.Span) x=default;</c> by <see cref="Clear()"/> to release slots.
	/// </summary>
	public void Clear() => _count = 0;

	/// <summary>
	/// Returns ref on element with given positive or negative index.
	/// </summary>
	/// <param name="index">If negative index from end, otherwise from start.</param>
	/// <exception cref="IndexOutOfRangeException"></exception>
	public ref T this[int index]
	{
		get
		{
			if (index < 0) index += _count;
			if (index >= _count) throw new IndexOutOfRangeException();
			return ref _buffer is null ? ref MemoryMarshal.CreateSpan(ref _ray.Value, _count)[index] : ref _buffer[index];
		}
	}

	IEnumerator<T> IEnumerable<T>.GetEnumerator()
	{
		for (var i = 0; i < _count; i++) yield return this[i];
	}

	// ReSharper disable once NotDisposedResourceIsReturned
	IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable<T>)this).GetEnumerator();
}