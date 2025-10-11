// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: Copyright 2024-2025 TautCony

namespace ISTAlter.Utils;

public class SpanHandler(TransactionHandler transaction, string operation) : IDisposable
{
    private readonly ISpan _span = transaction.StartChild(operation);

    public void SetExtra(string key, object value)
    {
        this._span.SetExtra(key, value);
    }

    public ISpan StartChild(string operation)
    {
        return this._span.StartChild(operation);
    }

    public void Finish()
    {
        if (!this._span.IsFinished)
        {
            this._span.Finish();
        }
    }

    public void Dispose()
    {
        this.Finish();
        GC.SuppressFinalize(this);
    }
}
