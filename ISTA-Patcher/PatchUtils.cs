using System.Reflection.Emit;
using Mono.Cecil;
using Mono.Cecil.Cil;
using OpCodes = Mono.Cecil.Cil.OpCodes;

namespace ISTA_Patcher
{
    internal class PatchUtils
    {
        public static AssemblyDefinition LoadAssembly(string fileName)
        {
            var assemblyResolver = new DefaultAssemblyResolver();
            assemblyResolver.AddSearchDirectory(Path.GetDirectoryName(fileName));
            var assembly = AssemblyDefinition.ReadAssembly(fileName, new ReaderParameters { AssemblyResolver = assemblyResolver, InMemory = true });
            return assembly;
        }

        public static bool PatchIntegrityManager(AssemblyDefinition assembly)
        {
            var integrityManagerConstructor = assembly.GetMethod(
                "BMW.Rheingold.SecurityAndLicense.IntegrityManager",
                ".ctor",
                "()System.Void"
            );
            if (integrityManagerConstructor == null)
            {
                // Console.WriteLine($"{nameof(integrityManagerConstructor)} not found, skiping this...");
                return false;
            }
            integrityManagerConstructor.EmptyingMethod();
            return true;
        }

        public static bool PatchLicenseStatusChecker(AssemblyDefinition assembly)
        {
            var isLicenseValid = assembly.GetMethod(
                "BMW.Rheingold.CoreFramework.LicenseManagement.LicenseStatusChecker",
                "IsLicenseValid",
                "(BMW.Rheingold.CoreFramework.LicenseInfo,System.Boolean)BMW.Rheingold.CoreFramework.LicenseStatus"
            );
            if (isLicenseValid == null)
            {
                // Console.WriteLine($"{nameof(isLicenseValid)} not found, skiping this...");
                return false;
            }
            isLicenseValid.ReturnZeroMethod();
            return true;
        }

        public static bool PatchCheckSignature(AssemblyDefinition assembly)
        {
            var CheckSignature = assembly.GetMethod(
                "BMW.Rheingold.CoreFramework.WcfCommon.IstaProcessStarter",
                "CheckSignature",
                "(System.String)System.Void"
            );
            if (CheckSignature == null)
            {
                // Console.WriteLine($"{nameof(CheckSignature)} not found, skiping this...");
                return false;
            }
            CheckSignature.EmptyingMethod();
            return true;
        }

        public static bool PatchLicenseManager(AssemblyDefinition assembly)
        {
            var VerifyLicense = assembly.GetMethod(
                "BMW.Rheingold.CoreFramework.LicenseManager",
                "VerifyLicense",
                "(System.Boolean)System.Void"
            );
            if (VerifyLicense == null)
            {
                // Console.WriteLine($"{nameof(VerifyLicense)} not found, skiping this...");
                return false;
            }
            VerifyLicense.EmptyingMethod();
            return true;
        }

        public static bool PatchAOSLicenseManager(AssemblyDefinition assembly)
        {
            var VerifyLicense = assembly.GetMethod(
                "BMW.Rheingold.CoreFramework.LicenseAOSManager",
                "VerifyLicense",
                "()System.Void"
            );
            if (VerifyLicense == null)
            {
                // Console.WriteLine($"{nameof(VerifyLicense)} not found, skiping this...");
                return false;
            }
            VerifyLicense.EmptyingMethod();
            return true;
        }

        public static bool PatchIstaIcsServiceClient(AssemblyDefinition assembly)
        {
            var ValidateHost = assembly.GetMethod(
                "BMW.ISPI.IstaServices.Client.IstaIcsServiceClient",
                "ValidateHost",
                "()System.Void"
            );
            if (ValidateHost == null)
            {
                // Console.WriteLine($"{nameof(ValidateHost)} not found, skiping this...");
                return false;
            }
            ValidateHost.EmptyingMethod();
            var VerifyLicense = assembly.GetMethod(
                "BMW.ISPI.IstaServices.Client.IstaIcsServiceClient",
                "VerifyLicense",
                "()System.Void"
            );
            if (VerifyLicense == null)
            {
                // Console.WriteLine($"{nameof(VerifyLicense)} not found, skiping this...");
                return false;
            }
            VerifyLicense.EmptyingMethod();
            return true;
        }

        public static bool PatchCommonServiceWrapper(AssemblyDefinition assembly)
        {
            var VerifyLicense = assembly.GetMethod(
                "BMW.Rheingold.RheingoldISPINext.ICS.CommonServiceWrapper",
                "VerifyLicense",
                "()System.Void"
            );
            if (VerifyLicense == null)
            {
                // Console.WriteLine($"{nameof(VerifyLicense)} not found, skiping this...");
                return false;
            }
            VerifyLicense.EmptyingMethod();
            return true;
        }

