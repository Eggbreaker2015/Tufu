using System;
using System.Collections.Generic;
using CodeBase.Audio;
using Codebase.Core;
using Codebase.Core.UI;
using Codebase.Quests;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Codebase.Analytics;

namespace Codebase.UI.Quests
{
	public class QuestNpcDialogueView : PopupView
	{
		[SerializeField]
		private Image npcIcon;
		[SerializeField]
		private TMP_Text npcName;
		[SerializeField]
		private TypewriterText monologue;
		[SerializeField]
		private Button tapButton;
		[Inject]
		private IAudioPlayer audioPlayer;
		private List<QuestDialogueLine> lines;
		private int lineIndex;
		private Action onComplete;
		private SoundHandle speechHandle;
		protected override void Awake()
		{ // Original ELF 0x16351ec; candidate only.
			base.Awake();
			tapButton.onClick.AddListener(HandleTap);
		}

		public void Setup(Sprite icon, string nameKey, List<QuestDialogueLine> lines, Action onComplete)
		{ // Original ELF 0x1635288; candidate only.
			npcIcon.sprite = icon;
			npcName.text = Codebase.Utilities.LocalizationStrings.Get(nameKey);
			this.lines = lines;
			lineIndex = 0;
			this.onComplete = onComplete;
			PlayCurrentLine();
		}

		private void PlayCurrentLine()
		{ // Original ELF 0x163531c; candidate only.
			var line = lines[lineIndex];
			speechHandle?.Stop();
			speechHandle = audioPlayer.PlayClipOnceInterruptible(line.Clip, 1f);
			monologue.Play(Codebase.Utilities.LocalizationStrings.Get(line.TextKey));
		}

		private void HandleTap()
		{ // Original ELF 0x163545c; candidate only.
			audioPlayer.PlaySoundOnce("tap");
			if (monologue.IsTyping)
			{
				monologue.Skip();
				return;
			}

			lineIndex++;
			if (lineIndex < lines.Count)
				PlayCurrentLine();
			else
				Finish();
		}

		private void Finish()
		{ // Original ELF 0x163557c; candidate only.
			speechHandle?.Stop();
			speechHandle = null;
			var callback = onComplete;
			onComplete = null;
			callback?.Invoke();
			Close();
		}
	}
}
