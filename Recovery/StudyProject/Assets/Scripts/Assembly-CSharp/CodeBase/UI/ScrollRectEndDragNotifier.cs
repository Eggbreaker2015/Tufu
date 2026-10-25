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
			[CompilerGenerated]
			add
			{
			}
			[CompilerGenerated]
			remove
			{
			}
		}

		public event Action Begin
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

		public void OnEndDrag(PointerEventData eventData)
		{
		}

		public void OnBeginDrag(PointerEventData eventData)
		{
		}
	}
}
