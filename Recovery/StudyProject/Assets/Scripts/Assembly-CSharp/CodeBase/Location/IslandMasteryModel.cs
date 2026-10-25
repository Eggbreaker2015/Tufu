using System;
using Codebase.Enemy;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Codebase.Core;
using Codebase.Meta;
using UniMob;
using UniMob.Core;

namespace Codebase.Location
{
	public class IslandMasteryModel : Model<IslandMasteryUd>
	{
		public const int MaxMedalLevel = 5;
		public const double BonusPerMedalLevel = 0.1;
		private readonly Queue<IslandMasteryPresentationResult> presentationQueue;
		[Inject]
		private MetaStatsModel metaStatsModel;
		[CompilerGenerated]
		private int __StateVersion__BackingField;
		[CompilerGenerated]
		private int __MaxLevelClearVersion__BackingField;
		private ComputedAtom<int> __StateVersion;
		private ComputedAtom<int> __MaxLevelClearVersion;
		public IslandMasteryResolver Resolver { get; private set; }

		public int StateVersion
		{
			get
			{
				LifetimeScopeExtension.ThrowIfDisposed(this);
				if (__StateVersion == null)
					__StateVersion = CodeGenAtom.CreatePooled(this, null, () => StateVersion, false);
				return __StateVersion.DirectEvaluate() ? __StateVersion__BackingField : __StateVersion.Value;
			}

			private set
			{
				LifetimeScopeExtension.ThrowIfDisposed(this);
				if (__StateVersion != null && !__StateVersion.CompareAndInvalidate(value))
					return;
				__StateVersion__BackingField = value;
			}
		}

		public int MaxLevelClearVersion
		{
			get
			{
				LifetimeScopeExtension.ThrowIfDisposed(this);
				if (__MaxLevelClearVersion == null)
					__MaxLevelClearVersion = CodeGenAtom.CreatePooled(this, null, () => MaxLevelClearVersion, false);
				return __MaxLevelClearVersion.DirectEvaluate() ? __MaxLevelClearVersion__BackingField : __MaxLevelClearVersion.Value;
			}

			private set
			{
				LifetimeScopeExtension.ThrowIfDisposed(this);
				if (__MaxLevelClearVersion != null && !__MaxLevelClearVersion.CompareAndInvalidate(value))
					return;
				__MaxLevelClearVersion__BackingField = value;
			}
		}

		public int MaxLevelClearLocationNumber { get; private set; }

		public bool HasPendingPresentation
		{
			get
			{
				return presentationQueue.Count > 0;
			}
		}

		internal bool IsLegacyMigrationComplete
		{
			get
			{
				return Data.LegacyMigrationComplete.Value;
			}
		}

		public IslandMasteryModel(Lifetime lifetime) : base(lifetime)
		{
			presentationQueue = new Queue<IslandMasteryPresentationResult>();
		}

		public override void Initialize()
		{
			base.Initialize();
			Resolver = new IslandMasteryResolver(GameContext.Get<EnemiesModel>());
		}

		public int GetMedalLevel(int locationNumber)
		{
			return Data.Locations.TryGetValue(locationNumber.ToString(), out IslandMasteryLocationUd location) ? Math.Clamp(location.MedalLevel.Value, 0, 5) : 0;
		}

		public double GetMedalBonus(int locationNumber)
		{
			return metaStatsModel.Get("island_medal_bonus").GetValue(locationNumber);
		}

		public bool IsFullyCleared(int locationNumber)
		{
			return Resolver.IsFullyCleared(locationNumber);
		}

		public bool TryConsumePresentation(out IslandMasteryPresentationResult result)
		{
			if (presentationQueue.Count == 0)
			{
				result = default;
				return false;
			}

			result = presentationQueue.Dequeue();
			return true;
		}

		internal IslandMasteryLocationUd GetOrCreateLocation(int locationNumber)
		{
			return Data.Locations.GetOrCreate(ToLocationKey(locationNumber), out _);
		}

		internal void SetMedalLevel(int locationNumber, int level)
		{
			var location = GetOrCreateLocation(locationNumber);
			int clamped = Math.Clamp(level, 0, MaxMedalLevel);
			location.MedalLevel.Value = clamped;
			ApplyMedalBonusModifier(locationNumber, clamped);
		}

		internal void SyncMetaStatModifiers(IReadOnlyList<int> locationNumbers)
		{
			for (int i = 0; i < locationNumbers.Count; i++)
			{
				int locationNumber = locationNumbers[i];
				ApplyMedalBonusModifier(locationNumber, GetMedalLevel(locationNumber));
			}
		}

		internal void MarkLegacyMigrationComplete()
		{
			Data.LegacyMigrationComplete.Value = true;
		}

		internal void QueuePresentation(IslandMasteryPresentationResult result)
		{
			presentationQueue.Enqueue(result);
		}

		internal void BumpStateVersion()
		{
			StateVersion++;
		}

		internal void NotifyMaxLevelClear(int locationNumber)
		{
			MaxLevelClearLocationNumber = locationNumber;
			MaxLevelClearVersion++;
		}

		private static string ToLocationKey(int locationNumber)
		{
			return locationNumber.ToString();
		}

		private void ApplyMedalBonusModifier(int locationNumber, int level)
		{
			var stat = metaStatsModel.Get("island_medal_bonus");
			string name = GetModifierName(locationNumber);
			var old = stat.Modifiers.Find(modifier => modifier.Name == name && modifier.Source == global::Codebase.Unit.ModifierSource.Reward);
			if (old != null)
				stat.RemoveModifier(old);
			if (level > 0)
				stat.AddModifier(new global::Codebase.Unit.StatModifier { Name = name, ModifierType = global::Codebase.Unit.ModifierType.Flat, Source = global::Codebase.Unit.ModifierSource.Reward, Param = locationNumber, Value = (float)((double)level * BonusPerMedalLevel) });
		}

		private static string GetModifierName(int locationNumber)
		{
			return string.Format("island_medal_{0}", locationNumber);
		}
	}
}
