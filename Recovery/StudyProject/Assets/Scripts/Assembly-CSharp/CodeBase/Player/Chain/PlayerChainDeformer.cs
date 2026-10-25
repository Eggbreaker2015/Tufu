using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Codebase.Player.Chain
{
	public class PlayerChainDeformer : MonoBehaviour
	{
		private const float Epsilon = 0.0001f;
		private const float Tau = (float)Math.PI * 2f;
		private const int MaxProceduralLinks = 512;
		private Transform startSpace;
		private Vector3 startLocalPosition;
		private Transform endAnchor;
		[SerializeField]
		private Transform bonesParent;
		[Tooltip("Weapon chains live under animated spine bones, so on Awake the deformer detaches to world space and follows its original parent manually. A dynamically spawned/destroyed chain (e.g. the devour grab tentacle) must NOT detach — otherwise it leaks when its owner is destroyed. Leave true for weapon slots, false for spawned tentacles.")]
		[SerializeField]
		private bool detachOnAwake;
		private Transform startBone;
		private Transform endBone;
		[SerializeField]
		private Transform[] bones;
		[SerializeField]
		private AnimationCurve middleWeight;
		[SerializeField]
		[Min(0.1f)]
		private float baseLengthMultiplier;
		[Tooltip("Chain length tracks the current reach distance times this factor (slight slack), instead of a fixed baked length. >1 keeps a gentle arc; keeps links from over-drooping at short reach or snapping taut at long reach.")]
		[SerializeField]
		[Min(1f)]
		private float reachSlackFactor;
		[SerializeField]
		[Min(0f)]
		private float rotationDamping;
		[SerializeField]
		[Min(0f)]
		private float bendDirectionLag;
		[SerializeField]
		[Range(-180f, 180f)]
		private float bendYaw;
		[SerializeField]
		private ChainAxisDirection linkTopDirection;
		[SerializeField]
		[Min(1f)]
		private int bendCount;
		[SerializeField]
		[Min(0.2f)]
		private float bendSharpness;
		[SerializeField]
		private ChainDanceMode danceMode;
		[SerializeField]
		private ChainDanceFormula danceFormula;
		[SerializeField]
		[Min(0f)]
		private float danceAmplitude;
		[SerializeField]
		[Min(0f)]
		private float danceFrequency;
		[SerializeField]
		[Min(0f)]
		private float danceSpeed;
		[SerializeField]
		private Vector3 linkBoxSize;
		[SerializeField]
		private Mesh linkMesh;
		[SerializeField]
		private Material linkMaterial;
		[SerializeField]
		[Min(0.02f)]
		private float linkSpacing;
		[SerializeField]
		private Vector3 linkLocalScale;
		[Tooltip("Particle-system style modifier: the curve value at each link's position along the chain is ADDED as a percent to the base scale (0 = unchanged, 0.5 = +50%, -0.5 = half). X axis: 0 = base link at the player, 1 = link at the weapon/hook. Default flat 0 keeps every link identical.")]
		[SerializeField]
		private AnimationCurve linkScaleOverChain;
		[Tooltip("Same as the scale curve, but modifies the spacing to the NEXT link (0 = unchanged, 0.5 = +50% gap, -0.5 = half gap).")]
		[SerializeField]
		private AnimationCurve linkSpacingOverChain;
		[SerializeField]
		private ChainAxisDirection linkForwardAxis;
		[SerializeField]
		private bool alternateLinkRoll;
		[SerializeField]
		private bool linkOutline;
		[SerializeField]
		[Min(0f)]
		private float linkOutlineWidth;
		[Tooltip("Albedo tint for the links. Multiplies the shared material's chain texture, so the original two-tone + gradient across each link is preserved. HDR: push above 1 to brighten without flattening the texture variation.")]
		[SerializeField]
		[ColorUsage(false, true)]
		private Color linkTint;
		[Tooltip("Toon shadow colour for the links. The shared material's shadow is a dark blue that reads muddy/dark on the chain; a neutral light grey keeps the links bright while still shading them.")]
		[SerializeField]
		private Color linkShadowColor;
		[Tooltip("Re-read ALL procedural link settings (mesh, spacing, scale, curves, roll, axis, colors, outline) every frame so they can be tuned live in play mode. Keep OFF outside tuning sessions — everything is then captured once on Awake.")]
		[SerializeField]
		private bool liveSettingsSync;
		private SkinnedMeshRenderer skinnedRenderer;
		private Material runtimeLinkMaterial;
		private Material runtimeLinkSource;
		private Mesh cachedLinkMesh;
		private float cachedLinkSpacing;
		private Vector3 cachedLinkLocalScale;
		private AnimationCurve cachedLinkScaleOverChain;
		private AnimationCurve cachedLinkSpacingOverChain;
		private bool cachedAlternateLinkRoll;
		private Vector3 cachedLinkAxis;
		private float phaseOffset;
		private Vector3[] targetPositions;
		private Vector3[] gizmoPositions;
		private float[] segmentLengths;
		private float baseLength;
		private bool isIdle;
		private Vector3 currentBendDirection;
		private Vector3 cachedTopAxis;
		private float cachedSlackDistance;
		private float cachedSlackSlack;
		private float cachedSlackAmplitude;
		private float[] middleWeights;
		private float[] edgeDanceWeights;
		private float danceBlend;
		private const float DanceBlendSpeed = 4f;
		private Transform originalParent;
		private Vector3 originalLocalPosition;
		private Quaternion originalLocalRotation;
		private Vector3 originalLocalScale;
		private bool frozen;
		private bool? danceOverride;
		public Vector3 ScaleMultiplier { get; set; }
		public float LengthMultiplier { get; set; }

		private bool UsesProceduralLinks
		{
			get
			{
				return cachedLinkMesh;
			}
		}

		public Vector3 GetStartWorldPosition()
		{
			return startSpace.TransformPoint(startLocalPosition);
		}

		public void SetFrozen(bool value)
		{
			this.frozen = value;
		}

		public void SetEndAnchor(Transform value)
		{
			endAnchor = value;
		}

		public void SetReachSlack(float factor)
		{
			reachSlackFactor = factor < 1 ? 1 : factor;
		}

		public void SetDanceOverride(bool? value)
		{
			danceOverride = value;
		}

		public void SetHiddenRenderer(SkinnedMeshRenderer renderer)
		{
			skinnedRenderer = renderer;
		}

		public void SetupFromLegacy(Transform start, Transform end)
		{
			startBone = start;
			endBone = end;
			CaptureStartLocalPosition();
			CacheChainData();
		}

		public void SetIdleState(bool value)
		{
			this.isIdle = value;
		}

		private void CacheLinkSettings()
		{
			cachedLinkMesh = linkMesh;
			cachedLinkLocalScale = linkLocalScale;
			cachedLinkSpacing = Mathf.Max(0.02f, linkSpacing);
			cachedLinkScaleOverChain = new AnimationCurve(linkScaleOverChain.keys);
			cachedLinkSpacingOverChain = new AnimationCurve(linkSpacingOverChain.keys);
			cachedAlternateLinkRoll = alternateLinkRoll;
			cachedLinkAxis = AxisToVector(linkForwardAxis);
		}

		private void Awake()
		{
			phaseOffset = UnityEngine.Random.value * Tau;
			CacheChainData();
			CacheLinkSettings();
			skinnedRenderer = GetComponentInChildren<SkinnedMeshRenderer>(true);
			if (!linkMaterial && skinnedRenderer)
				linkMaterial = skinnedRenderer.sharedMaterial;
			if (!detachOnAwake)
				return;
			originalParent = transform.parent;
			originalLocalPosition = transform.localPosition;
			originalLocalRotation = transform.localRotation;
			originalLocalScale = transform.lossyScale;
			transform.SetParent(null, false);
			transform.localScale = originalLocalScale;
		}

		private void OnValidate()
		{
			if (!Application.isPlaying)
				CacheChainData();
		}

		private void OnDestroy()
		{
			if (runtimeLinkMaterial)
				Destroy(runtimeLinkMaterial);
		}

		private void RefillBones()
		{
			if (bonesParent == null)
				return;
			var result = new List<Transform>();
			CollectChildrenRecursive(bonesParent, result);
			if (result.Count < 2)
				return;
			bones = result.ToArray();
			startBone = bones[0];
			endBone = bones[bones.Length - 1];
			CaptureStartLocalPosition();
			CacheChainData();
		}

		private void LateUpdate()
		{
			if (originalParent != null)
			{
				transform.position = originalParent.TransformPoint(originalLocalPosition);
				transform.rotation = originalParent.rotation * originalLocalRotation;
				transform.localScale = Vector3.Scale(originalLocalScale, ScaleMultiplier);
			}

			if (UsesProceduralLinks && skinnedRenderer && skinnedRenderer.enabled)
				skinnedRenderer.enabled = false;
			if (bones.Length < 2 || endAnchor == null || frozen)
				return;
			float deltaTime = Time.deltaTime;
			Vector3 start = startSpace != null ? startSpace.TransformPoint(startLocalPosition) : startBone != null ? startBone.position : bones[0].position;
			Vector3 end = endAnchor.position;
			Vector3 difference = end - start;
			Vector3 forward = difference.magnitude > Epsilon ? difference.normalized : transform.forward;
			Vector3 bend = GetBendVector(forward);
			if (currentBendDirection.sqrMagnitude <= Epsilon)
				currentBendDirection = bend;
			else
			{
				float follow = bendDirectionLag > Epsilon ? 1f - Mathf.Exp(-deltaTime / bendDirectionLag) : 1f;
				currentBendDirection = Vector3.Slerp(currentBendDirection, bend, follow);
				if (currentBendDirection.sqrMagnitude <= Epsilon)
					currentBendDirection = bend;
			}

			bend = currentBendDirection.normalized;
			Vector3 side = Vector3.Cross(forward, bend).normalized;
			bool dancing = danceOverride ?? (danceMode == ChainDanceMode.Always || danceMode == ChainDanceMode.IdleOnly && isIdle);
			danceBlend = Mathf.MoveTowards(danceBlend, dancing ? 1f : 0f, deltaTime * DanceBlendSpeed);
			FillPositions(targetPositions, start, end, bend, side, Time.time, danceBlend);
			float rotationBlend = rotationDamping > Epsilon ? 1f - Mathf.Exp(-deltaTime * rotationDamping) : 1f;
			for (int i = 0; i < bones.Length; i++)
			{
				Transform bone = bones[i];
				Quaternion current = bone.rotation;
				Vector3 direction = i < bones.Length - 1 ? targetPositions[i + 1] - targetPositions[i] : targetPositions[i] - targetPositions[i - 1];
				if (direction.sqrMagnitude <= Epsilon)
				{
					bone.position = targetPositions[i];
					continue;
				}

				Vector3 top = current * cachedTopAxis;
				if (top.sqrMagnitude <= Epsilon)
				{
					bone.position = targetPositions[i];
					continue;
				}

				Quaternion rotation = Quaternion.FromToRotation(top.normalized, direction.normalized) * current;
				rotation = Quaternion.Slerp(current, rotation, rotationBlend);
				bone.SetPositionAndRotation(targetPositions[i], rotation);
			}

			if (UsesProceduralLinks)
				RenderProceduralLinks();
		}

		private void RenderProceduralLinks()
		{
			if (liveSettingsSync)
				CacheLinkSettings();
			Vector3[] curve = targetPositions;
			if (curve.Length < 2 || Vector3.Distance(curve[0], curve[curve.Length - 1]) <= Epsilon)
				return;
			Material source = linkMaterial ? linkMaterial : skinnedRenderer ? skinnedRenderer.sharedMaterial : null;
			if (!source)
				return;
			if (!runtimeLinkMaterial || runtimeLinkSource != source)
			{
				if (runtimeLinkMaterial)
					Destroy(runtimeLinkMaterial);
				runtimeLinkMaterial = new Material(source);
				runtimeLinkSource = source;
				ApplyLinkMaterialProps(runtimeLinkMaterial);
			}
			else if (liveSettingsSync)
				ApplyLinkMaterialProps(runtimeLinkMaterial);
			float length = 0f;
			for (int i = 1; i < curve.Length; i++)
				length += Vector3.Distance(curve[i - 1], curve[i]);
			Vector3 worldScale = transform.lossyScale;
			worldScale = new Vector3(Mathf.Abs(worldScale.x), Mathf.Abs(worldScale.y), Mathf.Abs(worldScale.z));
			Vector3 baseScale = Vector3.Scale(cachedLinkLocalScale, worldScale);
			float distance = 0f;
			int beyondEnd = 0;
			for (int index = 0; index < MaxProceduralLinks; index++)
			{
				float t = length > Epsilon ? 1f - Mathf.Clamp01(distance / length) : 1f;
				float spacing = cachedLinkSpacing * Mathf.Max(0.05f, 1f + cachedLinkSpacingOverChain.Evaluate(t));
				Vector3 position = SampleFromEnd(curve, distance);
				Vector3 direction = SampleFromEnd(curve, Mathf.Max(0f, distance - spacing * 0.5f)) - position;
				if (direction.sqrMagnitude <= Epsilon)
					direction = curve[curve.Length - 1] - curve[0];
				float square = direction.sqrMagnitude;
				direction.Normalize();
				Quaternion rotation = square <= Epsilon ? Quaternion.identity : Quaternion.FromToRotation(cachedLinkAxis, direction);
				if ((index & 1) != 0 && cachedAlternateLinkRoll)
					rotation = Quaternion.AngleAxis(90f, direction) * rotation;
				float scale = Mathf.Max(0.01f, 1f + cachedLinkScaleOverChain.Evaluate(t));
				Matrix4x4 matrix = Matrix4x4.TRS(position, rotation, baseScale * scale);
				Graphics.DrawMesh(cachedLinkMesh, matrix, runtimeLinkMaterial, gameObject.layer, null, 0, null, ShadowCastingMode.On, true, transform, LightProbeUsage.BlendProbes, null);
				distance += spacing;
				if (distance > length)
					beyondEnd++;
				if (beyondEnd > 1)
					break;
			}
		}

		private void ApplyLinkMaterialProps(Material mat)
		{
			if (mat.HasProperty("_UseOutline"))
				mat.SetFloat("_UseOutline", linkOutline ? 1f : 0f);
			if (linkOutline && mat.HasProperty("_OutlineWidth"))
				mat.SetFloat("_OutlineWidth", linkOutlineWidth);
			if (mat.HasProperty("_BaseColor"))
				mat.SetColor("_BaseColor", linkTint);
			if (mat.HasProperty("_Color"))
				mat.SetColor("_Color", linkTint);
			if (mat.HasProperty("_SColor"))
				mat.SetColor("_SColor", linkShadowColor);
		}

		private Vector3 SampleFromEnd(Vector3[] curve, float dist)
		{
			if (dist <= 0f)
				return curve[curve.Length - 1];
			float travelled = 0f;
			for (int i = curve.Length - 1; i >= 1; i--)
			{
				float length = Vector3.Distance(curve[i], curve[i - 1]);
				float next = travelled + length;
				if (next >= dist)
				{
					float t = length > Epsilon ? (dist - travelled) / length : 0f;
					return Vector3.Lerp(curve[i], curve[i - 1], t);
				}

				travelled = next;
			}

			return curve[0];
		}

		private void CacheChainData()
		{
			TryAutoCollectBones();
			CaptureStartLocalPosition();
			if (bones.Length < 2)
			{
				baseLength = 0f;
				targetPositions = Array.Empty<Vector3>();
				gizmoPositions = Array.Empty<Vector3>();
				segmentLengths = Array.Empty<float>();
				middleWeights = Array.Empty<float>();
				edgeDanceWeights = Array.Empty<float>();
				return;
			}

			targetPositions = new Vector3[bones.Length];
			gizmoPositions = new Vector3[bones.Length];
			segmentLengths = new float[bones.Length - 1];
			baseLength = 0f;
			for (int i = 1; i < bones.Length; i++)
			{
				segmentLengths[i - 1] = Vector3.Distance(bones[i - 1].position, bones[i].position);
				baseLength += segmentLengths[i - 1];
			}

			baseLength *= baseLengthMultiplier;
			cachedTopAxis = AxisToVector(linkTopDirection);
			middleWeights = new float[bones.Length];
			edgeDanceWeights = new float[bones.Length];
			for (int i = 0; i < bones.Length; i++)
			{
				middleWeights[i] = Mathf.Clamp01(middleWeight.Evaluate((float)i / (bones.Length - 1)));
				edgeDanceWeights[i] = GetEdgeDanceWeight(i, bones.Length);
			}
		}

		private void CaptureStartLocalPosition()
		{
			if (startBone == null || startBone.parent == null)
				return;
			startSpace = startBone.parent;
			startLocalPosition = startSpace.InverseTransformPoint(startBone.position);
		}

		private void TryAutoCollectBones()
		{
			if (bones.Length >= 2)
			{
				startBone = bones[0];
				endBone = bones[bones.Length - 1];
				return;
			}

			if (bonesParent != null)
			{
				var collected = new List<Transform>();
				CollectChildrenRecursive(bonesParent, collected);
				if (collected.Count >= 2)
				{
					bones = collected.ToArray();
					startBone = bones[0];
					endBone = bones[bones.Length - 1];
					return;
				}
			}

			if (startBone == null || endBone == null)
				return;
			var chain = new List<Transform>();
			Transform current = endBone;
			while (current != null)
			{
				chain.Add(current);
				if (current == startBone)
					break;
				current = current.parent;
			}

			if (chain.Count < 2)
				return;
			chain.Reverse();
			bones = chain.ToArray();
		}

		private void CollectChildrenRecursive(Transform parent, List<Transform> result)
		{
			for (int i = 0; i < parent.childCount; i++)
			{
				Transform child = parent.GetChild(i);
				result.Add(child);
				CollectChildrenRecursive(child, result);
			}
		}

		private Vector3 GetAxisVector(ChainAxisDirection axis)
		{
			return AxisToVector(axis);
		}

		public static Vector3 AxisToVector(ChainAxisDirection axis)
		{
			switch (axis)
			{
				case ChainAxisDirection.Down:
					return Vector3.down;
				case ChainAxisDirection.Right:
					return Vector3.right;
				case ChainAxisDirection.Left:
					return Vector3.left;
				case ChainAxisDirection.Forward:
					return Vector3.forward;
				case ChainAxisDirection.Back:
					return Vector3.back;
				default:
					return Vector3.up;
			}
		}

		private Vector3 GetBendVector(Vector3 forward)
		{
			Vector3 up = transform.up.sqrMagnitude > Epsilon ? transform.up : Vector3.up;
			Vector3 projected = Vector3.ProjectOnPlane(forward, up);
			if (projected.sqrMagnitude <= Epsilon)
				projected = Vector3.ProjectOnPlane(transform.forward, up);
			if (projected.sqrMagnitude <= Epsilon)
				projected = Vector3.ProjectOnPlane(transform.right, up);
			if (projected.sqrMagnitude <= Epsilon)
				projected = Vector3.forward;
			projected.Normalize();
			Vector3 side = Vector3.Cross(up, projected).normalized;
			float radians = bendYaw * Mathf.Deg2Rad;
			return (projected * Mathf.Cos(radians) + side * Mathf.Sin(radians)).normalized;
		}

		private float GetSlackArcAmplitude(float distance, float slack)
		{
			float targetLength = distance * reachSlackFactor * Mathf.Max(0f, LengthMultiplier);
			if (slack <= Epsilon || targetLength <= distance + Epsilon)
				return 0f;
			if (Mathf.Abs(distance - cachedSlackDistance) < 0.02f && Mathf.Abs(slack - cachedSlackSlack) < 0.02f)
				return cachedSlackAmplitude;
			cachedSlackDistance = distance;
			cachedSlackSlack = slack;
			float low = 0f;
			float high = Mathf.Max(targetLength, slack);
			for (int i = 0; i < 12; i++)
			{
				float middle = (low + high) * 0.5f;
				if (GetArcPolylineLength(distance, middle) < targetLength)
					low = middle;
				else
					high = middle;
			}

			cachedSlackAmplitude = high;
			return high;
		}

		private void FillPositions(Vector3[] positions, Vector3 start, Vector3 end, Vector3 bend, Vector3 side, float timeValue, float danceBlend)
		{
			float distance = Vector3.Distance(start, end);
			float slack = Mathf.Max(0f, distance * reachSlackFactor * Mathf.Max(0f, LengthMultiplier) - distance);
			float amplitude = GetSlackArcAmplitude(distance, slack);
			for (int i = 0; i < positions.Length; i++)
			{
				float t = (float)i / (positions.Length - 1);
				float middle = middleWeights.Length == positions.Length ? middleWeights[i] : Mathf.Clamp01(middleWeight.Evaluate(t));
				Vector3 arc = amplitude <= Epsilon ? Vector3.zero : bend * (amplitude * GetBendWave(t));
				float edge = middleWeights.Length == positions.Length ? edgeDanceWeights[i] : GetEdgeDanceWeight(i, positions.Length);
				Vector3 dance = danceBlend <= Epsilon ? Vector3.zero : GetDanceOffset(t, side, bend, Mathf.Min(middle, edge) * danceBlend, timeValue);
				positions[i] = i == 0 ? start : i == positions.Length - 1 ? end : Vector3.Lerp(start, end, t) + arc + dance;
			}
		}

		private float GetArcPolylineLength(float distance, float amplitude)
		{
			float length = 0f;
			Vector2 previous = Vector2.zero;
			for (int i = 1; i <= 20; i++)
			{
				float t = i / 20f;
				Vector2 point = new Vector2(t * distance, GetBendWave(t) * amplitude);
				length += Vector2.Distance(previous, point);
				previous = point;
			}

			return length;
		}

		private Vector3 GetDanceOffset(float t, Vector3 side, Vector3 bend, float weight, float timeValue)
		{
			float phase = phaseOffset + danceSpeed * timeValue + danceFrequency * t * Tau;
			float x, y;
			switch (danceFormula)
			{
				case ChainDanceFormula.Figure8:
					x = Mathf.Sin(phase);
					y = Mathf.Sin(phase * 2f);
					break;
				case ChainDanceFormula.Pendulum:
					x = Mathf.Sin(phase);
					y = Mathf.Sin(phase * 0.5f + Mathf.PI * 0.5f) * 0.65f;
					break;
				case ChainDanceFormula.Lissajous:
					x = Mathf.Sin(phase * 1.3f);
					y = Mathf.Sin(phase * 2.1f + Mathf.PI * 0.25f);
					break;
				case ChainDanceFormula.Triangle:
					x = TriangleWave(phase);
					y = TriangleWave(phase * 0.75f + Mathf.PI * 0.5f) * 0.85f;
					break;
				case ChainDanceFormula.Saw:
					x = SawWave(phase);
					y = SawWave(phase * 0.6f + Mathf.PI * 0.5f) * 0.7f;
					break;
				case ChainDanceFormula.Bounce:
					x = Mathf.Sin(phase);
					y = Mathf.Abs(Mathf.Sin(phase * 1.4f));
					break;
				case ChainDanceFormula.Clover:
					x = Mathf.Sin(phase * 2f);
					y = Mathf.Cos(phase * 3f);
					break;
				default:
					x = Mathf.Sin(phase);
					y = Mathf.Cos(phase * 0.73f);
					break;
			}

			return (side * x + bend * y) * danceAmplitude * weight;
		}

		private float GetBendWave(float t)
		{
			float sine = Mathf.Sin(Mathf.Clamp01(t) * Mathf.Max(1, bendCount) * Mathf.PI);
			return Mathf.Sign(sine) * Mathf.Pow(Mathf.Abs(sine), Mathf.Max(0.2f, bendSharpness));
		}

		private float TriangleWave(float phase)
		{
			return Mathf.PingPong(phase / Mathf.PI, 1f) * 2f - 1f;
		}

		private float SawWave(float phase)
		{
			float value = phase / Tau;
			return 2f * (value - Mathf.Floor(value + 0.5f));
		}

		private float GetEdgeDanceWeight(int index, int count)
		{
			if (count <= 1)
				return 0f;
			int edge = Mathf.Min(index, count - 1 - index);
			return edge <= 0 ? 0f : edge == 1 ? 0.1f : edge == 2 ? 0.22f : 1f;
		}

		public PlayerChainDeformer()
		{
			detachOnAwake = true;
			bones = Array.Empty<Transform>();
			middleWeight = new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(0.5f, 1f), new Keyframe(1f, 0f));
			baseLengthMultiplier = 1f;
			reachSlackFactor = 1.15f;
			rotationDamping = 18f;
			bendDirectionLag = 0.15f;
			bendCount = 1;
			bendSharpness = 1f;
			danceMode = ChainDanceMode.Always;
			danceAmplitude = 0.08f;
			danceFrequency = 2.2f;
			danceSpeed = 2.5f;
			linkBoxSize = new Vector3(0.04f, 0.18f, 0.04f);
			linkSpacing = 0.18f;
			linkLocalScale = Vector3.one;
			linkScaleOverChain = AnimationCurve.Constant(0f, 1f, 0f);
			linkSpacingOverChain = AnimationCurve.Constant(0f, 1f, 0f);
			linkForwardAxis = ChainAxisDirection.Right;
			alternateLinkRoll = true;
			linkOutline = true;
			linkOutlineWidth = 2f;
			linkTint = Color.white;
			linkShadowColor = new Color(0.72f, 0.72f, 0.72f, 1f);
			targetPositions = Array.Empty<Vector3>();
			gizmoPositions = Array.Empty<Vector3>();
			segmentLengths = Array.Empty<float>();
			cachedSlackDistance = float.NegativeInfinity;
			cachedSlackSlack = float.NegativeInfinity;
			middleWeights = Array.Empty<float>();
			edgeDanceWeights = Array.Empty<float>();
			ScaleMultiplier = Vector3.one;
			LengthMultiplier = 1f;
		}
	}
}
