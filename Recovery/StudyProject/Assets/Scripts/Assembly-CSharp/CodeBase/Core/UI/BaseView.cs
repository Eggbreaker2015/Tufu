using Codebase.Localization;
using UniMob;
using UnityEngine;

namespace Codebase.Core.UI
{
	public class BaseView : LifetimeMonoBehaviour
	{
		public bool IsInitialized { get; private set; }

		public RectTransform rectTransform { get; protected set; }

		protected virtual void Awake()
		{
			// Native ELF VA 0x1548358. IsInitialized is deliberately an ordinary bool here.
			rectTransform = GetComponent<RectTransform>();
			Atom.Reaction(Lifetime, RenderWrap);
			LocalizationFontApplier.Register(gameObject);
		}

		private void RenderWrap()
		{
			// Native ELF VA 0x154847c.
			if (IsInitialized) Render();
		}

		public virtual void Initialize()
		{
			// Native ELF VA 0x1548494.
			IsInitialized = true;
		}

		public virtual void Cleanup()
		{
			// Native ELF VA 0x15484a0.
			IsInitialized = false;
		}

		protected virtual void Render()
		{
		}
	}
}
