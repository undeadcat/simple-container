using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

// ReSharper disable once CheckNamespace
namespace System.Reflection
{
	internal static class SystemReflectionExtensions
	{
		public static IEnumerable<Type> GetTypes(this Assembly assembly)
		{
			return assembly.DefinedTypes.Select(t => t.AsType());
		}

		public static MethodInfo GetAddMethod(this EventInfo eventInfo, bool nonPublic = false)
		{
			if (eventInfo.AddMethod == null || (!nonPublic && !eventInfo.AddMethod.IsPublic))
			{
				return null;
			}

			return eventInfo.AddMethod;
		}

		public static MethodInfo GetRemoveMethod(this EventInfo eventInfo, bool nonPublic = false)
		{
			if (eventInfo.RemoveMethod == null || (!nonPublic && !eventInfo.RemoveMethod.IsPublic))
			{
				return null;
			}

			return eventInfo.RemoveMethod;
		}

		public static MethodInfo GetGetMethod(this PropertyInfo property, bool nonPublic = false)
		{
			if (property.GetMethod == null || (!nonPublic && !property.GetMethod.IsPublic))
			{
				return null;
			}

			return property.GetMethod;
		}

		public static MethodInfo GetSetMethod(this PropertyInfo property, bool nonPublic = false)
		{
			if (property.SetMethod == null || (!nonPublic && !property.SetMethod.IsPublic))
			{
				return null;
			}

			return property.SetMethod;
		}

		public static Stream GetManifestResourceStream(this Assembly assembly,
			Type type,
			string name)
		{
			var sb = new StringBuilder();
			if (type == null)
			{
				if (name == null)
					throw new ArgumentNullException("type");
			}
			else
			{
				var nameSpace = type.Namespace;
				if (nameSpace != null)
				{
					sb.Append(nameSpace);
					if (name != null)
						sb.Append('.');
				}
			}

			if (name != null)
				sb.Append(name);

			return assembly.GetManifestResourceStream(sb.ToString());
		}
	}
}