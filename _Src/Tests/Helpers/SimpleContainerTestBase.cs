using System;
using System.Collections.Generic;
using System.Text;
using SimpleContainer.Configuration;
using BindingFlags = System.Reflection.BindingFlags;

namespace SimpleContainer.Tests.Helpers
{
	public abstract class SimpleContainerTestBase : UnitTestBase
	{
		protected List<IDisposable> disposables;

		protected static string ContainerAsembly = typeof(IContainer).Assembly.GetName().Name;
		protected static string TestsAsembly = typeof(SimpleContainerTestBase).Assembly.GetName().Name;
		protected static string defaultScannedAssemblies = string.Format("\r\nscanned assemblies\r\n\t{0}\r\n\t{1}", ContainerAsembly, TestsAsembly);

		protected override void SetUp()
		{
			base.SetUp();
			disposables = new List<IDisposable>();
			LogBuilder = new StringBuilder();
		}

		public static StringBuilder LogBuilder { get; private set; }

		protected override void TearDown()
		{
			if (disposables != null)
				foreach (var disposable in disposables)
					disposable.Dispose();
			base.TearDown();
		}

		protected ContainerFactory Factory()
		{
			var targetTypes = GetType().GetNestedTypesRecursive(BindingFlags.NonPublic | BindingFlags.Public);
			return new ContainerFactory()
				.WithAssembliesFilter(x => x.Name == ContainerAsembly || x.Name == TestsAsembly)
				.WithTypes(targetTypes);
		}

		protected IContainer Container(Action<ContainerConfigurationBuilder> configure = null)
		{
			var result = Factory()
				.WithConfigurator(configure)
				.Build();
			disposables.Add(result);
			return result;
		}

		protected static string FormatExpectedMessage(string s)
		{
			const string crlf = "\r\n";
			return s.Substring(crlf.Length);
		}
	}
}