using Codebase.Buildings;
using Codebase.Unit;
using Codebase.Player;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Codebase.Core;
using Codebase.Player.Models;
using UnityEngine;

namespace Codebase.Camera
{
	[DefaultExecutionOrder(-1000)]
	public class CameraFollowSystem : MonoBehaviour
	{
		[CompilerGenerated]
		private sealed class _003COverrideFollowTarget_003Ed__44 : IEnumerator<object>, IEnumerator, IDisposable
		{
			private int _003C_003E1__state;

			private object _003C_003E2__current;

			public CameraFollowSystem _003C_003E4__this;

			public Vector3 target;

			private float _003Ctimer_003E5__2;

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
			public _003COverrideFollowTarget_003Ed__44(int _003C_003E1__state)
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

		[SerializeField]
		private UnityEngine.Camera cam;

		[SerializeField]
		private CameraSettings settings;

		[Inject]
		private PlayerModel playerModel;

		private bool isOverridingTarget;

		private Vector3 overrideTarget;

		private float overrideInertia;

		private Transform followingTransform;

		private float extraDistance;

		private float extraHeight;

		private float smoothedTargetY;

		private float smoothedTargetYVelocity;

		private bool hasSmoothedTargetY;

		private float transitionDelay;

		private float maxTransitionDelay;

		private float smoothedMovementSpeedHeightBonus;

		private float smoothedMovementSpeedHeightBonusVelocity;

		private float zoneDistanceBonusTarget;

		private float smoothedZoneDistanceBonus;

		private float smoothedZoneDistanceBonusVelocity;

		private float shakeDuration;

		private float shakeMagnitude;

		private bool lookAtFollowingTarget;

		private bool useTargetForwardOffset;

		private bool useExactTransform;

		private const float MovementSpeedHeightDivisor = 5f;

		private const float MovementSpeedHeightSmoothTime = 0.5f;

		private const float ZoneDistanceSmoothTime = 0.5f;

		public UnityEngine.Camera Camera => this.cam;

		public Vector3 PositionOfAttack { get; private set; }

		public bool IsShowingGate => isOverridingTarget || ShowingGate;

		public bool ShowingGate { get; set; }

		private void Awake()
		{
			// Native ELF VA 0x154dd74.
			GameContext.Set(this);
		}

		public void AddZoneDistanceBonus(float amount)
		{
			// Native ELF VA 0x154dde4.
			zoneDistanceBonusTarget += amount;
		}

		public void RemoveZoneDistanceBonus(float amount)
		{
			// Native ELF VA 0x154ddf4.
			zoneDistanceBonusTarget = Mathf.Max(0f, zoneDistanceBonusTarget - amount);
		}

		public void Follow(Vector3 target)
		{
			// Native ELF VA 0x154de10.
			StopAllCoroutines();
			StartCoroutine(OverrideFollowTarget(target));
		}

		public void Follow(Transform target, float distance, float height = 0f, bool lookAt = false, float transitionDelay = 0f)
		{
			// Native ELF VA 0x154defc.
			followingTransform = target;
			extraDistance = distance;
			extraHeight = height;
			overrideInertia = 25f;
			lookAtFollowingTarget = lookAt;
			maxTransitionDelay = transitionDelay;
			this.transitionDelay = 0f;
			useTargetForwardOffset = false;
			useExactTransform = false;
		}

		public void FollowAlongTargetForward(Transform target, float distance, float height, float transitionDelay)
		{
			// Native ELF VA 0x154df5c.
			followingTransform = target;
			extraDistance = distance;
			extraHeight = height;
			overrideInertia = 25f;
			lookAtFollowingTarget = true;
			maxTransitionDelay = transitionDelay;
			this.transitionDelay = 0f;
			useTargetForwardOffset = true;
			useExactTransform = false;
		}

		public void FollowExactTransform(Transform target, float transitionDelay)
		{
			// Native ELF VA 0x154dfb4.
			followingTransform = target;
			overrideInertia = 25f;
			lookAtFollowingTarget = false;
			useTargetForwardOffset = false;
			useExactTransform = true;
			maxTransitionDelay = transitionDelay;
			this.transitionDelay = 0f;
		}

		public void Unfollow()
		{
			// Native ELF VA 0x154dff8.
			followingTransform = null;
			isOverridingTarget = false;
			hasSmoothedTargetY = false;
			smoothedTargetYVelocity = 0f;
			extraHeight = 0f;
			lookAtFollowingTarget = false;
			useTargetForwardOffset = false;
			useExactTransform = false;
		}

		private IEnumerator OverrideFollowTarget(Vector3 target)
		{
			// Native ELF VA 0x154f1a4 (MoveNext), fixed 0.1 Slerp per rendered frame.
			isOverridingTarget = true;
			overrideTarget = target;
			overrideInertia = 25f;
			float timer = 3.5f;
			while (timer > 0f)
			{
			    timer -= Time.deltaTime;
			    Transform cameraTransform = cam.transform;
			    cameraTransform.position = Vector3.Slerp(cameraTransform.position, new Vector3(target.x, settings.Distance, target.z + 18f), 0.1f);
			    yield return null;
			}
			isOverridingTarget = false;
		}

