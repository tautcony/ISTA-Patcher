// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: Copyright 2025 TautCony

namespace ISTAlter.Utils;

using System.Reflection;

/// <summary>
/// A wrapper class for MethodInfo that supports custom attributes and custom name.
/// </summary>
public class MethodInfoWrapper(MethodInfo baseMethod, Attribute[] customAttributes, string customName)
    : MethodInfo
{
    public override Attribute[] GetCustomAttributes(bool inherit)
    {
        // Return the custom attributes array directly
        // This works because Attribute[] can be implicitly converted to object[]
        return customAttributes;
    }

    public override Attribute[] GetCustomAttributes(Type attributeType, bool inherit)
    {
        // Filter and return matching attributes
        // Return as Attribute[] which can be implicitly converted to object[]
        return customAttributes.Where(attributeType.IsInstanceOfType).ToArray();
    }

    public override bool IsDefined(Type attributeType, bool inherit)
    {
        return customAttributes.Any(attributeType.IsInstanceOfType);
    }

    public override string Name => customName;

    public override Type DeclaringType => baseMethod.DeclaringType!;

    public override Type ReflectedType => baseMethod.ReflectedType!;

    public override RuntimeMethodHandle MethodHandle => baseMethod.MethodHandle;

    public override System.Reflection.MethodAttributes Attributes => baseMethod.Attributes;

    public override MethodInfo GetBaseDefinition() => baseMethod.GetBaseDefinition();

    public override ICustomAttributeProvider ReturnTypeCustomAttributes => baseMethod.ReturnTypeCustomAttributes;

    public override ParameterInfo[] GetParameters() => baseMethod.GetParameters();

    public override System.Reflection.MethodImplAttributes GetMethodImplementationFlags() => baseMethod.GetMethodImplementationFlags();

    public override object? Invoke(object? obj, BindingFlags invokeAttr, Binder? binder, object?[]? parameters, System.Globalization.CultureInfo? culture)
    {
        return baseMethod.Invoke(obj, invokeAttr, binder, parameters, culture);
    }
}
