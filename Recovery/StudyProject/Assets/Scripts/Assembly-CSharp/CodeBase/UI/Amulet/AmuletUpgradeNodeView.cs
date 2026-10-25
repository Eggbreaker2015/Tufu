using System.Threading;
using System;
using System.Runtime.CompilerServices;
using AssetKits.ParticleImage;
using BrunoMikoski.AnimationSequencer;
using CodeBase.Audio;
using Codebase.Amulet;
using Codebase.Amulet.Upgrades;
using Codebase.Core;
using Codebase.Core.UI;
using Codebase.UI.Utilities;
using UnityEngine;
using UnityEngine.UI;

namespace Codebase.UI.Amulet
{
	public class AmuletUpgradeNodeView : View
	{
		[SerializeField]
		private Button button;

		[SerializeField]
		private Image iconImage;

		[SerializeField]
		private Image backgroundImage;

		[Header("Additional Images")]
		[SerializeField]
		private Image upgradeAvailableImage;

		[SerializeField]
		private SoulView soulView;

		[SerializeField]
		private Image checkmarkImage;

		[SerializeField]
		private ImageOverlay imageOverlay;

		[SerializeField]
		private RectTransform progress;

		[SerializeField]
		private Image[] levelDependentImages;

		[Header("Background Sprites")]
		[SerializeField]
		private Sprite unknownSprite;

		[SerializeField]
		private Sprite unknownLockedSprite;

		[SerializeField]
		private Sprite knownSprite;

		[SerializeField]
		private Sprite knownLockedSprite;

		[SerializeField]
		private Sprite inProgressSprite;

		[SerializeField]
		private Sprite maxLevelSprite;

		[Header("Notification")]
		[SerializeField]
		private GameObject notification;

		[Header("Effects")]
		[SerializeField]
		private ParticleImage upgradeParticle;

		[SerializeField]
		private AnimationSequencerController tapAnimation;

		[Inject]
		private AmuletUpgradesModel upgradesModel;

		[Inject]
		private AmuletUpgradeConfig config;

		[Inject]
		private SoulStyleConfig soulStyleConfig;

		[Inject]
		private IAudioPlayer audioPlayer;

		[CompilerGenerated]
		private Action<AmuletUpgradeNodeView> m_OnClicked;

		private Sprite iconSprite;

		private float defaultProgressHeight;

		private int lastRenderedLevel;

		private string lastKnownSoulKey;

		private bool isZoomedOut;

		public string UpgradeKey { get; private set; }

		public Vector2 Position => rectTransform.anchoredPosition; // Native ELF VA 0x13d843c.

		// Native ELF VA 0x13d4fcc/0x13d4c4c: original CompareExchange event accessors.
        public event Action<AmuletUpgradeNodeView> OnClicked
        {
            add
            {
                Action<AmuletUpgradeNodeView> current, next;
                do { current = m_OnClicked; next = (Action<AmuletUpgradeNodeView>)Delegate.Combine(current,value); }
                while (Interlocked.CompareExchange(ref m_OnClicked,next,current) != current);
            }
            remove
            {
                Action<AmuletUpgradeNodeView> current, next;
                do { current = m_OnClicked; next = (Action<AmuletUpgradeNodeView>)Delegate.Remove(current,value); }
                while (Interlocked.CompareExchange(ref m_OnClicked,next,current) != current);
            }
        }

        protected override void Awake()
		{
// Native ELF VA 0x13d8454.
            base.Awake();
            button.onClick.AddListener(HandleClick);
            defaultProgressHeight = progress.sizeDelta.y;
		}

		public void Setup(string upgradeKey, Sprite icon, Vector2 position)
		{
// Native ELF VA 0x13d4f24.
            UpgradeKey = upgradeKey;
            iconSprite = icon;
            rectTransform.anchoredPosition = position;
            lastRenderedLevel = upgradesModel.GetLevel(upgradeKey);
            Initialize();
            Render();
		}

