using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Codebase.Camera
{
	public class CameraGateSystem : MonoBehaviour
	{
		[CompilerGenerated]
		private sealed class _003CFollowRoutine_003Ed__3 : IEnumerator<object>, IEnumerator, IDisposable
		{
			private int _003C_003E1__state;

			private object _003C_003E2__current;

			public CameraGateSystem _003C_003E4__this;

			public Vector3 target;

			private GameObject _003CcamObj_003E5__2;

			private Vector3 _003CstartPos_003E5__3;

			private float _003Cduration_003E5__4;

			private float _003Celapsed_003E5__5;

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
			public _003CFollowRoutine_003Ed__3(int _003C_003E1__state)
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

		public GameObject cameraPrefab;

		private void Awake()
		{
            global::Codebase.Core.GameContext.Set<CameraGateSystem>(this);
        }

		public void Follow(Vector3 target)
		{
		}

		[IteratorStateMachine(typeof(_003CFollowRoutine_003Ed__3))]
		private IEnumerator FollowRoutine(Vector3 target)
		{
			return null;
		}
	}
}
