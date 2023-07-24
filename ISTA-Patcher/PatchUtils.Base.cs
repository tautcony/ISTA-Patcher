// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: Copyright 2022-2023 TautCony

// ReSharper disable CommentTypo
namespace ISTA_Patcher;

using System.Runtime.CompilerServices;
using de4dot.code;
using de4dot.code.AssemblyClient;
using de4dot.code.deobfuscators;
using de4dot.code.deobfuscators.Dotfuscator;
using dnlib.DotNet;
using Serilog;
using AssemblyDefinition = dnlib.DotNet.AssemblyDef;

/// <summary>
/// A utility class for patching files and directories.
/// Contains helper functions and variables.
/// </summary>
internal static partial class PatchUtils
{
    private static readonly string Timestamp = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
    private static readonly ModuleContext ModCtx = ModuleDef.CreateModuleContext();
    private static readonly IDeobfuscatorContext DeobfuscatorContext = new DeobfuscatorContext();
    private static readonly NewProcessAssemblyClientFactory ProcessAssemblyClientFactory = new();

    private static string Version
    {
        get
        {
            var version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version ?? new Version("0.0.0.0");
            return version.ToString();
        }
    }

    /// <summary>
    /// Loads a module from the specified file.
    /// </summary>
    /// <param name="fileName">The path to the module file.</param>
    /// <returns>The loaded <see cref="ModuleDefMD"/>.</returns>
    public static ModuleDefMD LoadModule(string fileName)
    {
        var module = ModuleDefMD.Load(fileName, ModCtx);
        return module;
    }

    /// <summary>
    /// Applies a patch to a method in the specified assembly.
    /// </summary>
    /// <param name="assembly">The <see cref="AssemblyDefinition"/> to apply the patch to.</param>
    /// <param name="type">The full name of the type containing the method.</param>
    /// <param name="name">The name of the method.</param>
    /// <param name="desc">The description of the method.</param>
    /// <param name="operation">The action representing the patch operation to be applied to the method.</param>
    /// <param name="memberName">The name of the function applying the patch.</param>
    /// <returns>The number of functions patched.</returns>
    private static int PatchFunction(
        this AssemblyDefinition assembly,
        string type,
        string name,
        string desc,
        Action<MethodDef> operation,
        [CallerMemberName] string memberName = "")
    {
        var function = assembly.GetMethod(type, name, desc);
        Log.Verbose("Applying patch {PatchName} => {Name} <= {Assembly}: {Result}", memberName, name, assembly, function != null);
        if (function == null)
        {
            return 0;
        }

        operation(function);
        return 1;
    }

    /// <summary>
    /// Check if the assembly is patched by this patcher.
    /// </summary>
    /// <param name="assembly">assembly to check.</param>
    /// <returns>ture for assembly has been patched.</returns>
    public static bool HavePatchedMark(AssemblyDefinition assembly)
    {
        var patchedType = assembly.Modules.First().GetType("Patched.By.TC");
        return patchedType != null;
    }

    /// <summary>
    /// Set the patched mark to the assembly.
    /// </summary>
    /// <param name="assembly">assembly to set.</param>
    public static void SetPatchedMark(AssemblyDefinition assembly)
    {
        var module = assembly.Modules.FirstOrDefault();
        if (module == null || HavePatchedMark(assembly))
        {
            return;
        }

        var patchedType = new TypeDefUser(
            "Patched.By",
            "TC",
            module.CorLibTypes.Object.TypeDefOrRef)
        {
            Attributes = TypeAttributes.Class | TypeAttributes.NestedPrivate,
        };
        var dateField = new FieldDefUser(
            "date",
            new FieldSig(module.CorLibTypes.String),
            FieldAttributes.Private | FieldAttributes.Static
        )
        {
            Constant = new ConstantUser(Timestamp),
        };
        var urlField = new FieldDefUser(
            "repo",
            new FieldSig(module.CorLibTypes.String),
            FieldAttributes.Private | FieldAttributes.Static
        )
        {
            Constant = new ConstantUser("https://github.com/tautcony/ISTA-Patcher"),
        };

        var versionField = new FieldDefUser(
            "version",
            new FieldSig(module.CorLibTypes.String),
            FieldAttributes.Private | FieldAttributes.Static
        )
        {
            Constant = new ConstantUser(Version),
        };

        patchedType.Fields.Add(dateField);
        patchedType.Fields.Add(urlField);
        patchedType.Fields.Add(versionField);
        module.Types.Add(patchedType);
    }

    /// <summary>
    /// Performs deobfuscation on an obfuscated file and saves the result to a new file.
    /// </summary>
    /// <param name="fileName">The path to the obfuscated file.</param>
    /// <param name="newFileName">The path to save the deobfuscated file.</param>
    public static void DeObfuscation(string fileName, string newFileName)
    {
        var deobfuscatorInfo = new DeobfuscatorInfo();

        using var file = new ObfuscatedFile(
            new ObfuscatedFile.Options
        {
            ControlFlowDeobfuscation = true,
            Filename = fileName,
            NewFilename = newFileName,
            StringDecrypterType = DecrypterType.Static,
        },
            ModCtx,
            ProcessAssemblyClientFactory)
        {
            DeobfuscatorContext = DeobfuscatorContext,
        };

        file.Load(new List<IDeobfuscator> { deobfuscatorInfo.CreateDeobfuscator() });
        file.DeobfuscateBegin();
        file.Deobfuscate();
        file.DeobfuscateEnd();
        file.Save();
    }
}
