// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: Copyright 2022-2024 TautCony

namespace ISTAlter.Core;

using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using de4dot.code;
using de4dot.code.AssemblyClient;
using de4dot.code.deobfuscators;
using de4dot.code.deobfuscators.Dotfuscator;
using dnlib.DotNet;
using dnlib.DotNet.Writer;
using ISTAlter.Models.Rheingold.DatabaseProvider;
using ISTAlter.Models.Rheingold.LicenseManagement.CoreFramework;
using ISTAlter.Utils;
using Serilog;

/// <summary>
/// A utility class for patching files and directories.
/// Contains helper functions and variables.
/// </summary>
public static partial class PatchUtils
{
    private static readonly string Timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ", CultureInfo.CurrentCulture);
    private static readonly ModuleContext ModCtx = new(TheAssemblyResolver.Instance);
    private static readonly IDeobfuscatorContext DeobfuscatorContext = new DeobfuscatorContext();
    private static readonly NewProcessAssemblyClientFactory ProcessAssemblyClientFactory = new();

    private static byte[] Version
    {
        get
        {
            var infoVersion = Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>();
            var version = string.Empty;
            if (infoVersion != null)
            {
                var match = VersionPattern().Match(infoVersion.InformationalVersion);
                if (!match.Success)
                {
                    version = infoVersion.InformationalVersion;
                }
                else
                {
                    var shortHash = match.Groups["hash"].Value[..7];
                    version = $"{match.Groups["version"].Value}+{shortHash}";
                }
            }

            if (string.IsNullOrEmpty(version))
            {
                version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "0.0.0.0";
            }

            return Encoding.UTF8.GetBytes(version);
        }
    }

    public static string Config => Encoding.UTF8.GetString(((byte[])[
        0x50, 0x6f, 0x77, 0x65, 0x72, 0x65, 0x64, 0x20, 0x62, 0x79, 0x20, 0x49, 0x53, 0x54, 0x41, 0x2d, 0x50, 0x61, 0x74,
        0x63, 0x68, 0x65, 0x72, 0x20,
    ]).Concat(Version).ToArray());

    public static byte[] Source => [
        0x68, 0x74, 0x74, 0x70, 0x73, 0x3a, 0x2f, 0x2f, 0x67, 0x69, 0x74, 0x68, 0x75, 0x62, 0x2e, 0x63, 0x6f, 0x6d,
        0x2f, 0x74, 0x61, 0x75, 0x74, 0x63, 0x6f, 0x6e, 0x79, 0x2f, 0x49, 0x53, 0x54, 0x41, 0x2d, 0x50, 0x61, 0x74,
        0x63, 0x68, 0x65, 0x72,
    ];

    /// <summary>
    /// Loads a module from the specified file.
    /// </summary>
    /// <param name="fileName">The path to the module file.</param>
    /// <returns>The loaded <see cref="dnlib.DotNet.ModuleDefMD"/>.</returns>
    public static ModuleDefMD LoadModule(string fileName)
    {
        var options = new ModuleCreationOptions(ModCtx) { TryToLoadPdbFromDisk = false };
        var module = ModuleDefMD.Load(fileName, options);
        return module;
    }

    /// <summary>
    /// Saves the given assembly module to a file with the specified filename.
    /// </summary>
    /// <param name="module">The <see cref="dnlib.DotNet.ModuleDef"/> to be saved.</param>
    /// <param name="newFilename">The path to the module file will be saved.</param>
    public static void SaveModule(ModuleDefMD module, string newFilename)
    {
        if (module.IsILOnly)
        {
            var writerOptions = new ModuleWriterOptions(module);
            module.Write(newFilename, writerOptions);
        }
        else
        {
            var writerOptions = new NativeModuleWriterOptions(module, optimizeImageSize: true)
            {
                KeepExtraPEData = true,
                KeepWin32Resources = true,
            };
            module.NativeWrite(newFilename, writerOptions);
        }
    }

