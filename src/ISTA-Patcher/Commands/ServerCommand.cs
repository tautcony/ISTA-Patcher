// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: Copyright 2025 TautCony

namespace ISTAPatcher.Commands;

using System.Net;
using DotMake.CommandLine;
using GenHTTP.Engine.Internal;
using GenHTTP.Modules.ApiBrowsing;
using GenHTTP.Modules.Controllers;
using GenHTTP.Modules.Layouting;
using GenHTTP.Modules.OpenApi;
using GenHTTP.Modules.Practices;
using GenHTTP.Modules.Security;
using ISTAPatcher.Controllers;
using ISTAPatcher.Utils;
using Serilog;

[CliCommand(
    Name="server",
    Description = "Perform server-related operations.",
    NameCasingConvention = CliNameCasingConvention.KebabCase,
    NamePrefixConvention = CliNamePrefixConvention.DoubleHyphen,
    ShortFormAutoGenerate = false,
    Parent = typeof(RootCommand)
)]
public class ServerCommand
{
    [CliOption(Description = "Start the server.", Required = false)]
    public bool Start { get; set; }

    [CliOption(Description = "Specify the server port, use 0 to look for an open port.", Required = false)]
    public int ServerPort { get; set; } = 8080;

    [CliOption(Description = "Specify the server host.", Required = false)]
    public string ServerHost { get; set; } = "127.0.0.1";

    public async Task<int> RunAsync()
    {
        if (!this.Start)
        {
            return 0;
        }

        var api = Layout.Create()
            .AddController<iLeanController>("/api/v1/ilean")
            .AddController<CryptoController>("/api/v1/crypto")
            .AddOpenApi()
            .AddScalar()
            .Add(CorsPolicy.Permissive());

        var address = IPAddress.Parse(this.ServerHost);
        var port = this.ServerPort == 0 ? AvailablePorts.GetAvailablePort(8080) : this.ServerPort;

        Log.Information("Server is starting...");
        Log.Information("Service is available at: http://{Host}:{Port}/", address, port);
        Log.Information("API documentation can be accessed at: http://{Host}:{Port}/scalar/", address, port);

        var host = await Host.Create()
            .Bind(address, (ushort)port)
            .Port((ushort)port)
            .Console()
            .Handler(api)
            .Defaults()
            #if DEBUG
            .Development()
            #endif
            .RunAsync();
        return host;
    }
}
