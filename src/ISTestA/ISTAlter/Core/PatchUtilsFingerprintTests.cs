// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: Copyright 2026 TautCony

using dnlib.DotNet;
using dnlib.DotNet.Emit;
using global::ISTAlter.Core;

namespace ISTestA.ISTAlter.Core;

/// <summary>
/// Tests for ComputeBodyFingerprint to ensure all IL operand types are handled correctly.
/// </summary>
public class PatchUtilsFingerprintTests
{
    private ModuleDefUser? _testModule;

    [SetUp]
    public void Setup()
    {
        // Create a test module in memory
        _testModule = new ModuleDefUser("TestModule");
    }

    [TearDown]
    public void TearDown()
    {
        _testModule?.Dispose();
    }

    /// <summary>
    /// Test that switch instructions (IList&lt;Instruction&gt; operands) are properly hashed.
    /// </summary>
    [Test]
    public void ComputeBodyFingerprint_SwitchInstruction_ShouldBeHashed()
    {
        // Arrange: Create two methods with different switch targets
        var method1 = CreateMethodWithSwitch([0, 1, 2]);
        var method2 = CreateMethodWithSwitch([0, 1, 3]); // Different target at index 2

        // Act: Compute fingerprints using reflection to access private method
        var fingerprint1 = ComputeFingerprintViaReflection(method1);
        var fingerprint2 = ComputeFingerprintViaReflection(method2);

        // Assert: Fingerprints should be different because switch targets differ
        Assert.That(fingerprint1, Is.Not.EqualTo(fingerprint2),
            "Methods with different switch targets should have different fingerprints");
    }

    /// <summary>
    /// Test that switch instructions with same targets produce same fingerprint.
    /// </summary>
    [Test]
    public void ComputeBodyFingerprint_SwitchInstruction_SameTargets_ShouldMatch()
    {
        // Arrange: Create two methods with identical switch targets
        var method1 = CreateMethodWithSwitch([0, 1, 2]);
        var method2 = CreateMethodWithSwitch([0, 1, 2]);

        // Act
        var fingerprint1 = ComputeFingerprintViaReflection(method1);
        var fingerprint2 = ComputeFingerprintViaReflection(method2);

        // Assert: Fingerprints should be identical
        Assert.That(fingerprint1, Is.EqualTo(fingerprint2),
            "Methods with identical switch targets should have identical fingerprints");
    }

    /// <summary>
    /// Test that switch instruction count affects fingerprint.
    /// </summary>
    [Test]
    public void ComputeBodyFingerprint_SwitchInstruction_DifferentCount_ShouldDiffer()
    {
        // Arrange: Create methods with different number of switch cases
        var method1 = CreateMethodWithSwitch([0, 1]);
        var method2 = CreateMethodWithSwitch([0, 1, 2]);

        // Act
        var fingerprint1 = ComputeFingerprintViaReflection(method1);
        var fingerprint2 = ComputeFingerprintViaReflection(method2);

        // Assert
        Assert.That(fingerprint1, Is.Not.EqualTo(fingerprint2),
            "Methods with different switch case counts should have different fingerprints");
    }

    /// <summary>
    /// Test that single branch instructions still work correctly.
    /// </summary>
    [Test]
    public void ComputeBodyFingerprint_SingleBranchInstruction_ShouldBeHashed()
    {
        // Arrange: Create two methods with different branch targets
        var method1 = CreateMethodWithBranch(targetIndex: 2);
        var method2 = CreateMethodWithBranch(targetIndex: 3);

        // Act
        var fingerprint1 = ComputeFingerprintViaReflection(method1);
        var fingerprint2 = ComputeFingerprintViaReflection(method2);

        // Assert
        Assert.That(fingerprint1, Is.Not.EqualTo(fingerprint2),
            "Methods with different branch targets should have different fingerprints");
    }

    /// <summary>
    /// Test that local operands are included in the fingerprint.
    /// </summary>
    [Test]
    public void ComputeBodyFingerprint_LocalOperand_ShouldBeHashed()
    {
        var method1 = CreateMethodWithLocalOperand(_testModule!.CorLibTypes.Int32);
        var method2 = CreateMethodWithLocalOperand(_testModule!.CorLibTypes.String);

        var fingerprint1 = ComputeFingerprintViaReflection(method1);
        var fingerprint2 = ComputeFingerprintViaReflection(method2);

        Assert.That(fingerprint1, Is.Not.EqualTo(fingerprint2),
            "Methods with different local operand metadata should have different fingerprints");
    }

