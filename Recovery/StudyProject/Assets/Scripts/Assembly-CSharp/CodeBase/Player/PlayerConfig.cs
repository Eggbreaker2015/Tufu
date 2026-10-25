using System.Collections.Generic;
using Codebase.Player.Inventory;
using Codebase.UI.World;
using Codebase.Unit;
using UnityEngine;

namespace Codebase.Player
{
	public class PlayerConfig : ScriptableObject
	{
		public List<Stat> Stats;

		public List<ItemSlotModel> Slots;

		public HealthBar HealthBarPrefab;

		public ItemConfig StartingWeapon;
	}
}
