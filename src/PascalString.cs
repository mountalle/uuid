namespace System;

using Diagnostics;
using Runtime.CompilerServices;
using Runtime.InteropServices;
using Text;

[DebuggerDisplay("{ToString()}")]
public struct PascalString : IComparable<PascalString>
{
	public const int ByteSize = 256, MaxLength = ByteSize - 1, LongSize = ByteSize >> 3;

	private Ray _ray;

	public int Length => _ray.Value;

	public PascalString(ReadOnlySpan<byte> utf8)
	{
		if (utf8.Length > MaxLength) utf8 = utf8[..MaxLength];
		_ray.Value = unchecked((byte)utf8.Length);
		utf8.CopyTo(_ray.Span);
	}

	public ReadOnlySpan<byte> Span => _ray.Span;
	public byte this[int index] => _ray[index & 0xff];

	[InlineArray(ByteSize)]
	private struct Ray
	{
		public byte Value;
		public Unsafe<ulong> Origin => new Unsafe<byte>(ref Value).As<ulong>();
		public Span<byte> Span => MemoryMarshal.CreateSpan(ref Unsafe.Add(ref Value, 1), Value);
	}

	public int CompareTo(PascalString other)
	{
		var r = Length - other.Length;
		if (r != 0) return r;
		Unsafe<ulong> a = _ray.Origin, b = other._ray.Origin;
		for (var i = 0; i < LongSize; i++)
		{
			r = a[i].CompareTo(b[i]);
			if (r != 0) return r;
		}
		return 0;
	}


	public override string ToString() => Encoding.UTF8.GetString(_ray.Span);

	public static implicit operator PascalString(string content)
	{
		if (content.Length > MaxLength) content = content[..MaxLength];

		Span<byte> span = stackalloc byte[ByteSize << 1];
		var len = Encoding.UTF8.GetBytes(content, span.Slice(1)) & 0xFF;
		var res = new PascalString();
		res._ray.Value = unchecked((byte)len);
		span[..len].CopyTo(res._ray.Span);
		return res;
	}
}