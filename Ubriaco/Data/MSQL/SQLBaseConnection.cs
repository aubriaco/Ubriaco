/*---------------------------------------------------------------------------------------------
 *  Copyright (c) by Solusek Systems. All rights reserved.
 *--------------------------------------------------------------------------------------------*/

using Microsoft.Data.SqlClient;

namespace Ubriaco;

public class SQLBaseConnection : IAsyncDisposable
{
    private SqlConnection _C;

    public SqlConnection C
    {
        get { return _C; }
    }

    public SqlConnection Connection
    {
        get { return _C; }
    }

    private string CS;

    public SQLBaseConnection(string? cs = null)
    {
        if (cs == null) throw new Exception("WHAT CONNECTION?");
        CS = cs;
        _C = new SqlConnection(CS);
    }


    public static async Task<SQLBaseConnection> CreateAsync(string? cs = null)
    {
        var db = new SQLBaseConnection(cs);
        await db.C.OpenAsync();
        return db;
    }

    public async Task CloseAsync()
    {
        await _C.CloseAsync();
    }

    public async ValueTask DisposeAsync()
    {
        await CloseAsync();
        await _C.DisposeAsync();
    }
}