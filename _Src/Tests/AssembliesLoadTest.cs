using System;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using SimpleContainer.Helpers;
using SimpleContainer.Interface;
using SimpleContainer.Tests.Helpers;

namespace SimpleContainer.Tests
{
	public abstract class AssembliesLoadTest : SimpleContainerTestBase
	{
		protected override void SetUp()
		{
			base.SetUp();
			if (Directory.Exists(testDirectory))
				Directory.Delete(testDirectory, true);
			Directory.CreateDirectory(testDirectory);

			appDomain = AppDomain.CreateDomain("test", null, new AppDomainSetup {ApplicationBase = testDirectory});
		}

		protected override void TearDown()
		{
			if (appDomain != null)
				AppDomain.Unload(appDomain);
			if (Directory.Exists(testDirectory))
				Directory.Delete(testDirectory, true);
			base.TearDown();
		}

		protected AppDomain appDomain;
		private static readonly string testDirectory = Path.GetFullPath("testDirectory");

		private static void CopyAssemblyToTestDirectory(Assembly assembly)
		{
			File.Copy(assembly.Location, Path.Combine(testDirectory, Path.GetFileName(assembly.Location)));
		}

		private FactoryInvoker GetInvoker()
		{
			return (FactoryInvoker) appDomain.CreateInstanceAndUnwrap(Assembly.GetExecutingAssembly().GetName().FullName,
				typeof (FactoryInvoker).FullName);
		}

		private static ContainerFactory FactoryForTestDir()
		{
			return new ContainerFactory()
				.WithAssembliesFilter(x => x.Name.StartsWith("tmp_"))
				.WithTypesFromAssemblies(
					Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory, "*.dll").Select(Assembly.LoadFile));
		}

		private class FactoryInvoker : MarshalByRefObject
		{
			public string CreateContainerWithCrash()
			{
				try
				{
					FactoryForTestDir().Build();
					return "can't reach here";
				}
				catch (Exception e)
				{
					return e.ToString();
				}
			}

			public void DoCallBack<T>(T parameter, Action<T> action)
			{
				action(parameter);
			}
		}

		public class DisplayScannedAssembliesInException : AssembliesLoadTest
		{
			private const string primaryAssemblyCode = @"
					using System;
					using A1;

					namespace A1
					{
						public interface ISomeInterface
						{
						}
					}
				";


			[Test]
			public void Test()
			{
				var primaryAssembly = AssemblyCompiler.Compile(primaryAssemblyCode);
				var assemblyName = primaryAssembly.GetName().Name;

				CopyAssemblyToTestDirectory(primaryAssembly);
				CopyAssemblyToTestDirectory(typeof (IContainer).Assembly);
				CopyAssemblyToTestDirectory(Assembly.GetExecutingAssembly());
				CopyAssemblyToTestDirectory(typeof (Assert).Assembly);

				GetInvoker().DoCallBack(assemblyName, delegate(string s)
				{
					var f = FactoryForTestDir();
					var type = Type.GetType("A1.ISomeInterface, " + s);
					Assert.That(type, Is.Not.Null);
					using (var c = f.Build())
					{
						var exception = Assert.Throws<SimpleContainerException>(() => c.Get(type));
						var assemblies = new[] {ContainerAsembly, s}.OrderBy(x => x).Select(x => "\t" + x).JoinStrings("\r\n");
						const string expectedMessage = "no instances for [ISomeInterface]\r\n\r\n!" +
						                               "ISomeInterface - has no implementations\r\n" +
						                               "scanned assemblies\r\n";
						Assert.That(exception.Message, Is.EqualTo(expectedMessage + assemblies));
					}
				});
			}

		
		}

		public class CorrectExceptionHandling : AssembliesLoadTest
		{
			private const string referencedAssemblyCodeV1 = @"
					using SimpleContainer.Configuration;
					using SimpleContainer;
					using System;

					namespace A1
					{
						public interface ISomeInterface
						{
						}
					}
				";

			private const string referencedAssemblyCodeV2 = @"
					using SimpleContainer.Configuration;
					using SimpleContainer;
					using System;

					namespace A1
					{
						public interface ISomeInterface
						{
							void Do();
						}
					}
				";

			private const string primaryAssemblyCode = @"
					using System;
					using A1;

					namespace A2
					{
						public class TestClass: ISomeInterface
						{
							void ISomeInterface.Do()
							{
							}
						}
					}
				";


			[Test]
			public void Test()
			{
				var referencedAssemblyV2 = AssemblyCompiler.Compile(referencedAssemblyCodeV2);
				AssemblyCompiler.Compile(referencedAssemblyCodeV1,
					Path.Combine(testDirectory, Path.GetFileName(referencedAssemblyV2.Location)));
				var primaryAssembly = AssemblyCompiler.Compile(primaryAssemblyCode, referencedAssemblyV2);

				CopyAssemblyToTestDirectory(primaryAssembly);
				CopyAssemblyToTestDirectory(typeof (IContainer).Assembly);
				CopyAssemblyToTestDirectory(Assembly.GetExecutingAssembly());

				var exceptionText = GetInvoker().CreateContainerWithCrash();
				Assert.That(exceptionText, Is.StringContaining("A1.ISomeInterface.Do"));

				const string englishText = "Unable to load one or more of the requested types";
				const string russianText = "�� ������� ��������� ���� ��� ����� ����������� �����";
				Assert.That(exceptionText, Is.StringContaining(englishText).Or.StringContaining(russianText));
				Assert.That(exceptionText, Is.StringContaining(primaryAssembly.GetName().Name));
			}
		}
	}
}