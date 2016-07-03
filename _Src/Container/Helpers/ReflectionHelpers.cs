using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace SimpleContainer.Helpers
{
	internal static class ReflectionHelpers
	{
		public static HashSet<Type> GenericParameters(this Type type)
		{
			var result = new HashSet<Type>();
			FillGenericParameters(type, result);
			return result;
		}

		private static void FillGenericParameters(Type t, HashSet<Type> result)
		{
			if (t.IsGenericParameter)
				result.Add(t);
			else if (t.GetTypeInfo().IsGenericType)
				foreach (var x in t.GetGenericArguments())
					FillGenericParameters(x, result);
		}

		public static Type TryCloseByPattern(this Type definition, Type pattern, Type value)
		{
			var argumentsCount = definition.GetGenericArguments().Length;
			var arguments = new Type[argumentsCount];
			if (!pattern.TryMatchWith(value, arguments))
				return null;
			foreach (var argument in arguments)
				if (argument == null)
					return null;
			return definition.MakeGenericType(arguments);
		}

		private static bool TryMatchWith(this Type pattern, Type value, Type[] matched)
		{
			var patternTypeInfo = pattern.GetTypeInfo();
			if (pattern.IsGenericParameter)
			{
				if (value.IsGenericParameter)
					return true;
				foreach (var constraint in patternTypeInfo.GetGenericParameterConstraints())
					if (!constraint.IsAssignableFrom(value))
						return false;
				if (patternTypeInfo.GenericParameterAttributes.HasFlag(GenericParameterAttributes.DefaultConstructorConstraint))
					if (value.GetConstructor(InternalHelpers.emptyTypes) == null)
						return false;
				if (matched != null)
				{
					var position = pattern.GenericParameterPosition;
					if (matched[position] != null && matched[position] != value)
						return false;
					matched[position] = value;
				}
				return true;
			}
			if (patternTypeInfo.IsGenericType ^ value.GetTypeInfo().IsGenericType)
				return false;
			if (!patternTypeInfo.IsGenericType)
				return pattern == value;
			if (pattern.GetGenericTypeDefinition() != value.GetGenericTypeDefinition())
				return false;
			var patternArguments = pattern.GetGenericArguments();
			var valueArguments = value.GetGenericArguments();
			for (var i = 0; i < patternArguments.Length; i++)
				if (!patternArguments[i].TryMatchWith(valueArguments[i], matched))
					return false;
			return true;
		}

		public static Type UnwrapEnumerable(this Type type)
		{
			if (type.GetTypeInfo().IsGenericType && type.GetGenericTypeDefinition() == typeof(IEnumerable<>))
				return type.GetGenericArguments()[0];
			return type.IsArray ? type.GetElementType() : type;
		}

		public static Type MemberType(this MemberInfo memberInfo)
		{
			if (memberInfo is PropertyInfo) return (memberInfo as PropertyInfo).PropertyType;
			if (memberInfo is FieldInfo) return (memberInfo as FieldInfo).FieldType;
			return null;
		}

		public static bool IsStatic(this MemberInfo memberInfo)
		{
			if (memberInfo == null)
				return false;
			var property = memberInfo as PropertyInfo;
			if (property != null)
				return IsStatic(property.GetGetMethod()) || IsStatic(property.GetSetMethod());
			var field = memberInfo as FieldInfo;
			if (field != null)
				return field.IsStatic;
			var method = memberInfo as MethodBase;
			return method != null && method.IsStatic;
		}

		public static bool IsNullableOf(this Type type1, Type type2)
		{
			return Nullable.GetUnderlyingType(type1) == type2;
		}

		public static List<Type> ImplementationsOf(this Type implementation, Type interfaceDefinition)
		{
			var result = new List<Type>();
			if (interfaceDefinition.GetTypeInfo().IsInterface)
			{
				var interfaces = implementation.GetInterfaces();
				foreach (var interfaceImpl in interfaces)
					if (interfaceImpl.GetDefinition() == interfaceDefinition)
						result.Add(interfaceImpl);
			}
			else
			{
				var current = implementation;
				while (current != null)
				{
					if (current.GetDefinition() == interfaceDefinition)
					{
						result.Add(current);
						break;
					}
					current = current.GetTypeInfo().BaseType;
				}
			}
			return result;
		}

		public static string FormatName(this Type type)
		{
			string result;
			if (typeNames.TryGetValue(type, out result))
				return result;
			if (type.IsArray)
				return type.GetElementType().FormatName() + "[]";
			if (type.IsDelegate() && type.IsNested)
				return type.DeclaringType.FormatName() + "." + type.Name;

			if (!type.IsNested || !type.DeclaringType.GetTypeInfo().IsGenericType || type.IsGenericParameter)
				return FormatGenericType(type, type.GetGenericArguments());

			var declaringHierarchy = DeclaringHierarchy(type)
				.TakeWhile(t => t.GetTypeInfo().IsGenericType)
				.Reverse();

			var knownGenericArguments = type.GetGenericTypeDefinition().GetGenericArguments()
				.Zip(type.GetGenericArguments(), (definition, closed) => new {definition, closed})
				.ToDictionary(x => x.definition.GenericParameterPosition, x => x.closed);

			var hierarchyNames = new List<string>();

			foreach (var t in declaringHierarchy)
			{
				var tArguments = t.GetGenericTypeDefinition()
					.GetGenericArguments()
					.Where(x => knownGenericArguments.ContainsKey(x.GenericParameterPosition))
					.ToArray();

				hierarchyNames.Add(FormatGenericType(t,
					tArguments.Select(x => knownGenericArguments[x.GenericParameterPosition]).ToArray()));

				foreach (var tArgument in tArguments)
					knownGenericArguments.Remove(tArgument.GenericParameterPosition);
			}
			return string.Join(".", hierarchyNames.ToArray());
		}

		private static IEnumerable<Type> DeclaringHierarchy(Type type)
		{
			yield return type;
			while (type.DeclaringType != null)
			{
				yield return type.DeclaringType;
				type = type.DeclaringType;
			}
		}

		private static string FormatGenericType(Type type, Type[] arguments)
		{
			var genericMarkerIndex = type.Name.IndexOf("`", StringComparison.Ordinal);
			return genericMarkerIndex > 0
				? string.Format("{0}<{1}>", type.Name.Substring(0, genericMarkerIndex),
					arguments.Select(FormatName).JoinStrings(","))
				: type.Name;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Type GetDefinition(this Type type)
		{
			var typeInfo = type.GetTypeInfo();
			return typeInfo.IsGenericType && !typeInfo.IsGenericTypeDefinition ? type.GetGenericTypeDefinition() : type;
		}

		public static bool IsSimpleType(this Type type)
		{
			if (simpleTypes.Contains(type) || type.GetTypeInfo().IsEnum)
				return true;
			var nullableWrapped = Nullable.GetUnderlyingType(type);
			return nullableWrapped != null && nullableWrapped.IsSimpleType();
		}

		public static bool IsDelegate(this Type type)
		{
			return type.GetTypeInfo().BaseType == typeof(MulticastDelegate);
		}

		private static readonly ISet<Type> simpleTypes = new HashSet<Type>
		{
			typeof(byte),
			typeof(short),
			typeof(ushort),
			typeof(int),
			typeof(uint),
			typeof(long),
			typeof(ulong),
			typeof(double),
			typeof(float),
			typeof(string),
			typeof(Guid),
			typeof(bool),
			typeof(DateTime),
			typeof(TimeSpan)
		};

		private static readonly IDictionary<Type, string> typeNames = new Dictionary<Type, string>
		{
			{typeof(object), "object"},
			{typeof(byte), "byte"},
			{typeof(short), "short"},
			{typeof(ushort), "ushort"},
			{typeof(int), "int"},
			{typeof(uint), "uint"},
			{typeof(long), "long"},
			{typeof(ulong), "ulong"},
			{typeof(double), "double"},
			{typeof(float), "float"},
			{typeof(string), "string"},
			{typeof(bool), "bool"}
		};

		#region Attributes

		public static TAttribute[] GetCustomAttributes<TAttribute>(this Type attributeProvider,
			bool inherit = true)
		{
			return (TAttribute[]) (object) attributeProvider.GetCustomAttributesCached(typeof(TAttribute), inherit);
		}

		public static IEnumerable<Attribute> GetCustomAttributesCached(this Type attributeProvider,
			Type type, bool inherit = true)
		{
			return (IEnumerable<Attribute>) AttributesCache.instance.GetCustomAttributes(attributeProvider, type, inherit);
		}

		public static bool TryGetCustomAttribute<TAttribute>(this Type memberInfo, out TAttribute result)
			where TAttribute : Attribute
		{
			var attributes = memberInfo.GetCustomAttributes<TAttribute>();
			if (attributes.Length == 1)
			{
				result = attributes[0];
				return true;
			}
			result = null;
			return false;
		}

		public static bool IsDefined<TAttribute>(this Type type, bool inherit = true)
			where TAttribute : Attribute
		{
			return type.GetCustomAttributes<TAttribute>(inherit).Any();
		}

		public static bool IsDefined(this Type customAttributeProvider, string attributeName)
		{
			return customAttributeProvider.GetTypeInfo().GetCustomAttributes(false).Any(a => a.GetType().Name == attributeName);
		}

		public static TAttribute[] GetCustomAttributes<TAttribute>(this MethodBase attributeProvider,
			bool inherit = true)
		{
			return (TAttribute[]) (object) attributeProvider.GetCustomAttributesCached(typeof(TAttribute), inherit);
		}

		public static IEnumerable<Attribute> GetCustomAttributesCached(this MethodBase attributeProvider,
			Type type, bool inherit = true)
		{
			return (IEnumerable<Attribute>) AttributesCache.instance.GetCustomAttributes(attributeProvider, type, inherit);
		}

		public static bool TryGetCustomAttribute<TAttribute>(this MethodBase memberInfo, out TAttribute result)
			where TAttribute : Attribute
		{
			var attributes = memberInfo.GetCustomAttributes<TAttribute>();
			if (attributes.Length == 1)
			{
				result = attributes[0];
				return true;
			}
			result = null;
			return false;
		}

		public static bool IsDefined<TAttribute>(this MethodBase type, bool inherit = true)
			where TAttribute : Attribute
		{
			return type.GetCustomAttributes<TAttribute>(inherit).Any();
		}

		public static bool IsDefined(this MethodBase customAttributeProvider, string attributeName)
		{
			return customAttributeProvider.GetCustomAttributes(false).Any(a => a.GetType().Name == attributeName);
		}


		public static TAttribute[] GetCustomAttributes<TAttribute>(this ParameterInfo attributeProvider,
			bool inherit = true)
		{
			return (TAttribute[]) (object) attributeProvider.GetCustomAttributesCached(typeof(TAttribute), inherit);
		}

		public static IEnumerable<Attribute> GetCustomAttributesCached(this ParameterInfo attributeProvider,
			Type type, bool inherit = true)
		{
			return (IEnumerable<Attribute>) AttributesCache.instance.GetCustomAttributes(attributeProvider, type, inherit);
		}

		public static bool TryGetCustomAttribute<TAttribute>(this ParameterInfo memberInfo, out TAttribute result)
			where TAttribute : Attribute
		{
			var attributes = memberInfo.GetCustomAttributes<TAttribute>();
			if (attributes.Length == 1)
			{
				result = attributes[0];
				return true;
			}
			result = null;
			return false;
		}

		public static bool IsDefined<TAttribute>(this ParameterInfo type, bool inherit = true)
			where TAttribute : Attribute
		{
			return type.GetCustomAttributes<TAttribute>(inherit).Any();
		}

		public static bool IsDefined(this ParameterInfo customAttributeProvider, string attributeName)
		{
			return customAttributeProvider.GetCustomAttributes(false).Any(a => a.GetType().Name == attributeName);
		}


		public static TAttribute[] GetCustomAttributes<TAttribute>(this MemberInfo attributeProvider,
			bool inherit = true)
		{
			return (TAttribute[]) (object) attributeProvider.GetCustomAttributesCached(typeof(TAttribute), inherit);
		}

		public static IEnumerable<Attribute> GetCustomAttributesCached(this MemberInfo attributeProvider,
			Type type, bool inherit = true)
		{
			return (IEnumerable<Attribute>) AttributesCache.instance.GetCustomAttributes(attributeProvider, type, inherit);
		}

		public static bool TryGetCustomAttribute<TAttribute>(this MemberInfo memberInfo, out TAttribute result)
			where TAttribute : Attribute
		{
			var attributes = memberInfo.GetCustomAttributes<TAttribute>();
			if (attributes.Length == 1)
			{
				result = attributes[0];
				return true;
			}
			result = null;
			return false;
		}

		public static bool IsDefined<TAttribute>(this MemberInfo type, bool inherit = true)
			where TAttribute : Attribute
		{
			return type.GetCustomAttributes<TAttribute>(inherit).Any();
		}

		public static bool IsDefined(this MemberInfo customAttributeProvider, string attributeName)
		{
			return customAttributeProvider.GetCustomAttributes(false).Any(a => a.GetType().Name == attributeName);
		}

		#endregion
	}
}