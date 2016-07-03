﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using SimpleContainer.Implementation;
using SimpleContainer.Infection;
using SimpleContainer.Interface;

namespace SimpleContainer.Helpers
{
	internal static class InternalHelpers
	{
		public static bool IsGood(this ServiceStatus status)
		{
			return status == ServiceStatus.Ok || status == ServiceStatus.NotResolved;
		}

		public static bool IsBad(this ServiceStatus status)
		{
			return status == ServiceStatus.Error || status == ServiceStatus.DependencyError;
		}

		public static string FormatContractsKey(IEnumerable<string> contracts)
		{
			return contracts == null ? null : string.Join("->", contracts);
		}

		public static string NameOf<T>() where T : RequireContractAttribute, new()
		{
			return new T().ContractName;
		}

		public static string[] ParseContracts(MemberInfo provider)
		{
			return ParseContracts(provider.GetCustomAttributes<RequireContractAttribute>());
		}

		public static string[] ParseContracts(ParameterInfo provider)
		{
			return ParseContracts(provider.GetCustomAttributes<RequireContractAttribute>());
		}

		public static string[] ParseContracts(MethodBase provider)
		{
			return ParseContracts(provider.GetCustomAttributes<RequireContractAttribute>());
		}

		public static string[] ParseContracts(Type provider)
		{
			return ParseContracts(provider.GetCustomAttributes<RequireContractAttribute>());
		}

		public static string[] ParseContracts(RequireContractAttribute[] attributes)
		{
			if (attributes.Length == 0)
				return emptyStrings;
			if (attributes.Length > 1)
				throw new SimpleContainerException("assertion failure");
			var contractsSequence = attributes[0] as ContractsSequenceAttribute;
			return contractsSequence == null
				? new[] {attributes[0].ContractName}
				: contractsSequence.ContractAttributeTypes
					.Select(x => ((RequireContractAttribute) Activator.CreateInstance(x)).ContractName)
					.ToArray();
		}

		public static FuncResult<ConstructorInfo> GetConstructor(this Type target)
		{
			var allConstructors = target.GetConstructors();
			ConstructorInfo publicConstructor = null;
			ConstructorInfo containerConstructor = null;
			var hasManyPublicConstructors = false;
			foreach (var constructor in allConstructors)
			{
				if (!constructor.IsPublic)
					continue;
				if (publicConstructor != null)
					hasManyPublicConstructors = true;
				else
					publicConstructor = constructor;
				if (constructor.IsDefined("ContainerConstructorAttribute"))
				{
					if (containerConstructor != null)
						return Result.Fail<ConstructorInfo>("many ctors with [ContainerConstructor] attribute");
					containerConstructor = constructor;
				}
			}
			if (containerConstructor != null)
				return Result.Ok(containerConstructor);
			if (hasManyPublicConstructors)
				return Result.Fail<ConstructorInfo>("many public ctors");
			return publicConstructor == null
				? Result.Fail<ConstructorInfo>("no public ctors")
				: Result.Ok(publicConstructor);
		}

		public static readonly string[] emptyStrings = new string[0];
		public static readonly List<Type> emptyTypesList = new List<Type>(0);
		public static readonly ServiceName[] emptyServiceNames = new ServiceName[0];
		public static Type[] emptyTypes = new Type[0];

		public static string DumpValue(object value)
		{
			if (value == null)
				return "<null>";
			var result = value.ToString();
			return value is bool ? result.ToLower() : result;
		}
	}
}