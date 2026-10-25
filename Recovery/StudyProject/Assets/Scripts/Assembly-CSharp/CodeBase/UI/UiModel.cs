using System.Runtime.CompilerServices;
using Codebase.Core;
using UniMob;
using UniMob.Core;

namespace Codebase.UI
{
	public class UiModel : Model
	{
		[CompilerGenerated]
		private bool __IsMinimapVisible__BackingField;

		[CompilerGenerated]
		private bool __IsHudVisible__BackingField;

		[CompilerGenerated]
		private bool __IsWorldUiVisible__BackingField;

		[CompilerGenerated]
		private bool __BossHudVisible__BackingField;

		[CompilerGenerated]
		private bool __BlockItemsHotbar__BackingField;

		[CompilerGenerated]
		private bool __IsVipProgressCurrentlyShowing__BackingField;

		[CompilerGenerated]
		private bool __IsItemSlotUnlockShowing__BackingField;

		[CompilerGenerated]
		private bool __IsGateOpeningShowing__BackingField;

		[CompilerGenerated]
		private bool __IsPortalGemCollectShowing__BackingField;

		private ComputedAtom<bool> __IsMinimapVisible;

		private ComputedAtom<bool> __IsHudVisible;

		private ComputedAtom<bool> __IsWorldUiVisible;

		private ComputedAtom<bool> __BossHudVisible;

		private ComputedAtom<bool> __BlockItemsHotbar;

		private ComputedAtom<bool> __IsVipProgressCurrentlyShowing;

		private ComputedAtom<bool> __IsItemSlotUnlockShowing;

		private ComputedAtom<bool> __IsGateOpeningShowing;

		private ComputedAtom<bool> __IsPortalGemCollectShowing;

		public bool IsMinimapVisible
        {
            get
            {
                LifetimeScopeExtension.ThrowIfDisposed(this);
                if (__IsMinimapVisible == null) __IsMinimapVisible = CodeGenAtom.CreatePooled(this, null, () => IsMinimapVisible, false);
                return __IsMinimapVisible.DirectEvaluate() ? __IsMinimapVisible__BackingField : __IsMinimapVisible.Value;
            }
            set
            {
                LifetimeScopeExtension.ThrowIfDisposed(this);
                if (__IsMinimapVisible != null && !__IsMinimapVisible.CompareAndInvalidate(value)) return;
                __IsMinimapVisible__BackingField = value;
            }
        }

		public bool IsHudVisible
        {
            get
            {
                LifetimeScopeExtension.ThrowIfDisposed(this);
                if (__IsHudVisible == null) __IsHudVisible = CodeGenAtom.CreatePooled(this, null, () => IsHudVisible, false);
                return __IsHudVisible.DirectEvaluate() ? __IsHudVisible__BackingField : __IsHudVisible.Value;
            }
            set
            {
                LifetimeScopeExtension.ThrowIfDisposed(this);
                if (__IsHudVisible != null && !__IsHudVisible.CompareAndInvalidate(value)) return;
                __IsHudVisible__BackingField = value;
            }
        }

		public bool IsWorldUiVisible
        {
            get
            {
                LifetimeScopeExtension.ThrowIfDisposed(this);
                if (__IsWorldUiVisible == null) __IsWorldUiVisible = CodeGenAtom.CreatePooled(this, null, () => IsWorldUiVisible, false);
                return __IsWorldUiVisible.DirectEvaluate() ? __IsWorldUiVisible__BackingField : __IsWorldUiVisible.Value;
            }
            set
            {
                LifetimeScopeExtension.ThrowIfDisposed(this);
                if (__IsWorldUiVisible != null && !__IsWorldUiVisible.CompareAndInvalidate(value)) return;
                __IsWorldUiVisible__BackingField = value;
            }
        }

		public bool BossHudVisible
        {
            get
            {
                LifetimeScopeExtension.ThrowIfDisposed(this);
                if (__BossHudVisible == null) __BossHudVisible = CodeGenAtom.CreatePooled(this, null, () => BossHudVisible, false);
                return __BossHudVisible.DirectEvaluate() ? __BossHudVisible__BackingField : __BossHudVisible.Value;
            }
            set
            {
                LifetimeScopeExtension.ThrowIfDisposed(this);
                if (__BossHudVisible != null && !__BossHudVisible.CompareAndInvalidate(value)) return;
                __BossHudVisible__BackingField = value;
            }
        }

