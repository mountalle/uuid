namespace System;

using Reflection;

[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
public sealed class AssemblyLoadContextOnUnloadAttribute(string scope, Type type, string methodName) : Attribute
{
	public string Scope { get; } = scope;
	public Type Type { get; } = type;
	public string MethodName { get; } = methodName;
	public MethodInfo? Method => Type.GetMethod(MethodName, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
}