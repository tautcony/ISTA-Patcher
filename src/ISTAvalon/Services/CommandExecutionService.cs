// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: Copyright 2026 TautCony

namespace ISTAvalon.Services;

using System.Reflection;
using ISTAvalon;
using ISTAvalon.Models;
using ISTAvalon.ViewModels;

public static class CommandExecutionService
{
    public static async Task<int> ExecuteAsync(CommandDescriptor descriptor, IReadOnlyList<ParameterViewModel> parameters)
    {
        var command = Activator.CreateInstance(descriptor.CommandType)!;

        // Set the parent command with parent-level options when available.
        var parentProp = descriptor.CommandType.GetProperty("ParentCommand", BindingFlags.Public | BindingFlags.Instance);
        if (parentProp != null && descriptor.ParentCommandType != null)
        {
            var parentCommand = Activator.CreateInstance(descriptor.ParentCommandType);
            if (parentCommand == null)
            {
                throw new InvalidOperationException($"Unable to create parent command instance for {descriptor.CommandType.Name}.");
            }

            foreach (var param in parameters.Where(p => p.Descriptor.IsParentOption))
            {
                param.Descriptor.PropertyInfo.SetValue(parentCommand, ConvertValue(param));
            }

            parentProp.SetValue(command, parentCommand);
        }

        // Set command-level options and arguments.
        foreach (var param in parameters.Where(p => !p.Descriptor.IsParentOption))
        {
            var value = ConvertValue(param);
            param.Descriptor.PropertyInfo.SetValue(command, value);
        }

        // Invoke RunAsync.
        var runMethod = descriptor.CommandType.GetMethod("RunAsync", BindingFlags.Public | BindingFlags.Instance);
        if (runMethod == null)
        {
            throw new InvalidOperationException($"Command type {descriptor.CommandType.Name} does not have a RunAsync method.");
        }

        using var capture = new ConsoleCaptureScope(App.LogSink.Publish);

        var result = runMethod.Invoke(command, null);
        switch (result)
        {
            case Task<int> taskInt:
                return await taskInt.ConfigureAwait(false);
            case Task task:
                await task.ConfigureAwait(false);
                break;
        }

        return 0;
    }

    private static object? ConvertValue(ParameterViewModel param)
    {
        var value = param.GetValue();
        var targetType = param.Descriptor.PropertyType;

        if (value == null)
        {
            return targetType.IsValueType ? Activator.CreateInstance(targetType) : null;
        }

        // Enum: stored as string, need to parse back.
        if (targetType.IsEnum && value is string strVal)
        {
            return Enum.Parse(targetType, strVal);
        }

        // Numeric: stored as decimal, need to convert.
        if (value is decimal decVal)
        {
            return Convert.ChangeType(decVal, targetType);
        }

        // String[]: stored as raw text, split by commas.
        if (targetType == typeof(string[]) && value is string rawArray)
        {
            return rawArray.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        }

        return value;
    }
}
