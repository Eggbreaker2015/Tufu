using System;

namespace Codebase.Core
{
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Field)]
	public sealed class StaticResetIgnoreAttribute : Attribute
	{
	}
}