		public void LateUpdate()
		{
			// Native ELF VA 0x154e030. ARM64 preserves all Vector3/Quaternion return registers.
			Transform cameraTransform = cam.transform;
			if (followingTransform != null)
			{
			    Vector3 target = followingTransform.position;
			    transitionDelay = Mathf.Min(transitionDelay + Time.deltaTime, maxTransitionDelay);
			    float transition = transitionDelay / maxTransitionDelay;
			    if (useExactTransform)
			    {
			        cameraTransform.position = maxTransitionDelay > 0f ? Vector3.Lerp(cameraTransform.position, target, transition) : target;
			        Quaternion rotation = followingTransform.rotation;
			        cameraTransform.rotation = maxTransitionDelay > 0f ? Quaternion.Slerp(cameraTransform.rotation, rotation, transition) : rotation;
			        PositionOfAttack = target;
			        return;
			    }
			    Vector3 desired;
			    if (useTargetForwardOffset)
			    {
			        Vector3 forward = followingTransform.forward;
			        forward.y = 0f;
			        forward = forward.sqrMagnitude < 0.0001f ? Vector3.forward : forward.normalized;
			        desired = new Vector3(target.x + forward.x * (extraDistance * 0.5f + 20f), extraDistance + settings.Distance + extraHeight, target.z + forward.z * (extraDistance * 0.5f + 20f));
			    }
			    else desired = new Vector3(target.x, extraDistance + settings.Distance + extraHeight, target.z + 20f + extraDistance * 0.5f);
			    cameraTransform.position = maxTransitionDelay > 0f ? Vector3.Lerp(cameraTransform.position, desired, transition) : desired;
			    PositionOfAttack = target;
			    if (lookAtFollowingTarget)
			    {
			        Quaternion rotation = Quaternion.LookRotation(target - cameraTransform.position);
			        cameraTransform.rotation = maxTransitionDelay > 0f ? Quaternion.Slerp(cameraTransform.rotation, rotation, transition) : rotation;
			    }
			    return;
			}
			if (isOverridingTarget) return;
			Vector3 playerPosition = playerModel.Unit.transform.position;
			float targetY = Mathf.Max(playerPosition.y, GameContext.Get<FirecampSystem>().transform.position.y);
			if (!hasSmoothedTargetY)
			{
			    smoothedTargetY = targetY;
			    hasSmoothedTargetY = true;
			}
			float smoothTime = Mathf.Max(0f, settings.HeightSmoothTime);
			smoothedTargetY = smoothTime > 0f ? Mathf.SmoothDamp(smoothedTargetY, targetY, ref smoothedTargetYVelocity, smoothTime, float.PositiveInfinity, Time.deltaTime) : targetY;
			float zoneBonus = CalculateZoneDistanceBonus();
			float angle = settings.AngleOfAttack * Mathf.Deg2Rad;
			float cosine = Mathf.Cos(angle);
			float sine = Mathf.Sin(angle);
			float yaw = settings.Yaw * Mathf.Deg2Rad;
			float heightBonus = CalculateMovementSpeedHeightBonus();
			float distance = smoothedTargetY + settings.Distance + zoneBonus;
			float vertical = distance * sine + heightBonus;
			float horizontal = distance * cosine;
			Vector3 desiredPosition = new Vector3(playerPosition.x + horizontal * Mathf.Sin(yaw), smoothedTargetY + vertical, playerPosition.z + horizontal * Mathf.Cos(yaw));
			float amount = settings.Inertia > 0f ? Mathf.Max(1f, Vector3.Distance(cameraTransform.position, desiredPosition)) / settings.Inertia * Time.deltaTime : 1f;
			cameraTransform.position = Vector3.Slerp(cameraTransform.position, desiredPosition, amount);
			Vector3 lookTarget = new Vector3(playerPosition.x, targetY, playerPosition.z);
			cameraTransform.LookAt(lookTarget);
			if (shakeDuration > 0f)
			{
			    cameraTransform.position += UnityEngine.Random.insideUnitSphere * shakeMagnitude;
			    shakeDuration -= Time.deltaTime;
			}
			PositionOfAttack = lookTarget;
		}

		public void Shake(float duration, float magnitude)
		{
			// Native ELF VA 0x154e920.
			shakeDuration = duration;
			shakeMagnitude = magnitude;
		}

		private float CalculateMovementSpeedHeightBonus()
		{
			// Native ELF VA 0x154e838.
			PlayerUnit unit = playerModel?.Unit;
			float bonus = unit != null ? (float)unit.GetStatSpecificValue(StatType.MovementSpeed) / 5f : 0f;
			smoothedMovementSpeedHeightBonus = Mathf.SmoothDamp(smoothedMovementSpeedHeightBonus, bonus, ref smoothedMovementSpeedHeightBonusVelocity, 0.5f, float.PositiveInfinity, Time.deltaTime);
			return smoothedMovementSpeedHeightBonus;
		}

		private float CalculateZoneDistanceBonus()
		{
			// Native ELF VA 0x154e7ec.
			smoothedZoneDistanceBonus = Mathf.SmoothDamp(smoothedZoneDistanceBonus, zoneDistanceBonusTarget, ref smoothedZoneDistanceBonusVelocity, 0.5f, float.PositiveInfinity, Time.deltaTime);
			return smoothedZoneDistanceBonus;
		}
	}
}
