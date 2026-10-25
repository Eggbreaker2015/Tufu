using BrunoMikoski.AnimationSequencer;
using Codebase.Analytics;
using Codebase.Core;
using Codebase.Player.Models;
using UnityEngine;
using UnityEngine.AI;
using Codebase.Analytics.Events;

namespace Codebase.Gates
{
	public class Gate : MonoBehaviour
	{
		[SerializeField]
		private AnimationSequencerController openAnimation;
		[SerializeField]
		private NavMeshObstacle navMeshObstacle;
		[SerializeField]
		public Transform Exit;
		public int NextLocationNumber;
		private GateDataUd data;
		[Inject]
		private PlayerModel playerModel;
		[Inject]
		private IAnalyticsWrapper analytics;
		[Inject]
		private GatesModel gatesModel;
		[Inject]
		private UserModel userModel;
		public bool IsUnlocked
		{
			get
			{
				return data != null && data.IsUnlocked.Value;
			}
		}

		public string Key
		{
			get
			{
				return name;
			}
		}

		public int MinimapVersion { get; private set; }

		private void Start()
		{
			gatesModel.GateCores.Add(this);
			data = gatesModel.GetOrCreateGate(name);
			if (IsUnlocked)
			{
				playerModel.AdvanceBossProgressLocation(NextLocationNumber);
				Open();
			}
		}

		public void Open()
		{
			openAnimation.Play();
			navMeshObstacle.enabled = false;
			if (!IsUnlocked)
			{
				analytics.Send(new GateOpenEvent(NextLocationNumber.ToString(), name));
				playerModel.UnlockLocation(NextLocationNumber);
				playerModel.AdvanceBossProgressLocation(NextLocationNumber);
				MinimapVersion++;
			}

			gatesModel.OpenGate(name);
		}

		private void OnDrawGizmos()
		{
		}
	}
}
