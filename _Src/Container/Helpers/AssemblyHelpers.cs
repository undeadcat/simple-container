using System;
using System.Reflection;
using SimpleContainer.Interface;

namespace SimpleContainer.Helpers
{
	internal static class AssemblyHelpers
	{
		public static Assembly LoadAssembly(AssemblyName name)
		{
			try
			{
				return Assembly.Load(name);
			}
			catch (BadImageFormatException e)
			{
				const string messageFormat = "bad assembly image, assembly name [{0}]";
				throw new SimpleContainerException(string.Format(messageFormat, e.FileName), e);
			}
		}
	}
}