        public static bool PatchSecureAccessHelper(AssemblyDefinition assembly)
        {
            var IsCodeAccessPermitted = assembly.GetMethod(
                "BMW.iLean.CommonServices.Helper.SecureAccessHelper",
                "IsCodeAccessPermitted",
                "(System.Reflection.Assembly,System.Reflection.Assembly)System.Boolean"
            );
            if (IsCodeAccessPermitted == null)
            {
                // Console.WriteLine($"{nameof(IsCodeAccessPermitted)} not found, skiping this...");
                return false;
            }
            IsCodeAccessPermitted.EmptyingMethod();
            return true;
        }

        public static bool PatchLicenseWizardHelper(AssemblyDefinition assembly)
        {
            var DoLicenseCheck = assembly.GetMethod(
                "BMW.Rheingold.CoreFramework.LicenseManagement.LicenseWizardHelper",
                "DoLicenseCheck",
                "(System.String)System.Boolean"
            );
            if (DoLicenseCheck == null)
            {
                // Console.WriteLine($"{nameof(IsCodeAccessPermitted)} not found, skiping this...");
                return false;
            }
            DoLicenseCheck.ReturnOneMethod();
            return true;
        }

        public static bool CheckPatchedMark(AssemblyDefinition assembly)
        {
            var patchedType = assembly.MainModule.GetType("Patched.By.TC");
            return patchedType != null;
        }

        public static void SetPatchedMark(AssemblyDefinition assembly)
        {
            var patchedType = new TypeDefinition(
                            "Patched.By", "TC",
                            TypeAttributes.NestedPrivate,
                            assembly.MainModule.ImportReference(typeof(object)));
            var dateField = new FieldDefinition(
                "date",
                FieldAttributes.Private | FieldAttributes.Static,
                assembly.MainModule.ImportReference(typeof(string)))
            {
                Constant = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
            };

            patchedType.Fields.Add(dateField);
            assembly.MainModule.Types.Add(patchedType);
        }

        public static string DecryptString(string value, int baseSeed, int seed)
        {
            char[] charArray = value.ToCharArray();
            int key = baseSeed + seed;
            for (var i = 0; i < charArray.Length; i++)
            {
                char ch = charArray[i];
                byte ch_lo_byte = (byte)(ch & 0xff);
                byte ch_hi_byte = (byte)(ch >> 8);

                byte real_hi_byte = (byte)(ch_lo_byte ^ key);
                byte real_lo_byte = (byte)(ch_hi_byte ^ (key + 1));
                char real_ch = (char)(((uint)real_hi_byte << 8) | real_lo_byte);
                charArray[i] = real_ch;

                key += 2;
            }
            return string.Intern(new string(charArray));
        }

        public static bool DecryptParameter(AssemblyDefinition assembly)
        {
            var b = assembly.GetMethod(
                "BMW.Rheingold.CoreFramework.LicenseManagement.LicenseWizardHelper",
                "b", 
                "(System.String,System.Int32)System.String");
            if (b == null)
            {
                return false;
            }

            var baseSeed = 0;
            foreach (var instruction in b.Body.Instructions)
            {
                if (instruction.OpCode == OpCodes.Stloc_1)
                {
                    break;
                }
                if (instruction.OpCode == OpCodes.Ldc_I4)
                {
                    baseSeed += (int) instruction.Operand;
                }
            }
            
            foreach (var type in assembly.MainModule.Types)
            {
                foreach (var method in type.Methods)
                {
                    if (method.Body == null)
                    {
                        continue;
                    }
                    var decodedStrings = new List<KeyValuePair<int, string>>();
                    for (var i = 0; i < method.Body.Instructions.Count; ++i)
                    {
                        var instruction = method.Body.Instructions[i];
                        if (instruction.OpCode != OpCodes.Call || instruction.Operand != b) continue;
                        if (instruction.Previous.OpCode != OpCodes.Ldloc ||
                            instruction.Previous.Previous.OpCode != OpCodes.Ldstr)
                        {
                            continue;
                        }
                        var seed = (VariableDefinition) instruction.Previous.Operand;
                        var seedValue = int.MaxValue;
                        var instruction2 = method.Body.Instructions.FirstOrDefault(inst =>
                            inst.OpCode == OpCodes.Stloc && inst.Operand == seed);
                        if (instruction2 != null)
                        {
                            seedValue = (int) instruction2.Previous.Operand;
                        }
                        if (seedValue == int.MaxValue) continue;
                        var str = (string) instruction.Previous.Previous.Operand;
                        decodedStrings.Add(new KeyValuePair<int, string>(i, PatchUtils.DecryptString(str, baseSeed, seedValue)));
                    }

                    var processor = method.Body.GetILProcessor();
                    decodedStrings.Reverse();
                    foreach (var pair in decodedStrings)
                    {
                        // ldstr
                        // ldloc
                        // call  -> ldstr
                        // 7 8 9
                        processor.Replace(pair.Key, Instruction.Create(OpCodes.Ldstr, pair.Value));
                        processor.RemoveAt(pair.Key - 1);
                        processor.RemoveAt(pair.Key - 2);
                    }
                }
            }

            return true;
        }
    }
}
