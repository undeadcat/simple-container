using System;
using System.IO;
using System.Linq;
using System.Reflection;
using SimpleContainer.Configuration;
using SimpleContainer.Helpers;

namespace SimpleContainer
{
	public static class ContainerFactoryExtensions
	{
		public static ContainerFactory WithTypesFromDefaultBinDirectory(this ContainerFactory containerFactory,
			bool withExecutables)
		{
			return containerFactory.WithTypesFromDirectory(GetBinDirectory(), withExecutables);
		}

		public static ContainerFactory WithTypesFromDirectory(this ContainerFactory containerFactory, string directory,
			bool withExecutables)
		{
			var assemblies = Directory.GetFiles(directory, "*.dll")
				.Union(withExecutables ? Directory.GetFiles(directory, "*.exe") : Enumerable.Empty<string>())
				.Select(delegate(string s)
				{
					try
					{
						return AssemblyName.GetAssemblyName(s);
					}
					catch (BadImageFormatException)
					{
						return null;
					}
				})
				.NotNull()
				.AsParallel()
				.Select(AssemblyHelpers.LoadAssembly);
			return containerFactory.WithTypesFromAssemblies(assemblies);
		}

		private static string GetBinDirectory()
		{
			var relativePath = AppDomain.CurrentDomain.RelativeSearchPath;
			var basePath = AppDomain.CurrentDomain.BaseDirectory;
			return string.IsNullOrEmpty(relativePath) || !relativePath.IsSubdirectoryOf(basePath)
				? basePath
				: relativePath;
		}

		public static ContainerFactory WithConfigFile(this ContainerFactory containerFactory, string fileName)
		{
			if (!string.IsNullOrEmpty(fileName) && File.Exists(fileName))
				return containerFactory.WithFileConfiguration(typesList => FileConfigurationParser.Parse(typesList, fileName));
			return containerFactory;
		}
	}
}