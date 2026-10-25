using TMPro;
using UnityEngine;

namespace Codebase.Utilities
{
	[RequireComponent(typeof(TMP_Text))]
	public class TextLayoutUpdater : MonoBehaviour
	{
		private TMP_Text text;

		private void Awake()
		{
		}

		private void OnDestroy()
		{
		}

		private void OnTextChanged(Object obj)
		{
		}
	}
}
