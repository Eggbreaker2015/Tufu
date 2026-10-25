using Codebase.Utilities;
using System;
using System.Runtime.CompilerServices;
using Codebase.Core.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Codebase.UI.Shop
{
	public class ShopTabButtonView : View
	{
		[SerializeField]
		private Button button;

		[SerializeField]
		private GameObject pressedState;

		[SerializeField]
		private GameObject lockedState;

		[SerializeField]
		private TMP_Text[] titleTexts;

		[SerializeField]
		private GameObject notification;

		[CompilerGenerated]
		private Action<ShopTab> m_OnClicked;

		private string localizationKey;

		private bool isSelected;

		private bool hasNotification;

		private bool isLocked;

		public ShopTab Tab { get; private set; }

		public event Action<ShopTab> OnClicked
		{
            add { m_OnClicked += value; }
            remove { m_OnClicked -= value; }
		}

		protected override void Awake()
		{
            // Native 0x1629e10.
            base.Awake();
            button.onClick.AddListener(HandleClick);
		}

		public void Setup(ShopTabDef tab)
		{
            // Native 0x1629eac.
            Tab = tab.Tab; localizationKey = tab.LocalizationKey;
            Initialize(); Render();
		}

		public void SetSelected(bool selected)
		{
            // Native 0x1629ee8.
            isSelected = selected;
            Render();
		}

		public void SetNotification(bool value)
		{
            // Native 0x1629f00.
            hasNotification = value;
            Render();
		}

		public void SetLocked(bool locked)
		{
            // Native 0x1629f18.
            isLocked = locked;
            Render();
		}

		protected override void Render()
		{
            // Native 0x1629f30.
            string title = LocalizationStrings.Get(localizationKey);
            foreach (var text in titleTexts) text.text = title;
            button.interactable = !isLocked;
            lockedState.SetActive(isLocked);
            pressedState.SetActive(!isLocked && isSelected);
            notification.SetActive(!isLocked && hasNotification && !isSelected);
		}

		private void HandleClick()
		{
            // Native 0x162a04c.
            if (!isLocked) m_OnClicked?.Invoke(Tab);
		}

		public override void Cleanup()
		{
            // Native 0x162a088.
            base.Cleanup();
            m_OnClicked = null;
		}
	}
}
