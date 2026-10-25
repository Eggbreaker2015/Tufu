using System;
using System.Runtime.CompilerServices;
using Codebase.Core;
using Codebase.Player.Models;
using Codebase.Utilities;
using UniMob;
using UniMob.Core;

namespace Codebase.Player.Inventory
{
	[Serializable]
	public class ItemSlotModel : Model
	{
		[KeysDropdown(typeof(ItemSlotKeys))]
		public string Key;

		public ItemSlotType SlotType;

		public bool UnlockedByDefault;

		public bool IsVip;

		private bool isUnlocked;

		private bool isPinned;

		[CompilerGenerated]
		private ItemModel __ItemModel__BackingField;

		private ComputedAtom<ItemModel> __ItemModel;

		private ComputedAtom<bool> __IsUnlocked;

		private ComputedAtom<bool> __IsEquipped;

		public ItemSlotDataUd Data { get; private set; }

		public ItemModel ItemModel
		{
			[CompilerGenerated]
			get
			{
				LifetimeScopeExtension.ThrowIfDisposed(this);
				if (__ItemModel == null) __ItemModel = CodeGenAtom.CreatePooled(this, null, () => ItemModel, false);
				return __ItemModel.DirectEvaluate() ? __ItemModel__BackingField : __ItemModel.Value;
			}
			[CompilerGenerated]
			private set
			{
				LifetimeScopeExtension.ThrowIfDisposed(this);
				if (__ItemModel != null && !__ItemModel.CompareAndInvalidate(value)) return;
				__ItemModel__BackingField = value;
			}
		}

		public bool IsUnlocked
		{
			get
			{
				LifetimeScopeExtension.ThrowIfDisposed(this);
				if (__IsUnlocked == null) __IsUnlocked = CodeGenAtom.CreatePooled(this, null, () => IsUnlocked, false);
				return __IsUnlocked.DirectEvaluate() ? isUnlocked : __IsUnlocked.Value;
			}
			set
			{
				LifetimeScopeExtension.ThrowIfDisposed(this);
				if (__IsUnlocked != null && !__IsUnlocked.CompareAndInvalidate(value)) return;
				isUnlocked = value;
				if (Data != null) Data.IsUnlocked.Value = value;
			}
		}

		public bool IsPinned
		{
			get
			{
				return this.isPinned;
			}
			set
			{
				isPinned = value;
				if (Data != null) Data.IsPinned.Value = value;
			}
		}

		public bool IsEquipped
		{
			get
			{
				LifetimeScopeExtension.ThrowIfDisposed(this);
				if (__IsEquipped == null) __IsEquipped = CodeGenAtom.CreatePooled(this, null, () => IsEquipped, false);
				return __IsEquipped.DirectEvaluate() ? ItemModel != null : __IsEquipped.Value;
			}
		}

		public ItemSlotModel(Lifetime lifetime, ItemSlotType slotType, bool isUnlocked)
			: base(lifetime)
		{
			// Native ELF VA 0x1461058.
			SlotType = slotType;
			this.isUnlocked = isUnlocked;
		}

		public void Equip(ItemModel item)
		{
			// Native ELF VA 0x145d1f8; bounded assembly ends at UdValue<string>.set_Value.
			ItemModel = item;
			if (Data != null) Data.ItemKey.Value = item?.Config.Key ?? string.Empty;
		}

		public void Unequip()
		{
			// Native ELF VA 0x145d0d0.
			ItemModel = null;
			if (Data != null) Data.ItemKey.Value = string.Empty;
		}

		public void BindToUserData(ItemSlotDataUd dataUd)
		{
			// Native ELF VA 0x145d15c: direct backing assignment avoids writing the save back.
			isUnlocked = dataUd.IsUnlocked.Value;
			isPinned = dataUd.IsPinned.Value;
			Data = dataUd;
		}
	}
}
