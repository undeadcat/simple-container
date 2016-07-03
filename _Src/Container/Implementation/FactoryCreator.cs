using System;
using System.Linq;
using System.Reflection;
using SimpleContainer.Annotations;
using SimpleContainer.Helpers;
using SimpleContainer.Interface;

namespace SimpleContainer.Implementation
{
	internal static class FactoryCreator
	{
		private static readonly ILookup<Type, MethodInfo> signatures = typeof (SupportedSignatures)
			.GetMethods(BindingFlags.Static | BindingFlags.Public)
			.ToLookup(x => x.ReturnType.GetGenericTypeDefinition());

		private static readonly IConcurrentCache<SignatureDelegateKey, Delegate> casters =
			Caches.Create<SignatureDelegateKey, Delegate>();

		private static readonly Func<SignatureDelegateKey, Delegate> createCaster = delegate(SignatureDelegateKey key)
		{
			var delegateType = typeof (Func<Func<Type, object, object>, object>);
			return key.signature.MakeGenericMethod(key.resultType).CreateDelegate(delegateType);
		};

		public static object TryCreate(ContainerService.Builder builder)
		{
			var funcType = builder.Type;
			if (!funcType.GetTypeInfo().IsGenericType || !funcType.IsDelegate())
				return null;
			Type resultType;
			var signature = FindSignature(funcType, out resultType);
			if (signature == null)
				return null;
			var factory = CreateFactory(builder, resultType);
			var caster = casters.GetOrAdd(new SignatureDelegateKey(resultType, signature), createCaster);
			var typedCaster = (Func<Func<Type, object, object>, object>) caster;
			return typedCaster(factory);
		}

		private static MethodInfo FindSignature(Type funcType, out Type resultType)
		{
			var funcArgumentTypes = funcType.GetGenericArguments();
			foreach (var signature in signatures[funcType.GetGenericTypeDefinition()])
				if (signature.ReturnType.GetGenericArguments().SameAs(funcArgumentTypes, funcArgumentTypes.Length - 1))
				{
					resultType = funcArgumentTypes[funcArgumentTypes.Length - 1];
					return signature;
				}
			resultType = null;
			return null;
		}

		private static Func<Type, object, object> CreateFactory(ContainerService.Builder builder, Type resultType)
		{
			var container = builder.Context.Container;
			if (builder.Context.Contracts.Count() > 0)
			{
				var oldValue = builder.Context.AnalizeDependenciesOnly;
				builder.Context.AnalizeDependenciesOnly = true;
				var containerService = builder.Context.Container.ResolveCore(new ServiceName(resultType), true,
					null, builder.Context);
				builder.Context.AnalizeDependenciesOnly = oldValue;
				builder.UnionUsedContracts(containerService);
			}
			builder.EndResolveDependencies();
			var factoryContractsArray = builder.FinalUsedContracts;
			return (type, arguments) =>
			{
				var name = ServiceName.Parse(type.UnwrapEnumerable(), factoryContractsArray);
				return container.Create(name, name.Type != type, arguments);
			};
		}

		private static class SupportedSignatures
		{
			[UsedImplicitly]
			public static Func<T> WithoutArguments<T>(Func<Type, object, object> f)
			{
				return () => (T) f(typeof (T), null);
			}

			[UsedImplicitly]
			public static Func<object, T> WithArguments<T>(Func<Type, object, object> f)
			{
				return o => (T) f(typeof (T), o);
			}

			[UsedImplicitly]
			public static Func<Type, object, T> WithTypeAndArguments<T>(Func<Type, object, object> f)
			{
				return (t, o) => (T) f(t, o);
			}

			[UsedImplicitly]
			public static Func<object, Type, T> WithArgumentsAndType<T>(Func<Type, object, object> f)
			{
				return (o, t) => (T) f(t, o);
			}

			[UsedImplicitly]
			public static Func<Type, T> WithType<T>(Func<Type, object, object> f)
			{
				return t => (T) f(t, null);
			}
		}

		private struct SignatureDelegateKey : IEquatable<SignatureDelegateKey>
		{
			public readonly Type resultType;
			public readonly MethodInfo signature;

			public SignatureDelegateKey(Type resultType, MethodInfo signature)
			{
				this.resultType = resultType;
				this.signature = signature;
			}

			public bool Equals(SignatureDelegateKey other)
			{
				return resultType == other.resultType && signature.Equals(other.signature);
			}

			public override bool Equals(object obj)
			{
				if (ReferenceEquals(null, obj)) return false;
				return obj is SignatureDelegateKey && Equals((SignatureDelegateKey) obj);
			}

			public override int GetHashCode()
			{
				unchecked
				{
					return (resultType.GetHashCode()*397) ^ signature.GetHashCode();
				}
			}
		}
	}
}