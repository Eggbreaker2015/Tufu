using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Codebase.Player.Chain;
using Codebase.Player.Inventory;
using Codebase.Player.Models;
using Codebase.Unit;
using DG.Tweening;
using UnityEngine;

namespace Codebase.Player
{
	public class WeaponView : MonoBehaviour
	{
		private struct TimingData
		{
			public float AttackCooldown;

			public float VisualDuration;

			public float HitTime;

			public float WindupEnd;

			public float TravelEnd;
		}

		private struct ArcFrame
		{
			public Vector3 Position;

			public Vector3 Tangent;

			public Vector3 Normal;
		}

		private struct FollowVfxRuntime
		{
			public Transform Instance;

			public WeaponAttackPhase Phase;

			public WeaponAttackVfxOffsetSpace OffsetSpace;

			public Vector3 PositionOffset;

			public Vector3 RotationOffset;

			public float EndTime;
		}

		[CompilerGenerated]
		private sealed class _003CDespawnVfx_003Ed__198 : IEnumerator<object>, IEnumerator, IDisposable
		{
			private int _003C_003E1__state;

			private object _003C_003E2__current;

			public float delay;

			public Transform vfx;

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
			public _003CDespawnVfx_003Ed__198(int _003C_003E1__state)
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

		[CompilerGenerated]
		private sealed class _003CSpawnAttackVfxDelayed_003Ed__188 : IEnumerator<object>, IEnumerator, IDisposable
		{
			private int _003C_003E1__state;

			private object _003C_003E2__current;

			public float delay;

			public WeaponView _003C_003E4__this;

			public WeaponAttackVfxSettings settings;

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
			public _003CSpawnAttackVfxDelayed_003Ed__188(int _003C_003E1__state)
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

		private static readonly int AttackType = Animator.StringToHash("AttackType");

		private static readonly Vector3 One = Vector3.one;

		private static readonly Vector3 DefaultWindupOffset = new Vector3(-2.4f, 2.6f, .8f);

		private const float DefaultAnimationDurationCap = 0.8f;

		private const float DefaultOvertravelRatio = 0f;

		private const float ReturnHoldRatio = 1f / 3f;

		private const float TargetLostReturnDuration = 0.3f;

		[SerializeField]
		private WeaponAttackType attackType = (WeaponAttackType)2;

		[SerializeField]
		private Vector3 hitOffset = new Vector3(0, 1.2f, 0);

		[SerializeField]
		private float manualMaxDistance = 6;

		[Header("Animation Settings")]
		[SerializeField]
		private WeaponAttackAnimationSettings animationSettings;

		[Header("Idle Animation Settings")]
		[SerializeField]
		private WeaponIdleAnimationSettings idleAnimationSettings;

		[Header("Attack VFX Settings")]
		[SerializeField]
		private List<WeaponAttackVfxSettings> attackVfx = new List<WeaponAttackVfxSettings>();

		private ItemSlotModel model;

		private Transform target;

		private PlayerChainDeformer chainDeformer;

		private Vector3 lastTargetPosition;

		private Vector3 targetPos;

		private int manualMode;

		private float manualCycleCooldown;

		private bool isDetachedForAttack;

		private Transform originalParent;

		private Vector3 originalLocalPosition;

		private Vector3 attackStartWorldPosition;

		private Quaternion attackStartWorldRotation;

		private Vector3 originalLocalScale;

		private Vector3 attackStartScale;

		private float angleOffset;

		private Transform playerTransform;

		private PlayerModel playerModel;

		private UnitMovement playerMovement;

		private string animationKey;

		private Animator animator;

		private int slotIndex;

		private WeaponSlotSide slotSide;

		private Vector3 positionOffset;

		private Vector3 rotationOffset;

		private Vector3 scaleOffset;

		private bool useWorldIdle;

		private Transform idleReference;

		private Vector3 cachedAttackTargetOffset;

		private Vector3 cachedIdleLocalPosition;

		private bool hasTarget;

		private Transform currentTargetTransform;

		private Vector3 castTargetPoint;

		private Vector3 strikeTargetPoint;

		private Vector3 travelEndPoint;

		private Vector3 returnTargetPoint;

		private Vector3 returnStartPosition;

		private Quaternion returnStartRotation;

		private Vector3 returnStartScale;

		private bool returnRotationCaptured;

		private bool isReturningToIdleAfterTargetLost;

		private float targetLostReturnElapsed;

		private Vector3 targetLostReturnStartPosition;

		private Quaternion targetLostReturnStartRotation;

		private Vector3 targetLostReturnStartScale;

		private WeaponAttackPhase currentAttackPhase;

		private Vector3 idleLoopOffsetLocal;

		private Vector3 idleLoopTargetOffsetLocal;

		private bool idleLoopHasTargetOffset;

		private bool idleLoopReturnToBase;

		private Quaternion idleLoopRotationOffset = Quaternion.identity;

		private Quaternion idleLoopRotationTarget = Quaternion.identity;

		private bool idleLoopHasRotationTarget;

		private bool attackVfxSpawnedForCurrentSwing;

		private readonly List<Coroutine> scheduledVfxCoroutines = new List<Coroutine>();

		private readonly List<FollowVfxRuntime> activeFollowVfx = new List<FollowVfxRuntime>();

		private float cachedAnimationDurationCap;

		private float cachedOvertravelRatio;

		private float cachedOvertravelDirectionAngle;

		private float cachedAngleOffset;

		private float cachedWindupRatio;

		private float cachedTravelRatio;

		private float cachedReturnRatio;

		private Vector3 cachedWindupOffset;

		private WeaponWindupOffsetSpace cachedWindupOffsetSpace;

		private AttackPoseSettings cachedWindupPose;

		private AttackPoseSettings cachedHitPose;

		private AttackPoseSettings cachedOvertravelPose;

		private Vector3 cachedTravelEndPositionOffset;

		private Vector3 cachedWeaponTopAxisLocal;

		private Vector3 cachedWeaponBladeAxisLocal;

		private Ease[] cachedPhaseEase = new Ease[4];

		private Ease[] cachedPhaseScaleEase = new Ease[4];

		private Ease[] cachedPhaseRotationEase = new Ease[4];

		private WeaponTravelType[] cachedPhaseTravelType = new WeaponTravelType[4];

		private WeaponPathFormula[] cachedPhasePathFormula = new WeaponPathFormula[4];

		private float[] cachedPhaseStraightening = new float[4];

		private float cachedTimingAttackCooldown = -1;

		private TimingData cachedTimingData;

		public Vector3 ScaleMultiplier { get; set; } = Vector3.one;

		public bool IsExternallyAnimated { get; set; }

		private bool UsesCharacterAnimator => animator != null;

		public WeaponAttackAnimationSettings AnimationSettings => this.animationSettings;

		public float AnimationDurationCap => this.cachedAnimationDurationCap;

		public float OvertravelRatio => this.cachedOvertravelRatio;

		public float OvertravelDirectionAngle => this.cachedOvertravelDirectionAngle;

		public float AngleOffset => this.cachedAngleOffset;

		public float WindupRatio => this.cachedWindupRatio;

		public float TravelRatio => this.cachedTravelRatio;

		public float ReturnRatio => this.cachedReturnRatio;

