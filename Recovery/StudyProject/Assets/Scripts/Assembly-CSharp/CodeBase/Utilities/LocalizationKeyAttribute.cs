using System;

namespace Codebase.Utilities
{
	[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
	public class LocalizationKeyAttribute : Attribute
	{
		public string Table { get; set; }

		public string Prefix { get; set; }
	}
}
