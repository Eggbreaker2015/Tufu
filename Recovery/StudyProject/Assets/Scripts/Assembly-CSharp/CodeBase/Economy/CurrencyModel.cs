using System.Collections.Generic;
using System.Linq;
using Codebase.Core;
using UniMob;
namespace Codebase.Economy
{
    public class CurrencyModel : Model<CurrencyUd>
    {
        // Original native methods 0x1516550, 0x15165a8, 0x1516638, 0x15148a8,
        // 0x15166c8, 0x1516778, 0x15167b0. Balances use the original saved UdDictValue.
        public CurrencyModel(Lifetime lifetime) : base(lifetime) { }
        public double Get(string key) => Data.Balances.TryGetValue(key,out double value) ? value : 0.0;
        public void Set(string key,double amount) => Data.Balances[key] = amount >= 0.0 ? amount : 0.0;
        public void Add(string key,double amount) { if (amount <= 0.0) return; Data.Balances[key] = Get(key) + amount; }
        public bool TrySpend(string key,double amount)
        {
            if (amount > 0.0) { double current = Get(key); if (current < amount) return false; Data.Balances[key] = current - amount; }
            return true;
        }
        public bool HasEnough(string key,double amount) => !(amount > 0.0) || Get(key) >= amount;
        public IReadOnlyList<string> KeysWithPrefix(string prefix) => Data.Balances.Keys.Where(key => key.StartsWith(prefix)).ToList();
    }
}
