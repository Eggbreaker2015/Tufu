using Codebase.Core;
using Codebase.Player;
using Codebase.Player.Input;
using Codebase.Player.Models;
using Codebase.Rpg.UI;
using Scellecs.Morpeh;
using UnityEngine;

namespace Codebase.Rpg.Systems
{
	public class HeroControlSystem : SystemBase
	{
		[Inject] private PlayerModel playerModel;
		[Inject] private HeroRosterModel roster;
		private RpgProgressHudView hud;

		public override void OnAwake() { }

		public override void OnUpdate(float deltaTime)
		{
			if (hud == null && GameContext.Has<Joystick>()) hud = RpgProgressHudView.Create(GameContext.Get<Joystick>());
			HeroModel request = roster.TakeRequest();
			if (UnityEngine.Input.GetKeyDown(KeyCode.Tab)) request = NextLiving();
			for (int i = 0; i < 5; i++)
				if (UnityEngine.Input.GetKeyDown(KeyCode.Alpha1 + i)) request = roster.Heroes.Find(hero => hero.SlotOrder == i);
			PlayerUnit player = playerModel.Unit;
			HeroModel protagonist = roster.Get(player);
			if (player == null || protagonist == null) return;
			if (roster.InWipe)
			{
				if (roster.Controlled != protagonist) roster.ForceControl(protagonist);
				request = null;
			}
			else if (roster.Controlled != null && !roster.Controlled.IsAlive)
			{
				HeroModel next = FirstLiving();
				if (next != null) roster.ForceControl(next);
				request = null;
			}
			if (request != null) roster.TrySwitch(request);
		}

		public override void Dispose()
		{
			if (hud != null) Object.Destroy(hud.gameObject);
		}

		private HeroModel FirstLiving()
		{
			HeroModel best = null;
			foreach (HeroModel hero in roster.Heroes)
				if (hero.IsAlive && (best == null || hero.SlotOrder < best.SlotOrder)) best = hero;
			return best;
		}

		private HeroModel NextLiving()
		{
			HeroModel first = FirstLiving();
			HeroModel best = null;
			int slot = roster.Controlled != null ? roster.Controlled.SlotOrder : -1;
			foreach (HeroModel hero in roster.Heroes)
				if (hero.IsAlive && hero.SlotOrder > slot && (best == null || hero.SlotOrder < best.SlotOrder)) best = hero;
			return best ?? first;
		}

	}
}
