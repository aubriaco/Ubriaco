/*---------------------------------------------------------------------------------------------
 *  Copyright (c) by Alessandro Ubriaco. All rights reserved.
 *--------------------------------------------------------------------------------------------*/

namespace Ubriaco.Util;

public static class LockUtil
{
    /// <summary>Acquires one slot atomically. Allows the action if Redis is unavailable or acquisition fails.</summary>
    public static bool Lock(string lockName, string lockIdent, int maxCount, DateTime expires)
    {
        if (maxCount <= 0 || expires.ToUniversalTime() <= DateTime.UtcNow) return false;
        try
        {
            using var redis = new RedisUtil();
            return redis.AcquireLimit("lock-" + lockName + "-" + lockIdent, maxCount, expires);
        }
        catch (Exception)
        {
            // This is an availability-first safeguard, not an authorization boundary.
            return true;
        }
    }

    /// <summary>Acquires one slot atomically. Allows the action if Redis is unavailable or acquisition fails.</summary>
    public static async Task<bool> LockAsync(string lockName, string lockIdent, int maxCount, DateTime expires)
    {
        if (maxCount <= 0 || expires.ToUniversalTime() <= DateTime.UtcNow) return false;
        try
        {
            using var redis = await RedisUtil.CreateAsync();
            return await redis.AcquireLimitAsync("lock-" + lockName + "-" + lockIdent, maxCount, expires);
        }
        catch (Exception)
        {
            // This is an availability-first safeguard, not an authorization boundary.
            return true;
        }
    }
}
