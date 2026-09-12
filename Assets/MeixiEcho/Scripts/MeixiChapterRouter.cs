using UnityEngine;
using UnityEngine.SceneManagement;

namespace MeixiEcho
{
    /// <summary>Single authoritative entry point for loading chapter three.</summary>
    public static class MeixiChapterRouter
    {
        public const string ChapterThreeScene = "MeixiChapterThree";

        public static bool CanLoadChapterThree(out string error)
        {
            if (Application.CanStreamedLevelBeLoaded(ChapterThreeScene))
            {
                error = string.Empty;
                return true;
            }
            error = "第三章场景未加入当前构建。";
            Debug.LogError("[MeixiChapterThree] Scene is missing from Build Settings: " + ChapterThreeScene);
            return false;
        }

        public static bool TryLoadChapterThree(bool resetChapterProgress, out string error)
        {
            if (!CanLoadChapterThree(out error)) return false;
            if (resetChapterProgress) MeixiLocalProgress.SetGameplayChapter(3);
            Time.timeScale = 1f;
            AudioListener.pause = false;
            MeixiCursorState.SetPointer();
            AsyncOperation operation = SceneManager.LoadSceneAsync(ChapterThreeScene, LoadSceneMode.Single);
            if (operation != null) return true;
            error = "Unity 无法创建第三章加载任务。";
            Debug.LogError("[MeixiChapterThree] LoadSceneAsync returned null.");
            return false;
        }
    }
}
