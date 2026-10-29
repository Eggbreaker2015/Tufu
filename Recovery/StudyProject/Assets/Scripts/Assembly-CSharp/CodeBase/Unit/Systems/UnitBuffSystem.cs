using DG.Tweening;
using Codebase.Enemy;
using Codebase.Unit;
using Codebase.BossTower.Components;
using Codebase.Core;
using Codebase.Unit.Components;
using Scellecs.Morpeh;

namespace Codebase.Unit.Systems
{
	public class UnitBuffSystem : SystemBase
	{
		private Filter buffsFilter;

		private Filter stunFilter;

		[Inject]
		private Stash<BuffStunComponent> stunStash;

		[Inject]
		private Stash<UnitComponent> unitStash;

		[Inject]
		private Stash<HasTemporaryModifiersTag> tempTagStash;

		[Inject]
		private Stash<ScaleComponent> scaleStash;

		public override void OnAwake()
		{
			// Native ELF VA 0x15ccbf0.
			buffsFilter=Filter.With<UnitComponent>().With<HasTemporaryModifiersTag>().Build();
			stunFilter=Filter.With<UnitComponent>().With<BuffStunComponent>().Build();
		}

		public override void OnUpdate(float deltaTime)
		{
			// Native ELF VA 0x15ccd80.
			UpdateTemporaryModifiers(deltaTime);
			UpdateStuns(deltaTime);
		}

		private void UpdateTemporaryModifiers(float deltaTime)
		{
			// Native ELF VA 0x15ccda8; reverse modifier iteration, return expired modifiers, clear exhausted tags.
			foreach(Entity entity in buffsFilter){
			    bool hasRemaining=false;
			    foreach(Stat stat in unitStash.Get(entity).Core.Stats){
			        for(int i=stat.TemporaryModifiers.Count-1;i>=0;i--){
			            StatModifier modifier=stat.TemporaryModifiers[i];modifier.Duration-=deltaTime;
			            if(modifier.Duration<=0f){
			                stat.RemoveModifier(modifier);GameContext.Get<StatModifierPool>().Return(modifier);
			                if(stat.Type==StatType.VisualScaleMultiplier&&scaleStash.Has(entity)){
			                    var original=scaleStash.Get(entity).OriginalScale;float scale=(float)stat.Value;
			                    var transform=unitStash.Get(entity).Core.transform;transform.DOKill();
			                    // Original rodata ELF 0x920ed8=.35f, Ease numeric 0x1e.
			                    transform.DOScale(original*scale,.35f).SetEase((Ease)0x1e);
			                }
			            }
			        }
			        hasRemaining|=stat.TemporaryModifiers.Count>0;
			    }
			    if(!hasRemaining)tempTagStash.Remove(entity);
			}
		}

		private void UpdateStuns(float deltaTime)
		{
			// Native ELF VA 0x15cd31c. Skills: tolerate non-enemy holders instead of the original enemy-only cast.
			foreach(Entity entity in stunFilter){ref BuffStunComponent stun=ref stunStash.Get(entity);stun.Duration-=deltaTime;if(stun.Duration<=0f){stunStash.Remove(entity);(unitStash.Get(entity).Core as EnemyUnit)?.ApplyStunAnimation(false);}}
		}
	}
}
