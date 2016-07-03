using System;
using System.Collections.Concurrent;
using System.Reflection;

namespace SimpleContainer.Helpers
{
	internal static class Caches
	{
		public static IConcurrentCache<TKey, TValue> Create<TKey, TValue>()
		{
#if FULLFRAMEWORK
			return new ConcurrentDictionaryCache<TKey, TValue>();
#else
			return new SimpleContainer.PortableHacks.NonConcurrentDictionary<TKey, TValue>();
#endif
		}

	}
}