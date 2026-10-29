using Codebase.Core;
using Codebase.Player.Models;
using Codebase.SecretChest.Components;
using Codebase.Utilities;
using Scellecs.Morpeh;
using UnityEngine;
using UnityEngine.AI;

namespace Codebase.SecretChest.Systems
{
    public class SecretChestProximitySystem : SystemBase
    {
        private const float HideBuffer = 1f;
        private const float SampleRadius = 3f;
        private const float UpdateInterval = 0.15f;
        [Inject] private Stash<SecretChestComponent> chestStash;
        [Inject] private Stash<SecretChestNearMarker> nearMarkerStash;
        [Inject] private PlayerModel playerModel;
        private Filter filter;
        private NavMeshPath path;
        private float updateTimer;

        public override void OnAwake()
        {
            // APK ARM64 0x1412274.
            filter = Filter.With<SecretChestComponent>().Build();
            path = new NavMeshPath();
        }

        public override void OnUpdate(float deltaTime)
        {
            // APK 0x1412368. Opened chests still show the found-count label nearby.
            updateTimer += deltaTime;
            if (updateTimer < UpdateInterval) return;
            updateTimer = 0f;
            if (global::Codebase.Rpg.RpgControl.ControlledUnit(playerModel) == null) return;
            Vector3 from = global::Codebase.Rpg.RpgControl.ControlledUnit(playerModel).transform.position;
            foreach (var entity in filter)
            {
                ref var chest = ref chestStash.Get(entity);
                float distance = chest.ShowDistance + (nearMarkerStash.Has(entity) ? HideBuffer : 0f);
                nearMarkerStash.SetIf(entity, chest.Root != null && chest.Root.gameObject.activeInHierarchy &&
                    IsWithinNavMeshDistance(from, chest.Root.position, distance));
            }
        }

        private bool IsWithinNavMeshDistance(Vector3 from, Vector3 to, float maxDistance)
        {
            // APK 0x14125d0: Euclidean rejection followed by a complete walkable path.
            if ((from - to).sqrMagnitude > maxDistance * maxDistance) return false;
            if (!NavMesh.SamplePosition(from, out var fromHit, SampleRadius, NavMesh.AllAreas) ||
                !NavMesh.SamplePosition(to, out var toHit, SampleRadius, NavMesh.AllAreas)) return false;
            return NavMesh.CalculatePath(fromHit.position, toHit.position, NavMesh.AllAreas, path) &&
                path.status == NavMeshPathStatus.PathComplete && PathLength() <= maxDistance;
        }

        private float PathLength()
        {
            // APK 0x14126dc.
            Vector3[] corners = path.corners;
            float length = 0f;
            for (int i = 1; i < corners.Length; i++) length += Vector3.Distance(corners[i - 1], corners[i]);
            return length;
        }
    }
}
