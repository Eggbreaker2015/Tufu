using Cysharp.Threading.Tasks;
using UniMob;

namespace Codebase.Integrations.TapJoy
{
	public interface IOfferwallService
	{
		Atom<bool> IsContentReady { get; }

		UniTask<string> ShowContent();
	}
}
