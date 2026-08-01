using UnityEngine;
using UnityEngine.SceneManagement;

namespace MeixiEcho
{
    /// <summary>
    /// Single source of truth for the desktop cursor.  Gameplay and UI code
    /// request a mode, while the coordinator reapplies it after every frame so
    /// scene activation and modal transitions cannot leave a stale cursor state.
    /// </summary>
    public static class MeixiCursorState
    {
        private static bool pointerMode;

        public static bool IsPointerMode => pointerMode;

        public static void SetGameplay()
        {
            pointerMode = false;
            Apply();
        }

        public static void SetPointer()
        {
            pointerMode = true;
            Apply();
        }

        public static void Apply()
        {
            if (!Application.isFocused) return;
            CursorLockMode targetLock = pointerMode ? CursorLockMode.None : CursorLockMode.Locked;
            if (Cursor.lockState != targetLock) Cursor.lockState = targetLock;
            if (Cursor.visible != pointerMode) Cursor.visible = pointerMode;
        }
    }

    [DefaultExecutionOrder(32000)]
    internal sealed class MeixiCursorCoordinator : MonoBehaviour
    {
        private static MeixiCursorCoordinator instance;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Create()
        {
            if (instance != null) return;
            GameObject root = new GameObject("MeixiCursorCoordinator");
            instance = root.AddComponent<MeixiCursorCoordinator>();
            DontDestroyOnLoad(root);
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;
            DontDestroyOnLoad(gameObject);
            SceneManager.activeSceneChanged += HandleSceneChanged;
            MeixiCursorState.SetGameplay();
        }

        private void OnDestroy()
        {
            if (instance != this) return;
            SceneManager.activeSceneChanged -= HandleSceneChanged;
            instance = null;
        }

        private void HandleSceneChanged(Scene previous, Scene next)
        {
            // Every scene starts in gameplay mode.  A quiz, menu or inventory
            // explicitly opts into pointer mode on the following frame.
            MeixiCursorState.SetGameplay();
        }

        private void OnApplicationFocus(bool focused)
        {
            if (focused) MeixiCursorState.Apply();
        }

        private void LateUpdate()
        {
            MeixiCursorState.Apply();
        }
    }
}
