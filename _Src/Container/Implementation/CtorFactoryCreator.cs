using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using SimpleContainer.Helpers;
using SimpleContainer.Helpers.ReflectionEmit;
using SimpleContainer.Interface;

namespace SimpleContainer.Implementation
{
	internal static class CtorFactoryCreator
	{
		private static readonly ICtorFactory ctorFactory;

		static CtorFactoryCreator()
		{
#if FULLFRAMEWORK
			ctorFactory = new ReflectionEmitCtorFactory();
#else
			ctorFactory = new ReflectionCtorFactory();
#endif
		}

		public static bool TryCreate(ContainerService.Builder builder)
		{
			if (!builder.Type.IsDelegate() || builder.Type.FullName.StartsWith("System.Func`"))
				return false;
			var invokeMethod = builder.Type.GetMethod("Invoke");
			if (!builder.Type.GetTypeInfo().IsNestedPublic)
			{
				builder.SetError(string.Format("can't create delegate [{0}]. must be nested public", builder.Type.FormatName()));
				return true;
			}
			if (invokeMethod.ReturnType != builder.Type.DeclaringType)
			{
				builder.SetError(string.Format("can't create delegate [{0}]. return type must match declaring",
					builder.Type.FormatName()));
				return true;
			}
			const BindingFlags ctorBindingFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
			var constructors = builder.Type.DeclaringType.GetConstructors(ctorBindingFlags)
				.Where(x => Match(invokeMethod, x))
				.ToArray();
			if (constructors.Length == 0)
			{
				builder.SetError("can't find matching ctor");
				return true;
			}
			if (constructors.Length > 1)
			{
				builder.SetError("more than one matched ctors found");
				return true;
			}
			var delegateParameters = invokeMethod.GetParameters();
			var delegateParameterNameToIndexMap = new Dictionary<string, int>();
			for (var i = 0; i < delegateParameters.Length; i++)
				delegateParameterNameToIndexMap[delegateParameters[i].Name] = i;

			var ctorFormalParams = constructors[0].GetParameters();
			var resolvedServices = new Dictionary<Type, ParameterConfig>();
			var parameterConfigs = new ParameterConfig[ctorFormalParams.Length];
			for (var index = 0; index < ctorFormalParams.Length; index++)
			{
				var ctorFormalParam = ctorFormalParams[index];
				int delegateParameterIndex;
				if (delegateParameterNameToIndexMap.TryGetValue(ctorFormalParam.Name, out delegateParameterIndex))
				{
					var delegateParameterType = delegateParameters[delegateParameterIndex].ParameterType;
					if (!ctorFormalParam.ParameterType.IsAssignableFrom(delegateParameterType))
					{
						const string messageFormat = "type mismatch for [{0}], delegate type [{1}], ctor type [{2}]";
						builder.SetError(string.Format(messageFormat,
							ctorFormalParam.Name, delegateParameterType.FormatName(), ctorFormalParam.ParameterType.FormatName()));
						return true;
					}
					parameterConfigs[index] = ParameterConfig.Delegate(delegateParameterIndex + 1);
					delegateParameterNameToIndexMap.Remove(ctorFormalParam.Name);
				}
				else if (ctorFormalParam.ParameterType != typeof (ServiceName))
				{
					ParameterConfig service;
					if (!resolvedServices.TryGetValue(ctorFormalParam.ParameterType, out service))
					{
						var dependency =
							builder.Context.Container.InstantiateDependency(ctorFormalParam, builder).CastTo(ctorFormalParam.ParameterType);
						builder.AddDependency(dependency, false);
						if (dependency.ContainerService != null)
							builder.UnionUsedContracts(dependency.ContainerService);
						if (builder.Status != ServiceStatus.Ok)
							return true;
						var value = dependency.Value;
						service = ParameterConfig.Service(value);
						resolvedServices.Add(ctorFormalParam.ParameterType, service);
						parameterConfigs[index] = service;
					}
					else
						parameterConfigs[index] = service;
				}
			}
			if (delegateParameterNameToIndexMap.Count > 0)
			{
				builder.SetError(string.Format("delegate has not used parameters [{0}]",
					delegateParameterNameToIndexMap.Keys.JoinStrings(",")));
				return true;
			}
			builder.EndResolveDependencies();
			var serviceName = new ServiceName(builder.Type.DeclaringType, builder.FinalUsedContracts);
			for (var i = 0; i < ctorFormalParams.Length; i++)
			{
				if (ctorFormalParams[i].ParameterType == typeof (ServiceName))
					parameterConfigs[i] = ParameterConfig.Service(serviceName);
			}

			builder.AddInstance(ctorFactory.Emit(parameterConfigs, invokeMethod, builder.Type, constructors[0]), true, false);
			return true;
		}

		private static bool Match(MethodInfo method, ConstructorInfo ctor)
		{
			var methodParameters = new Dictionary<string, Type>();
			foreach (var p in method.GetParameters())
				methodParameters[p.Name] = p.ParameterType;
			foreach (var p in ctor.GetParameters())
			{
				Type methodParameterType;
				if (methodParameters.TryGetValue(p.Name, out methodParameterType))
				{
					if (!p.ParameterType.IsAssignableFrom(methodParameterType))
						return false;
				}
				else if (p.ParameterType.IsSimpleType())
					return false;
			}
			return true;
		}

		public struct ParameterConfig
		{
			public bool IsDelegateParam
			{
				get { return DelegateParamIndex.HasValue; }
			}

			public int? DelegateParamIndex { get; set; }
			public object ServiceValue { get; private set; }

			private ParameterConfig(int? delegateParamIndex, object serviceValue)
				: this()
			{
				DelegateParamIndex = delegateParamIndex;
				ServiceValue = serviceValue;
			}

			public static ParameterConfig Delegate(int index)
			{
				return new ParameterConfig(index, null);
			}

			public static ParameterConfig Service(object value)
			{
				return new ParameterConfig(null, value);
			}
		}
	}
}