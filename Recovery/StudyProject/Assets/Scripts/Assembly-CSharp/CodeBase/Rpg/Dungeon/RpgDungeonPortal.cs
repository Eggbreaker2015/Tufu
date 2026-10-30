using Codebase.Gates;
using Codebase.UI;
using TMPro;
using UnityEngine;

namespace Codebase.Rpg.Dungeon
{
	// Runtime-only portal: a copy of the Boss Tower portal's base mesh and vortex (scripts stripped) plus a world label.
	public class RpgDungeonPortal : MonoBehaviour
	{
		private TextMeshPro label;

		public static RpgDungeonPortal Create(string name, Vector3 position, float yaw)
		{
			GameObject root = new GameObject(name);
			root.SetActive(false);
			root.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, yaw, 0f));
			Portal template = Object.FindFirstObjectByType<Portal>(FindObjectsInactive.Include);
			bool copied = false;
			if (template != null)
				foreach (string child in new[] { "Portal", "VortexPortalBlue" })
					copied |= CopyVisual(template.transform, child, root.transform);
			if (!copied)
			{
				GameObject disc = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
				Object.Destroy(disc.GetComponent<Collider>());
				disc.transform.SetParent(root.transform, false);
				disc.transform.localScale = new Vector3(3f, 0.05f, 3f);
				disc.GetComponent<Renderer>().material.color = new Color(0.3f, 0.6f, 1f);
			}
			RpgDungeonPortal portal = root.AddComponent<RpgDungeonPortal>();
			portal.CreateLabel();
			root.SetActive(true);
			return portal;
		}

		private static bool CopyVisual(Transform template, string childName, Transform parent)
		{
			Transform source = template.Find(childName);
			if (source == null) return false;
			// The parent is inactive, so no Awake runs before the ECS providers are stripped.
			GameObject copy = Object.Instantiate(source.gameObject, parent);
			foreach (MonoBehaviour behaviour in copy.GetComponentsInChildren<MonoBehaviour>(true))
				Object.DestroyImmediate(behaviour);
			copy.transform.localPosition = source.position - template.position;
			copy.transform.localRotation = Quaternion.Inverse(template.rotation) * source.rotation;
			copy.transform.localScale = source.lossyScale;
			copy.SetActive(true);
			return true;
		}

		private void CreateLabel()
		{
			GameObject go = new GameObject("Label");
			go.transform.SetParent(transform, false);
			go.transform.localPosition = new Vector3(0f, 8f, 0f);
			label = go.AddComponent<TextMeshPro>();
			if (TMP_Settings.defaultFontAsset != null) label.font = TMP_Settings.defaultFontAsset;
			label.alignment = TextAlignmentOptions.Center;
			label.fontSize = 9f;
			label.textWrappingMode = TextWrappingModes.NoWrap;
			label.rectTransform.sizeDelta = new Vector2(20f, 4f);
			label.outlineWidth = 0.2f;
			label.outlineColor = Color.black;
		}

		public void SetLabel(string text, Color color)
		{
			if (label == null) return;
			PetStatsText.SetText(label, text);
			label.color = color;
		}

		private void LateUpdate()
		{
			UnityEngine.Camera camera = UnityEngine.Camera.main;
			if (label != null && camera != null) label.transform.rotation = camera.transform.rotation;
		}
	}
}
