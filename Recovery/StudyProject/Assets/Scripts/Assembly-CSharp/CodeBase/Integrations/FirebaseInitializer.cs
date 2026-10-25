using System.Threading.Tasks;
using Codebase.UserConsent;
using Cysharp.Threading.Tasks;
using Firebase;
using UnityEngine;

namespace Codebase.Integrations
{
	public class FirebaseInitializer : MonoBehaviour
	{
		private IUserConsentService userConsent;

		public bool IsReady { get; private set; }

		public UniTask InitializeAsync()
		{
			return default;
		}

		private void HandleDependencyResult(Task<DependencyStatus> task)
		{
		}

		private void ApplyConsent()
		{
		}
	}
}