		protected override void Render()
		{
// Native ELF VA 0x13d8508, including original state-update/effect/cache ordering.
            AmuletUpgradeNodeState state = GetState();
            bool canAfford = upgradesModel.CanAfford(UpgradeKey);
            int level = upgradesModel.GetLevel(UpgradeKey);
            UpdateIconImage(state);
            UpdateBackgroundSprite(state);
            UpdateSoulImage(state);
            UpdateCheckmarkImage(state);
            UpdateImageOverlay(state);
            UpdateProgress(level);
            UpdateLevelDependentImages(level);
            UpdateUpgradeAvailableImage(state,canAfford);
            UpdateNotification();
            button.interactable = state == AmuletUpgradeNodeState.Known || state == AmuletUpgradeNodeState.InProgress || state == AmuletUpgradeNodeState.MaxLevel;
            if (lastRenderedLevel < level) PlayUpgradeEffects();
            lastRenderedLevel = level;
            string soulKey = GetNextLevelSoulKey();
            if (!string.IsNullOrEmpty(soulKey)) lastKnownSoulKey = soulKey;
		}

		private AmuletUpgradeNodeState GetState()
		{
// Native ELF VA 0x13d8644.
            int level = upgradesModel.GetLevel(UpgradeKey);
            if (upgradesModel.IsMaxLevel(UpgradeKey)) return AmuletUpgradeNodeState.MaxLevel;
            if (level > 0) return AmuletUpgradeNodeState.InProgress;
            bool unknown = IsUnknown();
            bool locked = upgradesModel.IsAmuletLevelLocked(UpgradeKey);
            if (unknown) return locked ? AmuletUpgradeNodeState.UnknownLocked : AmuletUpgradeNodeState.Unknown;
            return locked ? AmuletUpgradeNodeState.KnownLocked : AmuletUpgradeNodeState.Known;
		}

		private bool IsUnknown()
		{
// Native ELF VA 0x13d8c68: any purchased parent makes the original node known.
            AmuletUpgradeDef definition = config.GetUpgrade(UpgradeKey);
            if (definition == null) return true;
            if (definition.RequiredUpgradeKeys == null || definition.RequiredUpgradeKeys.Count == 0) return false;
            foreach (string parent in definition.RequiredUpgradeKeys) if (upgradesModel.GetLevel(parent) > 0) return false;
            return true;
		}

		private void UpdateIconImage(AmuletUpgradeNodeState state)
		{
// Native ELF VA 0x13d86e8.
            bool visible = (uint)state > 1;
            iconImage.gameObject.SetActive(visible);
            if (visible && iconSprite != null) iconImage.sprite = iconSprite;
		}

		private void UpdateBackgroundSprite(AmuletUpgradeNodeState state)
		{
// Native ELF VA 0x13d87a0.
            switch (state)
            {
                case AmuletUpgradeNodeState.Unknown: backgroundImage.sprite = unknownSprite; break;
                case AmuletUpgradeNodeState.UnknownLocked: backgroundImage.sprite = unknownLockedSprite; break;
                case AmuletUpgradeNodeState.KnownLocked: backgroundImage.sprite = knownLockedSprite; break;
                case AmuletUpgradeNodeState.InProgress: backgroundImage.sprite = inProgressSprite; break;
                case AmuletUpgradeNodeState.MaxLevel: backgroundImage.sprite = maxLevelSprite; break;
                default: backgroundImage.sprite = knownSprite; break;
            }
		}

		private void UpdateSoulImage(AmuletUpgradeNodeState state)
		{
// Native ELF VA 0x13d8820.
            if (state != AmuletUpgradeNodeState.Unknown && state != AmuletUpgradeNodeState.UnknownLocked && state != AmuletUpgradeNodeState.MaxLevel)
            {
                string soulKey = GetNextLevelSoulKey();
                if (!string.IsNullOrEmpty(soulKey))
                {
                    soulView.Setup(soulStyleConfig.GetStyle(soulKey));
                    soulView.gameObject.SetActive(true);
                    return;
                }
            }
            soulView.gameObject.SetActive(false);
		}

		private void UpdateCheckmarkImage(AmuletUpgradeNodeState state)
		{
// Native ELF VA 0x13d88d4.
            checkmarkImage.gameObject.SetActive(state == AmuletUpgradeNodeState.MaxLevel && !isZoomedOut);
		}

