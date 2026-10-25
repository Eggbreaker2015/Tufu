using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Codebase.Core.UI;
using Codebase.Enemy;
using TMPro;
using UniMob;
using UnityEngine;
using UnityEngine.UI;

namespace Codebase.UI
{
	public class SpawnerView : LifetimeMonoBehaviour
	{
		[CompilerGenerated]
		private sealed class _003CGetChargeColors_003Ed__15 : IEnumerable<Color>, IEnumerable, IEnumerator<Color>, IEnumerator, IDisposable
		{
			private int _003C_003E1__state;

			private Color _003C_003E2__current;

			private int _003C_003El__initialThreadId;

			public SpawnerView _003C_003E4__this;

			private int _003CbonusToShow_003E5__2;

			private int _003CnormalToShow_003E5__3;

			private int _003CinactiveToShow_003E5__4;

			private int _003Ci_003E5__5;

			Color IEnumerator<Color>.Current
			{
				[DebuggerHidden]
				get
				{
					return default;
				}
			}

			object IEnumerator.Current
			{
				[DebuggerHidden]
				get
				{
					return null;
				}
			}

			[DebuggerHidden]
			public _003CGetChargeColors_003Ed__15(int _003C_003E1__state)
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

			[DebuggerHidden]
			IEnumerator<Color> IEnumerable<Color>.GetEnumerator()
			{
				return null;
			}

			[DebuggerHidden]
			IEnumerator IEnumerable.GetEnumerator()
			{
				return null;
			}
		}

		[SerializeField]
		private GameObject root;

		[SerializeField]
		private TMP_Text titleField;

		[SerializeField]
		private TMP_Text cooldownField;

		[SerializeField]
		private Image progress;

		[Header("Charges")]
		[SerializeField]
		private Transform chargesContainer;

		[SerializeField]
		private ChargeView chargePrefab;

		[SerializeField]
		private Color chargeNormalColor;

		[SerializeField]
		private Color chargeBonusColor;

		[SerializeField]
		private Color chargeInactiveColor;

		[SerializeField]
		private int maxVisibleCharges;

		private EnemySpawner spawner;

		private Transform spawnerTransform;

		private float timer;

		private ViewContainer<ChargeView, Color> chargesViewContainer;

		public void Initialize(EnemySpawner spawner)
		{
		}

		[IteratorStateMachine(typeof(_003CGetChargeColors_003Ed__15))]
		private IEnumerable<Color> GetChargeColors()
		{
			return null;
		}

		private void LateUpdate()
		{
		}

		private void Render()
		{
		}

		private void Show()
		{
		}

		private void Hide()
		{
		}

		public void Dispose()
		{
		}
	}
}
