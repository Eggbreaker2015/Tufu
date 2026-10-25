using System;
using Codebase.Player.Inventory;

namespace Codebase.Core
{
	[Serializable]
	public class ItemWrapper
	{
		public string Key;

		public int Level;

		public ItemWrapper(ItemModel item)
		{
		}

		public ItemWrapper()
		{
		}
	}
}
