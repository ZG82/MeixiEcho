using UnityEngine;

namespace MeixiEcho
{
    /// <summary>A collider-backed hinged door used by the chapter-two exploration rooms.</summary>
    [DisallowMultipleComponent]
    public sealed class TheatreSwingDoor : MonoBehaviour
    {
        private Quaternion closedRotation;
        private Quaternion openRotation;
        private bool configured;
        private bool open;

        public bool IsOpen => open;
        public string InteractionPrompt => open ? "关门" : "开门";

        public void Configure(float openYaw)
        {
            closedRotation = transform.localRotation;
            openRotation = closedRotation * Quaternion.Euler(0f, openYaw, 0f);
            configured = true;
        }

        public void Toggle()
        {
            if (!configured) Configure(-96f);
            open = !open;
        }

        private void Update()
        {
            if (!configured) Configure(-96f);
            Quaternion target = open ? openRotation : closedRotation;
            transform.localRotation = Quaternion.Slerp(transform.localRotation, target,
                1f - Mathf.Exp(-8.5f * Time.deltaTime));
        }
    }
}
