using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace MeixiEcho
{
    public sealed class MeixiStartupLogo : MonoBehaviour
    {
        private const string LogoResourcePath = "Art/GameStartLogoZJH";
        private static bool hasPlayed;
        public static bool IsShowing { get; private set; }

        [SerializeField] private float fadeInSeconds = 0.72f;
        [SerializeField] private float holdSeconds = 0.92f;
        [SerializeField] private float fadeOutSeconds = 0.58f;

        private CanvasGroup group;
        private CanvasGroup logoGroup;
        private RectTransform logoRect;
        private RectTransform accentLine;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSplashScreen)]
        private static void ShowBeforeSplashScreen()
        {
            if (IsCaptureRun()) return;
            ShowOnce();
        }

        public static void ShowOnce()
        {
            if (hasPlayed) return;
            hasPlayed = true;
            IsShowing = true;

            var root = new GameObject("MeixiStartupLogo");
            DontDestroyOnLoad(root);
            root.AddComponent<MeixiStartupLogo>();
        }

        private void Awake()
        {
            if (IsCaptureRun())
            {
                IsShowing = false;
                Destroy(gameObject);
                return;
            }
            BuildOverlay();
        }

        private static bool IsCaptureRun()
        {
            foreach (string argument in System.Environment.GetCommandLineArgs())
                if (argument.StartsWith("-meixi", System.StringComparison.OrdinalIgnoreCase) &&
                    argument.EndsWith("Capture", System.StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }

        private void Start()
        {
            StartCoroutine(PlayRoutine());
        }

        private void BuildOverlay()
        {
            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10000;

            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            group = gameObject.AddComponent<CanvasGroup>();
            group.alpha = 1f;
            group.blocksRaycasts = true;
            group.interactable = false;

            var backgroundObject = new GameObject("Background");
            backgroundObject.transform.SetParent(transform, false);
            var background = backgroundObject.AddComponent<Image>();
            background.color = new Color(0.965f, 0.976f, 0.992f, 1f);
            background.raycastTarget = false;
            Stretch(background.rectTransform);

            var imageObject = new GameObject("LogoImage");
            imageObject.transform.SetParent(transform, false);
            logoGroup = imageObject.AddComponent<CanvasGroup>();
            logoGroup.alpha = 0f;
            var image = imageObject.AddComponent<Image>();
            image.color = Color.white;
            image.raycastTarget = false;
            image.preserveAspect = true;

            Texture2D texture = Resources.Load<Texture2D>(LogoResourcePath);
            if (texture != null)
            {
                image.sprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
            }
            else
            {
                image.color = new Color(0.005f, 0.012f, 0.02f, 1f);
            }

            logoRect = image.rectTransform;
            logoRect.anchorMin = new Vector2(0.31f, 0.19f);
            logoRect.anchorMax = new Vector2(0.69f, 0.81f);
            logoRect.offsetMin = Vector2.zero;
            logoRect.offsetMax = Vector2.zero;
            logoRect.localScale = Vector3.one * 0.88f;

            var lineObject = new GameObject("StudioAccent");
            lineObject.transform.SetParent(transform, false);
            var line = lineObject.AddComponent<Image>();
            line.color = new Color(0.08f, 0.46f, 0.92f, 0.82f);
            line.raycastTarget = false;
            accentLine = line.rectTransform;
            accentLine.anchorMin = accentLine.anchorMax = new Vector2(0.5f, 0.205f);
            accentLine.sizeDelta = new Vector2(0f, 2f);
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private IEnumerator PlayRoutine()
        {
            yield return LogoReveal();
            yield return new WaitForSecondsRealtime(holdSeconds);
            yield return OverlayReveal(1f, 0f, fadeOutSeconds);
            IsShowing = false;
            Destroy(gameObject);
        }

        private void OnDestroy()
        {
            IsShowing = false;
        }

        private IEnumerator LogoReveal()
        {
            if (fadeInSeconds <= 0f)
            {
                logoGroup.alpha = 1f;
                logoRect.localScale = Vector3.one;
                yield break;
            }

            float elapsed = 0f;
            while (elapsed < fadeInSeconds)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / fadeInSeconds);
                float eased = EaseOutCubic(t);
                logoGroup.alpha = eased;
                logoRect.localScale = Vector3.one * Mathf.Lerp(0.88f, 1f, eased);
                logoRect.anchoredPosition = new Vector2(0f, Mathf.Lerp(-12f, 0f, eased));
                accentLine.sizeDelta = new Vector2(Mathf.Lerp(0f, 220f, Mathf.SmoothStep(0f, 1f, t)), 2f);
                yield return null;
            }

            logoGroup.alpha = 1f;
            logoRect.localScale = Vector3.one;
            logoRect.anchoredPosition = Vector2.zero;
            accentLine.sizeDelta = new Vector2(220f, 2f);
        }

        private IEnumerator OverlayReveal(float from, float to, float duration)
        {
            if (duration <= 0f)
            {
                group.alpha = to;
                yield break;
            }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                group.alpha = Mathf.Lerp(from, to, Mathf.SmoothStep(0f, 1f, t));
                yield return null;
            }

            group.alpha = to;
        }

        private static float EaseOutCubic(float t)
        {
            float p = 1f - t;
            return 1f - p * p * p;
        }
    }
}
