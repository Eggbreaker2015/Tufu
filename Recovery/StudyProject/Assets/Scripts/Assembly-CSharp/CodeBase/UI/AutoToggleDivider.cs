using UnityEngine;
using UnityEngine.UI;

namespace Codebase.UI
{
	[ExecuteAlways]
	[RequireComponent(typeof(Image))]
	public class AutoToggleDivider : MonoBehaviour
	{
		[SerializeField]
		private bool checkBeforeToo;

		private Image _image;

		private Transform _parent;

		private void Awake()
		{
		}

		private void OnEnable()
		{
		}

		private void LateUpdate()
		{
		}

		private void OnTransformParentChanged()
		{
		}

		private void OnTransformChildrenChanged()
		{
		}

		private void UpdateState()
		{
		}

		private bool HasActiveSiblingAfter()
		{
			return false;
		}

		private bool HasActiveSiblingBefore()
		{
			return false;
		}
	}
}
