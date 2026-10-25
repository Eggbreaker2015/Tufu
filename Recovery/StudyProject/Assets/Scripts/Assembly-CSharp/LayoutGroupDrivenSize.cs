using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

[ExecuteAlways]
public class LayoutGroupDrivenSize : UIBehaviour
{
	[SerializeField]
	private bool _multipleRoots;

	[SerializeField]
	private RectTransform _layoutRoot;

	[SerializeField]
	private RectTransform[] _layoutRoots;

	[SerializeField]
	private float _extraSize;

	private RectTransform _rectTransform;

	private bool _isDirty;

	private RectTransform.Axis _axis;

	protected override void Awake()
	{
		// Native ELF0x1379c24: deliberately no base.Awake call.
		Initialize();
	}

	protected override void OnEnable()
	{
		// Native ELF0x1379cf4.
		base.OnEnable(); _isDirty = true;
	}

	protected override void OnDisable()
	{
		// Native ELF0x1379d14.
		base.OnDisable();
	}

	private void Initialize()
	{
        // Native ELF0x1379c28.
        _rectTransform = GetComponent<RectTransform>();
        var root = _multipleRoots ? GetFirstRoot() : _layoutRoot;
        if (root != null) _axis = GetAxis(root);
	}

	private void LateUpdate()
	{
        // Native ELF0x1379e68.
        if (!_isDirty) return;
        if (_multipleRoots) UpdateFromMultipleRoots(); else UpdateFromSingleRoot();
	}

	private void UpdateFromSingleRoot()
	{
        // Native ELF0x1379f94: clear dirty before forcing root rebuild.
        if (_layoutRoot == null) return;
        _isDirty = false;
        float size = GetRootSize(_layoutRoot, _axis);
        _rectTransform.SetSizeWithCurrentAnchors(_axis, size + _extraSize);
	}

	private void UpdateFromMultipleRoots()
	{
        // Native ELF0x1379e84; sum only actual nonnull roots, not a maximum.
        if (_layoutRoots == null || _layoutRoots.Length == 0) return;
        _isDirty = false;
        float size = 0f;
        foreach (var root in _layoutRoots) if (root != null) size = size + GetRootSize(root, _axis);
        _rectTransform.SetSizeWithCurrentAnchors(_axis, size + _extraSize);
	}

	private float GetRootSize(RectTransform root, RectTransform.Axis axis)
	{
        // Native ELF0x137a034: rebuild must precede LayoutUtility's preferred dimension query.
        LayoutRebuilder.ForceRebuildLayoutImmediate(root);
        return axis == RectTransform.Axis.Horizontal ? LayoutUtility.GetPreferredWidth(root) : LayoutUtility.GetPreferredHeight(root);
	}

	private RectTransform.Axis GetAxis(RectTransform root)
	{
        // Native ELF0x1379dd0.
        return root.GetComponent<HorizontalLayoutGroup>() != null ? RectTransform.Axis.Horizontal : RectTransform.Axis.Vertical;
	}

	private RectTransform GetFirstRoot()
	{
        // Native ELF0x1379d1c.
        if (_layoutRoots != null) foreach (var root in _layoutRoots) if (root != null) return root;
        return null;
	}

	protected override void OnRectTransformDimensionsChange()
	{
		// Native ELF0x137a0b8.
		_isDirty = true;
	}

	private void OnTransformChildrenChanged()
	{
		// Native ELF0x137a0c4.
		_isDirty = true;
	}

	public void SetDirty()
	{
		// Native ELF0x137a0d0.
		_isDirty = true;
	}
}
