using System;
using System.Linq;
using Codebase.Core;
using Codebase.Enemy;
using Codebase.Pet;
using Codebase.Player.Inventory;
using Codebase.Player.Models;
using UnityEngine;

namespace Codebase.Rpg
{
	public static class RpgRecruitService
	{
		public static event Action<ItemModel> Recruited;
		public static event Action<ItemModel, int, Vector3> ShardsGained;
		public static event Action<ItemModel, int> StarChanged;
		[StaticReset]
		private static void Reset() { Recruited = null; ShardsGained = null; StarChanged = null; }
		public static void OnKill(ItemConfig item, EnemySpawnerConfig config, EnemyUnit enemy)
		{
			if (item == null || item.SlotType != ItemSlotType.Weapon || PetEconomy.FindSpecies(item) == null) return;
			PlayerModel player = GameContext.Get<PlayerModel>();
			ItemModel owned = player.Items.FirstOrDefault(candidate => candidate.Config.Key == item.Key);
			if (owned == null)
			{
				owned = player.CreateItem(item, 1);
				Recruited?.Invoke(owned);
				return;
			}
			int oldStar = RpgStars.FromCopies(owned.Copies);
			int shards = RpgModeConfig.Instance.ShardsFor(RpgSpawnerRules.Kind(config));
			owned.Copies += shards;
			ShardsGained?.Invoke(owned, shards, enemy.transform.position);
			int star = RpgStars.FromCopies(owned.Copies);
			if (star != oldStar) StarChanged?.Invoke(owned, star);
		}
	}
}
