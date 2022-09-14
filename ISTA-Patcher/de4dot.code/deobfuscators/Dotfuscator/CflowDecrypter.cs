/*
    Copyright (C) 2011-2015 de4dot@gmail.com

    This file is part of de4dot.

    de4dot is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    de4dot is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with de4dot.  If not, see <http://www.gnu.org/licenses/>.
*/

using System.Collections.Generic;
using de4dot.blocks;
using dnlib.DotNet;
using dnlib.DotNet.Emit;

namespace de4dot.code.deobfuscators.Dotfuscator {
	class CflowDecrypter {
		ModuleDefMD module;

		public CflowDecrypter(ModuleDefMD module) => this.module = module;

		public void CflowClean() {
			foreach (var type in module.GetTypes()) {
				if (!type.HasMethods)
					continue;
				foreach (var method in type.Methods) {
					CleanMethod(method);
				}
			}
		}

		public void CleanMethod(MethodDef method) {
			if (!method.HasBody)
				return;
			if (!method.Body.HasInstructions)
				return;
			if (method.Body.Instructions.Count < 4)
				return;
			if (method.Body.Variables.Count == 0)
				return;
			var instructions = method.Body.Instructions;
			GetFixIndexs(instructions, out var nopIdxs, out var ldlocIdxs);
			GetFixIndexs2(method, ref nopIdxs);
			if (nopIdxs.Count > 0) {
				foreach (var idx in nopIdxs) {
					method.Body.Instructions[idx].OpCode = OpCodes.Nop;
					method.Body.Instructions[idx].Operand = null;
				}
			}
			if (ldlocIdxs.Count > 0) {
				foreach (var idx in ldlocIdxs) {
					method.Body.Instructions[idx].OpCode = OpCodes.Ldloc;
				}
			}
		}

		void GetFixIndexs(IList<Instruction> instructions, out List<int> nopIdxs, out List<int> ldlocIdxs) {
			var insNoNops = new List<Instruction>();
			foreach (var ins in instructions) {
				if (ins.OpCode != OpCodes.Nop)
					insNoNops.Add(ins);
			}
			nopIdxs = new List<int>();
			ldlocIdxs = new List<int>();
			for (int i = 3; i < insNoNops.Count - 1; i++) {
				var ldind = insNoNops[i];
				if (ldind.OpCode != OpCodes.Ldind_I4 && ldind.OpCode != OpCodes.Ldind_I2)
					continue;
				var ldlocX = insNoNops[i - 1];
				if (!ldlocX.IsLdloc() && ldlocX.OpCode.Code != Code.Ldloca && ldlocX.OpCode.Code != Code.Ldloca_S)
					continue;
				var stloc = insNoNops[i - 2];
				if (!stloc.IsStloc())
					continue;
				var ldci4 = insNoNops[i - 3];
				if (!ldci4.IsLdcI4())
					continue;
				ldlocIdxs.Add(instructions.IndexOf(ldlocX));
				nopIdxs.Add(instructions.IndexOf(ldind));
				var convi2 = insNoNops[i + 1];
				if (ldind.OpCode == OpCodes.Ldind_I2 && convi2.OpCode == OpCodes.Conv_I2)
					nopIdxs.Add(instructions.IndexOf(convi2));
				var convi = insNoNops[i + 2];
				if (ldind.OpCode == OpCodes.Ldind_I2 && convi.OpCode == OpCodes.Conv_I)
					nopIdxs.Add(instructions.IndexOf(convi));
			}
		}

		void GetFixIndexs2(MethodDef method, ref List<int> nopIdxs) {
			Local local = null;
			var shortArray = new List<short>();

			var instructions = method.Body.Instructions;
			for (int i = 0; i < instructions.Count; i++) {
				var ldci4 = instructions[i];
				if (!ldci4.IsLdcI4())
					continue;
				if (instructions.Count <= i + 5)
					continue;
				var newArr = instructions[i + 1];
				if (newArr.OpCode.Code != Code.Newarr)
					continue;
				var stloc = instructions[i + 2];
				if (!stloc.IsStloc())
					continue;
				local = stloc.GetLocal(method.Body.Variables);
				if (!local.Type.IsSZArray || local.Type.Next != module.CorLibTypes.Int16)
					continue;
				var ldloc = instructions[i + 3];
				if (!ldloc.IsLdloc() || ldloc.GetLocal(method.Body.Variables) != local)
					continue;
				var ldtoken = instructions[i + 4];
				if (ldtoken.OpCode.Code != Code.Ldtoken)
					continue;
				if (ldtoken.Operand is not FieldDef arrayInitField || arrayInitField.InitialValue == null || arrayInitField.InitialValue.Length == 0)
					continue;
				var call = instructions[i + 5];
				if (call.OpCode.Code != Code.Call)
					continue;
				var calledMethod = call.Operand as IMethod;
				if (!DotNetUtils.IsMethod(calledMethod, "System.Void", "(System.Array,System.RuntimeFieldHandle)"))
					continue;
				var array = arrayInitField.InitialValue;
				if (array.Length % 2 != 0)
					continue;
				for (int j = 0; j < array.Length; j += 2)
					shortArray.Add((short)(array[j] | array[j + 1] << 8));

				var startIndex = instructions.IndexOf(ldci4);
				for (int j = startIndex; j <= startIndex + 5; j++)
					nopIdxs.Add(j);

				break;
			}

			if (local == null || shortArray.Count == 0)
				return;

			for (int i = 0; i < instructions.Count - 1; i++) {
				var ldelem = instructions[i];
				if (ldelem.OpCode != OpCodes.Ldelem_I2)
					continue;
				var ldci4 = instructions[i - 1];
				if (!ldci4.IsLdcI4())
					continue;
				var ldloc = instructions[i - 2];
				if (!ldloc.IsLdloc() || ldloc.GetLocal(method.Body.Variables) != local)
					continue;

				instructions[i - 1] = Instruction.CreateLdcI4(shortArray[ldci4.GetLdcI4Value()]);

				nopIdxs.Add(instructions.IndexOf(ldelem));
				nopIdxs.Add(instructions.IndexOf(ldloc));
			}
		}
	}
}
