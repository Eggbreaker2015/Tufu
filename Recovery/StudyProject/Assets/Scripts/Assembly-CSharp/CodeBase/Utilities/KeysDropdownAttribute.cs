using System;

namespace Codebase.Utilities
{
	[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
	public class KeysDropdownAttribute : Attribute
	{
		public Type KeysType { get; }

		public string Prefix { get; set; }

		public KeysDropdownAttribute(Type keysType)
		{
		}
	}
}
