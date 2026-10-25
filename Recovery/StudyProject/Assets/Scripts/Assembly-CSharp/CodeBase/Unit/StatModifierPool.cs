using UnityEngine.Pool;

namespace Codebase.Unit
{
	public class StatModifierPool
	{
		// Native ELF VA 0x15cad50 and original factory 0x15caf24; default capacity 10/max 10000, collectionCheck=true.
        private readonly ObjectPool<StatModifier> pool = new ObjectPool<StatModifier>(() => new StatModifier(), null, null, null, true, 10, 10000);

		public StatModifier Get(string name, StatType statType, ModifierType modifierType, ModifierSource source, float value, float duration)
		{
			// Native ELF VA 0x15caba0; original overwrites these six fields on reuse.
			StatModifier modifier=pool.Get();modifier.Name=name;modifier.StatType=statType;modifier.ModifierType=modifierType;modifier.Source=source;modifier.Value=value;modifier.Duration=duration;
			return modifier;
		}

		public void Return(StatModifier modifier)
		{
			// Native ELF VA 0x15cac4c; bounded original ARM64 delegates Release, without field reset.
			pool.Release(modifier);
		}
	}
}
