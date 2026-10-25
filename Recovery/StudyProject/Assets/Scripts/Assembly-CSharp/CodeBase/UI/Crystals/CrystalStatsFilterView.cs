using Codebase.Utilities;
using UniMob;
using System.Linq;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Codebase.Core;
using Codebase.Core.UI;
using Codebase.Crystals;
using Codebase.Unit;
using UniMob.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Codebase.UI.Crystals
{
	public class CrystalStatsFilterView : PopupView
	{
		private struct PendingFilterState
		{
			public StatType? Base;

			public StatType? Special;

			public LegendaryAbilityType? Legendary;
		}

		private readonly struct SpecialFilterRow
		{
			public StatType? Stat { get; }

			public LegendaryAbilityType? Legendary { get; }

			public bool IsLegendary => Legendary.HasValue; // Native ELF0x13b8c18.

			public SpecialFilterRow(StatType stat)
			{
            // Native ELF0x13b85b8.
                Stat = stat; Legendary = null;
        }

			public SpecialFilterRow(LegendaryAbilityType legendary)
			{
            // Native ELF0x13b8808.
                Stat = null; Legendary = legendary;
        }
		}

		[SerializeField]
		private Transform baseStatsContainer;

		[SerializeField]
		private Transform specialStatsContainer;

		[SerializeField]
		private CrystalStatFilterItemView itemPrefab;

		[SerializeField]
		private Button closeButton;

		[SerializeField]
		private Button applyButton;

		[SerializeField]
		private Button resetButton;

		[Inject]
		private CrystalModel crystalModel;

		private ViewContainer<CrystalStatFilterItemView, StatType> baseContainer;

		private ViewContainer<CrystalStatFilterItemView, SpecialFilterRow> specialSectionContainer;

		private readonly List<StatType> cachedBaseStats = new List<StatType>(); // Native ctor13b9d94.

		private readonly List<SpecialFilterRow> cachedSpecialRows = new List<SpecialFilterRow>();

		private bool filterUiSyncRegistered;

		[CompilerGenerated]
		private PendingFilterState __PendingFilter__BackingField;

		private Action<HashSet<StatType>, HashSet<StatType>, HashSet<LegendaryAbilityType>> onApply;

		private ComputedAtom<PendingFilterState> __PendingFilter;

		private PendingFilterState PendingFilter
		{
            // Native ELF0x13b57a4.
            get {
                UniMob.LifetimeScopeExtension.ThrowIfDisposed(this);
                if (__PendingFilter == null) __PendingFilter = CodeGenAtom.CreatePooled(this, null, () => PendingFilter, false);
                if (!__PendingFilter.DirectEvaluate()) return __PendingFilter.Value;
                return __PendingFilter__BackingField;
            }
            set {
                // Native ELF0x13b58e8.
                UniMob.LifetimeScopeExtension.ThrowIfDisposed(this);
                if (__PendingFilter != null && !__PendingFilter.CompareAndInvalidate(value)) return;
                __PendingFilter__BackingField = value;
            }
        }

		protected override void Awake()
		{
            // Native ELF0x13b5978.
            base.Awake(); closeButton.onClick.AddListener(Close); applyButton.onClick.AddListener(HandleApply); resetButton.onClick.AddListener(HandleReset);
        }

		public void Setup(IEnumerable<StatType> currentBaseFilter, IEnumerable<StatType> currentSpecialFilter, IEnumerable<LegendaryAbilityType> currentLegendaryFilter, Action<HashSet<StatType>, HashSet<StatType>, HashSet<LegendaryAbilityType>> onApply)
		{
            // Native ELF0x13aec64.
            var filter = new PendingFilterState();
            if (currentBaseFilter != null) foreach (var value in currentBaseFilter) filter.Base = value;
            if (currentSpecialFilter != null) foreach (var value in currentSpecialFilter) filter.Special = value;
            if (currentLegendaryFilter != null) foreach (var value in currentLegendaryFilter) filter.Legendary = value;
            this.onApply = onApply;
            using (Atom.NoWatch) PendingFilter = SanitizePendingFilter(filter);
            Initialize();
        }

		public override void Initialize()
		{
            // Native ELF0x13b5c9c.
            base.Initialize();
            if (baseContainer == null) {
                using (var controller = new LifetimeController()) {
                    baseContainer = new ViewContainer<CrystalStatFilterItemView, StatType>(baseStatsContainer, itemPrefab)
                        .BindTo(controller.Lifetime, () => ComputeBaseStats(PendingFilter)).WatchFor(() => PendingFilter)
                        .Setup((view, stat) => view.Setup(stat, () => ToggleBase(stat)));
                    specialSectionContainer = new ViewContainer<CrystalStatFilterItemView, SpecialFilterRow>(specialStatsContainer, itemPrefab)
                        .BindTo(controller.Lifetime, () => ComputeSpecialSectionRows(PendingFilter)).WatchFor(() => PendingFilter).WithEqualityCheck(AreSpecialFilterRowsEqual)
                        .Setup((view, row) => {
                            if (row.IsLegendary) { var ability = row.Legendary.Value; view.Setup(crystalModel.Config.GetLegendaryAbilityIcon(ability), GetLegendaryAbilityFilterName(ability), ability, () => ToggleLegendary(ability)); }
                            else { var stat = row.Stat.Value; view.Setup(stat, () => ToggleSpecial(stat)); }
                        });
                }
            } else {
                cachedBaseStats.Clear(); cachedSpecialRows.Clear(); baseContainer.ForceRebuild(); specialSectionContainer.ForceRebuild();
                using (Atom.NoWatch) ApplyCheckmarks(PendingFilter);
            }
            if (!filterUiSyncRegistered) { filterUiSyncRegistered = true; Atom.Reaction(Lifetime, SyncFilterUi); }
        }

		private void SyncFilterUi()
		{
            // Native ELF0x13b6c80.
            var filter = PendingFilter; List<StatType> baseStats; List<SpecialFilterRow> specialRows;
            using (Atom.NoWatch) { baseStats = ComputeBaseStats(filter); specialRows = ComputeSpecialSectionRows(filter); }
            bool sameBase = AreBaseStatsSame(cachedBaseStats, baseStats), sameSpecial = AreSpecialRowsSame(cachedSpecialRows, specialRows);
            if (!sameBase) { CopyBaseStats(cachedBaseStats, baseStats); baseContainer.ForceRebuild(); }
            if (!sameSpecial) { CopySpecialRows(cachedSpecialRows, specialRows); specialSectionContainer.ForceRebuild(); }
            using (Atom.NoWatch) ApplyCheckmarks(filter);
        }

		private void SetPendingFilter(PendingFilterState filter)
		{
            // Native ELF0x13b7c34.
            PendingFilter = filter;
        }

		private void ApplyCheckmarks(PendingFilterState filter)
		{
            // Native ELF0x13b63dc.
            foreach (var view in baseContainer.Active) view.SetChecked(view.FilterStat.HasValue && filter.Base.HasValue && view.FilterStat.Value == filter.Base.Value);
            foreach (var view in specialSectionContainer.Active) {
                bool selected = view.FilterLegendary.HasValue ? filter.Legendary.HasValue && view.FilterLegendary.Value == filter.Legendary.Value : view.FilterStat.HasValue && filter.Special.HasValue && view.FilterStat.Value == filter.Special.Value;
                view.SetChecked(selected);
            }
        }

		private List<StatType> ComputeBaseStats(PendingFilterState filter)
		{
            // Native ELF0x13b6f68.
            var pool = GetBasePoolSet(); var result = new List<StatType>();
            using (Atom.NoWatch) foreach (var stat in pool) if (HasUnequippedCrystalWithBaseStat(stat, filter)) result.Add(stat);
            return result;
        }

		private List<SpecialFilterRow> ComputeSpecialSectionRows(PendingFilterState filter)
		{
            // Native ELF0x13b72b4.
            var result = new List<SpecialFilterRow>();
            using (Atom.NoWatch) {
                foreach (var stat in GetSpecialPoolSet()) if (HasUnequippedCrystalWithSpecialStat(stat, filter)) result.Add(new SpecialFilterRow(stat));
                var seen = new HashSet<LegendaryAbilityType>();
                foreach (var c in crystalModel.Crystals) {
                    if (c.IsEquipped || !CrystalMatchesPendingBase(c, filter) || !CrystalMatchesPendingSpecial(c, filter) || !c.HasLegendaryAbility || !seen.Add(c.LegendaryAbility)) continue;
                    result.Add(new SpecialFilterRow(c.LegendaryAbility));
                }
            }
            return result;
        }

		private bool AreBaseStatsSame(List<StatType> left, List<StatType> right)
		{
            // Native ELF0x13b78ec.
            if (left.Count != right.Count) return false;
            for (int i = 0; i < left.Count; i++) if (left[i] != right[i]) return false;
            return true;
        }

		private bool AreSpecialRowsSame(List<SpecialFilterRow> left, List<SpecialFilterRow> right)
		{
            // Native ELF0x13b79d0.
            if (left.Count != right.Count) return false;
            for (int i = 0; i < left.Count; i++) if (!AreSpecialFilterRowsEqual(left[i], right[i])) return false;
            return true;
        }

		private void CopyBaseStats(List<StatType> target, List<StatType> source)
		{
            // Native ELF0x13b7ac4.
            target.Clear(); foreach (var value in source) target.Add(value);
        }

		private void CopySpecialRows(List<SpecialFilterRow> target, List<SpecialFilterRow> source)
		{
            // Native ELF0x13b7b7c.
            target.Clear(); foreach (var value in source) target.Add(value);
        }

		private bool AreSpecialFilterRowsEqual(SpecialFilterRow left, SpecialFilterRow right)
		{
            // Native ELF0x13b887c.
            return left.IsLegendary == right.IsLegendary && (left.IsLegendary ? left.Legendary == right.Legendary : left.Stat == right.Stat);
        }

		private HashSet<StatType> GetBasePoolSet()
		{
            // Native ELF0x13b7c60.
            var pool = new HashSet<StatType>(crystalModel.Config.BaseStatPool); foreach (var stat in crystalModel.Config.MagicStatPool) pool.Add(stat); return pool;
        }

		private HashSet<StatType> GetSpecialPoolSet()
		{
            // Native ELF0x13b811c.
            var pool = new HashSet<StatType>(); foreach (var def in crystalModel.Config.SpecialStats) pool.Add(def.StatType); return pool;
        }

		private bool HasUnequippedCrystalWithBaseStat(StatType stat, PendingFilterState filter)
		{
            // Native ELF0x13b7ea0.
            foreach (var c in crystalModel.Crystals)
                if (!c.IsEquipped && CrystalMatchesPendingSpecial(c, filter) && CrystalMatchesPendingLegendary(c, filter) && CrystalHasBaseStat(c, stat)) return true;
            return false;
        }

		private bool HasUnequippedCrystalWithSpecialStat(StatType stat, PendingFilterState filter)
		{
            // Native ELF0x13b8354.
            foreach (var c in crystalModel.Crystals)
                if (!c.IsEquipped && CrystalMatchesPendingBase(c, filter) && CrystalMatchesPendingLegendary(c, filter) && c.HasSpecialStat && c.SpecialStatType == stat) return true;
            return false;
        }

		private bool CrystalMatchesPendingBase(CrystalItemModel crystal, PendingFilterState filter)
		{
            // Native ELF0x13b8628.
            return !filter.Base.HasValue || CrystalHasBaseStat(crystal, filter.Base.Value);
        }

		private bool CrystalMatchesPendingSpecial(CrystalItemModel crystal, PendingFilterState filter)
		{
            // Native ELF0x13b8720.
            return !filter.Special.HasValue || (crystal.HasSpecialStat && crystal.SpecialStatType == filter.Special.Value);
        }

		private bool CrystalMatchesPendingLegendary(CrystalItemModel crystal, PendingFilterState filter)
		{
            // Native ELF0x13b8c94.
            return !filter.Legendary.HasValue || (crystal.HasLegendaryAbility && crystal.LegendaryAbility == filter.Legendary.Value);
        }

		private bool ExistsUnequippedCrystalWith(StatType? baseStat, StatType? specialStat, LegendaryAbilityType? legendary)
		{
            // Native ELF0x13b8d7c.
            foreach (var c in crystalModel.Crystals) {
                if (c.IsEquipped || (baseStat.HasValue && !CrystalHasBaseStat(c, baseStat.Value))) continue;
                if (specialStat.HasValue && (!c.HasSpecialStat || c.SpecialStatType != specialStat.Value)) continue;
                if (legendary.HasValue && (!c.HasLegendaryAbility || c.LegendaryAbility != legendary.Value)) continue;
                return true;
            }
            return false;
        }

		private bool CrystalHasBaseStat(CrystalItemModel crystal, StatType stat)
		{
            // Native ELF0x13b9234.
            return crystal.BaseStatType == stat || (crystal.HasSecondaryStat && crystal.SecondaryStatType == stat);
        }

		private PendingFilterState SanitizePendingFilter(PendingFilterState filter, bool skipBase = false, bool skipSpecial = false, bool skipLegendary = false)
		{
            // Native ELF0x13b5abc.
            if (!skipSpecial && filter.Base.HasValue && filter.Special.HasValue && !ExistsUnequippedCrystalWith(filter.Base, filter.Special, null)) filter.Special = null;
            if (!skipBase && filter.Base.HasValue && filter.Special.HasValue && !ExistsUnequippedCrystalWith(filter.Base, filter.Special, null)) filter.Base = null;
            if (!skipLegendary && filter.Legendary.HasValue && !ExistsUnequippedCrystalWith(filter.Base, filter.Special, filter.Legendary)) filter.Legendary = null;
            return filter;
        }

		private void ToggleBase(StatType stat)
		{
            // Native ELF0x13b92a0.
            var filter = PendingFilter; filter.Base = filter.Base.HasValue && filter.Base.Value == stat ? (StatType?)null : stat;
            PendingFilter = SanitizePendingFilter(filter, true, false, false);
        }

		private void ToggleSpecial(StatType stat)
		{
            // Native ELF0x13b944c.
            var filter = PendingFilter; filter.Special = filter.Special.HasValue && filter.Special.Value == stat ? (StatType?)null : stat;
            PendingFilter = SanitizePendingFilter(filter, false, true, false);
        }

		private void ToggleLegendary(LegendaryAbilityType ability)
		{
            // Native ELF0x13b95f8.
            var filter = PendingFilter; filter.Legendary = filter.Legendary.HasValue && filter.Legendary.Value == ability ? (LegendaryAbilityType?)null : ability;
            PendingFilter = SanitizePendingFilter(filter, false, false, true);
        }

		private void HandleApply()
		{
            // Native ELF0x13b97a4.
            var filter = PendingFilter; var bases = new HashSet<StatType>(); var special = new HashSet<StatType>(); var legendary = new HashSet<LegendaryAbilityType>();
            if (filter.Base.HasValue) bases.Add(filter.Base.Value); if (filter.Special.HasValue) special.Add(filter.Special.Value); if (filter.Legendary.HasValue) legendary.Add(filter.Legendary.Value);
            onApply?.Invoke(bases, special, legendary); Close();
        }

		private void HandleReset()
		{
            // Native ELF0x13b9a64.
            PendingFilter = default; onApply?.Invoke(new HashSet<StatType>(), new HashSet<StatType>(), new HashSet<LegendaryAbilityType>()); Close();
        }

		public override void Cleanup()
		{
            // Native ELF0x13b9b74.
            base.Cleanup(); onApply = null;
        }

		private static string GetLegendaryAbilityFilterName(LegendaryAbilityType ability)
		{
            // Native ELF0x13b9b98.
            string text = LocalizationStrings.Get(string.Format("CRYSTALS_LEGENDARY_{0}", ability)); int at = text.IndexOf("{value}", StringComparison.Ordinal);
            if (at < 0) return text;
            string before = text.Substring(0, at).TrimEnd(' ', '+', '-'), after = text.Substring(at + 7).TrimStart('%', ' ');
            if (before.Length > 0 && after.Length > 0) return before + " " + after;
            return before.Length > 0 ? before : after;
        }
	}
}
