using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Codebase.CloudSave;
using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.CompilerServices;
using Multicast.CloudSave;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Codebase.UI
{
	public class CloudSaveConflictView : MonoBehaviour
	{
		[StructLayout((LayoutKind)3)]
		[CompilerGenerated]
		private struct _003CShowAndWaitForChoice_003Ed__20 : IAsyncStateMachine
		{
			public int _003C_003E1__state;

			public AsyncUniTaskMethodBuilder<CloudSaveChoice> _003C_003Et__builder;

			public CloudSaveConflictView _003C_003E4__this;

			public DateTimeOffset? serverTime;

			public DevourSaveSummary local;

			public DevourSaveSummary cloud;

			public DateTimeOffset? cloudUpdatedAt;

			public string cloudClientVersion;

			private UniTask.Awaiter _003C_003Eu__1;

			private void MoveNext()
			{
			}

			void IAsyncStateMachine.MoveNext()
			{
				//ILSpy generated this explicit interface implementation from .override directive in MoveNext
				this.MoveNext();
			}

			[DebuggerHidden]
			private void SetStateMachine(IAsyncStateMachine stateMachine)
			{
			}

			void IAsyncStateMachine.SetStateMachine(IAsyncStateMachine stateMachine)
			{
				//ILSpy generated this explicit interface implementation from .override directive in SetStateMachine
				this.SetStateMachine(stateMachine);
			}
		}

		[SerializeField]
		private GameObject panel;

		[SerializeField]
		private TextMeshProUGUI titleText;

		[SerializeField]
		private TextMeshProUGUI localDetailsText;

		[SerializeField]
		private TextMeshProUGUI localSavedAtText;

		[SerializeField]
		private GameObject localMoreProgressBadge;

		[SerializeField]
		private GameObject localMoreCurrencyBadge;

		[SerializeField]
		private Button localButton;

		[SerializeField]
		private TextMeshProUGUI cloudDetailsText;

		[SerializeField]
		private TextMeshProUGUI cloudSavedAtText;

		[SerializeField]
		private TextMeshProUGUI cloudClientVersionText;

		[SerializeField]
		private GameObject cloudMoreProgressBadge;

		[SerializeField]
		private GameObject cloudMoreCurrencyBadge;

		[SerializeField]
		private Button cloudButton;

		[SerializeField]
		private string title;

		[SerializeField]
		private string unavailableText;

		[SerializeField]
		private string justNowText;

		[SerializeField]
		private string clientVersionFormat;

		private bool waitingForChoice;

		private CloudSaveChoice choice;

		private void Awake()
		{
		}

		[AsyncStateMachine(typeof(_003CShowAndWaitForChoice_003Ed__20))]
		public UniTask<CloudSaveChoice> ShowAndWaitForChoice(DevourSaveSummary local, DevourSaveSummary cloud, DateTimeOffset? cloudUpdatedAt, DateTimeOffset? serverTime, string cloudClientVersion)
		{
			return default;
		}

		private void FillCard(DevourSaveSummary summary, TextMeshProUGUI detailsText, TextMeshProUGUI savedAtText, DateTime? savedAtUtc, DateTimeOffset now)
		{
		}

		private void ApplyBadges(DevourSaveSummary local, DevourSaveSummary cloud)
		{
		}

		private void Select(CloudSaveChoice value)
		{
		}

		private static string FormatDetails(DevourSaveSummary summary)
		{
			return null;
		}

		private static string FormatPlaytime(int minutes)
		{
			return null;
		}

		private string FormatAge(TimeSpan age)
		{
			return null;
		}
	}
}