		private bool HasOvertravelState => cachedOvertravelRatio > .0001f;

		private float StrikeTravelT => 1f / Mathf.Max(1f + Mathf.Max(0, cachedOvertravelRatio), .0001f);

		public void Setup(WeaponViewSetupData data)
		{
			// Native 0x14497e8. All references come from the original player slot and prefab.
			model=data.Model; target=data.Target; chainDeformer=data.ChainDeformer;
			angleOffset=data.SlotIndex*90-135;
			playerTransform=data.PlayerTransform; playerModel=data.PlayerModel;
			animationKey=data.AnimationKey; animator=data.Animator;
			slotIndex=data.SlotIndex; slotSide=data.SlotSide;
			playerMovement=playerTransform.GetComponent<UnitMovement>();
			positionOffset=data.PositionOffset; rotationOffset=data.RotationOffset; scaleOffset=data.ScaleOffset;
			useWorldIdle=data.UseWorldIdle; idleReference=data.IdleReference;
			ResetIdleLoopState();
			targetPos=useWorldIdle ? GetIdleWorldPosition() : (target!=null ? target.position : transform.position);
			ApplyOffsets();
			if (useWorldIdle && idleReference!=null) cachedIdleLocalPosition=idleReference.InverseTransformPoint(transform.position);
			if (chainDeformer!=null) chainDeformer.SetEndAnchor(transform);
			CacheAnimationSettings();
		}

		private void CacheAnimationSettings()
		{
			// Native 0x144b8c0: retain authored phase settings, including defaults when no asset exists.
			var settings=animationSettings;
			cachedAnimationDurationCap=settings ? settings.AnimationDurationCap : DefaultAnimationDurationCap;
			cachedOvertravelRatio=settings ? settings.OvertravelRatio : 0;
			cachedOvertravelDirectionAngle=settings ? settings.OvertravelDirectionAngle : 0;
			cachedAngleOffset=settings ? settings.AngleOffset : 0;
			cachedWindupRatio=settings ? settings.WindupRatio : 1f/3;
			cachedTravelRatio=settings ? settings.TravelRatio : 1f/3;
			cachedReturnRatio=settings ? settings.ReturnRatio : 1f/3;
			cachedWindupOffset=settings ? settings.WindupOffset : DefaultWindupOffset;
			cachedWindupOffsetSpace=settings ? settings.WindupOffsetSpace : 0;
			cachedWindupPose=settings ? settings.Windup : new AttackPoseSettings {ScaleMultiplier=One};
			cachedHitPose=settings ? settings.Hit : new AttackPoseSettings {ScaleMultiplier=One};
			cachedOvertravelPose=settings ? settings.Overtravel : cachedHitPose;
			cachedAttackTargetOffset=settings ? settings.AttackTargetPositionOffset : Vector3.zero;
			cachedTravelEndPositionOffset=settings ? settings.TravelEndPositionOffset : Vector3.zero;
			cachedWeaponTopAxisLocal=settings ? LocalAxis(settings.WeaponTopAxis, Vector3.up) : Vector3.up;
			cachedWeaponBladeAxisLocal=settings ? LocalAxis(settings.WeaponBladeAxis, Vector3.forward) : Vector3.forward;
			if (!settings) {
			    for(int i=0;i<4;i++) {cachedPhaseEase[i]=Ease.Linear;cachedPhaseScaleEase[i]=Ease.Linear;cachedPhaseRotationEase[i]=Ease.Linear;cachedPhaseTravelType[i]=WeaponTravelType.CircularArc;cachedPhasePathFormula[i]=WeaponPathFormula.Circular;cachedPhaseStraightening[i]=1;}
			    return;
			}
			cachedPhaseEase=new[]{settings.WindupEase,settings.TravelEase,settings.OvertravelEase,settings.ReturnEase};
			cachedPhaseScaleEase=new[]{settings.WindupScaleEase,settings.TravelScaleEase,settings.OvertravelScaleEase,settings.ReturnScaleEase};
			cachedPhaseRotationEase=new[]{settings.WindupRotationEase,settings.TravelRotationEase,settings.OvertravelRotationEase,settings.ReturnRotationEase};
			cachedPhaseTravelType=new[]{settings.WindupTravelType,settings.TravelTravelType,settings.OvertravelTravelType,settings.ReturnTravelType};
			cachedPhasePathFormula=new[]{settings.WindupPathFormula,settings.TravelPathFormula,settings.OvertravelPathFormula,settings.ReturnPathFormula};
			cachedPhaseStraightening=new[]{settings.WindupStraightening,settings.TravelStraightening,settings.OvertravelStraightening,settings.ReturnStraightening};
		}

		private void Update()
		{
			// Native 0x144c3f4, bounded ASM; damage is emitted by PlayerAttackSystem.
			if(model==null || model.ItemModel==null || IsExternallyAnimated) return;
			ItemModel item=model.ItemModel;
			if(item.JustStartedAttack) BeginAttackCycle();
			UpdateTargetPosition();TriggerAttackAnimation();UpdateFollowedVfx();
			if(UsesCharacterAnimator) {ResetToIdlePosition();return;}
			if(TryReturnToIdleAfterTargetLost()) return;
			float cooldown=item.Cooldown;
			float backPoint=item.Config.AnimationBackPoint*item.Config.AttackCooldown;
			if(cooldown<=backPoint || cooldown<=0 || (!hasTarget && chainDeformer!=null)) {
			    currentAttackPhase=WeaponAttackPhase.None;ResetToIdlePosition();
			} else PerformAttackAnimation(cooldown);
			if(chainDeformer!=null) chainDeformer.SetIdleState(cooldown<=backPoint || currentAttackPhase==WeaponAttackPhase.None);
		}

		private void UpdateTargetPosition()
		{
			var visualTarget=playerMovement.VisualTarget;
			if(visualTarget!=null) {
			    hasTarget=true;currentTargetTransform=visualTarget.transform;
			    lastTargetPosition=visualTarget.VisualRoot.TransformPoint(cachedAttackTargetOffset)+hitOffset;
			} else {hasTarget=false;currentTargetTransform=null;}
		}

		private void TriggerAttackAnimation()
		{
			if(animator==null || animationKey==string.Empty || !model.ItemModel.JustStartedAttack) return;
			bool useConfiguredType=attackType!=WeaponAttackType.Cut || playerModel==null || ItemSlotKeys.EquippedWeaponCount(playerModel)<2 || !ItemSlotKeys.HasChainWeaponEquipped(playerModel);
			animator.SetFloat(AttackType,useConfiguredType ? (float)attackType : 1);
			animator.SetTrigger(animationKey);
		}

