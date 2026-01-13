/*---------------------------------------------------------------------------------------------
 *  Copyright (c) by Solusek Systems. All rights reserved.
 *--------------------------------------------------------------------------------------------*/

using Ubriaco.Attributes;
using Ubriaco.Data;
using Ubriaco.Models;
using Ubriaco.Test;
using Ubriaco.Test.Attributes;
using Ubriaco.Util;


[Table("testdata")]
public class TestData : BaseModel
{
    public string Name { get; set; } = string.Empty;
    public decimal N { get; set; }
}

[RunTest("transaction")]
public class TransactionTest : BaseTest
{
    public TransactionTest(string name) : base(name)
    {
    }

    public override async Task RunAsync(string[] args)
    {
        BaseConnection.SetConnectionString("Host=localhost;Username=test;Password=Test123!;Database=test");

        using var db = await BaseConnection.CreateAsync();

        await new TestData().SyncStructureAsync(db);

        using var tx = await db.OpenTransaction();

        try
        {
            var item = new TestData()
            {
                Name = "AAA",
                N = 1
            };
            item.ID = await item.InsertAsync(db, tx);
            Console.WriteLine("INSERT");

            var itemB = new TestData()
            {
                Name = "BBB",
                N = 3
            };
            itemB.ID = await itemB.InsertAsync(db, tx);
            Console.WriteLine("INSERT");

            //throw new Exception("ZZZZ");   // throw expection to test rollback

            await tx.CommitAsync();
            Console.WriteLine("COMMIT");
        }
        catch
        {
            await tx.RollbackAsync();
            Console.WriteLine("ROLLBACK");
        }

    }
}