// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: Copyright 2026 TautCony

namespace ISTestA;

using dnlib.DotNet;
using dnlib.DotNet.Emit;
using ISTAlter.Utils;

/// <summary>
/// Tests for DnlibUtils extension methods and static helpers.
/// </summary>
public class DnlibUtilsTests
{
    private ModuleDefUser _module = null!;
    private Importer _importer;

    [SetUp]
    public void Setup()
    {
        _module = new ModuleDefUser("TestModule");
        _importer = new Importer(_module);
    }

    [TearDown]
    public void TearDown()
    {
        _module.Dispose();
    }

    // ────────────── DescriptionOf ──────────────

    [Test]
    public void DescriptionOf_NoParams_ReturnsEmptyParensReturnType()
    {
        var strSig = _importer.ImportAsTypeSig(typeof(string));
        var method = new MethodDefUser("Test", MethodSig.CreateStatic(strSig));

        var desc = DnlibUtils.DescriptionOf(method);

        Assert.That(desc, Is.EqualTo("()System.String"));
    }

    [Test]
    public void DescriptionOf_MultipleParams_CommaSeparated()
    {
        var intSig = _importer.ImportAsTypeSig(typeof(int));
        var boolSig = _importer.ImportAsTypeSig(typeof(bool));
        var voidSig = _module.CorLibTypes.Void;
        var method = new MethodDefUser("Test", MethodSig.CreateStatic(voidSig, intSig, boolSig));

        var desc = DnlibUtils.DescriptionOf(method);

        Assert.That(desc, Is.EqualTo("(System.Int32,System.Boolean)System.Void"));
    }

    // ────────────── GetType / GetMethod on ModuleDefMD ──────────────

    [Test]
    public void GetType_ExistingType_ReturnsTypeDef()
    {
        using var module = ModuleDefMD.Load(typeof(ISTAlter.Utils.HashFileInfo).Module);

        var type = module.GetType("ISTAlter.Utils.HashFileInfo");

        Assert.That(type, Is.Not.Null);
        Assert.That(type!.FullName, Is.EqualTo("ISTAlter.Utils.HashFileInfo"));
    }

    [Test]
    public void GetType_NonExistingType_ReturnsNull()
    {
        using var module = ModuleDefMD.Load(typeof(ISTAlter.Utils.HashFileInfo).Module);

        var type = module.GetType("ISTAlter.Utils.DoesNotExist");

        Assert.That(type, Is.Null);
    }

    [Test]
    public void GetMethod_ExistingMethod_ReturnsMethodDef()
    {
        using var module = ModuleDefMD.Load(typeof(ISTAlter.Utils.HashFileInfo).Module);

        // CalculateHash(string) → Task<string>
        var method = module.GetMethod(
            "ISTAlter.Utils.HashFileInfo",
            "CalculateHash",
            "(System.String)System.Threading.Tasks.Task`1<System.String>");

        Assert.That(method, Is.Not.Null);
        Assert.That(method!.Name.String, Is.EqualTo("CalculateHash"));
    }

    [Test]
    public void GetMethod_NonExistingMethod_ReturnsNull()
    {
        using var module = ModuleDefMD.Load(typeof(ISTAlter.Utils.HashFileInfo).Module);

        var method = module.GetMethod("ISTAlter.Utils.HashFileInfo", "DoesNotExist", "()System.Void");

        Assert.That(method, Is.Null);
    }

    // ────────────── EmptyingMethod ──────────────

