using System.Linq;
using Codebase.Crystals.Objective;
using BrunoMikoski.AnimationSequencer;
using Codebase.Amulet;
using Codebase.Core;
using Codebase.DebugTools.Dummy;
using Codebase.Economy;
using Codebase.Enemy;
using Codebase.Events;
using Codebase.Events.Models;
using Codebase.Player;
using Codebase.Player.Models;
using Codebase.Unit;
using Codebase.Unit.Components;
using Codebase.Utilities;
using DG.Tweening;
using Scellecs.Morpeh;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Codebase.UI.World
{
	public class HealthBar : MonoBehaviour
	{
		[SerializeField]
		private Image progress;

		[SerializeField]
		private TMP_Text text;

		[SerializeField]
		private TMP_Text drops;

		[SerializeField]
		private TMP_Text resists;

		[SerializeField]
		private TMP_Text soul;

		[SerializeField]
		private GameObject glow;

		[SerializeField]
		private GameObject marksRoot;

		[SerializeField]
		private TMP_Text marksText;

		[SerializeField]
		private AnimationSequencerController marksAnimation;

		[SerializeField]
		private TMP_Text dummyStats;

		[SerializeField]
		private VisibilityAnimator visibilityAnimator;

		[Inject]
		private PlayerModel playerModel;

		[Inject]
		private IconStyleConfig iconStyleConfig;

		[Inject]
		private EventStyleConfig eventStyleConfig;

		[Inject]
		private SoulStyleConfig soulStyleConfig;

		[Inject]
		private CurrencyStyleConfig currencyStyleConfig;

		[Inject]
		private EventsRepository eventsRepository;

		[Inject]
		private Stash<MarkComponent> markStash;

		[Inject]
		private Stash<DummyTargetTag> dummyStash;

		[Inject]
		private Stash<UnitSpawnActiveMarker> spawnActiveStash;

		private UnitCore unit;

		private EnemySkillStatusView skillStatus;

		private PlayerUnit playerUnit;

		private double previousHealth;

		private bool showSoul;

		private bool showEmptyAmulet;

		private int previousMarks;

		private bool isDummy;

		private bool spawnHidden;

		private bool wasSpawnActive;

		private Vector3 originalScale = Vector3.one;

		private static readonly Color MaxStacksColor = new Color(1f, 0.9215686321258545f, 0.01568627543747425f, 1f);

		private static readonly Color DefaultColor = Color.white;

		private bool IsEnemy => unit is EnemyUnit;

		public void Initialize(UnitCore unit)
		{
			// Native ELF VA 0x1600b18; follow the original injection/entity sequence.
			this.unit = unit;
			if (skillStatus != null) skillStatus.Unbind();
			if (playerUnit == null) playerUnit = playerModel.Unit;
			isDummy = dummyStash.Has(unit.EntityProvider.Entity);
			previousMarks = -1;
			if (dummyStats != null)
			{
				dummyStats.gameObject.SetActive(isDummy);
				dummyStats.text = string.Empty;
			}
			spawnHidden = false;
			wasSpawnActive = false;
			RenderHealth();
			RenderDrops();
			if (unit is PlayerUnit player)
			{
				originalScale = transform.localScale;
				player.OnDeathPhaseEnter += HandleDeathPhase;
			}
			UpdateSpawnVisibility(true);
		}

		// Enemy cast bar and buff icons (EnemyHealthBarSystem); built once per pooled bar.
		public void BindSkillStatus()
		{
			if (!IsEnemy) return;
			if (skillStatus == null) skillStatus = EnemySkillStatusView.Create(this, (RectTransform)transform, text);
			skillStatus.Bind(unit);
		}

		public void UnbindSkillStatus(UnitCore owner)
		{
			if (skillStatus != null && skillStatus.IsBoundTo(owner)) skillStatus.Unbind();
		}

		private void OnDestroy()
		{
			// Native ELF VA 0x1601a58.
			if (unit is PlayerUnit player) player.OnDeathPhaseEnter -= HandleDeathPhase;
			DOTween.Kill(transform);
		}

		private void HandleDeathPhase(DeathPhase phase, float duration)
		{
			// Native ELF VA 0x1601bc0: only Hiding and Reviving change this bar.
			if (phase == DeathPhase.Hiding)
			{
				DOTween.Kill(transform);
				transform.DOScale(Vector3.zero, duration).SetEase((Ease)5).SetTarget(transform);
			}
			else if (phase == DeathPhase.Reviving)
			{
				DOTween.Kill(transform);
				transform.DOScale(originalScale, 0.2f).SetDelay(duration).SetEase((Ease)27).SetTarget(transform).OnComplete(RenderHealth);
			}
		}

		private void Start()
		{
			// Native ELF VA 0x1601e44.
			playerUnit = playerModel.Unit;
		}

		private void LateUpdate()
		{
			// Native ELF VA 0x1601e60.
			if (IsEnemy)
			{
				EnemyUnit enemy = (EnemyUnit)unit;
				if (showSoul != enemy.HasSoul || showEmptyAmulet != enemy.HasEmptyAmulet)
				{
					RenderDrops();
					showSoul = enemy.HasSoul;
					showEmptyAmulet = enemy.HasEmptyAmulet;
					return;
				}
			}
			if (unit.CurrentHealth < 0.0) return;
			if (unit.CurrentHealth != previousHealth) RenderHealth();
			RenderMarks();
			RenderDummyStats();
			UpdateSpawnVisibility(false);
		}

		private void UpdateSpawnVisibility(bool force)
		{
			// Native ELF VA 0x1601960.
			if (!IsEnemy) return;
			bool active = spawnActiveStash.Has(unit.EntityProvider.Entity);
			if (!force && wasSpawnActive == active) return;
			wasSpawnActive = active;
			if (active)
			{
				visibilityAnimator.SetVisible(false);
				spawnHidden = true;
			}
			else if (spawnHidden)
			{
				spawnHidden = false;
				visibilityAnimator.SetVisible(true);
			}
		}

		private void RenderHealth()
		{
			// Native ELF VA 0x1600d90; native uses the shared StringBuilder formatter.
			previousHealth = unit.CurrentHealth;
			progress.fillAmount = (float)(unit.CurrentHealth / unit.GetStatValue(StatType.Health));
			text.SetText((System.Text.StringBuilder)unit.CurrentHealth.FormatNonAlloc());
		}

		private void RenderMarks()
        {
            // Native ELF VA 0x1601f68; no mark component is created by this read.
            if (marksRoot == null) return;
            ref MarkComponent mark = ref markStash.Get(unit.EntityProvider.Entity, out bool exists);
            int stacks = exists ? mark.Stacks : 0;
            if (stacks == previousMarks) return;
            previousMarks = stacks;
            if (stacks == 0) { marksRoot.SetActive(false); return; }
            marksRoot.SetActive(true);
            marksText.SetText(string.Format("<sprite name=atlas_110> {0}", stacks));
            marksText.color = exists && stacks >= mark.MaxStacks ? MaxStacksColor : DefaultColor;
            if (marksAnimation != null) marksAnimation.Play();
        }

        private void RenderDummyStats()
        {
            // Original ARM64 ELF VA 0x16021ac; this only renders the existing dummy telemetry.
            if (!isDummy || dummyStats == null) return;
            ref DummyTargetTag dummy = ref dummyStash.Get(unit.EntityProvider.Entity, out bool exists);
            if (!exists || !dummy.Data.IsInCombat) { dummyStats.text = string.Empty; return; }
            dummyStats.text = string.Format("DPS: {0}\nTotal: {1}\nLast: {2}",
                dummy.Data.Dps.Format(), dummy.Data.TotalDamage.Format(), dummy.Data.LastHitDamage.Format());
        }

        private void RenderDrops()
        {
            if (global::Codebase.Rpg.RpgModeConfig.Enabled && unit is EnemyUnit rpgEnemy) { RenderRpgDrops(rpgEnemy); return; }
            // Original ARM64 ELF VA 0x1600e5c. A preview never grants or multiplies rewards.
            // Pet mode: pets have no drops either; the original only returned for the player.
            if (!(unit is EnemyUnit)) return;
            var enemy = (EnemyUnit)unit;
            glow.SetActive(!string.IsNullOrWhiteSpace(enemy.Config.CurrencyReward));
            drops.text = enemy.ItemReward != null ? "<sprite name=" + enemy.ItemReward.Icon.name + ">" : string.Empty;
            var (currencyKey, currencyAmount) = GetEventCurrency(enemy);
            if (currencyAmount > 0f)
                drops.text += string.Format("{0}<sprite name={1}>", currencyAmount, eventStyleConfig.GetCurrencyStyle(currencyKey).Sprite.name);
            if (enemy is CrystalUnit crystal && crystal.BlacksmithKillReward > 0)
                drops.text += string.Format("{0}<sprite name=atlas_97>", crystal.BlacksmithKillReward);
            foreach (var reward in enemy.StatRewards)
            {
                var preview = new StatModifier { Name = reward.Name, ModifierType = reward.ModifierType,
                    Duration = reward.Duration, Source = reward.Source, Value = reward.Value, StatType = reward.StatType };
                drops.text += string.Format("{0}<sprite name={1}>", preview, iconStyleConfig.Styles.First(style => style.StatType == preview.StatType).Sprite.name);
            }
            if (enemy.HasEmptyAmulet)
                soul.text = "<size=130%><sprite name=" + currencyStyleConfig.GetIcon(enemy.Config.CurrencyReward).name + ">";
            else if (enemy.HasSoul)
                soul.text = "<sprite name=" + soulStyleConfig.GetIcon(enemy.SoulReward).name + ">";
            else soul.text = string.Empty;
            resists.text = string.Empty;
            if (enemy.Config.ArmorConfig == null) return;
            foreach (var armor in enemy.Config.ArmorConfig.Armors)
            {
                var style = iconStyleConfig.ResistStyles.First(value => value.WeaponType == armor.Type);
                double resist = armor.Resist;
                Sprite sprite = resist < -0.6 ? style.Effective.High : resist < -0.3 ? style.Effective.Medium :
                    resist < -0.1 ? style.Effective.Low : resist > 0.6 ? style.Ineffective.High :
                    resist > 0.3 ? style.Ineffective.Medium : resist > 0.1 ? style.Ineffective.Low : null;
                if (sprite != null) resists.text += "<sprite name=" + sprite.name + ">";
            }
        }

        private void RenderRpgDrops(EnemyUnit enemy)
        {
            glow.SetActive(false);
            drops.text = "Lv " + enemy.RpgLevel;
            soul.text = string.Empty;
            resists.text = string.Empty;
            if (enemy.Config.ArmorConfig == null) return;
            foreach (var armor in enemy.Config.ArmorConfig.Armors)
            {
                var style = iconStyleConfig.ResistStyles.First(value => value.WeaponType == armor.Type);
                double resist = armor.Resist;
                Sprite sprite = resist < -0.6 ? style.Effective.High : resist < -0.3 ? style.Effective.Medium :
                    resist < -0.1 ? style.Effective.Low : resist > 0.6 ? style.Ineffective.High :
                    resist > 0.3 ? style.Ineffective.Medium : resist > 0.1 ? style.Ineffective.Low : null;
                if (sprite != null) resists.text += "<sprite name=" + sprite.name + ">";
            }
        }

        private (string, float) GetEventCurrency(EnemyUnit unit)
        {
            // Original ARM64 ELF VA 0x1602448; the original matches event keys without a date gate.
            var current = eventsRepository.CurrentEvent;
            if (current != null && unit.Config.EventDef != null && unit.Config.EventDef.Key == current.Key && unit.EventUnitReward != null)
                return (unit.EventUnitReward.CurrencyKey, unit.EventUnitReward.CurrencyAmount);
            return (string.Empty, 0f);
        }
	}
}

