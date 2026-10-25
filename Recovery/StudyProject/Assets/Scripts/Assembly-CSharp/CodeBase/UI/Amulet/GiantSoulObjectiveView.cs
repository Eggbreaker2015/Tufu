using UnityEngine;

namespace Codebase.UI.Amulet
{
	public class GiantSoulObjectiveView : MonoBehaviour
	{
		[SerializeField]
		private GameObject root;

		private void Awake()
		{
            global::Codebase.Core.GameContext.Set(this);
            root.SetActive(false);
        }

		public void Show()
		{
		}

		public void Hide()
		{
		}
	}
}
