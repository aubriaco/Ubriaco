/*---------------------------------------------------------------------------------------------
 *  Copyright (c) by Alessandro Ubriaco. All rights reserved.
 *--------------------------------------------------------------------------------------------*/



using StackExchange.Redis;

namespace Ubriaco.Util
{
    /// <summary>
    /// Provides a set of methods for interacting with a Redis server.
    /// </summary>
    public class RedisUtil : IDisposable
    {
        private static ConnectionMultiplexer? R = null;
        private static readonly SemaphoreSlim ConnectionGate = new(1, 1);

        private static string Host = "localhost";
        private static int Port = 6379;
        private static string Prefix = "";
        private static bool EnableSSL = false;
        private bool AsyncMode = false;



        /// <summary>
        /// Initializes the RedisUtil class with the specified host, port, prefix, and SSL settings.
        /// </summary>
        /// <param name="host">The Redis server host.</param>
        /// <param name="port">The Redis server port.</param>
        /// <param name="prefix">The key prefix.</param>
        /// <param name="ssl">Indicates whether SSL is enabled.</param>
        public static void Init(string host, int port = 6379, string prefix = "", bool ssl = false)
        {
            if (host != null)
                Host = host;
            Port = port;
            Prefix = prefix;
            EnableSSL = ssl;
        }

        /// <summary>
        /// Initializes the RedisUtil class with the specified host, port, and prefix.
        /// </summary>
        public RedisUtil(bool noAutoConnect = false)
        {
            if (!noAutoConnect)
                Connect();
        }

        private static ConfigurationOptions GetConfiguration() => new()
        {
            EndPoints = { Host + ":" + Port },
            Ssl = EnableSSL,
            SslProtocols = EnableSSL
                ? System.Security.Authentication.SslProtocols.Tls12 | System.Security.Authentication.SslProtocols.Tls13
                : System.Security.Authentication.SslProtocols.None,
            AbortOnConnectFail = false
        };

        public void Connect()
        {
            ConnectionGate.Wait();
            try
            {
                if (R == null)
                    R = ConnectionMultiplexer.Connect(GetConfiguration());
            }
            finally { ConnectionGate.Release(); }
        }

        public async Task ConnectAsync()
        {
            await ConnectionGate.WaitAsync();
            try
            {
                if (R == null)
                    R = await ConnectionMultiplexer.ConnectAsync(GetConfiguration());
            }
            finally { ConnectionGate.Release(); }
        }

        // The complete acquisition executes on Redis atomically. Errors intentionally propagate:
        // inability to contact Redis must never be interpreted as permission to acquire a slot.
        private const string AcquireLimitScript = @"
local expires = tonumber(ARGV[2])
local now = redis.call('TIME')
local nowMillis = tonumber(now[1]) * 1000 + math.floor(tonumber(now[2]) / 1000)
if expires <= nowMillis then return 0 end
local remaining = redis.call('GET', KEYS[1])
if not remaining then
    redis.call('SET', KEYS[1], tonumber(ARGV[1]) - 1)
    redis.call('PEXPIREAT', KEYS[1], ARGV[2])
    return 1
end
if tonumber(remaining) <= 0 then return 0 end
redis.call('DECR', KEYS[1])
if redis.call('PTTL', KEYS[1]) == -1 then
    redis.call('PEXPIREAT', KEYS[1], ARGV[2])
end
return 1";

        internal bool AcquireLimit(string key, int maxCount, DateTime expires)
        {
            if (maxCount <= 0) return false;
            var result = R!.GetDatabase().ScriptEvaluate(AcquireLimitScript,
                new RedisKey[] { Prefix + key },
                new RedisValue[] { maxCount, new DateTimeOffset(expires.ToUniversalTime()).ToUnixTimeMilliseconds() });
            return (long)result == 1;
        }

        internal async Task<bool> AcquireLimitAsync(string key, int maxCount, DateTime expires)
        {
            if (maxCount <= 0) return false;
            var result = await R!.GetDatabase().ScriptEvaluateAsync(AcquireLimitScript,
                new RedisKey[] { Prefix + key },
                new RedisValue[] { maxCount, new DateTimeOffset(expires.ToUniversalTime()).ToUnixTimeMilliseconds() });
            return (long)result == 1;
        }

        public static async Task<RedisUtil> CreateAsync()
        {
            var r = new RedisUtil(true);
            r.AsyncMode = true;
            await r.ConnectAsync();
            return r;
        }



        /// <summary>
        /// Retrieves the value associated with the specified key from Redis.
        /// </summary>
        /// <param name="key">The key of the value to retrieve.</param>
        /// <returns>The value associated with the specified key, or null if the key does not exist.</returns>
        public string? Get(string key)
        {
            try
            {
                var db = R.GetDatabase();
                var s = db.StringGet(Prefix + key);
                return s.ToString();
            }
            catch
            {
                return null;
            }
        }

