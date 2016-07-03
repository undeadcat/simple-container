using System;
using System.Reflection;
using SimpleContainer.Implementation;

namespace SimpleContainer.Helpers
{
	internal interface ICtorFactory
	{
		object Emit(CtorFactoryCreator.ParameterConfig[] configs, MethodInfo delegateSignature, Type delegateType, ConstructorInfo constructorInfo);
	}
}