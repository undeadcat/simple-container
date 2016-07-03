using System;
using System.Reflection;

namespace SimpleContainer.Helpers.ReflectionEmit
{
	internal class ReflectionMemberAccessorFactory<TOutput> : IMemberAccessorFactory<TOutput>
	{
		private static readonly Action<object, TOutput> emptySetter = (o, value) => { };
		private static readonly Func<object, TOutput> emptyGetter = o => default(TOutput);
		private static readonly object[] emptyObjects = new object[0];

		public Action<object, TOutput> CreateSetter(MemberInfo memberInfo)
		{
			var fieldInfo = memberInfo as FieldInfo;
			if (fieldInfo != null)
				return (o, value) => fieldInfo.SetValue(o, value);
			var propertyInfo = memberInfo as PropertyInfo;
			if (propertyInfo != null)
			{
				var setMethod = propertyInfo.GetSetMethod(true);
				if (setMethod != null)
					return (o, value) => setMethod.Invoke(o, new object[] {value});
			}
			return emptySetter;
		}

		public Func<object, TOutput> CreateGetter(MemberInfo memberInfo)
		{
			var fieldInfo = memberInfo as FieldInfo;
			if (fieldInfo != null)
				return o => (TOutput) fieldInfo.GetValue(o);
			var propertyInfo = memberInfo as PropertyInfo;
			if (propertyInfo != null)
			{
				var getMethod = propertyInfo.GetGetMethod(true);
				if (getMethod != null)
					return o => (TOutput) getMethod.Invoke(o, emptyObjects);
			}
			return emptyGetter;
		}
	}
}