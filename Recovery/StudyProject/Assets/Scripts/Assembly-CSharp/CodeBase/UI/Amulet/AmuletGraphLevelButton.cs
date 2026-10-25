using System;
using System.Runtime.CompilerServices;
using System.Threading;
using BrunoMikoski.AnimationSequencer;
using Codebase.Amulet;
using Codebase.Amulet.Upgrades;
using Codebase.Core;
using Codebase.Core.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Codebase.UI.Amulet
{
	public class AmuletGraphLevelButton : View
	{
		[SerializeField]
		private Button button;

		[SerializeField]
		private TMP_Text[] levelTexts;

		[SerializeField]
		private GameObject lockState;

		[SerializeField]
		private Image notification;

		[SerializeField]
		private GameObject newNotification;

		[Header("Selection")]
		[SerializeField]
		private GameObject defaultState;

		[SerializeField]
		private GameObject maxedState;

		[SerializeField]
		private GameObject selectedState;

		[Header("Effects")]
		[SerializeField]
		private AnimationSequencerController lockedTapAnimation;

		[Inject]
		private AmuletUpgradeConfig config;

		[Inject]
		private AmuletModel amuletModel;

		[Inject]
		private AmuletUpgradesModel upgradesModel;

		[CompilerGenerated]
		private Action<AmuletGraphLevelButton> m_OnClicked;

		private bool isSelected;

		public int AmuletLevel { get; private set; }

		public event Action<AmuletGraphLevelButton> OnClicked
		{
			[CompilerGenerated]
			add
			{
				Action<AmuletGraphLevelButton> previous, current = m_OnClicked;
				do { previous = current; current = Interlocked.CompareExchange(ref m_OnClicked, (Action<AmuletGraphLevelButton>)Delegate.Combine(previous, value), previous); } while (current != previous);
			}
			[CompilerGenerated]
			remove
			{
				Action<AmuletGraphLevelButton> previous, current = m_OnClicked;
				do { previous = current; current = Interlocked.CompareExchange(ref m_OnClicked, (Action<AmuletGraphLevelButton>)Delegate.Remove(previous, value), previous); } while (current != previous);
			}
		}

		protected override void Awake()
		{
			// Native ELF VA 0x13c983c.
			base.Awake();
			button.onClick.AddListener(HandleClick);
		}

		public void Setup(int amuletLevel)
		{
			// Native ELF VA 0x13c98d8.
			AmuletLevel = amuletLevel;
			string text = amuletLevel.ToString();
			foreach (TMP_Text field in levelTexts) field.text = text;
			Initialize();
			Render();
		}

		public void SetSelected(bool selected)
		{
			// Native ELF VA 0x13c9990.
			isSelected = selected;
			Render();
		}

		protected override void Render()
		{
			// Native ELF VA 0x13c99a8: read Version even when the displayed state is unchanged.
			_ = upgradesModel.Version;
			bool locked = IsLocked();
			lockState.SetActive(locked);
			bool maxed = false;
			if (!locked)
			{
				newNotification.SetActive(!isSelected && HasNewNodes());
				maxed = IsAmuletLevelFullyMaxed();
			}
			else newNotification.SetActive(false);
			defaultState.SetActive(!isSelected && !maxed);
			maxedState.SetActive(!isSelected && maxed);
			selectedState.SetActive(isSelected);
		}

		private bool IsAmuletLevelFullyMaxed()
		{
			// Native ELF VA 0x13c9cd4: an empty level is not fully maxed.
			bool found = false;
			foreach (AmuletUpgradeDef definition in config.Upgrades)
			{
				if (definition.RequiredAmuletLevel != AmuletLevel) continue;
				found = true;
				if (!upgradesModel.IsMaxLevel(definition.Key)) return false;
			}
			return found;
		}

		private bool HasNewNodes()
		{
			// Native ELF VA 0x13c9ad0.
			foreach (AmuletUpgradeDef definition in config.Upgrades)
				if (definition.RequiredAmuletLevel == AmuletLevel && upgradesModel.IsNewNode(definition.Key)) return true;
			return false;
		}

		private bool IsLocked()
		{
			// Native ELF VA 0x13c9aa0.
			return amuletModel.Level < AmuletLevel;
		}

		private void HandleClick()
		{
			// Native ELF VA 0x13c9ee8.
			if (IsLocked())
			{
				lockedTapAnimation.ClearPlayingSequence();
				lockedTapAnimation.ResetToInitialState();
				lockedTapAnimation.Play();
			}
			else m_OnClicked?.Invoke(this);
		}

		public override void Cleanup()
		{
			// Native ELF VA 0x13c9f64.
			base.Cleanup();
			m_OnClicked = null;
		}
	}
}
