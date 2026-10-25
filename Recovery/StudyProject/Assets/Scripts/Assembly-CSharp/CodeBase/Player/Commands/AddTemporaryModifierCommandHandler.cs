using Scellecs.Morpeh;
using Codebase.Unit.Components;
using Codebase.Unit;
using Codebase.Core;
using System.Linq;
using System.Runtime.InteropServices;
using Codebase.Core.Commands;

namespace Codebase.Player.Commands
{
	[StructLayout((LayoutKind)0, Size = 1)]
	public struct AddTemporaryModifierCommandHandler : ICommandHandler<AddTemporaryModifierCommand>
	{
		public void Execute(AddTemporaryModifierCommand command)
		{
			// Native ELF VA 0x14681d4 and original duplicate predicate 0x14684cc.
			if(command.Unit==null||!command.Unit.TryGetStat(command.StatType,out Stat stat))return;
			if(command.RemoveDuplicates){StatModifier old=stat.Modifiers.FirstOrDefault(modifier=>modifier.Name==command.Name&&modifier.Source==command.Source);if(old!=null){stat.RemoveModifier(old);GameContext.Get<StatModifierPool>().Return(old);}}
			StatModifier next=GameContext.Get<StatModifierPool>().Get(command.Name,command.StatType,command.ModifierType,command.Source,command.Value,command.Duration);
			stat.AddModifier(next);
			World.Default.GetStash<HasTemporaryModifiersTag>().Set(command.Unit.EntityProvider.Entity,default);
		}
	}
}
