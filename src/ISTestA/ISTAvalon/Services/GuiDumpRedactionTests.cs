// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: Copyright 2026 TautCony

namespace ISTestA.ISTAvalon.Services;

using global::ISTAvalon.Models;
using global::ISTAvalon.Services;

public class GuiDumpRedactionTests
{
    [Test]
    public void RedactValue_Password_IsRedacted()
    {
        var result = GuiDumpRedaction.RedactValue("Password", ParameterKind.String, "s3cret");

        Assert.That(result, Is.EqualTo("[REDACTED]"));
    }

    [Test]
    public void RedactValue_MachineGuid_IsRedacted()
    {
        var result = GuiDumpRedaction.RedactValue("MachineGuid", ParameterKind.String, "abc-123");

        Assert.That(result, Is.EqualTo("[REDACTED]"));
    }

    [Test]
    public void RedactValue_Path_KeepsFileNameOnly()
    {
        var result = GuiDumpRedaction.RedactValue(
            "TargetPath",
            ParameterKind.Path,
            "/Users/secret/ISTA/4.58");

        Assert.That(result, Is.EqualTo("[REDACTED_PATH]/4.58"));
    }

    [Test]
    public void RedactValue_PlainString_Unchanged()
    {
        var result = GuiDumpRedaction.RedactValue("MarketLanguage", ParameterKind.String, "en-GB");

        Assert.That(result, Is.EqualTo("en-GB"));
    }

    [Test]
    public void RedactLogMessage_StripsSensitiveAssignments()
    {
        const string message = "Password: hunter2 MachineGuid: deadbeef";

        var result = GuiDumpRedaction.RedactLogMessage(message);

        Assert.That(result, Does.Not.Contain("hunter2"));
        Assert.That(result, Does.Not.Contain("deadbeef"));
        Assert.That(result, Does.Contain("[REDACTED]"));
    }
}
