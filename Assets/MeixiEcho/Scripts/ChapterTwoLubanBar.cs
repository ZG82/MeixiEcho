using UnityEngine;

namespace MeixiEcho
{
    public sealed class ChapterTwoLubanBar : MonoBehaviour
    {
        private const float UnlockDistance = 1.15f;
        private const float UnlockDuration = .72f;
        private Vector3 exitDirection;
        private Vector3 lockedPosition;
        private Vector3 unlockedPosition;
        private bool unlocked;
        private float unlockStarted;
        private Renderer[] renderers;
        private Color[] baseColors;
        private MaterialPropertyBlock propertyBlock;

        public int Index { get; private set; }
        public bool IsUnlocked => unlocked;

        public void Configure(ChapterTwoAdventureRuntime owner, int index, Vector3 direction)
        {
            Index = index;
            exitDirection = direction.normalized;
            lockedPosition = transform.localPosition;
            unlockedPosition = lockedPosition + exitDirection * UnlockDistance;
            renderers = GetComponentsInChildren<Renderer>(true);
            baseColors = new Color[renderers.Length];
            for (int i = 0; i < renderers.Length; i++)
            {
                Material material = renderers[i].sharedMaterial;
                baseColors[i] = material != null && material.HasProperty("_BaseColor")
                    ? material.GetColor("_BaseColor")
                    : material != null && material.HasProperty("_Color")
                        ? material.color
                        : Color.white;
            }
            propertyBlock = new MaterialPropertyBlock();
        }

        public void SetHovered(bool hovered)
        {
            if (renderers == null) return;
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer target = renderers[i];
                if (target == null) continue;
                Color color = hovered
                    ? Color.Lerp(baseColors[i], new Color(1f, .78f, .34f, baseColors[i].a), .34f)
                    : baseColors[i];
                target.GetPropertyBlock(propertyBlock);
                propertyBlock.SetColor("_BaseColor", color);
                propertyBlock.SetColor("_Color", color);
                target.SetPropertyBlock(propertyBlock);
            }
        }

        public void Unlock()
        {
            if (unlocked) return;
            unlocked = true;
            unlockStarted = Time.unscaledTime;
            Collider hitVolume = GetComponent<Collider>();
            if (hitVolume != null) hitVolume.enabled = false;
        }

        private void Update()
        {
            if (!unlocked) return;
            float t = Mathf.Clamp01((Time.unscaledTime - unlockStarted) / UnlockDuration);
            float eased = t * t * (3f - 2f * t);
            float pullFeedback = Mathf.Sin(t * Mathf.PI) * .035f;
            transform.localPosition = Vector3.LerpUnclamped(lockedPosition, unlockedPosition, eased) +
                                      exitDirection * pullFeedback;

            // Keep the newly released bar readable for a moment without changing its material.
            if (renderers == null || propertyBlock == null) return;
            float highlight = Mathf.Sin(t * Mathf.PI) * .32f;
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer target = renderers[i];
                if (target == null) continue;
                Color color = Color.Lerp(baseColors[i], new Color(1f, .70f, .22f, baseColors[i].a), highlight);
                target.GetPropertyBlock(propertyBlock);
                propertyBlock.SetColor("_BaseColor", color);
                propertyBlock.SetColor("_Color", color);
                target.SetPropertyBlock(propertyBlock);
            }
        }
    }
}
