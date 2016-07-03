using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using SimpleContainer.Implementation;

namespace SimpleContainer.Helpers.ReflectionEmit
{
	internal class ReflectionEmitCtorFactory : ICtorFactory
	{
		public object Emit(CtorFactoryCreator.ParameterConfig[] configs, MethodInfo delegateSignature, Type delegateType,
			ConstructorInfo constructorInfo)
		{
			var delegateParameters = delegateSignature.GetParameters();
			var dynamicMethodParameterTypes = new Type[delegateParameters.Length + 1];
			dynamicMethodParameterTypes[0] = typeof (object[]);
			for (var i = 1; i < dynamicMethodParameterTypes.Length; i++)
				dynamicMethodParameterTypes[i] = delegateParameters[i - 1].ParameterType;
			var dynamicMethod = new DynamicMethod(delegateSignature.ReturnType.Name + "_ctor", delegateSignature.ReturnType,
				dynamicMethodParameterTypes, typeof (ReflectionEmitCtorFactory), true);
			var il = dynamicMethod.GetILGenerator();
			var serviceCount = 0;
			for (var i = 0; i < configs.Length; i++)
			{
				var arg = configs[i];
				if (arg.DelegateParamIndex.HasValue)
					il.EmitLdArg(arg.DelegateParamIndex.Value);
				else
				{
					il.Emit(OpCodes.Ldarg_0);
					il.EmitLdInt32(serviceCount);
					il.Emit(OpCodes.Ldelem_Ref);
					serviceCount++;

					var ctorArgType = constructorInfo.GetParameters()[i].ParameterType;
					il.Emit(ctorArgType.IsValueType ? OpCodes.Unbox_Any : OpCodes.Castclass, ctorArgType);
				}
			}
			il.Emit(OpCodes.Newobj, constructorInfo);
			il.Emit(OpCodes.Ret);
			var services = new List<object>(serviceCount);
			foreach (var config in configs)
			{
				if (!config.IsDelegateParam)
					services.Add(config.ServiceValue);
			}
			return dynamicMethod.CreateDelegate(delegateType, services.ToArray());
		}
	}
}