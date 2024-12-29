// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: Copyright 2024 TautCony

namespace ISTAlter.Utils;

public class TransactionHandler(string name, string operation) : IDisposable
{
    private readonly ITransactionTracer _transaction = SentrySdk.StartTransaction(name, operation);

    public void SetExtra(string key, object value)
    {
        this._transaction.SetExtra(key, value);
    }

    public ISpan StartChild(string operation)
    {
        return this._transaction.StartChild(operation);
    }

    public void Finish()
    {
        this._transaction.Finish();
    }

    public void Dispose()
    {
        this._transaction.Finish();
    }
}
