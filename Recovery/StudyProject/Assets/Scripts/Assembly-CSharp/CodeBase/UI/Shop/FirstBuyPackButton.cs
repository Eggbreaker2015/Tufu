using Codebase.Analytics;
using Codebase.Core;
using Codebase.Core.UI;
using Codebase.Player.Models;
using UnityEngine;
using UnityEngine.UI;

namespace Codebase.UI.Shop
{
	public class FirstBuyPackButton : View
	{
		[SerializeField]
		private Button button;

		[SerializeField]
		private GameObject notification;

		[Inject]
		private PlayerModel playerModel;

		[Inject]
		private UserModel userModel;

		[Inject]
		private UINavigator navigator;

		protected override void Start()
		{
		}

		protected override void Render()
		{
		}
	}
}
