using System.Data;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using Npgsql;
using Ubriaco.Attributes;
using Ubriaco.Data;
using Ubriaco.Models;
using Ubriaco.Test;
using Ubriaco.Test.Attributes;
using Ubriaco.Toolkit;
using Ubriaco.Util;

[RunTest("regression-fixes")]
public class RegressionFixesTest : BaseTest
{
    public RegressionFixesTest(string name) : base(name) { }

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }

    public override Task RunAsync(string[] args)
    {
        const string password = "test password é";
        string hash = CryptUtil.HashPasswordV2(password);
        Check(hash != CryptUtil.HashPasswordV2(password), "Password salts must differ.");
        Check(CryptUtil.VerifyPassword(password, hash), "Current password verification failed.");
        Check(!CryptUtil.VerifyPassword("wrong", hash), "Wrong password accepted.");
        Check(!CryptUtil.VerifyPassword(password, "UBPBKDF2$1$999999999$bad$bad"), "Malformed hash accepted.");
        Check(!CryptUtil.VerifyPassword(password, "not a hash"), "Invalid legacy hash accepted.");
        string legacyHash = Convert.ToBase64String(SHA256.HashData(
            Encoding.UTF8.GetBytes("6238fcf6-72c9-4d97-a39b-f6ed9c45db93" + password)));
        Check(CryptUtil.HashPassword(password) == legacyHash, "Legacy HashPassword integration changed.");
        Check(CryptUtil.VerifyPassword(password, legacyHash), "Legacy password verification failed.");
        Check(CryptUtil.PasswordNeedsRehash(legacyHash) && !CryptUtil.PasswordNeedsRehash(hash), "Rehash detection failed.");

        const string secret = "unicode secret 🔑 longer than thirty-two characters";
        string encrypted = CryptUtil.EncryptV2("hello 🌍", secret);
        Check(encrypted != CryptUtil.EncryptV2("hello 🌍", secret), "Encryption must be randomized.");
        Check(CryptUtil.DecryptV2(encrypted, secret) == "hello 🌍", "Encryption round trip failed.");
        Check(CryptUtil.DecryptV2(CryptUtil.EncryptV2("", secret), secret) == "", "Empty plaintext failed.");
        string[] parts = encrypted.Split('$');
        byte[] payload = Convert.FromBase64String(parts[2]);
        payload[^1] ^= 1;
        bool rejected = false;
        try { CryptUtil.DecryptV2(parts[0] + "$" + parts[1] + "$" + Convert.ToBase64String(payload), secret); }
        catch (CryptographicException) { rejected = true; }
        Check(rejected, "Tampered ciphertext accepted.");
        rejected = false;
        try { CryptUtil.DecryptV2(encrypted, "wrong secret"); }
        catch (CryptographicException) { rejected = true; }
        Check(rejected, "Wrong encryption secret accepted.");
        const string legacySecret = "legacy";
        using (var aes = Aes.Create())
        {
            byte[] key = Encoding.ASCII.GetBytes(legacySecret);
            Array.Resize(ref key, 32);
            aes.Key = key;
            aes.IV = new byte[16];
            using var encryptor = aes.CreateEncryptor();
            byte[] data = Encoding.UTF8.GetBytes("old data");
            string oldCipher = Convert.ToBase64String(encryptor.TransformFinalBlock(data, 0, data.Length));
            Check(CryptUtil.DecryptV2(oldCipher, legacySecret) == "old data", "Legacy ciphertext cannot be read.");
            Check(CryptUtil.Encrypt("old data", legacySecret) == oldCipher, "Legacy Encrypt integration changed.");
            Check(CryptUtil.Decrypt(oldCipher, legacySecret) == "old data", "Legacy Decrypt integration changed.");
        }

        using var table = new DataTable();
        table.Columns.Add("name", typeof(string));
        table.Columns.Add("amount", typeof(int));
        table.Rows.Add("Alice", 12);
        table.Rows.Add(DBNull.Value, DBNull.Value);
        using var reader = table.CreateDataReader();
        var pg = new NullPGModel();
        var sql = new NullSQLModel();
        Check(reader.Read(), "Missing first row.");
        pg.Populate(reader); sql.Populate(reader);
        Check(pg.Name == "Alice" && sql.Name == "Alice", "Initial population failed.");
        Check(reader.Read(), "Missing NULL row.");
        pg.Populate(reader); sql.Populate(reader);
        Check(pg.Name == null && pg.Amount == 0 && sql.Name == null && sql.Amount == 0, "NULL retained stale values.");

        var numeric = new FloatPGModel { Value = 1.25f };
        using var command = new NpgsqlCommand();
        numeric.PutParams(command);
        Check(command.Parameters["Value"].Value is float, "Float parameter serialized as text.");
        using var floatTable = new DataTable();
        floatTable.Columns.Add("value", typeof(double));
        floatTable.Rows.Add(1.25d);
        using var floatReader = floatTable.CreateDataReader();
        Check(floatReader.Read(), "Missing float row.");
        numeric.Value = 0;
        numeric.Populate(floatReader);
        Check(numeric.Value == 1.25f, "Float reader conversion failed.");

        var columnsMethod = typeof(BaseModel).GetMethod("GetCreateTableColumns", BindingFlags.Instance | BindingFlags.NonPublic)!;
        string idOnly = (string)columnsMethod.Invoke(new BaseModel(), null)!;
        Check(idOnly == "ID SERIAL PRIMARY KEY", "ID-only definition has a trailing comma.");
        string columns = (string)columnsMethod.Invoke(numeric, null)!;
        Check(columns.Split(',').All(c => !string.IsNullOrWhiteSpace(c)), "Empty table column definition.");
        Check(columns.Contains("value FLOAT") && columns.Contains("ID SERIAL PRIMARY KEY"), "Missing table columns.");

        string js = FormUtil.GenerateAngularJS(null!, new SelectFormModel(), "test");
        Check(js.Contains("id: 3000000000"), "Long select ID failed or was truncated.");
        LogLine("PASS: password migration, encryption/tampering, NULL reset, float mapping, schema generation, and long IDs.");
        return Task.CompletedTask;
    }

    public class NullPGModel : BaseModel { public string? Name { get; set; } public int Amount { get; set; } }
    public class NullSQLModel : MSQLBaseModel { public string? Name { get; set; } public int Amount { get; set; } }
    public class FloatPGModel : BaseModel { public float Value { get; set; } }
    public class SelectFormModel : BaseModel
    {
        [FormItem(Type = "select", ModelType = typeof(SelectItemModel))]
        public long Selected { get; set; }
    }
    public class SelectItemModel : BaseModel
    {
        public string Name { get; set; } = "Example";
        public override List<BaseModel> ListBase(BaseConnection c, List<string> q, string? tableName = null, string? sort = null, int limit = 0)
            => new() { new SelectItemModel { ID = 3000000000L } };
    }
}

