namespace System.Text;

public ref struct Cancer
{
	private Span<char> _span;
	private int _cursor;

	public Cancer(Span<char> span)
	{
		_span = span;
		_cursor = span.Length;
	}

	public int Length => _span.Length - _cursor;

	public bool IsEmpty => _cursor == _span.Length;

	private Span<char> Ensure(int length)
	{
		var start = _cursor - length;
		if (start < 0)
		{
			var neu = new char[_span.Length - start];
			if (_cursor < _span.Length) _span[_cursor..].CopyTo(neu.AsSpan(-start));
			_cursor -= start;
			_span = neu;
			start = 0;
		}

		return _span.Slice(start, length);
	}

	public void Insert(char value)
	{
		Ensure(1)[0] = value;
		_cursor--;
	}

	public void Insert(ReadOnlySpan<char> value)
	{
		value.CopyTo(Ensure(value.Length));
		_cursor -= value.Length;
	}

	public void Insert(char head, ReadOnlySpan<char> value)
	{
		var span = Ensure(value.Length + 1);
		span[0] = head;
		value.CopyTo(span[1..]);
		_cursor -= span.Length;
	}

	public void Insert(ReadOnlySpan<char> value, char tail)
	{
		var span = Ensure(value.Length + 1);
		value.CopyTo(span);
		span[^1] = tail;
		_cursor -= span.Length;
	}

	public void Insert(int value)
	{
		var minus = value < 0;
		if (minus)
			if (value != int.MinValue) value = -value;
			else
			{
				Insert("-2147483648");
				return;
			}

		do
		{
			(value, var r) = Math.DivRem(value, 10);
			Insert(unchecked((char)(48 + r)));
		} while (value > 0);

		if (minus) Insert('-');
	}


	public ReadOnlySpan<char> ToReadOnlySpan() => _cursor >= _span.Length ? default(ReadOnlySpan<char>) : _span[_cursor..];

	public int DumpTo(Span<char> span)
	{
		if (_cursor >= _span.Length) return 0;
		var me = _span[_cursor..];
		me.CopyTo(span);
		return me.Length;
	}

	public void Deflate(int count = 1)
	{
		if (count < 1) return;
		var probe = _cursor + count;
		_cursor = probe <= _span.Length ? probe : _span.Length;
	}

	public override string ToString() => new(_span[_cursor..]);
}