namespace System;

public sealed class AliasAttribute(string alias, string? scope = default, string? options = default) : Attribute
{
	public string Alias { get; } = alias;
	public string? Scope { get; } = scope;
	public string? Options { get; } = options;
}