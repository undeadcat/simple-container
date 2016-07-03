using System;
using NUnit.Framework;
using SimpleContainer.Interface;
using SimpleContainer.Tests.Generics;
using SimpleContainer.Tests.Helpers;

namespace SimpleContainer.Tests
{
	public abstract class ConfigurationBuilderValidationsTest : SimpleContainerTestBase
	{
		public class BindToInvalidImplementation : ConfigurationBuilderValidationsTest
		{
			[Test]
			public void Test()
			{
				var error = Assert.Throws<SimpleContainerException>(() => Container(x => x.Bind(typeof (A), typeof (B))));
				Assert.That(error.Message, Is.StringContaining("[A] is not assignable from [B]"));
			}

			public class A
			{
			}

			public class B
			{
			}

			public class Wrap
			{
				public Wrap(A a)
				{
				}
			}
		}

		public class CannotBindValueForOpenGeneric : GenericsInferringViaArgumentTypesTest
		{
			public class A<T>
			{
			}

			[Test]
			public void Test()
			{
				Exception exception = null;
				Container(b => exception = Assert.Throws<SimpleContainerException>(() => b.Bind(typeof (A<>), new A<int>())));
				Assert.That(exception, Is.Not.Null);
				Assert.That(exception.Message, Is.EqualTo("can't bind value for generic definition [A<T>]"));
			}
		}
		
		public class CannotBindFactoryForOpenGeneric : GenericsInferringViaArgumentTypesTest
		{
			public class A<T>
			{
			}

			[Test]
			public void Test()
			{
				Exception exception = null;
				Container(b => exception = Assert.Throws<SimpleContainerException>(() => b.Bind(typeof (A<>), new A<int>())));
				Assert.That(exception, Is.Not.Null);
				Assert.That(exception.Message, Is.EqualTo("can't bind value for generic definition [A<T>]"));
			}
		}
	}
}