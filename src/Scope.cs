namespace System;

public ref struct Scope<T>
{
	private readonly ref T _store;
	private readonly T _stored;

	public Scope(ref T store, T value)
	{
		_store = ref store;
		_stored = store;
		store = value;
	}

	public void Dispose() => _store = _stored;
}