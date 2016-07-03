using System;
using System.Collections.Generic;

namespace SimpleContainer.Helpers
{
	internal interface IConcurrentCache<TKey, TValue>
	{
		TValue GetOrAdd(TKey key, Func<TKey, TValue> creator);
		bool TryGetValue(TKey key, out TValue value);
		bool TryAdd(TKey key, TValue value);
		IEnumerable<TValue> Values { get; }
	}
}