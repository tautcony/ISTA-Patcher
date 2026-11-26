// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: Copyright 2026 TautCony

namespace ISTAvalon.Services;

using System.Net;
using System.Net.Sockets;
using Serilog;

public sealed class GuiObservationOptions
{
    public const int DefaultPort = 8765;
    public const string DefaultHost = "127.0.0.1";

    public bool Enabled { get; init; }
    public string Host { get; init; } = DefaultHost;
    public int Port { get; init; } = DefaultPort;

    /// <summary>True when the requested host was not loopback and was replaced with <see cref="DefaultHost"/>.</summary>
    public bool HostWasClampedToLoopback { get; init; }

    public string Prefix => $"http://{Host}:{Port}/";

    public static GuiObservationOptions From(string[] args)
    {
        var enabled = IsTruthy(Environment.GetEnvironmentVariable("ISTA_GUI_DUMP_HTTP"));
        var host = Environment.GetEnvironmentVariable("ISTA_GUI_DUMP_HTTP_HOST") ?? DefaultHost;
        var port = ParsePort(Environment.GetEnvironmentVariable("ISTA_GUI_DUMP_HTTP_PORT")) ?? DefaultPort;

        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            if (arg.Equals("--gui-dump-http", StringComparison.OrdinalIgnoreCase))
            {
                enabled = true;
                continue;
            }

            if (arg.StartsWith("--gui-dump-http=", StringComparison.OrdinalIgnoreCase))
            {
                enabled = IsTruthy(arg["--gui-dump-http=".Length..]);
                continue;
            }

            if (arg.Equals("--gui-dump-http-host", StringComparison.OrdinalIgnoreCase) &&
                i + 1 < args.Length)
            {
                host = args[++i];
                continue;
            }

            if (arg.StartsWith("--gui-dump-http-host=", StringComparison.OrdinalIgnoreCase))
            {
                host = arg["--gui-dump-http-host=".Length..];
                continue;
            }

            if (arg.Equals("--gui-dump-http-port", StringComparison.OrdinalIgnoreCase) &&
                i + 1 < args.Length)
            {
                port = ParsePort(args[++i]) ?? port;
                continue;
            }

            if (arg.StartsWith("--gui-dump-http-port=", StringComparison.OrdinalIgnoreCase))
            {
                port = ParsePort(arg["--gui-dump-http-port=".Length..]) ?? port;
            }
        }

        if (string.IsNullOrWhiteSpace(host))
        {
            host = DefaultHost;
        }

        var clamped = false;
        if (!IsLoopbackHost(host))
        {
            Log.Warning(
                "GUI observation host {Host} is not loopback; binding forced to {DefaultHost}",
                host,
                DefaultHost);
            host = DefaultHost;
            clamped = true;
        }
        else
        {
            host = NormalizeHostForPrefix(host);
        }

        return new GuiObservationOptions
        {
            Enabled = enabled,
            Host = host,
            Port = port,
            HostWasClampedToLoopback = clamped,
        };
    }

    /// <summary>
    /// Returns true for localhost names and IPv4/IPv6 loopback addresses only.
    /// Wildcards and LAN/public addresses are rejected.
    /// </summary>
    public static bool IsLoopbackHost(string host)
    {
        if (string.IsNullOrWhiteSpace(host))
        {
            return false;
        }

        host = host.Trim().Trim('[', ']');

        if (host.Equals("localhost", StringComparison.OrdinalIgnoreCase) ||
            host.Equals("localhost.", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (IPAddress.TryParse(host, out var address))
        {
            return IPAddress.IsLoopback(address);
        }

        return false;
    }

    /// <summary>Formats IPv6 addresses for <see cref="HttpListener"/> prefixes.</summary>
    internal static string NormalizeHostForPrefix(string host)
    {
        host = host.Trim();
        if (host.StartsWith('[') && host.EndsWith(']'))
        {
            return host;
        }

        var bare = host.Trim('[', ']');
        if (IPAddress.TryParse(bare, out var address) &&
            address.AddressFamily == AddressFamily.InterNetworkV6)
        {
            return $"[{address}]";
        }

        return host;
    }

    private static int? ParsePort(string? value) =>
        int.TryParse(value, out var port) && port is > 0 and <= 65535
            ? port
            : null;

    private static bool IsTruthy(string? value) =>
        value is not null &&
        (value.Equals("1", StringComparison.OrdinalIgnoreCase) ||
         value.Equals("true", StringComparison.OrdinalIgnoreCase) ||
         value.Equals("yes", StringComparison.OrdinalIgnoreCase) ||
         value.Equals("on", StringComparison.OrdinalIgnoreCase));
}
