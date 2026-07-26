using UnityEngine;

namespace MeixiEcho
{
    /// <summary>
    /// Lightweight unscaled-time reveal used by pause, dialogue, interaction and
    /// chapter cards. It adds no Update cost while the panel is hidden or settled.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MeixiUiPanelReveal : MonoBehaviour
    {
        private CanvasGroup group;
        private RectTransform rect;
        private Vector3 restingScale = Vector3.one;
        private float duration = .22f;
        private float startScale = .95f;
        private float startedAt;
        private bool revealing;

        public MeixiUiPanelReveal Configure(float revealDuration, float scaleFrom)
        {
            duration = Mathf.Max(.08f, revealDuration);
            startScale = Mathf.Clamp(scaleFrom, .82f, 1.08f);
            return this;
        }

        private void Awake()
        {
            rect = transform as RectTransform;
            restingScale = rect != null ? rect.localScale : transform.localScale;
            group = GetComponent<CanvasGroup>();
            if (group == null) group = gameObject.AddComponent<CanvasGroup>();
        }

        private void OnEnable()
        {
            if (group == null) Awake();
            startedAt = Time.unscaledTime;
            revealing = true;
            group.alpha = 0f;
            transform.localScale = restingScale * startScale;
        }

        private void OnDisable()
        {
            revealing = false;
            if (group != null) group.alpha = 1f;
            transform.localScale = restingScale;
        }

        private void Update()
        {
            if (!revealing) return;
            float t = Mathf.Clamp01((Time.unscaledTime - startedAt) / duration);
            float eased = 1f - Mathf.Pow(1f - t, 3f);
            group.alpha = eased;
            transform.localScale = Vector3.LerpUnclamped(restingScale * startScale, restingScale, eased);
            if (t < 1f) return;
            revealing = false;
            group.alpha = 1f;
            transform.localScale = restingScale;
        }
    }
}
