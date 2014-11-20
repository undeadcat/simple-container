using System;
using NUnit.Framework;
using SimpleContainer.Configuration;
using SimpleContainer.Implementation;

namespace SimpleContainer.Tests
{
	public abstract class ContainerConfigurationTest : SimpleContainerTestBase
	{
		public class SimpleConfigurators : ContainerConfigurationTest
		{
			public interface IInterface
			{
			}

			public class Impl1 : IInterface
			{
			}

			public class Impl2 : IInterface
			{
			}

			public class InterfaceConfigurator : IServiceConfigurator<IInterface>
			{
				public void Configure(ServiceConfigurationBuilder<IInterface> builder)
				{
					builder.Bind<Impl2>();
				}
			}

			[Test]
			public void Test()
			{
				var container = Container();
				Assert.That(container.Get<IInterface>(), Is.InstanceOf<Impl2>());
			}
		}

		public class ConfiguratorsWithSettings : ContainerConfigurationTest
		{
			public class Service
			{
				public readonly string parameter;

				public Service(string parameter)
				{
					this.parameter = parameter;
				}
			}

			public class MySubsystemSettings
			{
				public string MyParameter { get; set; }
			}

			public class ServiceConfigurator : IServiceConfigurator<MySubsystemSettings, Service>
			{
				public void Configure(MySubsystemSettings settings, ServiceConfigurationBuilder<Service> builder)
				{
					builder.Dependencies(new
					{
						parameter = settings.MyParameter
					});
				}
			}

			[Test]
			public void Test()
			{
				Func<Type, object> loadSettings = t => new MySubsystemSettings {MyParameter = "abc"};
				using (var staticContainer = CreateStaticContainer(x => x.SettingsLoader = loadSettings))
				using (var localContainer = LocalContainer(staticContainer, null))
				{
					var instance = localContainer.Get<Service>();
					Assert.That(instance.parameter, Is.EqualTo("abc"));
				}
			}
		}

		public class SettingsLoaderErrors : ContainerConfigurationTest
		{
			public class Service
			{
				public readonly string parameter;

				public Service(string parameter)
				{
					this.parameter = parameter;
				}
			}

			public class MySubsystemSettings
			{
				public string MyParameter { get; set; }
			}
			
			public class OtherSubsystemSettings
			{
			}

			public class ServiceConfigurator : IServiceConfigurator<MySubsystemSettings, Service>
			{
				public void Configure(MySubsystemSettings settings, ServiceConfigurationBuilder<Service> builder)
				{
					builder.Dependencies(new
					{
						parameter = settings.MyParameter
					});
				}
			}

			[Test]
			public void SettingsLoaderIsNotConfigured()
			{
				using (var staticContainer = CreateStaticContainer())
				{
					var error = Assert.Throws<SimpleContainerException>(() => LocalContainer(staticContainer, null));
					const string expectedMessage =
						"configurator [ServiceConfigurator] requires settings, but settings loader is not configured;" +
						"assign delegate to property ContainerFactory.SettingsLoader";
					Assert.That(error.Message, Is.EqualTo(expectedMessage));
				}
			}

			[Test]
			public void SettingsLoaderRetursNull()
			{
				Func<Type, object> loadSettings = t => null;
				using (var staticContainer = CreateStaticContainer(x => x.SettingsLoader = loadSettings))
				{
					var error = Assert.Throws<SimpleContainerException>(() => LocalContainer(staticContainer, null));
					const string expectedMessage = "configurator [ServiceConfigurator] requires settings, " +
					                               "but settings loader returned null";
					Assert.That(error.Message, Is.EqualTo(expectedMessage));
				}
			}

			[Test]
			public void SettingsLoaderReturnsObjectOfInvalidType()
			{
				Func<Type, object> loadSettings = t => new OtherSubsystemSettings();
				using (var staticContainer = CreateStaticContainer(x => x.SettingsLoader = loadSettings))
				{
					var error = Assert.Throws<SimpleContainerException>(() => LocalContainer(staticContainer, null));
					const string expectedMessage = "configurator [ServiceConfigurator] requires settings [MySubsystemSettings], " +
												   "but settings loader returned [OtherSubsystemSettings]";
					Assert.That(error.Message, Is.EqualTo(expectedMessage));
				}
			}
		}

		public class ServiceCanHaveManyConfigurators : ContainerConfigurationTest
		{
			public class Service
			{
				public readonly int argument1;
				public readonly int argument2;

				public Service(int argument1, int argument2)
				{
					this.argument1 = argument1;
					this.argument2 = argument2;
				}
			}

			public class ServiceConfigurator1 : IServiceConfigurator<Service>
			{
				public void Configure(ServiceConfigurationBuilder<Service> builder)
				{
					builder.Dependencies(new
					{
						argument1 = 1
					});
				}
			}

			public class ServiceConfigurator2 : IServiceConfigurator<Service>
			{
				public void Configure(ServiceConfigurationBuilder<Service> builder)
				{
					builder.Dependencies(new
					{
						argument2 = 2
					});
				}
			}

			[Test]
			public void Test()
			{
				var container = Container();
				var instance = container.Get<Service>();
				Assert.That(instance.argument1, Is.EqualTo(1));
				Assert.That(instance.argument2, Is.EqualTo(2));
			}
		}

		public class CanBindDependenciesViaAnonymousType : ContainerConfigurationTest
		{
			public class TestService
			{
				public readonly string stringVal;
				public readonly int intVal;

				public TestService(string stringVal, int intVal)
				{
					this.stringVal = stringVal;
					this.intVal = intVal;
				}
			}

			public class InterfaceConfigurator : IServiceConfigurator<TestService>
			{
				public void Configure(ServiceConfigurationBuilder<TestService> builder)
				{
					builder.Dependencies(new
					{
						stringVal = "testString",
						intVal = 42
					});
				}
			}

			[Test]
			public void Test()
			{
				var container = Container();
				var instance = container.Get<TestService>();
				Assert.That(instance.stringVal, Is.EqualTo("testString"));
				Assert.That(instance.intVal, Is.EqualTo(42));
			}
		}
	}
}