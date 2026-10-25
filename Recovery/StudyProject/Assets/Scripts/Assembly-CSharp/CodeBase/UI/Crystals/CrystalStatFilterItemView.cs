using Codebase.Utilities;
using System.Linq;
using System;
using BrunoMikoski.AnimationSequencer;
using Codebase.Core;
using Codebase.Core.UI;
using Codebase.Crystals;
using Codebase.Player;
using Codebase.Unit;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Codebase.UI.Crystals
{
	public class CrystalStatFilterItemView : View
	{
		[SerializeField]
		private Image icon;

		[SerializeField]
		private TMP_Text nameText;

		[SerializeField]
		private GameObject checkedMark;

		[SerializeField]
		private Button button;

		[SerializeField]
		private AnimationSequencerController clickAnimation;

		[Inject]
		private IconStyleConfig iconStyleConfig;

		private StatType stat;

		private Sprite overrideSprite;

		private string overrideName;

		private LegendaryAbilityType? legendary;

		private Action onClick;

		public StatType? FilterStat { get; private set; }

		public LegendaryAbilityType? FilterLegendary { get; private set; }

		protected override void Awake()
		{
            // Native ELF0x13b526c.
            base.Awake(); button.onClick.AddListener(HandleClick);
        }

		public void Setup(StatType stat, Action onClick)
		{
            // Native ELF0x13b5308.
            this.stat = stat; FilterStat = stat; legendary = null; FilterLegendary = null; overrideSprite = null; overrideName = null; this.onClick = onClick; Initialize();
        }

		public void Setup(Sprite sprite, string name, LegendaryAbilityType legendary, Action onClick)
		{
            // Native ELF0x13b53c8.
            FilterStat = null; FilterLegendary = legendary; this.legendary = legendary; overrideSprite = sprite; overrideName = name; this.onClick = onClick; Initialize();
        }

		public void SetChecked(bool isChecked)
		{
            // Native ELF0x13b54b4.
            checkedMark.SetActive(isChecked);
        }

		protected override void Render()
		{
            // Native ELF0x13b54d0.
            if (overrideName != null) { icon.sprite = overrideSprite; icon.enabled = overrideSprite != null; nameText.text = overrideName; return; }
            var style = iconStyleConfig.Styles.FirstOrDefault(s => s.StatType == stat); icon.sprite = style != null ? style.Sprite : null; icon.enabled = style != null && style.Sprite != null;
            nameText.text = LocalizationStrings.Get(string.Format("STAT_{0}", stat));
        }

		private void HandleClick()
		{
            // Native ELF0x13b5710.
            clickAnimation.ClearPlayingSequence(); clickAnimation.ResetToInitialState(); clickAnimation.Play(); onClick?.Invoke();
        }
	}
}
