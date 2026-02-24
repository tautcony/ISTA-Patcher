// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: Copyright 2026 TautCony

namespace ISTAvalon.Services;

using System.Reflection;
using System.Text;
using DotMake.CommandLine;
using ISTAvalon.Models;
using ISTAPatcher.Commands;

public static class CommandDiscoveryService
{
    private static readonly string[] TabOrder = ["patch", "ilean", "crypto"];

    private static readonly HashSet<string> ExcludedCommands = new(StringComparer.OrdinalIgnoreCase) { "cerebrumancy", "server" };

    public static IReadOnlyList<CommandDescriptor> DiscoverCommands()
    {
        var assembly = typeof(RootCommand).Assembly;
        var commandTypes = assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && t.GetCustomAttribute<CliCommandAttribute>() is { } attr && attr.Parent == typeof(RootCommand))
            .ToList();

        return commandTypes
            .Select(BuildCommandDescriptor)
            .Where(c => !ExcludedCommands.Contains(c.Name))
            .OrderBy(c =>
            {
                var index = Array.FindIndex(TabOrder, n => string.Equals(n, c.Name, StringComparison.OrdinalIgnoreCase));
                return index >= 0 ? index : TabOrder.Length;
            })
            .ToList();
    }

    private static CommandDescriptor BuildCommandDescriptor(Type commandType)
    {
        var cmdAttr = commandType.GetCustomAttribute<CliCommandAttribute>()!;
        var commandName = !string.IsNullOrEmpty(cmdAttr.Name)
            ? cmdAttr.Name
            : ToKebabCase(commandType.Name.Replace("Command", string.Empty));

        var parameters = new List<ParameterDescriptor>();

        // Collect root command parameters (inherited options like Verbosity).
        CollectParametersFromType(typeof(RootCommand), parameters, isParentOption: true);

        // Collect parameters from the command itself (including base class).
        CollectParametersFromType(commandType, parameters, isParentOption: false);

        // Collect parameters declared on implemented interfaces.
        CollectParametersFromInterfaces(commandType, parameters);

        return new CommandDescriptor
        {
            Name = commandName,
            Description = cmdAttr.Description ?? string.Empty,
            CommandType = commandType,
            IsHidden = cmdAttr.Hidden,
            Parameters = parameters,
        };
    }

    private static void CollectParametersFromType(Type type, List<ParameterDescriptor> results, bool isParentOption)
    {
        while (true)
        {
            var seen = results.Select(p => p.PropertyInfo.Name).ToHashSet();

            foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
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

            // Also scan base class (for classes like OptionalPatchOption).
            if (type.BaseType != null && type.BaseType != typeof(object))
            {
                type = type.BaseType;
                continue;
            }

            break;
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
}
