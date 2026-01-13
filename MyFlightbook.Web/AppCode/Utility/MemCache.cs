using System;
using System.Collections;
using System.Runtime.Caching;

/******************************************************
 * 
 * Copyright (c) 2008-2026 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.Injection
{
    /// <summary>
    /// Concrete memory cache implementation of ICacheService
    /// </summary>
    public class MemCache : ICacheService, IEnumerable
    {
        private static readonly MemoryCache _memCache = new MemoryCache("GlobalCache");

        public int FlushCache()
        {
            int items = 0;
            foreach (DictionaryEntry item in this)
            {
                _memCache.Remove((string) item.Key);
                items++;
            }

            GC.Collect();
            return items;
        }

        public void Remove(string key)
        {
            _memCache.Remove(key);
        }

        object ICacheService.Get(string key)
        {
            return _memCache.Get(key);
        }

        void ICacheService.Set(string key, object value, DateTimeOffset offset)
        {
            _memCache.Set(key, value, offset);
        }

        public IEnumerator GetEnumerator()
        {
            return ((IEnumerable)_memCache).GetEnumerator();
        }
    }
}