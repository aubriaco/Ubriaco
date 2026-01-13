/*---------------------------------------------------------------------------------------------
 *  Copyright (c) by Alessandro Ubriaco. All rights reserved.
 *--------------------------------------------------------------------------------------------*/


namespace Ubriaco.Util;
public static class LockUtil
{
    public static bool Lock(string lockName, string lockIdent, int maxCount, DateTime expires)
    {
        var keyName = "lock-" + lockName + "-" + lockIdent;
        using (var redis = new RedisUtil())
        {
            var data = redis.Get(keyName);
            if (string.IsNullOrEmpty(data))
            {
                redis.Put(keyName, (maxCount - 1).ToString(), expires);
                return true;
            }
            else
            {
                var curCount = Convert.ToInt32(data);
                if (curCount > 0)
                {
                    var exp = redis.GetTTL(keyName);
                    if (exp == null)
                        exp = expires;
                    redis.Put(keyName, (curCount - 1).ToString(), (DateTime)exp);
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }
    }

    public static async Task<bool> LockAsync(string lockName, string lockIdent, int maxCount, DateTime expires)
    {
        var keyName = "lock-" + lockName + "-" + lockIdent;
        using (var redis = await RedisUtil.CreateAsync())
        {
            var data = await redis.GetAsync(keyName);
            if (string.IsNullOrEmpty(data))
            {
                await redis.PutAsync(keyName, (maxCount - 1).ToString(), expires);
                return true;
            }
            else
            {
                var curCount = Convert.ToInt32(data);
                if (curCount > 0)
                {
                    var exp = await redis.GetTTLAsync(keyName);
                    if (exp == null)
                        exp = expires;
                    await redis.PutAsync(keyName, (curCount - 1).ToString(), (DateTime)exp);
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }
    }
}
