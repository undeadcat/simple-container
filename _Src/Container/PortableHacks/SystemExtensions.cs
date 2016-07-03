using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using SimpleContainer.Helpers;

// ReSharper disable once CheckNamespace
namespace System
{
	internal static class SystemExtensions
	{
		private const BindingFlags DefaultLookup = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public;

		public static EventInfo GetEvent(this Type type, string name)
		{
			return type.GetRuntimeEvent(name);
		}

		public static IEnumerable<Type> GetInterfaces(this Type type)
		{
			return type.GetTypeInfo().ImplementedInterfaces;
		}

		public static bool IsAssignableFrom(this Type type, Type otherType)
		{
			return type.GetTypeInfo().IsAssignableFrom(otherType.GetTypeInfo());
		}

		public static IEnumerable<ConstructorInfo> GetConstructors(this Type type)
		{
			return GetConstructors(type, DefaultLookup);
		}

		public static ConstructorInfo GetConstructor(this Type target, Type[] types)
		{
			return
				target.GetConstructors().SingleOrDefault(x => x.GetParameters().Select(p => p.ParameterType).SequenceEqual(types));
		}

		public static bool IsInstanceOfType(this Type type, object obj)
		{
			// ReSharper disable once UseMethodIsInstanceOfType
			return obj != null && type.IsAssignableFrom(obj.GetType());
		}

		public static IEnumerable<PropertyInfo> GetProperties(this Type type)
		{
			return GetProperties(type, DefaultLookup);
		}

		public static IEnumerable<PropertyInfo> GetProperties(this Type type, BindingFlags flags)
		{
			var properties = type.GetRuntimeProperties();
			if ((flags & BindingFlags.DeclaredOnly) == BindingFlags.DeclaredOnly)
				properties = type.GetTypeInfo().DeclaredProperties;

			var props = properties.Select(property => new {property, getMethod = property.GetMethod})
				.Where(@t => @t.getMethod != null);
			if ((flags & BindingFlags.Public) != BindingFlags.Public)
				props = props.Where(x => !x.getMethod.IsPublic);
			if ((flags & BindingFlags.NonPublic) != BindingFlags.NonPublic)
				props = props.Where(x => x.getMethod.IsPublic);
			if ((flags & BindingFlags.Static) != BindingFlags.Static)
				props = props.Where(x => !x.getMethod.IsStatic);
			if ((flags & BindingFlags.Instance) != BindingFlags.Instance)
				props = props.Where(x => x.getMethod.IsStatic);
			return props
				.Select(@t => @t.property);
		}

		public static PropertyInfo GetProperty(this Type type, string name, BindingFlags flags)
		{
			return GetProperties(type, flags).FirstOrDefault(p => p.Name == name);
		}

		public static PropertyInfo GetProperty(this Type type, string name)
		{
			return GetProperties(type, DefaultLookup).FirstOrDefault(p => p.Name == name);
		}

		public static IEnumerable<MethodInfo> GetMethods(this Type type)
		{
			return GetMethods(type, DefaultLookup);
		}

		public static IEnumerable<MethodInfo> GetMethods(this Type type, BindingFlags flags)
		{
			var methods = type.GetRuntimeMethods();
			if ((flags & BindingFlags.DeclaredOnly) == BindingFlags.DeclaredOnly)
				methods = type.GetTypeInfo().DeclaredMethods;
			if ((flags & BindingFlags.Public) != BindingFlags.Public)
				methods = methods.Where(x => !x.IsPublic);
			if ((flags & BindingFlags.NonPublic) != BindingFlags.NonPublic)
				methods = methods.Where(x => x.IsPublic);
			if ((flags & BindingFlags.Static) != BindingFlags.Static)
				methods = methods.Where(x => !x.IsStatic);
			if ((flags & BindingFlags.Instance) != BindingFlags.Instance)
				methods = methods.Where(x => x.IsStatic);
			return methods;
		}

		public static MethodInfo GetMethod(this Type type, string name, BindingFlags flags)
		{
			return GetMethods(type, flags).FirstOrDefault(m => m.Name == name);
		}

		public static MethodInfo GetMethod(this Type type, string name)
		{
			return GetMethods(type, DefaultLookup).FirstOrDefault(m => m.Name == name);
		}

		public static IEnumerable<ConstructorInfo> GetConstructors(this Type type, BindingFlags flags)
		{
			var ctors = type.GetTypeInfo().DeclaredConstructors;
			if ((flags & BindingFlags.Public) != BindingFlags.Public)
				ctors = ctors.Where(x => !x.IsPublic);
			if ((flags & BindingFlags.NonPublic) != BindingFlags.NonPublic)
				ctors = ctors.Where(x => x.IsPublic);
			if ((flags & BindingFlags.Static) != BindingFlags.Static)
				ctors = ctors.Where(x => !x.IsStatic);
			if ((flags & BindingFlags.Instance) != BindingFlags.Instance)
				ctors = ctors.Where(x => x.IsStatic);
			return ctors;
		}

		public static IEnumerable<FieldInfo> GetFields(this Type type)
		{
			return GetFields(type, DefaultLookup);
		}

		public static IEnumerable<FieldInfo> GetFields(this Type type, BindingFlags flags)
		{
			var fields = type.GetRuntimeFields();
			if ((flags & BindingFlags.DeclaredOnly) == BindingFlags.DeclaredOnly)
				fields = type.GetTypeInfo().DeclaredFields;
			if ((flags & BindingFlags.Public) != BindingFlags.Public)
				fields = fields.Where(x => !x.IsPublic);
			if ((flags & BindingFlags.NonPublic) != BindingFlags.NonPublic)
				fields = fields.Where(x => x.IsPublic);
			if ((flags & BindingFlags.Static) != BindingFlags.Static)
				fields = fields.Where(x => !x.IsStatic);
			if ((flags & BindingFlags.Instance) != BindingFlags.Instance)
				fields = fields.Where(x => x.IsStatic);
			return fields;
		}

		public static FieldInfo GetField(this Type type, string name, BindingFlags flags)
		{
			return GetFields(type, flags).FirstOrDefault(p => p.Name == name);
		}

		public static FieldInfo GetField(this Type type, string name)
		{
			return GetFields(type, DefaultLookup).FirstOrDefault(p => p.Name == name);
		}

		public static Type[] GetGenericArguments(this Type type)
		{
			return type.GenericTypeArguments.Length == 0 ? type.GetTypeInfo().GenericTypeParameters : type.GenericTypeArguments;
		}

		public static Type GetNestedType(this Type type, string name)
		{
			var typeInfo = type.GetTypeInfo().GetDeclaredNestedType(name);
			if (typeInfo == null)
				return null;
			return typeInfo.AsType();
		}
	}
}