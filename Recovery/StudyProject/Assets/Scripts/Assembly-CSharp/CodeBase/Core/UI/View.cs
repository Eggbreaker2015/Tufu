using Codebase.Localization;
using System.Runtime.CompilerServices;
using UniMob;
using UniMob.Core;
using UnityEngine;

namespace Codebase.Core.UI
{
	public class View : LifetimeMonoBehaviour
	{
		[CompilerGenerated]
		private bool __IsInitialized__BackingField;

		protected RectTransform rectTransform;

		private ComputedAtom<bool> __IsInitialized;

		public bool IsInitialized
		{
			// Original generated atom getter/setter ELF VA 0x1549f78/0x154a0a8.
			get
			{
			    LifetimeScopeExtension.ThrowIfDisposed(this);
			    if (__IsInitialized == null) __IsInitialized = CodeGenAtom.CreatePooled(this, null, () => IsInitialized, false);
			    return __IsInitialized.DirectEvaluate() ? __IsInitialized__BackingField : __IsInitialized.Value;
			}
			private set
			{
			    LifetimeScopeExtension.ThrowIfDisposed(this);
			    if (__IsInitialized != null && !__IsInitialized.CompareAndInvalidate(value)) return;
			    __IsInitialized__BackingField = value;
			}
		}

		public RectTransform RectTransform => this.rectTransform;

		protected virtual void Awake()
		{
			// Native ELF VA 0x154a128.
			rectTransform = GetComponent<RectTransform>();
			Atom.Reaction(Lifetime, RenderWrap);
			LocalizationFontApplier.Register(gameObject);
		}

		private void RenderWrap()
		{
			// Native ELF VA 0x154a24c.
			if (IsInitialized) Render();
		}

		public virtual void Initialize()
		{
			// Native ELF VA 0x154a278.
			using (Atom.NoWatch) IsInitialized = true;
		}

		public virtual void Cleanup()
		{
			// Native ELF VA 0x154a394.
			using (Atom.NoWatch) IsInitialized = false;
		}

		protected virtual void Render()
		{
		}

		private void OnDisable()
		{
			// Native ELF VA 0x154a4b4.
			if (IsInitialized) Cleanup();
		}
	}
}
