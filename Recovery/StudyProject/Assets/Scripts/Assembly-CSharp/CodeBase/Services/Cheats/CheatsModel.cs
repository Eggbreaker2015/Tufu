using Codebase.Core;
using Codebase.Gates;
using Codebase.LevelPass;
using Codebase.Player.Inventory;
using Codebase.Player.Models;
using Codebase.SecretChest;
using UniMob;

namespace Codebase.Services.Cheats
{
	public class CheatsModel : Model
	{
		[Inject]
		private PlayerModel playerModel;

		[Inject]
		private GatesModel gatesModel;

		[Inject]
		private ItemsProvider itemsProvider;

		[Inject]
		private LevelPassModel levelPassModel;

		[Inject]
		private SecretChestModel secretChestModel;

		public int DropMultiplier { get; set; }

		public bool GodMode { get; set; }

		public bool StopBossTowerTimer { get; set; }

		public bool OneHitMode { get; set; }

		public bool OneDamageMode { get; set; }

		public bool SkipLocalPurchaseDelivery { get; set; }

		public int PlaytestOnlineMinutes { get; set; }

		public int PlaytestOfflineMinutes { get; set; }

		public float PlaytestTimeScale { get; set; }

		public CheatsModel(Lifetime lifetime)
			: base(lifetime)
		{
			// Native ELF VA 0x140d87c; the tail branch invokes Model(lifetime).
			DropMultiplier = 1;
			PlaytestTimeScale = 1f;
		}

		public void GiveAllEquipment()
		{
		}

		public void UnlockItemSlots()
		{
		}

		public void LevelUpItems(int amount)
		{
		}

		public void AddCopiesToAllItems(int amount)
		{
		}

		public void EvolveAllItems()
		{
		}

		public void UnlockAllGates()
		{
		}

		public void CompleteLevelPassTiers()
		{
		}

		public void UnlockPortal()
		{
		}

		public void LockPortal()
		{
		}

		public void CollectNextPortalGem()
		{
		}

		public void IncreaseMovementSpeed()
		{
		}

		public void SetPlayerHealthPercent(float percent)
		{
		}

		public void ResetCrystalObjective()
		{
		}

		public void ResetQuestProgress()
		{
		}

		public void SkipCurrentQuestToLastStep()
		{
		}

		public void TeleportToQuestMob()
		{
		}

		public void UnlockCrystalObjective()
		{
		}

		public void TeleportToFirstUnopenedSecretChest()
		{
		}
	}
}

