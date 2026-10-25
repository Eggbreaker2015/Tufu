using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using FIMSpace.FTools;
using UnityEngine;
using UnityEngine.EventSystems;

namespace FIMSpace.FTail
{
	[AddComponentMenu("FImpossible Creations/Tail Animator 2")]
	[DefaultExecutionOrder(-4)]
	[HelpURL("https://assetstore.unity.com/packages/tools/animation/tail-animator-121819")]
	public class TailAnimator2 : MonoBehaviour, IDropHandler, IEventSystemHandler, IFHierarchyIcon
	{
		[Serializable]
		public class TailSegment
		{
			public Vector3 ProceduralPosition;

			public Vector3 ProceduralPositionWeightBlended;

			public Quaternion TrueTargetRotation;

			public Quaternion PosRefRotation;

			public Quaternion PreviousPosReferenceRotation;

			public Vector3 PreviousPosition;

			public float BlendValue;

			public Vector3 BoneDimensionsScaled;

			public float BoneLengthScaled;

			public Vector3 InitialLocalPosition;

			public Vector3 InitialLocalPositionInRoot;

			public Quaternion InitialLocalRotationInRoot;

			public Vector3 LocalOffset;

			public Quaternion InitialLocalRotation;

			public float ColliderRadius;

			public bool CollisionContactFlag;

			public float CollisionContactRelevancy;

			public Collision collisionContacts;

			public Vector3 VelocityHelper;

			public Quaternion QVelocityHelper;

			public Vector3 PreviousPush;

			public Quaternion Curving;

			public Vector3 Gravity;

			public Vector3 GravityLookOffset;

			public float LengthMultiplier;

			public float PositionSpeed;

			public float RotationSpeed;

			public float Springiness;

			public float Slithery;

			public float Curling;

			public float Slippery;

			public Quaternion LastKeyframeLocalRotation;

			public Vector3 LastKeyframeLocalPosition;

			private float deflectionSmoothVelo;

			public TailSegment ParentBone { get; private set; }

			public TailSegment ChildBone { get; private set; }

			public Transform transform { get; private set; }

			public int Index { get; private set; }

			public float IndexOverlLength { get; private set; }

			public float BoneLength { get; private set; }

			public TailCollisionHelper CollisionHelper { get; internal set; }

			public bool IsDetachable { get; private set; }

			public Vector3 LastFinalPosition { get; private set; }

			public Quaternion LastFinalRotation { get; private set; }

			public float DeflectionFactor { get; private set; }

			public Vector3 Deflection { get; private set; }

			public float DeflectionSmooth { get; private set; }

			public Vector3 DeflectionWorldPosition { get; private set; }

			public int DeflectionRelevancy { get; private set; }

			public FImp_ColliderData_Base LatestSelectiveCollision { get; internal set; }

			public TailSegment()
			{
			}

			public TailSegment(Transform transform)
			{
			}

			public TailSegment(TailSegment copyFrom)
			{
			}

			public void ReInitializeLocalPosRot(Vector3 initLocalPos, Quaternion initLocalRot)
			{
			}

			public void SetIndex(int i, int tailSegments)
			{
			}

			public void SetParentRef(TailSegment parent)
			{
			}

			public void SetChildRef(TailSegment child)
			{
			}

			public float GetRadiusScaled()
			{
				return 0f;
			}

			public void AssignDetachedRootCoords(Transform root)
			{
			}

			internal Vector3 BlendMotionWeight(Vector3 newPosition)
			{
				return default;
			}

			internal void PreCalibrate()
			{
			}

			internal void Validate()
			{
			}

			public void RefreshKeyLocalPosition()
			{
			}

			public void RefreshKeyLocalPositionAndRotation()
			{
			}

			public void RefreshKeyLocalPositionAndRotation(Vector3 p, Quaternion r)
			{
			}

			internal Vector3 ParentToFrontOffset()
			{
				return default;
			}

			public void RefreshFinalPos(Vector3 pos)
			{
			}

			public void RefreshFinalRot(Quaternion rot)
			{
			}

			public bool CheckDeflectionState(float zeroWhenLower, float smoothTime, float delta)
			{
				return false;
			}

			public bool DeflectionRelevant()
			{
				return false;
			}

			public bool? DeflectionRestoreState()
			{
				return null;
			}

			internal void ParamsFrom(TailSegment other)
			{
			}

			internal void ParamsFromAll(TailSegment other)
			{
			}

			internal void User_ReassignTransform(Transform t)
			{
			}

			public void Reset()
			{
			}
		}

		public enum ECollisionSpace
		{
			World_Slow = 0,
			Selective_Fast = 1
		}

