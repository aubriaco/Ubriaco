/*---------------------------------------------------------------------------------------------
 *  Copyright (c) by Solusek Systems. All rights reserved.
 *--------------------------------------------------------------------------------------------*/

using Microsoft.Data.SqlClient;

namespace Ubriaco;

public class SQLBaseConnection : IAsyncDisposable
{
    private static string? CS = null;
    private SqlConnection _C;

    public SqlConnection C
    {
        get { return _C; }
    }

    public SqlConnection Connection
    {
        get { return _C; }
    }

    private string LocalCS;

    public static void SetConnectionString(string cs)
    {
        CS = cs;
    }

    public SQLBaseConnection(string? cs = null)
    {
        if (cs == null) cs = CS;
        if (cs == null) throw new Exception("WHAT CONNECTION?");
        LocalCS = cs;
        _C = new SqlConnection(LocalCS);
    }


    public static async Task<SQLBaseConnection> CreateAsync(string? cs = null)
    {
        if (cs == null) cs = CS;
        if (cs == null) throw new Exception("WHAT CONNECTION?");
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