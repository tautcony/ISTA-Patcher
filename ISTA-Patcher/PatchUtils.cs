using System.Reflection.Emit;
using Mono.Cecil;
using Mono.Cecil.Cil;
using OpCodes = Mono.Cecil.Cil.OpCodes;

namespace ISTA_Patcher
{
    internal class PatchUtils
    {
        private static string _timestamp = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
        
        public static AssemblyDefinition LoadAssembly(string fileName)
        {
            var assemblyResolver = new DefaultAssemblyResolver();
            assemblyResolver.AddSearchDirectory(Path.GetDirectoryName(fileName));
            var assembly = AssemblyDefinition.ReadAssembly(fileName,
                new ReaderParameters { AssemblyResolver = assemblyResolver, InMemory = true });
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
                return false;
            }

            DoLicenseCheck.ReturnOneMethod();
            return true;
        }

        public static bool PatchVerifyAssemblyHelper(AssemblyDefinition assembly)
        {
            var VerifyStrongName = assembly.GetMethod(
                "BMW.Rheingold.CoreFramework.InteropHelper.VerifyAssemblyHelper",
                "VerifyStrongName",
                "(System.String,System.Boolean)System.Boolean"
            );
            if (VerifyStrongName == null)
            {
                return false;
            }

            VerifyStrongName.ReturnOneMethod();
            return true;
        }

        public static bool PatchFscValidationClient(AssemblyDefinition assembly)
        {
            var IsValid = assembly.GetMethod(
                "BMW.TricTools.FscValidation.FscValidationClient",
                "IsValid",
                "(System.Byte[],System.Byte[])System.Boolean"
            );
            if (IsValid == null)
            {
                return false;
            }

            IsValid.ReturnOneMethod();
            return true;
        }
        
        public static bool PatchMainWindowViewModel(AssemblyDefinition assembly)
        {
            var IsValid = assembly.GetMethod(
                "BMW.Rheingold.ISTAGUI.ViewModels.MainWindowViewModel",
                "CheckExpirationDate",
                "()System.Void"
            );
            if (IsValid == null)
            {
                return false;
            }

            IsValid.EmptyingMethod();
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
                Constant = _timestamp
            };
            var urlField = new FieldDefinition(
                "repo",
                FieldAttributes.Private | FieldAttributes.Static,
                assembly.MainModule.ImportReference(typeof(string)))
            {
                Constant = "https://github.com/tautcony/ISTA-Patcher"
            };

            patchedType.Fields.Add(dateField);
            patchedType.Fields.Add(urlField);
            assembly.MainModule.Types.Add(patchedType);
        }

        public static string DecryptString(string value, int baseSeed, int seed)
        {
            var charArray = value.ToCharArray();
            var key = baseSeed + seed;
            for (var i = 0; i < charArray.Length; i++)
            {
                var ch = charArray[i];
                var chLoByte = (byte)(ch & 0xff);
                var chHiByte = (byte)(ch >> 8);

                var orgHiByte = (byte)(chLoByte ^ key++);
                var orgLoByte = (byte)(chHiByte ^ key++);
                var orgCh = (char)(((uint)orgHiByte << 8) | orgLoByte);
                charArray[i] = orgCh;
            }

            return string.Intern(new string(charArray));
        }

        public static bool DecryptParameter(AssemblyDefinition assembly,
            string typeName = "BMW.Rheingold.CoreFramework.LicenseManagement.LicenseWizardHelper",
            string functionName = "b")
        {
            var b = assembly.GetMethod(
                typeName,
                functionName,
                "(System.String,System.Int32)System.String");
            if (b == null)
            {
                return false;
            }

            var baseSeed = b.Body.Instructions
                .TakeWhile(instruction => instruction.OpCode != OpCodes.Stloc_1)
                .Where(instruction => instruction.OpCode == OpCodes.Ldc_I4)
                .Sum(instruction => (int)instruction.Operand);

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

                        var seed = (VariableDefinition)instruction.Previous.Operand;
                        var seedValue = int.MaxValue;
                        var instruction2 = method.Body.Instructions.FirstOrDefault(inst =>
                            inst.OpCode == OpCodes.Stloc && inst.Operand == seed);
                        if (instruction2 != null)
                        {
                            seedValue = (int)instruction2.Previous.Operand;
                        }

                        if (seedValue == int.MaxValue) continue;
                        var str = (string)instruction.Previous.Previous.Operand;
                        decodedStrings.Add(new KeyValuePair<int, string>(i,
                            PatchUtils.DecryptString(str, baseSeed, seedValue)));
                    }

                    var processor = method.Body.GetILProcessor();
                    decodedStrings.Reverse();
                    foreach (var pair in decodedStrings)
                    {
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
