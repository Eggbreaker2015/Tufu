using Codebase.Core;
using UnityEngine;

namespace Codebase.Crystals.Objective
{
	public class CrystalUnitScene : MonoBehaviour
	{
		[SerializeField]
		private ParticleSystem hitParticle;

		[SerializeField]
		private ParticleSystem destructionVfx;

		[SerializeField]
		private Transform bannerPoint;

		[SerializeField]
		private CrystalUnitBannerView bannerPrefab;

		[SerializeField]
		private GameObject[] aliveOnlyObjects;

		[SerializeField]
		private ParticleSystem[] hpColorParticles;

		[SerializeField]
		private ParticleSystem halfHpVfx;

		private const float HalfHpThreshold = 0.5f;

		private bool halfHpVfxPlaying;

		private static readonly Color FullColor = ParseColor("84FFF0");

		private static readonly Color DrainedColor = ParseColor("FFCF84");

		private static readonly float[] ColorUpperHp = { 1f, 0.7f, 0.4f };

		private static readonly float[] ColorLowerHp = { 0.7f, 0.4f, 0.2f };

		public Transform BannerPoint => this.bannerPoint;

		public CrystalUnitBannerView BannerPrefab => this.bannerPrefab;

		private void Awake()
		{
			GameContext.Set(this);
			StopHalfHpVfx();
		}

		private void Start()
		{
			SyncObjectiveVisualState();
		}

		public void SyncObjectiveVisualState()
		{
			CrystalModel model = GameContext.Get<CrystalModel>();
			if (model.IsObjectiveWaitingForRespawn)
			{
				SetAliveObjectsActive(false);
				StopHalfHpVfx();
			}
			else if (model.IsUnlocked)
			{
				SetAliveObjectsActive(true);
				UpdateHpColors(1f);
			}
		}

		public void PlayHit()
		{
			hitParticle.Play();
		}

		public void PlayDestruction()
		{
			destructionVfx.Play();
			StopHalfHpVfx();
			SetAliveObjectsActive(false);
		}

		public void UpdateHpColors(float hpFraction)
		{
			for (int i = 0; i < hpColorParticles.Length; i++)
			{
				float blend = Mathf.InverseLerp(ColorUpperHp[i], ColorLowerHp[i], hpFraction);
				var main = hpColorParticles[i].main;
				main.startColor = Color.Lerp(FullColor, DrainedColor, blend);
			}
			UpdateHalfHpVfx(hpFraction);
		}

		private void UpdateHalfHpVfx(float hpFraction)
		{
			bool play = hpFraction <= HalfHpThreshold;
			if (halfHpVfxPlaying == play) return;
			halfHpVfxPlaying = play;
			if (play) halfHpVfx.Play();
			else
			{
				halfHpVfx.Stop(true, ParticleSystemStopBehavior.StopEmitting);
				halfHpVfxPlaying = false;
			}
		}

		private void StopHalfHpVfx()
		{
			halfHpVfx.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
			halfHpVfxPlaying = false;
		}

		public void ResetAliveState()
		{
			SetAliveObjectsActive(true);
			UpdateHpColors(1f);
		}

		private void SetAliveObjectsActive(bool active)
		{
			foreach (GameObject aliveObject in aliveOnlyObjects) aliveObject.SetActive(active);
		}

		private static Color ParseColor(string hex)
		{
			ColorUtility.TryParseHtmlString("#" + hex, out Color color);
			return color;
		}
	}
}
