using System;
using System.Runtime.CompilerServices;
using UnityEngine;
using System.Threading;

namespace Codebase.Core.UI
{
	internal class ViewContainerHelper : MonoBehaviour
	{
		[CompilerGenerated]
		private Action m_OnEnabled;

		public event Action OnEnabled
		{
			[CompilerGenerated]
			add
			{
				Action previous, current = m_OnEnabled;
				do
				{
					previous = current;
					Action combined = (Action)Delegate.Combine(previous, value);
					current = Interlocked.CompareExchange(ref m_OnEnabled, combined, previous);
				} while (current != previous);
			}
			[CompilerGenerated]
			remove
			{
				Action previous, current = m_OnEnabled;
				do
				{
					previous = current;
					Action removed = (Action)Delegate.Remove(previous, value);
					current = Interlocked.CompareExchange(ref m_OnEnabled, removed, previous);
				} while (current != previous);
			}
		}

		private void OnEnable()
		{
			m_OnEnabled?.Invoke();
		}
	}
}
