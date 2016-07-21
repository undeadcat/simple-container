﻿#if FULLFRAMEWORK
using System;
using System.Globalization;
using System.IO;
using NUnit.Framework;
using SimpleContainer.Configuration;
using SimpleContainer.Infection;
using SimpleContainer.Interface;
using SimpleContainer.Tests.Helpers;

namespace SimpleContainer.Tests
{
	[TestFixture]
	internal abstract class FileConfigurationTest : SimpleContainerTestBase
	{
		public class Simple : FileConfigurationTest
		{
			public interface IA
			{
			}

			public class A1 : IA
			{
			}

			public class A2 : IA
			{
			}

			[Test]
			public void Test()
			{
				var container = Container("IA -> A2");
				Assert.That(container.Get<IA>(), Is.SameAs(container.Get<A2>()));
			}
		}
		
		public class CanOverrideLazyConfigurators : FileConfigurationTest
		{
			public interface IA
			{
			}

			public class A1 : IA
			{
			}

			public class A2 : IA
			{
			}

			public class AConfigurator : IServiceConfigurator<IA>
			{
				public void Configure(ConfigurationContext context, ServiceConfigurationBuilder<IA> builder)
				{
					builder.Bind<A1>();
				}
			}

			[Test]
			public void Test()
			{
				var container = Container("IA -> A2");
				Assert.That(container.Get<IA>(), Is.SameAs(container.Get<A2>()));
			}
		}

		public class StringDependency : FileConfigurationTest
		{
			public class A
			{
				public readonly string parameter;

				public A(string parameter)
				{
					this.parameter = parameter;
				}
			}

			[Test]
			public void Test()
			{
				var container = Container("A.parameter -> qq");
				Assert.That(container.Get<A>().parameter, Is.EqualTo("qq"));
			}
		}

		public class Contract : FileConfigurationTest
		{
			public class A
			{
				public readonly B bx;
				public readonly B by;
				public readonly B b;

				public A([TestContract("x")] B bx, [TestContract("y")] B by, B b)
				{
					this.bx = bx;
					this.by = by;
					this.b = b;
				}
			}

			public class B
			{
				public readonly int parameter;

				public B(int parameter)
				{
					this.parameter = parameter;
				}
			}

			[Test]
			public void Test()
			{
				var container = Container(@"
					[x]
					B.parameter -> 12
					[y]
					B.parameter -> 21
					[]
					B.parameter -> 111");
				var a = container.Get<A>();
				Assert.That(a.bx.parameter, Is.EqualTo(12));
				Assert.That(a.by.parameter, Is.EqualTo(21));
				Assert.That(a.b.parameter, Is.EqualTo(111));
			}
		}

		public class UnixStyleNewLinesAreOk : FileConfigurationTest
		{
			public class A
			{
				public readonly int parameter;

				public A(int parameter)
				{
					this.parameter = parameter;
				}
			}

			[Test]
			public void Test()
			{
				var container = Container("[x]\nA.parameter -> 12");
				var a = container.Get<A>("x");
				Assert.That(a.parameter, Is.EqualTo(12));
			}
		}

		public class IntDependency : FileConfigurationTest
		{
			public class A
			{
				public readonly int parameter;

				public A(int parameter)
				{
					this.parameter = parameter;
				}
			}

			[Test]
			public void Test()
			{
				var container = Container("A.parameter -> 42");
				Assert.That(container.Get<A>().parameter, Is.EqualTo(42));
			}
		}

		public class NullableLongDependency : FileConfigurationTest
		{
			public class A
			{
				public readonly long? parameter;

				public A(long? parameter)
				{
					this.parameter = parameter;
				}
			}

			[Test]
			public void Test()
			{
				var container = Container("A.parameter -> 42");
				Assert.That(container.Get<A>().parameter, Is.EqualTo(42));
			}
		}

		public class DontUse : FileConfigurationTest
		{
			public interface IInterface
			{
			}