		private bool TryReturnToIdleAfterTargetLost()
		{
			if(chainDeformer==null) return false;
			if(hasTarget) {isReturningToIdleAfterTargetLost=false;return false;}
			if(!isReturningToIdleAfterTargetLost) {
			    if(model.ItemModel.Cooldown<=0 || currentAttackPhase==WeaponAttackPhase.None) return false;
			    targetLostReturnElapsed=0;targetLostReturnStartPosition=transform.position;targetLostReturnStartRotation=transform.rotation;targetLostReturnStartScale=transform.localScale;
			    isReturningToIdleAfterTargetLost=true;CancelScheduledVfxSpawns();
			}
			targetLostReturnElapsed+=Time.deltaTime;currentAttackPhase=WeaponAttackPhase.Return;
			float progress=targetLostReturnElapsed/TargetLostReturnDuration;
			SetAttackPosition(Vector3.LerpUnclamped(targetLostReturnStartPosition,GetIdleWorldPosition(),progress));
			transform.rotation=Quaternion.SlerpUnclamped(targetLostReturnStartRotation,GetIdleWorldRotation(),progress);
			transform.localScale=Vector3.LerpUnclamped(targetLostReturnStartScale,Vector3.Scale(GetIdleWorldScale(),ScaleMultiplier),progress);
			if(targetLostReturnElapsed<TargetLostReturnDuration) return true;
			currentAttackPhase=WeaponAttackPhase.None;isReturningToIdleAfterTargetLost=false;
			SetAttackPosition(GetIdleWorldPosition());transform.rotation=GetIdleWorldRotation();transform.localScale=Vector3.Scale(GetIdleWorldScale(),ScaleMultiplier);
			return false;
		}

		private void PerformAttackAnimation(float cooldown)
		{
			// Native 0x144d2f8. Timings use the item asset's cooldown, not wall-clock duration.
			TimingData timing=GetTimingData(model.ItemModel.Config.AttackCooldown);
			float elapsed=Mathf.Clamp(timing.AttackCooldown-cooldown,0,timing.AttackCooldown);
			if(elapsed>=timing.VisualDuration) {currentAttackPhase=WeaponAttackPhase.None;ResetToIdlePosition();return;}
			if(currentAttackPhase==WeaponAttackPhase.None) BeginAttackCycle();
			BeginDetachedAttack();
			returnTargetPoint=GetIdleWorldPosition();strikeTargetPoint=lastTargetPosition;
			castTargetPoint=ComputeWindupPoint();travelEndPoint=ComputeTravelEndPoint();
			if(!attackVfxSpawnedForCurrentSwing && hasTarget && attackVfx.Count>0) {
			    float hitTime=HasOvertravelState ? timing.HitTime : GetVfxSpawnDelay(timing,attackVfx[0]);
			    if(elapsed>=hitTime) {attackVfxSpawnedForCurrentSwing=true;SpawnAttackVfxOnHit();}
			}
			if(elapsed<=timing.WindupEnd) {
			    float progress=timing.WindupEnd==0 ? 0 : Mathf.Clamp01(elapsed/Mathf.Max(.0001f,timing.WindupEnd));
			    float movement=EaseValue(progress,GetPhaseEase(WeaponAttackPhase.Windup));
			    ArcFrame frame=EvaluateArcFrame(useWorldIdle ? attackStartWorldPosition : returnTargetPoint,castTargetPoint,movement,1,cachedAngleOffset,WeaponAttackPhase.Windup);
			    Vector3 scale=Vector3.Lerp(attackStartScale,cachedWindupPose.ScaleMultiplier,EaseValue(progress,GetPhaseScaleEase(WeaponAttackPhase.Windup)));
			    returnRotationCaptured=false;currentAttackPhase=WeaponAttackPhase.Windup;
			    ApplyAttackTransform(frame.Position,GetWindupRotation(movement,EaseValue(progress,GetPhaseRotationEase(WeaponAttackPhase.Windup))),scale);
			    return;
			}
			if(elapsed>timing.TravelEnd) {
			    if(!returnRotationCaptured) {
			        if(model.ItemModel.Config.DamageType==Codebase.Enemy.WeaponType.Cutting) {returnStartPosition=transform.position;returnStartRotation=transform.rotation;returnStartScale=transform.localScale;}
			        else {
			            float end=HasOvertravelState ? StrikeTravelT : 1;
			            ArcFrame frame=EvaluateTravelArcFrame(GetTravelPositionT(end));
			            returnStartPosition=frame.Position;
			            if(chainDeformer!=null) returnStartPosition+=GetTargetBasisRotation(strikeTargetPoint)*cachedTravelEndPositionOffset*end;
			            returnStartRotation=ApplyArcRotationCompensation(GetTravelRotation(end),frame,end);returnStartScale=GetTravelScale(end);
			        }
			        returnRotationCaptured=true;
			    }
			    float progress=timing.TravelEnd==timing.VisualDuration ? 0 : Mathf.Clamp01((elapsed-timing.TravelEnd)/(timing.VisualDuration-timing.TravelEnd));
			    float returnProgress=Mathf.Clamp01((progress-ReturnHoldRatio)/(1-ReturnHoldRatio));
			    float movement=EaseValue(returnProgress,GetPhaseEase(WeaponAttackPhase.Return));
			    Vector3 position=returnStartPosition;
			    if(progress>ReturnHoldRatio) position=EvaluateArcFrame(returnStartPosition,returnTargetPoint,movement,1,cachedAngleOffset,WeaponAttackPhase.Return).Position;
			    currentAttackPhase=WeaponAttackPhase.Return;
			    ApplyAttackTransform(position,SlerpShortest(returnStartRotation,GetIdleWorldRotation(),EaseValue(returnProgress,GetPhaseRotationEase(WeaponAttackPhase.Return))),Vector3.Lerp(returnStartScale,GetIdleWorldScale(),EaseValue(returnProgress,GetPhaseScaleEase(WeaponAttackPhase.Return))));
			    return;
			}
			float travelT=timing.WindupEnd==Mathf.Max(timing.WindupEnd+.0001f,timing.TravelEnd) ? 0 : Mathf.Clamp01((elapsed-timing.WindupEnd)/(Mathf.Max(timing.WindupEnd+.0001f,timing.TravelEnd)-timing.WindupEnd));
			ArcFrame travelFrame=EvaluateTravelArcFrame(GetTravelPositionT(travelT));
			returnRotationCaptured=false;
			currentAttackPhase=HasOvertravelState && travelT>StrikeTravelT ? WeaponAttackPhase.Overtravel : WeaponAttackPhase.Travel;
			Vector3 travelPosition=travelFrame.Position;
			if(chainDeformer!=null) travelPosition+=GetTargetBasisRotation(strikeTargetPoint)*cachedTravelEndPositionOffset*travelT;
			ApplyAttackTransform(travelPosition,ApplyArcRotationCompensation(GetTravelRotation(travelT),travelFrame,travelT),GetTravelScale(travelT));
		}

		private TimingData GetTimingData(float attackCooldown)
		{
			float hitTime=(1-PlayerAttackSystem.GetAnimationCastPoint(model.ItemModel))*attackCooldown;
			float windupEnd=cachedWindupRatio*hitTime/(cachedWindupRatio+cachedTravelRatio);
			float travelEnd=HasOvertravelState ? windupEnd+(hitTime-windupEnd)*(Mathf.Max(0,cachedOvertravelRatio)+1) : hitTime;
			cachedTimingData=new TimingData {AttackCooldown=attackCooldown,VisualDuration=(1-model.ItemModel.Config.AnimationBackPoint)*attackCooldown,HitTime=hitTime,WindupEnd=windupEnd,TravelEnd=travelEnd};
			return cachedTimingData;
		}

