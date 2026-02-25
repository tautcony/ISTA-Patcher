// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: Copyright 2026 TautCony

namespace ISTAvalon.Services;

using System.Reflection;
using System.Text;
using DotMake.CommandLine;
using Models;
using ISTAPatcher.Commands;
using Serilog;

public static class CommandDiscoveryService
{
    private static readonly string[] TabOrder = ["patch", "ilean", "crypto"];

    private static readonly HashSet<string> ExcludedCommands = new(StringComparer.OrdinalIgnoreCase)
    {
        "server",
    };

    public static IReadOnlyList<CommandDescriptor> DiscoverCommands(bool includeHidden = false)
    {
        var assembly = typeof(RootCommand).Assembly;
        return DiscoverCommands(assembly.GetTypes(), includeHidden);
    }

    internal static IReadOnlyList<CommandDescriptor> DiscoverCommands(IEnumerable<Type> types, bool includeHidden = false)
    {
        var commandInfos = types
            .Where(t => t.IsClass && !t.IsAbstract)
            .Select(t => new { Type = t, Attr = t.GetCustomAttribute<CliCommandAttribute>() })
            .Where(x => x.Attr is not null)
            .Select(x => new CommandInfo(x.Type, x.Attr!))
            .ToDictionary(x => x.Type, x => x);

        // Build all descriptors first, then attach parent/child relationships.
        foreach (var info in commandInfos.Values)
        {
            info.Name = ResolveCommandName(info.Type, info.Attribute);
            info.ParentType = ResolveParentType(info.Type, info.Attribute, commandInfos);
            info.Parameters = BuildParameters(info.Type, info.ParentType);
        }

        var visibleInfos = commandInfos.Values
            .Where(i => i.IsExecutable)
            .Where(i => !ExcludedCommands.Contains(i.Name))
            .Where(i => includeHidden || !i.Attribute.Hidden)
            .ToDictionary(i => i.Type, i => i);

        var nodeMap = visibleInfos.Values.ToDictionary(i => i.Type, i =>
            new MutableCommandDescriptor
            {
                Name = i.Name,
                Description = i.Attribute.Description ?? string.Empty,
                CommandType = i.Type,
                ParentCommandType = i.ParentType,
                IsHidden = i.Attribute.Hidden,
                Parameters = i.Parameters,
            });

        foreach (var info in visibleInfos.Values)
        {
            var parentType = info.ParentType;
            if (parentType == null)
            {
                continue;
            }

            if (!nodeMap.TryGetValue(parentType, out var parent))
            {
                // Missing/filtered parent keeps the command discoverable as root.
                Log.Debug("Command {CommandName} parent {ParentType} is unavailable; promoting to root", info.Name, parentType.Name);
                continue;
            }

            parent.Subcommands.Add(nodeMap[info.Type]);
        }

        foreach (var descriptor in nodeMap.Values)
        {
            descriptor.Subcommands = descriptor.Subcommands
                .OrderBy(ChildSortKey)
                .ToList();
        }

        var roots = nodeMap.Values
            .Where(c => c.ParentCommandType == null || !nodeMap.ContainsKey(c.ParentCommandType))
            .OrderBy(RootSortKey)
            .Select(Freeze)
            .ToList();

        return roots;
    }

    private static IReadOnlyList<ParameterDescriptor> BuildParameters(Type commandType, Type? parentCommandType)
    {
        var parameters = new List<ParameterDescriptor>();

        if (parentCommandType != null)
        {
            CollectParametersFromTypeHierarchy(parentCommandType, parameters, isParentOption: true);
        }

        CollectParametersFromTypeHierarchy(commandType, parameters, isParentOption: false);
        CollectParametersFromInterfaces(commandType, parameters);

        return parameters;
    }

    private static Type? ResolveParentType(Type commandType, CliCommandAttribute attribute, IReadOnlyDictionary<Type, CommandInfo> allCommands)
    {
        if (commandType.IsNested && commandType.DeclaringType != null && allCommands.ContainsKey(commandType.DeclaringType))
        {
            if (attribute.Parent != null)
            {
                Log.Debug("Ignoring CliCommand.Parent for nested command type {CommandType}", commandType.Name);
            }

            return commandType.DeclaringType;
        }

        if (attribute.Parent == null)
        {
            return null;
        }

        if (allCommands.ContainsKey(attribute.Parent))
        {
            return attribute.Parent;
        }

        Log.Debug("Command {CommandType} points to missing parent type {ParentType}", commandType.Name, attribute.Parent.Name);
        return null;
    }

