#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace MeixiEcho.Editor
{
    /// <summary>
    /// Converts the procedural prototype world into a normal, editable Unity scene.
    /// Gameplay components remain on the baked objects; only their visual placement
    /// moves out of the runtime generator.
    /// </summary>
    public static class MeixiEditableSceneBuilder
    {
        private const string ScenePath = "Assets/Scenes/MeixiEditable.unity";
        private const string GeneratedFolder = "Assets/MeixiEcho/Generated/SceneAuthoring";

        [InitializeOnLoadMethod]
        private static void ScheduleCameraRepair()
        {
            EditorApplication.delayCall += RepairLoadedEditableSceneCamera;
        }

        [MenuItem("梅溪回响/修复可编辑场景相机", priority = 2)]
        public static void RepairLoadedEditableSceneCamera()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || scene.path != ScenePath) return;

            Camera sceneCamera = UnityEngine.Object.FindAnyObjectByType<Camera>();
            if (sceneCamera == null) return;

            bool changed = false;
            if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(sceneCamera.gameObject) > 0)
            {
                GameObjectUtility.RemoveMonoBehavioursWithMissingScript(sceneCamera.gameObject);
                changed = true;
            }
            if (sceneCamera.GetComponent<ThirdPersonOrbitCamera>() == null)
            {
                sceneCamera.gameObject.AddComponent<ThirdPersonOrbitCamera>();
                changed = true;
            }

            if (!changed) return;
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[Meixi] 已修复可编辑场景的第三人称相机组件。");
        }

        [MenuItem("梅溪回响/生成可编辑地图场景", priority = 1)]
        public static void BakeEditableScene()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                throw new InvalidOperationException("请先停止播放，再生成可编辑场景。");
            }

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            BakeInternal();
            EditorUtility.DisplayDialog("梅溪回响", "可编辑场景已生成并打开：\n" + ScenePath, "确定");
        }

        // Used by batch-mode validation and by the project build pipeline.
        public static void BakeEditableSceneBatch()
        {
            BakeInternal();
        }

        private static void BakeInternal()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            EnsureFolder("Assets/Scenes");
            if (AssetDatabase.IsValidFolder(GeneratedFolder)) AssetDatabase.DeleteAsset(GeneratedFolder);
            EnsureFolder(GeneratedFolder);

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var systems = new GameObject("梅溪回响_游戏逻辑");
            var game = systems.AddComponent<MeixiGame3D>();
            var world = new GameObject("梅溪回响_可编辑世界");

            Invoke(game, "BuildMaterials");
            Invoke(game, "LoadGeneratedVfxAssets");
            Invoke(game, "BuildWorld");
            AddTheatreExitRoad(world.transform);

            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root != systems && root != world) root.transform.SetParent(world.transform, true);
            }

            PersistGeneratedMaterials(world);
            PersistRenderSettings();
            PersistVolumeProfiles(world);

            RepairCameraOnObject(world.GetComponentInChildren<Camera>(true));

            var serializedGame = new SerializedObject(game);
            serializedGame.FindProperty("useSceneAuthoredWorld").boolValue = true;
            serializedGame.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
            {
                throw new IOException("无法保存可编辑场景：" + ScenePath);
            }

            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            Selection.activeGameObject = world;
            Debug.Log("[Meixi] 可编辑地图场景生成完成：" + ScenePath);
        }

        private static void RepairCameraOnObject(Camera sceneCamera)
        {
            if (sceneCamera == null) return;
            if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(sceneCamera.gameObject) > 0)
                GameObjectUtility.RemoveMonoBehavioursWithMissingScript(sceneCamera.gameObject);
            if (sceneCamera.GetComponent<ThirdPersonOrbitCamera>() == null)
                sceneCamera.gameObject.AddComponent<ThirdPersonOrbitCamera>();
        }

        private static void Invoke(MeixiGame3D game, string methodName)
        {
            MethodInfo method = typeof(MeixiGame3D).GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            if (method == null) throw new MissingMethodException(typeof(MeixiGame3D).FullName, methodName);
            method.Invoke(game, null);
        }

        private static void PersistGeneratedMaterials(GameObject world)
        {
            var saved = new Dictionary<Material, Material>();
            int index = 0;
            foreach (Renderer renderer in world.GetComponentsInChildren<Renderer>(true))
            {
                Material[] materials = renderer.sharedMaterials;
                bool changed = false;
                for (int i = 0; i < materials.Length; i++)
                {
                    Material source = materials[i];
                    if (source == null || AssetDatabase.Contains(source)) continue;
                    if (!saved.TryGetValue(source, out Material asset))
                    {
                        asset = new Material(source) { name = source.name };
                        string path = $"{GeneratedFolder}/Material_{index++:D3}_{SafeName(source.name)}.mat";
                        AssetDatabase.CreateAsset(asset, AssetDatabase.GenerateUniqueAssetPath(path));
                        saved[source] = asset;
                    }
                    materials[i] = asset;
                    changed = true;
                }
                if (changed) renderer.sharedMaterials = materials;
            }
        }

        private static void PersistRenderSettings()
        {
            Material sky = RenderSettings.skybox;
            if (sky == null || AssetDatabase.Contains(sky)) return;
            var asset = new Material(sky) { name = sky.name };
            AssetDatabase.CreateAsset(asset, GeneratedFolder + "/MeixiSkybox.mat");
            RenderSettings.skybox = asset;
        }

        private static void PersistVolumeProfiles(GameObject world)
        {
            int index = 0;
            foreach (Volume volume in world.GetComponentsInChildren<Volume>(true))
            {
                VolumeProfile source = volume.sharedProfile;
                if (source == null || AssetDatabase.Contains(source)) continue;
                VolumeProfile asset = UnityEngine.Object.Instantiate(source);
                asset.name = "MeixiVolumeProfile";
                string path = AssetDatabase.GenerateUniqueAssetPath($"{GeneratedFolder}/VolumeProfile_{index++:D2}.asset");
                AssetDatabase.CreateAsset(asset, path);
                volume.sharedProfile = asset;
            }
        }

        private static string SafeName(string value)
        {
            foreach (char c in Path.GetInvalidFileNameChars()) value = value.Replace(c, '_');
            return string.IsNullOrWhiteSpace(value) ? "Generated" : value;
        }

        private static void EnsureFolder(string path)
        {
            string[] parts = path.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }

        // Editor-only authoring: these are normal objects saved into MeixiEditable.unity.
        // Nothing here runs in a player build.
        private static void AddTheatreExitRoad(Transform world)
        {
            foreach (BoxCollider collider in world.GetComponentsInChildren<BoxCollider>(true))
            {
                Vector3 center = collider.transform.TransformPoint(collider.center);
                if (center.z > 23f && center.z < 25f && collider.size.x > 40f)
                    UnityEngine.Object.DestroyImmediate(collider.gameObject);
            }

            Material stone = CreateMaterial("舞台路湿青石", new Color(0.20f, 0.31f, 0.31f), .42f);
            var root = new GameObject("村口湿青石-通向舞台4");
            root.transform.SetParent(world, false);
            for (int i = 0; i < 20; i++)
            {
                var stoneObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
                stoneObject.name = "湿青石_" + (i + 1).ToString("D2");
                stoneObject.transform.SetParent(root.transform, false);
                stoneObject.transform.position = new Vector3(Mathf.Sin(i * .71f) * .22f, .035f, 4.9f + i * 1.42f);
                stoneObject.transform.localScale = new Vector3(1.22f + (i % 3) * .08f, .075f, 1.08f);
                stoneObject.transform.rotation = Quaternion.Euler(0f, Mathf.Sin(i * 1.3f) * 6f, 0f);
                stoneObject.GetComponent<Renderer>().sharedMaterial = stone;
            }
            var exit = new GameObject("通向戏剧舞台_切换区域");
            exit.transform.SetParent(root.transform, false);
            exit.transform.position = new Vector3(0f, 1.5f, 30.6f);
            var box = exit.AddComponent<BoxCollider>(); box.isTrigger = true; box.size = new Vector3(4.6f, 3f, 2.6f);
            exit.AddComponent<MeixiEcho.MeixiSceneExit>();
        }

        private static Material CreateMaterial(string name, Color color, float smoothness)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var material = new Material(shader) { name = name, color = color };
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", smoothness);
            return material;
        }
    }
}
#endif