		private Vector3 ComputeWindupPoint()
		{
			Vector3 offset=cachedWindupOffset;
			if((int)slotSide==1) offset.x=-offset.x;
			Vector3 origin=(int)cachedWindupOffsetSpace==1 ? GetIdleWorldPosition() : returnTargetPoint;
			Quaternion basis=(int)cachedWindupOffsetSpace==1 ? GetPlayerBasisRotation() : GetTargetBasisRotation(origin);
			return origin+basis*new Vector3(offset.x,0,offset.z)+Vector3.up*offset.y;
		}

		private Vector3 ComputeTravelEndPoint()
		{
			Vector3 result=EvaluateArcFrame(castTargetPoint,strikeTargetPoint,1,GetTravelTMax(),cachedAngleOffset,WeaponAttackPhase.Travel).Position;
			if(chainDeformer!=null) result+=GetTargetBasisRotation(strikeTargetPoint)*cachedTravelEndPositionOffset;
			return result;
		}

		private Vector3 EvaluateTravelArc(float t)
		{
			return EvaluateTravelArcFrame(t).Position;
		}

		private ArcFrame EvaluateTravelArcFrame(float t)
		{
			return EvaluateArcFrame(castTargetPoint,strikeTargetPoint,t,GetTravelTMax(),cachedAngleOffset,WeaponAttackPhase.Travel);
		}

		private float GetTravelTMax()
		{
			return 1+Mathf.Max(0,cachedOvertravelRatio);
		}

		private ArcFrame EvaluateArcFrame(Vector3 from, Vector3 to, float t, float paramTMax = 1f)
		{
			return EvaluateArcFrame(from,to,t,paramTMax,cachedAngleOffset,WeaponAttackPhase.Travel);
		}

		private ArcFrame EvaluateArcFrame(Vector3 from, Vector3 to, float t, float paramTMax, float angleOffset)
		{
			return EvaluateArcFrame(from,to,t,paramTMax,angleOffset,WeaponAttackPhase.Travel);
		}

		private ArcFrame EvaluateArcFrame(Vector3 from, Vector3 to, float t, float paramTMax, float angleOffset, WeaponAttackPhase phase)
		{
			// Native 0x144e7e0: native phase straightening is a blend FROM straight TO curved.
			t=Mathf.Clamp01(t);paramTMax=Mathf.Max(.0001f,paramTMax);
			Vector3 direction=to-from;float distance=direction.magnitude;
			if(distance<.0001f) return new ArcFrame {Position=from,Tangent=Vector3.forward,Normal=Vector3.up};
			Vector3 normal=GetArcNormal(from,to,angleOffset).normalized;
			float u=t*paramTMax;
			ArcFrame curved=GetPhaseCurveFrame(from,to,direction,distance,normal,u,paramTMax,phase);
			if(u<=1 || cachedOvertravelRatio<=.0001f) {
			    ArcFrame straight=new ArcFrame {Position=from+direction*u,Tangent=direction.normalized,Normal=normal};
			    return BlendStraightFrame(straight,curved,GetPhaseStraightening(phase));
			}
			ArcFrame end=GetPhaseCurveFrame(from,to,direction,distance,normal,1,paramTMax,WeaponAttackPhase.Overtravel);
			float progress=Mathf.Clamp01((u-1)/Mathf.Max(.0001f,paramTMax-1));
			Vector3 axis=Vector3.Cross(direction,normal);
			if(axis.sqrMagnitude<.0001f) axis=Vector3.up;
			axis.Normalize();
			Quaternion full=Quaternion.AngleAxis(cachedOvertravelDirectionAngle,axis);
			Quaternion partial=Quaternion.AngleAxis(progress*cachedOvertravelDirectionAngle,axis);
			Vector3 straightTangent=full*end.Tangent;
			if(straightTangent.sqrMagnitude<.0001f) straightTangent=end.Tangent;
			straightTangent.Normalize();
			Vector3 curvedTangent=partial*curved.Tangent;
			if(curvedTangent.sqrMagnitude<.0001f) curvedTangent=curved.Tangent;
			curvedTangent.Normalize();
			float blend=Mathf.Clamp01(GetPhaseStraightening(WeaponAttackPhase.Overtravel));
			Vector3 tangent=Vector3.Lerp(straightTangent,curvedTangent,blend);
			if(tangent.sqrMagnitude<.0001f) tangent=straightTangent;
			return new ArcFrame {
			    Position=Vector3.Lerp(to+straightTangent*distance*(u-1),to+partial*(curved.Position-to),blend),
			    Tangent=tangent.normalized,Normal=normal
			};
		}

		private ArcFrame EvaluateSineFrameByU(Vector3 from, Vector3 direction, float distance, Vector3 normal, float u, float paramTMax)
		{
			Vector3 projected=Vector3.ProjectOnPlane(normal,direction);
			if(projected.sqrMagnitude<.0001f) projected=normal;
			projected.Normalize();
			float angle=u*Mathf.PI;
			Vector3 derivative=(direction+projected*(distance*.5f*Mathf.PI*Mathf.Cos(angle)))*Mathf.Max(.0001f,paramTMax);
			if(derivative.sqrMagnitude<.0001f) derivative=direction;
			return new ArcFrame {Position=from+direction*u+projected*(distance*.5f*Mathf.Sin(angle)),Tangent=derivative.normalized,Normal=normal};
		}

		private ArcFrame EvaluateCircularFrameByU(Vector3 from, Vector3 to, Vector3 normal, float u, float paramTMax)
		{
			Vector3 center=(from+to)*.5f;
			Vector3 radial=(from-center).normalized;
			Vector3 perpendicular=Vector3.ProjectOnPlane(normal,radial);
			if(perpendicular.sqrMagnitude<.0001f) perpendicular=Vector3.Cross(Vector3.Cross(radial,Vector3.up),radial);
			perpendicular.Normalize();
			float radius=Vector3.Distance(from,to)*.5f;
			float angle=u*Mathf.PI;
			float sine=Mathf.Sin(angle),cosine=Mathf.Cos(angle);
			Vector3 tangent=(perpendicular*cosine-radial*sine)*(Mathf.Max(.0001f,paramTMax)*radius*Mathf.PI);
			if(tangent.sqrMagnitude<.0001f) tangent=to-from;
			return new ArcFrame {Position=center+radial*(cosine*radius)+perpendicular*(sine*radius),Tangent=tangent.normalized,Normal=normal};
		}

		private ArcFrame GetPhaseCurveFrame(Vector3 from, Vector3 to, Vector3 direction, float distance, Vector3 normal, float u, float paramTMax, WeaponAttackPhase phase)
		{
			if(GetPhaseTravelType(phase)==WeaponTravelType.Straight) return new ArcFrame {Position=from+direction*u,Tangent=direction.normalized,Normal=normal};
			return GetPhasePathFormula(phase)==WeaponPathFormula.Sine ? EvaluateSineFrameByU(from,direction,distance,normal,u,paramTMax) : EvaluateCircularFrameByU(from,to,normal,u,paramTMax);
		}

