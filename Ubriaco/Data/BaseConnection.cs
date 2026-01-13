/*---------------------------------------------------------------------------------------------
 *  Copyright (c) by Alessandro Ubriaco. All rights reserved.
 *--------------------------------------------------------------------------------------------*/

using Npgsql;

namespace Ubriaco.Data;

public class BaseConnection : IDisposable
{
    private static string? CS = null;
    public NpgsqlConnection? C;

    protected List<UTransaction> Transactions = new List<UTransaction>();

    public static void SetConnectionString(string cs)
    {
        CS = cs;
    }

    public BaseConnection(bool disableAutoOpen = false)
    {
        C = new NpgsqlConnection(CS);
        if (!disableAutoOpen)
            C.Open();
    }

    public BaseConnection(string cs, bool disableAutoOpen = false)
    {
        C = new NpgsqlConnection(cs);
        if (!disableAutoOpen)
            C.Open();
    }

    public static async Task<BaseConnection> CreateAsync()
    {
        var c = new BaseConnection(true);
        if (c.C == null)
            throw new Exception("Connection is null");
        await c.C.OpenAsync();
        return c;
    }

    public virtual void Dispose()
    {
        if (C != null)
        {
            C.Close();
            C.Dispose();
            C = null;
        }
    }

    public async Task<UTransaction> OpenTransaction()
    {
        var tx = new UTransaction(this);
        Transactions.Add(tx);
        await tx.OpenAsync();
        return tx;
    }

    public async Task CloseTransaction(UTransaction tx)
    {
        Transactions.Remove(tx);
        tx.Dispose();
    }

    public async Task CloseAllTransactions()
    {
        foreach (var tx in Transactions)
        {
            tx.Dispose();
        }
        Transactions.Clear();
    }
}
