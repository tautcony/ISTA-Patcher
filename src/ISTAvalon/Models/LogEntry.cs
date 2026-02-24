// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: Copyright 2026 TautCony

namespace ISTAvalon.Models;

using Serilog.Events;

public sealed record LogEntry(DateTimeOffset Timestamp, LogEventLevel Level, string Message);