    /// <summary>
    /// Test that parameter operands are included in the fingerprint.
    /// </summary>
    [Test]
    public void ComputeBodyFingerprint_ParameterOperand_ShouldBeHashed()
    {
        var method1 = CreateMethodWithParameterOperand(_testModule!.CorLibTypes.Int32, _testModule!.CorLibTypes.String);
        var method2 = CreateMethodWithParameterOperand(_testModule!.CorLibTypes.String, _testModule!.CorLibTypes.Int32);

        var fingerprint1 = ComputeFingerprintViaReflection(method1);
        var fingerprint2 = ComputeFingerprintViaReflection(method2);

        Assert.That(fingerprint1, Is.Not.EqualTo(fingerprint2),
            "Methods with different parameter operand metadata should have different fingerprints");
    }

    /// <summary>
    /// Test that sbyte operands are included in the fingerprint.
    /// </summary>
    [Test]
    public void ComputeBodyFingerprint_SByteOperand_ShouldBeHashed()
    {
        var method1 = CreateMethodWithSByteOperand(1);
        var method2 = CreateMethodWithSByteOperand(2);

        var fingerprint1 = ComputeFingerprintViaReflection(method1);
        var fingerprint2 = ComputeFingerprintViaReflection(method2);

        Assert.That(fingerprint1, Is.Not.EqualTo(fingerprint2),
            "Methods with different sbyte operands should have different fingerprints");
    }

    /// <summary>
    /// Test that methods with no body return fingerprint 0.
    /// </summary>
    [Test]
    public void ComputeBodyFingerprint_NoBody_ShouldReturnZero()
    {
        // Arrange: Create abstract method with no body
        var typeDef = new TypeDefUser("TestNamespace", "TestType", _testModule!.CorLibTypes.Object.TypeDefOrRef);
        _testModule.Types.Add(typeDef);

        var method = new MethodDefUser("AbstractMethod",
            MethodSig.CreateStatic(_testModule.CorLibTypes.Void),
            MethodAttributes.Public | MethodAttributes.Abstract);
        typeDef.Methods.Add(method);

        // Act
        var fingerprint = ComputeFingerprintViaReflection(method);

        // Assert
        Assert.That(fingerprint, Is.Zero,
            "Methods with no body should return fingerprint 0");
    }

