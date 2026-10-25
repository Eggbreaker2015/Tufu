using Codebase.Core;
using Codebase.Core.UI;
using Codebase.LevelPass;
using UnityEngine;
using UnityEngine.UI;

namespace Codebase.UI.LevelPass
{
	public class HudLevelPassButton : View
	{
		[SerializeField]
		private Button button;

		[SerializeField]
		private GameObject notification;

		[Inject]
		private LevelPassModel levelPassModel;

		[Inject]
		private UINavigator navigator;

		protected override void Awake()
		{
		}

		protected override void Render()
		{
		}

		private void OnButtonClick()
		{
		}
	}
}
