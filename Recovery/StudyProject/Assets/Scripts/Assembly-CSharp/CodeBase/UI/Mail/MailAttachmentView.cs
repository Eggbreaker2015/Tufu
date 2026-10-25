using System;
using System.Runtime.CompilerServices;
using Codebase.Core;
using Codebase.Core.UI;
using Codebase.UI.Shop;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Codebase.UI.Mail
{
	public class MailAttachmentView : View
	{
		[SerializeField]
		private GameObject claimedState;

		[SerializeField]
		private TMP_Text amount;

		[SerializeField]
		private Image icon;

		[SerializeField]
		private Sprite defaultIcon;

		[CompilerGenerated]
		private static Action<GemsReceivedSignal> m_OnGemsReceived;

		private double value;

		public static event Action<GemsReceivedSignal> OnGemsReceived
		{
			[CompilerGenerated]
			add
			{
			}
			[CompilerGenerated]
			remove
			{
			}
		}

		public void Setup(bool claimed, double value)
		{
		}

		public void Setup(bool claimed, double value, Sprite iconSprite)
		{
		}

		public void Claim()
		{
		}

		[StaticReset]
		private static void ResetStaticState()
		{
		}
	}
}
