using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Codebase.Core;
using Codebase.Events.Models;

namespace Codebase.Notifications.Schedules
{
	public class EventEndsSchedule : INotificationSchedule
	{
		[CompilerGenerated]
		private sealed class _003CBuild_003Ed__1 : IEnumerable<NotificationRequest>, IEnumerable, IEnumerator<NotificationRequest>, IEnumerator, IDisposable
		{
			private int _003C_003E1__state;

			private NotificationRequest _003C_003E2__current;

			private int _003C_003El__initialThreadId;

			public EventEndsSchedule _003C_003E4__this;

			private DateTime nowUtc;

			public DateTime _003C_003E3__nowUtc;

			NotificationRequest IEnumerator<NotificationRequest>.Current
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
			public _003CBuild_003Ed__1(int _003C_003E1__state)
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
			IEnumerator<NotificationRequest> IEnumerable<NotificationRequest>.GetEnumerator()
			{
				return null;
			}

			[DebuggerHidden]
			IEnumerator IEnumerable.GetEnumerator()
			{
				return null;
			}
		}

		[Inject]
		private EventsRepository eventsRepository;

		[IteratorStateMachine(typeof(_003CBuild_003Ed__1))]
		public IEnumerable<NotificationRequest> Build(DateTime nowUtc)
		{
			return null;
		}
	}
}
