using Codebase.ABTests;
using Codebase.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Codebase.UI.Shop
{
	public class ShopABTestUnlocker : MonoBehaviour
	{
		private const string PasswordHash = "54cc9238f8cecb81637341869ca52418933bddf2ecdae53acd2586be0d7aa3d9";

		private const int RequiredTaps = 10;

		private const int MaxAttempts = 2;

		private static int wrongAttempts;

		private static bool isBlocked;

		[SerializeField]
		private Button secretButton;

		[SerializeField]
		private GameObject passwordPanel;

		[SerializeField]
		private TMP_InputField passwordInput;

		[SerializeField]
		private Button submitButton;

		[SerializeField]
		private Button cancelButton;

		private int tapCount;

		private ABTestDebugView cachedDebugView;

		private void Awake()
		{
		}

		private void OnEnable()
		{
		}

		private void OnSecretTap()
		{
		}

		private void ShowPanel()
		{
		}

		private void HidePanel()
		{
		}

		private void OnSubmit()
		{
		}

		private void OpenAbTestMenu()
		{
		}

		private static string ComputeHash(string value)
		{
			return null;
		}

		[StaticReset]
		private static void ResetStaticState()
		{
		}
	}
}
