using System;
using System.Reflection.Emit;

namespace SimpleContainer.Helpers.ReflectionEmit
{
	internal abstract class Caster
	{
		protected readonly Type memberType;
		protected readonly Type outputType;

		protected Caster(Type outputType, Type memberType)
		{
			this.outputType = outputType;
			this.memberType = memberType;
		}

		protected abstract void EmitNullableCast(ILGenerator ilGenerator, Type nullableType);

		protected abstract void EmitValueTypeCast(ILGenerator ilGenerator);

		public void EmitCast(ILGenerator ilGenerator)
		{
			if (outputType == memberType)
				return;

			if (!outputType.IsAssignableFrom(memberType))
				throw new InvalidOperationException(string.Format("types [{0}] and [{1}] are not compatibe", outputType, memberType));

			if (memberType.IsValueType && !outputType.IsValueType)
				EmitValueTypeCast(ilGenerator);

			if (outputType.IsNullableOf(memberType))
				EmitNullableCast(ilGenerator, outputType);
		}
	}
}