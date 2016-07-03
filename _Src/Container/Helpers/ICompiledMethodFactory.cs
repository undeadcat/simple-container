using System;
using System.Reflection;

namespace SimpleContainer.Helpers
{
	internal interface ICompiledMethodFactory
	{
		Func<object, object[], object> EmitCallOf(MethodBase targetMethod);
	}
}