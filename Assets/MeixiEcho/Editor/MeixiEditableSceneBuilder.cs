#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
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
        private const string RuntimeVisualFolder = GeneratedFolder + "/RuntimeVisuals";
        private const string TaskRootName = "IDE任务流程_第一章";

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

        [MenuItem("梅溪回响/同步运行场景与任务到IDE", priority = 0)]
        public static void SyncRuntimeSceneToIde()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("请先停止播放，再同步运行场景到 IDE。");

            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || scene.path != ScenePath)
            {
                if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
                scene = File.Exists(Path.GetFullPath(ScenePath))
                    ? EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single)
                    : throw new FileNotFoundException("找不到可编辑场景，请先生成：" + ScenePath);
            }

            SyncRuntimeVisuals(scene);
            EditorUtility.DisplayDialog("梅溪回响",
                "运行场景、碰撞和第一章任务路线已同步到 Unity IDE。\n\n" +
                "Hierarchy 中可展开：\n· 第一章_仿真村落与边界远景\n· IDE任务流程_第一章",
                "确定");
        }

        // Batch/MCP entry point. Opens and saves the canonical editable scene.
        public static void SyncRuntimeSceneToIdeBatch()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            SyncRuntimeVisuals(scene);
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

            PersistGeneratedMeshes(world, GeneratedFolder);
            PersistGeneratedMaterials(world);
            PersistRenderSettings();
            PersistVolumeProfiles(world);
            CreateTaskVisualization(world);

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

        private static void SyncRuntimeVisuals(Scene scene)
        {
            GameObject previousVisual = GameObject.Find(MeixiChapterOneEnvironment.VisualRootName);
            if (previousVisual != null) UnityEngine.Object.DestroyImmediate(previousVisual);

            if (AssetDatabase.IsValidFolder(RuntimeVisualFolder)) AssetDatabase.DeleteAsset(RuntimeVisualFolder);
            EnsureFolder(RuntimeVisualFolder);

            MeixiChapterOneEnvironment.Ensure();
            MeixiGame3D game = UnityEngine.Object.FindAnyObjectByType<MeixiGame3D>(FindObjectsInactive.Include);
            game?.SyncChapterOneStoryNpcs();
            GameObject visualRoot = GameObject.Find(MeixiChapterOneEnvironment.VisualRootName);
            if (visualRoot == null)
                throw new InvalidOperationException("第一章运行时环境没有成功生成。请检查 Console 编译错误。");

            GameObject world = GameObject.Find("梅溪回响_可编辑世界");
            if (world == null)
            {
                world = new GameObject("梅溪回响_可编辑世界");
                SceneManager.MoveGameObjectToScene(world, scene);
            }
            if (visualRoot.transform.parent == null) visualRoot.transform.SetParent(world.transform, true);

            PersistGeneratedMeshes(visualRoot, RuntimeVisualFolder);
            // Character materials and small accessories live outside visualRoot, so
            // scan the complete authored world while saving into the same folder.
            PersistGeneratedMaterials(world, RuntimeVisualFolder);
            PersistVolumeProfiles(world);
            CreateTaskVisualization(world);
            RepairCameraOnObject(world.GetComponentInChildren<Camera>(true));

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
                throw new IOException("无法保存 IDE 可视化场景：" + ScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            Selection.activeGameObject = visualRoot;
            SceneView sceneView = SceneView.lastActiveSceneView;
            if (sceneView != null)
            {
                sceneView.FrameSelected();
                sceneView.Repaint();
            }
            Debug.Log("[MeixiIDE] 运行场景、持久 Mesh/Material 与第一章任务 Gizmo 已同步。"
                      + $" meshes={visualRoot.GetComponentsInChildren<MeshFilter>(true).Length}");
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
            PersistGeneratedMaterials(world, GeneratedFolder);
        }

        private static void PersistGeneratedMaterials(GameObject world, string targetFolder)
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
                        string path = $"{targetFolder}/Material_{index++:D3}_{SafeName(source.name)}.mat";
                        AssetDatabase.CreateAsset(asset, AssetDatabase.GenerateUniqueAssetPath(path));
                        saved[source] = asset;
                    }
                    materials[i] = asset;
                    changed = true;
                }
                if (changed) renderer.sharedMaterials = materials;
            }
        }

        private static void PersistGeneratedMeshes(GameObject root, string targetFolder)
        {
            var saved = new Dictionary<Mesh, Mesh>();
            int index = 0;
            foreach (MeshFilter filter in root.GetComponentsInChildren<MeshFilter>(true))
            {
                Mesh source = filter.sharedMesh;
                if (source == null || AssetDatabase.Contains(source)) continue;
                if (!saved.TryGetValue(source, out Mesh asset))
                {
                    asset = UnityEngine.Object.Instantiate(source);
                    asset.name = source.name;
                    string path = $"{targetFolder}/Mesh_{index++:D3}_{SafeName(source.name)}.asset";
                    AssetDatabase.CreateAsset(asset, AssetDatabase.GenerateUniqueAssetPath(path));
                    saved[source] = asset;
                }
                filter.sharedMesh = asset;
            }
        }

        private static void CreateTaskVisualization(GameObject world)
        {
            Transform existing = world.transform.Find(TaskRootName);
            if (existing != null) UnityEngine.Object.DestroyImmediate(existing.gameObject);

            GameObject root = new(TaskRootName) { tag = "EditorOnly" };
            root.transform.SetParent(world.transform, false);
            root.AddComponent<MeixiSceneTaskGizmos>();

            MeixiThirdPersonPlayer player = UnityEngine.Object.FindAnyObjectByType<MeixiThirdPersonPlayer>(FindObjectsInactive.Include);
            VillageNpc3D[] npcs = UnityEngine.Object.FindObjectsByType<VillageNpc3D>(FindObjectsInactive.Include);
            SoulNode3D soul = UnityEngine.Object.FindAnyObjectByType<SoulNode3D>(FindObjectsInactive.Include);
            CreekRhythmEnemy3D enemy = UnityEngine.Object.FindAnyObjectByType<CreekRhythmEnemy3D>(FindObjectsInactive.Include);
            MeixiSceneExit exit = UnityEngine.Object.FindAnyObjectByType<MeixiSceneExit>(FindObjectsInactive.Include);

            Vector3 playerPoint = player != null ? player.transform.position : new Vector3(0f, 0f, 1f);
            Vector3 npcPoint = npcs.Length > 0 ? npcs[0].transform.position : new Vector3(4f, 0f, 3f);
            if (npcs.Length > 1)
            {
                npcPoint = Vector3.zero;
                foreach (VillageNpc3D npc in npcs) npcPoint += npc.transform.position;
                npcPoint /= npcs.Length;
            }
            Vector3 soulPoint = soul != null ? soul.transform.position : new Vector3(-8f, 0f, -6f);
            Vector3 combatPoint = enemy != null ? enemy.transform.position : new Vector3(-12.4f, 0f, -5.6f);
            Vector3 exitPoint = exit != null ? exit.transform.position : new Vector3(0f, 0f, 30.6f);

            CreateTaskMarker(root.transform, "01｜村口调查：采集异常声纹", playerPoint);
            CreateTaskMarker(root.transform, "02｜村民调查：老村长 / 小孩 / 石婶", npcPoint);
            CreateTaskMarker(root.transform, "03｜溪流魂音：修复自然节拍", soulPoint);
            CreateTaskMarker(root.transform, "04｜节奏战斗：对抗失调溪灵", combatPoint);
            CreateTaskMarker(root.transform, "05｜章节出口：前往第二章古戏台", exitPoint);
        }

        private static void CreateTaskMarker(Transform parent, string name, Vector3 position)
        {
            GameObject marker = new(name) { tag = "EditorOnly" };
            marker.transform.SetParent(parent, false);
            marker.transform.position = position;
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
                VolumeProfile source = volume.HasInstantiatedProfile() ? volume.profile : volume.sharedProfile;
                if (source == null && volume.isGlobal && volume.priority >= 9f)
                {
                    source = CreateMeixiColorProfile();
                }
                if (source == null) continue;
                if (AssetDatabase.Contains(source))
                {
                    volume.sharedProfile = source;
                    continue;
                }

                VolumeProfile asset = ScriptableObject.CreateInstance<VolumeProfile>();
                asset.name = "MeixiVolumeProfile";
                string path = AssetDatabase.GenerateUniqueAssetPath($"{GeneratedFolder}/VolumeProfile_{index++:D2}.asset");
                AssetDatabase.CreateAsset(asset, path);
                foreach (VolumeComponent component in source.components)
                {
                    if (component == null) continue;
                    VolumeComponent copy = UnityEngine.Object.Instantiate(component);
                    copy.name = component.name;
                    copy.hideFlags = HideFlags.HideInHierarchy | HideFlags.HideInInspector;
                    asset.components.Add(copy);
                    AssetDatabase.AddObjectToAsset(copy, asset);
                }
                EditorUtility.SetDirty(asset);
                volume.sharedProfile = asset;
            }
        }

        private static VolumeProfile CreateMeixiColorProfile()
        {
            VolumeProfile profile = ScriptableObject.CreateInstance<VolumeProfile>();
            Bloom bloom = profile.Add<Bloom>(true);
            bloom.intensity.Override(.28f);
            bloom.threshold.Override(1.10f);
            bloom.scatter.Override(.56f);
            Vignette vignette = profile.Add<Vignette>(true);
            vignette.intensity.Override(.22f);
            vignette.smoothness.Override(.58f);
            ColorAdjustments color = profile.Add<ColorAdjustments>(true);
            color.postExposure.Override(-.08f);
            color.contrast.Override(9f);
            color.saturation.Override(4f);
            Tonemapping tonemapping = profile.Add<Tonemapping>(true);
            tonemapping.mode.Override(TonemappingMode.ACES);
            return profile;
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