		public enum ECollisionMode
		{
			m_3DCollision = 0,
			m_2DCollision = 1
		}

		[Serializable]
		public class IKBoneSettings
		{
			[Range(0f, 181f)]
			public float AngleLimit;

			[Range(0f, 181f)]
			public float TwistAngleLimit;

			public bool UseInChain;
		}

		public enum FEWavingType
		{
			Simple = 0,
			Advanced = 1
		}

		public enum EFDeltaType
		{
			DeltaTime = 0,
			SmoothDeltaTime = 1,
			UnscaledDeltaTime = 2,
			FixedDeltaTime = 3,
			SafeDelta = 4
		}

		public enum EAnimationStyle
		{
			Quick = 0,
			Accelerating = 1,
			Linear = 2
		}

		public enum ETailCategory
		{
			Setup = 0,
			Tweak = 1,
			Features = 2,
			Shaping = 3
		}

		public enum ETailFeaturesCategory
		{
			Main = 0,
			Collisions = 1,
			IK = 2,
			Experimental = 3
		}

		public enum EFixedMode
		{
			None = 0,
			Basic = 1,
			Late = 2
		}

		[CompilerGenerated]
		private sealed class _003CLateFixed_003Ed__278 : IEnumerator<object>, IEnumerator, IDisposable
		{
			private int _003C_003E1__state;

			private object _003C_003E2__current;

			public TailAnimator2 _003C_003E4__this;

			private WaitForFixedUpdate _003CfixedWait_003E5__2;

			object IEnumerator<object>.Current
			{
				[DebuggerHidden]
				get
				{
					return this._003C_003E2__current;
				}
			}

			object IEnumerator.Current
			{
				[DebuggerHidden]
				get
				{
					return this._003C_003E2__current;
				}
			}

			[DebuggerHidden]
			public _003CLateFixed_003Ed__278(int _003C_003E1__state)
			{
			}

			[DebuggerHidden]
			void IDisposable.Dispose()
			{
			}

			private bool MoveNext()
			{
				return false;
			}

			bool IEnumerator.MoveNext()
			{
				//ILSpy generated this explicit interface implementation from .override directive in MoveNext
				return this.MoveNext();
			}

			[DebuggerHidden]
			void IEnumerator.Reset()
			{
			}
		}

		[Tooltip("Using some simple calculations to make tail bend on colliders")]
		public bool UseCollision;

		[Tooltip("How collision should be detected, world gives you collision on all world colliders but with more use of cpu (using unity's rigidbodies), 'Selective' gives you possibility to detect collision on selected colliders without using Rigidbodies, it also gives smoother motion (deactivated colliders will still detect collision, unless its game object is disabled)")]
		public ECollisionSpace CollisionSpace;

		public ECollisionMode CollisionMode;

		[Tooltip("If you want to stop checking collision if segment collides with one collider\n\nSegment collision with two or more colliders in the same time with this option enabled can result in stuttery motion")]
		public bool CheapCollision;

		[Tooltip("Using trigger collider to include encountered colliders into collide with list")]
		public bool DynamicWorldCollidersInclusion;

		[Tooltip("Radius of trigger collider for dynamic inclusion of colliders")]
		public float InclusionRadius;

		public bool IgnoreMeshColliders;

		public List<Collider> IncludedColliders;

		public List<Collider2D> IncludedColliders2D;

		protected List<FImp_ColliderData_Base> IncludedCollidersData;

		protected List<FImp_ColliderData_Base> CollidersDataToCheck;

		[Tooltip("Capsules can give much more precise collision detection")]
		public int CollidersType;

		public bool CollideWithOtherTails;

		[Tooltip("Collision with colliders even if they're disabled (but game object must be enabled)\nHelpful to setup character limbs collisions without need to create new Layer")]
		public bool CollideWithDisabledColliders;

		[Range(0f, 1f)]
		public float CollisionSlippery;

		[Tooltip("If tail colliding objects should fit to colliders (0) or be reflect from them (Reflecting Only with 'Slithery' parameter greater than ~0.2)")]
		[Range(0f, 1f)]
		public float ReflectCollision;

		public AnimationCurve CollidersScaleCurve;

		public float CollidersScaleMul;

		[Range(0f, 1f)]
		public float CollisionsAutoCurve;

		public List<Collider> IgnoredColliders;

		public List<Collider2D> IgnoredColliders2D;

		public bool CollidersSameLayer;

		[Tooltip("If you add rigidbodies to each tail segment's collider, collision will work on everything but it will be less optimal, you don't have to add here rigidbodies but then you must have not kinematic rigidbodies on objects segments can collide")]
		public bool CollidersAddRigidbody;

		public float RigidbodyMass;

