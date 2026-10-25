using System;
using System.Runtime.CompilerServices;
using Codebase.Core;
using Codebase.Core.UI;
using Codebase.Events;
using Codebase.Events.Models;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Codebase.UI.Events
{
	public class DifficultyButton : View
	{
		private const float AnimationDuration = 0.15f;

		[SerializeField]
		private Button button;

		[SerializeField]
		private TMP_Text difficultyText;

		[SerializeField]
		private Image iconImage;

		[SerializeField]
		private Image glowImage;

		[SerializeField]
		private LayoutElement layoutElement;

		[Header("Width Settings")]
		[SerializeField]
		private float normalWidth;

		[SerializeField]
		private float selectedWidth;

		[Inject]
		private EventStyleConfig eventStyleConfig;

		[Inject]
		private EventsRepository eventsRepository;

		[CompilerGenerated]
		private Action<EventDifficulty> m_OnClick;

		private float targetWidth;

		private float currentWidth;

		private float animationVelocity;

		public EventDifficulty Difficulty { get; private set; }

		public event Action<EventDifficulty> OnClick
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

		protected override void Awake()
		{
		}

		public void Setup(EventDifficulty difficulty)
		{
		}

		protected override void Render()
		{
		}

		private void Update()
		{
		}

		private void HandleClick()
		{
		}
	}
}
