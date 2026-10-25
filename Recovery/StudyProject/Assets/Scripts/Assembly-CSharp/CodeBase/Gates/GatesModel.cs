using System.Collections.Generic;
using Codebase.Core;
using UniMob;

namespace Codebase.Gates
{
	public class GatesModel : Model<GatesUd>
	{
		public List<Gate> GateCores { get; }

		public GatesModel(Lifetime lifetime) : base(lifetime)
		{
			GateCores = new List<Gate>();
		}

		public bool IsGateUnlocked(string key)
		{
			GateDataUd gate = Data.Gates.Find(item => item.Key.Value == key);
			return gate != null && gate.IsUnlocked.Value;
		}

		public GateDataUd GetOrCreateGate(string key)
		{
			GateDataUd gate = Data.Gates.Find(item => item.Key.Value == key);
			if (gate != null)
				return gate;
			gate = Data.Gates.Create();
			gate.Key.Value = key;
			gate.IsUnlocked.Value = false;
			return gate;
		}

		public void OpenGate(string key)
		{
			GetOrCreateGate(key).IsUnlocked.Value = true;
		}
	}
}
