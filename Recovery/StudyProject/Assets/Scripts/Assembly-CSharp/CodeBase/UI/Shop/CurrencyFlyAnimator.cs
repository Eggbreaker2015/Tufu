using Codebase.Core;
using Codebase.Economy;
using Codebase.Amulet;
using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace Codebase.UI.Shop
{
	public class CurrencyFlyAnimator : MonoBehaviour
	{
		[Header("Target")]
		[SerializeField]
		private RectTransform target;

		[SerializeField]
		private Canvas rootCanvas;

		[Header("Visuals")]
		[SerializeField]
		private Sprite gemSprite;

		[SerializeField]
		private Vector2 spawnScaleRange = new Vector2(0.7f, 1f);

		[SerializeField]
		private Color startColor = Color.white;

		[SerializeField]
		private Color endColor = Color.white;

		[Header("Tuning")]
		[SerializeField]
		private int defaultCount = 12;

		[SerializeField]
		private float scatterRadius = 80f;

		[SerializeField]
		private float flyDuration = 0.7f;

		[SerializeField]
		private float spreadDuration = 0.15f;

		[SerializeField]
		private float spreadVariance = 0.4f;

		[SerializeField]
		private float arcHeight = 60f;

		[SerializeField]
		private Ease spreadEase = (Ease)27;

		[SerializeField]
		private Ease flyEase = (Ease)8;

		[Header("Bounce")]
		[SerializeField]
		private float bounceScaleAdd = 0.06f;

		[SerializeField]
		private float bounceDuration = 0.1f;

		[SerializeField]
		private Ease bounceEase = (Ease)27;

		[Header("Center FX")]
		[SerializeField]
		private Sprite centerSprite;

		[SerializeField]
		private float centerFadeInDuration = 0.3f;

		[SerializeField]
		private float centerAliveDuration = 1f;

		[SerializeField]
		private float centerFadeOutDuration = 0.3f;

		[SerializeField]
		private float centerRotationSpeed = 90f;

		[Header("Lifecycle")]
		[SerializeField]
		private bool destroyOnComplete = true;

		private readonly List<Image> spawnedImages = new List<Image>();

		private RectTransform spawnRoot;

		private Vector3 targetBaseScale;

		private bool baseScaleInitialized;

		private int activeTweens;

		private float bounceIntensity;

		private Tween bounceTween;

		private Action onAllCompletedCallback;

		private void Awake()
		{
            // Original ELF 0x16222a4.
            spawnRoot = transform as RectTransform;
            if (rootCanvas == null) rootCanvas = GetComponentInParent<Canvas>();
            if (rootCanvas != null) rootCanvas = rootCanvas.rootCanvas;
		}

		public void Setup(RectTransform newTarget)
		{
            // Original ELF 0x161b210: capture the target base scale once, after root-canvas normalization.
            target = newTarget;
            if (target == null) return;
            Canvas canvas = target.GetComponentInParent<Canvas>();
            if (canvas == null) return;
            rootCanvas = canvas.rootCanvas;
            if (transform.parent != rootCanvas.transform) { transform.SetParent(rootCanvas.transform, false); spawnRoot = transform as RectTransform; }
            if (!baseScaleInitialized) { targetBaseScale = target.localScale; baseScaleInitialized = true; }
		}

		public void SetSprite(Sprite sprite)
		{
            // Original ELF 0x16223f8.
            gemSprite = sprite;
		}

		public void SetCurrencyKey(string currencyKey)
		{
            // Original ELF 0x161b094; an absent resolved icon retains the existing sprite.
            if (string.IsNullOrEmpty(currencyKey)) return;
            Sprite sprite = currencyKey.StartsWith("soul.") ? GameContext.Get<SoulStyleConfig>().GetIcon(currencyKey.Substring("soul.".Length)) : GameContext.Get<CurrencyStyleConfig>().GetIcon(currencyKey);
            if (sprite != null) gemSprite = sprite;
		}

		public void Play(RectTransform from)
		{
            // Original ELF 0x1622400; this overload does not overwrite the stored completion delegate.
            Play(from, defaultCount, scatterRadius, flyDuration);
		}

		public void Play(RectTransform from, Action onAllCompleted)
		{
            // Original ELF 0x161b430.
            onAllCompletedCallback = onAllCompleted;
            Play(from, defaultCount, scatterRadius, flyDuration);
		}

		public void Play(RectTransform from, int count, float radius, float duration)
		{
            // Original ELF 0x162240c and original per-image quadratic path/completion delegates.
            if (from == null) return;
            if (rootCanvas == null)
            {
                Canvas canvas = target.GetComponentInParent<Canvas>();
                if (canvas == null) canvas = from.GetComponentInParent<Canvas>();
                if (canvas != null) rootCanvas = canvas.rootCanvas;
            }
            if (rootCanvas == null) return;
            if (spawnRoot == null) spawnRoot = transform as RectTransform;
            if (GetComponentInParent<Canvas>() != rootCanvas) { transform.SetParent(rootCanvas.transform, false); spawnRoot = transform as RectTransform; }
            UnityEngine.Camera camera = rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : rootCanvas.worldCamera != null ? rootCanvas.worldCamera : UnityEngine.Camera.main;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(spawnRoot, RectTransformUtility.WorldToScreenPoint(camera, from.position), camera, out Vector2 origin);
            SpawnCenterFx(origin);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(spawnRoot, RectTransformUtility.WorldToScreenPoint(camera, target.position), camera, out Vector2 destination);
            for (int i = 0; i < count; i++)
            {
                GameObject go = new GameObject("Gem_Fly", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                go.transform.SetParent(spawnRoot, false); var rect = (RectTransform)go.transform; var image = go.GetComponent<Image>();
                image.sprite = gemSprite; image.color = startColor;
                Vector2 scatter = UnityEngine.Random.insideUnitCircle;
                rect.anchoredPosition = origin;
                float scale = UnityEngine.Random.Range(spawnScaleRange.x, spawnScaleRange.y);
                rect.localScale = Vector3.one * scale * 0.6f;
                Vector2 spread = scatter * radius + origin;
                float spreadTime = spreadDuration * UnityEngine.Random.Range(1f - spreadVariance, spreadVariance + 1f);
                if (spreadTime < 0.01f) spreadTime = 0.01f;
                Tween spreadTween = rect.DOAnchorPos(spread, spreadTime).SetEase(spreadEase);
                Vector2 control = spread + (destination - spread) * 0.5f + Vector2.up * arcHeight;
                Sequence sequence = DOTween.Sequence();
                sequence.Append(spreadTween); sequence.Join(rect.DOScale(scale, spreadTime));
                sequence.Append(DOTween.To(() => 0f, progress =>
                {
                    float inverse = 1f - progress;
                    rect.anchoredPosition = spread * inverse * inverse + control * ((inverse + inverse) * progress) + destination * progress * progress;
                }, 1f, duration).SetEase(flyEase));
                sequence.Join(image.DOColor(endColor, duration));
                sequence.OnComplete(() =>
                {
                    Destroy(go); BounceTarget(); activeTweens--;
                    if (activeTweens > 0) return;
                    target.DOKill();
                    if (bounceTween != null && bounceTween.IsActive()) bounceTween.Kill();
                    bounceIntensity = 0f; target.localScale = targetBaseScale;
                    Action callback = onAllCompletedCallback; onAllCompletedCallback = null; callback?.Invoke();
                    if (destroyOnComplete) Destroy(gameObject);
                });
                spawnedImages.Add(image); activeTweens++;
            }
		}

		private void SpawnCenterFx(Vector2 position)
		{
            // Original ELF 0x1622f50: center rotation is linked to the transient FX object.
            GameObject go = new GameObject("Gem_SpreadCenterFX", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(spawnRoot, false); var rect = (RectTransform)go.transform; rect.anchoredPosition = position;
            var image = go.GetComponent<Image>(); image.sprite = centerSprite;
            Color color = image.color; color.a = 0f; image.color = color; image.SetNativeSize();
            rect.localScale = Vector3.one * 1.5f;
            rect.DORotate(new Vector3(0f, 0f, centerRotationSpeed), 1f, RotateMode.FastBeyond360).SetEase((Ease)1).SetLoops(-1).SetLink(go);
            DOTween.Sequence().Append(image.DOFade(1f, centerFadeInDuration).SetEase((Ease)1)).AppendInterval(centerAliveDuration)
                .Append(image.DOFade(0f, centerFadeOutDuration).SetEase((Ease)1)).OnComplete(() => Destroy(go));
		}

		private void BounceTarget()
		{
            // Original ELF 0x1623480: serialized bounceEase is intentionally unused by this original method.
            target.DOKill();
            float accumulated = bounceIntensity + bounceScaleAdd;
            float maximum = bounceScaleAdd * 4f;
            bounceIntensity = accumulated <= maximum ? accumulated : maximum;
            target.localScale = targetBaseScale * (bounceIntensity + 1f);
            if (bounceTween != null && bounceTween.IsActive()) bounceTween.Kill();
            bounceTween = DOTween.To(() => bounceIntensity, value => { bounceIntensity = value; target.localScale = targetBaseScale * (value + 1f); }, 0f, bounceDuration).SetEase((Ease)4);
		}
	}
}