    /// <summary>
    /// Helper method to create a method with a switch instruction.
    /// </summary>
    private MethodDefUser CreateMethodWithSwitch(int[] targetIndices)
    {
        var typeDef = new TypeDefUser("TestNamespace", "TestType", _testModule!.CorLibTypes.Object.TypeDefOrRef);
        _testModule.Types.Add(typeDef);

        var method = new MethodDefUser("TestMethod",
            MethodSig.CreateStatic(_testModule.CorLibTypes.Int32, _testModule.CorLibTypes.Int32),
            MethodAttributes.Public | MethodAttributes.Static);
        typeDef.Methods.Add(method);

        var body = new CilBody();
        method.Body = body;

        // Add instructions
        body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_0)); // Load argument

        // Create target instructions (one for each case)
        var targets = targetIndices.Select(value => Instruction.Create(OpCodes.Ldc_I4, value)).ToList();

        // Create switch instruction with targets
        body.Instructions.Add(Instruction.Create(OpCodes.Switch, [.. targets]));

        // Add target instructions to body
        foreach (var target in targets)
        {
            body.Instructions.Add(target);
        }

        body.Instructions.Add(Instruction.Create(OpCodes.Ret));

        return method;
    }

    /// <summary>
    /// Helper method to create a method with a single branch instruction.
    /// </summary>
    private MethodDefUser CreateMethodWithBranch(int targetIndex)
    {
        var typeDef = new TypeDefUser("TestNamespace", "TestType", _testModule!.CorLibTypes.Object.TypeDefOrRef);
        _testModule.Types.Add(typeDef);

        var method = new MethodDefUser("TestMethod",
            MethodSig.CreateStatic(_testModule.CorLibTypes.Void),
            MethodAttributes.Public | MethodAttributes.Static);
        typeDef.Methods.Add(method);

        var body = new CilBody();
        method.Body = body;

        // Create instructions
        var nop1 = Instruction.Create(OpCodes.Nop);
        var nop2 = Instruction.Create(OpCodes.Nop);
        var nop3 = Instruction.Create(OpCodes.Nop);
        var nop4 = Instruction.Create(OpCodes.Nop);

        body.Instructions.Add(nop1);
        body.Instructions.Add(nop2);
        body.Instructions.Add(nop3);
        body.Instructions.Add(nop4);

        // Add branch to specific target
        var target = body.Instructions[targetIndex];
        body.Instructions.Add(Instruction.Create(OpCodes.Br, target));
        body.Instructions.Add(Instruction.Create(OpCodes.Ret));

        return method;
    }

    /// <summary>
    /// Helper method to create a method with a local operand.
    /// </summary>
    private MethodDefUser CreateMethodWithLocalOperand(TypeSig localType)
    {
        var typeDef = new TypeDefUser("TestNamespace", "TestType", _testModule!.CorLibTypes.Object.TypeDefOrRef);
        _testModule.Types.Add(typeDef);

        var method = new MethodDefUser("TestMethod",
            MethodSig.CreateStatic(_testModule.CorLibTypes.Void),
            MethodAttributes.Public | MethodAttributes.Static);
        typeDef.Methods.Add(method);

        var body = new CilBody();
        method.Body = body;

        var local = new Local(localType);
        body.Variables.Add(local);
        body.Instructions.Add(Instruction.Create(OpCodes.Ldc_I4_0));
        body.Instructions.Add(Instruction.Create(OpCodes.Stloc, local));
        body.Instructions.Add(Instruction.Create(OpCodes.Ret));

        return method;
    }

    /// <summary>
    /// Helper method to create a method with a parameter operand.
    /// </summary>
    private MethodDefUser CreateMethodWithParameterOperand(TypeSig firstParameterType, TypeSig secondParameterType)
    {
        var typeDef = new TypeDefUser("TestNamespace", "TestType", _testModule!.CorLibTypes.Object.TypeDefOrRef);
        _testModule.Types.Add(typeDef);

        var method = new MethodDefUser("TestMethod",
            MethodSig.CreateStatic(_testModule.CorLibTypes.Void, firstParameterType, secondParameterType),
            MethodAttributes.Public | MethodAttributes.Static);
        typeDef.Methods.Add(method);

        var body = new CilBody();
        method.Body = body;

        body.Instructions.Add(Instruction.Create(OpCodes.Ldarg, method.Parameters[1]));
        body.Instructions.Add(Instruction.Create(OpCodes.Pop));
        body.Instructions.Add(Instruction.Create(OpCodes.Ret));

        return method;
    }

    /// <summary>
    /// Helper method to create a method with an sbyte operand.
    /// </summary>
    private MethodDefUser CreateMethodWithSByteOperand(sbyte value)
    {
        var typeDef = new TypeDefUser("TestNamespace", "TestType", _testModule!.CorLibTypes.Object.TypeDefOrRef);
        _testModule.Types.Add(typeDef);

        var method = new MethodDefUser("TestMethod",
            MethodSig.CreateStatic(_testModule.CorLibTypes.Int32),
            MethodAttributes.Public | MethodAttributes.Static);
        typeDef.Methods.Add(method);

        var body = new CilBody();
        method.Body = body;

        body.Instructions.Add(Instruction.Create(OpCodes.Ldc_I4_S, value));
        body.Instructions.Add(Instruction.Create(OpCodes.Ret));

        return method;
    }

    /// <summary>
    /// Use reflection to call the private ComputeBodyFingerprint method.
    /// </summary>
    private static int ComputeFingerprintViaReflection(MethodDef method)
    {
        var patchUtilsType = typeof(PatchUtils);
        var computeMethod = patchUtilsType.GetMethod("ComputeBodyFingerprint",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

        Assert.That(computeMethod, Is.Not.Null,
            "ComputeBodyFingerprint method should exist");

        var result = computeMethod!.Invoke(null, [method]);
        return (int)result!;
    }
}