		public bool BlockItemsHotbar
        {
            get
            {
                LifetimeScopeExtension.ThrowIfDisposed(this);
                if (__BlockItemsHotbar == null) __BlockItemsHotbar = CodeGenAtom.CreatePooled(this, null, () => BlockItemsHotbar, false);
                return __BlockItemsHotbar.DirectEvaluate() ? __BlockItemsHotbar__BackingField : __BlockItemsHotbar.Value;
            }
            set
            {
                LifetimeScopeExtension.ThrowIfDisposed(this);
                if (__BlockItemsHotbar != null && !__BlockItemsHotbar.CompareAndInvalidate(value)) return;
                __BlockItemsHotbar__BackingField = value;
            }
        }

		public bool IsVipProgressCurrentlyShowing
        {
            get
            {
                LifetimeScopeExtension.ThrowIfDisposed(this);
                if (__IsVipProgressCurrentlyShowing == null) __IsVipProgressCurrentlyShowing = CodeGenAtom.CreatePooled(this, null, () => IsVipProgressCurrentlyShowing, false);
                return __IsVipProgressCurrentlyShowing.DirectEvaluate() ? __IsVipProgressCurrentlyShowing__BackingField : __IsVipProgressCurrentlyShowing.Value;
            }
            set
            {
                LifetimeScopeExtension.ThrowIfDisposed(this);
                if (__IsVipProgressCurrentlyShowing != null && !__IsVipProgressCurrentlyShowing.CompareAndInvalidate(value)) return;
                __IsVipProgressCurrentlyShowing__BackingField = value;
            }
        }

		public bool IsItemSlotUnlockShowing
        {
            get
            {
                LifetimeScopeExtension.ThrowIfDisposed(this);
                if (__IsItemSlotUnlockShowing == null) __IsItemSlotUnlockShowing = CodeGenAtom.CreatePooled(this, null, () => IsItemSlotUnlockShowing, false);
                return __IsItemSlotUnlockShowing.DirectEvaluate() ? __IsItemSlotUnlockShowing__BackingField : __IsItemSlotUnlockShowing.Value;
            }
            set
            {
                LifetimeScopeExtension.ThrowIfDisposed(this);
                if (__IsItemSlotUnlockShowing != null && !__IsItemSlotUnlockShowing.CompareAndInvalidate(value)) return;
                __IsItemSlotUnlockShowing__BackingField = value;
            }
        }

		public bool IsGateOpeningShowing
        {
            get
            {
                LifetimeScopeExtension.ThrowIfDisposed(this);
                if (__IsGateOpeningShowing == null) __IsGateOpeningShowing = CodeGenAtom.CreatePooled(this, null, () => IsGateOpeningShowing, false);
                return __IsGateOpeningShowing.DirectEvaluate() ? __IsGateOpeningShowing__BackingField : __IsGateOpeningShowing.Value;
            }
            set
            {
                LifetimeScopeExtension.ThrowIfDisposed(this);
                if (__IsGateOpeningShowing != null && !__IsGateOpeningShowing.CompareAndInvalidate(value)) return;
                __IsGateOpeningShowing__BackingField = value;
            }
        }

		public bool IsPortalGemCollectShowing
        {
            get
            {
                LifetimeScopeExtension.ThrowIfDisposed(this);
                if (__IsPortalGemCollectShowing == null) __IsPortalGemCollectShowing = CodeGenAtom.CreatePooled(this, null, () => IsPortalGemCollectShowing, false);
                return __IsPortalGemCollectShowing.DirectEvaluate() ? __IsPortalGemCollectShowing__BackingField : __IsPortalGemCollectShowing.Value;
            }
            set
            {
                LifetimeScopeExtension.ThrowIfDisposed(this);
                if (__IsPortalGemCollectShowing != null && !__IsPortalGemCollectShowing.CompareAndInvalidate(value)) return;
                __IsPortalGemCollectShowing__BackingField = value;
            }
        }

		public UiModel(Lifetime lifetime)
            : base(lifetime)
        {
            // Native ELF VA 0x15ffcbc: minimap/HUD/world visible, hotbar blocked initially.
            __IsMinimapVisible__BackingField = true;
            __IsHudVisible__BackingField = true;
            __IsWorldUiVisible__BackingField = true;
            __BlockItemsHotbar__BackingField = true;
        }
	}
}
