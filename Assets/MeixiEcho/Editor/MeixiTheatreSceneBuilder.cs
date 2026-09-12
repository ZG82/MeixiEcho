#if UNITY_EDITOR
using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace MeixiEcho.Editor
{
    /// <summary>Creates a saved, editable theatre scene. This is never included in runtime code.</summary>
    public static class MeixiTheatreSceneBuilder
    {
        private const string ScenePath = "Assets/Scenes/MeixiTheatre.unity";
        private const string PreviewAssetFolder = "Assets/MeixiEcho/Generated/TheatrePreview";

        [InitializeOnLoadMethod]
        private static void ScheduleMissingPreviewRepair()
        {
            EditorApplication.delayCall += () =>
            {
                if (Application.isBatchMode || EditorApplication.isPlayingOrWillChangePlaymode) return;
                Scene active = SceneManager.GetActiveScene();
                if (!active.IsValid() || active.path != ScenePath) return;
                if (GameObject.Find("MeixiTheatre_EditorPreview") != null) return;
                BuildInternal();
                EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
                SceneView.lastActiveSceneView?.FrameSelected();
                Debug.Log("[MeixiTheatreIDE] 检测到第二章空预览，已自动恢复完整可视化场景。");
            };
        }

        [MenuItem("Meixi/Build Editable Theatre Scene", priority = 12)]
        public static void BuildEditableTheatre() { BuildInternal(); EditorSceneManager.OpenScene(ScenePath); }
        public static void EnsureSceneBatch() { BuildInternal(); }

        private static void BuildInternal()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var root = new GameObject("MeixiTheatre_RuntimeRoot");
            root.AddComponent<TheatreStageRuntime>();

            // Full EditorOnly copy: visible in Scene/Game View but stripped from the
            // Windows player. Runtime still creates its stable lightweight instance.
            var preview = new GameObject("MeixiTheatre_EditorPreview");
            preview.tag = "EditorOnly";
            TheatreRuntimeEnvironment.Ensure(preview.transform);
            PrepareEditorPreview(preview);
            PersistPreviewAssets(preview);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene("Assets/Scenes/MeixiEditable.unity", true),
                new EditorBuildSettingsScene(ScenePath, true),
                new EditorBuildSettingsScene("Assets/Scenes/MeixiChapterThree.unity", true)
            };
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            Selection.activeGameObject = preview;
            Debug.Log("[MeixiTheatreIDE] 第二章完整 EditorOnly 预览已生成，运行构建仍使用稳定动态场景。");
        }

        private static void PrepareEditorPreview(GameObject preview)
        {
            foreach (TheatreInstrument instrument in preview.GetComponentsInChildren<TheatreInstrument>(true))
                Object.DestroyImmediate(instrument);
            foreach (AudioSource audioSource in preview.GetComponentsInChildren<AudioSource>(true))
                Object.DestroyImmediate(audioSource);
            foreach (SphereCollider trigger in preview.GetComponentsInChildren<SphereCollider>(true))
                if (trigger.isTrigger) Object.DestroyImmediate(trigger);

            Camera camera = preview.GetComponentInChildren<Camera>(true);
            if (camera != null)
            {
                camera.enabled = true;
                camera.transform.position = new Vector3(10.8f, 6.5f, -7.8f);
                Vector3 focus = new Vector3(0f, 3.1f, 8.2f);
                camera.transform.rotation = Quaternion.LookRotation(focus - camera.transform.position, Vector3.up);
                camera.fieldOfView = 55f;
            }
        }

        private static void PersistPreviewAssets(GameObject preview)
        {
            if (AssetDatabase.IsValidFolder(PreviewAssetFolder)) AssetDatabase.DeleteAsset(PreviewAssetFolder);
            EnsureFolder("Assets/MeixiEcho/Generated");
            EnsureFolder(PreviewAssetFolder);

            var meshMap = new Dictionary<EntityId, Mesh>();
            int meshIndex = 0;
            foreach (MeshFilter filter in preview.GetComponentsInChildren<MeshFilter>(true))
            {
                Mesh source = filter.sharedMesh;
                if (source == null || !string.IsNullOrEmpty(AssetDatabase.GetAssetPath(source))) continue;
                EntityId sourceId = source.GetEntityId();
                if (!meshMap.TryGetValue(sourceId, out Mesh saved))
                {
                    saved = Object.Instantiate(source);
                    saved.name = source.name;
                    string path = $"{PreviewAssetFolder}/Mesh_{meshIndex++:D3}_{SafeName(source.name)}.asset";
                    AssetDatabase.CreateAsset(saved, path);
                    meshMap[sourceId] = saved;
                }
                filter.sharedMesh = saved;
            }

            var materialMap = new Dictionary<string, Material>(System.StringComparer.Ordinal);
            var textureMap = new Dictionary<EntityId, Texture2D>();
            int materialIndex = 0;
            int textureIndex = 0;
            foreach (Renderer renderer in preview.GetComponentsInChildren<Renderer>(true))
            {
                Material[] slots = renderer.sharedMaterials;
                bool changed = false;
                for (int i = 0; i < slots.Length; i++)
                {
                    Material source = slots[i];
                    if (source == null || !string.IsNullOrEmpty(AssetDatabase.GetAssetPath(source))) continue;
                    string materialKey = source.name.Replace(" (Instance)", string.Empty);
                    if (!materialMap.TryGetValue(materialKey, out Material saved))
                    {
                        saved = new Material(source) { name = materialKey };
                        PersistGeneratedTexture(saved, source, "_BaseMap", textureMap, ref textureIndex);
                        PersistGeneratedTexture(saved, source, "_MainTex", textureMap, ref textureIndex);
                        string path = $"{PreviewAssetFolder}/Material_{materialIndex++:D3}_{SafeName(materialKey)}.mat";
                        AssetDatabase.CreateAsset(saved, path);
                        materialMap[materialKey] = saved;
                    }
                    slots[i] = saved;
                    changed = true;
                }
                if (changed) renderer.sharedMaterials = slots;
            }

            int profileIndex = 0;
            foreach (Volume volume in preview.GetComponentsInChildren<Volume>(true))
            {
                VolumeProfile source = volume.profile;
                if (source == null || !string.IsNullOrEmpty(AssetDatabase.GetAssetPath(source))) continue;
                VolumeProfile saved = Object.Instantiate(source);
                saved.name = source.name;
                AssetDatabase.CreateAsset(saved, $"{PreviewAssetFolder}/Volume_{profileIndex++:D2}_{SafeName(source.name)}.asset");
                volume.sharedProfile = saved;
            }
        }

        private static void PersistGeneratedTexture(Material saved, Material source, string property,
            Dictionary<EntityId, Texture2D> textureMap, ref int textureIndex)
        {
            if (!source.HasProperty(property) || source.GetTexture(property) is not Texture2D texture) return;
            if (!string.IsNullOrEmpty(AssetDatabase.GetAssetPath(texture))) return;
            EntityId id = texture.GetEntityId();
            if (!textureMap.TryGetValue(id, out Texture2D persisted))
            {
                persisted = Object.Instantiate(texture);
                persisted.name = texture.name;
                string path = $"{PreviewAssetFolder}/Texture_{textureIndex++:D3}_{SafeName(texture.name)}.asset";
                AssetDatabase.CreateAsset(persisted, path);
                textureMap[id] = persisted;
            }
            saved.SetTexture(property, persisted);
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            string name = Path.GetFileName(path);
            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, name);
        }

        private static string SafeName(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return "Asset";
            foreach (char invalid in Path.GetInvalidFileNameChars()) value = value.Replace(invalid, '_');
            return value.Replace('/', '_').Replace('\\', '_');
        }

        private static void BuildLegacyReferenceScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var root = new GameObject("戏剧舞台_可编辑场景");
            var cream = Mat("米白墙面", new Color(.76f,.70f,.58f), .15f);
            var wood = Mat("暖红木", new Color(.25f,.07f,.035f), .35f);
            var floor = Mat("木地板", new Color(.29f,.12f,.055f), .32f);
            var gold = Mat("编钟青铜", new Color(.32f,.22f,.10f), .72f);
            var black = Mat("铁艺深色", new Color(.035f,.028f,.025f), .5f);
            var curtain = Mat("米色幕帘", new Color(.75f,.64f,.44f), .12f);
            var green = Mat("观众席地毯", new Color(.08f,.18f,.11f), .25f);
            var backdrop = Mat("梅溪古戏台实拍重构后景", Color.white, .18f);
            Texture2D backdropTexture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/MeixiEcho/Resources/Art/MeixiTheatreBackdrop.png");
            if (backdropTexture != null)
            {
                backdrop.mainTexture = backdropTexture;
                if (backdrop.HasProperty("_BaseMap")) backdrop.SetTexture("_BaseMap", backdropTexture);
            }

            Box("室内木地板", root.transform, new Vector3(0,-.12f,0), new Vector3(20,.24f,27), floor, true);
            Box("舞台木地板", root.transform, new Vector3(0,.55f,8.7f), new Vector3(16,1.1f,7.4f), floor, true);
            Box("舞台前沿", root.transform, new Vector3(0,.22f,5.05f), new Vector3(16,.45f,.38f), wood, true);
            for (int i = 0; i < 5; i++)
            {
                float top = .22f * (i + 1);
                Box("舞台中央缓步阶" + (i + 1), root.transform, new Vector3(0, top * .5f, 3.72f + i * .31f),
                    new Vector3(4.6f, top, .42f), floor, true);
            }
            Box("观众席地毯", root.transform, new Vector3(0,.02f,-6.4f), new Vector3(17,.05f,14.6f), green, false);
            Box("后墙", root.transform, new Vector3(0,4.3f,12.3f), new Vector3(20,8.6f,.3f), cream, true);
            Box("左墙", root.transform, new Vector3(-10,4.3f,0), new Vector3(.3f,8.6f,25), cream, true);
            Box("右墙", root.transform, new Vector3(10,4.3f,0), new Vector3(.3f,8.6f,25), cream, true);
            Box("天花板", root.transform, new Vector3(0,8.6f,0), new Vector3(20,.25f,25), cream, false);
            Box("红色舞台横梁", root.transform, new Vector3(0,7.8f,4.8f), new Vector3(20,.35f,.46f), wood, false);
            for (int x=-8; x<=8; x+=4) { Curtain(root.transform, new Vector3(x,6.65f,9.9f), curtain); Lamp(root.transform, new Vector3(x,6.9f,4.8f), gold); }
            BuildBackdrop(root.transform, backdrop, wood, black);
            BuildChime(root.transform, gold, black);
            BuildDrum(root.transform, wood, gold, black);
            BuildGuzheng(root.transform, wood, gold);
            BuildYangqin(root.transform, wood, gold);
            BuildAudience(root.transform, black, wood);
            BuildWindows(root.transform, black, cream);
            BuildPlayerAndCamera(root.transform, wood);

            for (int side = -1; side <= 1; side += 2)
            {
                Cylinder("戏台朱漆立柱", root.transform, new Vector3(side * 8.25f, 4.15f, 5.12f), new Vector3(.34f, 4.15f, .34f), wood, true);
                Box("戏台侧栏", root.transform, new Vector3(side * 7.55f, 1.55f, 7.2f), new Vector3(.16f, 1.25f, 4.2f), wood, false);
            }
            for (int i = -4; i <= 4; i++)
                Box("舞台金色地面嵌线", root.transform, new Vector3(i * 1.65f, 1.112f, 8.45f), new Vector3(.018f, .012f, 6.1f), gold, false);

            var light = new GameObject("暖色主灯").AddComponent<Light>(); light.type=LightType.Directional; light.color=new Color(1f,.72f,.42f); light.intensity=1.25f; light.shadows=LightShadows.Soft; light.transform.rotation=Quaternion.Euler(50,-25,0);
            for(int i=-1;i<=1;i++){ var fill=new GameObject("舞台暖光").AddComponent<Light>(); fill.type=LightType.Point; fill.color=new Color(1f,.48f,.16f); fill.range=12; fill.intensity=5; fill.transform.position=new Vector3(i*5,6,6); }
            for (int i = -1; i <= 1; i++)
            {
                var spot = new GameObject("舞台聚焦灯").AddComponent<Light>();
                spot.type = LightType.Spot; spot.color = i == 0 ? new Color(1f,.62f,.28f) : new Color(.42f,.72f,1f);
                spot.range = 22f; spot.intensity = i == 0 ? 9f : 6f; spot.spotAngle = 42f; spot.shadows = LightShadows.Soft;
                spot.transform.position = new Vector3(i * 6.2f, 7.7f, 2.8f);
                spot.transform.rotation = Quaternion.LookRotation(new Vector3(i * 2.4f, 2.2f, 9.0f) - spot.transform.position);
            }
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(.42f,.48f,.52f);
            RenderSettings.ambientEquatorColor = new Color(.20f,.16f,.13f);
            RenderSettings.ambientGroundColor = new Color(.045f,.035f,.028f);
            RenderSettings.fog = true; RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = new Color(.12f,.10f,.085f); RenderSettings.fogDensity = .008f;
            var runtime = root.AddComponent<TheatreStageRuntime>();
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene("Assets/Scenes/MeixiEditable.unity", true),
                new EditorBuildSettingsScene(ScenePath, true),
                new EditorBuildSettingsScene("Assets/Scenes/MeixiChapterThree.unity", true)
            };
            AssetDatabase.SaveAssets(); AssetDatabase.Refresh();
        }

        private static void BuildPlayerAndCamera(Transform root, Material coat)
        {
            var player=new GameObject("戏台访客"); player.transform.SetParent(root); player.transform.position=new Vector3(0,.08f,-10.5f); var cc=player.AddComponent<CharacterController>(); cc.height=1.82f; cc.radius=.34f; cc.center=new Vector3(0,.91f,0); cc.stepOffset=.38f; cc.slopeLimit=52f; cc.skinWidth=.045f;
            var body=GameObject.CreatePrimitive(PrimitiveType.Capsule); body.name="舞台角色加载占位"; body.transform.SetParent(player.transform,false); body.transform.localPosition=Vector3.up*.9f; body.GetComponent<Renderer>().sharedMaterial=coat; Object.DestroyImmediate(body.GetComponent<Collider>());
            var cam=new GameObject("戏台第三人称相机"); cam.transform.SetParent(root); cam.tag="MainCamera"; cam.AddComponent<Camera>().fieldOfView=58; cam.AddComponent<AudioListener>();
        }
        private static void BuildBackdrop(Transform root, Material backdrop, Material wood, Material black)
        {
            Box("梅溪古戏台数字后景",root,new Vector3(0,4.62f,12.08f),new Vector3(15.8f,7.0f,.055f),backdrop,false);
            Box("后景上框",root,new Vector3(0,8.14f,11.92f),new Vector3(16.4f,.24f,.22f),wood,false);
            Box("后景左框",root,new Vector3(-8.02f,4.62f,11.92f),new Vector3(.24f,7.25f,.22f),wood,false);
            Box("后景右框",root,new Vector3(8.02f,4.62f,11.92f),new Vector3(.24f,7.25f,.22f),wood,false);
            Box("舞台木质腰线",root,new Vector3(0,1.35f,11.75f),new Vector3(19,.55f,.25f),wood,false);
        }
        private static void BuildChime(Transform root, Material bronze, Material dark)
        {
            var r=new GameObject("中央编钟_可互动");r.transform.SetParent(root);r.transform.position=new Vector3(0,1.6f,9.6f); r.AddComponent<TheatreInstrument>().Configure("中央编钟","敲响编钟，作为剧情演出的节拍起点",1f);
            for(int level=0;level<3;level++){ float y=level*1.05f; Box("编钟横梁",r.transform,new Vector3(0,y+2.5f,0),new Vector3(6.8f,.22f,.24f),dark,false); for(int i=0;i<7-level;i++){ var bell=Cylinder("编钟",r.transform,new Vector3((i-(6-level)/2f)*.82f,y+1.85f,0),new Vector3(.24f,.38f,.24f),bronze,false); bell.transform.rotation=Quaternion.Euler(0,0,180); } }
            for(int x=-1;x<=1;x+=2){ Cylinder("编钟立柱",r.transform,new Vector3(x*3.1f,1.7f,0),new Vector3(.18f,1.8f,.18f),dark,false); }
        }
        private static void BuildDrum(Transform root, Material wood, Material gold, Material dark)
        {
            var r=new GameObject("大鼓_可互动");r.transform.SetParent(root);r.transform.position=new Vector3(-6.1f,1.45f,9.0f);r.AddComponent<TheatreInstrument>().Configure("大鼓","击鼓触发低沉鼓点与舞台提示",.52f);
            var drum=Cylinder("鼓面",r.transform,new Vector3(0,1.6f,0),new Vector3(1.15f,.45f,1.15f),wood,false);drum.transform.rotation=Quaternion.Euler(90,0,0); Cylinder("鼓架左",r.transform,new Vector3(-1.15f,.8f,0),new Vector3(.14f,.9f,.14f),dark,false); Cylinder("鼓架右",r.transform,new Vector3(1.15f,.8f,0),new Vector3(.14f,.9f,.14f),dark,false); Sphere("鼓钉",r.transform,new Vector3(0,1.6f,-.49f),new Vector3(.22f,.22f,.22f),gold,false);
        }
        private static void BuildGuzheng(Transform root, Material wood, Material gold)
        {
            var r=new GameObject("古筝_可互动");r.transform.SetParent(root);r.transform.position=new Vector3(5.5f,1.18f,7.2f);r.transform.rotation=Quaternion.Euler(0,-14,0);r.AddComponent<TheatreInstrument>().Configure("古筝","拨动古筝，演出时可作为旋律互动点",1.5f); Box("古筝琴身",r.transform,new Vector3(0,.75f,0),new Vector3(3.7f,.18f,.68f),wood,false); for(int i=0;i<11;i++) Box("古筝弦",r.transform,new Vector3(0,.87f,-.27f+i*.054f),new Vector3(3.3f,.014f,.012f),gold,false); for(int x=-1;x<=1;x+=2) Cylinder("古筝琴腿",r.transform,new Vector3(x*1.35f,.37f,0),new Vector3(.07f,.38f,.07f),wood,false);
        }
        private static void BuildYangqin(Transform root, Material wood, Material gold)
        { var r=new GameObject("扬琴_可互动");r.transform.SetParent(root);r.transform.position=new Vector3(-4.9f,1.05f,6.65f);r.AddComponent<TheatreInstrument>().Configure("扬琴","敲击扬琴，补充明亮的节奏声部",1.92f); Box("扬琴琴箱",r.transform,new Vector3(0,.68f,0),new Vector3(2.3f,.18f,1.2f),wood,false); for(int i=0;i<9;i++)Box("扬琴弦",r.transform,new Vector3(0,.80f,-.43f+i*.11f),new Vector3(2.05f,.012f,.012f),gold,false); }
        private static void BuildAudience(Transform root, Material dark, Material wood)
        { for(int row=0;row<5;row++)for(int col=-3;col<=3;col++){var p=new Vector3(col*2.18f,.55f,-1.2f-row*2.15f);Box("简化观众椅",root,p,new Vector3(.82f,.58f,.75f),dark,false);Box("椅背",root,p+new Vector3(0,.58f,.28f),new Vector3(.82f,.58f,.12f),dark,false);if(row%2==0)Cylinder("小圆桌",root,p+new Vector3(.8f,.42f,-.3f),new Vector3(.42f,.04f,.42f),wood,false);}}
        private static void BuildWindows(Transform root, Material dark, Material cream)
        { for(int z=-7;z<=4;z+=4){Box("中式格栅窗",root,new Vector3(-9.82f,4.1f,z),new Vector3(.08f,2.8f,2.6f),dark,false);for(int i=-1;i<=1;i++)Box("格栅",root,new Vector3(-9.7f,4.1f,z+i*.68f),new Vector3(.12f,2.1f,.07f),cream,false);} }
        private static void Curtain(Transform root, Vector3 p, Material mat){for(int i=-3;i<=3;i++)Cylinder("舞台幕帘",root,p+new Vector3(i*.3f,0,0),new Vector3(.13f,1.2f,.13f),mat,false);}
        private static void Lamp(Transform root, Vector3 p, Material mat){var l=Cylinder("暖色吊灯",root,p,new Vector3(.38f,.24f,.38f),mat,false);var light=l.gameObject.AddComponent<Light>();light.type=LightType.Point;light.range=5;light.intensity=1.2f;light.color=new Color(1f,.55f,.22f);}
        private static GameObject Box(string n,Transform p,Vector3 pos,Vector3 scale,Material m,bool collider){var o=GameObject.CreatePrimitive(PrimitiveType.Cube);o.name=n;o.transform.SetParent(p,false);o.transform.localPosition=pos;o.transform.localScale=scale;o.GetComponent<Renderer>().sharedMaterial=m;if(!collider)Object.DestroyImmediate(o.GetComponent<Collider>());return o;}
        private static GameObject Cylinder(string n,Transform p,Vector3 pos,Vector3 scale,Material m,bool collider){var o=GameObject.CreatePrimitive(PrimitiveType.Cylinder);o.name=n;o.transform.SetParent(p,false);o.transform.localPosition=pos;o.transform.localScale=scale;o.GetComponent<Renderer>().sharedMaterial=m;if(!collider)Object.DestroyImmediate(o.GetComponent<Collider>());return o;}
        private static GameObject Sphere(string n,Transform p,Vector3 pos,Vector3 scale,Material m,bool collider){var o=GameObject.CreatePrimitive(PrimitiveType.Sphere);o.name=n;o.transform.SetParent(p,false);o.transform.localPosition=pos;o.transform.localScale=scale;o.GetComponent<Renderer>().sharedMaterial=m;if(!collider)Object.DestroyImmediate(o.GetComponent<Collider>());return o;}
        private static Material Mat(string n,Color c,float smooth){var s=Shader.Find("Universal Render Pipeline/Lit")??Shader.Find("Standard");var m=new Material(s){name=n,color=c};if(m.HasProperty("_BaseColor"))m.SetColor("_BaseColor",c);if(m.HasProperty("_Smoothness"))m.SetFloat("_Smoothness",smooth);return m;}
    }
}
#endif
