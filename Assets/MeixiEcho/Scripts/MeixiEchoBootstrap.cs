using UnityEngine;
using UnityEngine.SceneManagement;

namespace MeixiEcho
{
    public static class MeixiEchoBootstrap
    {
        private static bool theatreCaptureRedirected;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Register()
        {
            theatreCaptureRedirected = false;
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (!theatreCaptureRedirected && scene.name != "MeixiTheatre" &&
                System.Array.Exists(System.Environment.GetCommandLineArgs(), value =>
                    string.Equals(value, "-meixiTheatreCapture", System.StringComparison.OrdinalIgnoreCase)))
            {
                theatreCaptureRedirected = true;
                // Loading another large scene synchronously from inside sceneLoaded
                // can re-enter Unity 6's level deserializer. Defer one frame so the
                // current scene finishes activation before redirecting validation.
                new GameObject("第二章延迟转场").AddComponent<MeixiTheatreCaptureRedirect>();
                return;
            }
            // The theatre is a self-contained performance scene.  It deliberately
            // does not use the village game root, so don't bootstrap the village
            // again when it is loaded through the exit road.
            if (Object.FindAnyObjectByType<MeixiGame3D>() != null ||
                Object.FindAnyObjectByType<TheatreStageRuntime>() != null)
            {
                return;
            }

            var root = new GameObject("梅溪回响_3DGameRoot");
            root.AddComponent<MeixiGame3D>();
        }
    }

    public sealed class MeixiTheatreCaptureRedirect : MonoBehaviour
    {
        private System.Collections.IEnumerator Start()
        {
            DontDestroyOnLoad(gameObject);
            yield return null;
            yield return SceneManager.LoadSceneAsync("MeixiTheatre", LoadSceneMode.Single);
            Destroy(gameObject);
        }
    }
}
