using System;
using System.Runtime.Serialization;

namespace SimpleContainer.Interface
{
	public class SimpleContainerException : Exception
	{
		public SimpleContainerException(string message)
			: base(message)
		{
		}

		public SimpleContainerException(string message, Exception innerException) : base(message, innerException)
		{
		}

	}
}