    [Test]
    public void EmptyingMethod_ClearsBodyAndAddsRet()
    {
        var method = CreateMethodWithBody(
            [OpCodes.Ldc_I4_1.ToInstruction(), OpCodes.Ret.ToInstruction()]);

        method.EmptyingMethod();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(method.Body.Instructions, Has.Count.EqualTo(1));
            Assert.That(method.Body.Instructions[0].OpCode, Is.EqualTo(OpCodes.Ret));
            Assert.That(method.Body.Variables, Is.Empty);
            Assert.That(method.Body.ExceptionHandlers, Is.Empty);
        }
    }

    [Test]
    public void EmptyingMethod_NullBody_ThrowsInvalidOperation()
    {
        var voidSig = _module.CorLibTypes.Void;
        var method = new MethodDefUser("Test", MethodSig.CreateStatic(voidSig));

        Assert.Throws<InvalidOperationException>(() => method.EmptyingMethod());
    }

    // ────────────── ReturnZero / ReturnOne / ReturnTrue / ReturnFalse ──────────────

    [Test]
    public void ReturnZeroMethod_SetsBodyToLdcI4Zero()
    {
        var method = CreateMethodWithBody([OpCodes.Ret.ToInstruction()]);

        method.ReturnZeroMethod();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(method.Body.Instructions, Has.Count.EqualTo(2));
            Assert.That(method.Body.Instructions[0].GetLdcI4Value(), Is.EqualTo(0));
            Assert.That(method.Body.Instructions[1].OpCode, Is.EqualTo(OpCodes.Ret));
        }
    }

    [Test]
    public void ReturnOneMethod_SetsBodyToLdcI4One()
    {
        var method = CreateMethodWithBody([OpCodes.Ret.ToInstruction()]);

        method.ReturnOneMethod();

        Assert.That(method.Body.Instructions[0].GetLdcI4Value(), Is.EqualTo(1));
    }

    [Test]
    public void ReturnTrueMethod_SetsBodyToLdcI4One()
    {
        var method = CreateMethodWithBody([OpCodes.Ret.ToInstruction()]);

        method.ReturnTrueMethod();

        Assert.That(method.Body.Instructions[0].GetLdcI4Value(), Is.EqualTo(1));
    }

    [Test]
    public void ReturnFalseMethod_SetsBodyToLdcI4Zero()
    {
        var method = CreateMethodWithBody([OpCodes.Ret.ToInstruction()]);

        method.ReturnFalseMethod();

        Assert.That(method.Body.Instructions[0].GetLdcI4Value(), Is.EqualTo(0));
    }

    [Test]
    public void ReturnStringMethod_SetsBodyToLdstr()
    {
        var method = CreateMethodWithBody([OpCodes.Ret.ToInstruction()]);

        DnlibUtils.ReturnStringMethod("hello")(method);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(method.Body.Instructions[0].OpCode, Is.EqualTo(OpCodes.Ldstr));
            Assert.That(method.Body.Instructions[0].Operand, Is.EqualTo("hello"));
        }
    }

    [Test]
    public void ReturnUInt32Method_SetsBodyToExpectedValue()
    {
        var method = CreateMethodWithBody([OpCodes.Ret.ToInstruction()]);

        DnlibUtils.ReturnUInt32Method(42u)(method);

        Assert.That(method.Body.Instructions[0].GetLdcI4Value(), Is.EqualTo(42));
    }

    // ────────────── FindInstruction / FindInstructions / FindIndexOfInstruction ──────────────

    [Test]
    public void FindInstruction_MatchingStringLoad_ReturnsInstruction()
    {
        var method = CreateMethodWithBody(
        [
            Instruction.Create(OpCodes.Ldstr, "hello"),
            Instruction.Create(OpCodes.Ldstr, "world"),
            OpCodes.Ret.ToInstruction(),
        ]);

        var found = method.FindInstruction(OpCodes.Ldstr, "hello");

        Assert.That(found, Is.Not.Null);
        Assert.That(found!.Operand, Is.EqualTo("hello"));
    }

    [Test]
    public void FindInstruction_NoMatch_ReturnsNull()
    {
        var method = CreateMethodWithBody([OpCodes.Ret.ToInstruction()]);

        var found = method.FindInstruction(OpCodes.Ldstr, "missing");

        Assert.That(found, Is.Null);
    }

    [Test]
    public void FindInstructions_MultipleMatches_ReturnsAll()
    {
        var method = CreateMethodWithBody(
        [
            Instruction.Create(OpCodes.Ldstr, "dup"),
            Instruction.Create(OpCodes.Ldstr, "dup"),
            Instruction.Create(OpCodes.Ldstr, "other"),
            OpCodes.Ret.ToInstruction(),
        ]);

        var result = method.FindInstructions(OpCodes.Ldstr, "dup");

        Assert.That(result, Has.Count.EqualTo(2));
    }

    [Test]
    public void FindIndexOfInstruction_ReturnsCorrectIndex()
    {
        var method = CreateMethodWithBody(
        [
            OpCodes.Nop.ToInstruction(),
            Instruction.Create(OpCodes.Ldstr, "find-me"),
            OpCodes.Ret.ToInstruction(),
        ]);

        var idx = method.FindIndexOfInstruction(OpCodes.Ldstr, "find-me");

        Assert.That(idx, Is.EqualTo(1));
    }

    [Test]
    public void FindIndexOfInstruction_NoMatch_ReturnsNegativeOne()
    {
        var method = CreateMethodWithBody([OpCodes.Ret.ToInstruction()]);

        var idx = method.FindIndexOfInstruction(OpCodes.Ldstr, "missing");

        Assert.That(idx, Is.EqualTo(-1));
    }

    // ────────────── ReplaceWith ──────────────

    [Test]
    public void ReplaceWith_ReplacesAllInstructions()
    {
        var method = CreateMethodWithBody([OpCodes.Nop.ToInstruction(), OpCodes.Ret.ToInstruction()]);
        var newInstructions = new[] { Instruction.Create(OpCodes.Ldc_I4_5), OpCodes.Ret.ToInstruction() };

        method.ReplaceWith(newInstructions);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(method.Body.Instructions, Has.Count.EqualTo(2));
            Assert.That(method.Body.Instructions[0].GetLdcI4Value(), Is.EqualTo(5));
        }
    }

    // ────────────── GetLocalByType ──────────────

    [Test]
    public void GetLocalByType_MatchingType_ReturnsLocal()
    {
        var method = CreateMethodWithBody([OpCodes.Ret.ToInstruction()]);
        var intSig = _importer.ImportAsTypeSig(typeof(int));
        method.Body.Variables.Add(new Local(intSig));

        var local = method.GetLocalByType("System.Int32");

        Assert.That(local, Is.Not.Null);
    }

    [Test]
    public void GetLocalByType_NoMatch_ReturnsNull()
    {
        var method = CreateMethodWithBody([OpCodes.Ret.ToInstruction()]);

        var local = method.GetLocalByType("System.String");

        Assert.That(local, Is.Null);
    }

    // ────────────── Helpers ──────────────

    private MethodDefUser CreateMethodWithBody(IEnumerable<Instruction> instructions)
    {
        var voidSig = _module.CorLibTypes.Void;
        var method = new MethodDefUser("Test", MethodSig.CreateStatic(voidSig));
        method.Body = new CilBody();
        foreach (var instr in instructions)
        {
            method.Body.Instructions.Add(instr);
        }

        return method;
    }
}
