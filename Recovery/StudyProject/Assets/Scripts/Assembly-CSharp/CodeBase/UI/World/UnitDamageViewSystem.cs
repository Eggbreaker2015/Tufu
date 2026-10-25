using System.Collections.Generic;
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

		public void Create(WeaponHitEvent data, double damage, double resist)
		{
		}
	}
}
