#if !FULLFRAMEWORK
using System;

// ReSharper disable once CheckNamespace
namespace SimpleContainer.Helpers
{
	[Flags]
	internal enum BindingFlags
	{
		Default = 0,
		DeclaredOnly = 2,
		Instance = 4,
		Static = 8,
		Public = 16,
		NonPublic = 32,
		FlattenHierarchy = 64
	}
}
#endif