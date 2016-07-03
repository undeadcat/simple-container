using System;
using System.Runtime.Serialization;

namespace SimpleContainer.Interface
{
#if FULLFRAMEWORK
	[Serializable]
#endif
	public class SimpleContainerException : Exception
	{
		public SimpleContainerException(string message)
			: base(message)
		{
		}

		public SimpleContainerException(string message, Exception innerException) : base(message, innerException)
		{
		}

#if FULLFRAMEWORK
	protected SimpleContainerException(SerializationInfo info, StreamingContext context) : base(info, context)
		{
		}
#endif
	}
}