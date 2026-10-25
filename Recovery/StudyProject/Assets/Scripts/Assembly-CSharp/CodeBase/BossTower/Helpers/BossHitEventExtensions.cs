using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Codebase.Unit.Events;
using Scellecs.Morpeh;
using Scellecs.Morpeh.Collections;
using Scellecs.Morpeh.Providers;

namespace Codebase.BossTower.Helpers
{
	public static class BossHitEventExtensions
	{
		[CompilerGenerated]
		private sealed class _003CIterateHitsOn_003Ed__0<T> : IEnumerable<(WeaponHitEvent, Entity)>, IEnumerable, IEnumerator<(WeaponHitEvent, Entity)>, IEnumerator, IDisposable where T : struct, IComponent
		{
			private int _003C_003E1__state;
			private (WeaponHitEvent Hit, Entity Entity) _003C_003E2__current;
			private int _003C_003El__initialThreadId;
			private Event<WeaponHitEvent> hitEvent;
			public Event<WeaponHitEvent> _003C_003E3__hitEvent;
			private Stash<T> stash;
			public Stash<T> _003C_003E3__stash;
			private FastList<WeaponHitEvent>.Enumerator _003C_003E7__wrap1;
			(WeaponHitEvent, Entity) IEnumerator<(WeaponHitEvent, Entity)>.Current
			{
				[DebuggerHidden]
				get
				{
					return System_002ECollections_002EGeneric_002EIEnumerator_003C_0028Codebase_002EUnit_002EEvents_002EWeaponHitEventHit_002CScellecs_002EMorpeh_002EEntityEntity_0029_003E_002Eget_Current();
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
			public _003CIterateHitsOn_003Ed__0(int _003C_003E1__state)
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
			private (WeaponHitEvent, Entity) System_002ECollections_002EGeneric_002EIEnumerator_003C_0028Codebase_002EUnit_002EEvents_002EWeaponHitEventHit_002CScellecs_002EMorpeh_002EEntityEntity_0029_003E_002Eget_Current()
			{
				return default;
			}

			[DebuggerHidden]
			void IEnumerator.Reset()
			{
			}

			[DebuggerHidden]
			IEnumerator<(WeaponHitEvent, Entity)> IEnumerable<(WeaponHitEvent, Entity)>.GetEnumerator()
			{
				return null;
			}

			[DebuggerHidden]
			IEnumerator IEnumerable.GetEnumerator()
			{
				return null;
			}
		}

		public static IEnumerable<(WeaponHitEvent, Entity)> IterateHitsOn<T>(this Event<WeaponHitEvent> hitEvent, Stash<T> stash)
			where T : struct, IComponent
		{
			foreach (WeaponHitEvent hit in hitEvent.BatchedChanges)
			{
				if (hit.Target == null)
					continue;
				EntityProvider provider = hit.Target.GetComponent<EntityProvider>();
				if (provider == null)
					continue;
				Entity entity = provider.Entity;
				if (stash.Has(entity))
					yield return (hit, entity);
			}
		}
	}
}