    /// <summary>
    /// Applies a patch to a method in the specified assembly.
    /// </summary>
    /// <param name="module">The <see cref="dnlib.DotNet.ModuleDefMD"/> to apply the patch to.</param>
    /// <param name="type">The full name of the type containing the method.</param>
    /// <param name="name">The name of the method.</param>
    /// <param name="desc">The description of the method.</param>
    /// <param name="operation">The action representing the patch operation to be applied to the method.</param>
    /// <param name="memberName">The name of the function applying the patch.</param>
    /// <returns>The number of functions patched.</returns>
    public static int PatchFunction(
        this ModuleDefMD module,
        string type,
        string name,
        string desc,
        Action<MethodDef> operation,
        [CallerMemberName] string memberName = "")
    {
        var function = module.GetMethod(type, name, desc);
        Log.Verbose("Applying patch {PatchName} => {Name} <= {Module}: {Result}", memberName, name, module, function != null);
        if (function == null)
        {
            return 0;
        }

        operation(function);
        return 1;
    }

    /// <summary>
    /// Applies a patch to a property getter in the specified assembly.
    /// </summary>
    /// <param name="module">The <see cref="dnlib.DotNet.ModuleDefMD"/> to apply the patch to.</param>
    /// <param name="type">The full name of the type containing the method.</param>
    /// <param name="propertyName">The name of the property.</param>
    /// <param name="operation">The action representing the patch operation to be applied to the method.</param>
    /// <param name="memberName">The name of the function applying the patch.</param>
    /// <returns>The number of functions patched.</returns>
    public static int PatcherGetter(
        this ModuleDefMD module,
        string type,
        string propertyName,
        Action<MethodDef> operation,
        [CallerMemberName] string memberName = "")
    {
        var typeDef = module.GetType(type);
        if (typeDef == null)
        {
            return 0;
        }

        var propertyDef = DnlibUtils.FindPropertyInClassAndBaseClasses(typeDef, propertyName);
        if (propertyDef?.GetMethod == null)
        {
            return 0;
        }

        Log.Verbose("Applying patch {PatchName} => {Name} <= {Module}: {Result}", memberName, $"{propertyName}::getter", module, propertyDef.GetMethod != null);

        operation(propertyDef.GetMethod);
        return 1;
    }

    /// <summary>
    /// Check if the assembly is patched by this patcher.
    /// </summary>
    /// <param name="module">module to check.</param>
    /// <returns>ture for assembly has been patched.</returns>
    public static string? HavePatchedMark(ModuleDefMD module)
    {
        var attribute = module.Assembly.CustomAttributes.FirstOrDefault(attribute =>
            attribute.AttributeType.Name == nameof(AssemblyMetadataAttribute) &&
            attribute.ConstructorArguments.Count == 2 &&
            string.Equals(
                attribute.ConstructorArguments[0].Value.ToString(),
                "Patched.Version",
                StringComparison.Ordinal
                )
            );
        if (attribute != null)
        {
            return attribute.ConstructorArguments[1].Value.ToString();
        }

        var patchedType = module.GetType("Patched.By.TC");
        if (patchedType == null)
        {
            return null;
        }

        var field = patchedType.Fields.FirstOrDefault(field => field.Name == "version");
        var version = field?.Constant.Value as string;
        return version;
    }

