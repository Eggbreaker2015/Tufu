using Lofelt.NiceVibrations;
using Scellecs.Morpeh;

namespace Codebase.Vibration.Events
{
	public struct HapticRequest : IRequestData
	{
		public HapticPatterns.PresetType preset;
	}
}