		private ArcFrame BlendStraightFrame(ArcFrame straightFrame, ArcFrame curvedFrame, float blend)
		{
			blend=Mathf.Clamp01(blend);
			Vector3 tangent=Vector3.Lerp(straightFrame.Tangent,curvedFrame.Tangent,blend);
			if(tangent.sqrMagnitude<.0001f) tangent=straightFrame.Tangent;
			return new ArcFrame {Position=Vector3.Lerp(straightFrame.Position,curvedFrame.Position,blend),Tangent=tangent.normalized,Normal=curvedFrame.Normal};
		}

		private Vector3 GetArcNormal(Vector3 from, Vector3 to)
		{
			return GetArcNormal(from,to,cachedAngleOffset);
		}

		private Vector3 GetArcNormal(Vector3 from, Vector3 to, float angleOffset)
		{
			Vector3 direction=to-from;
			if(direction.sqrMagnitude<.0001f) return Vector3.up;
			return Quaternion.AngleAxis(angleOffset,direction.normalized*((int)slotSide==1 ? -1 : 1))*Vector3.up;
		}

		private Quaternion ApplyArcRotationCompensation(Quaternion baseRotation, ArcFrame currentFrame, float progress)
		{
			ArcFrame reference=EvaluateArcFrame(castTargetPoint,strikeTargetPoint,progress,GetTravelTMax(),cachedAngleOffset,WeaponAttackPhase.Travel);
			return BuildFrameRotation(currentFrame)*Quaternion.Inverse(BuildFrameRotation(reference))*baseRotation;
		}

		private Quaternion BuildFrameRotation(ArcFrame frame)
		{
			Vector3 normal=frame.Normal.sqrMagnitude<.0001f ? Vector3.up : frame.Normal.normalized;
			Vector3 direction=Vector3.ProjectOnPlane(frame.Tangent,normal);
			if(direction.sqrMagnitude<.0001f) direction=Vector3.ProjectOnPlane(Vector3.forward,normal);
			if(direction.sqrMagnitude<.0001f) direction=Vector3.right;
			return Quaternion.LookRotation(direction.normalized,normal);
		}

		private Quaternion SlerpShortest(Quaternion from, Quaternion to, float t)
		{
			if(Quaternion.Dot(from,to)<0) to=new Quaternion(-to.x,-to.y,-to.z,-to.w);
			return Quaternion.Slerp(from,to,t);
		}

		private Quaternion SlerpBySlot(Quaternion from, Quaternion to, float t)
		{
			// Native 0x144fe24 is a bounded tail branch to SlerpShortest (0x1451324).
			return SlerpShortest(from,to,t);
		}

		private Quaternion MirrorRotationByPlane(Quaternion rotation, Vector3 planeNormal)
		{
			// Native 0x1451368; bounded ARM64 returns LookRotation(reflected forward, reflected up).
			Vector3 forward=Vector3.Reflect(rotation*Vector3.forward,planeNormal);
			Vector3 up=Vector3.Reflect(rotation*Vector3.up,planeNormal);
			if(forward.sqrMagnitude<.0001f) forward=Vector3.forward;
			if(up.sqrMagnitude<.0001f) up=Vector3.up;
			up.Normalize();
			Vector3 projected=Vector3.ProjectOnPlane(forward,up);
			if(projected.sqrMagnitude>=.0001f) forward=projected;
			return Quaternion.LookRotation(forward.normalized,up.normalized);
		}

		private Vector3 GetWeaponTopAxisLocal()
		{
			return cachedWeaponTopAxisLocal;
		}

		private Vector3 GetWeaponBladeAxisLocal()
		{
			return cachedWeaponBladeAxisLocal;
		}

		private AttackPoseSettings GetWindupPose()
		{
			return cachedWindupPose;
		}

		private AttackPoseSettings GetHitPose()
		{
			return cachedHitPose;
		}

		private AttackPoseSettings GetOvertravelPose()
		{
			return cachedOvertravelPose;
		}

		private float GetTravelPositionT(float travelT)
		{
			if(!HasOvertravelState) return EaseValue(travelT,GetPhaseEase(WeaponAttackPhase.Travel));
			float strike=StrikeTravelT;
			return travelT<=strike ? EaseValue(Mathf.Clamp01(travelT/Mathf.Max(.0001f,strike)),GetPhaseEase(WeaponAttackPhase.Travel))*strike
			    : strike+EaseValue(Mathf.Clamp01((travelT-strike)/Mathf.Max(.0001f,1-strike)),GetPhaseEase(WeaponAttackPhase.Overtravel))*(1-strike);
		}

		private Quaternion GetTravelRotation(float travelT)
		{
			Quaternion from,to;WeaponAttackPhase phase;float progress;
			if(!HasOvertravelState) {
			    from=GetTargetRelativeAttackRotation(castTargetPoint,cachedWindupPose.RotationOffset);
			    to=GetTargetRelativeAttackRotation(travelEndPoint,cachedHitPose.RotationOffset);
			    phase=WeaponAttackPhase.Travel;progress=travelT;
			} else if(travelT<=StrikeTravelT) {
			    from=GetTargetRelativeAttackRotation(castTargetPoint,cachedWindupPose.RotationOffset);
			    to=GetTargetRelativeAttackRotation(strikeTargetPoint,cachedHitPose.RotationOffset);
			    phase=WeaponAttackPhase.Travel;progress=travelT/Mathf.Max(.0001f,StrikeTravelT);
			} else {
			    from=GetTargetRelativeAttackRotation(strikeTargetPoint,cachedHitPose.RotationOffset);
			    to=GetTargetRelativeAttackRotation(travelEndPoint,cachedOvertravelPose.RotationOffset);
			    phase=WeaponAttackPhase.Overtravel;progress=(travelT-StrikeTravelT)/Mathf.Max(.0001f,1-StrikeTravelT);
			}
			return SlerpShortest(from,to,EaseValue(progress,GetPhaseRotationEase(phase)));
		}

		private Vector3 GetTravelScale(float travelT)
		{
			if(!HasOvertravelState) return Vector3.Lerp(cachedWindupPose.ScaleMultiplier,cachedHitPose.ScaleMultiplier,EaseValue(travelT,GetPhaseScaleEase(WeaponAttackPhase.Travel)));
			if(travelT<=StrikeTravelT) return Vector3.Lerp(cachedWindupPose.ScaleMultiplier,cachedHitPose.ScaleMultiplier,EaseValue(travelT/Mathf.Max(.0001f,StrikeTravelT),GetPhaseScaleEase(WeaponAttackPhase.Travel)));
			return Vector3.Lerp(cachedHitPose.ScaleMultiplier,cachedOvertravelPose.ScaleMultiplier,EaseValue((travelT-StrikeTravelT)/Mathf.Max(.0001f,1-StrikeTravelT),GetPhaseScaleEase(WeaponAttackPhase.Overtravel)));
		}

		private Vector3 GetTravelEndScale()
		{
			return HasOvertravelState ? cachedOvertravelPose.ScaleMultiplier : cachedHitPose.ScaleMultiplier;
		}

		private Ease GetPhaseEase(WeaponAttackPhase phase)
		{
			int index=(int)phase-1;
			return (uint)index<4 ? cachedPhaseEase[index] : Ease.Linear;
		}

		private Ease GetPhaseScaleEase(WeaponAttackPhase phase)
		{
			int index=(int)phase-1;
			return (uint)index<4 ? cachedPhaseScaleEase[index] : Ease.Linear;
		}

