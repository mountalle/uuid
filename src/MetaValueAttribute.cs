namespace System;

public sealed class MetaValueAttribute(string name, string value, string? options = null) : Attribute
{
	public string Name { get; } = name;
	public string Value { get; } = value;
	public string? Options { get; } = options;
}