		public void SetZoomedOut(bool isZoomedOut)
		{
// Native ELF VA 0x13d1240.
            this.isZoomedOut = isZoomedOut;
            UpdateCheckmarkImage(GetState());
		}

		private void UpdateImageOverlay(AmuletUpgradeNodeState state)
		{
// Native ELF VA 0x13d8928.
            imageOverlay.enabled = state == AmuletUpgradeNodeState.MaxLevel;
		}

		private void UpdateProgress(int level)
		{
// Native ELF VA 0x13d8948, original square-root progression and 40-unit span.
            int maxLevel = upgradesModel.GetMaxLevel(UpgradeKey);
            float scale = maxLevel < 1 ? 0f : 40f / Mathf.Sqrt(maxLevel);
            Vector2 size = progress.sizeDelta;
            size.y = defaultProgressHeight + Mathf.Sqrt(level) * scale;
            progress.sizeDelta = size;
		}

		private void UpdateLevelDependentImages(int level)
		{
// Native ELF VA 0x13d89d8.
            foreach (Image image in levelDependentImages) image.enabled = level > 0;
            backgroundImage.enabled = level < 1;
		}

		private string GetNextLevelSoulKey()
		{
// Native ELF VA 0x13d8c48.
            return upgradesModel.GetNextSoulKey(UpgradeKey);
		}

		private void UpdateUpgradeAvailableImage(AmuletUpgradeNodeState state, bool canAfford)
		{
// Native ELF VA 0x13d8a64.
            upgradeAvailableImage.gameObject.SetActive((state == AmuletUpgradeNodeState.Known || state == AmuletUpgradeNodeState.InProgress) && canAfford);
		}

		private void UpdateNotification()
		{
// Native ELF VA 0x13d8aac.
            notification.SetActive(upgradesModel.IsNewNode(UpgradeKey));
		}

		private void HandleClick()
		{
// Native ELF VA 0x13d8e70. Clicking marks seen and invokes the real subscriber; no purchase is fabricated here.
            upgradesModel.MarkNodeSeen(UpgradeKey);
            PlayTapAnimation();
            m_OnClicked?.Invoke(this);
		}

		private void PlayTapAnimation()
		{
// Native ELF VA 0x13d8ec0 virtual slots 22 (ResetToInitialState), 8 (Play).
            tapAnimation.ClearPlayingSequence();
            tapAnimation.ResetToInitialState();
            tapAnimation.Play();
		}

		private void PlayUpgradeEffects()
		{
// Native ELF VA 0x13d8ae8 retains previous soul for max-level upgrade effects.
            string soulKey = lastKnownSoulKey ?? GetNextLevelSoulKey();
            if (!string.IsNullOrEmpty(soulKey))
            {
                SoulStyle style = soulStyleConfig.GetStyle(soulKey);
                if (style != null) upgradeParticle.startColor = new ParticleSystem.MinMaxGradient(style.Color);
            }
            upgradeParticle.Play();
            audioPlayer.PlaySoundOnce(GetUpgradeSoundKey(soulKey));
		}

		private string GetUpgradeSoundKey(string soulKey)
		{
// Native ELF VA 0x13d8f0c; original metadata strings, no replacement audio keys.
            if (GetSoulNominal(soulKey) >= 6) return "graph_unregular_buy";
            return upgradesModel.IsMaxLevel(UpgradeKey) ? "graph_regular_final_buy" : "graph_regular_buy";
		}

		private static int GetSoulNominal(string soulKey)
		{
// Native ELF VA 0x13d8fb0.
            switch (soulKey)
            {
                case "large": return 2;
                case "elite": return 3;
                case "huge": return 4;
                case "great": return 5;
                case "divine": return 6;
                case "chaotic": return 7;
                default: return 1;
            }
		}

		public override void Cleanup()
		{
// Native ELF VA 0x13d91c4. Original listener remains bound to pooled component.
            base.Cleanup();
            m_OnClicked = null;
		}
	}
}
