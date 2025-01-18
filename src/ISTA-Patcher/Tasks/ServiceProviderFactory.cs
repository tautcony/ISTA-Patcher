// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: Copyright 2024 TautCony

namespace ISTAPatcher.Tasks;

using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

[ServiceProvider(typeof(IStartupTask))]
public static class ServiceProviderFactory
{
    private static readonly Lazy<ServiceProvider> _serviceProvider = new(BuildServiceProvider);

    public static ServiceProvider Instance => _serviceProvider.Value;

    private static ServiceProvider BuildServiceProvider()
    {
        var serviceCollection = new ServiceCollection();

        var taskTypes = typeof(ServiceProviderFactory).GetCustomAttributes<ServiceProviderAttribute>()
            .SelectMany(attr => attr.TaskType)
            .ToList();

        foreach (var taskType in taskTypes)
        {
            var startupTaskTypes = Assembly.GetExecutingAssembly()
                .GetTypes()
                .Where(t => taskType.IsAssignableFrom(t) && t.IsClass && !t.IsAbstract);

            foreach (var type in startupTaskTypes)
            {
                serviceCollection.AddSingleton(taskType, type);
            }
        }

        return serviceCollection.BuildServiceProvider();
    }

    public static void Run(this IEnumerable<IStartupTask> tasks)
    {
        tasks.ToList().ForEach(startupTask => startupTask.Execute());
    }
}

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
internal sealed class ServiceProviderAttribute(params Type[] taskType) : Attribute
{
    public Type[] TaskType { get; } = taskType;
}
