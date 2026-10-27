using Codebase.Core;
using UniMob;
using UniMob.Core;

namespace Codebase.SecretChest
{
    public class SecretChestModel : Model<SecretChestsUd>
    {
        private int totalCount;
        private ComputedAtom<int> totalCountAtom;

        // APK ARM64 0x1410f54 / 0x141107c: the total is a reactive scene count.
        public int TotalCount
        {
            get
            {
                LifetimeScopeExtension.ThrowIfDisposed(this);
                if (totalCountAtom == null)
                    totalCountAtom = CodeGenAtom.CreatePooled(this, null, () => TotalCount, false);
                return totalCountAtom.DirectEvaluate() ? totalCount : totalCountAtom.Value;
            }
            private set
            {
                LifetimeScopeExtension.ThrowIfDisposed(this);
                if (totalCountAtom != null && !totalCountAtom.CompareAndInvalidate(value)) return;
                totalCount = value;
            }
        }

        // APK 0x141114c: count saved opened entries, including unloaded islands.
        public int OpenedCount
        {
            get
            {
                int count = 0;
                foreach (SecretChestDataUd chest in Data.Chests)
                    if (chest.IsOpened.Value) count++;
                return count;
            }
        }

        public SecretChestModel(Lifetime lifetime) : base(lifetime) { }

        // APK 0x140f480: querying an unknown key must not create save entries.
        public bool IsOpened(string chestKey)
        {
            return !string.IsNullOrEmpty(chestKey) &&
                Data.Chests.TryGetValue(chestKey, out var chest) && chest.IsOpened.Value;
        }

        public void MarkOpened(string chestKey)
        {
            // APK 0x1411348.
            Data.Chests.GetOrCreate(chestKey, out _).IsOpened.Value = true;
        }

        public void SetTotalCount(int count)
        {
            // APK 0x14113fc.
            if (TotalCount != count) TotalCount = count;
        }
    }
}
