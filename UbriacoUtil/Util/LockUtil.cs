/*---------------------------------------------------------------------------------------------
 *  Copyright (c) by Alessandro Ubriaco. All rights reserved.
 *--------------------------------------------------------------------------------------------*/

namespace Ubriaco.Util;

public static class LockUtil
{
    /// <summary>Acquires one slot atomically. Redis failures propagate to the caller.</summary>
    public static bool Lock(string lockName, string lockIdent, int maxCount, DateTime expires)
    {
        if (maxCount <= 0 || expires.ToUniversalTime() <= DateTime.UtcNow) return false;
        using var redis = new RedisUtil();
        return redis.AcquireLimit("lock-" + lockName + "-" + lockIdent, maxCount, expires);
    }

    /// <summary>Acquires one slot atomically. Redis failures propagate to the caller.</summary>
    public static async Task<bool> LockAsync(string lockName, string lockIdent, int maxCount, DateTime expires)
    {
        if (maxCount <= 0 || expires.ToUniversalTime() <= DateTime.UtcNow) return false;
        using var redis = await RedisUtil.CreateAsync();
        return await redis.AcquireLimitAsync("lock-" + lockName + "-" + lockIdent, maxCount, expires);
    }
}
