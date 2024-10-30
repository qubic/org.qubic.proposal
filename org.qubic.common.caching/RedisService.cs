using StackExchange.Redis;
using System.Data.SqlTypes;
using System.Text.Json;

namespace org.qubic.common.caching
{

    /// <summary>
    /// interface needed to configure redis
    /// </summary>
    public interface IRedisConfiguration
    {
        public string Url { get; set; }
    }

    /// <summary>
    /// default implementation of IRedisConfiguration
    /// </summary>
    public class RedisConfigurationDefaultImpl : IRedisConfiguration
    {
        public string Url { get; set; }
    }

    /// <summary>
    /// redis helper class for qubic applications
    /// 
    /// todo: create an interface
    /// 
    /// </summary>
    public class RedisService
    {

        /// <summary>
        /// 
        /// </summary>
        /// <param name="config"></param>
        public RedisService(IRedisConfiguration config)
        {
            connectionString = config.Url; //config.GetSection("redis:url")?.Value;

            // auto connect in initialization
            Connect();
        }


        private string? connectionString;
        private ConnectionMultiplexer _redis;

        private void Connect()
        {
            _redis = ConnectionMultiplexer.Connect(connectionString);
        }

        /// <summary>
        /// the redis database to have it as simple accessor
        /// </summary>
        public IDatabase Database { get { return _redis.GetDatabase(); } }

        public ISubscriber GetSubscriber()
        {
            return _redis.GetSubscriber();
        }

        /// <summary>
        /// ONLY USE THIS WHEN YOU KNOW WHAT YOU DO
        /// IT IS INEFFICIENT!
        /// rebuild an index based on a specific key pattern.
        /// </summary>
        /// <param name="index"></param>
        /// <param name="pattern"></param>
        public async void RebuildIndex(string index, string pattern)
        {
            _redis.GetDatabase().KeyDelete(index);
            UpdateIndex(index, pattern);
        }


        /// <summary>
        /// ONLY USE THIS WHEN YOU KNOW WHAT YOU DO
        /// IT IS INEFFICIENT!
        /// </summary>
        /// <param name="pattern"></param>
        /// <returns></returns>
        public List<string> SearchKeys(string pattern)
        {

            // todo: could be parallized

            // get the target server
            var keyList = new List<string>();
            foreach (var server in _redis.GetServers())
            {
                // show all keys in database 0 that include "foo" in their name
                foreach (var key in server.Keys(pattern: pattern))
                {
                    keyList.Add(key);
                }
            }

            return keyList;
        }

        /// <summary>
        /// ONLY USE THIS WHEN YOU KNOW WHAT YOU DO
        /// IT IS INEFFICIENT!
        /// 
        /// update an index with getting the keys by pattern
        /// </summary>
        /// <param name="index"></param>
        /// <param name="pattern"></param>
        public async void UpdateIndex(string index, string pattern)
        {
            // get the target server
            var keyList = SearchKeys(pattern);

            _ = _redis.GetDatabase().SetAddAsync(index, keyList.Select(s => new RedisValue(s)).ToArray());

        }


        #region index functions
        public void Add(string key, int number)
        {
            _ = Database.StringSetAsync(key, number);
        }
        public void Add(string key, double number)
        {
            _ = Database.StringSetAsync(key, number);
        }

        public long GetSetLength(string setKey)
        {
            return Database.SetLength(setKey);
        }

        public long GetIndexLength(string index)
        {
            return Database.SetLength(index);
        }

        public List<string> GetKeys(string index)
        {
            var output = new List<string>();
            foreach (var redisValue in Database.SetScan(index))
            {
                output.Add(redisValue.ToString());

            }
            return output;
        }

        public List<T?> GetEntries<T>(string index)
           where T : class
        {
            return GetEntries<T>(GetKeys(index));
        }

        public List<T?> GetEntries<T>(List<string> keyList, int batchSize = 10000)
   where T : class
        {
            var result = new List<T?>();

            // Process in batches
            foreach (var batch in SplitIntoBatches(keyList, batchSize))
            {
                var batchResults = Database.StringGet(batch.Select(s => new RedisKey(s)).ToArray()).Select(s =>
                {
                    try
                    {
                        if (!string.IsNullOrEmpty(s))
                        {
                            return JsonSerializer.Deserialize<T>(s);
                        }
                    }
                    catch
                    {
                        //ignore for the moment
                    }
                    return null;
                }).ToList();

                result.AddRange(batchResults);
            }

            return result;
        }

        // Helper method to split the key list into batches
        private IEnumerable<List<string>> SplitIntoBatches(List<string> keyList, int batchSize)
        {
            for (int i = 0; i < keyList.Count; i += batchSize)
            {
                yield return keyList.GetRange(i, Math.Min(batchSize, keyList.Count - i));
            }
        }

        public async void Add<T>(string key, T instance, string index = null)
           where T : class
        {
            Add(key, instance, [index]);
        }

        public async Task<bool> Add<T>(string key, T instance, string[]? indices = null)
            where T : class
        {
            var added = await AddOrUpdate(key, instance);
            if (added && indices != null)
                AddToIndices(key, indices);

            return added;
        }

        public void AddToIndices(string key, params string[] indices)
        {
            foreach (var index in indices.Distinct())
            {
                if (index != null)
                    AddKeyToIndex(index, key);
            }

        }

        public async void AddKeyToIndex(string index, string key)
        {
            await Database.SetAddAsync(index, key);
        }

        public string ConvertToString<T>(T instance)
        {
            if (instance == null)
                return string.Empty;

            if (typeof(T).IsAssignableFrom(typeof(int)) || typeof(T).IsAssignableFrom(typeof(int?)))
            {
                return instance.ToString();
            }
            else if (typeof(T).IsAssignableFrom(typeof(string)))
            {
                return instance.ToString();
            }
            else
            {
                return JsonSerializer.Serialize(instance);
            }

        }

        public async Task<T> Get<T>(string key)
            where T : class
        {
            var stringInstance = await Database.StringGetAsync(key);
            return JsonSerializer.Deserialize<T>(stringInstance);
        }

        public async Task<bool> AddOrUpdate<T>(string key, T instance,
        TimeSpan? expiration = null)
            where T : class
        {

            var fullKey = key;
            try
            {
                await Database.StringSetAsync(fullKey, ConvertToString(instance), expiration);
            }
            catch (Exception ex)
            {
                return false;
                // ignore for the moment
            }

            return true;
        }
        #endregion

    }
}