[RunTest("redis-regression-fixes")]
public class RedisRegressionFixesTest : BaseTest
{
    public RedisRegressionFixesTest(string name) : base(name) { }
    private static void Check(bool condition, string message) { if (!condition) throw new Exception(message); }

    public override async Task RunAsync(string[] args)
    {
        string prefix = "ubriaco-test-" + Guid.NewGuid().ToString("N") + ":";
        RedisUtil.Init(args.Length > 1 ? args[1] : "localhost", args.Length > 2 ? int.Parse(args[2]) : 6379, prefix);
        // Run as a separate process so this also exercises first-use initialization.
        await Task.WhenAll(Enumerable.Range(0, 20).Select(async _ =>
        {
            using var r = await RedisUtil.CreateAsync();
            Check(await r.PutAsync("init", "ok"), "Redis unavailable.");
        }));
        using var redis = await RedisUtil.CreateAsync();
        string[] keys = { "init", "ttl", "list", "lock-limit-async", "lock-limit-sync" };
        try
        {
            var expires = DateTime.UtcNow.AddMinutes(1);
            Check(await redis.PutAsync("ttl", "data", expires), "TTL setup failed.");
            Check(await redis.GetTTLAsync("ttl") != null && redis.GetTTL("ttl") != null, "Prefixed TTL lookup failed.");
            Check(await redis.PushAsync("list", "first"), "List setup failed.");
            Check(await redis.PushAsync("list", "second"), "List setup failed.");
            Check(await redis.PeekAsync("list") == "first", "Prefixed peek failed.");
            var results = await Task.WhenAll(Enumerable.Range(0, 100)
                .Select(_ => LockUtil.LockAsync("limit", "async", 7, expires)));
            Check(results.Count(x => x) == 7, "Async acquisitions exceeded or missed limit.");
            int acquired = 0;
            Parallel.For(0, 100, _ => { if (LockUtil.Lock("limit", "sync", 7, expires)) Interlocked.Increment(ref acquired); });
            Check(acquired == 7, "Sync acquisitions exceeded or missed limit.");
            Check(!await LockUtil.LockAsync("limit", "async", 0, expires), "Zero limit accepted.");
            Check(!LockUtil.Lock("limit", "sync", 7, DateTime.UtcNow.AddSeconds(-1)), "Expired lock accepted.");
            Check(await redis.GetTTLAsync("lock-limit-async") != null, "Lock expiration lost.");
            LogLine("PASS: prefixed TTL/peek, concurrent initialization, sync/async slot limits, and expiration.");
        }
        finally { foreach (string key in keys) await redis.DeleteAsync(key); }
    }
}
