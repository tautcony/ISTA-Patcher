// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: Copyright 2026 TautCony

namespace ISTestA;

using System.Reflection;
using ISTAlter.Utils;

public class MethodInfoWrapperTests
{
    private static MethodInfo GetSampleMethod() =>
        typeof(object).GetMethod(nameof(object.ToString))!;

    [Test]
    public void Name_ReturnsCustomName()
    {
        var wrapper = new MethodInfoWrapper(GetSampleMethod(), [], "MyCustomName");
        Assert.That(wrapper.Name, Is.EqualTo("MyCustomName"));
    }

    [Test]
    public void GetCustomAttributes_Bool_ReturnsProvidedArray()
    {
        var attrs = new Attribute[] { new ObsoleteAttribute("old") };
        var wrapper = new MethodInfoWrapper(GetSampleMethod(), attrs, "X");

        var result = wrapper.GetCustomAttributes(inherit: false);

        Assert.That(result, Is.EquivalentTo(attrs));
    }

    [Test]
    public void GetCustomAttributes_WithType_FiltersCorrectly()
    {
        var attrs = new Attribute[] { new ObsoleteAttribute("old"), new CLSCompliantAttribute(true) };
        var wrapper = new MethodInfoWrapper(GetSampleMethod(), attrs, "X");

        var result = wrapper.GetCustomAttributes(typeof(ObsoleteAttribute), inherit: false);

        Assert.That(result, Has.Length.EqualTo(1));
        Assert.That(result[0], Is.InstanceOf<ObsoleteAttribute>());
    }

    [Test]
    public void IsDefined_MatchingType_ReturnsTrue()
    {
        var attrs = new Attribute[] { new ObsoleteAttribute("old") };
        var wrapper = new MethodInfoWrapper(GetSampleMethod(), attrs, "X");

        Assert.That(wrapper.IsDefined(typeof(ObsoleteAttribute), inherit: false), Is.True);
    }

    [Test]
    public void IsDefined_NonMatchingType_ReturnsFalse()
    {
        var wrapper = new MethodInfoWrapper(GetSampleMethod(), [], "X");

        Assert.That(wrapper.IsDefined(typeof(ObsoleteAttribute), inherit: false), Is.False);
    }

    [Test]
    public void DeclaringType_ForwardsToBase()
    {
        var baseMethod = GetSampleMethod();
        var wrapper = new MethodInfoWrapper(baseMethod, [], "X");

        Assert.That(wrapper.DeclaringType, Is.EqualTo(baseMethod.DeclaringType));
    }

    [Test]
    public void ReflectedType_ForwardsToBase()
    {
        var baseMethod = GetSampleMethod();
        var wrapper = new MethodInfoWrapper(baseMethod, [], "X");

        Assert.That(wrapper.ReflectedType, Is.EqualTo(baseMethod.ReflectedType));
    }

    [Test]
    public void MethodHandle_ForwardsToBase()
    {
        var baseMethod = GetSampleMethod();
        var wrapper = new MethodInfoWrapper(baseMethod, [], "X");

        Assert.That(wrapper.MethodHandle, Is.EqualTo(baseMethod.MethodHandle));
    }

    [Test]
    public void Attributes_ForwardsToBase()
    {
        var baseMethod = GetSampleMethod();
        var wrapper = new MethodInfoWrapper(baseMethod, [], "X");

        Assert.That(wrapper.Attributes, Is.EqualTo(baseMethod.Attributes));
    }

    [Test]
    public void GetBaseDefinition_ForwardsToBase()
    {
        var baseMethod = GetSampleMethod();
        var wrapper = new MethodInfoWrapper(baseMethod, [], "X");

        Assert.That(wrapper.GetBaseDefinition(), Is.EqualTo(baseMethod.GetBaseDefinition()));
    }

    [Test]
    public void ReturnTypeCustomAttributes_ForwardsToBase()
    {
        var baseMethod = GetSampleMethod();
        var wrapper = new MethodInfoWrapper(baseMethod, [], "X");

        Assert.That(wrapper.ReturnTypeCustomAttributes, Is.EqualTo(baseMethod.ReturnTypeCustomAttributes));
    }

    [Test]
    public void GetParameters_ForwardsToBase()
    {
        var baseMethod = GetSampleMethod();
        var wrapper = new MethodInfoWrapper(baseMethod, [], "X");

        Assert.That(wrapper.GetParameters(), Is.EquivalentTo(baseMethod.GetParameters()));
    }

    [Test]
    public void GetMethodImplementationFlags_ForwardsToBase()
    {
        var baseMethod = GetSampleMethod();
        var wrapper = new MethodInfoWrapper(baseMethod, [], "X");

        Assert.That(wrapper.GetMethodImplementationFlags(), Is.EqualTo(baseMethod.GetMethodImplementationFlags()));
    }

    [Test]
    public void Invoke_CallsUnderlyingMethod()
    {
        var baseMethod = GetSampleMethod();
        var wrapper = new MethodInfoWrapper(baseMethod, [], "X");

        var result = wrapper.Invoke(42, BindingFlags.Default, null, null, null);

        Assert.That(result, Is.EqualTo("42"));
    }
}
