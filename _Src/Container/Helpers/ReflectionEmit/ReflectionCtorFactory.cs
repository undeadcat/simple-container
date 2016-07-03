using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using SimpleContainer.Implementation;

namespace SimpleContainer.Helpers.ReflectionEmit
{
	internal class ReflectionCtorFactory : ICtorFactory
	{
		public object Emit(CtorFactoryCreator.ParameterConfig[] configs, MethodInfo delegateSignature, Type delegateType,
			ConstructorInfo constructorInfo)
		{
			var delegateFormals = delegateSignature.GetParameters();
			var xDelegateParams = new List<ParameterExpression>(delegateFormals.Length);
			foreach (var parameterInfo in delegateFormals)
				xDelegateParams.Add(Expression.Parameter(parameterInfo.ParameterType, parameterInfo.Name));

			var ctorFormals = constructorInfo.GetParameters();
			var xCtorArgs = new Expression[ctorFormals.Length];
			for (var i = 0; i < configs.Length; i++)
			{
				var config = configs[i];
				if (config.DelegateParamIndex.HasValue)
					xCtorArgs[i] = xDelegateParams[config.DelegateParamIndex.Value - 1];
				else xCtorArgs[i] = Expression.Convert(Expression.Constant(config.ServiceValue), ctorFormals[i].ParameterType);
			}

			var xBody = Expression.New(constructorInfo, xCtorArgs);
			return Expression.Lambda(delegateType, xBody, xDelegateParams).Compile();
		}
	}
}