		[FPD_Layers]
		public int CollidersLayer;

		public bool UseSlitheryCurve;

		[FPD_FixedCurveWindow(0f, 0f, 1f, 1.2f, 0.1f, 0.8f, 1f, 0.9f)]
		public AnimationCurve SlitheryCurve;

		private float lastSlithery;

		private Keyframe[] lastSlitheryCurvKeys;

		public bool UseCurlingCurve;

		[FPD_FixedCurveWindow(0f, 0f, 1f, 1f, 0.65f, 0.4f, 1f, 0.9f)]
		public AnimationCurve CurlingCurve;

		private float lastCurling;

		private Keyframe[] lastCurlingCurvKeys;

		public bool UseSpringCurve;

		[FPD_FixedCurveWindow(0f, 0f, 1f, 1f, 0.9f, 0.7f, 0.2f, 0.9f)]
		public AnimationCurve SpringCurve;

		private float lastSpringiness;

		private Keyframe[] lastSpringCurvKeys;

		public bool UseSlipperyCurve;

		[FPD_FixedCurveWindow(0f, 0f, 1f, 1f, 0.2f, 0.9f, 0.6f, 0.9f)]
		public AnimationCurve SlipperyCurve;

		private float lastSlippery;

		private Keyframe[] lastSlipperyCurvKeys;

		public bool UsePosSpeedCurve;

		[FPD_FixedCurveWindow(0f, 0f, 1f, 1f, 0.2f, 1f, 0.3f, 0.9f)]
		public AnimationCurve PosCurve;

		private float lastPosSpeeds;

		private Keyframe[] lastPosCurvKeys;

		public bool UseRotSpeedCurve;

		[FPD_FixedCurveWindow(0f, 0f, 1f, 1f, 0.7f, 0.7f, 0.7f, 0.9f)]
		public AnimationCurve RotCurve;

		private float lastRotSpeeds;

		private Keyframe[] lastRotCurvKeys;

		[Tooltip("Spreading Tail Animator motion weight over bones")]
		public bool UsePartialBlend;

		[FPD_FixedCurveWindow(0f, 0f, 1f, 1f, 0.2f, 0.5f, 0.85f, 1f)]
		public AnimationCurve BlendCurve;

		private float lastTailAnimatorAmount;

		private Keyframe[] lastBlendCurvKeys;

		private TailSegment _ex_bone;

		public bool UseIK;

		private bool ikInitialized;

		[SerializeField]
		private FIK_CCDProcessor IK;

		[Tooltip("Target object to follow by IK")]
		public Transform IKTarget;

		public bool IKAutoWeights;

		[Range(0f, 1f)]
		public float IKBaseReactionWeight;

		[FPD_FixedCurveWindow(0f, 0f, 1f, 1f, 0.2f, 0.5f, 0.85f, 1f)]
		public AnimationCurve IKReactionWeightCurve;

		public bool IKAutoAngleLimits;

		[FPD_Suffix(0f, 181f, FPD_SuffixAttribute.SuffixMode.FromMinToMaxRounded, "°", true, 0)]
		public float IKAutoAngleLimit;

		[Tooltip("If ik process should work referencing to previously computed CCDIK pose (can be more precise but need more adjusting in weights and angle limits)")]
		public bool IKContinousSolve;

		[Tooltip("Inverting ik iteration order to generate different pose results - more straight towards target")]
		public bool IkInvertOrder;

		[FPD_Suffix(0f, 1f, FPD_SuffixAttribute.SuffixMode.From0to100, "%", true, 0)]
		[Tooltip("How much IK motion sohuld be used in tail animator motion -> 0: turned off")]
		public float IKBlend;

		[FPD_Suffix(0f, 1f, FPD_SuffixAttribute.SuffixMode.From0to100, "%", true, 0)]
		[Tooltip("If syncing with animator then applying motion of keyframe animation for IK")]
		public float IKAnimatorBlend;

		[Range(1f, 32f)]
		[Tooltip("How much iterations should do CCDIK algorithm in one frame")]
		public int IKReactionQuality;

		[Range(0f, 1f)]
		[Tooltip("Smoothing reactions in CCD IK algorithm")]
		public float IKSmoothing;

		[Range(0f, 1.5f)]
		public float IKStretchToTarget;

		[FPD_FixedCurveWindow(0f, 0f, 1f, 1f, 0.9f, 0.4f, 0.5f, 1f)]
		public AnimationCurve IKStretchCurve;

		public List<IKBoneSettings> IKLimitSettings;

		public bool IKSelectiveChain;

		private Vector3? _IKCustomPos;

		private List<TailSegment> _pp_reference;

		private TailSegment _pp_ref_rootParent;

