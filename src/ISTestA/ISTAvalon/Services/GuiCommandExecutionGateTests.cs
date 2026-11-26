// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: Copyright 2026 TautCony

namespace ISTestA.ISTAvalon.Services;

using global::ISTAvalon.Services;

public class GuiCommandExecutionGateTests
{
    [TearDown]
    public void TearDown()
    {
        // Ensure tests never leave the gate locked.
        while (GuiCommandExecutionGate.IsBusy)
        {
            GuiCommandExecutionGate.Exit();
        }
    }

    [Test]
    public void TryEnter_SecondCaller_FailsUntilExit()
    {
        Assert.That(GuiCommandExecutionGate.TryEnter(), Is.True);
        Assert.That(GuiCommandExecutionGate.IsBusy, Is.True);
        Assert.That(GuiCommandExecutionGate.TryEnter(), Is.False);

        GuiCommandExecutionGate.Exit();

        Assert.That(GuiCommandExecutionGate.IsBusy, Is.False);
        Assert.That(GuiCommandExecutionGate.TryEnter(), Is.True);
        GuiCommandExecutionGate.Exit();
    }

    [Test]
    public void StateChanged_FiresOnEnterAndExit()
    {
        var count = 0;
        void Handler() => count++;
        GuiCommandExecutionGate.StateChanged += Handler;
        try
        {
            Assert.That(GuiCommandExecutionGate.TryEnter(), Is.True);
            GuiCommandExecutionGate.Exit();
            Assert.That(count, Is.EqualTo(2));
        }
        finally
        {
            GuiCommandExecutionGate.StateChanged -= Handler;
        }
    }
}
