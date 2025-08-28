// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: Copyright 2022-2025 TautCony

namespace ISTAlter.Core;

using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using dnlib.DotNet.Writer;
using ISTAlter.Utils;
using Serilog;
using static System.Reflection.CustomAttributeExtensions;

/// <summary>
/// A utility class for patching files and directories.
/// </summary>
public static partial class PatchUtils
{
    private static readonly string Timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ", CultureInfo.CurrentCulture);
    private static readonly ModuleContext ModCtx = new();

    private static byte[] Version
    {
        get
        {
            var executingAssembly = System.Reflection.Assembly.GetExecutingAssembly();
            var infoVersion = executingAssembly.GetCustomAttribute<System.Reflection.AssemblyInformationalVersionAttribute>();
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
                version = executingAssembly.GetName().Version?.ToString() ?? "0.0.0.0";
            }

            return Encoding.UTF8.GetBytes(version);
        }
    }

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
    /// <param name="fileName">The path to the module file will be saved.</param>
    public static void SaveModule(ModuleDefMD module, string fileName)
    {
        if (HavePatchedMark(module) == null)
        {
            return;
        }

        if (module.IsILOnly)
        {
            var writerOptions = new ModuleWriterOptions(module);
            module.Write(fileName, writerOptions);
        }
        else
        {
            var writerOptions = new NativeModuleWriterOptions(module, optimizeImageSize: true)
            {
                KeepExtraPEData = true,
                KeepWin32Resources = true,
            };
            module.NativeWrite(fileName, writerOptions);
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
    /// <param name="memberName">The name of the method applying the patch.</param>
    /// <returns>The number of methods patched.</returns>
    public static int PatchFunction(
        this ModuleDefMD module,
        string type,
        string name,
        string desc,
        Action<MethodDef> operation,
        [CallerMemberName] string memberName = "")
    {
        var method = module.GetMethod(type, name, desc);
        Log.Verbose("Applying patch {PatchName} => {Name} <= {Module}: {Result}", memberName, name, module, method != null);
        if (method == null)
        {
            return 0;
        }

        operation(method);
        return 1;
    }

    /// <summary>
    /// Applies a patch to an async method in the specified assembly.
    /// </summary>
    /// <param name="module">The <see cref="dnlib.DotNet.ModuleDefMD"/> to apply the patch to.</param>
    /// <param name="type">The full name of the type containing the method.</param>
    /// <param name="name">The name of the method.</param>
    /// <param name="desc">The description of the method.</param>
    /// <param name="operation">The action representing the patch operation to be applied to the method.</param>
    /// <param name="memberName">The name of the method applying the patch.</param>
    /// <returns>The number of methods patched.</returns>
    public static int PatchAsyncFunction(
        this ModuleDefMD module,
        string type,
        string name,
        string desc,
        Action<MethodDef> operation,
        [CallerMemberName] string memberName = "")
    {
        var method = module.GetMethod(type, name, desc);
        Log.Verbose("Applying patch {PatchName} => {Name} <= {Module}: {Result}", memberName, name, module, method != null);
        if (method == null)
        {
            return 0;
        }

        var asyncStateMachineAttribute = method.CustomAttributes.FirstOrDefault(i => string.Equals(i.TypeFullName, "System.Runtime.CompilerServices.AsyncStateMachineAttribute", StringComparison.Ordinal));
        if (asyncStateMachineAttribute is not { ConstructorArguments: [{ Value: ValueTypeSig stateMachineType }] })
        {
            Log.Warning("Required attribute not found, can not patch {Method}", method.FullName);
            return 0;
        }

        var typeDef = stateMachineType.TypeDefOrRef.ResolveTypeDef();
        if (typeDef?.Methods.FirstOrDefault(m => m.Name == "MoveNext" && m.HasOverrides) is not { } generateMethod)
        {
            Log.Warning("Required attribute not found, can not patch {Method}", method.FullName);
            return 0;
        }

        operation(generateMethod);
        return 1;
    }

    /// <summary>
    /// Applies a patch to a property getter in the specified assembly.
    /// </summary>
    /// <param name="module">The <see cref="dnlib.DotNet.ModuleDefMD"/> to apply the patch to.</param>
    /// <param name="type">The full name of the type containing the method.</param>
    /// <param name="propertyName">The name of the property.</param>
    /// <param name="operation">The action representing the patch operation to be applied to the method.</param>
    /// <param name="memberName">The name of the method applying the patch.</param>
    /// <returns>The number of methods patched.</returns>
    public static int PatchGetter(
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
        var attributeNeo = module.Assembly.CustomAttributes.FirstOrDefault(attr =>
            attr.AttributeType.Name == "PatchedAttribute" &&
            attr.ConstructorArguments.Count == 2 &&
            string.Equals(
                attr.ConstructorArguments[0].Value.ToString(),
                "Version",
                StringComparison.Ordinal
            )
        );
        if (attributeNeo != null)
        {
            return attributeNeo.ConstructorArguments[1].Value.ToString();
        }

        var attribute = module.Assembly.CustomAttributes.FirstOrDefault(attr =>
            attr.AttributeType.Name == nameof(System.Reflection.AssemblyMetadataAttribute) &&
            attr.ConstructorArguments.Count == 2 &&
            string.Equals(
                attr.ConstructorArguments[0].Value.ToString(),
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

        var patchedAttribute = AddPatchedAttribute(module);
        var ctor = patchedAttribute.FindConstructors().First();
        var attributes = new List<CustomAttribute>
        {
            new(ctor) { ConstructorArguments = { new CAArgument(module.CorLibTypes.String, "By"), new CAArgument(module.CorLibTypes.String, "ISTA-Patcher") } },
            new(ctor) { ConstructorArguments = { new CAArgument(module.CorLibTypes.String, "At"), new CAArgument(module.CorLibTypes.String, Timestamp) } },
            new(ctor) { ConstructorArguments = { new CAArgument(module.CorLibTypes.String, "Repo"), new CAArgument(module.CorLibTypes.String, Encoding.UTF8.GetString(Source)) } },
            new(ctor) { ConstructorArguments = { new CAArgument(module.CorLibTypes.String, "Version"), new CAArgument(module.CorLibTypes.String, Encoding.UTF8.GetString(Version)) } },
        };
        foreach (var attribute in attributes)
        {
            module.Assembly.CustomAttributes.Add(attribute);
        }

        var description = module.Assembly.CustomAttributes.FirstOrDefault(attr => attr.AttributeType.Name == nameof(System.Reflection.AssemblyDescriptionAttribute));
        if (description is { HasConstructorArguments: true })
        {
            description.ConstructorArguments[0] = new CAArgument(module.CorLibTypes.String, Encoding.UTF8.GetString(Config));
        }

        PatchInteractionModel(module);
    }

    /// <summary>
    /// Add a patched attribute to the module.
    /// </summary>
    /// <param name="module">module to add.</param>
    /// <returns>The added attribute.</returns>
    public static TypeDefUser AddPatchedAttribute(ModuleDefMD module)
    {
        var attributeType = new TypeDefUser("ISTAttributes", "PatchedAttribute", module.CorLibTypes.GetTypeRef("System", "Attribute"));
        module.Types.Add(attributeType);

        var keyField = new FieldDefUser("key", new FieldSig(module.CorLibTypes.String), FieldAttributes.Public);
        attributeType.Fields.Add(keyField);

        var valueField = new FieldDefUser("value", new FieldSig(module.CorLibTypes.String), FieldAttributes.Public);
        attributeType.Fields.Add(valueField);

        var ctor = new MethodDefUser(
            ".ctor",
            MethodSig.CreateInstance(module.CorLibTypes.Void, module.CorLibTypes.String, module.CorLibTypes.String),
            MethodImplAttributes.IL | MethodImplAttributes.Managed,
            MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName);
        attributeType.Methods.Add(ctor);

        var body = new CilBody();
        ctor.Body = body;
        body.Instructions.Add(OpCodes.Ldarg_0.ToInstruction());
        body.Instructions.Add(OpCodes.Call.ToInstruction(new MemberRefUser(module, ".ctor", MethodSig.CreateInstance(module.CorLibTypes.Void), module.CorLibTypes.GetTypeRef("System", "Attribute"))));
        body.Instructions.Add(OpCodes.Ldarg_0.ToInstruction());
        body.Instructions.Add(OpCodes.Ldarg_1.ToInstruction());
        body.Instructions.Add(OpCodes.Stfld.ToInstruction(keyField));
        body.Instructions.Add(OpCodes.Ldarg_0.ToInstruction());
        body.Instructions.Add(OpCodes.Ldarg_2.ToInstruction());
        body.Instructions.Add(OpCodes.Stfld.ToInstruction(valueField));
        body.Instructions.Add(OpCodes.Ret.ToInstruction());

        return attributeType;
    }

    /// <summary>
    /// Check if the patcher is applicable to the assembly by validating both library name and version.
    /// </summary>
    /// <param name="module">Module to check.</param>
    /// <param name="patcher">Patcher to check.</param>
    /// <returns>True if the patcher is applicable.</returns>
    public static bool IsPatchApplicable(ModuleDefMD module, System.Reflection.MethodInfo? patcher)
    {
        var libraryNames = patcher?.GetCustomAttribute<LibraryNameAttribute>()?.FileName;
        var untilVersion = patcher?.GetCustomAttribute<UntilVersionAttribute>()?.Version;
        var fromVersion = patcher?.GetCustomAttribute<FromVersionAttribute>()?.Version;

        // Validate attribute dependencies
        if ((untilVersion != null || fromVersion != null) && libraryNames == null)
        {
            Log.Error("Patcher {PatcherName} has version constraints but no LibraryName attribute", patcher.Name);
        }

        // Check library name first
        if (libraryNames != null)
        {
            if (!libraryNames.Contains(module.FullName, StringComparer.Ordinal))
            {
                Log.Debug("{PatcherName} is not valid for library: {Library}", patcher.Name, module.FullName);
                return false;
            }
        }
        else
        {
            Log.Debug("Skip library check for {PatchName} due to no library name is set", patcher?.Name);
        }

        // Then check version
        if (untilVersion != null || fromVersion != null)
        {
            var moduleVersion = module.Assembly.Version;

            // A valid patcher should match the version range: moduleVersion ∈ [fromVersion, untilVersion)
            if (fromVersion != null && moduleVersion < fromVersion)
            {
                Log.Warning("{PatcherName} is not valid for assembly yet: {Assembly}({Version})", patcher.Name, module.Assembly.Name, module.Assembly.Version);
                return false;
            }

            if (untilVersion != null && moduleVersion >= untilVersion)
            {
                Log.Warning("{PatcherName} is no longer valid for assembly: {Assembly}({Version})", patcher.Name, module.Assembly.Name, module.Assembly.Version);
                return false;
            }
        }
        else
        {
            Log.Debug("No version check for {PatcherName} has been set", patcher?.Name);
        }

        return true;
    }

    [GeneratedRegex(@"(?<version>\d+\.\d+\.\d+)\+(?<hash>[a-f0-9]+)", RegexOptions.Compiled, matchTimeoutMilliseconds: 1000)]
    private static partial Regex VersionPattern();
}
