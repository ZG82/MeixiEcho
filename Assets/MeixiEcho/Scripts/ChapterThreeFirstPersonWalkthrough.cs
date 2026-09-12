using UnityEngine;
using UnityEngine.InputSystem;

namespace MeixiEcho
{
    /// <summary>
    /// Smooth first-person navigation for Chapter Three. This component lives in its own
    /// script asset so editor-baked scenes retain a stable MonoScript reference.
    /// </summary>
    public sealed class ChapterThreeFirstPersonWalkthrough : MonoBehaviour
    {
        private CharacterController capsule;
        private Camera viewCamera;
        private float pitch;
        private float verticalVelocity;
        private Vector3 smoothVelocity;
        private Vector3 smoothVelocityRef;
        private Vector3 cameraBasePosition;
        private float stepPhase;

        public void Bind(CharacterController value) => capsule = value;

        private void Awake()
        {
            if (capsule == null) capsule = GetComponent<CharacterController>();
        }

        private void Start()
        {
            viewCamera = GetComponentInChildren<Camera>();
            if (viewCamera != null) cameraBasePosition = viewCamera.transform.localPosition;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void Update()
        {
            if (MeixiCursorState.GameplayInputBlocked || Time.timeScale == 0f) return;
            if (capsule == null || viewCamera == null || Keyboard.current == null || Mouse.current == null) return;

            Vector2 move = new(
                (Keyboard.current.dKey.isPressed ? 1f : 0f) - (Keyboard.current.aKey.isPressed ? 1f : 0f),
                (Keyboard.current.wKey.isPressed ? 1f : 0f) - (Keyboard.current.sKey.isPressed ? 1f : 0f));
            // A small pace lift keeps the enlarged homestay comfortable to
            // traverse without turning normal walking into a sprint.
            float speed = Keyboard.current.leftShiftKey.isPressed ? 4.40f : 2.88f;
            if (capsule.isGrounded && verticalVelocity < 0f) verticalVelocity = -1.4f;
            verticalVelocity += Physics.gravity.y * Time.deltaTime;

            Vector3 direction = (transform.right * move.x + transform.forward * move.y).normalized;
            Vector3 targetVelocity = direction * speed;
            smoothVelocity = Vector3.SmoothDamp(
                smoothVelocity,
                targetVelocity,
                ref smoothVelocityRef,
                direction.sqrMagnitude > .01f ? .085f : .12f);
            capsule.Move((smoothVelocity + Vector3.up * verticalVelocity) * Time.deltaTime);

            Vector2 mouse = Mouse.current.delta.ReadValue() * .055f;
            transform.Rotate(Vector3.up, mouse.x);
            pitch = Mathf.Clamp(pitch - mouse.y, -82f, 82f);
            viewCamera.transform.localRotation = Quaternion.Euler(pitch, 0f, 0f);

            float planarSpeed = new Vector2(smoothVelocity.x, smoothVelocity.z).magnitude;
            if (capsule.isGrounded && planarSpeed > .18f)
                stepPhase += Time.deltaTime * (Keyboard.current.leftShiftKey.isPressed ? 10.8f : 7.8f);
            else
                stepPhase = Mathf.Lerp(stepPhase, 0f, Time.deltaTime * 4f);

            float bob = capsule.isGrounded
                ? Mathf.Sin(stepPhase) * Mathf.Clamp01(planarSpeed) * .018f
                : 0f;
            viewCamera.transform.localPosition = Vector3.Lerp(
                viewCamera.transform.localPosition,
                cameraBasePosition + Vector3.up * bob,
                Time.deltaTime * 12f);
            viewCamera.fieldOfView = Mathf.Lerp(
                viewCamera.fieldOfView,
                Keyboard.current.leftShiftKey.isPressed && move.sqrMagnitude > .01f ? 76f : 72f,
                Time.deltaTime * 7f);

            // Escape is owned by ChapterThreeGameDirector's shared pause menu.
            // Handling it here only unlocked the cursor and prevented a real pause.
        }
    }
}