		private TailSegment _pp_ref_lastChild;

		private bool _pp_initialized;

		[Tooltip("Rotation offset for tail (just first (root) bone is rotated)")]
		public Quaternion RotationOffset;

		[Tooltip("Rotate each segment a bit to create curving effect")]
		public Quaternion Curving;

		[Tooltip("Spread curving rotation offset weight over tail segments")]
		public bool UseCurvingCurve;

		[FPD_FixedCurveWindow(0f, -1f, 1f, 1f, 0.75f, 0.75f, 0.75f, 0.85f)]
		public AnimationCurve CurvCurve;

		private Quaternion lastCurving;

		private Keyframe[] lastCurvingKeys;

		[Tooltip("Make tail longer or shorter")]
		public float LengthMultiplier;

		[Tooltip("Spread length multiplier weight over tail segments")]
		public bool UseLengthMulCurve;

		[FPD_FixedCurveWindow(0f, 0f, 1f, 3f, 0f, 1f, 1f, 1f)]
		public AnimationCurve LengthMulCurve;

		private float lastLengthMul;

		private Keyframe[] lastLengthKeys;

		[Tooltip("Spread gravity weight over tail segments")]
		public bool UseGravityCurve;

		[FPD_FixedCurveWindow(0f, 0f, 1f, 1f, 0.85f, 0.35f, 0.25f, 0.85f)]
		[Tooltip("Spread gravity weight over tail segments")]
		public AnimationCurve GravityCurve;

		[Tooltip("Simulate gravity weight for tail logics")]
		public Vector3 Gravity;

		private Vector3 lastGravity;

		private Keyframe[] lastGravityKeys;

		[Tooltip("Using auto waving option to give floating effect")]
		public bool UseWaving;

		[Tooltip("Adding some variation to waving animation")]
		public bool CosinusAdd;

		[Tooltip("If you want few tails to wave in the same way you can set this sinus period cycle value")]
		public float FixedCycle;

		[Tooltip("How frequent swings should be")]
		public float WavingSpeed;

		[Tooltip("How big swings should be")]
		public float WavingRange;

		[Tooltip("What rotation axis should be used in auto waving")]
		public Vector3 WavingAxis;

		[Tooltip("Type of waving animation algorithm, it can be simple trigonometric wave or animation based on noises (advanced)")]
		public FEWavingType WavingType;

		[Tooltip("Offsetting perlin noise to generate different variation of tail rotations")]
		public float AlternateWave;

		private float _waving_waveTime;

		private float _waving_cosTime;

		private Vector3 _waving_sustain;

		public bool UseWind;

		[FPD_Suffix(0f, 2.5f, FPD_SuffixAttribute.SuffixMode.PercentageUnclamped, "%", true, 0)]
		public float WindEffectPower;

		[FPD_Suffix(0f, 2.5f, FPD_SuffixAttribute.SuffixMode.PercentageUnclamped, "%", true, 0)]
		public float WindTurbulencePower;

		[FPD_Suffix(0f, 1.5f, FPD_SuffixAttribute.SuffixMode.PercentageUnclamped, "%", true, 0)]
		public float WindWorldNoisePower;

		public Vector3 WindEffect;

		public List<TailSegment> TailSegments;

		[SerializeField]
		private TailSegment GhostParent;

		[SerializeField]
		private TailSegment GhostChild;

		private Vector3 _limiting_limitPosition;

		private Vector3 _limiting_influenceOffset;

		private float _limiting_stretchingHelperTooLong;

		private float _limiting_stretchingHelperTooShort;

		private Quaternion _limiting_angle_ToTargetRot;

		private Quaternion _limiting_angle_targetInLocal;

		private Quaternion _limiting_angle_newLocal;

		private Vector3 _tc_segmentGravityOffset;

		private Vector3 _tc_segmentGravityToParentDir;

		private Vector3 _tc_preGravOff;

		[Tooltip("If you want to use max distance fade option to smoothly disable tail animator when object is going far away from camera")]
		public bool UseMaxDistance;

		[Tooltip("(By default camera transform) Measuring distance from this object to define if object is too far and not need to update tail animator")]
		public Transform DistanceFrom;

		[HideInInspector]
		public Transform _distanceFrom_Auto;

		[Tooltip("Max distance to main camera / target object to smoothly turn off tail animator.")]
		public float MaximumDistance;

		[Tooltip("If object in range should be detected only when is nearer than 'MaxDistance' to avoid stuttery enabled - disable switching")]
		[Range(0f, 1f)]
		public float MaxOutDistanceFactor;

		[Tooltip("If distance should be measured not using Up (y) axis")]
		public bool DistanceWithoutY;

