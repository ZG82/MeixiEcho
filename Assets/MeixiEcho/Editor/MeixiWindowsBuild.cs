#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace MeixiEcho.Editor
{
    public static class MeixiWindowsBuild
    {
        [MenuItem("梅溪回响/构建 Windows Demo")]
        public static void BuildWindowsDemo()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
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
    }
}
#endif
