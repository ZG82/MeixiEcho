using System.Collections.Generic;
using UnityEngine;

namespace MeixiEcho
{
    public sealed class ChapterTwoInteractableHighlight : MonoBehaviour
    {
        private readonly List<GameObject> shells = new();
        private Material shellMaterial;
        private bool highlighted;

        public string Prompt { get; set; }

        public void Initialize()
        {
            if (shells.Count > 0) return;
            Shader shader = Shader.Find("Meixi/InteractableOutline");
            if (shader == null) shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Unlit/Color");
            if (shader == null) return;
            shellMaterial = new Material(shader)
            {
                name = "InteractableOutline_" + name,
                renderQueue = 3001
            };
            shellMaterial.SetColor("_OutlineColor", new Color(1f, 1f, 1f, 0f));
            if (shellMaterial.HasProperty("_BaseColor")) shellMaterial.SetColor("_BaseColor", new Color(1f, 1f, 1f, 0f));
            if (shellMaterial.HasProperty("_Color")) shellMaterial.SetColor("_Color", new Color(1f, 1f, 1f, 0f));
            if (shellMaterial.HasProperty("_Surface")) shellMaterial.SetFloat("_Surface", 1f);
            if (shellMaterial.HasProperty("_Cull")) shellMaterial.SetFloat("_Cull", 1f);
            if (shellMaterial.HasProperty("_ZWrite")) shellMaterial.SetFloat("_ZWrite", 0f);
            shellMaterial.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            int shellCount = 0;
            foreach (Renderer source in GetComponentsInChildren<Renderer>(true))
            {
                if (shellCount >= 24 || !ShouldOutline(source)) continue;
                MeshFilter filter = source.GetComponent<MeshFilter>();
                if (filter == null || filter.sharedMesh == null) continue;
                GameObject shell = new("交互白边");
                shell.transform.SetParent(source.transform, false);
                shell.transform.localScale = Vector3.one * 1.035f;
                MeshFilter shellFilter = shell.AddComponent<MeshFilter>();
                shellFilter.sharedMesh = filter.sharedMesh;
                MeshRenderer shellRenderer = shell.AddComponent<MeshRenderer>();
                shellRenderer.sharedMaterial = shellMaterial;
                shellRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                shellRenderer.receiveShadows = false;
                shell.SetActive(false);
                shells.Add(shell);
                shellCount++;
            }
        }

        private static bool ShouldOutline(Renderer source)
        {
            if (source == null || !source.enabled) return false;
            string objectName = source.gameObject.name;
            if (objectName.Contains("String") || objectName.Contains("琴弦") ||
                objectName.Contains("铜钉") || objectName.Contains("TuningPeg") ||
                objectName.Contains("纹") || objectName.Contains("墨痕")) return false;
            Vector3 size = source.bounds.size;
            float largest = Mathf.Max(size.x, Mathf.Max(size.y, size.z));
            float second = size.x + size.y + size.z - largest - Mathf.Min(size.x, Mathf.Min(size.y, size.z));
            return largest >= .075f && second >= .018f;
        }

        public void SetHighlighted(bool value)
        {
            highlighted = value;
            Initialize();
            foreach (GameObject shell in shells)
                if (shell != null) shell.SetActive(value && isActiveAndEnabled);
        }

        private void Update()
        {
            if (shellMaterial == null || !highlighted) return;
            float pulse = .78f + Mathf.Sin(Time.unscaledTime * 5.6f) * .16f;
            shellMaterial.SetColor("_OutlineColor", new Color(1f, 1f, 1f, highlighted ? pulse : 0f));
            if (shellMaterial.HasProperty("_BaseColor")) shellMaterial.SetColor("_BaseColor", new Color(1f, 1f, 1f, highlighted ? pulse : 0f));
            if (shellMaterial.HasProperty("_Color")) shellMaterial.SetColor("_Color", new Color(1f, 1f, 1f, highlighted ? pulse : 0f));
        }

        private void OnDisable()
        {
            foreach (GameObject shell in shells)
                if (shell != null) shell.SetActive(false);
        }
    }
}
