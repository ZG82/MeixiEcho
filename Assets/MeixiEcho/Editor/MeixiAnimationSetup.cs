#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace MeixiEcho.Editor
{
    [InitializeOnLoad]
    public static class MeixiAnimationSetup
    {
        private const string LinMoModel = "Assets/MeixiEcho/Resources/Models/Characters/LinMo.fbx";
        private static readonly string[] RealisticCastModels =
        {
            "Assets/MeixiEcho/Resources/Models/Characters/RealisticCast/LinMo/Male_Adult_20.fbx",
            "Assets/MeixiEcho/Resources/Models/Characters/RealisticCast/VillageElder/Male_Adult_03.fbx",
            "Assets/MeixiEcho/Resources/Models/Characters/RealisticCast/VillageChild/Male_Child_02.fbx",
            "Assets/MeixiEcho/Resources/Models/Characters/RealisticCast/VillageAunt/Female_Adult_12.fbx"
        };
        private const string RealisticCastFolder = "Assets/MeixiEcho/Resources/Models/Characters/RealisticCast";
        private const string MotionModel = "Assets/MeixiEcho/Resources/Animations/KayKitHumanoidMotions.fbx";
        private const string AnimationFolder = "Assets/MeixiEcho/Resources/Animations";
        private const string StarterLocomotionFolder = AnimationFolder + "/StarterLocomotion";
        private const string StarterIdleModel = StarterLocomotionFolder + "/Stand--Idle.anim.fbx";
        private const string StarterWalkModel = StarterLocomotionFolder + "/Locomotion--Walk_N.anim.fbx";
        private const string StarterRunModel = StarterLocomotionFolder + "/Locomotion--Run_N.anim.fbx";
        private const string PlayerController = AnimationFolder + "/LinMoCombat.controller";
        private const string ControllerVersion = "MeixiEcho.LinMoCombat.v12-hybrid-compatible-locomotion";

        static MeixiAnimationSetup()
        {
            EditorApplication.delayCall += EnsureAssets;
        }

        [MenuItem("梅溪回响/重建第一章角色动画")]
        public static void RebuildAssets()
        {
            EnsureAssets(true);
        }

        public static void EnsureAssets()
        {
            EnsureAssets(false);
        }

        private static void EnsureAssets(bool forceControllerRebuild)
        {
            // Never delete/recreate animation assets while Play Mode is starting. That race left the
            // first runtime Animator holding a destroyed controller; the second Play then appeared fine.
            if (EditorApplication.isCompiling || EditorApplication.isUpdating ||
                EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (AssetDatabase.LoadAssetAtPath<GameObject>(LinMoModel) == null ||
                AssetDatabase.LoadAssetAtPath<GameObject>(MotionModel) == null ||
                AssetDatabase.LoadAssetAtPath<GameObject>(StarterIdleModel) == null ||
                AssetDatabase.LoadAssetAtPath<GameObject>(StarterWalkModel) == null ||
                AssetDatabase.LoadAssetAtPath<GameObject>(StarterRunModel) == null) return;

            EnsureFolder(AnimationFolder);
            ConfigureHumanoid(LinMoModel, false);
            foreach (string castModel in RealisticCastModels)
            {
                if (AssetDatabase.LoadAssetAtPath<GameObject>(castModel) != null)
                    ConfigureHumanoid(castModel, false);
            }
            ConfigureRealisticCastTextures();
            ConfigureHumanoid(MotionModel, true);
            ConfigureClipLoops(MotionModel);
            ConfigureStarterLocomotion(StarterIdleModel);
            ConfigureStarterLocomotion(StarterWalkModel);
            ConfigureStarterLocomotion(StarterRunModel);

            Dictionary<string, AnimationClip> playerClips = LoadClips(MotionModel);
            AnimationClip starterIdle = LoadPrimaryClip(StarterIdleModel);
            AnimationClip starterWalk = LoadPrimaryClip(StarterWalkModel);
            AnimationClip starterRun = LoadPrimaryClip(StarterRunModel);

            if (playerClips.Count == 0 || starterIdle == null || starterWalk == null || starterRun == null)
            {
                Debug.LogWarning("梅溪回响：第一章动作源中没有找到动画片段。");
                return;
            }

            if (forceControllerRebuild || !ControllerIsCurrent())
                BuildPlayerController(playerClips, starterIdle, starterWalk, starterRun);
            AssetDatabase.SaveAssets();
        }

        private static bool ControllerIsCurrent()
        {
            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(PlayerController);
            AssetImporter importer = AssetImporter.GetAtPath(PlayerController);
            if (controller == null || importer == null || importer.userData != ControllerVersion) return false;
            if (controller.layers == null || controller.layers.Length == 0 || controller.parameters == null) return false;

            string[] required = { "Speed", "GaitPhase", "Grounded", "Light", "Heavy", "Dodge", "Hit", "Victory" };
            return required.All(name => controller.parameters.Any(parameter => parameter.name == name));
        }

        private static Dictionary<string, AnimationClip> LoadClips(string path)
        {
            return AssetDatabase.LoadAllAssetsAtPath(path)
                .OfType<AnimationClip>()
                .Where(clip => !clip.name.StartsWith("__preview__", StringComparison.OrdinalIgnoreCase))
                .GroupBy(clip => clip.name)
                .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);
        }

        private static AnimationClip LoadPrimaryClip(string path)
        {
            return AssetDatabase.LoadAllAssetsAtPath(path)
                .OfType<AnimationClip>()
                .FirstOrDefault(clip => !clip.name.StartsWith("__preview__", StringComparison.OrdinalIgnoreCase));
        }

        private static void ConfigureHumanoid(string path, bool importAnimations)
        {
            if (AssetImporter.GetAtPath(path) is not ModelImporter importer) return;
            bool changed = importer.animationType != ModelImporterAnimationType.Human ||
                           importer.importAnimation != importAnimations ||
                           importer.optimizeGameObjects;
            if (!changed) return;

            importer.animationType = ModelImporterAnimationType.Human;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            importer.importAnimation = importAnimations;
            importer.optimizeGameObjects = false;
            importer.importCameras = false;
            importer.importLights = false;
            importer.SaveAndReimport();
        }

        private static void ConfigureClipLoops(string path)
        {
            if (AssetImporter.GetAtPath(path) is not ModelImporter importer) return;
            ModelImporterClipAnimation[] clips = importer.clipAnimations.Length > 0
                ? importer.clipAnimations
                : importer.defaultClipAnimations;
            bool changed = false;
            foreach (ModelImporterClipAnimation clip in clips)
            {
                bool shouldLoop = clip.name.Contains("Idle", StringComparison.OrdinalIgnoreCase) ||
                                  clip.name.Contains("Walking", StringComparison.OrdinalIgnoreCase) ||
                                  clip.name.Contains("Running", StringComparison.OrdinalIgnoreCase) ||
                                  clip.name.Contains("Spellcasting", StringComparison.OrdinalIgnoreCase);
                if (clip.loopTime == shouldLoop && (!shouldLoop || clip.loopPose)) continue;
                clip.loopTime = shouldLoop;
                clip.loopPose = shouldLoop;
                changed = true;
            }
            if (!changed) return;
            importer.clipAnimations = clips;
            importer.SaveAndReimport();
        }

        private static void ConfigureStarterLocomotion(string path)
        {
            ConfigureHumanoid(path, true);
            if (AssetImporter.GetAtPath(path) is not ModelImporter importer) return;
            ModelImporterClipAnimation[] clips = importer.clipAnimations.Length > 0
                ? importer.clipAnimations
                : importer.defaultClipAnimations;
            bool changed = false;
            foreach (ModelImporterClipAnimation clip in clips)
            {
                if (clip.loopTime && clip.loopPose && clip.keepOriginalOrientation &&
                    clip.keepOriginalPositionY && clip.keepOriginalPositionXZ) continue;
                clip.loopTime = true;
                clip.loopPose = true;
                clip.keepOriginalOrientation = true;
                clip.keepOriginalPositionY = true;
                clip.keepOriginalPositionXZ = true;
                changed = true;
            }
            if (!changed) return;
            importer.clipAnimations = clips;
            importer.SaveAndReimport();
        }

        private static void ConfigureRealisticCastTextures()
        {
            if (!AssetDatabase.IsValidFolder(RealisticCastFolder)) return;
            foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { RealisticCastFolder }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (AssetImporter.GetAtPath(path) is not TextureImporter importer) continue;
                string lower = path.ToLowerInvariant();
                bool normal = lower.Contains("_normal");
                bool dataMap = normal || lower.Contains("_specular");
                bool changed = importer.maxTextureSize != 2048 ||
                               importer.textureCompression != TextureImporterCompression.CompressedHQ ||
                               importer.textureType != (normal ? TextureImporterType.NormalMap : TextureImporterType.Default) ||
                               importer.sRGBTexture == dataMap;
                if (!changed) continue;

                importer.maxTextureSize = 2048;
                importer.textureCompression = TextureImporterCompression.CompressedHQ;
                importer.crunchedCompression = false;
                importer.textureType = normal ? TextureImporterType.NormalMap : TextureImporterType.Default;
                importer.sRGBTexture = !dataMap;
                importer.alphaIsTransparency = lower.Contains("_opacity");
                importer.SaveAndReimport();
            }
        }

        private static void BuildPlayerController(IReadOnlyDictionary<string, AnimationClip> clips,
            AnimationClip starterIdle, AnimationClip starterWalk, AnimationClip starterRun)
        {
            AnimatorController controller = RecreateController(PlayerController);
            controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
            controller.AddParameter("GaitPhase", AnimatorControllerParameterType.Float);
            controller.AddParameter("Grounded", AnimatorControllerParameterType.Bool);
            controller.AddParameter("Light", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("Heavy", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("Dodge", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("Hit", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("Victory", AnimatorControllerParameterType.Trigger);

            AnimatorStateMachine machine = controller.layers[0].stateMachine;
            AnimatorControllerLayer baseLayer = controller.layers[0];
            baseLayer.iKPass = true;
            controller.layers = new[] { baseLayer };
            var locomotionTree = new BlendTree
            {
                name = "MeixiLocomotionBlend",
                blendType = BlendTreeType.Simple1D,
                blendParameter = "Speed",
                useAutomaticThresholds = false
            };
            AssetDatabase.AddObjectToAsset(locomotionTree, controller);
            locomotionTree.AddChild(Find(clips, "Idle", "Unarmed_Idle"), 0f);
            locomotionTree.AddChild(Find(clips, "Walking_A", "Walking_B"), 0.46f);
            locomotionTree.AddChild(Find(clips, "Running_A", "Running_B"), 1f);

            AnimatorState locomotion = machine.AddState("移动混合", new Vector3(350f, 90f));
            locomotion.motion = locomotionTree;
            locomotion.writeDefaultValues = true;
            machine.defaultState = locomotion;

            AddAction(machine, locomotion, "轻击", Find(clips, "Unarmed_Melee_Attack_Punch_A", "1H_Melee_Attack_Slice_Horizontal"), "Light", new Vector3(350f, 250f), 0.78f);
            AddAction(machine, locomotion, "重击", Find(clips, "Unarmed_Melee_Attack_Kick", "1H_Melee_Attack_Chop"), "Heavy", new Vector3(540f, 250f), 0.82f);
            AddAction(machine, locomotion, "踏音闪避", Find(clips, "Dodge_Forward", "Dodge_Left"), "Dodge", new Vector3(730f, 250f), 0.88f);
            AddAction(machine, locomotion, "受击", Find(clips, "Hit_A", "Hit_B"), "Hit", new Vector3(920f, 250f), 0.78f);
            AddAction(machine, locomotion, "章末收势", Find(clips, "Interact", "Spellcast_Raise", "Idle"), "Victory", new Vector3(1110f, 250f), 0.90f);

            BuildUpperBodyLocomotionLayer(controller, starterIdle, starterWalk, starterRun);

            AssetImporter importer = AssetImporter.GetAtPath(PlayerController);
            if (importer != null) importer.userData = ControllerVersion;
            AssetDatabase.WriteImportSettingsIfDirty(PlayerController);
            EditorUtility.SetDirty(controller);
        }

        private static void BuildUpperBodyLocomotionLayer(AnimatorController controller,
            AnimationClip idle, AnimationClip walk, AnimationClip run)
        {
            var mask = new AvatarMask { name = "MeixiNaturalArmsMask" };
            for (int i = 0; i < (int)AvatarMaskBodyPart.LastBodyPart; i++)
                mask.SetHumanoidBodyPartActive((AvatarMaskBodyPart)i, false);
            mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.LeftArm, true);
            mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.RightArm, true);
            mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.LeftFingers, true);
            mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.RightFingers, true);
            AssetDatabase.AddObjectToAsset(mask, controller);

            var stateMachine = new AnimatorStateMachine { name = "MeixiNaturalArms" };
            AssetDatabase.AddObjectToAsset(stateMachine, controller);
            var layer = new AnimatorControllerLayer
            {
                name = "LocomotionUpperBody",
                defaultWeight = 1f,
                blendingMode = AnimatorLayerBlendingMode.Override,
                avatarMask = mask,
                iKPass = false,
                stateMachine = stateMachine
            };
            controller.AddLayer(layer);

            var tree = new BlendTree
            {
                name = "MeixiNaturalArmsBlend",
                blendType = BlendTreeType.Simple1D,
                blendParameter = "Speed",
                useAutomaticThresholds = false
            };
            AssetDatabase.AddObjectToAsset(tree, controller);
            tree.AddChild(idle, 0f);
            tree.AddChild(walk, .46f);
            tree.AddChild(run, 1f);

            AnimatorState state = stateMachine.AddState("Natural arm locomotion", new Vector3(260f, 90f));
            state.motion = tree;
            state.timeParameterActive = true;
            state.timeParameter = "GaitPhase";
            state.writeDefaultValues = true;
            stateMachine.defaultState = state;
        }

        private static AnimatorController RecreateController(string path)
        {
            // Rebuilding is an explicit/editor-only operation. Automatic project loads skip this path
            // when the version is current, so a running character never loses its controller asset.
            if (AssetDatabase.LoadAssetAtPath<AnimatorController>(path) != null) AssetDatabase.DeleteAsset(path);
            return AnimatorController.CreateAnimatorControllerAtPath(path);
        }

        private static AnimatorState AddState(AnimatorStateMachine machine, string name, AnimationClip clip, Vector3 position)
        {
            AnimatorState state = machine.AddState(name, position);
            state.motion = clip;
            state.writeDefaultValues = true;
            return state;
        }

        private static void AddFloatTransition(AnimatorState from, AnimatorState to, string parameter, AnimatorConditionMode mode, float threshold, float duration)
        {
            AnimatorStateTransition transition = from.AddTransition(to);
            transition.AddCondition(mode, threshold, parameter);
            transition.hasExitTime = false;
            transition.duration = duration;
        }

        private static void AddAction(AnimatorStateMachine machine, AnimatorState returnState, string name, AnimationClip clip, string trigger, Vector3 position, float exitTime)
        {
            AnimatorState state = AddState(machine, name, clip, position);
            AnimatorStateTransition enter = machine.AddAnyStateTransition(state);
            enter.AddCondition(AnimatorConditionMode.If, 0f, trigger);
            enter.hasExitTime = false;
            enter.duration = 0.06f;
            enter.canTransitionToSelf = false;

            AnimatorStateTransition exit = state.AddTransition(returnState);
            exit.hasExitTime = true;
            exit.exitTime = exitTime;
            exit.duration = 0.10f;
        }

        private static AnimationClip Find(IReadOnlyDictionary<string, AnimationClip> clips, params string[] names)
        {
            foreach (string name in names)
            {
                if (clips.TryGetValue(name, out AnimationClip exact)) return exact;
            }
            foreach (string name in names)
            {
                AnimationClip partial = clips.Values.FirstOrDefault(clip => clip.name.Contains(name, StringComparison.OrdinalIgnoreCase));
                if (partial != null) return partial;
            }
            return clips.Values.First();
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
    }
}
#endif
