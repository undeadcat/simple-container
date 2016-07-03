using System;
using System.Reflection;
using SimpleContainer.Helpers.ReflectionEmit;

namespace SimpleContainer.Helpers
{
	internal static class MemberAccessors
	{
		private static readonly IConcurrentCache<MemberInfo, Func<object, object>> getters =
			Caches.Create<MemberInfo, Func<object, object>>();

		private static readonly IConcurrentCache<MemberInfo, Action<object, object>> setters =
			Caches.Create<MemberInfo, Action<object, object>>();

		private static readonly Func<MemberInfo, Func<object, object>> createGetter;
		private static readonly Func<MemberInfo, Action<object, object>> createSetter;

		static MemberAccessors()
		{
#if FULLFRAMEWORK
			var factory = new EmittedMemberAccessorFactory<object>();
#else
			var factory = new ReflectionMemberAccessorFactory<object>();
#endif
			createGetter = factory.CreateGetter;
			createSetter = factory.CreateSetter;
		}

		public static Func<object, object> GetGetter(MemberInfo member)
		{
			return getters.GetOrAdd(member, createGetter);
		}

		public static Action<object, object> GetSetter(MemberInfo member)
		{
			return setters.GetOrAdd(member, createSetter);
		}
	}
}