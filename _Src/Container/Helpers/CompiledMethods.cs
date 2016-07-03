using System;
using System.Reflection;
using SimpleContainer.Helpers.ReflectionEmit;

namespace SimpleContainer.Helpers
{
	internal static class CompiledMethods
	{
		private static readonly IConcurrentCache<MethodBase, Func<object, object[], object>> compiledMethods =
			Caches.Create<MethodBase, Func<object, object[], object>>();

		private static readonly Func<MethodBase, Func<object, object[], object>> compileMethodDelegate;

		static CompiledMethods()
		{
#if FULLFRAMEWORK
			var factory = new EmittedCompiledMethodFactory();
#else
			var factory = new ReflectionCompiledMethodFactory();
#endif
			compileMethodDelegate = factory.EmitCallOf;
		}

		public static Func<object, object[], object> Compile(this MethodBase method)
		{
			return compiledMethods.GetOrAdd(method, compileMethodDelegate);
		}
	}
}