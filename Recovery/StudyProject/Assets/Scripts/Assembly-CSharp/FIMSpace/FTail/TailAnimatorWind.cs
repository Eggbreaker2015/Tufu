using UnityEngine;
using UnityEngine.EventSystems;

namespace FIMSpace.FTail
{
	[AddComponentMenu("FImpossible Creations/Tail Animator Utilities/Tail Animator Wind")]
	public class TailAnimatorWind : MonoBehaviour, IDropHandler, IEventSystemHandler, IFHierarchyIcon
	{
		[Header("In playmode you will find this object in DontDestroyOnLoad")]
		[FPD_Header("Main Wind Setings", 2f, 4f, 2)]
		public float power;

		public float additionalTurbulence;

		public float additionalTurbSpeed;

		[Space(7f)]
		public WindZone SyncWithUnityWindZone;

		public float UnityWindZonePowerMul;

		public float UnityWindZoneTurbMul;

		[Header("Overriding wind if value below different than 0,0,0")]
		public Vector3 overrideWind;

		[FPD_Header("Procedural Wind Settings (if not syncing and not overriding)", 6f, 4f, 2)]
		[Range(0.1f, 1f)]
		public float rapidness;

		[FPD_Suffix(0f, 360f, FPD_SuffixAttribute.SuffixMode.FromMinToMaxRounded, "°", true, 0)]
		public float changesPower;

		[Header("Extra")]
		[Range(0f, 10f)]
		public float turbulenceSpeed;

		[FPD_Header("World Position Turbulence", 6f, 4f, 2)]
		[Tooltip("Increase to make objects next to each other wave in slightly different way")]
		public float worldTurb;

		[Tooltip("If higher no performance cost, it is just a number")]
		public float worldTurbScale;

		public float worldTurbSpeed;

		[FPD_Header("Tail Compoenents Related", 6f, 4f, 2)]
		[Tooltip("When tail is longer then power of wind should be higher")]
		public bool powerDependOnTailLength;

		[Tooltip("Don't destroy on load")]
		public bool persistThroughAllScenes;

		private Vector3 targetWind;

		private Vector3 smoothWind;

		private Vector3 windVeloHelper;

		private Quaternion windOrientation;

		private Quaternion smoothWindOrient;

		private Quaternion smoothWindOrientHelper;

		private float[] randNumbers;

		private float[] randTimes;

		private float[] randSpeeds;

		private int frameOffset;

		private Vector3 finalAddTurbulence;

		private Vector3 addTurbHelper;

		private Vector3 smoothAddTurbulence;

		public string EditorIconPath => null;

		public static TailAnimatorWind Instance { get; private set; }

		public void OnDrop(PointerEventData data)
		{
		}

		private void Awake()
		{
		}

		public void OnValidate()
		{
		}

		private void Update()
		{
		}

		public static void Refresh()
		{
		}

		public void AffectTailWithWind(TailAnimator2 t)
		{
		}

		private void Start()
		{
		}

		private void ComputeWind()
		{
		}

		private Vector3 GetAddTurbulence()
		{
			return default;
		}
	}
}