		private Ease GetPhaseRotationEase(WeaponAttackPhase phase)
		{
			int index=(int)phase-1;
			return (uint)index<4 ? cachedPhaseRotationEase[index] : Ease.Linear;
		}

		private WeaponTravelType GetPhaseTravelType(WeaponAttackPhase phase)
		{
			int index=(int)phase-1;
			return (uint)index<4 ? cachedPhaseTravelType[index] : WeaponTravelType.CircularArc;
		}

		private WeaponPathFormula GetPhasePathFormula(WeaponAttackPhase phase)
		{
			int index=(int)phase-1;
			return (uint)index<4 ? cachedPhasePathFormula[index] : WeaponPathFormula.Circular;
		}

		private float GetPhaseStraightening(WeaponAttackPhase phase)
		{
			int index=(int)phase-1;
			return (uint)index<4 ? cachedPhaseStraightening[index] : 1;
		}

		private float EaseValue(float t, Ease ease)
		{
			return DOVirtual.EasedValue(0,1,Mathf.Clamp01(t),ease);
		}

		private void SetAttackPosition(Vector3 desiredPosition)
		{
			targetPos=desiredPosition;transform.position=targetPos;
		}

		private void ApplyAttackTransform(Vector3 desiredPosition, Quaternion desiredRotation, Vector3 scaleMultiplier)
		{
			SetAttackPosition(desiredPosition);transform.rotation=desiredRotation;
			transform.localScale=Vector3.Scale(scaleMultiplier,ScaleMultiplier);
		}

		private Quaternion GetTargetRelativeAttackRotation(Vector3 desiredPosition, Vector3 euler)
		{
			return GetTargetBasisRotation(desiredPosition)*Quaternion.Euler(euler);
		}

		private Quaternion GetSlotMirroredAttackRotation(Vector3 euler)
		{
			return Quaternion.Euler(euler);
		}

		private Quaternion GetIdleLocalRotation()
		{
			return Quaternion.Euler(rotationOffset);
		}

		private Quaternion GetIdleWorldRotation()
		{
			return useWorldIdle ? GetIdleBaseWorldRotation()*idleLoopRotationOffset : (target!=null ? target.rotation*Quaternion.Euler(rotationOffset) : transform.rotation);
		}

		private Vector3 GetIdleWorldPosition()
		{
			return useWorldIdle ? GetIdleBaseWorldPosition()+GetIdleLoopWorldOffset() : (target!=null ? target.TransformPoint(positionOffset) : transform.position);
		}

		private Vector3 GetIdleWorldScale()
		{
			return useWorldIdle ? (idleReference!=null ? idleReference.localScale : transform.localScale) : scaleOffset;
		}

		private Vector3 GetIdleBaseWorldPosition()
		{
			return idleReference!=null ? idleReference.TransformPoint(cachedIdleLocalPosition) : transform.position;
		}

		private Quaternion GetIdleBaseWorldRotation()
		{
			return idleReference!=null ? idleReference.rotation : transform.rotation;
		}

		private Vector3 GetIdleLoopWorldOffset()
		{
			return idleReference!=null ? idleReference.rotation*idleLoopOffsetLocal : idleLoopOffsetLocal;
		}

		private void ResetIdleLoopState()
		{
			idleLoopOffsetLocal=Vector3.zero;idleLoopTargetOffsetLocal=Vector3.zero;
			idleLoopHasTargetOffset=false;idleLoopReturnToBase=false;
			idleLoopRotationOffset=Quaternion.identity;idleLoopRotationTarget=Quaternion.identity;idleLoopHasRotationTarget=false;
		}

		private void UpdateIdleLoopState()
		{
			// Native 0x1451b8c: alternate shell targets and the base, with bounded original speeds.
			if(!useWorldIdle || idleAnimationSettings==null || !idleAnimationSettings.EnableLoop) {ResetIdleLoopState();return;}
			float outer=Mathf.Max(0,idleAnimationSettings.PositionOuterRadius);
			float inner=Mathf.Clamp(idleAnimationSettings.PositionInnerRadius,0,outer);
			if(outer<=0) idleLoopOffsetLocal=Vector3.zero;
			else {
			    if(!idleLoopHasTargetOffset) {idleLoopTargetOffsetLocal=idleLoopReturnToBase ? Vector3.zero : PickIdleShellOffsetLocal(outer,inner);idleLoopHasTargetOffset=true;}
			    idleLoopOffsetLocal=Vector3.MoveTowards(idleLoopOffsetLocal,idleLoopTargetOffsetLocal,Mathf.Max(.01f,idleAnimationSettings.PositionLoopSpeed)*Time.deltaTime);
			    if((idleLoopOffsetLocal-idleLoopTargetOffsetLocal).sqrMagnitude<=.0004f) {idleLoopHasTargetOffset=false;idleLoopReturnToBase=!idleLoopReturnToBase;}
			}
			if(idleAnimationSettings.EnableRotationLoop) {
			    if(!idleLoopHasRotationTarget) {
			        Vector3 min=idleAnimationSettings.RotationMinEuler,max=idleAnimationSettings.RotationMaxEuler;
			        idleLoopRotationTarget=Quaternion.Euler(UnityEngine.Random.Range(min.x,max.x),UnityEngine.Random.Range(min.y,max.y),UnityEngine.Random.Range(min.z,max.z));
			        idleLoopHasRotationTarget=true;
			    }
			    idleLoopRotationOffset=Quaternion.RotateTowards(idleLoopRotationOffset,idleLoopRotationTarget,Mathf.Max(1,idleAnimationSettings.RotationLoopSpeed)*Time.deltaTime);
			    if(Quaternion.Angle(idleLoopRotationOffset,idleLoopRotationTarget)<=.35f) idleLoopHasRotationTarget=false;
			} else {idleLoopRotationOffset=Quaternion.identity;idleLoopRotationTarget=Quaternion.identity;idleLoopHasRotationTarget=false;}
		}

		private Vector3 PickIdleShellOffsetLocal(float outerRadius, float innerRadius)
		{
			for(int i=0;i<16;i++) {Vector3 candidate=UnityEngine.Random.insideUnitSphere*outerRadius;if(candidate.sqrMagnitude>=innerRadius*innerRadius) return candidate;}
			Vector3 direction=UnityEngine.Random.onUnitSphere;
			return direction.sqrMagnitude<.0001f ? Vector3.up*innerRadius : direction*Mathf.Lerp(innerRadius,outerRadius,UnityEngine.Random.value);
		}

		private float GetIdlePositionLerpSpeed()
		{
			return useWorldIdle && idleAnimationSettings!=null ? Mathf.Lerp(40,2,Mathf.Clamp01(idleAnimationSettings.PositionLag)) : 30;
		}

		private float GetIdleRotationLerpSpeed()
		{
			return useWorldIdle && idleAnimationSettings!=null ? Mathf.Lerp(40,2,Mathf.Clamp01(idleAnimationSettings.RotationLag)) : 30;
		}

		private Quaternion GetPlayerBasisRotation()
		{
			if(playerTransform==null) return Quaternion.identity;
			Vector3 direction=playerTransform.forward;direction.y=0;
			if(direction.sqrMagnitude<.0001f) direction=Vector3.forward;
			return Quaternion.LookRotation(direction.normalized,Vector3.up);
		}

