namespace System;

using Reflection;

[AttributeUsage(AttributeTargets.Assembly)]
public sealed class AssemblyLoadContextOnLoadAttribute(Type type, string methodName) : Attribute
{
	public Type Type { get; } = type;
	public string MethodName { get; } = methodName;
	public MethodInfo? Method => Type.GetMethod(MethodName, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
}