using StackExchange.Redis;
using System.Collections.Concurrent;

namespace org.qubic.proposal.api.Helper
{
    /// <summary>
    /// helps to do a distributed lock
    /// </summary>
    public static class RedisLockHelper
    {
        private static string GetLockKey(string lockKey)
        {
            return lockKey;
        }

        /// <summary>
        /// createa a temp lock
        /// </summary>
        /// <param name="_redisDatabase"></param>
        /// <param name="lockKey"></param>
        /// <param name="expiryTime">default: 60 seconds</param>
        /// <returns></returns>
        public static async Task<bool> TryAcquireLockAsync(IDatabase _redisDatabase, string lockKey, TimeSpan? expiryTime = null)
        {
            if (expiryTime == null)
                expiryTime = TimeSpan.FromSeconds(60);

            string lockValue = Guid.NewGuid().ToString();

            // Try to acquire the lock once
            var wasAcquired = await _redisDatabase.StringSetAsync(
                GetLockKey(lockKey),
                lockValue,
                expiryTime,
                When.NotExists
            );

            if (wasAcquired)
            {
                // Store lock value for release
                LockData.SetLockValue(lockKey, lockValue);
                return true;
            }

            return false; // Return false if the lock was not acquired
        }

        public static async Task ReleaseLockAsync(IDatabase _redisDatabase, string lockKey)
        {
            string lockValue = LockData.GetLockValue(lockKey);

            if (string.IsNullOrEmpty(lockValue))
            {
                throw new InvalidOperationException("Lock was not acquired by this process.");
            }

            var script = @"
                if redis.call('get', KEYS[1]) == ARGV[1] then
                    return redis.call('del', KEYS[1])
                else
                    return 0
                end";

            await _redisDatabase.ScriptEvaluateAsync(script, new RedisKey[] { GetLockKey(lockKey) }, new RedisValue[] { lockValue });

            // Clear lock value after release
            LockData.ClearLockValue(lockKey);
        }

        private static class LockData
        {
            private static readonly ConcurrentDictionary<string, string> _lockValues = new ConcurrentDictionary<string, string>();

            public static void SetLockValue(string key, string value)
            {
                _lockValues[key] = value;
            }

            public static string GetLockValue(string key)
            {
                if (_lockValues != null && _lockValues.TryGetValue(key, out var value))
                {
                    return value;
                }

                return null;
            }

            public static void ClearLockValue(string key)
            {
                _lockValues?.TryRemove(key, out var x);
            }
        }
    }
}
