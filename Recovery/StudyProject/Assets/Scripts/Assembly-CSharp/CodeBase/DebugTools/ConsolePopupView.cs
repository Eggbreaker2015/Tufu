using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Codebase.Core;
using Codebase.Core.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Codebase.DebugTools
{
	public class ConsolePopupView : PopupView
	{
		private struct LogEntry
		{
			public string FirstLine;

			public string FullLog;

			public bool IsError;
		}

		[CompilerGenerated]
		private static Action m_OnLogsCleared;

		[SerializeField]
		private Transform contentParent;

		[SerializeField]
		private GameObject logButtonPrefab;

		[SerializeField]
		private int maxLogs;

		[Header("Buttons")]
		[SerializeField]
		private Button closeButton;

		[SerializeField]
		private Button clearButton;

		[Header("Filters")]
		[SerializeField]
		private Toggle showErrorsToggle;

		[SerializeField]
		private Toggle showLogsToggle;

		[Header("Detail Popup")]
		[SerializeField]
		private GameObject detailPopup;

		[SerializeField]
		private TMP_Text detailText;

		[SerializeField]
		private Button detailCloseButton;

		[SerializeField]
		private Button detailCopyButton;

		[Header("Colors")]
		[SerializeField]
		private Color errorColor;

		[SerializeField]
		private Color logColor;

		private static readonly List<LogEntry> allLogs;

		private static int maxLogsStatic;

		private static bool isListenerRegistered;

		private readonly List<GameObject> activeViews;

		private readonly Queue<GameObject> viewPool;

		private bool showErrors;

		private bool showLogs;

		private string currentDetailLog;

		public static int ErrorCount { get; private set; }

		public static event Action OnLogsCleared
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

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
		private static void RegisterLogListener()
		{
		}

		private static void HandleLogStatic(string logString, string stackTrace, LogType type)
		{
		}

		private static string GetFirstLineStatic(string text)
		{
			return null;
		}

		public override void Initialize()
		{
		}

		public override void Cleanup()
		{
		}

		private bool ShouldDisplay(LogEntry entry)
		{
			return false;
		}

		private void CreateLogView(LogEntry entry)
		{
		}

		private GameObject GetOrCreateView()
		{
			return null;
		}

		private void RecycleView(GameObject view)
		{
		}

		private void RebuildLogList()
		{
		}

		private void OnShowErrorsChanged(bool value)
		{
		}

		private void OnShowLogsChanged(bool value)
		{
		}

		private void ShowDetailPopup(string fullLog)
		{
		}

		private void CloseDetailPopup()
		{
		}

		private void CopyDetailToClipboard()
		{
		}

		private void ClearLogs()
		{
		}

		private void ClearViews()
		{
		}

		[StaticReset]
		private static void ResetStaticState()
		{
		}
	}
}
