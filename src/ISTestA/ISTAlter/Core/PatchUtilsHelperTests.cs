// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: Copyright 2026 TautCony

namespace ISTestA.ISTAlter.Core;

using dnlib.DotNet;
using dnlib.DotNet.Emit;
using global::ISTAlter.Core;
using global::ISTAlter.Utils;

public class PatchUtilsHelperTests
{
    private ModuleDefUser _module = null!;

    [SetUp]
    public void Setup()
    {
        _module = CreateModule("Target.dll", new Version(4, 56));
    }

    [TearDown]
    public void TearDown()
    {
        _module.Dispose();
    }

    [Test]
    public void AddPatchedAttribute_AddsAttributeTypeWithExpectedMembers()
    {
        var attributeType = PatchUtils.AddPatchedAttribute(_module);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(_module.Types, Does.Contain(attributeType));
            Assert.That(attributeType.FullName, Is.EqualTo("ISTAttributes.PatchedAttribute"));
            Assert.That(attributeType.Fields.Select(field => field.Name.String), Is.EquivalentTo(["key", "value"]));
            Assert.That(attributeType.Methods, Has.One.Matches<MethodDef>(method => method.Name == ".ctor"));
        }
    }

    [Test]
    public void SetPatchedMarkInner_AddsVersionMarkAndIsIdempotent()
    {
        PatchUtils.SetPatchedMarkInner(_module);
        var firstCount = _module.Assembly.CustomAttributes.Count;
        var version = PatchUtils.HavePatchedMark(_module);

        PatchUtils.SetPatchedMarkInner(_module);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(version, Is.Not.Null.And.Not.Empty);
            Assert.That(_module.Assembly.CustomAttributes, Has.Count.EqualTo(firstCount));
        }
    }

    [Test]
    public void HavePatchedMark_AssemblyMetadataAttribute_ReturnsVersion()
    {
        var ctor = (ICustomAttributeType)_module.Import(typeof(System.Reflection.AssemblyMetadataAttribute).GetConstructor([typeof(string), typeof(string)])!);
        _module.Assembly.CustomAttributes.Add(new CustomAttribute(ctor)
        {
            ConstructorArguments =
            {
                new CAArgument(_module.CorLibTypes.String, "Patched.Version"),
                new CAArgument(_module.CorLibTypes.String, "1.2.3-test"),
            },
        });

        var version = PatchUtils.HavePatchedMark(_module);

        Assert.That(version, Is.EqualTo("1.2.3-test"));
    }

    [TestCase(nameof(VersionedPatch), true)]
    [TestCase(nameof(FuturePatch), false)]
    [TestCase(nameof(ExpiredPatch), false)]
    [TestCase(nameof(UnboundedPatch), true)]
    public void IsVersionInRange_EvaluatesOpenAndClosedBounds(string methodName, bool expected)
    {
        var method = typeof(PatchUtilsHelperTests).GetMethod(methodName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

        var result = PatchUtils.IsVersionInRange(_module, method);

        Assert.That(result, Is.EqualTo(expected));
    }

    [Test]
    public void IsVersionInRange_NullPatcher_ReturnsTrue()
    {
        Assert.That(PatchUtils.IsVersionInRange(_module, patcher: null), Is.True);
    }

    [TestCase(nameof(VersionedPatch), "Target.dll", true)]
    [TestCase(nameof(VersionedPatch), "Other.dll", false)]
    [TestCase(nameof(NoLibraryNamePatch), "Target.dll", true)]
    public void IsPatchApplicable_EvaluatesLibraryName(string methodName, string moduleName, bool expected)
    {
        using var module = CreateModule(moduleName, new Version(4, 56));
        var method = typeof(PatchUtilsHelperTests).GetMethod(methodName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

        var result = PatchUtils.IsPatchApplicable(module, method);

        Assert.That(result, Is.EqualTo(expected));
    }

    [Test]
    public void PatchFunction_MatchingMethod_ReturnsOneWhenBodyChanges()
    {
        var type = AddType(_module, "Fixture", "PatchTarget");
        var method = AddStaticMethod(type, "GetValue", MethodSig.CreateStatic(_module.CorLibTypes.Int32));
        method.Body.Instructions.Add(OpCodes.Ldc_I4_0.ToInstruction());
        method.Body.Instructions.Add(OpCodes.Ret.ToInstruction());

        var patched = _module.PatchFunction(
            "Fixture.PatchTarget",
            "GetValue",
            "()System.Int32",
            target => target.ReturnOneMethod());

        using (Assert.EnterMultipleScope())
        {
            Assert.That(patched, Is.EqualTo(1));
            Assert.That(method.Body.Instructions[0].GetLdcI4Value(), Is.EqualTo(1));
        }
    }

    [Test]
    public void PatchFunction_MissingMethod_ReturnsZero()
    {
        var patched = _module.PatchFunction(
            "Fixture.Missing",
            "GetValue",
            "()System.Int32",
            target => target.ReturnOneMethod());

        Assert.That(patched, Is.Zero);
    }

    [Test]
    public void PatchGetter_MatchingPropertyGetter_ReturnsOneWhenBodyChanges()
    {
        var type = AddType(_module, "Fixture", "PropertyTarget");
        var getter = AddStaticMethod(type, "get_Enabled", MethodSig.CreateStatic(_module.CorLibTypes.Boolean));
        getter.Body.Instructions.Add(OpCodes.Ldc_I4_0.ToInstruction());
        getter.Body.Instructions.Add(OpCodes.Ret.ToInstruction());
        type.Properties.Add(new PropertyDefUser("Enabled", PropertySig.CreateStatic(_module.CorLibTypes.Boolean))
        {
            GetMethod = getter,
        });

        var patched = _module.PatchGetter(
            "Fixture.PropertyTarget",
            "Enabled",
            target => target.ReturnTrueMethod());

        using (Assert.EnterMultipleScope())
        {
            Assert.That(patched, Is.EqualTo(1));
            Assert.That(getter.Body.Instructions[0].GetLdcI4Value(), Is.EqualTo(1));
        }
    }

    [Test]
    public void PatchAsyncFunction_MissingMethod_ReturnsZero()
    {
        var patched = _module.PatchAsyncFunction(
            "Fixture.Missing",
            "RunAsync",
            "()System.Threading.Tasks.Task",
            target => target.ReturnTrueMethod());

        Assert.That(patched, Is.Zero);
    }

    private static ModuleDefUser CreateModule(string name, Version version)
    {
        var module = new ModuleDefUser(name);
        var assembly = new AssemblyDefUser("TestAssembly", version);
        assembly.Modules.Add(module);
        return module;
    }

    private static TypeDefUser AddType(ModuleDef module, string @namespace, string name)
    {
        var type = new TypeDefUser(@namespace, name, module.CorLibTypes.Object.TypeDefOrRef);
        module.Types.Add(type);
        return type;
    }

    private static MethodDefUser AddStaticMethod(TypeDef type, string name, MethodSig signature)
    {
        var method = new MethodDefUser(
            name,
            signature,
            MethodImplAttributes.IL,
            MethodAttributes.Public | MethodAttributes.Static)
        {
            Body = new CilBody()
        };
        type.Methods.Add(method);
        return method;
    }

    [LibraryName("Target.dll")]
    [FromVersion("4.55")]
    [UntilVersion("4.58")]
    private static void VersionedPatch()
    {
    }

    [LibraryName("Target.dll")]
    [FromVersion("4.60")]
    private static void FuturePatch()
    {
    }

    [LibraryName("Target.dll")]
    [UntilVersion("4.56")]
    private static void ExpiredPatch()
    {
    }

    private static void UnboundedPatch()
    {
    }

    [FromVersion("4.55")]
    private static void NoLibraryNamePatch()
    {
    }
}
