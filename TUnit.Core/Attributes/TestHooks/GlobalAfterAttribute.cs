namespace TUnit.Core;

#pragma warning disable CS9113
[AttributeUsage(AttributeTargets.Method)]
public sealed class GlobalAfterAttribute(HookType hookType) : TUnitAttribute;