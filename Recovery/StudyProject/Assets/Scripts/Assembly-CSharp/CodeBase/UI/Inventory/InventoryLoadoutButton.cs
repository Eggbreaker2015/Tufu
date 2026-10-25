using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using BrunoMikoski.AnimationSequencer;
using CodeBase.Audio;
using Codebase.Core;
using Codebase.Core.UI;
using Codebase.Player.Models;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Codebase.UI.Inventory
{
	public class InventoryLoadoutButton : View, IPointerDownHandler, IEventSystemHandler, IPointerUpHandler
	{
		[CompilerGenerated]
		private sealed class _003CFillProgressBar_003Ed__11 : IEnumerator<object>, IEnumerator, IDisposable
		{
			private int _003C_003E1__state;

			private object _003C_003E2__current;

			public InventoryLoadoutButton _003C_003E4__this;

			private float _003Celapsed_003E5__2;

			object IEnumerator<object>.Current
			{
				[DebuggerHidden]
				get
				{
					return this._003C_003E2__current;
				}
			}

			object IEnumerator.Current
			{
				[DebuggerHidden]
				get
				{
					return this._003C_003E2__current;
				}
			}

			[DebuggerHidden]
			public _003CFillProgressBar_003Ed__11(int _003C_003E1__state)
			{
			}

			[DebuggerHidden]
			void IDisposable.Dispose()
			{
			}

			private bool MoveNext()
			{
				return false;
			}

			bool IEnumerator.MoveNext()
			{
				//ILSpy generated this explicit interface implementation from .override directive in MoveNext
				return this.MoveNext();
			}

			[DebuggerHidden]
			void IEnumerator.Reset()
			{
			}
		}

		[SerializeField]
		private Image fillBar;

		[SerializeField]
		private TMP_Text messageText;

		[SerializeField]
		private AnimationSequencerController messageAnimation;

		[SerializeField]
		private float holdDuration;

		[Inject]
		private PlayerModel playerModel;

		[Inject]
		private IAudioPlayer audioPlayer;

		private Coroutine fillCoroutine;

		protected override void Awake()
		{
		}

		private void OnEnable()
		{
		}

		public void OnPointerDown(PointerEventData eventData)
		{
		}

		public void OnPointerUp(PointerEventData eventData)
		{
		}

		[IteratorStateMachine(typeof(_003CFillProgressBar_003Ed__11))]
		private IEnumerator FillProgressBar()
		{
			return null;
		}

		private void OnHoldComplete()
		{
		}

		private void OnTap()
		{
		}

		private void ShowMessage(string key)
		{
		}
	}
}
