using UnityEngine;

namespace MeixiEcho
{
    /// <summary>Loads the performance hall when the player walks beyond the village exit.</summary>
    [RequireComponent(typeof(BoxCollider))]
    public sealed class MeixiSceneExit : MonoBehaviour
    {
        private bool loading;

        private void OnTriggerEnter(Collider other)
        {
            if (loading || other.GetComponentInParent<MeixiThirdPersonPlayer>() == null) return;
            MeixiGame3D game = FindAnyObjectByType<MeixiGame3D>();
            if (game == null) return;
            loading = true;
            game.TryEnterSecondChapterFromRoad();
            // If the route has not been unlocked yet, allow the player to return
            // after leaving and entering the trigger again later.
            Invoke(nameof(ResetTrigger), 1f);
        }

        private void ResetTrigger() => loading = false;
    }
}
