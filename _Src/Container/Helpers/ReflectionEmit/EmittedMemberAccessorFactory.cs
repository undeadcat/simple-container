#if FULLFRAMEWORK
using System;
using System.Reflection;
using System.Reflection.Emit;

namespace SimpleContainer.Helpers.ReflectionEmit
{
	internal class EmittedMemberAccessorFactory<TOutput> : IMemberAccessorFactory<TOutput>
	{
		public Action<object, TOutput> CreateSetter(MemberInfo memberInfo)
		{
			var dynamicMethod = new DynamicMethod("set_" + memberInfo.Name,
				null,
				new[] {typeof (object), typeof (TOutput)},
				typeof (EmittedMemberAccessorFactory<TOutput>),
				true);
			var ilGenerator = dynamicMethod.GetILGenerator();
			var propertyInfo = memberInfo as PropertyInfo;
			if (propertyInfo != null)
				if (!TryEmitSet(propertyInfo, ilGenerator))
					return null;
			var fieldInfo = memberInfo as FieldInfo;
			if (fieldInfo != null)
				if (!TryEmitSet(fieldInfo, ilGenerator))
					return null;
			return (Action<object, TOutput>) dynamicMethod.CreateDelegate(typeof (Action<object, TOutput>));
		}

		public Func<object, TOutput> CreateGetter(MemberInfo memberInfo)
		{
			var method = new DynamicMethod("get_" + memberInfo.Name,
				typeof (TOutput),
				new[] {typeof (object)},
				typeof (EmittedMemberAccessorFactory<TOutput>),
				true);
			var ilGenerator = method.GetILGenerator();
			var propertyInfo = memberInfo as PropertyInfo;
			if (propertyInfo != null)
				if (!TryEmitGet(propertyInfo, ilGenerator))
					return null;
			var fieldInfo = memberInfo as FieldInfo;
			if (fieldInfo != null)
				if (!TryEmitGet(fieldInfo, ilGenerator))
					return null;
			return (Func<object, TOutput>) method.CreateDelegate(typeof (Func<object, TOutput>));
		}

		private static void EmitLoadTarget(ILGenerator ilGenerator, MemberInfo member)
		{
			if (member.IsStatic())
				return;
			ilGenerator.Emit(OpCodes.Ldarg_0);
			var declaringType = member.DeclaringType;
			if (!declaringType.IsValueType)
				return;
			ilGenerator.Emit(OpCodes.Unbox_Any, declaringType);
			ilGenerator.DeclareLocal(declaringType);
			ilGenerator.Emit(OpCodes.Stloc_0);
			ilGenerator.Emit(OpCodes.Ldloca_S, 0);
		}

		private static bool TryEmitSet(FieldInfo fieldInfo, ILGenerator ilGenerator)
		{
			if (!fieldInfo.IsStatic)
				ilGenerator.Emit(OpCodes.Ldarg_0);
			ilGenerator.Emit(OpCodes.Ldarg_1);
			EmitUnboxingCast(fieldInfo.FieldType, ilGenerator);
			ilGenerator.Emit(fieldInfo.IsStatic ? OpCodes.Stsfld : OpCodes.Stfld, fieldInfo);
			ilGenerator.Emit(OpCodes.Ret);
			return true;
		}

		private static bool TryEmitGet(FieldInfo fieldInfo, ILGenerator ilGenerator)
		{
			EmitLoadTarget(ilGenerator, fieldInfo);
			ilGenerator.Emit(fieldInfo.IsStatic ? OpCodes.Ldsfld : OpCodes.Ldfld, fieldInfo);
			EmitBoxingCast(fieldInfo.FieldType, ilGenerator);
			ilGenerator.Emit(OpCodes.Ret);
			return true;
		}

		private static bool TryEmitSet(PropertyInfo propertyInfo, ILGenerator ilGenerator)
		{
			var setter = propertyInfo.GetSetMethod(true);
			if (setter == null)
				return false;
			if (!propertyInfo.IsStatic())
				ilGenerator.Emit(OpCodes.Ldarg_0);
			ilGenerator.Emit(OpCodes.Ldarg_1);
			EmitUnboxingCast(propertyInfo.PropertyType, ilGenerator);
			ilGenerator.Emit(OpCodes.Call, setter);
			ilGenerator.Emit(OpCodes.Ret);
			return true;
		}

		private static bool TryEmitGet(PropertyInfo propertyInfo, ILGenerator ilGenerator)
		{
			var getter = propertyInfo.GetGetMethod(true);
			if (getter == null)
				return false;
			EmitLoadTarget(ilGenerator, propertyInfo);
			ilGenerator.Emit(OpCodes.Call, getter);
			EmitBoxingCast(propertyInfo.PropertyType, ilGenerator);
			ilGenerator.Emit(OpCodes.Ret);

			return true;
		}

		private static void EmitBoxingCast(Type memberType, ILGenerator ilGenerator)
		{
			new BoxingCaster(typeof (TOutput), memberType).EmitCast(ilGenerator);
		}

		private static void EmitUnboxingCast(Type memberType, ILGenerator ilGenerator)
		{
			new UnboxingCaster(typeof (TOutput), memberType).EmitCast(ilGenerator);
		}
	}
}
#endif