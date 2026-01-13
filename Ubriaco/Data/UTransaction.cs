/*---------------------------------------------------------------------------------------------
 *  Copyright (c) by Solusek Systems. All rights reserved.
 *--------------------------------------------------------------------------------------------*/

using Npgsql;

namespace Ubriaco.Data;

public class UTransaction : IDisposable
{
    protected BaseConnection DB;
    public NpgsqlTransaction? T;

    public UTransaction(BaseConnection db)
    {
        DB = db;
    }

    public async Task OpenAsync()
    {
        if (DB.C == null) throw new Exception("Connection is broken.");
        T = await DB.C.BeginTransactionAsync();
    }

    public async Task CommitAsync()
    {
        if (T == null) throw new Exception("Transaction is not open.");
        await T.CommitAsync();
    }

    public async Task RollbackAsync()
    {
        if (T == null) throw new Exception("Transaction is not open.");
        await T.RollbackAsync();
    }

    public void Dispose()
    {
        if (T != null) T.Dispose();
    }
}