		[Tooltip("Offsetting point from which we want to measure distance to target")]
		public Vector3 DistanceMeasurePoint;

		[Tooltip("Disable fade duration in seconds")]
		[Range(0.25f, 2f)]
		public float FadeDuration;

		private bool maxDistanceExceed;

		private Transform finalDistanceFrom;

		private bool wasCameraSearch;

		private float distanceWeight;

		private int _tc_startI;

		private int _tc_startII;

		private TailSegment _tc_rootBone;

		private Quaternion _tc_lookRot;

		private Quaternion _tc_targetParentRot;

		private Quaternion _tc_startBoneRotOffset;

		private float _tc_tangle;

		private float _sg_springVelo;

		private float _sg_curly;

		private Vector3 _sg_push;

		private Vector3 _sg_targetPos;

		private Vector3 _sg_targetChildWorldPosInParentFront;

		private Vector3 _sg_dirToTargetParentFront;

		private Quaternion _sg_orientation;

		private float _sg_slitFactor;

		private bool wasDisabled;

		private float justDelta;

		private float secPeriodDelta;

		private float deltaForLerps;

		private float rateDelta;

		protected float collectedDelta;

		protected int framesToSimulate;

		protected int previousframesToSimulate;

		private bool updateTailAnimator;

		private int startAfterTPoseCounter;

		private bool fixedUpdated;

		private bool lateFixedIsRunning;

		private bool fixedAllow;

		[Tooltip("Making tail segment deflection influence back segments")]
		[Range(0f, 1f)]
		public float Deflection;

		[FPD_Suffix(1f, 89f, FPD_SuffixAttribute.SuffixMode.FromMinToMaxRounded, "°", true, 0)]
		public float DeflectionStartAngle;

		[Range(0f, 1f)]
		public float DeflectionSmooth;

		[FPD_FixedCurveWindow(0f, 0f, 1f, 1f, 0.65f, 0.4f, 1f, 0.9f)]
		public AnimationCurve DeflectionFalloff;

		[Tooltip("Deflection can be triggered every time tail is waving but you not always would want this feature be enabled (different behaviour of tail motion)")]
		public bool DeflectOnlyCollisions;

		private List<TailSegment> _defl_source;

		private float _defl_treshold;

		private bool _forceDisable;

		private float _forceDisableElapsed;

		public ETailCategory _Editor_Category;

		public ETailFeaturesCategory _Editor_FeaturesCategory;

		public bool DrawGizmos;

		[Tooltip("First bone of tail motion chain")]
		public Transform StartBone;

		[Tooltip("Finish bone of tail motion chain")]
		public Transform EndBone;

		[Tooltip("Adjusting end point for end tail bone motion")]
		public Vector3 EndBoneJointOffset;

		public List<Transform> _TransformsGhostChain;

		public int _GhostChainInitCount;

		protected bool initialized;

		[Tooltip("Target FPS update rate for Tail Animator.\n\nIf you want Tail Animator to behave the same in low/high fps, set this value for example to 60.\nIt also can help optimizing if your game have more than 60 fps.")]
		public int UpdateRate;

		[Tooltip("If your character Unity's Animator have update mode set to 'Animate Physics' you should enable it here too")]
		public EFixedMode AnimatePhysics;

		[Tooltip("When using target fps rate you can interpolate coordinates for smoother effect when object with tail is moving a lot")]
		public bool InterpolateRate;

		[Tooltip("Simulating tail motion at initiation to prevent jiggle start")]
		public bool Prewarm;

		internal float OverrideWeight;

		protected float conditionalWeight;

		protected bool collisionInitialized;

		protected bool forceRefreshCollidersData;

		private Vector3 previousWorldPosition;

		protected Transform rootTransform;

		protected bool preAutoCorrect;

		[Tooltip("Blending Slithery - smooth & soft tentacle like movement (value = 1)\nwith more stiff & springy motion (value = 0)\n\n0: Stiff somewhat like tree branch\n1: Soft like squid tentacle / Animal tail")]
		[Range(0f, 1.2f)]
		public float Slithery;

		[Tooltip("How curly motion should be applied to tail segments")]
		[Range(0f, 1f)]
		public float Curling;

		[Tooltip("Elastic spring effect making motion more 'meaty'")]
		[Range(0f, 1f)]
		public float Springiness;

		[Tooltip("If you want to limit stretching/gumminess of position motion when object moves fast. Recommended adjust to go with it under 0.3 value.\nValue = 1: Unlimited stretching")]
		[Range(0f, 1f)]
		public float MaxStretching;

		[Tooltip("Limiting max rotation angle for each tail segment")]
		[FPD_Suffix(1f, 181f, FPD_SuffixAttribute.SuffixMode.FromMinToMaxRounded, "°", true, 0)]
		public float AngleLimit;

