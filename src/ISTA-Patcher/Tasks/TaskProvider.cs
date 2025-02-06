// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: Copyright 2024 TautCony

namespace ISTAPatcher.Tasks;

using System.Reflection;

public static class TaskProvider
{
    public static IEnumerable<Type> GatherTasks<T>()
    {
        return ((Type[])[typeof(T)])
            .SelectMany(taskType => Assembly.GetExecutingAssembly()
                .GetTypes()
                .Where(t => taskType.IsAssignableFrom(t) && t.IsClass && !t.IsAbstract)
            );
    }

    public static void Run<T>(this IEnumerable<Type> tasks, T?[] parameters)
    {
        tasks.ToList().ForEach(startupTask =>
        {
            var startupTaskInstance = Activator.CreateInstance(startupTask);
            var executeMethod = startupTask.GetMethod("Execute");
            executeMethod?.Invoke(startupTaskInstance, parameters: [parameters]);
        });
    }
}
