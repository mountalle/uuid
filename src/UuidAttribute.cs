namespace System;

public sealed class UuidAttribute(ulong lo, ulong hi) : Attribute
{
	public Uuid Uuid => (lo, hi);
}