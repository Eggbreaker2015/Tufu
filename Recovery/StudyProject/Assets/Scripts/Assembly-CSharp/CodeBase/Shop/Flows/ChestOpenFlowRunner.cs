using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using CodeBase.Audio;
using Codebase.Core;
using Codebase.Core.Flows;
using Codebase.Core.UI;
using Codebase.Pet;
using Codebase.Player.Inventory;
using Codebase.Player.Models;
using Codebase.Rewards;
using Codebase.UI.Shop;
using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.CompilerServices;
using System.Linq;

namespace Codebase.Shop.Flows
{
	public class ChestOpenFlowRunner : IFlowRunner<ChestOpenFlow>, IFlowHandlerBase
	{
		[StructLayout((LayoutKind)3)]
		[CompilerGenerated]
		private struct _003CExecute_003Ed__5 : IAsyncStateMachine
		{
			public int _003C_003E1__state;
			public AsyncUniTaskMethodBuilder _003C_003Et__builder;
			public ChestOpenFlow flow;
			public ChestOpenFlowRunner _003C_003E4__this;
			private int _003CchestsRemaining_003E5__2;
			private List<ItemModel> _003CdroppedItems_003E5__3;
			private ChestOpenView _003CchestOpenView_003E5__4;
			private int _003CcopiesGained_003E5__5;
			private UniTask.Awaiter _003C_003Eu__1;
			private void MoveNext()
			{
			}

			void IAsyncStateMachine.MoveNext()
			{
				//ILSpy generated this explicit interface implementation from .override directive in MoveNext
				this.MoveNext();
			}

			[DebuggerHidden]
			private void SetStateMachine(IAsyncStateMachine stateMachine)
			{
			}

			void IAsyncStateMachine.SetStateMachine(IAsyncStateMachine stateMachine)
			{
				//ILSpy generated this explicit interface implementation from .override directive in SetStateMachine
				this.SetStateMachine(stateMachine);
			}
		}

		[StructLayout((LayoutKind)3)]
		[CompilerGenerated]
		private struct _003CGenerateDropsAndAddToPlayer_003Ed__6 : IAsyncStateMachine
		{
			public int _003C_003E1__state;
			public AsyncUniTaskMethodBuilder _003C_003Et__builder;
			public ChestOpenFlowRunner _003C_003E4__this;
			public ChestDef chestDef;
			public int dropCopies;
			public List<ItemModel> droppedItems;
			public int chestCount;
			private UniTask.Awaiter _003C_003Eu__1;
			private void MoveNext()
			{
			}

			void IAsyncStateMachine.MoveNext()
			{
				//ILSpy generated this explicit interface implementation from .override directive in MoveNext
				this.MoveNext();
			}

			[DebuggerHidden]
			private void SetStateMachine(IAsyncStateMachine stateMachine)
			{
			}

			void IAsyncStateMachine.SetStateMachine(IAsyncStateMachine stateMachine)
			{
				//ILSpy generated this explicit interface implementation from .override directive in SetStateMachine
				this.SetStateMachine(stateMachine);
			}
		}

		[Inject]
		private PlayerModel playerModel;
		[Inject]
		private ItemsProvider itemsProvider;
		[Inject]
		private IAudioPlayer audioPlayer;
		[Inject]
		private UINavigator navigator;
		[Inject]
		private ChestsModel chestsModel;
		public async UniTask Execute(ChestOpenFlow flow)
		{
			int chestsRemaining = flow.ChestCount;
			var droppedItems = new List<ItemModel>();
			var view = navigator.Open<ChestOpenView>("Assets/Prefabs/UI/ChestOpenView.prefab", false);
			audioPlayer.PlaySoundOnce("chest_buy");
			int copiesGained = chestsModel.GetEquipmentDropCopies(flow.ChestDef.Key, flow.ChestDef.Drops[0].ItemType, chestsModel.Level);
			await GenerateDropsAndAddToPlayer(flow.ChestDef, chestsRemaining, copiesGained, droppedItems);
			PetEconomy.OnChestsOpened(flow.ChestCount);
			droppedItems = droppedItems.OrderByDescending(item => item.Rarity).ThenByDescending(item => item.EffectiveLevel).ThenByDescending(item => item.Copies).ToList();
			view.SetDroppedItems(droppedItems);
			view.SetChestSprites(flow.ChestDef.ClosedSprite, flow.ChestDef.OpenedSprite);
			while (chestsRemaining > 0)
			{
				view.SetChestCount(chestsRemaining);
				view.SetActive(true);
				await view.WaitForTap();
				view.SetDrop(droppedItems[chestsRemaining - 1], copiesGained);
				audioPlayer.PlaySoundOnce("chest_riser");
				await view.WaitForTapOrAnimationEnd();
				await view.WaitForTap();
				chestsRemaining--;
				if (chestsRemaining > 0)
					view.ResetAnimation();
			}

			view.Close();
		}

		private async UniTask GenerateDropsAndAddToPlayer(ChestDef chestDef, int chestCount, int dropCopies, List<ItemModel> droppedItems)
		{
			for (int i = 0; i < chestCount; i++)
			{
				var drop = SelectDrop(chestDef);
				var slotTypes = GetSlotTypesForItemType(drop.ItemType);
				var candidates = itemsProvider.Items.Where(item => ItemsProvider.IsAvailable(item) && item.Rarity == drop.Rarity && slotTypes.Contains(item.SlotType)).ToList();
				ItemConfig selected = candidates[UnityEngine.Random.Range(0, candidates.Count)];
				ItemModel existing = playerModel.Items.FirstOrDefault(item => item.Config == selected);
				var display = new ItemModel(playerModel.Lifetime, selected);
				if (existing != null)
				{
					display.SetInitialCopies(unchecked(dropCopies + existing.Copies));
					if (existing.Evolved)
					{
						display.Evolved = true;
						display.Level = existing.Level;
					}
				}
				else
					display.SetInitialCopies(dropCopies);
				var granted = new ItemModel(playerModel.Lifetime, selected);
				granted.SetInitialCopies(dropCopies);
				if (!global::Codebase.Rpg.RpgModeConfig.Enabled)
				playerModel.AddItem(granted, false, true);
				droppedItems.Add(display);
			}

			await UniTask.CompletedTask;
		}

		private List<ItemSlotType> GetSlotTypesForItemType(ItemType itemType)
		{
			var result = new List<ItemSlotType>();
			if (itemType == ItemType.Weapon)
				result.Add(ItemSlotType.Weapon);
			else if (itemType == ItemType.Armor)
			{
				result.Add(ItemSlotType.Helmet);
				result.Add(ItemSlotType.Shield);
			}

			return result;
		}

		private ChestDropDef SelectDrop(ChestDef chestDef)
		{
			if (chestsModel.IsRareChest(chestDef.Key))
				return new ChestDropDef
				{
					ItemType = chestDef.Drops[0].ItemType,
					Rarity = chestsModel.RollRareEquipmentRarity(),
					Weight = 1f
				};
			return SelectDropByWeight(chestDef.Drops);
		}

		private ChestDropDef SelectDropByWeight(List<ChestDropDef> drops)
		{
			float total = drops.Sum(drop => drop.Weight);
			float roll = UnityEngine.Random.value * total;
			float cumulative = 0f;
			foreach (var drop in drops)
			{
				cumulative += drop.Weight;
				if (roll <= cumulative)
					return drop;
			}

			return drops.Last();
		}
	}
}
