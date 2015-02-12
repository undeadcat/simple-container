﻿using System;
using System.Collections.Generic;
using SimpleContainer.Helpers;

namespace SimpleContainer.Interface
{
	public class ServiceName
	{
		public Type Type { get; private set; }
		public List<string> Contracts { get; private set; }

		internal ServiceName(Type type, List<string> contracts)
		{
			Type = type;
			Contracts = contracts;
		}

		public string FormatName()
		{
			return FormatTypeName() + FormatContracts();
		}

		public string FormatTypeName()
		{
			return Type.FormatName();
		}

		public string FormatContracts()
		{
			return Contracts.IsEmpty() ? "" : "[" + InternalHelpers.FormatContractsKey(Contracts) + "]";
		}
	}
}