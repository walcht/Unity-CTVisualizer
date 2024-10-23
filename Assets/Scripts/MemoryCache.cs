using System;
using System.Collections.Concurrent;
using System.Diagnostics;

public class CacheEntry<T> where T : unmanaged {
    public readonly T[] data;
    public readonly T max;
    public readonly T min;
    public long timestamp;

    public CacheEntry(T[] data, T max, T min) {
        this.data = data;
        this.max = max;
        this.min = min;
        this.timestamp = DateTimeOffset.Now.ToUnixTimeMilliseconds();
    }
};

public class MemoryCache<T> where T : unmanaged {
    public event Action<UInt32, CacheEntry<T>> CacheEntryLoaded;
    private readonly ConcurrentDictionary<UInt32, CacheEntry<T>> m_cache;

    private object m_lock = new object();

    /// <summary>
    ///     Memory size limit in MB of the dictionary's entries (i.e., not including
    ///     the dictionary size itself). This helps in avoiding potentially expensive
    ///     dictionary resizing operations during runtime.
    /// </summary>
    private readonly int m_capacity;

    public MemoryCache(long memory_size_limit_mb, long brick_size_bytes) {
        m_capacity = (int)Math.Ceiling((memory_size_limit_mb / (brick_size_bytes / 1024.0f)));
        m_cache = new(Environment.ProcessorCount * 2, m_capacity);
    }

    public bool Set(UInt32 id, CacheEntry<T> entry) {
        // check memory size limit
        if (m_cache.Count == m_capacity) {
            // TODO: cache replacement policy implementation
        }
        m_cache.TryAdd(id, entry);
        lock (m_lock) {
            CacheEntryLoaded?.Invoke(id, entry);
        }
        return true;
    }

    /// <summary>
    ///     Tries to get the brick cache entry with the provided id.
    /// </summary>
    ///
    /// <param name="id">
    ///     The unique ID of the brick (TODO see ...)
    /// </param>
    /// 
    /// <returns>
    ///     The chache entry if it exists, null otherwise.
    /// </returns>
    public CacheEntry<T> Get(UInt32 id) {
        // update entry's timestamp
        CacheEntry<T> e = null;
        if (m_cache.TryGetValue(id, out e)) {
            e.timestamp = DateTimeOffset.Now.ToUnixTimeMilliseconds();
        }
        return e;
    }

    public int GetCapacity() => m_capacity;

    /// <summary>
    ///     Evict a cache entry based on the chosen chache replacement
    ///     policy.
    /// </summary>
    private void evict() {
    }
}