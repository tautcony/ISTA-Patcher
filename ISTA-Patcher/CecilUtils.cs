using Mono.Cecil;
using Mono.Cecil.Cil;
using System.Text;

namespace ISTA_Patcher
{
    public static class CecilUtils
    {
        public static string DescriptionOf(MethodDefinition md)
        {
            var sb = new StringBuilder();
            sb.Append('(');

            var set = false;
            foreach (var param in md.Parameters)
            {
                sb.Append(param.ParameterType.FullName);
                sb.Append(',');
                set = true;
            }
            if (set) sb.Length -= 1;

            sb.Append(')');
            sb.Append(md.ReturnType.FullName);
            return sb.ToString();
        }

        public static bool IsGettingField(Instruction ins)
        {
            return ins.OpCode == OpCodes.Ldfld || ins.OpCode == OpCodes.Ldflda;
        }

        public static bool IsPuttingField(Instruction ins)
        {
            return ins.OpCode == OpCodes.Stfld;
        }

        public static bool IsNativeType(string returnName)
        {
            return returnName.Equals(typeof(long).FullName) ||
                returnName.Equals(typeof(ulong).FullName) ||
                returnName.Equals(typeof(int).FullName) ||
                returnName.Equals(typeof(uint).FullName) ||
                returnName.Equals(typeof(short).FullName) ||
                returnName.Equals(typeof(ushort).FullName) ||
                returnName.Equals(typeof(byte).FullName) ||
                returnName.Equals(typeof(bool).FullName);
        }

        public static bool IsJump(OpCode oc)
        {
            return
                oc == OpCodes.Br || oc == OpCodes.Br_S ||
                oc == OpCodes.Brtrue || oc == OpCodes.Brtrue_S ||
                oc == OpCodes.Brfalse || oc == OpCodes.Brfalse_S ||
                oc == OpCodes.Bne_Un || oc == OpCodes.Bne_Un_S ||
                oc == OpCodes.Blt_Un || oc == OpCodes.Blt_Un_S ||
                oc == OpCodes.Ble_Un || oc == OpCodes.Ble_Un_S ||
                oc == OpCodes.Bge_Un || oc == OpCodes.Bge_Un_S ||
                oc == OpCodes.Bgt_Un || oc == OpCodes.Bge_Un_S ||
                oc == OpCodes.Beq || oc == OpCodes.Beq_S ||
                oc == OpCodes.Ble || oc == OpCodes.Ble_S ||
                oc == OpCodes.Blt || oc == OpCodes.Blt_S
                ;
        }

        public static MethodDefinition? GetMethod(this AssemblyDefinition asm, string type, string name, string desc)
        {
            var tds = asm.Modules.Where(m => m.GetType(type) != null).Select(m => m.GetType(type)).ToList();
            if (!tds.Any())
            {
                return null;
            }
            if (tds.Count != 1)
            {
                throw new Exception();
            }
            var td = tds.First();
            return td.Methods.FirstOrDefault(m => m.Name.Equals(name) && DescriptionOf(m).Equals(desc.Replace(" ", string.Empty)));
        }

        public static FieldDefinition? GetField(this AssemblyDefinition asm, string type, string name, string fieldType)
        {
            var tds = asm.Modules.Where(m => m.GetType(type) != null).Select(m => m.GetType(type)).ToList();
            if (!tds.Any())
            {
                return null;
            }
            if (tds.Count > 1)
            {
                throw new Exception();
            }
            var td = tds.First();
            return td.Fields.FirstOrDefault(f => f.Name.Equals(name) && f.FieldType.Resolve().FullName.Equals(fieldType));
        }


        public static void EmptyingMethod(this MethodDefinition method)
        {
            var body = method.Body;
            if (body != null)
            {
                body.Variables.Clear();
                body.ExceptionHandlers.Clear();
                var ilProcessor = body.GetILProcessor();
                ilProcessor.Clear();
                ilProcessor.Append(Instruction.Create(OpCodes.Ret));
            }
            else
            {
                throw new Exception($"{method.FullName}.Body null!");
            }
        }

        public static void ReturnZeroMethod(this MethodDefinition method)
        {
            var body = method.Body;
            if (body != null)
            {
                body.Variables.Clear();
                body.ExceptionHandlers.Clear();
                var ilProcessor = body.GetILProcessor();
                ilProcessor.Clear();
                ilProcessor.Append(Instruction.Create(OpCodes.Ldc_I4_0));
                ilProcessor.Append(Instruction.Create(OpCodes.Ret));
            }
            else
            {
                throw new Exception($"{method.FullName}.Body null!");
            }
        }

        public static void ReturnOneMethod(this MethodDefinition method)
        {
            var body = method.Body;
            if (body != null)
            {
                body.Variables.Clear();
                body.ExceptionHandlers.Clear();
                var ilProcessor = body.GetILProcessor();
                ilProcessor.Clear();
                ilProcessor.Append(Instruction.Create(OpCodes.Ldc_I4_1));
                ilProcessor.Append(Instruction.Create(OpCodes.Ret));
            }
            else
            {
                throw new Exception($"{method.FullName}.Body null!");
            }
        }
    }
}
