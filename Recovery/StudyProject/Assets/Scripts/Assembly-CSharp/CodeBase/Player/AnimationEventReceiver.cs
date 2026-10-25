using System;
using System.Runtime.CompilerServices;
using System.Threading;
using UnityEngine;

namespace Codebase.Player
{
	public class AnimationEventReceiver : MonoBehaviour
	{
		[CompilerGenerated]
		private Action m_OnWalkEnabled = delegate { };

		[CompilerGenerated]
		private Action m_OnWeaponAppear = delegate { };

		public event Action OnWalkEnabled
		{
			[CompilerGenerated]
			add
			{
				Action current = m_OnWalkEnabled;
				Action previous;
				do
				{
					previous = current;
					current = Interlocked.CompareExchange(ref m_OnWalkEnabled, (Action)Delegate.Combine(previous, value), previous);
				} while (current != previous);
			}
			[CompilerGenerated]
			remove
			{
				Action current = m_OnWalkEnabled;
				Action previous;
				do
				{
					previous = current;
					current = Interlocked.CompareExchange(ref m_OnWalkEnabled, (Action)Delegate.Remove(previous, value), previous);
				} while (current != previous);
			}
		}

		public event Action OnWeaponAppear
		{
			[CompilerGenerated]
			add
			{
				Action current = m_OnWeaponAppear;
				Action previous;
				do
				{
					previous = current;
					current = Interlocked.CompareExchange(ref m_OnWeaponAppear, (Action)Delegate.Combine(previous, value), previous);
				} while (current != previous);
			}
			[CompilerGenerated]
			remove
			{
				Action current = m_OnWeaponAppear;
				Action previous;
				do
				{
					previous = current;
					current = Interlocked.CompareExchange(ref m_OnWeaponAppear, (Action)Delegate.Remove(previous, value), previous);
				} while (current != previous);
			}
		}

		public void WalkEnabled()
		{
			m_OnWalkEnabled();
		}

		public void WeaponAppear()
		{
			m_OnWeaponAppear();
		}
	}
}
