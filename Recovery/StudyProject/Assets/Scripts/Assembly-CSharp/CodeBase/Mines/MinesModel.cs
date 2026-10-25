using Codebase.Core;
using UniMob;

namespace Codebase.Mines
{
	public class MinesModel : Model<MinesUd>
	{
		public bool IsUnlocked
		{
			get
			{ // Original ELF0x14854c4.
				return Data.IsUnlocked.Value;
			}
		}

		public MinesModel(Lifetime lifetime) : base(lifetime)
		{ // Original ELF0x148546c: forward actual lifetime.
		}

		public void Unlock()
		{ // Original ELF0x1485534.
			Data.IsUnlocked.Value = true;
		}
	}
}
