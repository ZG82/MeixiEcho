using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace MeixiEcho
{
    /// <summary>
    /// Runtime visual authoring for chapter one. Detailed geometry is merged into a
    /// small number of renderers while a separate low-cost proxy layer supplies only
    /// the house and creek-bank collision required for believable traversal.
    /// </summary>
    public static class MeixiChapterOneEnvironment
    {
        private const string LegacyVisualRootName = "第一章_仿真村落与边界远景";
        private const string LegacyVisualRootNameV4 = "第一章_仿真村落与边界远景_v4";
        private const string LegacyVisualRootNameV5 = "第一章_仿真村落与边界远景_v5";
        private const string LegacyVisualRootNameV6 = "第一章_仿真村落与边界远景_v6";
        private const string LegacyVisualRootNameV7 = "第一章_仿真村落与边界远景_v7";
        private const string LegacyVisualRootNameV8 = "第一章_仿真村落与边界远景_v8";
        public const string VisualRootName = "第一章_仿真村落与边界远景_v9";
        private const string RealisticVisualName = "写实动作角色_完整蒙皮_v3";
        private const string LegacyRealisticVisualName = "写实角色模型";
        private const string LegacyRealisticVisualNameV2 = "写实角色模型_完整蒙皮_v2";
        private static Material sharedHumanMaterial;
        private static Material sharedPlayerMaterial;
        private static readonly HashSet<string> NonInteractiveColliderNames = new(StringComparer.Ordinal)
        {
            "竹叶云", "橄榄树自然分枝", "橄榄树冠", "环形音粒", "梅溪水面", "梯田浅水",
            "乡灯青瓦帽", "暖色竹编乡灯", "阿禾耳机", "waterplant_A", "waterplant_B", "waterlily_A"
        };

        public static void Ensure()
        {
            OptimizeAuthoredDecorations();
            UpgradeCharacterVisuals();
            GameObject legacyShell = GameObject.Find(LegacyVisualRootName);
            if (legacyShell != null) legacyShell.SetActive(false);
            GameObject legacyShellV4 = GameObject.Find(LegacyVisualRootNameV4);
            if (legacyShellV4 != null) legacyShellV4.SetActive(false);
            GameObject legacyShellV5 = GameObject.Find(LegacyVisualRootNameV5);
            if (legacyShellV5 != null) legacyShellV5.SetActive(false);
            GameObject legacyShellV6 = GameObject.Find(LegacyVisualRootNameV6);
            if (legacyShellV6 != null) legacyShellV6.SetActive(false);
            GameObject legacyShellV7 = GameObject.Find(LegacyVisualRootNameV7);
            if (legacyShellV7 != null) legacyShellV7.SetActive(false);
            GameObject legacyShellV8 = GameObject.Find(LegacyVisualRootNameV8);
            if (legacyShellV8 != null) legacyShellV8.SetActive(false);
            if (GameObject.Find(VisualRootName) != null) return;

            GameObject root = new(VisualRootName) { isStatic = true };
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = new Color(.56f, .70f, .72f);
            RenderSettings.fogStartDistance = 48f;
            RenderSettings.fogEndDistance = 138f;
            MeshBuilder plaster = new();
            MeshBuilder stone = new();
            MeshBuilder timber = new();
            MeshBuilder roof = new();
            MeshBuilder glass = new();
            MeshBuilder outerGrass = new();
            MeshBuilder mountains = new();
            MeshBuilder treeBark = new();
            MeshBuilder foliage = new();
            MeshBuilder water = new();
            MeshBuilder playableWater = new();
            MeshBuilder creekBed = new();
            MeshBuilder naturalRocks = new();
            MeshBuilder decals = new();
            MeshBuilder surfaceDecals = new();
            MeshBuilder grassPatches = new();
            MeshBuilder grassBlades = new();
            MeshBuilder playableGrass = new();

            AddPlayableGround(playableGrass, creekBed);

            // Foreground houses frame the playable road. Rendering and collision stay
            // separate so the detailed meshes never become expensive MeshColliders.
            AddHouse(plaster, stone, timber, roof, glass, decals, new Vector3(13.7f, 0f, 9.4f), 198f, 1.08f, 0);
            AddHouse(plaster, stone, timber, roof, glass, decals, new Vector3(-14.2f, 0f, 13.8f), 154f, .94f, 4);
            AddHouse(plaster, stone, timber, roof, glass, decals, new Vector3(18.2f, .38f, 18.8f), 214f, .76f, 8);
            AddHouseSurfaceDetails(surfaceDecals, new Vector3(13.7f, 0f, 9.4f), 198f, 1.08f, 0);
            AddHouseSurfaceDetails(surfaceDecals, new Vector3(-14.2f, 0f, 13.8f), 154f, .94f, 5);
            AddHouseSurfaceDetails(surfaceDecals, new Vector3(18.2f, .38f, 18.8f), 214f, .76f, 9);
            AddTerraceWalls(stone, new Vector3(0f, -.18f, 19.5f));
            AddPlayableCreek(playableWater, creekBed, surfaceDecals);
            AddDetailedStoneBridge(stone, timber);
            AddPlayableRockClusters(naturalRocks);
            AddBoundaryFenceVisuals(stone, timber);
            AddPlayableNaturalVegetation(treeBark, foliage, grassBlades);

            // Outside the playable boundary: complete village blocks, fields and mountain layers.
            AddOuterVillage(plaster, stone, timber, roof, glass, decals);
            AddOuterLandscape(outerGrass, mountains, stone, treeBark, foliage, water, grassPatches);

            Texture2D atlas = Resources.Load<Texture2D>("Textures/Realistic/Generated/MeixiVillageMaterialAtlas");
            Texture2D decalAtlas = Resources.Load<Texture2D>("Textures/Realistic/Generated/MeixiVillageDecals");
            Texture2D surfaceDecalAtlas = Resources.Load<Texture2D>("Textures/Realistic/Generated/MeixiSurfaceDecals");
            Texture2D foliageAtlas = Resources.Load<Texture2D>("Textures/Realistic/Generated/MeixiFoliageAtlas");
            Texture2D grass = Resources.Load<Texture2D>("Textures/Realistic/HumidGrassGround");
            Texture2D mossyBluestone = Resources.Load<Texture2D>("Textures/Realistic/MossyBluestone");
            Texture2D dampGround = Resources.Load<Texture2D>("Textures/Realistic/Generated/MeixiDampVillageGround");
            Texture2D meadowGround = Resources.Load<Texture2D>("Textures/Realistic/Generated/MeixiMeadowGround");

            CreateMeshObject(root.transform, "潮湿灰白夯土墙", plaster,
                CreateAtlasMaterial("仿真旧灰墙", atlas, new Vector2(0f, .5f), new Color(.94f, .92f, .86f), .08f), true, true);
            CreateMeshObject(root.transform, "湿润梅溪青石", stone,
                CreateAtlasMaterial("仿真湿青石", atlas, new Vector2(.5f, .5f), new Color(.82f, .88f, .88f), .16f), true, true);
            CreateMeshObject(root.transform, "闽式杉木构架", timber,
                CreateAtlasMaterial("仿真老杉木", atlas, new Vector2(.5f, 0f), new Color(.88f, .78f, .68f), .20f), true, true);
            CreateMeshObject(root.transform, "橄榄与竹林自然树皮", treeBark,
                CreateAtlasMaterial("湿润自然树皮", atlas, new Vector2(.5f, 0f), new Color(.58f, .46f, .34f), .12f), true, true);
            CreateMeshObject(root.transform, "闽式层叠灰瓦", roof,
                CreateAtlasMaterial("仿真灰瓦", atlas, new Vector2(0f, 0f), new Color(.78f, .82f, .84f), .20f), true, true);
            CreateMeshObject(root.transform, "窗内暗面与玻璃", glass,
                CreateMaterial("旧窗玻璃", new Color(.055f, .10f, .105f, .94f), .70f), false, true);
            CreateMeshObject(root.transform, "边界内外连续湿润草地", outerGrass,
                CreateTexturedMaterial("边界内外统一湿润草地", meadowGround != null ? meadowGround : grass,
                    new Color(.86f, .90f, .80f), .035f, new Vector2(12f, 12f)), false, true);
            CreateMeshObject(root.transform, "梅溪多层写实远山", mountains,
                CreateTexturedMaterial("远山岩土与植被", dampGround != null ? dampGround : grass,
                    new Color(.56f, .64f, .54f), .05f, new Vector2(9f, 9f)), false, true);
            CreateMeshObject(root.transform, "边界外复层林带", foliage,
                CreateDoubleSidedFoliageMaterial("远景与可玩区常绿叶片", foliageAtlas, new Color(.88f, .92f, .84f), .03f), false, true);
            CreateMeshObject(root.transform, "四向统一草皮贴花", grassPatches,
                CreateTexturedMaterial("统一湿润草皮贴花", grass, new Color(.72f, .82f, .68f), .04f, Vector2.one), false, true);
            CreateMeshObject(root.transform, "可玩区写实湿润草地", playableGrass,
                CreateTexturedMaterial("可玩区湿润草地", meadowGround != null ? meadowGround : dampGround != null ? dampGround : grass,
                    new Color(.86f, .90f, .80f), .035f, new Vector2(12f, 12f)), false, true);
            CreateMeshObject(root.transform, "可玩区湿润草叶", grassBlades,
                CreateDoubleSidedFoliageMaterial("写实草叶", null, new Color(.17f, .34f, .16f), .02f), false, true);
            CreateMeshObject(root.transform, "溪床湿润卵石", creekBed,
                CreateTexturedMaterial("溪床湿青石", mossyBluestone, new Color(.48f, .56f, .55f), .20f,
                    new Vector2(5f, 2.2f)), false, true);
            CreateMeshObject(root.transform, "可玩区自然山石", naturalRocks,
                CreateTexturedMaterial("苔痕自然山石", mossyBluestone, new Color(.72f, .78f, .76f), .13f,
                    new Vector2(1.8f, 1.8f)), true, true);
            CreateMeshObject(root.transform, "边界外梅溪水脉", water,
                CreateTransparentMaterial("远景溪水", null, new Color(.10f, .34f, .38f, .78f), .78f), false, true);
            CreateMeshObject(root.transform, "连续弯曲梅溪水面", playableWater,
                CreateCreekWaterMaterial(), false, false);
            CreateMeshObject(root.transform, "村落岁月痕迹贴花", decals,
                CreateTransparentMaterial("仿真村落贴花", decalAtlas, Color.white, .04f), false, false);
            CreateMeshObject(root.transform, "地板与溪岸仿真细节", surfaceDecals,
                CreateTransparentMaterial("生成式地表细节贴花", surfaceDecalAtlas, Color.white, .06f), false, false);

            CreateLowCostCollisionProxies(root.transform);
            root.AddComponent<MeixiRuntimePerformanceProbe>();

            Debug.Log($"[MeixiScene] Photoreal chapter-one shell v9 ready: 18 combined renderers, " +
                      $"{decals.QuadCount + surfaceDecals.QuadCount} decals, visible creek, grounded trees/rocks, " +
                      "dense foliage, detailed modular collision and fence-aligned air walls.");
        }

        private static void OptimizeAuthoredDecorations()
        {
            foreach (Collider collider in UnityEngine.Object.FindObjectsByType<Collider>(FindObjectsInactive.Include))
            {
                Bounds bounds = collider != null ? collider.bounds : default;
                if (collider != null && Mathf.Abs(bounds.center.x + 10.5f) < .35f &&
                    Mathf.Abs(bounds.center.z + 10.3f) < .45f && bounds.size.z > 7.5f && bounds.size.x > 2.8f)
                {
                    // Superseded by the arched, segmented bridge deck below.
                    collider.enabled = false;
                    continue;
                }
                if (collider != null && collider.enabled &&
                    (NonInteractiveColliderNames.Contains(collider.gameObject.name) ||
                     IsReplacedLowDetailVisual(collider.transform))) collider.enabled = false;
            }

            foreach (Renderer renderer in UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include))
            {
                if (renderer == null) continue;
                Vector3 scale = renderer.transform.lossyScale;
                if (renderer.TryGetComponent(out BoxCollider groundCollider) &&
                    scale.x >= 45f && scale.z >= 45f && scale.y <= 1.5f)
                {
                    // Keep the authored primitive as the stable traversal floor, but
                    // hide it so the carved creek-bank mesh reveals the lowered water.
                    renderer.enabled = false;
                    continue;
                }
                string objectName = renderer.gameObject.name;
                if (objectName == "梅溪水面")
                {
                    // The authored overlapping cubes are replaced by one continuous
                    // irregular ribbon, eliminating seams and transparent overdraw.
                    renderer.enabled = false;
                    continue;
                }
                if (objectName.Contains("waterlily", StringComparison.OrdinalIgnoreCase))
                {
                    // The legacy asset is a bright low-poly disk with a triangular
                    // cutout. It read as a game token on top of the rebuilt creek.
                    renderer.enabled = false;
                    renderer.forceRenderingOff = true;
                    continue;
                }
                if (IsLegacyBoundaryVisual(renderer.transform))
                {
                    // Keep the authored colliders as invisible air walls. Their old
                    // stepped meshes are replaced by the continuous outer landscape.
                    renderer.enabled = false;
                    continue;
                }
                if (IsReplacedLowDetailVisual(renderer.transform))
                {
                    // The runtime shell supplies grounded detailed houses, branched
                    // trees and bamboo foliage. Keep the old colliders, but avoid
                    // drawing both visual sets on top of each other.
                    renderer.enabled = false;
                    renderer.forceRenderingOff = true;
                    continue;
                }
                if (objectName.Contains("竹叶云", StringComparison.Ordinal) ||
                    objectName.Contains("waterplant", StringComparison.OrdinalIgnoreCase) ||
                    objectName.Contains("waterlily", StringComparison.OrdinalIgnoreCase) ||
                    objectName.Contains("环形音粒", StringComparison.Ordinal) ||
                    objectName.Contains("音符头高光", StringComparison.Ordinal) ||
                    objectName.Contains("弯曲音符旗", StringComparison.Ordinal))
                {
                    renderer.shadowCastingMode = ShadowCastingMode.Off;
                    renderer.receiveShadows = false;
                }
                foreach (Material material in renderer.sharedMaterials)
                    if (material != null) material.enableInstancing = true;
            }

            foreach (Light light in UnityEngine.Object.FindObjectsByType<Light>(FindObjectsInactive.Include))
            {
                if (light == null) continue;
                if (light.type == LightType.Point)
                {
                    light.shadows = LightShadows.None;
                    light.intensity = Mathf.Min(light.intensity, 1.10f);
                    light.range = Mathf.Min(light.range, 6.5f);
                }
                else if (light.type == LightType.Directional)
                {
                    light.intensity = Mathf.Min(light.intensity, 1.12f);
                }
            }
        }

        private static bool IsReplacedLowDetailVisual(Transform transform)
        {
            for (Transform current = transform; current != null; current = current.parent)
            {
                string name = current.name;
                if (name.Equals("闽清古厝", StringComparison.Ordinal) ||
                    name.Contains("橄榄树", StringComparison.Ordinal) ||
                    name.Equals("竹林", StringComparison.Ordinal) ||
                    name.Equals("竹叶云", StringComparison.Ordinal)) return true;
            }
            return false;
        }

        private static bool IsLegacyBoundaryVisual(Transform transform)
        {
            for (Transform current = transform; current != null; current = current.parent)
            {
                string name = current.name;
                if (name.Contains("梅溪远山", StringComparison.Ordinal) ||
                    name.Contains("北山界", StringComparison.Ordinal) ||
                    name.Contains("南山界", StringComparison.Ordinal) ||
                    name.Contains("东山界", StringComparison.Ordinal) ||
                    name.Contains("西山界", StringComparison.Ordinal)) return true;
            }
            return false;
        }

        private static void UpgradeCharacterVisuals()
        {
            GameObject fallbackAsset = Resources.Load<GameObject>("Models/Characters/LinMo") ??
                                       Resources.Load<GameObject>("Models/Characters/RealisticMale/Models/DefaultMale");
            GameObject playerAsset = MeixiRealisticCast.Load(MeixiCastRole.LinMo) ?? fallbackAsset;
            if (playerAsset == null)
            {
                Debug.LogError("[MeixiCharacter] No playable character resource is available in the standalone build.");
                return;
            }

            MeixiGame3D game = UnityEngine.Object.FindAnyObjectByType<MeixiGame3D>();
            ThirdPersonOrbitCamera cameraRig = UnityEngine.Object.FindAnyObjectByType<ThirdPersonOrbitCamera>();
            MeixiThirdPersonPlayer player = UnityEngine.Object.FindAnyObjectByType<MeixiThirdPersonPlayer>();
            if (player != null)
            {
                Transform model = EnsureRealisticBody(player.transform, playerAsset, MeixiCastRole.LinMo,
                    1.90f, Color.white, true, true);
                if (model != null)
                {
                    Renderer[] characterRenderers = model.GetComponentsInChildren<Renderer>(true);
                    Bounds visibleBounds = CalculateBounds(model.gameObject);
                    Renderer primary = characterRenderers.Length > 0 ? characterRenderers[0] : null;
                    Debug.Log($"[MeixiCharacter] Player visual={model.name} active={model.gameObject.activeInHierarchy} " +
                              $"position={model.position} scale={model.lossyScale} bounds={visibleBounds.center}/{visibleBounds.size} " +
                              $"renderers={characterRenderers.Length} rendererEnabled={primary != null && primary.enabled} " +
                              $"forceOff={primary != null && primary.forceRenderingOff} shader={primary?.sharedMaterial?.shader?.name}");
                }
                if (model != null && game != null) player.Configure(game, cameraRig, model);
            }

            VillageNpc3D[] npcs = UnityEngine.Object.FindObjectsByType<VillageNpc3D>(FindObjectsInactive.Include);
            for (int i = 0; i < npcs.Length; i++)
            {
                VillageNpc3D npc = npcs[i];
                if (npc == null) continue;
                MeixiCastRole role = MeixiRealisticCast.RoleForNpc(npc.DisplayName, i);
                GameObject npcAsset = MeixiRealisticCast.Load(role) ?? fallbackAsset;
                float height = role == MeixiCastRole.VillageChild ? 1.42f :
                    role == MeixiCastRole.VillageAunt ? 1.64f : 1.70f;
                Color tint = role switch
                {
                    MeixiCastRole.VillageChild => new Color(1.08f, 1.06f, 1.04f),
                    MeixiCastRole.VillageAunt => new Color(1.02f, .97f, .93f),
                    _ => new Color(.94f, .91f, .86f)
                };
                Transform npcModel = EnsureRealisticBody(npc.transform, npcAsset, role, height, tint, false, true);
                MeixiRealisticCast.ApplyTint(npcModel != null ? npcModel.gameObject : null, tint,
                    role == MeixiCastRole.VillageChild ? .38f : .24f);
                KeepOnlyNpcCharacterModel(npc.transform, npcModel);
                ConfigureNpcPresentation(npcModel, role);
                if (game != null && player != null) npc.Rebind(game, player.transform);
            }

            CreekRhythmEnemy3D enemy = UnityEngine.Object.FindAnyObjectByType<CreekRhythmEnemy3D>(FindObjectsInactive.Include);
            if (enemy != null)
            {
                EnsureRealisticBody(enemy.transform, fallbackAsset, MeixiCastRole.LinMo, 1.94f,
                    new Color(.38f, .78f, .88f), false, false);
                if (game != null && player != null) enemy.Rebind(game, player.transform);
            }
        }

        private static void KeepOnlyNpcCharacterModel(Transform npcRoot, Transform model)
        {
            if (npcRoot == null || model == null) return;
            foreach (Transform child in npcRoot.GetComponentsInChildren<Transform>(true))
            {
                if (child == npcRoot || child == model || child.IsChildOf(model)) continue;
                // The root keeps VillageNpc3D and its interaction collider. Everything
                // else is old hats, cylinders, ellipses, labels or duplicate bodies.
                child.gameObject.SetActive(false);
            }
        }

        private static void ConfigureNpcPresentation(Transform model, MeixiCastRole role)
        {
            if (model == null) return;
            MeixiNpcPresentation legacyPresentation = model.GetComponent<MeixiNpcPresentation>();
            if (legacyPresentation != null) legacyPresentation.enabled = false;
            Animator animator = model.GetComponent<Animator>() ?? model.GetComponentInChildren<Animator>(true);
            if (animator == null || !animator.isActiveAndEnabled) return;
            animator.SetFloat("Speed", 0f);
            float phase = role switch
            {
                MeixiCastRole.VillageElder => .12f,
                MeixiCastRole.VillageChild => .48f,
                MeixiCastRole.VillageAunt => .76f,
                _ => 0f
            };
            animator.Play(0, 0, phase);
            animator.speed = role switch
            {
                MeixiCastRole.VillageElder => .62f,
                MeixiCastRole.VillageChild => .92f,
                MeixiCastRole.VillageAunt => .74f,
                _ => 1f
            };
            animator.Update(0f);
            NaturalHumanoidLocomotionPolish posture = model.GetComponent<NaturalHumanoidLocomotionPolish>();
            if (posture == null) posture = model.gameObject.AddComponent<NaturalHumanoidLocomotionPolish>();
            posture.ConfigureNpc(role switch
            {
                MeixiCastRole.VillageElder => 0,
                MeixiCastRole.VillageChild => 1,
                MeixiCastRole.VillageAunt => 2,
                _ => 3
            });
            posture.enabled = true;
        }

        private static Transform EnsureRealisticBody(Transform owner, GameObject asset, MeixiCastRole castRole,
            float targetHeight, Color tint,
            bool playerBody, bool guardArmPose)
        {
            Transform legacyVisual = owner.Find(LegacyRealisticVisualName);
            if (legacyVisual != null) legacyVisual.gameObject.SetActive(false);
            Transform legacyVisualV2 = owner.Find(LegacyRealisticVisualNameV2);
            if (legacyVisualV2 != null) legacyVisualV2.gameObject.SetActive(false);
            Transform existing = owner.Find(RealisticVisualName);
            string castMarkerName = "角色资源_" + asset.name;
            if (existing != null && existing.Find(castMarkerName) == null)
            {
                existing.name = RealisticVisualName + "_已替换";
                existing.gameObject.SetActive(false);
                existing = null;
            }
            if (existing != null)
            {
                // Older baked scenes may still contain the late-update arm guard. It
                // competed with Humanoid retargeting and was the main source of the
                // stiff / snapping shoulder motion reported during locomotion.
                NaturalArmSwingCorrection legacyGuard = existing.GetComponent<NaturalArmSwingCorrection>();
                if (legacyGuard != null) legacyGuard.enabled = false;
                if (playerBody) DisableLegacyPlayerAdditiveGeometry(existing);
                MeixiRealisticCast.PrepareVisual(existing.gameObject, castRole, playerBody);
                ConfigureRealisticAnimator(existing.gameObject);

                // Keep exactly one skinned character body. Older baked scenes may
                // retain the original model beside the upgraded body; its unsupported
                // material rendered as a magenta T-pose in Windows builds.
                foreach (Animator candidate in owner.GetComponentsInChildren<Animator>(true))
                {
                    if (candidate == null || candidate.transform == existing || candidate.transform.IsChildOf(existing)) continue;
                    candidate.gameObject.SetActive(false);
                }
                if (playerBody)
                {
                    EnsureLocomotionPolish(existing);
                }
                return existing;
            }

            Animator oldAnimator = owner.GetComponentInChildren<Animator>(true);
            Transform oldVisual = oldAnimator != null ? oldAnimator.transform : owner.childCount > 0 ? owner.GetChild(0) : null;
            GameObject model = UnityEngine.Object.Instantiate(asset, owner);
            model.name = RealisticVisualName;
            model.transform.SetSiblingIndex(0);
            model.transform.localPosition = Vector3.zero;
            model.transform.localRotation = Quaternion.identity;
            model.transform.localScale = Vector3.one;

            MeixiRealisticCast.PrepareVisual(model, castRole, playerBody);

            Bounds bounds = MeixiRealisticCast.CalculateVisibleBounds(model);
            float scale = bounds.size.y > .01f ? targetHeight / bounds.size.y : 1f;
            model.transform.localScale = Vector3.one * scale;
            bounds = MeixiRealisticCast.CalculateVisibleBounds(model);
            model.transform.position += new Vector3(owner.position.x - bounds.center.x, owner.position.y - bounds.min.y, owner.position.z - bounds.center.z);

            GameObject castMarker = new(castMarkerName);
            castMarker.transform.SetParent(model.transform, false);
            Animator animator = ConfigureRealisticAnimator(model);
            ProceduralHumanoidFallback fallback = model.GetComponent<ProceduralHumanoidFallback>();
            if (fallback == null) fallback = model.AddComponent<ProceduralHumanoidFallback>();
            // Humanoid retargeting drives this higher-detail mesh. Keep the procedural
            // component dormant as an emergency fallback if the controller is missing.
            fallback.enabled = animator == null || animator.runtimeAnimatorController == null;
            if (oldVisual != null && oldVisual != model.transform) oldVisual.gameObject.SetActive(false);

            // A small layered field jacket breaks up the generic base-body silhouette
            // without adding another skinned mesh.
            if (playerBody)
            {
                EnsureLocomotionPolish(model.transform);
            }
            return model.transform;
        }

        private static void ApplyCharacterVisualMaterial(GameObject model, Color tint, bool playerBody)
        {
            if (playerBody) ApplyPlayerCharacterMaterial(model);
            else ApplyRealisticCharacterMaterial(model, tint);
        }

        private static void ApplyPlayerCharacterMaterial(GameObject model)
        {
            if (model == null) return;
            if (sharedPlayerMaterial == null)
            {
                Texture2D albedo = Resources.Load<Texture2D>("Models/Characters/100Avatars_069_Kyle_Refined");
                if (albedo == null) albedo = Resources.Load<Texture2D>("Models/Characters/100Avatars_069_Kyle");
                sharedPlayerMaterial = CreateTexturedMaterial("林默写实调研服与面部", albedo, Color.white, .18f, Vector2.one);
                if (sharedPlayerMaterial.HasProperty("_Metallic")) sharedPlayerMaterial.SetFloat("_Metallic", 0f);
                if (sharedPlayerMaterial.HasProperty("_Smoothness")) sharedPlayerMaterial.SetFloat("_Smoothness", .16f);
                if (sharedPlayerMaterial.HasProperty("_SpecColor"))
                    sharedPlayerMaterial.SetColor("_SpecColor", new Color(.16f, .15f, .14f, 1f));
                if (sharedPlayerMaterial.HasProperty("_Surface")) sharedPlayerMaterial.SetFloat("_Surface", 0f);
                if (sharedPlayerMaterial.HasProperty("_AlphaClip")) sharedPlayerMaterial.SetFloat("_AlphaClip", 0f);
                if (sharedPlayerMaterial.HasProperty("_ZWrite")) sharedPlayerMaterial.SetFloat("_ZWrite", 1f);
                sharedPlayerMaterial.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
                sharedPlayerMaterial.DisableKeyword("_ALPHATEST_ON");
                sharedPlayerMaterial.enableInstancing = true;
                sharedPlayerMaterial.renderQueue = (int)RenderQueue.Geometry;
            }
            foreach (Renderer renderer in model.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer == null) continue;
                Material[] slots = renderer.sharedMaterials;
                if (slots.Length == 0) renderer.sharedMaterial = sharedPlayerMaterial;
                else
                {
                    for (int i = 0; i < slots.Length; i++) slots[i] = sharedPlayerMaterial;
                    renderer.sharedMaterials = slots;
                }
                renderer.enabled = true;
                renderer.forceRenderingOff = false;
                renderer.shadowCastingMode = ShadowCastingMode.On;
                renderer.receiveShadows = true;
                if (renderer is SkinnedMeshRenderer skinned)
                {
                    skinned.updateWhenOffscreen = true;
                    Bounds stableBounds = skinned.sharedMesh != null ? skinned.sharedMesh.bounds : skinned.localBounds;
                    stableBounds.Expand(new Vector3(.35f, .28f, .35f));
                    skinned.localBounds = stableBounds;
                }
            }
        }

        private static void DisableLegacyPlayerAdditiveGeometry(Transform model)
        {
            if (model == null) return;
            // The former jacket lapels, hair cap and backpack were primitive meshes
            // authored against a different skeleton scale. On LinMo's rig they stretch
            // into long capsules, cover the face and intersect the shoulders during a
            // walk cycle. The skinned FBX and its UV atlas now own the entire visible
            // body, so old baked scenes are cleaned without touching the actual rig.
            foreach (Transform child in model.GetComponentsInChildren<Transform>(true))
            {
                if (child == null || child == model) continue;
                string childName = child.name;
                if (childName == "林默调研装备" ||
                    childName == "林默写实外观细节_v2" ||
                    childName == "调研设备轻量背包")
                {
                    child.gameObject.SetActive(false);
                }
            }
        }

        private static void ApplyRealisticCharacterMaterial(GameObject model, Color tint)
        {
            if (model == null) return;
            Material humanMaterial = GetHumanMaterial();
            // Keep face/skin albedo natural. NPC identity colors are now a restrained
            // cloth-grade tint instead of multiplying the whole face into grey/brown.
            Color materialTint = Color.Lerp(Color.white, tint, .20f);
            materialTint.a = 1f;
            bool playerModel = model.GetComponentInParent<MeixiThirdPersonPlayer>() != null;
            foreach (Renderer renderer in model.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer == null) continue;
                Material[] slots = renderer.sharedMaterials;
                if (slots.Length == 0) renderer.sharedMaterial = humanMaterial;
                else
                {
                    for (int i = 0; i < slots.Length; i++) slots[i] = humanMaterial;
                    renderer.sharedMaterials = slots;
                }
                renderer.shadowCastingMode = ShadowCastingMode.On;
                renderer.receiveShadows = true;
                renderer.forceRenderingOff = false;
                if (playerModel && renderer is SkinnedMeshRenderer skinned)
                {
                    // Humanoid retargeting changes bone extents after the baked FBX
                    // bounds were calculated. Keep only the player updating offscreen
                    // so the camera cannot cull the visible body while its shadow pass
                    // still renders; the small NPC crowd retains cheaper static bounds.
                    skinned.updateWhenOffscreen = true;
                    Bounds stableBounds = skinned.localBounds;
                    stableBounds.Expand(new Vector3(.45f, .35f, .45f));
                    skinned.localBounds = stableBounds;
                }
                MaterialPropertyBlock block = new();
                renderer.GetPropertyBlock(block);
                block.SetColor("_BaseColor", materialTint);
                block.SetColor("_Color", materialTint);
                renderer.SetPropertyBlock(block);
            }
        }

        private static Animator ConfigureRealisticAnimator(GameObject model)
        {
            if (model == null) return null;
            Animator animator = model.GetComponent<Animator>() ?? model.GetComponentInChildren<Animator>(true);
            if (animator == null) return null;
            RuntimeAnimatorController controller = Resources.Load<RuntimeAnimatorController>("Animations/LinMoCombat");
            if (controller != null) animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.enabled = animator.runtimeAnimatorController != null;
            if (animator.enabled)
            {
                animator.Rebind();
                if (animator.isActiveAndEnabled) animator.Update(0f);
            }
            ProceduralHumanoidFallback fallback = model.GetComponent<ProceduralHumanoidFallback>();
            if (fallback != null) fallback.enabled = !animator.enabled;
            return animator;
        }

        private static Material GetHumanMaterial()
        {
            if (sharedHumanMaterial != null) return sharedHumanMaterial;
            const string root = "Models/Characters/RealisticMale/Textures/";
            Texture2D albedo = Resources.Load<Texture2D>(root + "DefaultMale_Albedo");
            Texture2D normal = Resources.Load<Texture2D>(root + "DefaultMale_Normal");
            Texture2D occlusion = Resources.Load<Texture2D>(root + "DefaultMale_Occlusion");
            Texture2D specular = Resources.Load<Texture2D>(root + "DefaultMale_SpecSmooth");
            sharedHumanMaterial = CreateTexturedMaterial("写实人物皮肤与衣物", albedo, Color.white, .28f, Vector2.one);
            if (normal != null && sharedHumanMaterial.HasProperty("_BumpMap"))
            {
                sharedHumanMaterial.SetTexture("_BumpMap", normal);
                sharedHumanMaterial.SetFloat("_BumpScale", .62f);
                sharedHumanMaterial.EnableKeyword("_NORMALMAP");
            }
            if (occlusion != null && sharedHumanMaterial.HasProperty("_OcclusionMap"))
            {
                sharedHumanMaterial.SetTexture("_OcclusionMap", occlusion);
                sharedHumanMaterial.SetFloat("_OcclusionStrength", .78f);
            }
            if (specular != null && sharedHumanMaterial.HasProperty("_SpecGlossMap"))
            {
                sharedHumanMaterial.SetTexture("_SpecGlossMap", specular);
            }
            // Runtime-only specular keyword combinations can be stripped from a
            // standalone URP build. Use the guaranteed opaque metallic workflow with
            // zero metalness; the normal/occlusion maps still supply facial detail.
            if (sharedHumanMaterial.HasProperty("_WorkflowMode")) sharedHumanMaterial.SetFloat("_WorkflowMode", 1f);
            if (sharedHumanMaterial.HasProperty("_Metallic")) sharedHumanMaterial.SetFloat("_Metallic", 0f);
            if (sharedHumanMaterial.HasProperty("_Surface")) sharedHumanMaterial.SetFloat("_Surface", 0f);
            if (sharedHumanMaterial.HasProperty("_AlphaClip")) sharedHumanMaterial.SetFloat("_AlphaClip", 0f);
            if (sharedHumanMaterial.HasProperty("_ZWrite")) sharedHumanMaterial.SetFloat("_ZWrite", 1f);
            if (sharedHumanMaterial.HasProperty("_Cull")) sharedHumanMaterial.SetFloat("_Cull", (float)CullMode.Back);
            sharedHumanMaterial.DisableKeyword("_SPECULAR_SETUP");
            sharedHumanMaterial.DisableKeyword("_SPECGLOSSMAP");
            sharedHumanMaterial.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
            sharedHumanMaterial.DisableKeyword("_ALPHATEST_ON");
            sharedHumanMaterial.renderQueue = (int)RenderQueue.Geometry;
            // A restrained specular response keeps facial planes readable while the
            // jacket remains cloth-like instead of making the whole body look plastic.
            if (sharedHumanMaterial.HasProperty("_SpecColor"))
                sharedHumanMaterial.SetColor("_SpecColor", new Color(.24f, .23f, .22f, 1f));
            if (sharedHumanMaterial.HasProperty("_Smoothness")) sharedHumanMaterial.SetFloat("_Smoothness", .22f);
            return sharedHumanMaterial;
        }

        private static void AddPlayerAccessories(Transform model)
        {
            if (model.Find("林默调研装备") != null) return;

            Transform legacyPack = model.Find("调研设备轻量背包");
            if (legacyPack != null) legacyPack.gameObject.SetActive(false);

            Animator animator = model.GetComponent<Animator>();
            Transform chest = animator != null ? animator.GetBoneTransform(HumanBodyBones.Chest) : null;
            if (chest == null) return;

            Material canvas = CreateMaterial("林默深蓝调研帆布", new Color(.035f, .085f, .14f), .20f);
            Material trim = CreateMaterial("林默调研装备皮革包边", new Color(.12f, .075f, .045f), .26f);
            GameObject rig = new("林默调研装备");
            rig.transform.SetParent(chest, false);

            // Rounded primitives follow the animated chest bone and read as a soft
            // field pack rather than the previous rigid cube clipped into the torso.
            GameObject pack = CreateAccessoryPrimitive(PrimitiveType.Capsule, "调研帆布背包主体", rig.transform,
                new Vector3(0f, .015f, -.185f), Quaternion.Euler(0f, 0f, 90f), new Vector3(.105f, .205f, .255f), canvas);
            CreateAccessoryPrimitive(PrimitiveType.Capsule, "背包上盖", rig.transform,
                new Vector3(0f, .155f, -.205f), Quaternion.Euler(0f, 0f, 90f), new Vector3(.115f, .155f, .235f), canvas);
            CreateAccessoryPrimitive(PrimitiveType.Cylinder, "背包束带", rig.transform,
                new Vector3(0f, .02f, -.292f), Quaternion.Euler(0f, 0f, 90f), new Vector3(.022f, .205f, .022f), trim);

            // A compact recorder on the lower side gives the electronic-information
            // student a clearer silhouette while staying clear of elbows and hips.
            CreateAccessoryPrimitive(PrimitiveType.Capsule, "声纹采集器", rig.transform,
                new Vector3(.205f, -.205f, -.085f), Quaternion.Euler(0f, 0f, 16f), new Vector3(.055f, .085f, .040f), trim);
        }

        private static void AddPlayerAppearanceDetails(Transform model)
        {
            if (model == null || model.Find("林默写实外观细节_v2") != null) return;
            Animator animator = model.GetComponent<Animator>() ?? model.GetComponentInChildren<Animator>(true);
            if (animator == null || animator.avatar == null || !animator.avatar.isHuman) return;

            Transform chest = animator.GetBoneTransform(HumanBodyBones.Chest);
            Transform head = animator.GetBoneTransform(HumanBodyBones.Head);
            if (chest == null || head == null) return;

            GameObject appearance = new("林默写实外观细节_v2");
            appearance.transform.SetParent(model, false);
            Material jacket = CreateMaterial("林默靛青调研夹克织物", new Color(.025f, .075f, .125f), .14f);
            Material jacketEdge = CreateMaterial("林默夹克深色滚边", new Color(.015f, .025f, .035f), .11f);
            Material hair = CreateMaterial("林默自然黑短发", new Color(.018f, .014f, .012f), .12f);

            GameObject clothing = new("轻量调研夹克层");
            clothing.transform.SetParent(chest, false);
            // Rounded, low-profile lapels and shoulder straps stay clear of the arm
            // skinning envelope. They give the torso a layered field-jacket silhouette
            // without the rigid rectangular panels that clipped during combat.
            CreateAccessoryPrimitive(PrimitiveType.Capsule, "左夹克翻领", clothing.transform,
                new Vector3(-.052f, .135f, .112f), Quaternion.Euler(0f, 0f, -23f), new Vector3(.026f, .090f, .016f), jacket);
            CreateAccessoryPrimitive(PrimitiveType.Capsule, "右夹克翻领", clothing.transform,
                new Vector3(.052f, .135f, .112f), Quaternion.Euler(0f, 0f, 23f), new Vector3(.026f, .090f, .016f), jacket);
            CreateAccessoryPrimitive(PrimitiveType.Capsule, "左调研背带前段", clothing.transform,
                new Vector3(-.112f, -.015f, .105f), Quaternion.Euler(0f, 0f, -5f), new Vector3(.022f, .175f, .014f), jacketEdge);
            CreateAccessoryPrimitive(PrimitiveType.Capsule, "右调研背带前段", clothing.transform,
                new Vector3(.112f, -.015f, .105f), Quaternion.Euler(0f, 0f, 5f), new Vector3(.022f, .175f, .014f), jacketEdge);
            CreateAccessoryPrimitive(PrimitiveType.Capsule, "夹克前襟软边", clothing.transform,
                new Vector3(0f, -.070f, .116f), Quaternion.identity, new Vector3(.010f, .185f, .008f), jacketEdge);

            // A compressed sphere reads as a close-cropped hair cap and avoids a
            // second skinned mesh. It follows the Head bone and stays above the face.
            CreateAccessoryPrimitive(PrimitiveType.Sphere, "自然层次短发", head,
                new Vector3(0f, .128f, -.010f), Quaternion.identity,
                new Vector3(.108f, .070f, .104f), hair);
        }

        private static void EnsureLocomotionPolish(Transform model)
        {
            if (model == null) return;
            NaturalArmSwingCorrection legacy = model.GetComponent<NaturalArmSwingCorrection>();
            if (legacy != null) legacy.enabled = false;
            NaturalHumanoidLocomotionPolish polish = model.GetComponent<NaturalHumanoidLocomotionPolish>();
            if (polish == null) polish = model.gameObject.AddComponent<NaturalHumanoidLocomotionPolish>();
            polish.enabled = true;
        }

        private static GameObject CreateAccessoryPrimitive(PrimitiveType type, string name, Transform parent,
            Vector3 localPosition, Quaternion localRotation, Vector3 localScale, Material material)
        {
            GameObject part = GameObject.CreatePrimitive(type);
            part.name = name;
            part.transform.SetParent(parent, false);
            part.transform.localPosition = localPosition;
            part.transform.localRotation = localRotation;
            part.transform.localScale = localScale;
            part.GetComponent<Renderer>().sharedMaterial = material;
            Collider collider = part.GetComponent<Collider>();
            if (collider != null)
            {
                if (Application.isPlaying) UnityEngine.Object.Destroy(collider);
                else UnityEngine.Object.DestroyImmediate(collider);
            }
            return part;
        }

        private static void AddHouse(MeshBuilder plaster, MeshBuilder stone, MeshBuilder timber, MeshBuilder roof,
            MeshBuilder glass, MeshBuilder decals, Vector3 origin, float yaw, float scale, int decalSeed)
        {
            // Keep a clean shadow gap above the wall cap without exposing the gable.
            // The former 14 cm lift still read as if the eaves were glued to the wall.
            const float roofLift = .30f;
            Quaternion rotation = Quaternion.Euler(0f, yaw, 0f);
            void Box(MeshBuilder target, Vector3 center, Vector3 size, Vector3 euler)
                => target.AddBox(origin + rotation * (center * scale), size * scale, rotation * Quaternion.Euler(euler));
            Vector3 Point(Vector3 local) => origin + rotation * (local * scale);

            const float width = 6.4f;
            const float depth = 4.6f;
            const float wallHeight = 3.8f;
            Box(stone, new Vector3(0f, .38f, 0f), new Vector3(width + .52f, .76f, depth + .52f), Vector3.zero);
            Box(stone, new Vector3(0f, .88f, -depth * .5f - .48f), new Vector3(2.15f, .18f, .78f), Vector3.zero);
            Box(stone, new Vector3(0f, 1.05f, -depth * .5f - .70f), new Vector3(1.75f, .18f, .42f), Vector3.zero);

            // Separate narrow boards give the doorway a readable timber floor instead
            // of one flat slab. They still end up in the single combined timber mesh.
            for (int board = -4; board <= 4; board++)
            {
                float offset = board * .64f;
                float wear = (board & 1) == 0 ? .010f : 0f;
                Box(timber, new Vector3(offset, .815f + wear, -.12f),
                    new Vector3(.60f, .105f, depth - .62f), new Vector3(0f, board * .42f, 0f));
            }

            // Irregular exterior pavers visually connect the threshold to the village road.
            for (int slab = -2; slab <= 2; slab++)
            {
                float side = slab * .72f;
                Box(stone, new Vector3(side, .88f + Mathf.Abs(slab) * .006f, -depth * .5f - 1.12f),
                    new Vector3(.66f, .10f, .72f + (slab & 1) * .08f), new Vector3(0f, slab * 4.5f, 0f));
            }

            Box(plaster, new Vector3(0f, 2.35f, depth * .5f), new Vector3(width, wallHeight, .36f), Vector3.zero);
            Box(plaster, new Vector3(-2.28f, 2.35f, -depth * .5f), new Vector3(1.84f, wallHeight, .36f), Vector3.zero);
            Box(plaster, new Vector3(2.28f, 2.35f, -depth * .5f), new Vector3(1.84f, wallHeight, .36f), Vector3.zero);
            Box(plaster, new Vector3(0f, 3.73f, -depth * .5f), new Vector3(2.72f, 1.04f, .36f), Vector3.zero);
            Box(plaster, new Vector3(-width * .5f, 2.35f, 0f), new Vector3(.36f, wallHeight, depth), Vector3.zero);
            Box(plaster, new Vector3(width * .5f, 2.35f, 0f), new Vector3(.36f, wallHeight, depth), Vector3.zero);

            // Solid front/back gables close the former daylight gap between the
            // rectangular wall cap and the pitched roof.
            Vector3 gableA = Point(new Vector3(-width * .5f + .18f, 4.16f, -depth * .5f + .02f));
            Vector3 gableB = Point(new Vector3(0f, 5.58f + roofLift, -depth * .5f + .02f));
            Vector3 gableC = Point(new Vector3(width * .5f - .18f, 4.16f, -depth * .5f + .02f));
            plaster.AddTriangularPrism(gableA, gableB, gableC, rotation * Vector3.forward * ((depth - .04f) * scale));

            // Recessed double-leaf timber door, complete jamb, lintel, threshold and
            // panel relief. Every part overlaps slightly so camera movement never
            // reveals an empty seam around the doorway.
            Box(glass, new Vector3(0f, 2.08f, -depth * .5f - .205f), new Vector3(1.78f, 2.70f, .10f), Vector3.zero);
            Box(timber, new Vector3(-.39f, 2.08f, -depth * .5f - .30f), new Vector3(.74f, 2.54f, .16f), Vector3.zero);
            Box(timber, new Vector3(.39f, 2.08f, -depth * .5f - .30f), new Vector3(.74f, 2.54f, .16f), Vector3.zero);
            Box(timber, new Vector3(-.39f, 2.08f, -depth * .5f - .395f), new Vector3(.54f, 1.62f, .055f), Vector3.zero);
            Box(timber, new Vector3(.39f, 2.08f, -depth * .5f - .395f), new Vector3(.54f, 1.62f, .055f), Vector3.zero);
            Box(timber, new Vector3(-.92f, 2.08f, -depth * .5f - .32f), new Vector3(.18f, 2.82f, .24f), Vector3.zero);
            Box(timber, new Vector3(.92f, 2.08f, -depth * .5f - .32f), new Vector3(.18f, 2.82f, .24f), Vector3.zero);
            Box(timber, new Vector3(0f, 3.48f, -depth * .5f - .32f), new Vector3(2.02f, .20f, .25f), Vector3.zero);
            Box(stone, new Vector3(0f, .80f, -depth * .5f - .42f), new Vector3(2.06f, .16f, .62f), Vector3.zero);
            Box(timber, new Vector3(-.13f, 2.03f, -depth * .5f - .445f), new Vector3(.07f, .11f, .07f), Vector3.zero);
            Box(timber, new Vector3(.13f, 2.03f, -depth * .5f - .445f), new Vector3(.07f, .11f, .07f), Vector3.zero);
            Box(timber, new Vector3(-width * .5f + .18f, 2.32f, -depth * .5f - .22f), new Vector3(.24f, 3.55f, .25f), Vector3.zero);
            Box(timber, new Vector3(width * .5f - .18f, 2.32f, -depth * .5f - .22f), new Vector3(.24f, 3.55f, .25f), Vector3.zero);
            Box(timber, new Vector3(0f, 4.12f, -depth * .5f - .22f), new Vector3(width, .24f, .25f), Vector3.zero);
            Box(timber, new Vector3(0f, 3.58f, -depth * .5f - .24f), new Vector3(width - .35f, .16f, .18f), Vector3.zero);

            AddLatticeWindow(timber, glass, origin, rotation, new Vector3(-2.15f, 2.48f, -depth * .5f - .25f), scale);
            AddLatticeWindow(timber, glass, origin, rotation, new Vector3(2.15f, 2.48f, -depth * .5f - .25f), scale);

            // Left and right panels rise toward the ridge. The previous signs made
            // both eaves higher than the centre, so some rotated houses read upside-down.
            Box(roof, new Vector3(-1.72f, 4.65f + roofLift, 0f), new Vector3(4.05f, .22f, depth + 1.55f), new Vector3(0f, 0f, 28f));
            Box(roof, new Vector3(1.72f, 4.65f + roofLift, 0f), new Vector3(4.05f, .22f, depth + 1.55f), new Vector3(0f, 0f, -28f));
            Box(roof, new Vector3(-3.50f, 3.71f + roofLift, 0f), new Vector3(.18f, .24f, depth + 1.65f), Vector3.zero);
            Box(roof, new Vector3(3.50f, 3.71f + roofLift, 0f), new Vector3(.18f, .24f, depth + 1.65f), Vector3.zero);
            // Slender longitudinal tile ridges follow the actual roof slope. They
            // break the former smooth slab silhouette without adding renderers.
            for (int side = -1; side <= 1; side += 2)
            {
                for (int row = 0; row < 8; row++)
                {
                    float x = side * (.22f + row * .44f);
                    float y = 5.60f + roofLift - Mathf.Abs(x) * Mathf.Tan(28f * Mathf.Deg2Rad) + .055f;
                    roof.AddCylinder(Point(new Vector3(x, y, 0f)), .038f * scale,
                        (depth + 1.70f) * scale, rotation * Quaternion.Euler(90f, 0f, 0f), 7);
                }
            }
            // Ridge and eave caps bridge the two roof planes and hide their cut ends.
            roof.AddCylinder(Point(new Vector3(0f, 5.60f + roofLift, 0f)), .145f * scale,
                (depth + 1.82f) * scale, rotation * Quaternion.Euler(90f, 0f, 0f), 10);
            roof.AddCylinder(Point(new Vector3(-3.51f, 3.71f + roofLift, 0f)), .105f * scale,
                (depth + 1.76f) * scale, rotation * Quaternion.Euler(90f, 0f, 0f), 9);
            roof.AddCylinder(Point(new Vector3(3.51f, 3.71f + roofLift, 0f)), .105f * scale,
                (depth + 1.76f) * scale, rotation * Quaternion.Euler(90f, 0f, 0f), 9);
            Box(timber, new Vector3(0f, 5.52f + roofLift, 0f), new Vector3(.20f, .20f, depth + 1.62f), Vector3.zero);
            for (int i = -2; i <= 2; i++)
                Box(timber, new Vector3(i * 1.22f, 3.73f, -depth * .5f - .56f), new Vector3(.13f, .34f, .82f), new Vector3(18f, 0f, 0f));

            // Continuous sill courses, corner feet and short diagonal brackets make
            // the facade read as a load-bearing timber house instead of a flat box.
            Box(stone, new Vector3(0f, 1.02f, depth * .5f + .205f), new Vector3(width + .10f, .25f, .18f), Vector3.zero);
            Box(timber, new Vector3(0f, 1.18f, -depth * .5f - .235f), new Vector3(width - .30f, .16f, .18f), Vector3.zero);
            for (int side = -1; side <= 1; side += 2)
            {
                Box(timber, new Vector3(side * 2.66f, 3.83f, -depth * .5f - .47f),
                    new Vector3(.14f, .74f, .16f), new Vector3(0f, 0f, side * 38f));
                Box(stone, new Vector3(side * 2.78f, .84f, -depth * .5f - .28f),
                    new Vector3(.58f, .30f, .54f), new Vector3(0f, side * 7f, 0f));
            }

            AddWallDecal(decals, origin, rotation, new Vector3(-2.10f, 1.20f, -depth * .5f - .372f), new Vector2(1.18f, .88f) * scale, decalSeed % 16);
            AddWallDecal(decals, origin, rotation, new Vector3(2.30f, 3.12f, -depth * .5f - .374f), new Vector2(.90f, .72f) * scale, (decalSeed + 3) % 16);
            AddGroundDecal(decals, origin + rotation * new Vector3(-1.6f, .82f, -depth * .5f - 1.18f), rotation, new Vector2(1.10f, .72f) * scale, (decalSeed + 6) % 16);
        }

        private static void AddHouseSurfaceDetails(MeshBuilder details, Vector3 origin, float yaw, float scale, int seed)
        {
            Quaternion rotation = Quaternion.Euler(0f, yaw, 0f);
            // Worn floor scuffs, damp stone marks and leaves are generated as quads in
            // the same renderer. Slight height separation avoids z-fighting.
            AddGroundDecal(details, origin + rotation * new Vector3(-1.15f, .878f, -.36f), rotation,
                new Vector2(1.32f, 1.05f) * scale, (seed + 8) % 16);
            AddGroundDecal(details, origin + rotation * new Vector3(1.25f, .880f, .48f), rotation,
                new Vector2(1.05f, .82f) * scale, (seed + 12) % 16);
            AddGroundDecal(details, origin + rotation * new Vector3(.35f, .955f, -3.46f), rotation,
                new Vector2(1.25f, .88f) * scale, (seed + 4) % 16);
            AddGroundDecal(details, origin + rotation * new Vector3(-1.45f, .958f, -3.22f), rotation,
                new Vector2(.92f, .72f) * scale, (seed + 1) % 16);
        }

        private static void AddPlayableGround(MeshBuilder grass, MeshBuilder creekBed)
        {
            const int segments = 44;
            for (int i = 0; i < segments; i++)
            {
                float x0 = Mathf.Lerp(-24f, 24f, i / (float)segments);
                float x1 = Mathf.Lerp(-24f, 24f, (i + 1f) / segments);
                float left0 = CreekCenterZ(x0) - CreekHalfWidth(x0) - .72f;
                float left1 = CreekCenterZ(x1) - CreekHalfWidth(x1) - .72f;
                float right0 = CreekCenterZ(x0) + CreekHalfWidth(x0) + .72f;
                float right1 = CreekCenterZ(x1) + CreekHalfWidth(x1) + .72f;
                float u0 = i / (float)segments;
                float u1 = (i + 1f) / segments;

                grass.AddQuad(new Vector3(x0, -.035f, -24f), new Vector3(x1, -.035f, -24f),
                    new Vector3(x1, -.035f, left1), new Vector3(x0, -.035f, left0),
                    new Rect(u0 * 12f, 0f, (u1 - u0) * 12f, 5.2f));
                grass.AddQuad(new Vector3(x0, -.035f, right0), new Vector3(x1, -.035f, right1),
                    new Vector3(x1, -.035f, 24f), new Vector3(x0, -.035f, 24f),
                    new Rect(u0 * 12f, 6.8f, (u1 - u0) * 12f, 5.2f));

                Vector3 lowerLeft0 = new(x0, -.57f, CreekCenterZ(x0) - CreekHalfWidth(x0) * .94f);
                Vector3 lowerLeft1 = new(x1, -.57f, CreekCenterZ(x1) - CreekHalfWidth(x1) * .94f);
                Vector3 lowerRight0 = new(x0, -.57f, CreekCenterZ(x0) + CreekHalfWidth(x0) * .94f);
                Vector3 lowerRight1 = new(x1, -.57f, CreekCenterZ(x1) + CreekHalfWidth(x1) * .94f);
                creekBed.AddQuad(new Vector3(x0, -.035f, left0), new Vector3(x1, -.035f, left1),
                    lowerLeft1, lowerLeft0, new Rect(u0 * 9f, 0f, (u1 - u0) * 9f, 1.35f));
                creekBed.AddQuad(lowerRight0, lowerRight1,
                    new Vector3(x1, -.035f, right1), new Vector3(x0, -.035f, right0),
                    new Rect(u0 * 9f, 1.35f, (u1 - u0) * 9f, 1.35f));
            }
        }

        private static void AddPlayableCreek(MeshBuilder water, MeshBuilder creekBed, MeshBuilder details)
        {
            const int segments = 44;
            Vector3[] left = new Vector3[segments + 1];
            Vector3[] right = new Vector3[segments + 1];
            Vector3[] bedLeft = new Vector3[segments + 1];
            Vector3[] bedRight = new Vector3[segments + 1];

            for (int i = 0; i <= segments; i++)
            {
                float t = i / (float)segments;
                float x = Mathf.Lerp(-22f, 21f, t);
                float centerZ = CreekCenterZ(x);
                float halfWidth = CreekHalfWidth(x);
                // The surface sits below the grass lip, so the creek reads as a
                // carved channel instead of a transparent sticker on the ground.
                const float y = -.305f;
                left[i] = new Vector3(x, y, centerZ - halfWidth);
                right[i] = new Vector3(x, y, centerZ + halfWidth);
                bedLeft[i] = new Vector3(x, -.57f, centerZ - halfWidth * .94f);
                bedRight[i] = new Vector3(x, -.57f, centerZ + halfWidth * .94f);
            }

            for (int i = 0; i < segments; i++)
            {
                float u0 = i / (float)segments * 9f;
                float u1 = (i + 1f) / segments * 9f;
                creekBed.AddQuad(bedLeft[i], bedLeft[i + 1], bedRight[i + 1], bedRight[i],
                    new Rect(u0, 0f, u1 - u0, 2.4f));
                water.AddQuad(left[i], left[i + 1], right[i + 1], right[i],
                    new Rect(u0, 0f, u1 - u0, 1f));
                Vector3 leftDepth0 = new(left[i].x, -.54f, left[i].z);
                Vector3 leftDepth1 = new(left[i + 1].x, -.54f, left[i + 1].z);
                Vector3 rightDepth0 = new(right[i].x, -.54f, right[i].z);
                Vector3 rightDepth1 = new(right[i + 1].x, -.54f, right[i + 1].z);
                water.AddQuad(left[i], leftDepth0, leftDepth1, left[i + 1],
                    new Rect(u0, 0f, u1 - u0, .22f));
                water.AddQuad(right[i + 1], rightDepth1, rightDepth0, right[i],
                    new Rect(u0, .78f, u1 - u0, .22f));
            }

            // Generated foam, algae, silt and pebble decals break up the ribbon edge.
            int[] waterCells = { 1, 5, 7, 9, 10, 11, 12, 14 };
            // Deliberate 2.30 m spacing keeps transparent cards from stacking over
            // one another while the alternating banks still read as a continuous wet edge.
            for (int i = 0; i < 18; i++)
            {
                float x = -19.6f + i * 2.30f;
                float z = CreekCenterZ(x);
                float side = (i & 1) == 0 ? -1f : 1f;
                float lateral = side * (1.94f + (i % 3) * .14f);
                Quaternion rotation = Quaternion.Euler(0f, -8f + Mathf.Sin(i * .81f) * 18f, 0f);
                AddGroundDecal(details, new Vector3(x, -.028f, z + lateral), rotation,
                    new Vector2(.72f + (i % 4) * .09f, .40f + (i % 3) * .08f), waterCells[i % waterCells.Length]);
            }
        }

        private static void AddDetailedStoneBridge(MeshBuilder stone, MeshBuilder timber)
        {
            Vector3 center = new(-10.5f, 0f, -10.3f);
            for (int side = -1; side <= 1; side += 2)
            {
                float endZ = center.z + side * 3.55f;
                stone.AddBox(new Vector3(center.x, .16f, endZ), new Vector3(3.75f, .48f, 1.08f), Quaternion.identity);
                for (int step = 0; step < 3; step++)
                {
                    float z = endZ + side * (.72f + step * .48f);
                    stone.AddBox(new Vector3(center.x, .075f + step * .035f, z),
                        new Vector3(3.18f - step * .14f, .18f, .62f), Quaternion.identity);
                }
            }

            for (int edge = -1; edge <= 1; edge += 2)
            {
                float x = center.x + edge * 1.56f;
                for (int post = 0; post < 7; post++)
                {
                    float t = post / 6f;
                    float z = Mathf.Lerp(center.z - 3.25f, center.z + 3.25f, t);
                    float arch = .60f + Mathf.Sin(t * Mathf.PI) * .28f;
                    stone.AddBox(new Vector3(x, arch, z), new Vector3(.28f, .76f, .32f), Quaternion.identity);
                }
                timber.AddBox(new Vector3(x, 1.08f, center.z), new Vector3(.16f, .14f, 6.75f), Quaternion.identity);
                timber.AddBox(new Vector3(x, .77f, center.z), new Vector3(.12f, .10f, 6.55f), Quaternion.identity);
            }
            stone.AddBox(new Vector3(center.x, -.38f, center.z - 2.75f), new Vector3(3.55f, .44f, .72f), Quaternion.identity);
            stone.AddBox(new Vector3(center.x, -.38f, center.z + 2.75f), new Vector3(3.55f, .44f, .72f), Quaternion.identity);
        }

        private static float CreekCenterZ(float x) =>
            -11f + Mathf.Sin((x + 15f) * .21f) * 2f + Mathf.Sin((x - 4f) * .075f) * .34f;

        private static float CreekHalfWidth(float x)
        {
            float sample = Mathf.InverseLerp(-22f, 21f, x) * 44f;
            return 2.25f + Mathf.Sin(sample * 1.73f) * .18f + Mathf.Sin(sample * .53f) * .12f;
        }

        private static void AddBoundaryFenceVisuals(MeshBuilder stone, MeshBuilder timber)
        {
            // The visible fence sits just inside the physics boundary. The north side
            // deliberately leaves a broad gate for the road to chapter two.
            AddFenceRun(stone, timber, new Vector3(-22.8f, 0f, -22.8f), new Vector3(22.8f, 0f, -22.8f));
            AddFenceRun(stone, timber, new Vector3(-22.8f, 0f, -22.8f), new Vector3(-22.8f, 0f, 22.8f));
            AddFenceRun(stone, timber, new Vector3(22.8f, 0f, -22.8f), new Vector3(22.8f, 0f, 22.8f));
            AddFenceRun(stone, timber, new Vector3(-22.8f, 0f, 22.8f), new Vector3(-4.2f, 0f, 22.8f));
            AddFenceRun(stone, timber, new Vector3(4.2f, 0f, 22.8f), new Vector3(22.8f, 0f, 22.8f));
        }

        private static void AddFenceRun(MeshBuilder stone, MeshBuilder timber, Vector3 from, Vector3 to)
        {
            Vector3 delta = to - from;
            float length = delta.magnitude;
            if (length < .1f) return;
            Vector3 tangent = delta / length;
            Quaternion rotation = Quaternion.LookRotation(tangent, Vector3.up);
            Vector3 midpoint = (from + to) * .5f;

            stone.AddBox(midpoint + Vector3.up * .12f, new Vector3(.42f, .24f, length + .25f), rotation);
            int sections = Mathf.Max(1, Mathf.CeilToInt(length / 2.35f));
            for (int i = 0; i <= sections; i++)
            {
                float t = i / (float)sections;
                Vector3 post = Vector3.Lerp(from, to, t);
                timber.AddCylinder(post + Vector3.up * .76f, .085f + (i % 3) * .008f,
                    1.50f, Quaternion.Euler(0f, i * 17f, (i % 2 == 0 ? -1f : 1f) * 1.6f), 8);
                if (i >= sections) continue;
                Vector3 next = Vector3.Lerp(from, to, (i + 1f) / sections);
                float railLength = Vector3.Distance(post, next) + .10f;
                Vector3 railCenter = (post + next) * .5f;
                timber.AddBox(railCenter + Vector3.up * .55f, new Vector3(.10f, .10f, railLength), rotation);
                timber.AddBox(railCenter + Vector3.up * 1.05f, new Vector3(.095f, .095f, railLength), rotation);
            }
        }

        private static void CreateLowCostCollisionProxies(Transform parent)
        {
            GameObject root = new("低成本建筑与溪岸碰撞代理") { isStatic = true };
            root.transform.SetParent(parent, false);

            AddHouseProxy(root.transform, new Vector3(13.7f, 0f, 9.4f), 198f, 1.08f);
            AddHouseProxy(root.transform, new Vector3(-14.2f, 0f, 13.8f), 154f, .94f);
            AddHouseProxy(root.transform, new Vector3(18.2f, .38f, 18.8f), 214f, .76f);

            int creekProxyCount = 0;
            const int creekSegments = 32;
            const float creekStartX = -21.5f;
            const float creekEndX = 21.5f;
            for (int segment = 0; segment < creekSegments; segment++)
            {
                float x = Mathf.Lerp(creekStartX, creekEndX, segment / (float)creekSegments);
                float nextX = Mathf.Lerp(creekStartX, creekEndX, (segment + 1f) / creekSegments);
                // Preserve a generous opening around the authored stone bridge.
                if (Mathf.Abs((x + nextX) * .5f + 10.5f) < 3.2f) continue;

                Vector3 from = new(x, 0f, CreekCenterZ(x));
                Vector3 to = new(nextX, 0f, CreekCenterZ(nextX));
                Vector3 tangent = (to - from).normalized;
                Vector3 normal = new(-tangent.z, 0f, tangent.x);
                Quaternion rotation = Quaternion.LookRotation(tangent, Vector3.up);
                Vector3 middle = Vector3.Lerp(from, to, .5f);
                float bankOffset = CreekHalfWidth((x + nextX) * .5f) + .27f;
                for (int side = -1; side <= 1; side += 2)
                {
                    GameObject proxy = new($"溪岸碰撞_{creekProxyCount++:00}") { isStatic = true };
                    proxy.transform.SetParent(root.transform, false);
                    proxy.transform.position = middle + normal * (side * bankOffset) + Vector3.up * .14f;
                    proxy.transform.rotation = rotation;
                    BoxCollider collider = proxy.AddComponent<BoxCollider>();
                    collider.size = new Vector3(.42f, .56f, Vector3.Distance(from, to) + .12f);
                }
            }

            AddVegetationCollisionProxies(root.transform);
            AddRockCollisionProxies(root.transform);
            AddBridgeCollisionProxies(root.transform);

            AddBoundaryAirWall(root.transform, "南侧栅栏空气墙", new Vector3(0f, 1.6f, -23.55f), new Vector3(47.1f, 3.4f, .72f));
            AddBoundaryAirWall(root.transform, "西侧栅栏空气墙", new Vector3(-23.55f, 1.6f, 0f), new Vector3(.72f, 3.4f, 47.1f));
            AddBoundaryAirWall(root.transform, "东侧栅栏空气墙", new Vector3(23.55f, 1.6f, 0f), new Vector3(.72f, 3.4f, 47.1f));
            AddBoundaryAirWall(root.transform, "北侧栅栏空气墙_左", new Vector3(-13.85f, 1.6f, 23.55f), new Vector3(19.1f, 3.4f, .72f));
            AddBoundaryAirWall(root.transform, "北侧栅栏空气墙_右", new Vector3(13.85f, 1.6f, 23.55f), new Vector3(19.1f, 3.4f, .72f));
        }

        private static void AddBoundaryAirWall(Transform parent, string name, Vector3 position, Vector3 size)
        {
            GameObject wall = new(name) { isStatic = true };
            wall.transform.SetParent(parent, false);
            wall.transform.position = position;
            BoxCollider collider = wall.AddComponent<BoxCollider>();
            collider.size = size;
        }

        private static void AddHouseProxy(Transform parent, Vector3 origin, float yaw, float scale)
        {
            GameObject proxy = new("古厝分段碰撞代理") { isStatic = true };
            proxy.transform.SetParent(parent, false);
            proxy.transform.position = origin;
            proxy.transform.rotation = Quaternion.Euler(0f, yaw, 0f);

            void AddWall(string wallName, Vector3 center, Vector3 size, Vector3 euler = default)
            {
                GameObject wall = new(wallName) { isStatic = true };
                wall.transform.SetParent(proxy.transform, false);
                wall.transform.localPosition = center * scale;
                wall.transform.localRotation = Quaternion.Euler(euler);
                BoxCollider collider = wall.AddComponent<BoxCollider>();
                collider.size = size * scale;
            }

            AddWall("古厝石基碰撞", new Vector3(0f, .38f, 0f), new Vector3(6.78f, .76f, 4.98f));
            AddWall("古厝后墙碰撞", new Vector3(0f, 2.52f, 2.30f), new Vector3(6.42f, 3.70f, .38f));
            AddWall("古厝左墙碰撞", new Vector3(-3.20f, 2.52f, 0f), new Vector3(.38f, 3.70f, 4.62f));
            AddWall("古厝右墙碰撞", new Vector3(3.20f, 2.52f, 0f), new Vector3(.38f, 3.70f, 4.62f));
            AddWall("古厝前墙左碰撞", new Vector3(-2.28f, 2.52f, -2.30f), new Vector3(1.86f, 3.70f, .38f));
            AddWall("古厝前墙右碰撞", new Vector3(2.28f, 2.52f, -2.30f), new Vector3(1.86f, 3.70f, .38f));
            AddWall("古厝门楣碰撞", new Vector3(0f, 3.86f, -2.30f), new Vector3(2.75f, 1.02f, .38f));
            AddWall("古厝门框左碰撞", new Vector3(-1.36f, 2.18f, -2.34f), new Vector3(.15f, 2.65f, .30f));
            AddWall("古厝门框右碰撞", new Vector3(1.36f, 2.18f, -2.34f), new Vector3(.15f, 2.65f, .30f));
            AddWall("古厝门槛碰撞", new Vector3(0f, .88f, -2.38f), new Vector3(2.62f, .14f, .38f));
            const float roofLift = .30f;
            AddWall("古厝左坡屋顶碰撞", new Vector3(-1.72f, 4.65f + roofLift, 0f),
                new Vector3(4.05f, .20f, 6.15f), new Vector3(0f, 0f, 28f));
            AddWall("古厝右坡屋顶碰撞", new Vector3(1.72f, 4.65f + roofLift, 0f),
                new Vector3(4.05f, .20f, 6.15f), new Vector3(0f, 0f, -28f));
        }

        private static void AddVegetationCollisionProxies(Transform parent)
        {
            void Capsule(string name, Vector3 position, float radius, float height)
            {
                GameObject proxy = new(name) { isStatic = true };
                proxy.transform.SetParent(parent, false);
                proxy.transform.position = position;
                CapsuleCollider collider = proxy.AddComponent<CapsuleCollider>();
                collider.direction = 1;
                collider.radius = radius;
                collider.height = Mathf.Max(height, radius * 2f);
                collider.center = Vector3.up * (collider.height * .5f);
            }

            for (int i = 0; i < 18; i++)
            {
                float angle = i * 137.5f * Mathf.Deg2Rad;
                float radius = 20.5f + (i % 4) * 1.15f;
                Vector3 position = new(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
                if (i % 3 == 0)
                {
                    float treeHeight = 5.2f + (i % 4) * .42f;
                    Capsule($"橄榄树干碰撞_{i:00}", position, treeHeight * .052f, treeHeight * .56f);
                }
                else
                {
                    Capsule($"竹丛碰撞_{i:00}", position, .38f, 3.35f);
                }
            }

            Vector3[] creekBamboo =
            {
                new(-19.0f, 0f, -3.5f), new(-18.0f, 0f, -11.8f), new(-6.2f, 0f, -14.8f),
                new(-2.8f, 0f, -10.8f), new(-18.5f, 0f, -16.2f), new(-10.0f, 0f, 4.2f)
            };
            for (int i = 0; i < creekBamboo.Length; i++)
                Capsule($"溪岸竹丛碰撞_{i:00}", creekBamboo[i], .46f, 3.55f);
        }

        private static void AddRockCollisionProxies(Transform parent)
        {
            Vector3[] anchors =
            {
                new(-17.8f, 0f, -7.0f), new(-11.6f, 0f, -12.2f), new(-3.8f, 0f, -8.7f),
                new(7.4f, 0f, -4.8f), new(14.8f, 0f, -1.1f), new(18.5f, 0f, 4.2f),
                new(-18.7f, 0f, 4.6f), new(4.6f, 0f, 17.8f)
            };
            for (int i = 0; i < anchors.Length; i++)
            {
                GameObject proxy = new($"自然石组碰撞_{i:00}") { isStatic = true };
                proxy.transform.SetParent(parent, false);
                proxy.transform.position = anchors[i];
                SphereCollider collider = proxy.AddComponent<SphereCollider>();
                collider.center = new Vector3(-.18f, .24f, .03f);
                collider.radius = .42f + (i % 3) * .06f;
                SphereCollider shoulder = proxy.AddComponent<SphereCollider>();
                shoulder.center = new Vector3(.32f, .18f, -.12f);
                shoulder.radius = .31f + (i % 2) * .05f;
            }
        }

        private static void AddBridgeCollisionProxies(Transform parent)
        {
            Vector3 center = new(-10.5f, 0f, -10.3f);
            const int segments = 7;
            for (int i = 0; i < segments; i++)
            {
                float t = (i + .5f) / segments;
                float z = Mathf.Lerp(center.z - 3.5f, center.z + 3.5f, t);
                float deckY = .61f + Mathf.Sin(t * Mathf.PI) * .18f;
                float slope = Mathf.Cos(t * Mathf.PI) * -7.2f;
                GameObject deck = new($"石桥桥面碰撞_{i:00}") { isStatic = true };
                deck.transform.SetParent(parent, false);
                deck.transform.position = new Vector3(center.x, deckY, z);
                deck.transform.rotation = Quaternion.Euler(slope, 0f, 0f);
                BoxCollider collider = deck.AddComponent<BoxCollider>();
                collider.size = new Vector3(3.08f, .28f, 1.18f);
            }

            for (int edge = -1; edge <= 1; edge += 2)
            {
                GameObject rail = new($"石桥护栏碰撞_{(edge < 0 ? "左" : "右")}") { isStatic = true };
                rail.transform.SetParent(parent, false);
                rail.transform.position = new Vector3(center.x + edge * 1.55f, 1.14f, center.z);
                BoxCollider collider = rail.AddComponent<BoxCollider>();
                collider.size = new Vector3(.22f, 1.05f, 7.2f);
            }
        }

        private static void AddLatticeWindow(MeshBuilder timber, MeshBuilder glass, Vector3 origin, Quaternion rotation, Vector3 localCenter, float scale)
        {
            Vector3 center = origin + rotation * (localCenter * scale);
            glass.AddBox(center + rotation * Vector3.forward * .015f, new Vector3(1.30f, 1.30f, .10f) * scale, rotation);
            void Bar(Vector3 offset, Vector3 size) => timber.AddBox(center + rotation * (offset * scale), size * scale, rotation);
            Bar(new Vector3(0f, .62f, -.05f), new Vector3(1.42f, .10f, .15f));
            Bar(new Vector3(0f, -.62f, -.05f), new Vector3(1.42f, .10f, .15f));
            Bar(new Vector3(-.66f, 0f, -.05f), new Vector3(.10f, 1.42f, .15f));
            Bar(new Vector3(.66f, 0f, -.05f), new Vector3(.10f, 1.42f, .15f));
            Bar(new Vector3(0f, 0f, -.07f), new Vector3(.07f, 1.28f, .16f));
            Bar(new Vector3(0f, 0f, -.07f), new Vector3(1.28f, .07f, .16f));
        }

        private static void AddOuterVillage(MeshBuilder plaster, MeshBuilder stone, MeshBuilder timber, MeshBuilder roof,
            MeshBuilder glass, MeshBuilder decals)
        {
            Vector3[] positions =
            {
                // North
                new(-34f, .8f, 36f), new(-13f, 1.6f, 48f), new(16f, 1.2f, 45f), new(38f, 2.7f, 57f),
                // South
                new(-37f, .9f, -39f), new(-14f, 1.4f, -51f), new(15f, 1.1f, -47f), new(39f, 2.5f, -58f),
                // East
                new(38f, .8f, -28f), new(49f, 1.7f, -7f), new(47f, 1.3f, 18f), new(59f, 2.8f, 39f),
                // West
                new(-39f, .9f, -29f), new(-51f, 1.5f, -6f), new(-48f, 1.2f, 20f), new(-60f, 2.7f, 40f)
            };
            for (int i = 0; i < positions.Length; i++)
            {
                Vector3 radial = positions[i];
                radial.y = 0f;
                float faceInwardYaw = Mathf.Atan2(radial.x, radial.z) * Mathf.Rad2Deg;
                float scale = .54f + (i % 4) * .055f;
                float yaw = faceInwardYaw + ((i % 3) - 1) * 7f;
                // Scenic houses are occasionally raised to follow the terraced
                // skyline. A continuous masonry plinth now connects every base to
                // ground level, eliminating the obvious floating silhouette.
                if (positions[i].y > .04f)
                    stone.AddBox(new Vector3(positions[i].x, positions[i].y * .5f, positions[i].z),
                        new Vector3(6.92f * scale, positions[i].y + .08f, 5.12f * scale), Quaternion.Euler(0f, yaw, 0f));
                AddHouse(plaster, stone, timber, roof, glass, decals, positions[i], yaw, scale, i + 2);
            }
        }

        private static void AddTerraceWalls(MeshBuilder stone, Vector3 origin)
        {
            for (int i = 0; i < 5; i++)
            {
                float x = -14f + i * 7f;
                float z = Mathf.Abs(i - 2) * 1.2f;
                stone.AddBox(origin + new Vector3(x, .45f + z * .08f, z), new Vector3(6.5f, .9f, .65f), Quaternion.Euler(0f, (i - 2) * 4f, 0f));
            }
        }

        private static void AddPlayableRockClusters(MeshBuilder stone)
        {
            Vector3[] anchors =
            {
                new(-17.8f, -.03f, -7.0f), new(-11.6f, -.04f, -12.2f),
                new(-3.8f, -.05f, -8.7f), new(7.4f, -.04f, -4.8f),
                new(14.8f, -.03f, -1.1f), new(18.5f, -.04f, 4.2f),
                new(-18.7f, -.05f, 4.6f), new(4.6f, -.04f, 17.8f)
            };
            System.Random random = new(11831);
            for (int cluster = 0; cluster < anchors.Length; cluster++)
            {
                int count = 3 + cluster % 3;
                for (int i = 0; i < count; i++)
                {
                    float angle = (float)random.NextDouble() * Mathf.PI * 2f;
                    float distance = Mathf.Lerp(.12f, .82f, (float)random.NextDouble());
                    Vector3 center = anchors[cluster] + new Vector3(Mathf.Cos(angle) * distance,
                        Mathf.Lerp(.10f, .24f, (float)random.NextDouble()), Mathf.Sin(angle) * distance);
                    Vector3 size = new(Mathf.Lerp(.34f, .92f, (float)random.NextDouble()),
                        Mathf.Lerp(.22f, .52f, (float)random.NextDouble()),
                        Mathf.Lerp(.30f, .78f, (float)random.NextDouble()));
                    stone.AddRock(center, size, 11831 + cluster * 101 + i * 17);
                }
            }
        }

        private static void AddPlayableNaturalVegetation(MeshBuilder timber, MeshBuilder foliage, MeshBuilder grassBlades)
        {
            // Replace the authored primitive crowns at their original gameplay
            // positions so existing navigation and trunk collision stay valid.
            for (int i = 0; i < 18; i++)
            {
                float angle = i * 137.5f * Mathf.Deg2Rad;
                float radius = 20.5f + (i % 4) * 1.15f;
                Vector3 position = new(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
                if (i % 3 == 0)
                {
                    float height = 5.2f + (i % 4) * .42f;
                    AddNaturalTree(timber, foliage, position, height, height * .31f, 7301 + i * 43);
                }
                else
                {
                    AddBambooStand(timber, foliage, position, 3 + (i & 1), 8101 + i * 29);
                }
            }

            Vector3[] creekBamboo =
            {
                new(-19.0f, 0f, -3.5f), new(-18.0f, 0f, -11.8f), new(-6.2f, 0f, -14.8f),
                new(-2.8f, 0f, -10.8f), new(-18.5f, 0f, -16.2f), new(-10.0f, 0f, 4.2f)
            };
            for (int i = 0; i < creekBamboo.Length; i++)
                AddBambooStand(timber, foliage, creekBamboo[i], 4, 9001 + i * 67);

            System.Random random = new(5279);
            int accepted = 0;
            for (int attempt = 0; attempt < 2800 && accepted < 820; attempt++)
            {
                float x = Mathf.Lerp(-22.5f, 22.5f, (float)random.NextDouble());
                float z = Mathf.Lerp(-22.5f, 22.5f, (float)random.NextDouble());
                Vector3 position = new(x, .055f, z);
                if (!GrassPositionIsClear(position)) continue;

                float angle = (float)random.NextDouble() * 180f;
                float height = Mathf.Lerp(.16f, .34f, (float)random.NextDouble());
                float width = Mathf.Lerp(.075f, .135f, (float)random.NextDouble());
                AddGrassBlade(grassBlades, position, angle, width, height);
                if ((accepted & 1) == 0)
                    AddGrassBlade(grassBlades, position + new Vector3(.10f, 0f, -.06f), angle + 67f,
                        width * .78f, height * .84f);
                if (accepted % 5 == 0)
                    AddGrassBlade(grassBlades, position + new Vector3(-.08f, .002f, .09f), angle - 51f,
                        width * .64f, height * .72f);
                accepted++;
            }
        }

        private static bool GrassPositionIsClear(Vector3 position)
        {
            if (Mathf.Abs(position.x) < 9.7f && Mathf.Abs(position.z) < 8.7f) return false;
            if (Mathf.Abs(position.z - CreekCenterZ(position.x)) < 2.75f) return false;
            Vector2 point = new(position.x, position.z);
            Vector2[] houses = { new(13.7f, 9.4f), new(-14.2f, 13.8f), new(18.2f, 18.8f) };
            foreach (Vector2 house in houses)
                if (Vector2.SqrMagnitude(point - house) < 24f) return false;
            return true;
        }

        private static void AddGrassBlade(MeshBuilder target, Vector3 root, float yaw, float halfWidth, float height)
        {
            Quaternion rotation = Quaternion.Euler(0f, yaw, 0f);
            Vector3 right = rotation * Vector3.right * halfWidth;
            Vector3 lean = rotation * Vector3.forward * (height * .18f);
            Vector3 mid = root + Vector3.up * (height * .58f) + lean * .28f;
            Vector3 tip = root + Vector3.up * height + lean;
            Vector3 a = root - right;
            Vector3 b = mid - right * .64f;
            Vector3 c = mid + right * .64f;
            Vector3 d = root + right;
            Vector3 e = tip - right * .08f;
            Vector3 f = tip + right * .08f;
            target.AddQuad(a, b, c, d, new Rect(0f, 0f, 1f, .58f));
            target.AddQuad(d, c, b, a, new Rect(0f, 0f, 1f, .58f));
            target.AddQuad(b, e, f, c, new Rect(0f, .58f, 1f, .42f));
            target.AddQuad(c, f, e, b, new Rect(0f, .58f, 1f, .42f));
        }

        private static void AddBambooStand(MeshBuilder timber, MeshBuilder foliage, Vector3 origin, int count, int seed)
        {
            System.Random random = new(seed);
            for (int stem = 0; stem < count; stem++)
            {
                float angle = stem / (float)Mathf.Max(1, count) * Mathf.PI * 2f + seed * .013f;
                Vector3 offset = new(Mathf.Cos(angle) * (.24f + stem * .055f), 0f,
                    Mathf.Sin(angle) * (.24f + stem * .055f));
                float height = Mathf.Lerp(4.1f, 5.4f, (float)random.NextDouble());
                float lean = Mathf.Lerp(-2.4f, 2.4f, (float)random.NextDouble());
                Quaternion stemRotation = Quaternion.Euler(lean, seed % 97, lean * .55f);
                timber.AddCylinder(origin + offset + Vector3.up * height * .5f, .052f,
                    height, stemRotation, 8);

                Vector3 crown = origin + offset + Vector3.up * (height * .76f);
                const int leafLayers = 5;
                for (int leaf = 0; leaf < leafLayers; leaf++)
                {
                    float leafAngle = leaf / (float)leafLayers * Mathf.PI * 2f + stem * .71f + seed * .021f;
                    float level = leaf * .22f;
                    Vector3 leafRoot = crown + Vector3.up * level;
                    Vector3 outward = new(Mathf.Cos(leafAngle) * .28f, Mathf.Lerp(.64f, .92f, (float)random.NextDouble()),
                        Mathf.Sin(leafAngle) * .28f);
                    Rect bambooUv = leaf % 3 == 0 ? new Rect(.58f, .66f, .42f, .34f) :
                        leaf % 3 == 1 ? new Rect(0f, .33f, .34f, .33f) : new Rect(.34f, 0f, .33f, .33f);
                    AddLeafCard(foliage, leafRoot, outward.normalized,
                        Mathf.Lerp(.58f, .84f, (float)random.NextDouble()),
                        Mathf.Lerp(.22f, .32f, (float)random.NextDouble()), bambooUv);
                }
            }
        }

        private static void AddLeafCard(MeshBuilder target, Vector3 root, Vector3 direction, float length,
            float halfWidth, Rect uv)
        {
            Vector3 side = Vector3.Cross(direction, Vector3.up);
            if (side.sqrMagnitude < .01f) side = Vector3.right;
            side = side.normalized * halfWidth;
            Vector3 tip = root + direction * length;
            Vector3 a = root - side;
            Vector3 b = root + side;
            Vector3 c = tip + side;
            Vector3 d = tip - side;
            target.AddQuad(a, b, c, d, uv);
            target.AddQuad(d, c, b, a, uv);
        }

        private static void AddOuterLandscape(MeshBuilder outerGrass, MeshBuilder mountains, MeshBuilder stone,
            MeshBuilder treeBark, MeshBuilder foliage, MeshBuilder water, MeshBuilder grassPatches)
        {
            // Four non-overlapping ground strips form a complete ring outside the
            // playable 48 x 48 metre square. Their top face uses the exact same
            // HumidGrassGround texture as the playable grass.
            outerGrass.AddBox(new Vector3(0f, -1.35f, 58f), new Vector3(140f, 2.7f, 68f), Quaternion.identity);
            outerGrass.AddBox(new Vector3(0f, -1.35f, -58f), new Vector3(140f, 2.7f, 68f), Quaternion.identity);
            outerGrass.AddBox(new Vector3(58f, -1.35f, 0f), new Vector3(68f, 2.7f, 48f), Quaternion.identity);
            outerGrass.AddBox(new Vector3(-58f, -1.35f, 0f), new Vector3(68f, 2.7f, 48f), Quaternion.identity);

            for (int direction = 0; direction < 4; direction++)
            {
                Quaternion turn = Quaternion.Euler(0f, direction * 90f, 0f);
                for (int side = -1; side <= 1; side += 2)
                {
                    for (int tier = 0; tier < 5; tier++)
                    {
                        float x = side * (29f + tier * 4.4f);
                        float z = 31f + tier * 9.2f;
                        float y = tier * .92f;
                        Quaternion terraceRotation = turn * Quaternion.Euler(0f, side * (4f + tier), 0f);
                        outerGrass.AddBox(turn * new Vector3(x, y - .45f, z), new Vector3(17f, 1.5f, 11f), terraceRotation);
                        stone.AddBox(turn * new Vector3(x - side * 8.0f, y + .18f, z - 4.9f),
                            new Vector3(.65f, 1.15f, 10.2f), terraceRotation);
                    }
                }
            }

            for (int i = 0; i < 16; i++)
            {
                float angle = i / 16f * Mathf.PI * 2f;
                float radius = 74f + (i % 4) * 7.5f;
                Vector3 center = new(Mathf.Sin(angle) * radius, 5f + (i % 5) * .82f, Mathf.Cos(angle) * radius);
                mountains.AddMountain(center, 19f + (i % 4) * 2.7f, 17f + ((i + 1) % 4) * 2.2f,
                    18f + (i % 5) * 1.85f, 41 + i * 31);
            }

            water.AddQuad(new Vector3(-4.8f, .02f, 38f), new Vector3(-4.8f, .02f, 101f),
                new Vector3(4.8f, .02f, 101f), new Vector3(4.8f, .02f, 38f), new Rect(0f, 0f, 5f, 24f));

            System.Random random = new(1949);
            for (int i = 0; i < 40; i++)
            {
                float angle = (float)random.NextDouble() * Mathf.PI * 2f;
                float radius = Mathf.Lerp(32f, 72f, (float)random.NextDouble());
                // Outer ground strips have a top surface at y=0. Previous sine-based
                // Y offsets lifted entire trees into the air independently of terrain.
                Vector3 position = new(Mathf.Sin(angle) * radius, 0f, Mathf.Cos(angle) * radius);
                for (int attempt = 0; attempt < 7 && !ScenicTreePositionIsClear(position); attempt++)
                {
                    angle += .23f + attempt * .041f;
                    radius += attempt % 2 == 0 ? 1.35f : -0.72f;
                    position = new Vector3(Mathf.Sin(angle) * radius, 0f, Mathf.Cos(angle) * radius);
                }
                float height = Mathf.Lerp(3.6f, 7.4f, (float)random.NextDouble());
                float crown = height * Mathf.Lerp(.24f, .34f, (float)random.NextDouble());
                AddNaturalTree(treeBark, foliage, position, height, crown, 1949 + i * 37);
            }

            for (int direction = 0; direction < 4; direction++)
            {
                Quaternion turn = Quaternion.Euler(0f, direction * 90f, 0f);
                for (int i = 0; i < 8; i++)
                {
                    Vector3 center = turn * new Vector3(-20f + i * 5.7f, .035f, 27f + Mathf.Sin(i * .88f) * 3.2f);
                    Vector3 right = turn * Vector3.right * .85f;
                    Vector3 forward = turn * Vector3.forward * .62f;
                    grassPatches.AddQuad(center - right - forward, center - right + forward,
                        center + right + forward, center + right - forward, new Rect(0f, 0f, 1f, 1f));
                }
            }
        }

        private static bool ScenicTreePositionIsClear(Vector3 position)
        {
            // Keep foliage clear of outer houses and the northbound creek ribbon.
            if (Mathf.Abs(position.x) < 7f && position.z > 34f) return false;
            Vector2 point = new(position.x, position.z);
            Vector2[] houses =
            {
                new(-34f, 36f), new(-13f, 48f), new(16f, 45f), new(38f, 57f),
                new(-37f, -39f), new(-14f, -51f), new(15f, -47f), new(39f, -58f),
                new(38f, -28f), new(49f, -7f), new(47f, 18f), new(59f, 39f),
                new(-39f, -29f), new(-51f, -6f), new(-48f, 20f), new(-60f, 40f)
            };
            foreach (Vector2 house in houses)
                if (Vector2.SqrMagnitude(point - house) < 43f) return false;
            return true;
        }

        private static void AddNaturalTree(MeshBuilder timber, MeshBuilder foliage, Vector3 basePosition,
            float height, float crown, int seed)
        {
            System.Random random = new(seed);
            float trunkHeight = height * .67f;
            Quaternion lowerLean = Quaternion.Euler(0f, seed % 31, Mathf.Sin(seed * .13f) * 1.2f);
            timber.AddCylinder(basePosition + Vector3.up * trunkHeight * .18f, crown * .17f,
                trunkHeight * .36f, lowerLean, 12);
            timber.AddCylinder(basePosition + Vector3.up * trunkHeight * .50f, crown * .135f,
                trunkHeight * .38f, Quaternion.Euler(0f, seed % 47, Mathf.Cos(seed * .09f) * 1.8f), 12);
            timber.AddCylinder(basePosition + Vector3.up * trunkHeight * .80f, crown * .095f,
                trunkHeight * .34f, Quaternion.Euler(0f, seed % 61, Mathf.Sin(seed * .07f) * 2.4f), 10);

            // Buttress roots visually plant the trunk into the soil instead of ending
            // in a perfectly flat cylinder cut.
            for (int root = 0; root < 4; root++)
            {
                float angle = root * Mathf.PI * .5f + seed * .031f;
                Vector3 from = basePosition + Vector3.up * .09f;
                Vector3 to = basePosition + new Vector3(Mathf.Cos(angle) * crown * .42f, .025f,
                    Mathf.Sin(angle) * crown * .42f);
                Vector3 direction = to - from;
                timber.AddCylinder((from + to) * .5f, crown * .055f, direction.magnitude,
                    Quaternion.FromToRotation(Vector3.up, direction.normalized), 7);
            }

            const int branchCount = 12;
            for (int branch = 0; branch < branchCount; branch++)
            {
                float angle = branch / (float)branchCount * Mathf.PI * 2f + seed * .071f;
                float radial = crown * Mathf.Lerp(.42f, .72f, (float)random.NextDouble());
                float branchY = height * Mathf.Lerp(.61f, .82f, (float)random.NextDouble());
                Vector3 start = basePosition + Vector3.up * height * Mathf.Lerp(.38f, .53f, (float)random.NextDouble());
                Vector3 end = basePosition + new Vector3(Mathf.Cos(angle) * radial, branchY,
                    Mathf.Sin(angle) * radial * .86f);
                Vector3 direction = end - start;
                timber.AddCylinder((start + end) * .5f, crown * Mathf.Lerp(.045f, .068f, (float)random.NextDouble()),
                    direction.magnitude, Quaternion.FromToRotation(Vector3.up, direction.normalized), 9);

                AddOliveLeafCluster(foliage, end + Vector3.up * crown * .06f,
                    crown * Mathf.Lerp(.72f, .94f, (float)random.NextDouble()), seed + branch * 97);
            }

            AddOliveLeafCluster(foliage, basePosition + Vector3.up * height * .87f, crown * 1.02f, seed + 701);
        }

        private static void AddOliveLeafCluster(MeshBuilder foliage, Vector3 center, float radius, int seed)
        {
            System.Random random = new(seed);
            const int leaves = 14;
            for (int i = 0; i < leaves; i++)
            {
                float angle = i / (float)leaves * Mathf.PI * 2f + seed * .017f;
                float elevation = Mathf.Lerp(-.28f, .42f, (float)random.NextDouble());
                Vector3 radial = new(Mathf.Cos(angle), elevation, Mathf.Sin(angle));
                Vector3 root = center + radial.normalized * radius * Mathf.Lerp(.05f, .22f, (float)random.NextDouble());
                Vector3 direction = new Vector3(Mathf.Cos(angle + .27f) * .26f,
                    Mathf.Lerp(.64f, .92f, (float)random.NextDouble()),
                    Mathf.Sin(angle + .27f) * .26f).normalized;
                Rect oliveUv = (i % 5) switch
                {
                    0 => new Rect(0f, .66f, .40f, .34f),
                    1 => new Rect(.34f, .33f, .33f, .33f),
                    2 => new Rect(.66f, .33f, .34f, .33f),
                    3 => new Rect(0f, 0f, .34f, .33f),
                    _ => new Rect(.66f, 0f, .34f, .33f)
                };
                AddLeafCard(foliage, root, direction,
                    radius * Mathf.Lerp(.90f, 1.16f, (float)random.NextDouble()),
                    radius * Mathf.Lerp(.46f, .60f, (float)random.NextDouble()), oliveUv);
            }
        }

        private static void AddWallDecal(MeshBuilder target, Vector3 origin, Quaternion rotation, Vector3 localCenter, Vector2 size, int cell)
        {
            Vector3 center = origin + rotation * localCenter;
            Vector3 right = rotation * Vector3.right * (size.x * .5f);
            Vector3 up = Vector3.up * (size.y * .5f);
            target.AddQuad(center - right - up, center - right + up, center + right + up, center + right - up, DecalUv(cell));
        }

        private static void AddGroundDecal(MeshBuilder target, Vector3 center, Quaternion rotation, Vector2 size, int cell)
        {
            Vector3 right = rotation * Vector3.right * (size.x * .5f);
            Vector3 forward = rotation * Vector3.forward * (size.y * .5f);
            target.AddQuad(center - right - forward, center - right + forward, center + right + forward, center + right - forward, DecalUv(cell));
        }

        private static Rect DecalUv(int cell)
        {
            int normalized = ((cell % 16) + 16) % 16;
            int column = normalized % 4;
            int rowFromTop = normalized / 4;
            return new Rect(column * .25f, .75f - rowFromTop * .25f, .25f, .25f);
        }

        private static Material CreateAtlasMaterial(string name, Texture texture, Vector2 offset, Color tint, float smoothness)
        {
            Material material = CreateTexturedMaterial(name, texture, tint, smoothness, new Vector2(.5f, .5f));
            material.mainTextureOffset = offset;
            if (material.HasProperty("_BaseMap")) material.SetTextureOffset("_BaseMap", offset);
            return material;
        }

        private static Material CreateTexturedMaterial(string name, Texture texture, Color color, float smoothness, Vector2 scale)
        {
            Material material = CreateMaterial(name, color, smoothness);
            if (texture != null)
            {
                texture.filterMode = FilterMode.Trilinear;
                texture.anisoLevel = 4;
                texture.mipMapBias = texture.name.Contains("Foliage", StringComparison.OrdinalIgnoreCase) ? .18f : .32f;
                material.mainTexture = texture;
                material.mainTextureScale = scale;
                if (material.HasProperty("_BaseMap"))
                {
                    material.SetTexture("_BaseMap", texture);
                    material.SetTextureScale("_BaseMap", scale);
                }
            }
            return material;
        }

        private static Material CreateMaterial(string name, Color color, float smoothness)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            Material material = new(shader) { name = name, enableInstancing = true };
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Color")) material.SetColor("_Color", color);
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", smoothness);
            if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", 0f);
            return material;
        }

        private static Material CreateDoubleSidedFoliageMaterial(string name, Texture texture, Color color, float smoothness)
        {
            if (texture != null)
            {
                Shader foliageShader = Shader.Find("MeixiEcho/Realistic Foliage");
                if (foliageShader != null)
                {
                    Material foliageMaterial = new(foliageShader) { name = name, enableInstancing = true };
                    foliageMaterial.SetTexture("_BaseMap", texture);
                    foliageMaterial.SetColor("_BaseColor", color);
                    foliageMaterial.SetFloat("_Cutoff", .38f);
                    foliageMaterial.renderQueue = (int)RenderQueue.AlphaTest;
                    foliageMaterial.doubleSidedGI = true;
                    return foliageMaterial;
                }
            }

            Material material = CreateTexturedMaterial(name, texture, color, smoothness, Vector2.one);
            if (material.HasProperty("_Cull")) material.SetFloat("_Cull", 0f);
            if (texture != null)
            {
                if (material.HasProperty("_AlphaClip")) material.SetFloat("_AlphaClip", 0f);
                if (material.HasProperty("_Surface")) material.SetFloat("_Surface", 1f);
                if (material.HasProperty("_Blend")) material.SetFloat("_Blend", 0f);
                if (material.HasProperty("_SrcBlend")) material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
                if (material.HasProperty("_DstBlend")) material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
                if (material.HasProperty("_ZWrite")) material.SetFloat("_ZWrite", 0f);
                if (material.HasProperty("_EmissionColor"))
                {
                    material.SetColor("_EmissionColor", new Color(.18f, .21f, .16f, 1f));
                    if (material.HasProperty("_EmissionMap")) material.SetTexture("_EmissionMap", texture);
                    material.EnableKeyword("_EMISSION");
                }
                material.DisableKeyword("_ALPHATEST_ON");
                material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                material.SetOverrideTag("RenderType", "Transparent");
                material.renderQueue = (int)RenderQueue.Transparent;
            }
            material.doubleSidedGI = true;
            return material;
        }

        private static Material CreateTransparentMaterial(string name, Texture texture, Color color, float smoothness)
        {
            Material material = CreateTexturedMaterial(name, texture, color, smoothness, Vector2.one);
            if (material.HasProperty("_Surface")) material.SetFloat("_Surface", 1f);
            if (material.HasProperty("_Blend")) material.SetFloat("_Blend", 0f);
            if (material.HasProperty("_SrcBlend")) material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            if (material.HasProperty("_DstBlend")) material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            if (material.HasProperty("_ZWrite")) material.SetFloat("_ZWrite", 0f);
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.EnableKeyword("_ALPHAPREMULTIPLY_ON");
            material.renderQueue = (int)RenderQueue.Transparent;
            return material;
        }

        private static Material CreateCreekWaterMaterial()
        {
            Shader creekShader = Shader.Find("MeixiEcho/Creek Water");
            if (creekShader != null)
            {
                Material creekMaterial = new(creekShader) { name = "梅溪连续浅水", enableInstancing = true };
                creekMaterial.SetColor("_ShallowColor", new Color(.16f, .46f, .45f, .44f));
                creekMaterial.SetColor("_DeepColor", new Color(.025f, .145f, .18f, .56f));
                creekMaterial.SetColor("_FoamColor", new Color(.76f, .90f, .84f, .42f));
                creekMaterial.SetFloat("_FlowSpeed", .44f);
                creekMaterial.SetFloat("_WaveStrength", .024f);
                creekMaterial.renderQueue = (int)RenderQueue.Transparent - 4;
                return creekMaterial;
            }

            Material material = CreateTransparentMaterial("梅溪连续浅水_兼容材质", null,
                new Color(.10f, .46f, .52f, .78f), .94f);
            if (material.HasProperty("_SpecularHighlights")) material.SetFloat("_SpecularHighlights", 1f);
            if (material.HasProperty("_EnvironmentReflections")) material.SetFloat("_EnvironmentReflections", 1f);
            if (material.HasProperty("_EmissionColor"))
            {
                material.SetColor("_EmissionColor", new Color(.008f, .035f, .040f));
                material.EnableKeyword("_EMISSION");
            }
            material.enableInstancing = true;
            return material;
        }

        private static void CreateMeshObject(Transform parent, string name, MeshBuilder builder, Material material,
            bool castShadows, bool receiveShadows)
        {
            if (builder.VertexCount == 0) return;
            GameObject gameObject = new(name) { isStatic = true };
            gameObject.transform.SetParent(parent, false);
            gameObject.AddComponent<MeshFilter>().sharedMesh = builder.Build(name);
            MeshRenderer renderer = gameObject.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = castShadows ? ShadowCastingMode.On : ShadowCastingMode.Off;
            renderer.receiveShadows = receiveShadows;
        }

        private static Bounds CalculateBounds(GameObject root)
        {
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0) return new Bounds(root.transform.position, Vector3.one);
            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
            return bounds;
        }

        private sealed class MeshBuilder
        {
            private readonly List<Vector3> vertices = new();
            private readonly List<Vector3> normals = new();
            private readonly List<Vector2> uv = new();
            private readonly List<int> triangles = new();
            public int VertexCount => vertices.Count;
            public int QuadCount { get; private set; }

            public void AddBox(Vector3 center, Vector3 size, Quaternion rotation)
            {
                Vector3 half = size * .5f;
                Vector3[] corners =
                {
                    new(-half.x, -half.y, -half.z), new(half.x, -half.y, -half.z),
                    new(half.x, half.y, -half.z), new(-half.x, half.y, -half.z),
                    new(-half.x, -half.y, half.z), new(half.x, -half.y, half.z),
                    new(half.x, half.y, half.z), new(-half.x, half.y, half.z)
                };
                for (int i = 0; i < corners.Length; i++) corners[i] = center + rotation * corners[i];
                Rect fullUv = new(0f, 0f, 1f, 1f);
                AddQuad(corners[0], corners[3], corners[2], corners[1], fullUv);
                AddQuad(corners[5], corners[6], corners[7], corners[4], fullUv);
                AddQuad(corners[4], corners[7], corners[3], corners[0], fullUv);
                AddQuad(corners[1], corners[2], corners[6], corners[5], fullUv);
                AddQuad(corners[3], corners[7], corners[6], corners[2], fullUv);
                AddQuad(corners[4], corners[0], corners[1], corners[5], fullUv);
            }

            public void AddTriangularPrism(Vector3 a, Vector3 b, Vector3 c, Vector3 extrusion)
            {
                Vector3 a2 = a + extrusion;
                Vector3 b2 = b + extrusion;
                Vector3 c2 = c + extrusion;
                AddTriangle(a, b, c);
                AddTriangle(c2, b2, a2);
                Rect fullUv = new(0f, 0f, 1f, 1f);
                AddQuad(a, a2, b2, b, fullUv);
                AddQuad(b, b2, c2, c, fullUv);
                AddQuad(c, c2, a2, a, fullUv);
            }

            public void AddPyramid(Vector3 center, Vector2 baseSize, float height)
            {
                Vector3 a = center + new Vector3(-baseSize.x * .5f, -height * .5f, -baseSize.y * .5f);
                Vector3 b = center + new Vector3(baseSize.x * .5f, -height * .5f, -baseSize.y * .5f);
                Vector3 c = center + new Vector3(baseSize.x * .5f, -height * .5f, baseSize.y * .5f);
                Vector3 d = center + new Vector3(-baseSize.x * .5f, -height * .5f, baseSize.y * .5f);
                Vector3 top = center + Vector3.up * height * .5f;
                AddTriangle(a, top, b); AddTriangle(b, top, c); AddTriangle(c, top, d); AddTriangle(d, top, a);
                AddQuad(d, c, b, a, new Rect(0f, 0f, 1f, 1f));
            }

            public void AddMountain(Vector3 center, float radiusX, float radiusZ, float height, int seed)
            {
                const int segments = 32;
                const int rings = 7;
                System.Random random = new(seed);
                Vector3[,] points = new Vector3[rings, segments];
                Vector3[,] smoothNormals = new Vector3[rings, segments];
                for (int ring = 0; ring < rings; ring++)
                {
                    float elevation = ring / (float)(rings - 1);
                    float ringHeight = Mathf.Lerp(-.46f, .38f, Mathf.SmoothStep(0f, 1f, elevation));
                    float ringRadius = Mathf.Lerp(1f, .17f, Mathf.Pow(elevation, .86f));
                    for (int i = 0; i < segments; i++)
                    {
                        float angle = i / (float)segments * Mathf.PI * 2f;
                        float broadNoise = Mathf.Sin(angle * 3f + seed * .19f) * .085f;
                        float fineNoise = Mathf.Sin(angle * 7f + ring * .71f + seed * .037f) * .035f;
                        float randomNoise = Mathf.Lerp(-.035f, .035f, (float)random.NextDouble());
                        float noise = 1f + broadNoise + fineNoise + randomNoise;
                        float driftX = Mathf.Sin(ring * 1.31f + seed * .07f) * radiusX * .055f;
                        float driftZ = Mathf.Cos(ring * 1.17f + seed * .05f) * radiusZ * .055f;
                        points[ring, i] = center + new Vector3(
                            Mathf.Cos(angle) * radiusX * ringRadius * noise + driftX,
                            ringHeight * height + Mathf.Sin(angle * 4f + seed) * height * .022f,
                            Mathf.Sin(angle) * radiusZ * ringRadius * noise + driftZ);
                        smoothNormals[ring, i] = new Vector3(Mathf.Cos(angle),
                            Mathf.Lerp(.34f, .82f, elevation), Mathf.Sin(angle)).normalized;
                    }
                }
                for (int ring = 0; ring < rings - 1; ring++)
                {
                    for (int i = 0; i < segments; i++)
                    {
                        int next = (i + 1) % segments;
                        AddSmoothQuad(points[ring, i], points[ring + 1, i], points[ring + 1, next], points[ring, next],
                            smoothNormals[ring, i], smoothNormals[ring + 1, i], smoothNormals[ring + 1, next],
                            smoothNormals[ring, next],
                            new Rect(i / (float)segments, ring / (float)rings, 1f / segments, 1f / rings));
                    }
                }
                Vector3 peak = center + new Vector3(radiusX * .055f * Mathf.Sin(seed), height * .56f,
                    radiusZ * .045f * Mathf.Cos(seed));
                for (int i = 0; i < segments; i++)
                    AddSmoothTriangle(points[rings - 1, i], peak, points[rings - 1, (i + 1) % segments],
                        smoothNormals[rings - 1, i], Vector3.up, smoothNormals[rings - 1, (i + 1) % segments]);
            }

            public void AddCylinder(Vector3 center, float radius, float height, Quaternion rotation, int segments)
            {
                int count = Mathf.Max(5, segments);
                Vector3 bottomCenter = center + rotation * Vector3.down * (height * .5f);
                Vector3 topCenter = center + rotation * Vector3.up * (height * .5f);
                for (int i = 0; i < count; i++)
                {
                    float a0 = i / (float)count * Mathf.PI * 2f;
                    float a1 = (i + 1) / (float)count * Mathf.PI * 2f;
                    Vector3 r0 = rotation * new Vector3(Mathf.Cos(a0) * radius, 0f, Mathf.Sin(a0) * radius);
                    Vector3 r1 = rotation * new Vector3(Mathf.Cos(a1) * radius, 0f, Mathf.Sin(a1) * radius);
                    AddQuad(bottomCenter + r0, topCenter + r0, topCenter + r1, bottomCenter + r1, new Rect(0f, 0f, 1f, 1f));
                    AddTriangle(topCenter, topCenter + r0, topCenter + r1);
                }
            }

            public void AddRock(Vector3 center, Vector3 size, int seed)
            {
                const int longitude = 12;
                const int latitude = 6;
                System.Random random = new(seed);
                Vector3[,] points = new Vector3[latitude + 1, longitude];
                for (int y = 0; y <= latitude; y++)
                {
                    float v = y / (float)latitude;
                    float vertical = (v - .5f) * Mathf.PI;
                    for (int x = 0; x < longitude; x++)
                    {
                        float u = x / (float)longitude;
                        float horizontal = u * Mathf.PI * 2f;
                        float noise = 1f + Mathf.Sin(horizontal * 3f + seed * .17f) * .08f +
                                      Mathf.Lerp(-.08f, .08f, (float)random.NextDouble());
                        Vector3 unit = new(Mathf.Cos(vertical) * Mathf.Cos(horizontal), Mathf.Sin(vertical),
                            Mathf.Cos(vertical) * Mathf.Sin(horizontal));
                        unit.x *= noise;
                        unit.z *= 1f + Mathf.Sin(horizontal * 5f + vertical * 2f) * .06f;
                        points[y, x] = center + Vector3.Scale(unit, size * .5f);
                    }
                }
                for (int y = 0; y < latitude; y++)
                {
                    for (int x = 0; x < longitude; x++)
                    {
                        int next = (x + 1) % longitude;
                        AddQuad(points[y, x], points[y + 1, x], points[y + 1, next], points[y, next],
                            new Rect(x / (float)longitude, y / (float)latitude, 1f / longitude, 1f / latitude));
                    }
                }
            }

            public void AddSphere(Vector3 center, Vector3 size, int longitude, int latitude)
            {
                int lon = Mathf.Max(6, longitude);
                int lat = Mathf.Max(3, latitude);
                for (int y = 0; y < lat; y++)
                {
                    float v0 = y / (float)lat;
                    float v1 = (y + 1) / (float)lat;
                    float p0 = (v0 - .5f) * Mathf.PI;
                    float p1 = (v1 - .5f) * Mathf.PI;
                    for (int x = 0; x < lon; x++)
                    {
                        float u0 = x / (float)lon;
                        float u1 = (x + 1) / (float)lon;
                        Vector3 a = Ellipsoid(center, size, p0, u0 * Mathf.PI * 2f);
                        Vector3 b = Ellipsoid(center, size, p1, u0 * Mathf.PI * 2f);
                        Vector3 c = Ellipsoid(center, size, p1, u1 * Mathf.PI * 2f);
                        Vector3 d = Ellipsoid(center, size, p0, u1 * Mathf.PI * 2f);
                        AddQuad(a, b, c, d, new Rect(u0, v0, u1 - u0, v1 - v0));
                    }
                }
            }

            private static Vector3 Ellipsoid(Vector3 center, Vector3 size, float latitude, float longitude)
            {
                float cos = Mathf.Cos(latitude);
                return center + new Vector3(Mathf.Cos(longitude) * cos * size.x * .5f,
                    Mathf.Sin(latitude) * size.y * .5f, Mathf.Sin(longitude) * cos * size.z * .5f);
            }

            public void AddQuad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Rect uvRect)
            {
                int start = vertices.Count;
                Vector3 normal = Vector3.Cross(b - a, c - a).normalized;
                vertices.Add(a); vertices.Add(b); vertices.Add(c); vertices.Add(d);
                normals.Add(normal); normals.Add(normal); normals.Add(normal); normals.Add(normal);
                uv.Add(new Vector2(uvRect.xMin, uvRect.yMin));
                uv.Add(new Vector2(uvRect.xMin, uvRect.yMax));
                uv.Add(new Vector2(uvRect.xMax, uvRect.yMax));
                uv.Add(new Vector2(uvRect.xMax, uvRect.yMin));
                triangles.Add(start); triangles.Add(start + 1); triangles.Add(start + 2);
                triangles.Add(start); triangles.Add(start + 2); triangles.Add(start + 3);
                QuadCount++;
            }

            private void AddSmoothQuad(Vector3 a, Vector3 b, Vector3 c, Vector3 d,
                Vector3 normalA, Vector3 normalB, Vector3 normalC, Vector3 normalD, Rect uvRect)
            {
                int start = vertices.Count;
                vertices.Add(a); vertices.Add(b); vertices.Add(c); vertices.Add(d);
                normals.Add(normalA); normals.Add(normalB); normals.Add(normalC); normals.Add(normalD);
                uv.Add(new Vector2(uvRect.xMin, uvRect.yMin));
                uv.Add(new Vector2(uvRect.xMin, uvRect.yMax));
                uv.Add(new Vector2(uvRect.xMax, uvRect.yMax));
                uv.Add(new Vector2(uvRect.xMax, uvRect.yMin));
                triangles.Add(start); triangles.Add(start + 1); triangles.Add(start + 2);
                triangles.Add(start); triangles.Add(start + 2); triangles.Add(start + 3);
                QuadCount++;
            }

            private void AddTriangle(Vector3 a, Vector3 b, Vector3 c)
            {
                int start = vertices.Count;
                Vector3 normal = Vector3.Cross(b - a, c - a).normalized;
                vertices.Add(a); vertices.Add(b); vertices.Add(c);
                normals.Add(normal); normals.Add(normal); normals.Add(normal);
                uv.Add(Vector2.zero); uv.Add(Vector2.up); uv.Add(Vector2.right);
                triangles.Add(start); triangles.Add(start + 1); triangles.Add(start + 2);
            }

            private void AddSmoothTriangle(Vector3 a, Vector3 b, Vector3 c,
                Vector3 normalA, Vector3 normalB, Vector3 normalC)
            {
                int start = vertices.Count;
                vertices.Add(a); vertices.Add(b); vertices.Add(c);
                normals.Add(normalA); normals.Add(normalB); normals.Add(normalC);
                uv.Add(Vector2.zero); uv.Add(Vector2.up); uv.Add(Vector2.right);
                triangles.Add(start); triangles.Add(start + 1); triangles.Add(start + 2);
            }

            public Mesh Build(string name)
            {
                Mesh mesh = new() { name = name, indexFormat = vertices.Count > 65000 ? IndexFormat.UInt32 : IndexFormat.UInt16 };
                mesh.SetVertices(vertices);
                mesh.SetNormals(normals);
                mesh.SetUVs(0, uv);
                mesh.SetTriangles(triangles, 0, true);
                mesh.RecalculateBounds();
                mesh.UploadMeshData(false);
                return mesh;
            }
        }
    }

    /// <summary>
    /// One-shot, allocation-free runtime sample used to catch visual upgrades that
    /// accidentally destabilise frame pacing. It disables itself after the sample.
    /// </summary>
    public sealed class MeixiRuntimePerformanceProbe : MonoBehaviour
    {
        private const int SampleFrames = 90;
        private float warmupUntil;
        private float accumulated;
        private float worstFrame;
        private int frames;
        private int skippedStalls;

        private void OnEnable()
        {
            warmupUntil = Time.unscaledTime + .55f;
            accumulated = 0f;
            worstFrame = 0f;
            frames = 0;
            skippedStalls = 0;
        }

        private void Update()
        {
            if (Time.unscaledTime < warmupUntil) return;
            float delta = Mathf.Max(.0001f, Time.unscaledDeltaTime);
            // Asset startup and the automated screenshot's synchronous Camera.Render
            // are not gameplay frame pacing. Record but exclude those one-off stalls.
            if (delta > .10f)
            {
                skippedStalls++;
                return;
            }
            accumulated += delta;
            worstFrame = Mathf.Max(worstFrame, delta);
            frames++;
            if (frames < SampleFrames) return;

            float averageMs = accumulated / frames * 1000f;
            Debug.Log($"[MeixiPerformance] visual sample frames={frames} avg={averageMs:F2}ms " +
                      $"fps={1000f / averageMs:F1} worst={worstFrame * 1000f:F2}ms " +
                      $"renderers={FindObjectsByType<Renderer>().Length} " +
                      $"colliders={FindObjectsByType<Collider>().Length} skippedStalls={skippedStalls}.");
            enabled = false;
        }
    }

    /// <summary>
    /// Editor-facing chapter route visualisation. The marker hierarchy is tagged
    /// EditorOnly by the baker, so it adds no renderer, collider or player-build cost.
    /// </summary>
    [ExecuteAlways]
    public sealed class MeixiSceneTaskGizmos : MonoBehaviour
    {
        [SerializeField] private Color routeColor = new(.18f, .92f, 1f, .92f);
        [SerializeField] private Color taskColor = new(1f, .65f, .16f, .96f);
        [SerializeField] private float markerRadius = .42f;

        private void OnDrawGizmos()
        {
            int count = transform.childCount;
            if (count == 0) return;

            Vector3 previous = Vector3.zero;
            for (int i = 0; i < count; i++)
            {
                Transform marker = transform.GetChild(i);
                Vector3 position = marker.position + Vector3.up * .32f;
                Gizmos.color = taskColor;
                Gizmos.DrawSphere(position, markerRadius);
                Gizmos.color = new Color(routeColor.r, routeColor.g, routeColor.b, .18f);
                Gizmos.DrawWireSphere(position, markerRadius * 1.65f);
                if (i > 0)
                {
                    Gizmos.color = routeColor;
                    Gizmos.DrawLine(previous, position);
                }
                previous = position;

#if UNITY_EDITOR
                UnityEditor.Handles.color = Color.white;
                UnityEditor.Handles.Label(position + Vector3.up * .62f, marker.name,
                    new GUIStyle(UnityEditor.EditorStyles.boldLabel)
                    {
                        normal = { textColor = new Color(1f, .92f, .66f) },
                        fontSize = 13
                    });
#endif
            }
        }
    }

    public sealed class MeixiNpcPresentation : MonoBehaviour { }
}
