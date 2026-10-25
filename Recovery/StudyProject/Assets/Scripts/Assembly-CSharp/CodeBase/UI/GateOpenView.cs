using Codebase.Core;
using Codebase.Camera;
using TMPro;
using UnityEngine;

namespace Codebase.UI
{
	public class GateOpenView : MonoBehaviour
	{
		[SerializeField]
		private TMP_Text text;

		private void Update()
		{
			// Original ELF0x15d5ef8; text visibility follows the true gate camera state each frame.
			text.enabled = GameContext.Get<CameraFollowSystem>().IsShowingGate;
		}
	}
}
