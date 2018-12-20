using Microsoft.Extensions.Caching.Distributed;
using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Extensions.Caching.Distributed
{
    public static class IDistributedCacheExtensions
    {
        /// <summary>
        /// 扩展Get方法,支持泛型
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="cache"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        public static T Get<T>(this IDistributedCache cache, string key)
        {
            var buffer = cache.Get(key);
            if (buffer == null)
            {
                return default(T);
            }
            var strContent = Encoding.UTF8.GetString(buffer);

            return Newtonsoft.Json.JsonConvert.DeserializeObject<T>(strContent);
        }

        /// <summary>
        /// 扩展Set方法,支持泛型
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="cache"></param>
        /// <param name="key"></param>
        /// <param name="obj"></param>
        /// <param name="expireTimeSpan"></param>
        public static void Set<T>(this IDistributedCache cache, string key, T obj, TimeSpan expireTimeSpan)
        {
            var buffer = Encoding.UTF8.GetBytes(Newtonsoft.Json.JsonConvert.SerializeObject(obj));
            cache.Set(key, buffer, new DistributedCacheEntryOptions() { AbsoluteExpirationRelativeToNow = expireTimeSpan });
        }

        /// <summary>
        /// 扩展Set方法,支持泛型
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="cache"></param>
        /// <param name="key"></param>
        /// <param name="obj"></param>
        /// <param name="expireTimeSpan"></param>
        public static void Set<T>(this IDistributedCache cache, string key, T obj, DateTimeOffset expireTimeSpan)
        {
            var buffer = Encoding.UTF8.GetBytes(Newtonsoft.Json.JsonConvert.SerializeObject(obj));
            cache.Set(key, buffer, new DistributedCacheEntryOptions() { AbsoluteExpiration = expireTimeSpan });
        }

    }
}
