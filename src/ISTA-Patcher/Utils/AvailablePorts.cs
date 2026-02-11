// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: Copyright 2025 TautCony

namespace ISTAPatcher.Utils;

using System.Net.NetworkInformation;

public static class AvailablePorts
{
    public static int GetAvailablePort(int startingPort)
    {
        if (startingPort > ushort.MaxValue)
        {
            throw new ArgumentException($"Can't be greater than {ushort.MaxValue}", nameof(startingPort));
        }

        var ipGlobalProperties = IPGlobalProperties.GetIPGlobalProperties();

        var connectionsEndpoints = ipGlobalProperties.GetActiveTcpConnections().Select(c => c.LocalEndPoint);
        var tcpListenersEndpoints = ipGlobalProperties.GetActiveTcpListeners();
        var udpListenersEndpoints = ipGlobalProperties.GetActiveUdpListeners();
        var portsInUse = connectionsEndpoints.Concat(tcpListenersEndpoints)
            .Concat(udpListenersEndpoints)
            .Select(e => e.Port);

        return Enumerable.Range(startingPort, ushort.MaxValue - startingPort + 1).Except(portsInUse).First();
    }
}