		[Tooltip("If you need specific axis to be limited.\nLeave unchanged to limit all axes.")]
		public Vector3 AngleLimitAxis;

		[Tooltip("If you want limit axes symmetrically leave this parameter unchanged, if you want limit one direction of axis more than reversed, tweak this parameter")]
		public Vector2 LimitAxisRange;

		[Tooltip("If limiting shouldn't be too rapidly performed")]
		[Range(0f, 1f)]
		public float LimitSmoothing;

		[Tooltip("If your object moves very fast making tail influenced by speed too much then you can controll it with this parameter")]
		[FPD_Suffix(0f, 1.5f, FPD_SuffixAttribute.SuffixMode.PercentageUnclamped, "%", true, 0)]
		public float MotionInfluence;

		[Tooltip("Additional Y influence controll useful when your character is jumping (works only when MotionInfluence value is other than 100%)")]
		[Range(0f, 1f)]
		public float MotionInfluenceInY;

		[Tooltip("If first bone of chain should also be affected with whole chain")]
		public bool IncludeParent;

		[Tooltip("By basic algorithm of Tail Animator different sized tails with different number of bones would animate with different bending thanks to this toggle every setup bends in very similar amount.\n\nShort tails will bend more and longer oner with bigger amount of bones less with this option enabled.")]
		[Range(0f, 1f)]
		public float UnifyBendiness;

		[Tooltip("Reaction Speed is defining how fast tail segments will return to target position, it gives animation more underwater/floaty feeling if it's lower")]
		[Range(0f, 1f)]
		public float ReactionSpeed;

		[Tooltip("Sustain is similar to reaction speed in reverse, but providing sustain motion effect when increased")]
		[Range(0f, 1f)]
		public float Sustain;

		[Tooltip("Rotation speed is defining how fast tail segments will return to target rotation, it gives animation more lazy feeling if it's lower")]
		[Range(0f, 1f)]
		public float RotationRelevancy;

		[Tooltip("Smoothing motion values change over time style to be applied for 'Reaction Speed' and 'Rotation Relevancy' parameters")]
		public EAnimationStyle SmoothingStyle;

		[Tooltip("Slowmo or speedup tail animation reaction")]
		public float TimeScale;

		[Tooltip("Delta time type to be used by algorithm")]
		public EFDeltaType DeltaType;

		[Tooltip("Useful when you use other components to affect bones hierarchy and you want this component to follow other component's changes\n\nIt can be really useful when working with 'Spine Animator'")]
		public bool UpdateAsLast;

		[Tooltip("Checking if keyframed animation has some empty keyframes which could cause unwanted twisting errors")]
		public bool DetectZeroKeyframes;

		[Tooltip("Initializing Tail Animator after first frames of game to not initialize with model's T-Pose but after playing some other animation")]
		public bool StartAfterTPose;

		[Tooltip("If you want Tail Animator to stop computing when choosed mesh is not visible in any camera view (editor's scene camera is detecting it too)")]
		public Renderer OptimizeWithMesh;

		[Tooltip("If you want to check multiple meshes visibility on screen to define if you want to disable tail animator. (useful when using LOD for skinned mesh renderer)")]
		public Renderer[] OptimizeWithMeshes;

		[Tooltip("Blend Source Animation (keyframed / unanimated) and Tail Animator")]
		[FPD_Suffix(0f, 1f, FPD_SuffixAttribute.SuffixMode.From0to100, "%", true, 0)]
		public float TailAnimatorAmount;

		[Tooltip("Removing transforms hierachy structure to optimize Unity's calculations on Matrixes.\nIt can give very big boost in performance for long tails but it can't work with animated models!")]
		public bool DetachChildren;

		[Tooltip("If tail movement should not move in depth you can use this parameter")]
		public int Axis2D;

		[Tooltip("[Experimental: Works only with Slithery Blend set to >= 1] Making each segment go to target pose in front of parent segment creating new animation effect")]
		[Range(-1f, 1f)]
		public float Tangle;

		[Tooltip("Making tail animate also roll rotation like it was done in Tail Animator V1 ! Use Rotation Relevancy Parameter (set lower than 0.5) !")]
		public bool AnimateRoll;

		[Tooltip("Overriding keyframe animation with just Tail Animator option (keyframe animation treated as t-pose bones rotations)")]
		[Range(0f, 1f)]
		public float OverrideKeyframeAnimation;

		private Transform _baseTransform;

		public List<Component> DynamicAlwaysInclude { get; private set; }

		public Quaternion WavingRotationOffset { get; private set; }

