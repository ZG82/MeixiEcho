using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace MeixiEcho
{
    public enum MeixiCastRole
    {
        LinMo,
        VillageElder,
        VillageChild,
        VillageAunt
    }

    /// <summary>
    /// Loads and prepares the MIT-licensed Microsoft Rocketbox cast. Keeping all
    /// material and LOD decisions here prevents the runtime scene builder and the
    /// authored-scene upgrade pass from producing different-looking characters.
    /// </summary>
    public static class MeixiRealisticCast
    {
        private readonly struct CastDefinition
        {
            public readonly string ModelPath;
            public readonly string TextureRoot;
            public readonly string TexturePrefix;

            public CastDefinition(string modelPath, string textureRoot, string texturePrefix)
            {
                ModelPath = modelPath;
                TextureRoot = textureRoot;
                TexturePrefix = texturePrefix;
            }
        }

        private static readonly Dictionary<string, Material> Materials = new(StringComparer.Ordinal);

        public static GameObject Load(MeixiCastRole role)
        {
            CastDefinition definition = GetDefinition(role);
            return Resources.Load<GameObject>(definition.ModelPath);
        }

        public static MeixiCastRole RoleForNpc(string displayName, int fallbackIndex = 0)
        {
            if (!string.IsNullOrWhiteSpace(displayName))
            {
                if (displayName.Contains("村长", StringComparison.Ordinal) ||
                    displayName.Contains("老人", StringComparison.Ordinal)) return MeixiCastRole.VillageElder;
                if (displayName.Contains("小孩", StringComparison.Ordinal) ||
                    displayName.Contains("阿禾", StringComparison.Ordinal) ||
                    displayName.Contains("童", StringComparison.Ordinal)) return MeixiCastRole.VillageChild;
                if (displayName.Contains("婶", StringComparison.Ordinal) ||
                    displayName.Contains("姨", StringComparison.Ordinal)) return MeixiCastRole.VillageAunt;
            }

            return (fallbackIndex % 3) switch
            {
                0 => MeixiCastRole.VillageElder,
                1 => MeixiCastRole.VillageChild,
                _ => MeixiCastRole.VillageAunt
            };
        }

        public static void PrepareVisual(GameObject model, MeixiCastRole role, bool player)
        {
            if (model == null) return;
            SelectSingleLod(model);
            CastDefinition definition = GetDefinition(role);
            foreach (Renderer renderer in model.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer == null || !renderer.gameObject.activeInHierarchy) continue;
                Material[] slots = renderer.sharedMaterials;
                if (slots == null || slots.Length == 0)
                {
                    renderer.sharedMaterial = GetMaterial(definition, "body");
                }
                else
                {
                    for (int i = 0; i < slots.Length; i++)
                    {
                        string slotName = slots[i] != null ? slots[i].name.ToLowerInvariant() : string.Empty;
                        string part = slotName.Contains("opacity") || slotName.Contains("hair")
                            ? "opacity"
                            : slotName.Contains("head") || slotName.Contains("face")
                                ? "head"
                                : i == 1 ? "head" : i >= 2 ? "opacity" : "body";
                        slots[i] = GetMaterial(definition, part);
                    }
                    renderer.sharedMaterials = slots;
                }

                renderer.enabled = true;
                renderer.forceRenderingOff = false;
                renderer.shadowCastingMode = ShadowCastingMode.On;
                renderer.receiveShadows = true;
                if (renderer is SkinnedMeshRenderer skinned)
                {
                    // Four characters are inexpensive enough to keep their bounds live.
                    // This eliminates the old "shadow visible but NPC body missing" bug.
                    skinned.updateWhenOffscreen = true;
                    Bounds stable = skinned.localBounds;
                    stable.Expand(player ? new Vector3(.42f, .35f, .42f) : new Vector3(.25f, .22f, .25f));
                    skinned.localBounds = stable;
                }
            }
        }

        public static void ApplyTint(GameObject model, Color tint, float strength = .24f)
        {
            if (model == null) return;
            Color materialTint = Color.Lerp(Color.white, tint, Mathf.Clamp01(strength));
            materialTint.a = 1f;
            foreach (Renderer renderer in model.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer == null) continue;
                MaterialPropertyBlock block = new();
                renderer.GetPropertyBlock(block);
                block.SetColor("_BaseColor", materialTint);
                block.SetColor("_Color", materialTint);
                renderer.SetPropertyBlock(block);
            }
        }

        public static Bounds CalculateVisibleBounds(GameObject root)
        {
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(false);
            if (renderers.Length == 0) return new Bounds(root.transform.position, Vector3.one);
            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
            return bounds;
        }

        private static void SelectSingleLod(GameObject model)
        {
            Transform[] transforms = model.GetComponentsInChildren<Transform>(true);
            bool hasHigh = false;
            foreach (Transform item in transforms)
            {
                if (item.name.Contains("hipoly", StringComparison.OrdinalIgnoreCase))
                {
                    hasHigh = true;
                    break;
                }
            }
            if (!hasHigh) return;

            foreach (Transform item in transforms)
            {
                string lower = item.name.ToLowerInvariant();
                if (!lower.Contains("poly")) continue;
                item.gameObject.SetActive(lower.Contains("hipoly"));
            }
        }

        private static Material GetMaterial(CastDefinition definition, string part)
        {
            string key = definition.TexturePrefix + "_" + part;
            if (Materials.TryGetValue(key, out Material cached) && cached != null) return cached;

            string textureStem = part == "opacity"
                ? definition.TexturePrefix + "_opacity"
                : definition.TexturePrefix + "_" + part;
            bool childHead = definition.TexturePrefix == "cm012" && part == "head";
            bool childBody = definition.TexturePrefix == "cm012" && part == "body";
            const string auntTextureRoot = "Models/Characters/RealisticCast/VillageAunt/";
            Texture2D color = childHead
                ? Resources.Load<Texture2D>(auntTextureRoot + "f012_head_color")
                : childBody
                    ? Resources.Load<Texture2D>(definition.TextureRoot + textureStem + "_east_asian_v2")
                    : null;
            if (color == null) color = Resources.Load<Texture2D>(definition.TextureRoot + textureStem + "_color");
            Texture2D normal = part == "opacity" ? null : Resources.Load<Texture2D>(
                childHead ? auntTextureRoot + "f012_head_normal" : definition.TextureRoot + textureStem + "_normal");
            Texture2D specular = part == "opacity" ? null : Resources.Load<Texture2D>(
                childHead ? auntTextureRoot + "f012_head_specular" : definition.TextureRoot + textureStem + "_specular");

            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            Material material = new(shader) { name = "Rocketbox_" + key };
            if (color != null)
            {
                color.filterMode = FilterMode.Trilinear;
                color.anisoLevel = 4;
                material.mainTexture = color;
                if (material.HasProperty("_BaseMap")) material.SetTexture("_BaseMap", color);
            }
            if (normal != null && material.HasProperty("_BumpMap"))
            {
                material.SetTexture("_BumpMap", normal);
                material.SetFloat("_BumpScale", .74f);
                material.EnableKeyword("_NORMALMAP");
            }
            if (specular != null && material.HasProperty("_SpecGlossMap"))
                material.SetTexture("_SpecGlossMap", specular);

            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", Color.white);
            if (material.HasProperty("_Color")) material.SetColor("_Color", Color.white);
            if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", 0f);
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", part == "head" ? .28f : .20f);
            if (material.HasProperty("_SpecColor")) material.SetColor("_SpecColor", new Color(.24f, .23f, .22f, 1f));

            if (part == "opacity")
            {
                if (material.HasProperty("_AlphaClip")) material.SetFloat("_AlphaClip", 1f);
                if (material.HasProperty("_Cutoff")) material.SetFloat("_Cutoff", .38f);
                if (material.HasProperty("_Cull")) material.SetFloat("_Cull", (float)CullMode.Off);
                material.EnableKeyword("_ALPHATEST_ON");
                material.renderQueue = (int)RenderQueue.AlphaTest;
            }
            else
            {
                if (material.HasProperty("_Surface")) material.SetFloat("_Surface", 0f);
                if (material.HasProperty("_ZWrite")) material.SetFloat("_ZWrite", 1f);
                material.renderQueue = (int)RenderQueue.Geometry;
            }
            material.enableInstancing = true;
            Materials[key] = material;
            return material;
        }

        private static CastDefinition GetDefinition(MeixiCastRole role)
        {
            return role switch
            {
                MeixiCastRole.LinMo => new CastDefinition(
                    "Models/Characters/RealisticCast/LinMo/Male_Adult_20",
                    "Models/Characters/RealisticCast/LinMo/", "m027"),
                MeixiCastRole.VillageElder => new CastDefinition(
                    "Models/Characters/RealisticCast/VillageElder/Male_Adult_03",
                    "Models/Characters/RealisticCast/VillageElder/", "m004"),
                MeixiCastRole.VillageChild => new CastDefinition(
                    "Models/Characters/RealisticCast/VillageChild/Male_Child_02",
                    "Models/Characters/RealisticCast/VillageChild/", "cm012"),
                _ => new CastDefinition(
                    "Models/Characters/RealisticCast/VillageAunt/Female_Adult_12",
                    "Models/Characters/RealisticCast/VillageAunt/", "f012")
            };
        }
    }
}
