using Codebase.Localization;
using System;
using System.Runtime.CompilerServices;
using UniMob;
using UniMob.Core;
using UnityEngine;

namespace Codebase.Core.UI
{
	public class PopupView : LifetimeMonoBehaviour
	{
		[CompilerGenerated]
		private bool __IsInitialized__BackingField;

		protected RectTransform rectTransform;

		private Action closeCallback;

		private Action userCloseCallback;

		private ComputedAtom<bool> __IsInitialized;

		public bool IsInitialized
		{
			// Original generated atom getter/setter ELF VA 0x15484bc/0x15485ec.
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
			// Native ELF VA 0x154866c.
			rectTransform = GetComponent<RectTransform>();
			Atom.Reaction(Lifetime, RenderWrap);
			LocalizationFontApplier.Register(gameObject);
		}

		private void RenderWrap()
		{
			// Native ELF VA 0x1548790.
			if (IsInitialized) Render();
		}

		internal void SetCloseCallback(Action callback)
		{
			// Native ELF VA 0x15487bc.
			closeCallback = callback;
		}

		public void SetUserCloseCallback(Action callback)
		{
			// Native ELF VA 0x15487c4.
			userCloseCallback = callback;
		}

		public virtual void Initialize()
		{
			// Native ELF VA 0x15487cc.
			using (Atom.NoWatch) IsInitialized = true;
		}

		public virtual void Cleanup()
		{
			// Native ELF VA 0x15488e8.
			using (Atom.NoWatch) IsInitialized = false;
			userCloseCallback = null;
		}

		protected virtual void Render()
		{
		}

		public void Close()
		{
			// Native ELF VA 0x1548a18.
			userCloseCallback?.Invoke();
			closeCallback?.Invoke();
		}

		private void OnDisable()
		{
			// Native ELF VA 0x1548a5c.
			if (IsInitialized) Cleanup();
		}
	}
}