    private static string ResolveCommandName(Type commandType, CliCommandAttribute cmdAttr)
    {
        return !string.IsNullOrEmpty(cmdAttr.Name)
            ? cmdAttr.Name
            : ToKebabCase(commandType.Name.Replace("Command", string.Empty));
    }

    private static CommandDescriptor Freeze(MutableCommandDescriptor source)
    {
        return new CommandDescriptor
        {
            Name = source.Name,
            Description = source.Description,
            CommandType = source.CommandType,
            ParentCommandType = source.ParentCommandType,
            IsHidden = source.IsHidden,
            Parameters = source.Parameters,
            Subcommands = source.Subcommands.Select(Freeze).ToList(),
        };
    }

    private static string RootSortKey(MutableCommandDescriptor descriptor)
    {
        var index = Array.FindIndex(TabOrder, n => string.Equals(n, descriptor.Name, StringComparison.OrdinalIgnoreCase));
        if (index >= 0)
        {
            return $"00-{index:000}-{descriptor.Name}";
        }

        return $"01-{descriptor.Name}";
    }

    private static string ChildSortKey(MutableCommandDescriptor descriptor)
    {
        return descriptor.Name;
    }

    private static void CollectParametersFromTypeHierarchy(Type type, List<ParameterDescriptor> results, bool isParentOption)
    {
        var hierarchy = new List<Type>();
        var cursor = type;
        while (cursor != typeof(object) && cursor != null)
        {
            hierarchy.Add(cursor);
            cursor = cursor.BaseType!;
        }

        // Derived types override base types when same property appears.
        foreach (var current in hierarchy)
        {
            var seen = results.Select(p => p.PropertyInfo.Name).ToHashSet();

            foreach (var prop in current.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            {
                if (seen.Contains(prop.Name))
                {
                    continue;
                }

                var descriptor = TryBuildParameterDescriptor(prop, isParentOption);
                if (descriptor != null)
                {
                    results.Add(descriptor);
                    seen.Add(prop.Name);
                }
            }
        }
    }

    private static void CollectParametersFromInterfaces(Type commandType, List<ParameterDescriptor> results)
    {
        var seen = results.Select(p => p.PropertyInfo.Name).ToHashSet();

        foreach (var iface in commandType.GetInterfaces())
        {
            foreach (var ifaceProp in iface.GetProperties())
            {
                if (seen.Contains(ifaceProp.Name))
                {
                    continue;
                }

                var implProp = commandType.GetProperty(ifaceProp.Name, BindingFlags.Public | BindingFlags.Instance);
                if (implProp == null)
                {
                    continue;
                }

                // Attribute lives on the interface property.
                var optAttr = ifaceProp.GetCustomAttribute<CliOptionAttribute>();
                var argAttr = ifaceProp.GetCustomAttribute<CliArgumentAttribute>();

                if (optAttr == null && argAttr == null)
                {
                    continue;
                }

                var descriptor = BuildDescriptorFromAttributes(implProp, optAttr, argAttr, isParentOption: false);
                results.Add(descriptor);
                seen.Add(implProp.Name);
            }
        }
    }

    private static ParameterDescriptor? TryBuildParameterDescriptor(PropertyInfo prop, bool isParentOption)
    {
        var optAttr = prop.GetCustomAttribute<CliOptionAttribute>();
        var argAttr = prop.GetCustomAttribute<CliArgumentAttribute>();

        if (optAttr == null && argAttr == null)
        {
            return null;
        }

        return BuildDescriptorFromAttributes(prop, optAttr, argAttr, isParentOption);
    }

    private static ParameterDescriptor BuildDescriptorFromAttributes(
        PropertyInfo prop,
        CliOptionAttribute? optAttr,
        CliArgumentAttribute? argAttr,
        bool isParentOption)
    {
        var isArgument = argAttr != null;
        var description = optAttr?.Description ?? argAttr?.Description ?? string.Empty;
        var isRequired = optAttr?.Required ?? argAttr?.Required ?? false;

        var explicitName = optAttr?.Name ?? argAttr?.Name;
        var displayName = !string.IsNullOrEmpty(explicitName) ? explicitName.TrimStart('-') : ToKebabCase(prop.Name);

        var propertyType = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;
        var kind = DetermineKind(prop, propertyType, optAttr, argAttr);
        var enumValues = propertyType.IsEnum ? Enum.GetNames(propertyType) : [];
        var defaultValue = GetDefaultValue(prop, propertyType, kind);

        return new ParameterDescriptor
        {
            Name = prop.Name,
            DisplayName = displayName,
            Description = description,
            Kind = kind,
            PropertyType = propertyType,
            IsRequired = isRequired,
            IsArgument = isArgument,
            IsParentOption = isParentOption,
            DefaultValue = defaultValue,
            EnumValues = enumValues,
            PropertyInfo = prop,
        };
    }

    private static ParameterKind DetermineKind(PropertyInfo prop, Type propertyType, CliOptionAttribute? optAttr, CliArgumentAttribute? argAttr)
    {
        if (propertyType == typeof(bool))
        {
            return ParameterKind.Bool;
        }

        if (propertyType.IsEnum)
        {
            return ParameterKind.Enum;
        }

        if (IsNumeric(propertyType))
        {
            return ParameterKind.Numeric;
        }

        if (propertyType == typeof(string[]) || (propertyType.IsGenericType && propertyType.GetGenericTypeDefinition() == typeof(List<>) && propertyType.GetGenericArguments()[0] == typeof(string)))
        {
            return ParameterKind.StringArray;
        }

        if (propertyType == typeof(string))
        {
            return IsPathLike(prop, optAttr, argAttr) ? ParameterKind.Path : ParameterKind.String;
        }

        return ParameterKind.Fallback;
    }

    private static bool IsPathLike(PropertyInfo prop, CliOptionAttribute? optAttr, CliArgumentAttribute? argAttr)
    {
        // Definitive: CliValidationRules.ExistingDirectory.
        if (argAttr != null && argAttr.ValidationRules.HasFlag(CliValidationRules.ExistingDirectory))
        {
            return true;
        }

        if (optAttr != null && optAttr.ValidationRules.HasFlag(CliValidationRules.ExistingDirectory))
        {
            return true;
        }

        // Heuristic: property name contains "Directory" or "Folder".
        var name = prop.Name;
        return name.Contains("Directory", StringComparison.OrdinalIgnoreCase) ||
               name.Contains("Folder", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsNumeric(Type type)
    {
        return type == typeof(int) || type == typeof(long) || type == typeof(short) ||
               type == typeof(double) || type == typeof(float) || type == typeof(decimal) ||
               type == typeof(byte) || type == typeof(uint) || type == typeof(ulong) || type == typeof(ushort);
    }

    private static object? GetDefaultValue(PropertyInfo prop, Type propertyType, ParameterKind kind)
    {
        try
        {
            // Create a temporary instance to read default property values.
            var instance = Activator.CreateInstance(prop.DeclaringType!);
            var value = prop.GetValue(instance);

            return kind switch
            {
                ParameterKind.Enum => value?.ToString(),
                _ => value,
            };
        }
        catch
        {
            return kind switch
            {
                ParameterKind.Bool => false,
                ParameterKind.Numeric => 0,
                _ => null,
            };
        }
    }

    private static string ToKebabCase(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return name;
        }

        var sb = new StringBuilder();
        for (var i = 0; i < name.Length; i++)
        {
            if (char.IsUpper(name[i]))
            {
                if (i > 0)
                {
                    sb.Append('-');
                }

                sb.Append(char.ToLowerInvariant(name[i]));
            }
            else
            {
                sb.Append(name[i]);
            }
        }

        return sb.ToString();
    }

    private sealed class CommandInfo(Type type, CliCommandAttribute attribute)
    {
        public Type Type { get; } = type;

        public CliCommandAttribute Attribute { get; } = attribute;

        public string Name { get; set; } = string.Empty;

        public Type? ParentType { get; set; }

        public IReadOnlyList<ParameterDescriptor> Parameters { get; set; } = [];

        public bool IsExecutable =>
            Type.GetMethod("RunAsync", BindingFlags.Public | BindingFlags.Instance) != null ||
            Type.GetMethod("Run", BindingFlags.Public | BindingFlags.Instance) != null;
    }

    private sealed class MutableCommandDescriptor
    {
        public required string Name { get; init; }

        public string Description { get; init; } = string.Empty;

        public required Type CommandType { get; init; }

        public Type? ParentCommandType { get; init; }

        public bool IsHidden { get; init; }

        public required IReadOnlyList<ParameterDescriptor> Parameters { get; init; }

        public List<MutableCommandDescriptor> Subcommands { get; set; } = [];
    }
}
