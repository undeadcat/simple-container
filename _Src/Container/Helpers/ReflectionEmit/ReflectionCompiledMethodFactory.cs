using System;
using System.Reflection;
using System.Runtime.ExceptionServices;

namespace SimpleContainer.Helpers.ReflectionEmit
{
	internal class ReflectionCompiledMethodFactory : ICompiledMethodFactory
	{
		public Func<object, object[], object> EmitCallOf(MethodBase targetMethod)
		{
			var constructorInfo = targetMethod as ConstructorInfo;
			if (constructorInfo != null)
				return UnwrapTargetInvocationException((_, objects) => constructorInfo.Invoke(objects));
			return UnwrapTargetInvocationException(targetMethod.Invoke);
		}

		private static Func<object, object[], object> UnwrapTargetInvocationException(Func<object, object[], object> method)
		{
			return (target, args) =>
			{
				try
				{
					return method(target, args);
				}
				catch (TargetInvocationException e)
				{
					ExceptionDispatchInfo.Capture(e.InnerException).Throw();
					throw;
				}
			};
		}
	}
}