		private Quaternion GetTargetBasisRotation(Vector3 origin)
		{
			Vector3 direction=strikeTargetPoint-returnTargetPoint;direction.y=0;
			if(direction.sqrMagnitude<.0001f) {direction=strikeTargetPoint-origin;direction.y=0;}
			if(direction.sqrMagnitude<.0001f && playerTransform!=null) {direction=playerTransform.forward;direction.y=0;}
			if(direction.sqrMagnitude<.0001f) direction=Vector3.forward;
			return Quaternion.LookRotation(direction.normalized,Vector3.up);
		}

		private void BeginAttackCycle()
		{
			isReturningToIdleAfterTargetLost=false;attackVfxSpawnedForCurrentSwing=false;
			if(useWorldIdle) {attackStartWorldPosition=transform.position;attackStartWorldRotation=transform.rotation;attackStartScale=transform.localScale;returnRotationCaptured=false;}
			CancelScheduledVfxSpawns();
		}

		private void SpawnAttackVfxOnHit()
		{
			if(attackVfx==null || attackVfx.Count==0 || model==null || model.ItemModel==null) return;
			foreach(var settings in attackVfx) {
			    if(settings.Prefab==null) continue;
			    WeaponAttackPhase phase=HasOvertravelState || settings.SpawnPhase==WeaponAttackPhase.Overtravel ? WeaponAttackPhase.Travel : settings.SpawnPhase;
			    SpawnAttackVfx(settings,phase,HasOvertravelState ? 1 : settings.PhaseProgress);
			}
		}

		private void ScheduleAttackVfxSpawns()
		{
			if(attackVfx==null || attackVfx.Count==0 || model==null || model.ItemModel==null) return;
			TimingData timing=GetTimingData(model.ItemModel.Config.AttackCooldown);
			foreach(var settings in attackVfx) if(settings.Prefab!=null) scheduledVfxCoroutines.Add(StartCoroutine(SpawnAttackVfxDelayed(settings,GetVfxSpawnDelay(timing,settings))));
		}

		
		private IEnumerator SpawnAttackVfxDelayed(WeaponAttackVfxSettings settings, float delay)
		{
			yield return new WaitForSeconds(Mathf.Max(0,delay));
			if(model!=null && model.ItemModel!=null && model.ItemModel.Cooldown>0) {
			    WeaponAttackPhase phase=settings.SpawnPhase==WeaponAttackPhase.Overtravel && !HasOvertravelState ? WeaponAttackPhase.Travel : settings.SpawnPhase;
			    SpawnAttackVfx(settings,phase,1);
			}
		}

		private float GetVfxSpawnDelay(TimingData timing, WeaponAttackVfxSettings settings)
		{
			float travel=Mathf.Max(0,timing.TravelEnd-timing.WindupEnd);
			switch(settings.SpawnPhase) {
			case WeaponAttackPhase.Windup:return Mathf.Max(0,timing.WindupEnd)*settings.PhaseProgress;
			case WeaponAttackPhase.Travel:return timing.WindupEnd+travel*(HasOvertravelState ? settings.PhaseProgress/(1+Mathf.Max(0,cachedOvertravelRatio)) : settings.PhaseProgress);
			case WeaponAttackPhase.Overtravel:
			    return HasOvertravelState ? timing.WindupEnd+travel*(StrikeTravelT+(1-StrikeTravelT)*Mathf.Clamp01(settings.PhaseProgress)) : timing.TravelEnd;
			case WeaponAttackPhase.Return:return timing.TravelEnd+Mathf.Max(0,timing.VisualDuration-timing.TravelEnd)*settings.PhaseProgress;
			default:return timing.VisualDuration;
			}
		}

		private void CancelScheduledVfxSpawns()
		{
			foreach(Coroutine coroutine in scheduledVfxCoroutines) if(coroutine!=null) StopCoroutine(coroutine);
			scheduledVfxCoroutines.Clear();
		}

		private void SpawnAttackVfx(WeaponAttackVfxSettings settings, WeaponAttackPhase phase, float phaseProgress)
		{
			Quaternion basis=GetVfxBaseRotation(settings.OffsetSpace,phase,phaseProgress);
			Vector3 position=GetVfxBasePosition(settings.OffsetSpace,phase,phaseProgress)+basis*settings.PositionOffset;
			Quaternion rotation=GetVfxOffsetRotation(settings.OffsetSpace,basis,settings.RotationOffset);
			Transform instance=Codebase.Utilities.ObjectPool.ObjectPoolUtility<Transform>.Spawn(settings.Prefab,position,rotation);
			instance.localScale=settings.ScaleOffset==Vector3.zero ? Vector3.one : settings.ScaleOffset;
			float lifetime=Mathf.Max(.01f,settings.Lifetime>0 ? settings.Lifetime : 3);
			StartCoroutine(DespawnVfx(instance,lifetime));
			if(settings.FollowWeaponUntilPhaseEndOrVfxEnd) activeFollowVfx.Add(new FollowVfxRuntime {Instance=instance,Phase=phase,OffsetSpace=settings.OffsetSpace,PositionOffset=settings.PositionOffset,RotationOffset=settings.RotationOffset,EndTime=Time.time+lifetime});
		}

		private Vector3 GetVfxBasePosition(WeaponAttackVfxOffsetSpace offsetSpace, WeaponAttackPhase phase, float phaseProgress)
		{
			if((int)offsetSpace==2 && playerTransform!=null) return playerTransform.position;
			if((int)offsetSpace==1) return currentTargetTransform!=null ? currentTargetTransform.position : strikeTargetPoint;
			return GetPhasePosition(phase,phaseProgress);
		}

		private Quaternion GetVfxBaseRotation(WeaponAttackVfxOffsetSpace offsetSpace, WeaponAttackPhase phase, float phaseProgress)
		{
			if((int)offsetSpace==2) return GetPlayerBasisRotation();
			if((int)offsetSpace==1) return currentTargetTransform!=null ? currentTargetTransform.rotation : GetTargetBasisRotation(strikeTargetPoint);
			return GetPhaseRotation(phase,phaseProgress);
		}

		private Quaternion GetVfxOffsetRotation(WeaponAttackVfxOffsetSpace offsetSpace, Quaternion baseRotation, Vector3 rotationOffset)
		{
			rotationOffset=GetSlotMirroredVfxRotationOffset(rotationOffset);
			return (int)offsetSpace==0 ? baseRotation*Quaternion.Euler(rotationOffset) : Quaternion.Euler(baseRotation.eulerAngles+rotationOffset);
		}

		private Vector3 GetSlotMirroredVfxRotationOffset(Vector3 rotationOffset)
		{
			if((int)slotSide==1) rotationOffset.x=-rotationOffset.x;
			return rotationOffset;
		}

