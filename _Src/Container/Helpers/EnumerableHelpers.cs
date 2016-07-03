﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace SimpleContainer.Helpers
{
	internal static class EnumerableHelpers
	{
		public static bool IsEmpty(this IEnumerable source)
		{
			return source == null || !source.Cast<object>().Any();
		}

		public static bool IsEquivalentTo<T>(this IEnumerable<T> source, IEnumerable<T> other,
			IEqualityComparer<T> comparer = null)
		{
			var set = new HashSet<T>(source, comparer ?? EqualityComparer<T>.Default);
			set.SymmetricExceptWith(other);
			return set.IsEmpty();
		}

		public static bool TrySingle<T>(this IEnumerable<T> source, out T result)
		{
			var slice = source.Take(2).ToArray();
			if (slice.Any())
			{
				result = slice.Single();
				return true;
			}
			result = default(T);
			return false;
		}

		public static bool SafeTrySingle<T>(this IEnumerable<T> source, out T result)
		{
			var slice = source.Take(2).ToArray();
			if (slice.Length == 1)
			{
				result = slice.Single();
				return true;
			}
			result = default(T);
			return false;
		}

		public static object[] CastToObjectArrayOf(this IEnumerable source, Type itemType)
		{
			return (object[]) source.CastToArrayOf(itemType);
		}

		public static Array CastToArrayOf(this IEnumerable source, Type itemType)
		{
			var sourceArray = source.Cast<object>().ToArray();
			var result = Array.CreateInstance(itemType, sourceArray.Length);
			Array.Copy(sourceArray, result, sourceArray.Length);
			return result;
		}

		public static string JoinStrings(this IEnumerable<string> source, string separator)
		{
			return string.Join(separator, source.ToArray());
		}

		public static IEnumerable<T> Concat<T>(this IEnumerable<T> source, params T[] value)
		{
			return source.Concat(value.AsEnumerable());
		}

		public static bool All<T>(this IEnumerable<T> source, Func<T, int, bool> filter)
		{
			return !source.Where((x, i) => !filter(x, i)).Any();
		}

		public static IEnumerable<T> Return<T>(T item)
		{
			yield return item;
		}

		public static IEnumerable<T> Prepend<T>(this IEnumerable<T> source, T value)
		{
			return new[] {value}.Concat(source);
		}

		public static IEnumerable<T> NotNull<T>(this IEnumerable<T> source)
		{
			return source.Where(x => !ReferenceEquals(x, default(T)));
		}

		public static IEnumerable<T> EmptyIfNull<T>(this IEnumerable<T> source)
		{
			return source ?? Enumerable.Empty<T>();
		}

		public static bool SafeTrySingle<T>(this IEnumerable<T> source, Func<T, bool> filter, out T result)
		{
			return source.Where(filter).SafeTrySingle(out result);
		}

		public static ISet<T> ToSet<T>(this IEnumerable<T> source)
		{
			return new HashSet<T>(source);
		}
	}
}