    /// <summary>
    /// Set the patched mark to the assembly.
    /// </summary>
    /// <param name="module">module to set.</param>
    public static void SetPatchedMark(ModuleDefMD module)
    {
        if (HavePatchedMark(module) != null)
        {
            return;
        }

        var assemblyTitleAttributeTypeDef = module.CorLibTypes.GetTypeRef("System.Reflection", "AssemblyMetadataAttribute").ResolveTypeDef();

        if (assemblyTitleAttributeTypeDef != null)
        {
            var ctor = module.Import(assemblyTitleAttributeTypeDef.FindConstructors().First());
            var attributes = new List<CustomAttribute>
            {
                new(ctor) { ConstructorArguments = { new CAArgument(module.CorLibTypes.String, "Patched.By"), new CAArgument(module.CorLibTypes.String, "ISTA-Patcher") } },
                new(ctor) { ConstructorArguments = { new CAArgument(module.CorLibTypes.String, "Patched.At"), new CAArgument(module.CorLibTypes.String, Timestamp) } },
                new(ctor) { ConstructorArguments = { new CAArgument(module.CorLibTypes.String, "Patched.Repo"), new CAArgument(module.CorLibTypes.String, Encoding.UTF8.GetString(Source)) } },
                new(ctor) { ConstructorArguments = { new CAArgument(module.CorLibTypes.String, "Patched.Version"), new CAArgument(module.CorLibTypes.String, Encoding.UTF8.GetString(Version)) } },
            };
            foreach (var attribute in attributes)
            {
                module.Assembly.CustomAttributes.Add(attribute);
            }
        }
        else
        {
            var patchedType = new TypeDefUser(
                "Patched.By",
                "TC",
                module.CorLibTypes.Object.TypeDefOrRef)
            {
                Attributes = dnlib.DotNet.TypeAttributes.Class | dnlib.DotNet.TypeAttributes.NestedPrivate,
            };
            var dateField = new FieldDefUser(
                "date",
                new FieldSig(module.CorLibTypes.String),
                dnlib.DotNet.FieldAttributes.Private | dnlib.DotNet.FieldAttributes.Static
            )
            {
                Constant = new ConstantUser(Timestamp),
            };
            var urlField = new FieldDefUser(
                "repo",
                new FieldSig(module.CorLibTypes.String),
                dnlib.DotNet.FieldAttributes.Private | dnlib.DotNet.FieldAttributes.Static
            )
            {
                Constant = new ConstantUser(Encoding.UTF8.GetString(Source)),
            };
            var versionField = new FieldDefUser(
                "version",
                new FieldSig(module.CorLibTypes.String),
                dnlib.DotNet.FieldAttributes.Private | dnlib.DotNet.FieldAttributes.Static
            )
            {
                Constant = new ConstantUser(Encoding.UTF8.GetString(Version)),
            };

            patchedType.Fields.Add(dateField);
            patchedType.Fields.Add(urlField);
            patchedType.Fields.Add(versionField);
            module.Types.Add(patchedType);

            // var runtimeVersion = module.Assembly.ManifestModule.RuntimeVersion;
        }

        var description = module.Assembly.CustomAttributes.FirstOrDefault(attribute =>
            attribute.AttributeType.Name == nameof(AssemblyDescriptionAttribute));
        if (description is { HasConstructorArguments: true })
        {
            description.ConstructorArguments[0] = new CAArgument(module.CorLibTypes.String, Config);
        }

        PatchInteractionModel(module);
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
            ProcessAssemblyClientFactory);
        file.DeobfuscatorContext = DeobfuscatorContext;

        file.Load(new List<IDeobfuscator> { deobfuscatorInfo.CreateDeobfuscator() });
        file.DeobfuscateBegin();
        file.Deobfuscate();
        file.DeobfuscateEnd();
        file.Save();
    }

    public static void CheckPatchVersion(ModuleDefMD module, System.Reflection.MethodInfo? patcher)
    {
        var untilVersion = patcher?.GetCustomAttribute<UntilVersionAttribute>()?.Version;
        if (untilVersion == null)
        {
            return;
        }

        var libraryNames = patcher.GetCustomAttribute<LibraryNameAttribute>()?.FileName;
        if (libraryNames == null || !libraryNames.Contains(module.FullName, StringComparer.Ordinal))
        {
            return;
        }

        var moduleVersion = module.Assembly.Version;
        if (moduleVersion.Major == untilVersion.Major && moduleVersion > untilVersion)
        {
            Log.Warning("{PatcherName} is no longer valid for version {Assembly}({Version})", patcher.Name, module.Assembly.Name, module.Assembly.Version);
        }
    }

    [GeneratedRegex(@"(?<version>\d+\.\d+\.\d+)\+(?<hash>[a-f0-9]+)", RegexOptions.Compiled, matchTimeoutMilliseconds: 1000)]
    private static partial Regex VersionPattern();
}
