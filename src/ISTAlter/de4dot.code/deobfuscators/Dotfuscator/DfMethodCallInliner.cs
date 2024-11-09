using System.Collections.Generic;
using de4dot.blocks;
using de4dot.blocks.cflow;
using dnlib.DotNet;
using dnlib.DotNet.Emit;

namespace de4dot.code.deobfuscators.Dotfuscator
{
	class DfMethodCallInliner : MethodCallInlinerBase, IBranchHandler {
		InstructionEmulator emulator;
		BranchEmulator branchEmulator;
		int emulateIndex;
		IList<Instruction> instructions;

		public DfMethodCallInliner() {
			emulator = new InstructionEmulator();
			branchEmulator = new BranchEmulator(emulator, this);
		}

		public static List<MethodDef> Find(ModuleDefMD module, IEnumerable<MethodDef> notInlinedMethods) {
			var notInlinedMethodsDict = new Dictionary<MethodDef, bool>();
			foreach (var method in notInlinedMethods)
				notInlinedMethodsDict[method] = true;

			var inlinedMethods = new List<MethodDef>();

			foreach (var type in module.GetTypes()) {
				foreach (var method in type.Methods) {
					if (!notInlinedMethodsDict.ContainsKey(method) && CanInline(method))
						inlinedMethods.Add(method);
				}
			}

			return inlinedMethods;
		}

		void IBranchHandler.HandleNormal(int stackArgs, bool isTaken) {
			if (!isTaken)
				emulateIndex++;
			else
				emulateIndex = instructions.IndexOf((Instruction)instructions[emulateIndex].Operand);
		}

		bool IBranchHandler.HandleSwitch(Int32Value switchIndex) {
			if (!switchIndex.AllBitsValid())
				return false;
			var instr = instructions[emulateIndex];
			var targets = (Instruction[])instr.Operand;
			if (switchIndex.Value >= 0 && switchIndex.Value < targets.Length)
				emulateIndex = instructions.IndexOf(targets[switchIndex.Value]);
			else
				emulateIndex++;
			return true;
		}

		protected override bool DeobfuscateInternal() {
			bool modified = false;
			var instrs = block.Instructions;
			for (int i = 0; i < instrs.Count; i++) {
				var instr = instrs[i].Instruction;
				if (instr.OpCode.Code == Code.Call)
					modified |= InlineMethod(instr, i);
			}

			return modified;
		}

		static bool CanInline(MethodDef method) {
			if (!DotNetUtils.IsMethod(method, "System.Int32", "(System.Int32)"))
				return false;
			if (!method.IsAssembly)
				return false;
			if (method.MethodSig.GetGenParamCount() > 0)
				return false;

			return method.IsStatic;
		}

		bool CanInline2(MethodDef method) => CanInline(method) && method != blocks.Method;

		bool InlineMethod(Instruction callInstr, int instrIndex) {
			var methodToInline = callInstr.Operand as MethodDef;
			if (methodToInline == null)
				return false;

			if (!CanInline2(methodToInline))
				return false;
			var body = methodToInline.Body;
			if (body == null)
				return false;

			if (instrIndex == 0)
				return false;

			var ldci4 = block.Instructions[instrIndex - 1];
			if (!ldci4.IsLdcI4())
				return false;
			if (!GetNewValue(methodToInline, ldci4.GetLdcI4Value(), out int newValue))
				return false;

			block.Instructions[instrIndex - 1] = new Instr(OpCodes.Nop.ToInstruction());
			block.Instructions[instrIndex] = new Instr(Instruction.CreateLdcI4(newValue));
			return true;
		}

		bool GetNewValue(MethodDef method, int arg, out int newValue) {
			newValue = 0;
			emulator.Initialize(method);
			emulator.SetArg(method.Parameters[0], new Int32Value(arg));

			emulateIndex = 0;
			instructions = method.Body.Instructions;
			int counter = 0;
			while (true) {
				if (counter++ >= 50)
					return false;
				if (emulateIndex < 0 || emulateIndex >= instructions.Count)
					return false;
				var instr = instructions[emulateIndex];
				switch (instr.OpCode.Code) {
				case Code.Br:
				case Code.Br_S:
				case Code.Beq:
				case Code.Beq_S:
				case Code.Bge:
				case Code.Bge_S:
				case Code.Bge_Un:
				case Code.Bge_Un_S:
				case Code.Bgt:
				case Code.Bgt_S:
				case Code.Bgt_Un:
				case Code.Bgt_Un_S:
				case Code.Ble:
				case Code.Ble_S:
				case Code.Ble_Un:
				case Code.Ble_Un_S:
				case Code.Blt:
				case Code.Blt_S:
				case Code.Blt_Un:
				case Code.Blt_Un_S:
				case Code.Bne_Un:
				case Code.Bne_Un_S:
				case Code.Brfalse:
				case Code.Brfalse_S:
				case Code.Brtrue:
				case Code.Brtrue_S:
				case Code.Switch:
					if (!branchEmulator.Emulate(instr))
						return false;
					break;

				case Code.Ret:
					var retValue = emulator.Pop();
					if (!retValue.IsInt32())
						return false;
					var retValue2 = (Int32Value)retValue;
					if (!retValue2.AllBitsValid())
						return false;
					newValue = retValue2.Value;
					return true;

				default:
					emulator.Emulate(instr);
					emulateIndex++;
					break;
				}
			}
		}

		protected override bool IsCompatibleType(int paramIndex, IType origType, IType newType) {
			if (new SigComparer(SigComparerOptions.IgnoreModifiers).Equals(origType, newType))
				return true;
			if (IsValueType(newType) || IsValueType(origType))
				return false;
			return newType.FullName == "System.Object";
		}
	}
}
