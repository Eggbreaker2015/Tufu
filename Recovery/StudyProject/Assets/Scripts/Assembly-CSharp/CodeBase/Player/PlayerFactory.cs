using Codebase.Core;
using Codebase.Player.Models;
using UnityEngine;

namespace Codebase.Player
{
    public class PlayerFactory : MonoBehaviour
    {
        [SerializeField] private PlayerUnit prefab;
        [SerializeField] private Transform spawnPoint;
        private void Start()
        {
            var unit = Object.Instantiate(prefab, spawnPoint);
            var model = GameContext.Get<PlayerModel>();
            model.InitializeUnit(unit);
            if (model.LastPosition != Vector3.zero)
                unit.Agent.Warp(model.LastPosition);
            model.CanMove = true;
        }
    }
}
