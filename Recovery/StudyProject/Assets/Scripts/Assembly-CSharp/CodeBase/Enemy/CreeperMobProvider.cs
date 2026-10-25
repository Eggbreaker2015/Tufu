using Codebase.Enemy.Components;
using Scellecs.Morpeh.Providers;

namespace Codebase.Enemy
{
	public class CreeperMobProvider : MonoProvider<CreeperMobComponent>
	{
		public bool ShouldDelayDeath
		{
			get
			{
				return GetData().State != CreeperMobState.Finalized;
			}
		}

		protected override void OnEnable()
		{
			base.OnEnable();
			GetData().State = CreeperMobState.Alive;
		}
	}
}
