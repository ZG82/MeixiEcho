#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace MeixiEcho.Editor
{
    public static class MeixiWindowsBuild
    {
        private const string ApplicationIconPath = "Assets/MeixiEcho/Art/Logos/meixi_app_icon.png";

        [MenuItem("梅溪回响/构建 Windows Demo")]
        public static void BuildWindowsDemo()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            ConfigureApplicationIcon();
            MeixiAnimationSetup.EnsureAssets();
            if (!File.Exists(Path.GetFullPath(Path.Combine(Application.dataPath, "Scenes", "MeixiEditable.unity"))))
            {
                MeixiEditableSceneBuilder.BakeEditableSceneBatch();
            }
            // Recreate the tiny theatre bootstrap scene. Its visual set is generated
            // after load so the Windows level stream stays stable across rebuilds.
            MeixiTheatreSceneBuilder.EnsureSceneBatch();
            string outputDirectory = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Build", "Windows"));
            Directory.CreateDirectory(outputDirectory);
            string executable = Path.Combine(outputDirectory, "MeixiEcho.exe");

            string[] scenes = { "Assets/Scenes/MeixiEditable.unity", "Assets/Scenes/MeixiTheatre.unity" };
            var options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = executable,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None
            };

            PlayerSettings.companyName = "Meixi Digital Culture Lab";
            PlayerSettings.productName = "梅溪回响";
            PlayerSettings.defaultScreenWidth = 1920;
            PlayerSettings.defaultScreenHeight = 1080;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.resizableWindow = true;
            PlayerSettings.allowFullscreenSwitch = true;

            BuildReport report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result == BuildResult.Succeeded)
            {
                string minOperaNotice = Path.GetFullPath(Path.Combine(Application.dataPath,
                    "MeixiEcho", "ThirdPartyNotices", "MinOperaPrelude.md"));
                if (File.Exists(minOperaNotice))
                    File.Copy(minOperaNotice, Path.Combine(outputDirectory,
                        "MinOperaPrelude-ThirdPartyNotice.txt"), true);
                Debug.Log($"《梅溪回响》Windows Demo 构建完成：{executable}");
                EditorUtility.RevealInFinder(executable);
            }
            else
            {
                Debug.LogError($"Windows 构建失败：{report.summary.result}");
            }
        }

        [MenuItem("Meixi Echo/Configure Application Icon")]
        public static void ConfigureApplicationIcon()
        {
            TextureImporter importer = AssetImporter.GetAtPath(ApplicationIconPath) as TextureImporter;
            if (importer == null)
                throw new FileNotFoundException("Application icon was not found.", ApplicationIconPath);

            bool importerChanged = importer.textureType != TextureImporterType.Default
                || importer.mipmapEnabled
                || importer.alphaIsTransparency
                || importer.maxTextureSize != 1024
                || importer.textureCompression != TextureImporterCompression.Uncompressed;

            if (importerChanged)
            {
                importer.textureType = TextureImporterType.Default;
                importer.mipmapEnabled = false;
                importer.alphaIsTransparency = false;
                importer.maxTextureSize = 1024;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.SaveAndReimport();
            }

            Texture2D icon = AssetDatabase.LoadAssetAtPath<Texture2D>(ApplicationIconPath);
            if (icon == null)
                throw new FileNotFoundException("Application icon could not be imported.", ApplicationIconPath);

            int[] iconSizes = PlayerSettings.GetIconSizes(NamedBuildTarget.Standalone, IconKind.Any);
            Texture2D[] icons = new Texture2D[iconSizes.Length];
            for (int index = 0; index < icons.Length; index++)
                icons[index] = icon;

            PlayerSettings.SetIcons(NamedBuildTarget.Standalone, icons, IconKind.Any);
            AssetDatabase.SaveAssets();
            Debug.Log($"Configured Windows application icon: {ApplicationIconPath}");
        }
    }
}
#endif
