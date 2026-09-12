using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace MeixiEcho
{
    /// <summary>Small runtime-only volume; static architecture remains bakeable.</summary>
    public sealed class ChapterThreeAtmosphereRuntime : MonoBehaviour
    {
        private VolumeProfile runtimeProfile;
        private Font runtimeWorldFont;
        private Material runtimeWorldTextMaterial;

        private void Awake()
        {
            RebindWorldTextForBuild();

            Camera camera = Camera.main;
            if (camera != null)
            {
                UniversalAdditionalCameraData data = camera.GetComponent<UniversalAdditionalCameraData>() ??
                                                     camera.gameObject.AddComponent<UniversalAdditionalCameraData>();
                data.renderPostProcessing = true;
                data.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
                data.antialiasingQuality = AntialiasingQuality.High;
            }

            GameObject volumeObject = new("ChapterThree_CinematicVolume");
            volumeObject.transform.SetParent(transform, false);
            Volume volume = volumeObject.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 20f;
            runtimeProfile = ScriptableObject.CreateInstance<VolumeProfile>();
            runtimeProfile.name = "ChapterThree_RuntimeAtmosphere";
            volume.profile = runtimeProfile;

            Tonemapping tonemapping = runtimeProfile.Add<Tonemapping>(true);
            tonemapping.mode.Override(TonemappingMode.ACES);

            ColorAdjustments color = runtimeProfile.Add<ColorAdjustments>(true);
            color.postExposure.Override(.42f);
            color.contrast.Override(7f);
            color.saturation.Override(-4f);
            color.colorFilter.Override(new Color(1f, .965f, .91f));

            Bloom bloom = runtimeProfile.Add<Bloom>(true);
            bloom.intensity.Override(.20f);
            bloom.threshold.Override(1.18f);
            bloom.scatter.Override(.52f);

            Vignette vignette = runtimeProfile.Add<Vignette>(true);
            vignette.intensity.Override(.085f);
            vignette.smoothness.Override(.66f);

            FilmGrain grain = runtimeProfile.Add<FilmGrain>(true);
            grain.type.Override(FilmGrainLookup.Thin1);
            grain.intensity.Override(.075f);
            grain.response.Override(.72f);
        }

        private void RebindWorldTextForBuild()
        {
            // Fonts created while the detailed scene is baked are not portable Unity
            // assets. Recreate the CJK-capable font in the player and give every
            // plaque a depth-tested material so glyphs cannot render through walls.
            runtimeWorldFont = Font.CreateDynamicFontFromOSFont(
                new[] { "Microsoft YaHei", "Microsoft JhengHei", "SimHei", "Arial Unicode MS", "Arial" }, 64);
            if (runtimeWorldFont == null) return;

            Shader depthShader = Shader.Find("MeixiEcho/WorldTextDepth");
            runtimeWorldTextMaterial = depthShader != null
                ? new Material(depthShader) { name = "Chapter3_WorldTextDepth_Runtime" }
                : new Material(runtimeWorldFont.material) { name = "Chapter3_WorldTextDepth_RuntimeFallback" };
            runtimeWorldTextMaterial.mainTexture = runtimeWorldFont.material.mainTexture;
            if (runtimeWorldTextMaterial.HasProperty("_ZTest"))
                runtimeWorldTextMaterial.SetInt("_ZTest", (int)CompareFunction.LessEqual);
            runtimeWorldTextMaterial.renderQueue = (int)RenderQueue.AlphaTest + 8;

            TextMesh[] labels = FindObjectsByType<TextMesh>(FindObjectsInactive.Include);
            for (int i = 0; i < labels.Length; i++)
            {
                TextMesh label = labels[i];
                if (label == null || (!label.name.StartsWith("PlaqueText_") && !label.name.StartsWith("WorldText_"))) continue;
                label.font = runtimeWorldFont;
                MeshRenderer renderer = label.GetComponent<MeshRenderer>();
                if (renderer != null)
                {
                    renderer.sharedMaterial = runtimeWorldTextMaterial;
                    renderer.shadowCastingMode = ShadowCastingMode.Off;
                    renderer.receiveShadows = false;
                }
            }
        }

        private void OnDestroy()
        {
            if (runtimeProfile == null) return;
            if (Application.isPlaying)
            {
                Destroy(runtimeProfile);
                if (runtimeWorldTextMaterial != null) Destroy(runtimeWorldTextMaterial);
                if (runtimeWorldFont != null) Destroy(runtimeWorldFont);
            }
            else
            {
                DestroyImmediate(runtimeProfile);
                if (runtimeWorldTextMaterial != null) DestroyImmediate(runtimeWorldTextMaterial);
                if (runtimeWorldFont != null) DestroyImmediate(runtimeWorldFont);
            }
        }
    }
}
