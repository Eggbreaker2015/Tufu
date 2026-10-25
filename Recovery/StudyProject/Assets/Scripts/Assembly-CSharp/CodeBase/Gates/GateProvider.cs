using Codebase.Gates.Components;
using Scellecs.Morpeh.Providers;
using Codebase.Map;

namespace Codebase.Gates
{
	public class GateProvider : MonoProvider<GateComponent>
	{
		private void Start()
		{
			Gate gate = GetComponent<Gate>();
			if (gate != null)
				GetData().Gate = GetComponent<Gate>();
			else
				GetComponent<PointOfInterestProvider>().enabled = false;
		}
	}
}