		public float _TC_TailLength { get; private set; }

		public string EditorIconPath => null;

		public bool IsInitialized => this.initialized;

		public Transform BaseTransform => null;

		private void RefreshSegmentsColliders()
		{
		}

		private void BeginCollisionsUpdate()
		{
		}

		private void SetupSphereColliders()
		{
		}

		internal void CollisionDetection(int index, Collision collision)
		{
		}

		internal void ExitCollision(int index)
		{
		}

		protected bool UseCollisionContact(int index, ref Vector3 pos)
		{
			return false;
		}

		public void RefreshIncludedCollidersDataList()
		{
		}

		public bool PushIfSegmentInsideCollider(TailSegment bone, ref Vector3 targetPoint)
		{
			return false;
		}

		protected float GetColliderSphereRadiusFor(int i)
		{
			return 0f;
		}

		protected float GetColliderSphereRadiusFor(List<Transform> transforms, int i)
		{
			return 0f;
		}

		public void AddCollider(Collider collider)
		{
		}

		public void AddCollider(Collider2D collider)
		{
		}

		public void CheckForColliderDuplicatesAndNulls()
		{
		}

		public void CheckForColliderDuplicatesAndNulls2D()
		{
		}

		private void TailCalculations_ComputeSegmentCollisions(TailSegment bone, ref Vector3 position)
		{
		}

		private void ExpertParamsUpdate()
		{
		}

		private void ExpertCurvesEndUpdate()
		{
		}

		private void Expert_UpdatePosSpeed()
		{
		}

		private void Expert_UpdateRotSpeed()
		{
		}

		private void Expert_UpdateSpringiness()
		{
		}

		private void Expert_UpdateSlithery()
		{
		}

		private void Expert_UpdateCurling()
		{
		}

		private void Expert_UpdateSlippery()
		{
		}

		private void Expert_UpdateBlending()
		{
		}

		private void InitIK()
		{
		}

		public void IKSetCustomPosition(Vector3? tgt)
		{
		}

		private void UpdateIK()
		{
		}

		public void IK_ApplyLimitBoneSettings()
		{
		}

		public void IK_RefreshLimitSettingsContainer()
		{
		}

		private bool PostProcessingNeeded()
		{
			return false;
		}

		private void PostProcessing_Begin()
		{
		}

		private void PostProcessing_ReferenceUpdate()
		{
		}

		private void ShapingParamsUpdate()
		{
		}

		private void Shaping_UpdateCurving()
		{
		}

		private void Shaping_UpdateGravity()
		{
		}

		private void Shaping_UpdateLengthMultiplier()
		{
		}

		private void ShapingEndUpdate()
		{
		}

		private void Waving_Initialize()
		{
		}

		private void Waving_AutoSwingUpdate()
		{
		}

		private void Waving_SustainUpdate()
		{
		}

		private void WindEffectUpdate()
		{
		}

		protected virtual void Init()
		{
		}

		public void DetachChildrenTransforms()
		{
		}

		private void InitializePostProcessing()
		{
		}

		protected void StretchingLimiting(TailSegment bone)
		{
		}

		protected Vector3 AngleLimiting(TailSegment child, Vector3 targetPos)
		{
			return default;
		}

		private void MotionInfluenceLimiting()
		{
		}

		private void CalculateGravityPositionOffsetForSegment(TailSegment bone)
		{
		}

		private void Axis2DLimit(TailSegment child)
		{
		}

		public float GetDistanceMeasure(Vector3 targetPosition)
		{
			return 0f;
		}

		private void MaxDistanceCalculations()
		{
		}

		private Vector3 TailCalculations_SmoothPosition(Vector3 from, Vector3 to, TailSegment bone)
		{
			return default;
		}

		private Vector3 TailCalculations_SmoothPositionLerp(Vector3 from, Vector3 to, float speed)
		{
			return default;
		}

		private Vector3 TailCalculations_SmoothPositionSmoothDamp(Vector3 from, Vector3 to, ref Vector3 velo, float speed)
		{
			return default;
		}

		private Vector3 TailCalculations_SmoothPositionLinear(Vector3 from, Vector3 to, float speed)
		{
			return default;
		}

		private Quaternion TailCalculations_SmoothRotation(Quaternion from, Quaternion to, TailSegment bone)
		{
			return default;
		}

		private Quaternion TailCalculations_SmoothRotationLerp(Quaternion from, Quaternion to, float speed)
		{
			return default;
		}

		private Quaternion TailCalculations_SmoothRotationSmoothDamp(Quaternion from, Quaternion to, ref Quaternion velo, float speed)
		{
			return default;
		}

		private Quaternion TailCalculations_SmoothRotationLinear(Quaternion from, Quaternion to, float speed)
		{
			return default;
		}

