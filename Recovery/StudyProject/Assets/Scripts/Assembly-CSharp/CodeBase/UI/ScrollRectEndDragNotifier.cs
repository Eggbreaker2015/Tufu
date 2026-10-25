using System.Threading;
using System;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Codebase.UI
{
	public class ScrollRectEndDragNotifier : MonoBehaviour, IEndDragHandler, IEventSystemHandler, IBeginDragHandler
	{
		[CompilerGenerated]
		private Action m_Ended;

		[CompilerGenerated]
		private Action m_Begin;

		public event Action Ended
		{
			add
			{
				// Original APK ARM64 ELF VA 0x15f969c.
				Action previous = m_Ended, current;
				do
				{
					current = previous;
					Action updated = (Action)Delegate.Combine(current, value);
					previous = Interlocked.CompareExchange(ref m_Ended, updated, current);
				} while (previous != current);
			}
			remove
			{
				// Original APK ARM64 ELF VA 0x15f9738.
				Action previous = m_Ended, current;
				do
				{
					current = previous;
					Action updated = (Action)Delegate.Remove(current, value);
					previous = Interlocked.CompareExchange(ref m_Ended, updated, current);
				} while (previous != current);
			}
		}
		public event Action Begin
		{
			add
			{
				// Original APK ARM64 ELF VA 0x15f97d4.
				Action previous = m_Begin, current;
				do
				{
					current = previous;
					Action updated = (Action)Delegate.Combine(current, value);
					previous = Interlocked.CompareExchange(ref m_Begin, updated, current);
				} while (previous != current);
			}
			remove
			{
				// Original APK ARM64 ELF VA 0x15f9870.
				Action previous = m_Begin, current;
				do
				{
					current = previous;
					Action updated = (Action)Delegate.Remove(current, value);
					previous = Interlocked.CompareExchange(ref m_Begin, updated, current);
				} while (previous != current);
			}
		}
		public void OnEndDrag(PointerEventData eventData)
		{
			// Original APK ARM64 ELF VA 0x15f990c.
			m_Ended?.Invoke();
		}

		public void OnBeginDrag(PointerEventData eventData)
		{
			// Original APK ARM64 ELF VA 0x15f9928.
			m_Begin?.Invoke();
		}
	}
}
