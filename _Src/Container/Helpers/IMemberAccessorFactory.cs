using System;
using System.Reflection;

namespace SimpleContainer.Helpers
{
	internal interface IMemberAccessorFactory<TOutput>
	{
		Action<object, TOutput> CreateSetter(MemberInfo memberInfo);
		Func<object, TOutput> CreateGetter(MemberInfo memberInfo);
	}
}