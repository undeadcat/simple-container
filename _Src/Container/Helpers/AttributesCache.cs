using System;
using System.Reflection;

namespace SimpleContainer.Helpers
{
	internal class AttributesCache
	{
		public static readonly AttributesCache instance = new AttributesCache();
		private readonly IConcurrentCache<Key, object> cache = Caches.Create<Key, object>();

		public AttributesCache()
		{
			createDelegate = CreateCustomAttributes;
		}

		private readonly Func<Key, object> createDelegate;

		private static object CreateCustomAttributes(Key key)
		{
			var type = key.attributeProvider as Type;
			if (type != null)
				return type.GetTypeInfo().GetCustomAttributes(key.attributeType, key.inherit);
			var methodBase = key.attributeProvider as MethodBase;
			if (methodBase != null)
				return methodBase.GetCustomAttributes(key.attributeType, key.inherit);
			var memberInfo = key.attributeProvider as MemberInfo;
			if (memberInfo != null)
				return memberInfo.GetCustomAttributes(key.attributeType, key.inherit);

			var parameterInfo = key.attributeProvider as ParameterInfo;
			if (parameterInfo != null)
				return parameterInfo.GetCustomAttributes(key.attributeType, key.inherit);

			var assembly = key.attributeProvider as Assembly;
			if (assembly != null)
				return assembly.GetCustomAttributes(key.attributeType);
			var module = key.attributeProvider as Module;
			if (module != null)
				return module.GetCustomAttributes(key.attributeType);
			throw new InvalidOperationException(string.Format("Could not get custom attributes from type {0}", key.attributeProvider.GetType()));
		}

		public object GetCustomAttributes(object attributeProvider, Type attributeType, bool inherit)
		{
			return cache.GetOrAdd(new Key(attributeProvider, attributeType, inherit), createDelegate);
		}

		private struct Key : IEquatable<Key>
		{
			public readonly object attributeProvider;
			public readonly Type attributeType;
			public readonly bool inherit;

			public Key(object attributeProvider, Type attributeType, bool inherit)
			{
				this.attributeProvider = attributeProvider;
				this.attributeType = attributeType;
				this.inherit = inherit;
			}

			public bool Equals(Key other)
			{
				var localInherit = inherit;
				return attributeProvider.Equals(other.attributeProvider) && attributeType == other.attributeType &&
				       localInherit.Equals(other.inherit);
			}

			public override bool Equals(object obj)
			{
				if (ReferenceEquals(null, obj)) return false;
				return obj is Key && Equals((Key) obj);
			}

			public override int GetHashCode()
			{
				unchecked
				{
					var hashCode = (attributeProvider != null ? attributeProvider.GetHashCode() : 0);
					hashCode = (hashCode*397) ^ (attributeType != null ? attributeType.GetHashCode() : 0);
					var localInherit = inherit;
					hashCode = (hashCode*397) ^ localInherit.GetHashCode();
					return hashCode;
				}
			}
		}
	}
}