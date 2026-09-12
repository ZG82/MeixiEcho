#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MeixiEcho.Editor
{
    /// <summary>Lightweight validation entry point for the standalone chapter-three scene.</summary>
    public static class MeixiChapterThreeSceneBuilder
    {
        private const string ScenePath = "Assets/Scenes/MeixiChapterThree.unity";
        private const string GeneratedFolder = "Assets/MeixiEcho/Generated/ChapterThree";

        [MenuItem("Meixi Echo/Chapter Three/Preview Homestay Scene", priority = 30)]
        public static void Preview()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            ChapterThreeHomestayRuntime runtime = UnityEngine.Object.FindAnyObjectByType<ChapterThreeHomestayRuntime>();
            if (runtime == null) throw new InvalidOperationException("Chapter three runtime root is missing.");
            runtime.BuildPreview();
            Selection.activeGameObject = GameObject.Find("ChapterThree_Homestay_Environment");
            SceneView.lastActiveSceneView?.FrameSelected();
            Debug.Log("[MeixiChapterThree] Homestay scene preview created.");
        }

        [MenuItem("Meixi Echo/Chapter Three/Bake Detailed Homestay Scene", priority = 31)]
        public static void BakeDetailedScene()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            ChapterThreeHomestayRuntime runtime = UnityEngine.Object.FindAnyObjectByType<ChapterThreeHomestayRuntime>();
            if (runtime == null) throw new InvalidOperationException("Chapter three runtime root is missing.");
            runtime.BuildPreview();
            GameObject environment = GameObject.Find("ChapterThree_Homestay_Environment");
            if (environment == null) throw new InvalidOperationException("Detailed homestay environment was not generated.");

            EnsureFolder(GeneratedFolder);
            PersistGeneratedAssets(environment);
            MarkStaticGeometry(environment);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
                throw new InvalidOperationException("Could not save the baked chapter-three scene.");
            try
            {
                StaticOcclusionCulling.Clear();
                if (!StaticOcclusionCulling.Compute())
                    Debug.LogWarning("[MeixiChapterThree] Occlusion bake did not produce data; scene remains valid without it.");
                else EditorSceneManager.SaveScene(scene, ScenePath);
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[MeixiChapterThree] Occlusion bake skipped: {exception.Message}");
            }
            AssetDatabase.SaveAssets();
            Debug.Log($"[MeixiChapterThree] Detailed authored scene baked: renderers={environment.GetComponentsInChildren<Renderer>(true).Length}, " +
                      $"colliders={environment.GetComponentsInChildren<Collider>(true).Length}.");
        }

        public static void BakeDetailedSceneBatch() => BakeDetailedScene();

        public static void ValidateBatch()
        {
            BakeDetailedScene();
            GameObject root = GameObject.Find("ChapterThree_Homestay_Environment");
            int renderers = root != null ? root.GetComponentsInChildren<Renderer>(true).Length : 0;
            int lights = root != null ? root.GetComponentsInChildren<Light>(true).Length : 0;
            int colliders = root != null ? root.GetComponentsInChildren<Collider>(true).Length : 0;
            Camera camera = UnityEngine.Object.FindAnyObjectByType<Camera>();
            ChapterThreeGameDirector director = root != null ? root.GetComponent<ChapterThreeGameDirector>() : null;
            if (root == null || camera == null || director == null || renderers < 600 || lights < 12 || colliders < 110)
                throw new InvalidOperationException($"Chapter three scene validation failed: renderers={renderers}, lights={lights}, colliders={colliders}, camera={(camera != null)}, director={(director != null)}");
            Debug.Log($"[MeixiChapterThree] VALID renderers={renderers} lights={lights} colliders={colliders} director=true.");
        }

        private static void PersistGeneratedAssets(GameObject environment)
        {
            var materialMap = new Dictionary<Material, Material>();
            foreach (Renderer renderer in environment.GetComponentsInChildren<Renderer>(true))
            {
                Material source = renderer.sharedMaterial;
                if (source == null || AssetDatabase.Contains(source)) continue;
                if (!materialMap.TryGetValue(source, out Material persistent))
                {
                    string path = $"{GeneratedFolder}/{SafeName(source.name)}.mat";
                    persistent = AssetDatabase.LoadAssetAtPath<Material>(path);
                    if (persistent == null)
                    {
                        persistent = new Material(source) { name = source.name };
                        AssetDatabase.CreateAsset(persistent, path);
                    }
                    else EditorUtility.CopySerialized(source, persistent);
                    materialMap[source] = persistent;
                }
                renderer.sharedMaterial = persistent;
            }

            var meshMap = new Dictionary<Mesh, Mesh>();
            foreach (MeshFilter filter in environment.GetComponentsInChildren<MeshFilter>(true))
            {
                Mesh source = filter.sharedMesh;
                if (source == null || AssetDatabase.Contains(source)) continue;
                if (!meshMap.TryGetValue(source, out Mesh persistent))
                {
                    string path = $"{GeneratedFolder}/{SafeName(source.name)}.asset";
                    persistent = AssetDatabase.LoadAssetAtPath<Mesh>(path);
                    Mesh clone = UnityEngine.Object.Instantiate(source);
                    clone.name = source.name;
                    if (clone.uv2 == null || clone.uv2.Length == 0)
                    {
                        try { Unwrapping.GenerateSecondaryUVSet(clone); }
                        catch (Exception exception) { Debug.LogWarning($"[MeixiChapterThree] UV2 generation skipped for {clone.name}: {exception.Message}"); }
                    }
                    if (persistent == null)
                    {
                        persistent = clone;
                        AssetDatabase.CreateAsset(persistent, path);
                    }
                    else
                    {
                        EditorUtility.CopySerialized(clone, persistent);
                        UnityEngine.Object.DestroyImmediate(clone);
                    }
                    meshMap[source] = persistent;
                }
                filter.sharedMesh = persistent;
            }

            Material sky = RenderSettings.skybox;
            if (sky != null && !AssetDatabase.Contains(sky))
            {
                string path = $"{GeneratedFolder}/Chapter3_ProceduralSky.mat";
                Material persistentSky = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (persistentSky == null)
                {
                    persistentSky = new Material(sky) { name = "Chapter3_ProceduralSky" };
                    AssetDatabase.CreateAsset(persistentSky, path);
                }
                else EditorUtility.CopySerialized(sky, persistentSky);
                RenderSettings.skybox = persistentSky;
            }
        }

        private static void MarkStaticGeometry(GameObject environment)
        {
            foreach (Renderer renderer in environment.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer is ParticleSystemRenderer || renderer.GetComponent<TextMesh>() != null) continue;
                if (HasMovingDoorParent(renderer.transform))
                {
                    GameObjectUtility.SetStaticEditorFlags(renderer.gameObject, 0);
                    continue;
                }

                StaticEditorFlags flags = StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccludeeStatic |
                                          StaticEditorFlags.ReflectionProbeStatic;
                Material material = renderer.sharedMaterial;
                bool alphaClip = material != null && material.IsKeywordEnabled("_ALPHATEST_ON");
                bool transparent = material != null &&
                    ((material.HasProperty("_Surface") && material.GetFloat("_Surface") > .5f) ||
                     string.Equals(material.GetTag("RenderType", false), "Transparent", StringComparison.OrdinalIgnoreCase) ||
                     (material.shader != null && material.shader.name == "MeixiEcho/StableTransparentPane"));
                // Transparent panes must remain separate renderers. Static batching
                // replaces their four-vertex meshes with huge scene-wide combined
                // meshes, which breaks per-pane depth ordering and causes window
                // frames/glass to shimmer as the camera moves.
                if (transparent)
                {
                    GameObjectUtility.SetStaticEditorFlags(renderer.gameObject, 0);
                    continue;
                }
                if (!alphaClip && !transparent && IsLargeOccluder(renderer)) flags |= StaticEditorFlags.OccluderStatic;
                GameObjectUtility.SetStaticEditorFlags(renderer.gameObject, flags);
            }
        }

        private static bool HasMovingDoorParent(Transform value)
        {
            for (Transform current = value; current != null; current = current.parent)
                if (current.name == "OutwardOpeningGuestDoor" || current.name == "BathroomDoorPivot" ||
                    current.name == "WardrobeDoorLeftPivot" || current.name == "WardrobeDoorRightPivot") return true;
            return false;
        }

        private static bool IsLargeOccluder(Renderer renderer)
        {
            Vector3 size = renderer.bounds.size;
            if (size.x * size.y * size.z < 1.25f) return false;
            string label = renderer.name;
            return label.Contains("Wall") || label.Contains("Floor") || label.Contains("Ceiling") ||
                   label.Contains("RoofSlope") || label.Contains("Foundation") || label.Contains("Gallery") ||
                   label.Contains("Facade") || label.Contains("LandscapeGround") || label.Contains("GrassBank");
        }

        private static void EnsureFolder(string folder)
        {
            string[] segments = folder.Split('/');
            string current = segments[0];
            for (int i = 1; i < segments.Length; i++)
            {
                string next = current + "/" + segments[i];
                if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, segments[i]);
                current = next;
            }
        }

        private static string SafeName(string value)
        {
            foreach (char invalid in Path.GetInvalidFileNameChars()) value = value.Replace(invalid, '_');
            return string.IsNullOrWhiteSpace(value) ? "Chapter3_Asset" : value;
        }
    }
}
#endif
