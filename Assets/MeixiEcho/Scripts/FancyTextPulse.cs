using UnityEngine;
using UnityEngine.UI;

namespace MeixiEcho
{
    public sealed class FancyTextPulse : FancyTextPulseBase { }

    public sealed class ToastBannerFx : MonoBehaviour
    {
        private RectTransform rect;
        private CanvasGroup group;
        private Image frame;
        private Vector3 baseScale;
        private Color baseFrameColor;
        private float age;

        public void Configure(Image frameImage)
        {
            rect = transform as RectTransform;
            group = GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();
            frame = frameImage;
            baseScale = rect != null ? rect.localScale : Vector3.one;
            baseFrameColor = frame != null ? frame.color : Color.white;
        }

        public void Play(Color accent)
        {
            gameObject.SetActive(true);
            age = 0f;
            if (group != null) group.alpha = 0f;
            if (rect != null) rect.localScale = baseScale * 0.93f;
            if (frame != null)
                frame.color = Color.Lerp(baseFrameColor, new Color(accent.r, accent.g, accent.b, 1f), 0.12f);
        }

        private void Update()
        {
            age += Time.unscaledDeltaTime;
            float intro = Mathf.Clamp01(age / 0.24f);
            float eased = 1f - Mathf.Pow(1f - intro, 3f);
            if (group != null) group.alpha = eased;
            if (rect != null)
            {
                float breathe = 1f + Mathf.Sin(Time.unscaledTime * 1.8f) * 0.003f;
                rect.localScale = baseScale * Mathf.Lerp(0.93f, breathe, eased);
            }
        }
    }
}
