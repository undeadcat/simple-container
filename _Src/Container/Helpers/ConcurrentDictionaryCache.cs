using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace SimpleContainer.Helpers
{
	internal class ConcurrentDictionaryCache<TKey, TValue> : IConcurrentCache<TKey, TValue>
	{
		private readonly ConcurrentDictionary<TKey, TValue> dictionary = new ConcurrentDictionary<TKey, TValue>(); 
		public TValue GetOrAdd(TKey key, Func<TKey, TValue> creator)
		{
			return dictionary.GetOrAdd(key, creator);
		}

		public bool TryGetValue(TKey key, out TValue value)
		{
			return dictionary.TryGetValue(key, out value);
		}

		public bool TryAdd(TKey key, TValue value)
		{
			return dictionary.TryAdd(key, value);
		}

		public IEnumerable<TValue> Values { get { return dictionary.Values; }}
	}
}