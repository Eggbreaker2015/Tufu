using System.Collections.Generic;
using Codebase.Core;
using Codebase.Enemy;
using Codebase.Unit.Events;
using UnityEngine;

namespace Codebase.UI.World
{
	public class UnitDamageViewSystem : MonoBehaviour
	{
		[SerializeField]
		private UnitDamageView prefab;

		private List<Vector3> offsets;

		private int playerOffsetIndex;

		private int enemyOffsetIndex;

		public void Awake()
		{
            global::Codebase.Core.GameContext.Set<UnitDamageViewSystem>(this);
        }

		// Body not recovered (empty stub); pet mode: implemented for damage numbers over pets, the only caller.
		public void Create(WeaponHitEvent data, double damage, double resist)
		{
			if (prefab == null || data.Target == null || !GameContext.Has<DynamicWorldCanvas>())
				return;
			if (offsets == null)
				offsets = new List<Vector3>
				{
					new Vector3(0f, 0f, 0f),
					new Vector3(0.5f, 0.3f, 0f),
					new Vector3(-0.5f, 0.15f, 0f),
					new Vector3(0.25f, 0.6f, 0f),
					new Vector3(-0.25f, 0.45f, 0f)
				};
			int index;
			if (data.Target is EnemyUnit)
				index = enemyOffsetIndex = (enemyOffsetIndex + 1) % offsets.Count;
			else
				index = playerOffsetIndex = (playerOffsetIndex + 1) % offsets.Count;
			Transform point = data.Target.HealthBarPoint != null ? data.Target.HealthBarPoint : data.Target.transform;
			DynamicWorldCanvas canvas = GameContext.Get<DynamicWorldCanvas>();
			UnitDamageView view = canvas.CreateNew(prefab);
			view.Setup(data, damage, resist);
			view.Place(canvas, point.position + offsets[index]);
		}
	}
}