        public async Task<string?> GetAsync(string key)
        {
            try
            {
                var db = R.GetDatabase();
                var s = await db.StringGetAsync(Prefix + key);
                return s.ToString();
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Puts the value associated with the specified key into Redis.
        /// </summary>
        /// <param name="key">The key of the value to put.</param>
        /// <param name="val">The value to put.</param>
        /// <returns>True if success.</returns>
        public bool Put(string key, string val)
        {
            try
            {
                var db = R.GetDatabase();
                db.StringSet(Prefix + key, val);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> PutAsync(string key, string val)
        {
            try
            {
                var db = R.GetDatabase();
                await db.StringSetAsync(Prefix + key, val);
                return true;
            }
            catch
            {
                return false;
            }
        }


        /// <summary>
        /// Puts the value associated with the specified key into Redis with an expiration date.
        /// </summary>
        /// <param name="key">The key of the value to put.</param>
        /// <param name="val">The value to put.</param>
        /// <param name="expires">The expiration date of the value.</param>
        /// <returns>True if success.</returns>
        public bool Put(string key, string val, DateTime expires)
        {
            try
            {
                var db = R.GetDatabase();
                db.StringSet(Prefix + key, val);
                db.KeyExpire(Prefix + key, expires);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> PutAsync(string key, string val, DateTime expires)
        {
            try
            {
                var db = R.GetDatabase();
                await db.StringSetAsync(Prefix + key, val);
                await db.KeyExpireAsync(Prefix + key, expires);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Gets the time to live (TTL) of the specified key.
        /// </summary>
        /// <param name="key">The key to get the TTL of.</param>
        /// <returns>The TTL of the specified key, or null if the key does not exist.</returns>        
        public DateTime? GetTTL(string key)
        {
            try
            {
                var db = R.GetDatabase();
                return db.KeyExpireTime(Prefix + key);
            }
            catch
            {
                return null;
            }
        }

        public async Task<DateTime?> GetTTLAsync(string key)
        {
            try
            {
                var db = R.GetDatabase();
                return await db.KeyExpireTimeAsync(Prefix + key);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Check if key exists.
        /// </summary>
        /// <param name="key">The key to check.</param>
        /// <returns>True if key exists.</returns>
        /// <returns>False if key does not exist.</returns>
        public bool Exits(string key)
        {
            try
            {
                var db = R.GetDatabase();
                return db.KeyExists(Prefix + key);
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> ExitsAsync(string key)
        {
            try
            {
                var db = R.GetDatabase();
                return await db.KeyExistsAsync(Prefix + key);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Pushes a value to the end of a list.
        /// </summary>
        /// <param name="key">The key of the list.</param>
        /// <param name="value">The value to push.</param>
        /// <returns>True if success.</returns>
        /// <returns>False if failed.</returns>
        public bool Push(string key, string value)
        {
            try
            {
                var db = R.GetDatabase();
                db.ListLeftPush(Prefix + key, value);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> PushAsync(string key, string value)
        {
            try
            {
                var db = R.GetDatabase();
                await db.ListLeftPushAsync(Prefix + key, value);
                return true;
            }
            catch
            {
                return false;
            }
        }


        /// <summary>
        /// Gets a value at the beginning of a list.
        /// </summary>
        /// <param name="key">The key of the list.</param>
        /// <returns>The value removed from the list, or null if the list is empty.</returns>
        public string? Shift(string key)
        {
            try
            {
                var db = R.GetDatabase();
                var s = db.ListLeftPop(Prefix + key).ToString();
                if (string.IsNullOrEmpty(s)) return null;
                return s;
            }
            catch
            {
                return null;
            }
        }

        public async Task<string?> ShiftAsync(string key)
        {
            try
            {
                var db = R.GetDatabase();
                var s = (await db.ListLeftPopAsync(Prefix + key)).ToString();
                if (string.IsNullOrEmpty(s)) return null;
                return s;
            }
            catch
            {
                return null;
            }
        }


        /// <summary>
        /// Gets a value from the end of the list.
        /// </summary>
        /// <param name="key">The key of the list.</param>
        /// <returns>The value removed from the list, or null if the list is empty.</returns>
        public string? Pop(string key)
        {
            try
            {
                var db = R.GetDatabase();
                var s = db.ListRightPop(Prefix + key).ToString();
                if (string.IsNullOrEmpty(s)) return null;
                return s;
            }
            catch
            {
                return null;
            }
        }

        public async Task<string?> PopAsync(string key)
        {
            try
            {
                var db = R.GetDatabase();
                var s = (await db.ListRightPopAsync(Prefix + key)).ToString();
                if (string.IsNullOrEmpty(s)) return null;
                return s;
            }
            catch
            {
                return null;
            }
        }

        public async Task<string?> PeekAsync(string key)
        {
            try
            {
                var db = R.GetDatabase();
                var value = await db.ListRangeAsync(Prefix + key, -1, -1);

                if (value.Length > 0)
                {
                    return value[0];
                }

                return null;
            }
            catch
            {
                return null;
            }
        }


        /// <summary>
        /// Gets the length of a list.
        /// </summary>
        /// <param name="key">The key of the list.</param>
        /// <returns>The length of the list.</returns>
        public long Len(string key)
        {
            try
            {
                var db = R.GetDatabase();
                return db.ListLength(Prefix + key);
            }
            catch
            {
                return 0;
            }
        }

        public async Task<long> LenAsync(string key)
        {
            try
            {
                var db = R.GetDatabase();
                return await db.ListLengthAsync(Prefix + key);
            }
            catch
            {
                return 0;
            }
        }

        /// <summary>
        /// Deletes the specified key from Redis.
        /// </summary>
        /// <param name="key">The key to delete.</param>
        /// <returns>True if success.</returns>
        public bool Delete(string key)
        {
            try
            {
                var db = R.GetDatabase();
                db.KeyDelete(Prefix + key);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> DeleteAsync(string key)
        {
            try
            {
                var db = R.GetDatabase();
                await db.KeyDeleteAsync(Prefix + key);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Disposes object.
        /// </summary>
        public async void Dispose()
        {

        }
    }

}
