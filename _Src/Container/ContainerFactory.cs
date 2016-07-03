using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using SimpleContainer.Configuration;
using SimpleContainer.Helpers;
using SimpleContainer.Implementation;
using SimpleContainer.Interface;

namespace SimpleContainer
{
	public class ContainerFactory
	{
		private Type profile;
		private Func<AssemblyName, bool> assembliesFilter = n => true;
		private Func<Type, string, object> settingsLoader;
		private LogError errorLogger;
		private LogInfo infoLogger;
		private Type[] priorities;
		private Func<Type[], Action<ContainerConfigurationBuilder>> fileConfigure;
		private Action<ContainerConfigurationBuilder> configure;
		private Action<ContainerConfigurationBuilder> staticConfigure;
		private TypesContext typesContextCache;
		private Func<Type[]> types;
		private IParametersSource parameters;
		private readonly Dictionary<Type, Func<object, string>> valueFormatters = new Dictionary<Type, Func<object, string>>();

		private readonly Dictionary<Type, ConfigurationRegistry> configurationByProfileCache =
			new Dictionary<Type, ConfigurationRegistry>();

		public ContainerFactory WithSettingsLoader(Func<Type, object> newLoader)
		{
			var cache = Caches.Create<Type, object>();
			settingsLoader = (t, _) => cache.GetOrAdd(t, newLoader);
			configurationByProfileCache.Clear();
			return this;
		}

		public ContainerFactory WithSettingsLoader(Func<Type, string, object> newLoader)
		{
			var cache = Caches.Create<string, object>();
			settingsLoader = (t, k) => cache.GetOrAdd(t.Name + k, s => newLoader(t, k));
			configurationByProfileCache.Clear();
			return this;
		}

		public ContainerFactory WithParameters(IParametersSource newParameters)
		{
			parameters = newParameters;
			configurationByProfileCache.Clear();
			return this;
		}

		public ContainerFactory WithValueFormatter<T>(Func<T, string> formatter)
		{
			valueFormatters[typeof (T)] = o => formatter((T) o);
			typesContextCache = null;
			configurationByProfileCache.Clear();
			return this;
		}

		public ContainerFactory WithAssembliesFilter(Func<AssemblyName, bool> newAssembliesFilter)
		{
			assembliesFilter = n => newAssembliesFilter(n) || n.Name == typeof(IContainer).GetTypeInfo().Assembly.GetName().Name;
			typesContextCache = null;
			configurationByProfileCache.Clear();
			return this;
		}

		public ContainerFactory WithPriorities(params Type[] newConfiguratorTypes)
		{
			priorities = newConfiguratorTypes;
			configurationByProfileCache.Clear();
			return this;
		}

		internal ContainerFactory WithFileConfiguration(Func<Type[], Action<ContainerConfigurationBuilder>> fileConfig)
		{
			fileConfigure = fileConfig;
			typesContextCache = null;
			configurationByProfileCache.Clear();
			return this;
		}

		public ContainerFactory WithErrorLogger(LogError logger)
		{
			errorLogger = logger;
			typesContextCache = null;
			configurationByProfileCache.Clear();
			return this;
		}

		public ContainerFactory WithInfoLogger(LogInfo logger)
		{
			infoLogger = logger;
			typesContextCache = null;
			configurationByProfileCache.Clear();
			return this;
		}

		public ContainerFactory WithProfile(Type newProfile)
		{
			if (newProfile != null && !typeof (IProfile).IsAssignableFrom(newProfile))
				throw new SimpleContainerException(string.Format("profile type [{0}] must inherit from IProfile",
					newProfile.FormatName()));
			profile = newProfile;
			return this;
		}

		public ContainerFactory WithConfigurator(Action<ContainerConfigurationBuilder> newConfigure)
		{
			configure = newConfigure;
			return this;
		}

