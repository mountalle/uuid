namespace System.Collections;

using Runtime.CompilerServices;

public struct InlinedTrashBin
{
	public uint Count;

	public bool this[uint index]
	{
		get => (Unsafe.Add(ref Count, 1 + (index >>> 5)) & (1U << (int)(index & 0x1FU))) != 0U;
		set
		{
			ref var stored = ref Unsafe.Add(ref Count, 1 + (index >>> 5));
			if (value)
			{
				var mask = 1U << (int)(index & 0x1FU);
				if ((stored & mask) != 0U) return;
				stored |= mask;
				Count++;
			}
			else
			{
				var mask = ~(1U << (int)(index & 0x1FU));
				if ((stored | mask) == mask) return;
				stored &= mask;
				Count--;
			}
		}
	}

	public void Clear(uint byteLength) => new Unsafe<uint>(ref Count).Init(0, byteLength);

	public bool TryRestore(out uint index)
	{
		if (Count == 0)
		{
			index = 0;
			return false;
		}

		ref var mask = ref Count;
		for (index = 31;; index += 32)
			if ((mask = ref Unsafe.Add(ref mask, 1)) != 0)
				return !(this[index -= uint.LeadingZeroCount(mask)] = false);
	}

	/// <summary>
	/// If deletion mark at ordinal is true, sets it to false, decrements <see cref="Count"/> and returns true.
	/// </summary>
	/// <param name="ordinal">Ordinal of deletion mark.</param>
	/// <returns>True if mark was changed from true to false, otherwise false.</returns>
	public bool Restore(int ordinal)
	{
		ref var stored = ref Unsafe.Add(ref Count, 1 + (ordinal >>> 5));
		var mask = 1U << (ordinal & 0x1F);
		if ((stored & mask) == 0U) return false;
		stored &= ~mask;
		Count--;
		return true;
	}


	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static uint IntsFor(uint capacity) => ((capacity & 0x1FU) == 0 ? (capacity >>> 5) + 1U : (capacity >>> 5) + 2U) << 1;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static uint BytesFor(uint capacity) => ((capacity & 0x1FU) == 0 ? (capacity >>> 5) + 1U : (capacity >>> 5) + 2U) << 2;
}