		private void TailCalculations_Begin()
		{
		}

		private void TailSegments_UpdateRootFeatures()
		{
		}

		private void TailCalculations_SegmentPreProcessingStack(TailSegment child)
		{
		}

		private void TailCalculations_SegmentPostProcessing(TailSegment bone)
		{
		}

		private void TailCalculations_SegmentRotation(TailSegment child, Vector3 localOffset)
		{
		}

		private void TailCalculations_SegmentRotationDetached(TailSegment child, Vector3 localOffset)
		{
		}

		private void TailCalculations_ApplySegmentMotion(TailSegment child)
		{
		}

		private void TailSegment_PrepareBoneLength(TailSegment child)
		{
		}

		private void TailSegment_PrepareMotionParameters(TailSegment child)
		{
		}

		private void TailSegment_PrepareVelocity(TailSegment child)
		{
		}

		private void TailSegment_PrepareRotation(TailSegment child)
		{
		}

		private void TailSegment_PrepareRotationDetached(TailSegment child)
		{
		}

		private void TailSegment_BaseSwingProcessing(TailSegment child)
		{
		}

		private void TailSegment_PreRotationPositionBlend(TailSegment child)
		{
		}

		private Quaternion TailSegment_RotationSlithery(TailSegment child)
		{
			return default;
		}

		private Quaternion TailSegment_RotationSlitheryDetached(TailSegment child)
		{
			return default;
		}

		private Quaternion GetSlitheryReferenceRotation(TailSegment child)
		{
			return default;
		}

		private Quaternion TailSegment_RotationStiff(TailSegment child)
		{
			return default;
		}

		private Quaternion TailSegment_GetSwingRotation(TailSegment child, float curlFactor)
		{
			return default;
		}

		private Quaternion TailSegment_GetSwingRotationDetached(TailSegment child, float curlFactor)
		{
			return default;
		}

		private float TailSegment_GetUnifiedBendinessMultiplier(TailSegment child)
		{
			return 0f;
		}

		public void TailSegments_UpdateCoordsForRootBone(TailSegment parent)
		{
		}

		public void TailCalculations_UpdateArtificialChildBone(TailSegment child)
		{
		}

		public void Editor_TailCalculations_RefreshArtificialParentBone()
		{
		}

		private void SimulateTailMotionFrame(bool pp)
		{
		}

		private void UpdateTailAlgorithm()
		{
		}

		private void CheckIfTailAnimatorShouldBeUpdated()
		{
		}

		public bool DisabledByInvisibility()
		{
			return false;
		}

		private void DeltaTimeCalculations()
		{
		}

		private void StableUpdateRateCalculations()
		{
		}

		private void PreCalibrateBones()
		{
		}

		private void CalibrateBones()
		{
		}

		public void CheckForNullsInGhostChain()
		{
		}

		private float GetClampedSmoothDelta()
		{
			return 0f;
		}

		private Quaternion MultiplyQ(Quaternion rotation, float times)
		{
			return default;
		}

		public float GetValueFromCurve(int i, AnimationCurve c)
		{
			return 0f;
		}

		public AnimationCurve ClampCurve(AnimationCurve a, float timeStart, float timeEnd, float lowest, float highest)
		{
			return null;
		}

		public void RefreshTransformsList()
		{
		}

		public TailSegment GetGhostChild()
		{
			return this.GhostChild;
		}

		[IteratorStateMachine(typeof(_003CLateFixed_003Ed__278))]
		private IEnumerator LateFixed()
		{
			return null;
		}

		private void Deflection_BeginUpdate()
		{
		}

		private void Deflection_RemoveDeflectionSource(TailSegment child)
		{
		}

		private void Deflection_AddDeflectionSource(TailSegment child)
		{
		}

		private void Deflection_SegmentOffsetSimple(TailSegment child, ref Vector3 position)
		{
		}

		public void User_SetTailTransforms(List<Transform> list)
		{
		}

		public TailSegment User_AddTailTransform(Transform transform)
		{
			return null;
		}

		public void User_CutEndSegmentsTo(int fromLastTo)
		{
		}

		public void User_ReposeTail()
		{
		}

		public void User_ForceDisabled(bool disable)
		{
			this._forceDisable = disable;
		}

		public void OnDrop(PointerEventData data)
		{
		}

		private void OnValidate()
		{
		}

		public void GetGhostChain()
		{
		}

		private void Start()
		{
		}

		private void Reset()
		{
		}

		private void Update()
		{
		}

		private void FixedUpdate()
		{
		}

		private void LateUpdate()
		{
		}

		private void EndUpdate()
		{
		}
	}
}
