using System.Collections.Generic;
using Codebase.Core;
using Codebase.Enemy;
using Codebase.Player.Inventory;
using Codebase.Player.Models;
using Codebase.Unit;
using Scellecs.Morpeh;

namespace Codebase.Abilities.Core
{
	public class AbilityContext
	{
		// Native ELF VA 0x15f3bf0.
		private static readonly Stack<AbilityContext> Pool = new Stack<AbilityContext>(8);
		public IAbilityOwner Owner { get; set; }
		public AbilityState State { get; set; }
		public AbilityDef Def => State.Def; // Native ELF VA 0x15f3a3c.
		public World World { get; set; }
		public PlayerModel PlayerModel { get; set; }

		public UnitCore PlayerUnit
		{
			get
			{
				return PlayerModel != null ? PlayerModel.Unit : null;
			}
		}

		public EnemyUnit Target { get; set; }
		public EnemySpawner EnemySpawner { get; set; }
		public double Damage { get; set; }
		public AbilityTrigger Trigger { get; set; }
		public int KillStatRewardMultiplier => Trigger == AbilityTrigger.OnKill && Target != null ? Target.SpawnerStatRewardMultiplier : 1; // Native 0x15f3ab4.
		public ItemModel SourceItem { get; set; }
		public bool ItemUnequipped { get; set; }
		public float DeltaTime { get; set; }

		public static AbilityContext Rent()
		{
			// Native ELF VA 0x15f3360.
			return Pool.Count > 0 ? Pool.Pop() : new AbilityContext();
		}

		public static void Release(AbilityContext context)
		{
			// Native ELF VA 0x15f3434: clear every retained reference and event payload.
			if (context == null) return;
			context.World = null;
			context.Owner = null;
			context.State = null;
			context.PlayerModel = null;
			context.Target = null;
			context.EnemySpawner = null;
			context.SourceItem = null;
			context.Damage = 0;
			context.Trigger = default;
			context.DeltaTime = 0;
			context.ItemUnequipped = false;
			Pool.Push(context);
		}

		[StaticReset]
		private static void ResetStaticState()
		{
			// Native ELF VA 0x15f3b78.
			Pool.Clear();
		}
	}
}
