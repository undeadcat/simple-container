using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using NUnit.Framework;
using SimpleContainer.Configuration;
using SimpleContainer.Infection;

namespace SimpleContainer.Tests
{
	public abstract class DisposeTest : SimpleContainerTestBase
	{
		public class DisposeInReverseTopSortOrder : DisposeTest
		{
			public class Disposable1 : IDisposable
			{
				public readonly Service1 service1;
				public readonly Disposable3 disposable3;

				public Disposable1(Service1 service1, Disposable3 disposable3)
				{
					this.service1 = service1;
					this.disposable3 = disposable3;
				}

				public void Dispose()
				{
					LogBuilder.Append("Disposable1.Dispose ");
				}
			}

			public class Service1
			{
				public readonly Disposable2 disposable2;

				public Service1(Disposable2 disposable2)
				{
					this.disposable2 = disposable2;
				}
			}

			public class Disposable2 : IDisposable
			{
				public readonly Disposable3 disposable3;

				public Disposable2(Disposable3 disposable3)
				{
					this.disposable3 = disposable3;
				}

				public void Dispose()
				{
					LogBuilder.Append("Disposable2.Dispose ");
				}
			}

			public class Disposable3 : IDisposable
			{
				public void Dispose()
				{
					LogBuilder.Append("Disposable3.Dispose ");
				}
			}

			[Test]
			public void Test()
			{
				var container = Container();
				Assert.That(LogBuilder.ToString(), Is.EqualTo(""));
				container.Get<Disposable1>();
				container.Dispose();
				Assert.That(LogBuilder.ToString(), Is.EqualTo("Disposable1.Dispose Disposable2.Dispose Disposable3.Dispose "));
			}
		}

		public class DisposeEachServiceOnlyOnce : DisposeTest
		{
			public interface IMyInterface : IDisposable
			{
			}

			public class MyImpl : IMyInterface
			{
				public void Dispose()
				{
					LogBuilder.Append("MyImpl.Dispose ");
				}
			}

			[Test]
			public void Test()
			{
				var container = Container();
				Assert.That(container.Get<IMyInterface>(), Is.SameAs(container.Get<MyImpl>()));
				container.Dispose();
				Assert.That(LogBuilder.ToString(), Is.EqualTo("MyImpl.Dispose "));
			}
		}

		public class SeparateDisposableImplementation : DisposeTest
		{
			[Static]
			public class Impl : IInterface, IDisposable
			{
				public void Dispose()
				{
					LogBuilder.Append("Impl.Dispose ");
				}
			}

			[Static]
			public interface IInterface
			{
			}

			[Test]
			public void Test()
			{
				using (var staticContainer = CreateStaticContainer())
				{
					staticContainer.Get<IInterface>();
					Assert.That(LogBuilder.ToString(), Is.EqualTo(""));
				}
				Assert.That(LogBuilder.ToString(), Is.EqualTo("Impl.Dispose "));
			}
		}

		public class CannotCallToDisposedContainer : DisposeTest
		{
			public class A
			{
			}

			[Test]
			public void Test()
			{
				var container = Container();
				container.Dispose();
				Assert.Throws<ObjectDisposedException>(() => container.Get<A>());
			}
		}

		public class DoNotDisposeServicesTwice : DisposeTest
		{
			public class A : IDisposable
			{
				public StringBuilder Logger { get; set; }

				public void Dispose()
				{
					Logger.Append("dispose ");
				}
			}

			[Test]
			public void Test()
			{
				var container = Container();
				var logger = new StringBuilder();
				var a = container.Get<A>();
				a.Logger = logger;
				container.Dispose();
				container.Dispose();
				Assert.That(logger.ToString(), Is.EqualTo("dispose "));
			}
		}

		public class DisposeUnreferencedObjects : DisposeTest
		{
			private static readonly StringBuilder logBuilder = new StringBuilder();

			public class Wrap
			{
				public readonly IEnumerable<A> enumerable;

				public Wrap(IEnumerable<A> enumerable)
				{
					this.enumerable = enumerable;
				}
			}

			public class A
			{
				public readonly B b;
				public readonly C c;

				public A(B b, C c)
				{
					this.b = b;
					this.c = c;
				}
			}

			public class B : IDisposable
			{
				public void Dispose()
				{
					logBuilder.Append("B.Dispose ");
				}
			}

			public class C
			{
			}

			[Test]
			public void Test()
			{
				var container = Container(b => b.DontUse<C>());
				var wrap = container.Get<Wrap>();
				Assert.That(wrap.enumerable, Is.Empty);
				container.Dispose();
				Assert.That(logBuilder.ToString(), Is.EqualTo("B.Dispose "));
			}
		}

		public class DisposeAllServicesEvenIfSomeOfThemCrashed : DisposeTest
		{
			public class Component1 : IDisposable
			{
				public readonly Component2 component2;

				public Component1(Component2 component2)
				{
					this.component2 = component2;
				}

				public void Dispose()
				{
					LogBuilder.AppendLine("Component1.OnStop ");
					throw new InvalidOperationException("test component1 crash");
				}
			}

			public class Component2 : IDisposable
			{
				public void Dispose()
				{
					LogBuilder.AppendLine("Component2.OnStop ");
					throw new InvalidOperationException("test component2 crash");
				}
			}

			[Test]
			public void Test()
			{
				using (var staticContainer = CreateStaticContainer())
				{
					var container = staticContainer.CreateLocalContainer(null, Assembly.GetExecutingAssembly(), null);
					container.Get<Component1>();
					var error = Assert.Throws<AggregateException>(container.Dispose);
					Assert.That(error.Message, Is.EqualTo("error disposing services"));
					Assert.That(error.InnerExceptions[0].Message, Is.EqualTo("error disposing [Component1]"));
					Assert.That(error.InnerExceptions[0].InnerException.Message, Is.EqualTo("test component1 crash"));
					Assert.That(error.InnerExceptions[1].Message, Is.EqualTo("error disposing [Component2]"));
					Assert.That(error.InnerExceptions[1].InnerException.Message, Is.EqualTo("test component2 crash"));
				}
			}
		}
	}
}