		private Quaternion GetPhaseRotation(WeaponAttackPhase phase, float phaseProgress)
		{
			phaseProgress=Mathf.Clamp01(phaseProgress);
			if(phase==WeaponAttackPhase.Windup) return GetWindupRotation(EaseValue(phaseProgress,GetPhaseEase(phase)),EaseValue(phaseProgress,GetPhaseRotationEase(phase)));
			if(phase==WeaponAttackPhase.Travel || phase==WeaponAttackPhase.Overtravel) {
			    float progress=HasOvertravelState ? (phase==WeaponAttackPhase.Travel ? phaseProgress*StrikeTravelT : StrikeTravelT+phaseProgress*(1-StrikeTravelT)) : (phase==WeaponAttackPhase.Travel ? phaseProgress : 1);
			    ArcFrame frame=EvaluateTravelArcFrame(GetTravelPositionT(progress));
			    return ApplyArcRotationCompensation(GetTravelRotation(progress),frame,progress);
			}
			if(phase==WeaponAttackPhase.Return) {
			    ArcFrame frame=EvaluateTravelArcFrame(1);
			    Quaternion start=ApplyArcRotationCompensation(GetTravelRotation(1),frame,1);
			    return SlerpShortest(start,GetIdleWorldRotation(),EaseValue(phaseProgress,GetPhaseRotationEase(phase)));
			}
			return transform.rotation;
		}

		private Quaternion GetWindupRotation(float movementT, float rotationT)
		{
			Quaternion result=SlerpShortest(attackStartWorldRotation,GetTargetRelativeAttackRotation(castTargetPoint,cachedWindupPose.RotationOffset),rotationT);
			float progress=Mathf.Clamp01(movementT)*.5f;
			ArcFrame frame=EvaluateArcFrame(castTargetPoint,strikeTargetPoint,progress,GetTravelTMax(),cachedAngleOffset,WeaponAttackPhase.Windup);
			return SlerpShortest(result,ApplyArcRotationCompensation(result,frame,progress),rotationT);
		}

		
		private IEnumerator DespawnVfx(Transform vfx, float delay)
		{
			yield return new WaitForSeconds(delay);
			if(vfx!=null) Codebase.Utilities.ObjectPool.ObjectPoolUtility<Transform>.Despawn(vfx);
		}

		private void UpdateFollowedVfx()
		{
			for(int i=activeFollowVfx.Count-1;i>=0;i--) {
			    FollowVfxRuntime current=activeFollowVfx[i];
			    if(current.Instance==null || current.EndTime<=Time.time || currentAttackPhase!=current.Phase) {activeFollowVfx.RemoveAt(i);continue;}
			    Quaternion rotation;Vector3 position;
			    if((int)current.OffsetSpace==1) {rotation=currentTargetTransform!=null ? currentTargetTransform.rotation : GetTargetBasisRotation(strikeTargetPoint);position=currentTargetTransform!=null ? currentTargetTransform.position : strikeTargetPoint;}
			    else if((int)current.OffsetSpace==2) {rotation=GetPlayerBasisRotation();position=playerTransform!=null ? playerTransform.position : transform.position;}
			    else {rotation=transform.rotation;position=transform.position;}
			    current.Instance.position=position+rotation*current.PositionOffset;
			    current.Instance.rotation=GetVfxOffsetRotation(current.OffsetSpace,rotation,current.RotationOffset);
			}
		}

		private void BeginDetachedAttack()
		{
			if(UsesCharacterAnimator || isDetachedForAttack) return;
			if(useWorldIdle) {
			    if(currentAttackPhase==WeaponAttackPhase.None) {attackStartWorldPosition=transform.position;attackStartWorldRotation=transform.rotation;attackStartScale=transform.localScale;}
			    returnRotationCaptured=false;return;
			}
			originalParent=transform.parent;originalLocalPosition=transform.localPosition;originalLocalScale=transform.localScale;
			attackStartWorldRotation=transform.rotation;returnRotationCaptured=false;
			transform.SetParent(null,true);attackStartScale=transform.localScale;isDetachedForAttack=true;
		}

		private void EndDetachedAttack()
		{
			if(useWorldIdle || !isDetachedForAttack) return;
			if(originalParent!=null) {transform.SetParent(originalParent,true);transform.localPosition=originalLocalPosition;transform.localScale=originalLocalScale;}
			targetPos=transform.position;isDetachedForAttack=false;returnRotationCaptured=false;
		}

		private void ResetFromAttack()
		{
			EndDetachedAttack();returnRotationCaptured=false;
		}

		private void ResetToIdlePosition()
		{
			EndDetachedAttack();CancelScheduledVfxSpawns();returnRotationCaptured=false;
			if(useWorldIdle) {
			    UpdateIdleLoopState();
			    transform.position=Vector3.Lerp(transform.position,GetIdleWorldPosition(),GetIdlePositionLerpSpeed()*Time.deltaTime);
			    transform.rotation=SlerpShortest(transform.rotation,GetIdleWorldRotation(),GetIdleRotationLerpSpeed()*Time.deltaTime);
			    transform.localScale=Vector3.Lerp(transform.localScale,Vector3.Scale(GetIdleWorldScale(),ScaleMultiplier),GetIdlePositionLerpSpeed()*Time.deltaTime);
			    targetPos=transform.position;
			} else {
			    transform.position=Vector3.Lerp(transform.position,GetIdleWorldPosition(),30*Time.deltaTime);
			    ApplyOffsets();
			}
		}

		private void ApplyOffsets()
		{
			if (useWorldIdle) {
			    transform.position=GetIdleWorldPosition();transform.rotation=GetIdleWorldRotation();
			    transform.localScale=Vector3.Scale(GetIdleWorldScale(),ScaleMultiplier);
			    targetPos=transform.position;
			} else {
			    transform.localPosition=positionOffset;transform.localRotation=Quaternion.Euler(rotationOffset);
			    transform.localScale=Vector3.Scale(scaleOffset,ScaleMultiplier);
			}
		}

		private Vector3 GetPhasePosition(WeaponAttackPhase phase, float phaseProgress)
		{
			phaseProgress=Mathf.Clamp01(phaseProgress);
			switch(phase) {
			case WeaponAttackPhase.Windup:return EvaluateArcFrame(returnTargetPoint,castTargetPoint,EaseValue(phaseProgress,GetPhaseEase(phase)),1,cachedAngleOffset,phase).Position;
			case WeaponAttackPhase.Travel:return EvaluateTravelArcFrame(GetTravelPositionT(phaseProgress*StrikeTravelT)).Position;
			case WeaponAttackPhase.Overtravel:return EvaluateTravelArcFrame(GetTravelPositionT(StrikeTravelT+phaseProgress*(1-StrikeTravelT))).Position;
			case WeaponAttackPhase.Return:return EvaluateArcFrame(travelEndPoint,returnTargetPoint,EaseValue(phaseProgress,GetPhaseEase(phase)),1,cachedAngleOffset,phase).Position;
			default:return transform.position;
			}
		}
        private static Vector3 LocalAxis(WeaponLocalAxisDirection axis, Vector3 fallback)
        {
            switch(axis) {case WeaponLocalAxisDirection.Up:return Vector3.up;case WeaponLocalAxisDirection.Down:return Vector3.down;case WeaponLocalAxisDirection.Right:return Vector3.right;case WeaponLocalAxisDirection.Left:return Vector3.left;case WeaponLocalAxisDirection.Forward:return Vector3.forward;case WeaponLocalAxisDirection.Back:return Vector3.back;default:return fallback;}
        }

	}
}
