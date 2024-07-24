namespace System.Collections.Generic;

using Runtime.CompilerServices;
using Runtime.ConstrainedExecution;
using Runtime.InteropServices;

public class UnmanagedMap<TKey, TValue> : CriticalFinalizerObject where TKey : unmanaged
{
	protected Buffer Store;

	public static readonly uint KeySize = (uint)Unsafe.SizeOf<TKey>();

	public UnmanagedMap(uint capacity)
	{
		if (capacity < 1) throw new ArgumentOutOfRangeException(nameof(capacity));
		Store = new(capacity);
	}

	~UnmanagedMap() => Store.Dispose();

	protected struct Buffer
	{
		private nint _bytes;
		private uint _capacity, _keyOffset, _trashOffset;
		public (TValue[] array, uint cursor) Values;


		public Unsafe<uint> Ordinals => new(_bytes);
		public Unsafe<TKey> Keys => new(_bytes + unchecked((nint)_keyOffset));

		public ref InlinedTrashBin TrashBin => ref new Unsafe<InlinedTrashBin>(_bytes + unchecked((nint)_trashOffset)).Value;

		public Buffer(uint capacity)
		{
			var (ordinals, keys, trash) = Bytes(_capacity = capacity);
			_bytes = Marshal.AllocHGlobal((nint)((_trashOffset = (_keyOffset = ordinals) + keys) + trash));
			Values = (new TValue[capacity], 0);
			TrashBin.Clear(trash);
		}

		public static (uint ordinals, uint keys, uint trash) Bytes(uint capacity) => (capacity << 3, capacity * KeySize, InlinedTrashBin.BytesFor(capacity));

		public void Expand()
		{
			var sourceCapacity = _capacity;
			var (ordinals, keys, trash) = Bytes(_capacity <<= 1);

			var sourceOrdinals = Ordinals;
			var sourceKeys = Keys;
			var old = _bytes;

			_bytes = Marshal.AllocHGlobal((nint)((_trashOffset = (_keyOffset = ordinals) + keys) + trash));
			sourceOrdinals.BlockCopyTo(Ordinals, sourceCapacity);
			sourceKeys.BlockCopyTo(Keys, sourceCapacity);
			Marshal.FreeHGlobal(old);
			TrashBin.Clear(trash);
		}

		public void Clear(bool defaultify)
		{
			if (defaultify) Values.array.AsSpan(0, (int)Values.cursor).Fill(default!);
			Values.cursor = 0;
			TrashBin.Clear(InlinedTrashBin.BytesFor(_capacity));
		}

		public void Dispose()
		{
			if (Interlocked.Exchange(ref _bytes, 0) is not 0 and var handle) Marshal.FreeHGlobal(handle);
		}
	}
}