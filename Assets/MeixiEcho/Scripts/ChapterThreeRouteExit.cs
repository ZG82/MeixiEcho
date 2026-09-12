using UnityEngine;

namespace MeixiEcho
{
    /// <summary>Loads the chapter-three homestay from the optional east branch.</summary>
    [RequireComponent(typeof(BoxCollider))]
    public sealed class ChapterThreeRouteExit : MonoBehaviour
    {
        private bool loading;

        private void OnTriggerEnter(Collider other)
        {
            if (loading || other.GetComponentInParent<MeixiThirdPersonPlayer>() == null) return;
            if (MeixiLocalProgress.CurrentChapter < 3)
            {
                Debug.Log("[MeixiRoute] Chapter three branch is locked until chapter two is complete.");
                return;
            }
            loading = true;
            if (!MeixiChapterRouter.TryLoadChapterThree(false, out string error))
            {
                loading = false;
                Debug.LogError("[MeixiRoute] " + error);
            }
        }
    }
}