		public ContainerFactory WithStaticConfigurator(Action<ContainerConfigurationBuilder> newConfigure)
		{
			staticConfigure = newConfigure;
			typesContextCache = null;
			configurationByProfileCache.Clear();
			return this;
		}

		public ContainerFactory WithTypesFromAssemblies(IEnumerable<Assembly> assemblies)
		{
			var tasks = assemblies
				.Where(x => assembliesFilter(x.GetName()))
				.Select(a =>
				{
					return Task.Run(() =>
					{
						try
						{
							return a.GetTypes();
						}
						catch (ReflectionTypeLoadException e)
						{
							const string messageFormat = "can't load types from assembly [{0}], loaderExceptions:\r\n{1}";
							var loaderExceptionsText = e.LoaderExceptions.Select(ex => ex.ToString()).JoinStrings("\r\n");
							throw new SimpleContainerException(string.Format(messageFormat, a.GetName(), loaderExceptionsText), e);
						}
					});
				});
			types = () => Task.WhenAll(tasks).Result.SelectMany(x => x).ToArray();
			typesContextCache = null;
			return this;
		}

		public ContainerFactory WithTypes(Type[] newTypes)
		{
			types = () => newTypes;
			typesContextCache = null;
			configurationByProfileCache.Clear();
			return this;
		}

		public IContainer Build()
		{
			var typesContext = typesContextCache;
			if (typesContext == null)
			{
				var targetTypes = types()
					.Concat(typeof(ContainerFactory).GetTypeInfo().Assembly.GetTypes())
					.Where(x => !x.Name.StartsWith("<>", StringComparison.OrdinalIgnoreCase))
					.Distinct()
					.ToArray();
				typesContext = new TypesContext {typesList = TypesList.Create(targetTypes)};
				typesContext.genericsAutoCloser = new GenericsAutoCloser(typesContext.typesList, assembliesFilter);
				if (fileConfigure != null)
					typesContext.fileConfigurator = fileConfigure(typesContext.typesList.Types);
				ConfigurationRegistry staticConfiguration;
				if (staticConfigure == null)
					staticConfiguration = ConfigurationRegistry.Empty;
				else
				{
					var builder = new ContainerConfigurationBuilder();
					staticConfigure(builder);
					staticConfiguration = builder.RegistryBuilder.Build(typesContext.typesList, null);
				}
				var configurationContainer = CreateContainer(typesContext, staticConfiguration);
				typesContext.configuratorRunner = configurationContainer.Get<ConfiguratorRunner>();
				typesContextCache = typesContext;
			}
			ConfigurationRegistry configurationRegistry;
			if (!configurationByProfileCache.TryGetValue(profile ?? typeof (ContainerFactory), out configurationRegistry))
			{
				var builder = new ContainerConfigurationBuilder();
				var configurationContext = new ConfigurationContext(profile, settingsLoader, parameters);
				typesContext.configuratorRunner.Run(builder, configurationContext, priorities);
				if (typesContext.fileConfigurator != null)
					typesContext.fileConfigurator(builder);
				configurationRegistry = builder.RegistryBuilder.Build(typesContext.typesList, null);
				configurationByProfileCache.Add(profile ?? typeof (ContainerFactory), configurationRegistry);
			}
			return CreateContainer(typesContext, configurationRegistry.Apply(typesContext.typesList, configure));
		}

		private IContainer CreateContainer(TypesContext currentTypesContext, ConfigurationRegistry configuration)
		{
			return new Implementation.SimpleContainer(configuration, new ContainerContext
			{
				infoLogger = infoLogger,
				genericsAutoCloser = currentTypesContext.genericsAutoCloser,
				typesList = currentTypesContext.typesList,
				valueFormatters = valueFormatters
			}, errorLogger);
		}

		private class TypesContext
		{
			public TypesList typesList;
			public GenericsAutoCloser genericsAutoCloser;
			public Action<ContainerConfigurationBuilder> fileConfigurator;
			public ConfiguratorRunner configuratorRunner;
		}
	}
}