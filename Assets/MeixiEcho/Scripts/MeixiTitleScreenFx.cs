using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MeixiEcho
{
    public sealed class MeixiTitleScreenFx : MonoBehaviour
    {
        private const float MinimumLoadingSeconds = 2f;

        private static readonly string[] StartupResources =
        {
            "Art/TitleBackgroundV2",
            "Art/MeixiTitleWordmark",
            "Art/QuestFrameV2",
            "Art/ChapterBackdrop",
            "Audio/PingFanZhiLu",
            "Models/Characters/LinMo",
            "Models/Characters/RealisticMale/Models/DefaultMale",
            "Models/Village/hexagons_medieval",
            "Animations/LinMoCombat",
            "Textures/Generated/MeixiBeatSigil",
            "Textures/Generated/MeixiFinalSlash"
        };

        private RectTransform backgroundRect;
        private RectTransform titleRoot;
        private CanvasGroup titleGroup;
        private CanvasGroup loadingGroup;
        private Image loadingFill;
        private Text statusText;
        private Text percentText;
        private Text readyText;
        private Text pressText;
        private Button startButton;
        private Graphic[] glowGraphics;
        private readonly List<RectTransform> sparkles = new();
        private float presentationElapsed;
        private float actualProgress;
        private float displayedProgress;
        private bool requestsComplete;
        private bool readyTransitionStarted;
        private bool readyVisualApplied;

        public bool IsReady => readyVisualApplied;

        public void Configure(
            RectTransform backgroundRect,
            RectTransform titleRoot,
            CanvasGroup titleGroup,
            CanvasGroup loadingGroup,
            Image loadingFill,
            Text statusText,
            Text percentText,
            Text readyText,
            Text pressText,
            Button startButton,
            Transform sparkleRoot,
            params Graphic[] glowGraphics)
        {
            this.backgroundRect = backgroundRect;
            this.titleRoot = titleRoot;
            this.titleGroup = titleGroup;
            this.loadingGroup = loadingGroup;
            this.loadingFill = loadingFill;
            this.statusText = statusText;
            this.percentText = percentText;
            this.readyText = readyText;
            this.pressText = pressText;
            this.startButton = startButton;
            this.glowGraphics = glowGraphics;
            presentationElapsed = 0f;

            if (startButton != null) startButton.interactable = false;
            if (readyText != null) readyText.gameObject.SetActive(false);
            if (pressText != null) pressText.gameObject.SetActive(false);
            if (loadingGroup != null) loadingGroup.alpha = 1f;
            if (loadingFill != null) loadingFill.fillAmount = 0f;
            BuildSparkles(sparkleRoot);
            StartCoroutine(LoadStartupResources());
        }

        private void BuildSparkles(Transform parent)
        {
            if (parent == null) return;
            for (int i = 0; i < 12; i++)
            {
                var sparkle = new GameObject("溪畔流萤 " + (i + 1)).AddComponent<Image>();
                sparkle.transform.SetParent(parent, false);
                sparkle.color = i % 3 == 0
                    ? new Color(1f, 0.74f, 0.28f, 0.42f)
                    : new Color(0.44f, 0.90f, 1f, 0.28f);
                sparkle.raycastTarget = false;
                RectTransform rect = sparkle.rectTransform;
                rect.anchorMin = rect.anchorMax = new Vector2(
                    Mathf.Repeat(i * 0.193f + 0.10f, 0.80f) + 0.10f,
                    Mathf.Repeat(i * 0.241f + 0.16f, 0.54f) + 0.16f);
                rect.sizeDelta = Vector2.one * (i % 4 == 0 ? 6f : 3.5f);
                rect.localRotation = Quaternion.Euler(0f, 0f, 45f);
                sparkles.Add(rect);
            }
        }

        private IEnumerator LoadStartupResources()
        {
            var requests = new List<ResourceRequest>(StartupResources.Length);
            foreach (string path in StartupResources)
                requests.Add(Resources.LoadAsync(path));

            while (true)
            {
                float total = 0f;
                int completed = 0;
                foreach (ResourceRequest request in requests)
                {
                    total += request.isDone ? 1f : request.progress;
                    if (request.isDone) completed++;
                }

                actualProgress = requests.Count == 0 ? 1f : total / requests.Count;
                if (statusText != null)
                {
                    statusText.text = completed < requests.Count * 0.55f
                        ? "正在读取世界数据"
                        : completed < requests.Count
                            ? "正在校准声纹与场景"
                            : "正在完成最后校验";
                }
                if (completed >= requests.Count) break;
                yield return null;
            }

            for (int i = 0; i < requests.Count; i++)
            {
                if (requests[i].asset == null)
                    Debug.LogWarning("[MeixiTitle] Optional startup resource was not found: " + StartupResources[i]);
            }

            actualProgress = 1f;
            requestsComplete = true;
        }

        private void Update()
        {
            float visualDelta = Mathf.Min(Time.unscaledDeltaTime, 0.10f);
            presentationElapsed += visualDelta;
            float t = presentationElapsed;
            float intro = EaseOutCubic(Mathf.Clamp01(t / 0.85f));
            float pulse = (Mathf.Sin(t * 1.25f) + 1f) * 0.5f;

            AnimateBackground(t);

            if (titleGroup != null) titleGroup.alpha = intro;
            if (titleRoot != null)
            {
                titleRoot.localScale = Vector3.one * Mathf.Lerp(1.045f, 1f + pulse * 0.003f, intro);
                titleRoot.anchoredPosition = new Vector2(0f, Mathf.Lerp(-14f, 0f, intro));
            }

            float progressTarget = actualProgress;
            if (requestsComplete && t < MinimumLoadingSeconds)
                progressTarget = Mathf.Min(0.99f, progressTarget);

            displayedProgress = Mathf.MoveTowards(displayedProgress, progressTarget, visualDelta * 0.82f);
            if (requestsComplete && t < MinimumLoadingSeconds)
                displayedProgress = Mathf.Min(displayedProgress, 0.99f);

            if (loadingFill != null) loadingFill.fillAmount = displayedProgress;
            if (percentText != null && !readyTransitionStarted)
                percentText.text = Mathf.RoundToInt(displayedProgress * 100f) + "%";

            if (requestsComplete && t >= MinimumLoadingSeconds && displayedProgress >= 0.999f && !readyTransitionStarted)
                StartCoroutine(ApplyReadyState());

            AnimateSparkles(t);
            AnimateGlow(t);
        }

        private void AnimateBackground(float t)
        {
            if (backgroundRect == null) return;
            float driftX = Mathf.Sin(t * 0.115f) * 5f;
            float driftY = Mathf.Cos(t * 0.09f) * 3f;
            backgroundRect.anchoredPosition = new Vector2(driftX, driftY);
            backgroundRect.localScale = Vector3.one * (1.025f + Mathf.Sin(t * 0.075f) * 0.003f);
        }

        private IEnumerator ApplyReadyState()
        {
            readyTransitionStarted = true;
            displayedProgress = 1f;
            if (loadingFill != null) loadingFill.fillAmount = 1f;
            if (percentText != null) percentText.text = "100%";

            float elapsed = 0f;
            const float fadeSeconds = 0.34f;
            while (elapsed < fadeSeconds)
            {
                elapsed += Time.unscaledDeltaTime;
                if (loadingGroup != null)
                    loadingGroup.alpha = 1f - EaseInOut(Mathf.Clamp01(elapsed / fadeSeconds));
                yield return null;
            }

            if (loadingGroup != null) loadingGroup.gameObject.SetActive(false);
            if (readyText != null) readyText.gameObject.SetActive(true);
            if (pressText != null) pressText.gameObject.SetActive(true);
            if (startButton != null) startButton.interactable = true;
            readyVisualApplied = true;
        }

        private void AnimateSparkles(float t)
        {
            for (int i = 0; i < sparkles.Count; i++)
            {
                RectTransform rect = sparkles[i];
                float rise = Mathf.Repeat(t * (0.014f + i * 0.001f) + i * 0.067f, 0.18f);
                rect.anchoredPosition = new Vector2(Mathf.Sin(t * 0.34f + i) * 7f, rise * 490f);
                rect.localScale = Vector3.one * (0.62f + (Mathf.Sin(t * 1.35f + i * 1.3f) + 1f) * 0.25f);
            }
        }

        private void AnimateGlow(float t)
        {
            if (pressText != null && readyVisualApplied)
            {
                Color color = pressText.color;
                color.a = Mathf.Lerp(0.42f, 1f, (Mathf.Sin(t * 2.1f) + 1f) * 0.5f);
                pressText.color = color;
            }

            if (glowGraphics == null) return;
            for (int i = 0; i < glowGraphics.Length; i++)
            {
                Graphic graphic = glowGraphics[i];
                if (graphic == null) continue;
                Color color = graphic.color;
                color.a = Mathf.Lerp(0.32f, 0.62f, (Mathf.Sin(t * (0.72f + i * 0.09f) + i) + 1f) * 0.5f);
                graphic.color = color;
            }
        }

        private static float EaseOutCubic(float t)
        {
            float p = 1f - t;
            return 1f - p * p * p;
        }

        private static float EaseInOut(float t)
        {
            return t * t * (3f - 2f * t);
        }
    }
}