			public class A : IInterface
			{
			}

			public class B : IInterface
			{
			}

			[Test]
			public void Test()
			{
				var container = Container("B ->");
				Assert.That(container.Get<IInterface>(), Is.SameAs(container.Get<A>()));
			}
		}

		public class NoTypesFound : FileConfigurationTest
		{
			[Test]
			public void Test()
			{
				var e = Assert.Throws<SimpleContainerException>(() => Container("B -> 1"));
				Assert.That(e.Message, Is.EqualTo("no types found for name [B]"));
			}
		}

		public class ParseFailure : FileConfigurationTest
		{
			public class A
			{
				public readonly int value;

				public A(int value)
				{
					this.value = value;
				}
			}

			[Test]
			public void Test()
			{
				var e = Assert.Throws<SimpleContainerException>(() => Container("A.value -> qq"));
				Assert.That(e.Message, Is.EqualTo("can't parse [A.value] from [qq] as [int]"));
			}
		}
		
		public class MoreThanOneConstructor : FileConfigurationTest
		{
			public class A
			{
				public readonly string value;

				public A(string value)
				{
					this.value = value;
				}
				
				public A(int value)
				{
					this.value = value.ToString(CultureInfo.InvariantCulture);
				}
			}

			[Test]
			public void Test()
			{
				var e = Assert.Throws<SimpleContainerException>(() => Container("A.value -> qq"));
				Assert.That(e.Message, Is.EqualTo("type [A] has many public ctors"));
			}
		}
		
		public class MoreThanOneConstructorWithContainerConstructorAttribute : FileConfigurationTest
		{
			public class A
			{
				public readonly string value;

				[ContainerConstructor]
				public A(string value)
				{
					this.value = value;
				}

				[ContainerConstructor]
				public A(int value)
				{
					this.value = value.ToString(CultureInfo.InvariantCulture);
				}
			}

			[Test]
			public void Test()
			{
				var e = Assert.Throws<SimpleContainerException>(() => Container("A.value -> qq"));
				Assert.That(e.Message, Is.EqualTo("type [A] has many ctors with [ContainerConstructor] attribute"));
			}
		}

		public class DependencyNotFound : FileConfigurationTest
		{
			public class A
			{
			}

			[Test]
			public void Test()
			{
				var e = Assert.Throws<SimpleContainerException>(() => Container("A.value -> qq"));
				Assert.That(e.Message, Is.EqualTo("type [A] has no dependency [value]"));
			}
		}

		public class MoreThanOneTypeByName : FileConfigurationTest
		{
			private const string testCode = @"
					namespace A1
					{
						public class A
						{
							public A(int parameter)
							{
							}
						}
					}

					namespace A2
					{
						public class A
						{
							public A(int parameter)
							{
							}
						}
					}
				";

			[Test]
			public void Test()
			{
				var assembly = AssemblyCompiler.Compile(testCode);
				File.WriteAllText(configFileName, "A.parameter->11");
				var factory = new ContainerFactory()
					.WithAssembliesFilter(x => x.Name.StartsWith("tmp_"))
					.WithConfigFile(configFileName)
					.WithTypesFromAssemblies(new[] { assembly });
				var e = Assert.Throws<SimpleContainerException>(() => factory.Build());
				Assert.That(e.Message, Is.EqualTo("for name [A] more than one type found [A1.A], [A2.A]"));
			}
		}

		protected string configFileName;

		protected override void SetUp()
		{
			configFileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "test.config");
			if (File.Exists(configFileName))
				File.Delete(configFileName);
			base.SetUp();
		}

		protected override void TearDown()
		{
			if (File.Exists(configFileName))
				File.Delete(configFileName);
			base.TearDown();
		}

		protected IContainer Container(string configText)
		{
			File.WriteAllText(configFileName, configText);
			var result = Factory().WithConfigFile(configFileName).Build();
			disposables.Add(result);
			return result;
		}
	}
}
#endif