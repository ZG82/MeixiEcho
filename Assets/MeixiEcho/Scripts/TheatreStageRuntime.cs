using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MeixiEcho
{
    /// <summary>Stable third-person controller and chapter presentation for the old theatre.</summary>
    public sealed class TheatreStageRuntime : MonoBehaviour
    {
        private CharacterController controller;
        private Transform player;
        private Transform cameraRig;
        private Camera stageCamera;
        private Animator animator;
        private Canvas canvas;
        private Text hint;
        private Text objective;
        private GameObject introPanel;
        private float verticalVelocity;
        private float yaw;
        private float pitch = 19f;
        private float cameraDistance = 6.6f;
        private float introUntil;
        private float lookBlockedUntil;
        private Vector3 horizontalVelocity;
        private Vector3 horizontalSmoothVelocity;
        private Vector3 cameraVelocity;
        private Vector2 smoothedLook;
        private readonly RaycastHit[] cameraHits = new RaycastHit[24];
        private readonly List<TheatreInstrument> nearbyInstruments = new();
        private readonly HashSet<TheatreInstrument> playedInstruments = new();

        private void Awake()
        {
            TheatreRuntimeEnvironment.Ensure(transform);
            GameObject playerObject = GameObject.Find("戏台访客");
            controller = playerObject != null ? playerObject.GetComponent<CharacterController>() : FindAnyObjectByType<CharacterController>();
            player = controller != null ? controller.transform : null;
            GameObject cameraObject = GameObject.Find("戏台第三人称相机");
            stageCamera = cameraObject != null ? cameraObject.GetComponent<Camera>() : Camera.main;
            cameraRig = stageCamera != null ? stageCamera.transform : null;
            if (player == null || cameraRig == null)
            {
                Debug.LogError("[MeixiTheatre] Missing player or camera binding.");
                enabled = false;
                return;
            }

            Time.timeScale = 1f;
            controller.enabled = true;
            EnsurePlayerVisual();
            animator = player.GetComponentInChildren<Animator>(true);
            yaw = player.eulerAngles.y;
            verticalVelocity = -1.8f;
            horizontalVelocity = Vector3.zero;
            horizontalSmoothVelocity = Vector3.zero;
            cameraVelocity = Vector3.zero;
            smoothedLook = Vector2.zero;
            lookBlockedUntil = Time.unscaledTime + 0.40f;
            introUntil = Time.unscaledTime + 3.55f;
            CreateHud();
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            PositionCameraImmediately();
            TryStartCaptureMode();
            Debug.Log("[MeixiTheatre] Runtime reset; third-person stage controller ready.");
        }

        private void EnsurePlayerVisual()
        {
            if (player.GetComponentInChildren<Animator>(true) != null) return;
            Transform placeholder = player.Find("舞台角色加载占位");
            if (placeholder != null) placeholder.gameObject.SetActive(false);
            GameObject source = Resources.Load<GameObject>("Models/Characters/LinMo");
            if (source == null) return;
            GameObject visual = Instantiate(source, player);
            visual.name = "林默_舞台模型";
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.identity;
            Renderer[] renderers = visual.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length > 0)
            {
                Bounds bounds = renderers[0].bounds;
                for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
                float scale = 1.76f / Mathf.Max(0.01f, bounds.size.y);
                visual.transform.localScale *= scale;
                renderers = visual.GetComponentsInChildren<Renderer>(true);
                bounds = renderers[0].bounds;
                for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
                visual.transform.position += new Vector3(player.position.x - bounds.center.x,
                    player.position.y + 0.02f - bounds.min.y, player.position.z - bounds.center.z);
            }
            Animator visualAnimator = visual.GetComponentInChildren<Animator>(true);
            if (visualAnimator != null)
            {
                visualAnimator.runtimeAnimatorController = Resources.Load<RuntimeAnimatorController>("Animations/LinMoCombat");
                visualAnimator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                visualAnimator.Rebind();
                visualAnimator.Update(0f);
            }
        }

        private void Update()
        {
            if (player == null || controller == null) return;
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
            {
                Time.timeScale = 1f;
                SceneManager.LoadScene("MeixiEditable");
                return;
            }

            bool introActive = Time.unscaledTime < introUntil;
            if (introActive && keyboard != null &&
                (keyboard.enterKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame ||
                 keyboard.wKey.wasPressedThisFrame || keyboard.aKey.wasPressedThisFrame ||
                 keyboard.sKey.wasPressedThisFrame || keyboard.dKey.wasPressedThisFrame ||
                 keyboard.upArrowKey.wasPressedThisFrame || keyboard.downArrowKey.wasPressedThisFrame ||
                 keyboard.leftArrowKey.wasPressedThisFrame || keyboard.rightArrowKey.wasPressedThisFrame))
            {
                introUntil = Time.unscaledTime;
                introActive = false;
                lookBlockedUntil = Time.unscaledTime + 0.12f;
                smoothedLook = Vector2.zero;
            }
            if (introPanel != null && introPanel.activeSelf && !introActive)
            {
                introPanel.SetActive(false);
                lookBlockedUntil = Time.unscaledTime + 0.22f;
                smoothedLook = Vector2.zero;
            }
            UpdateMovement(!introActive);
            UpdateInteraction();
        }

        private void LateUpdate()
        {
            if (player != null && cameraRig != null) UpdateCamera();
        }

        private void UpdateMovement(bool acceptsInput)
        {
            Keyboard keyboard = Keyboard.current;
            float x = 0f;
            float z = 0f;
            if (acceptsInput && keyboard != null)
            {
                x = (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed ? 1f : 0f) -
                    (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed ? 1f : 0f);
                z = (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed ? 1f : 0f) -
                    (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed ? 1f : 0f);
            }

            Vector3 cameraForward = Quaternion.Euler(0f, yaw, 0f) * Vector3.forward;
            Vector3 cameraRight = Quaternion.Euler(0f, yaw + 90f, 0f) * Vector3.forward;
            Vector3 move = Vector3.ClampMagnitude(cameraForward * z + cameraRight * x, 1f);
            if (move.sqrMagnitude > 0.01f)
                player.forward = Vector3.Slerp(player.forward, move, 1f - Mathf.Exp(-14f * Time.deltaTime));

            Vector3 targetVelocity = move * 4.7f;
            horizontalVelocity = Vector3.SmoothDamp(horizontalVelocity, targetVelocity, ref horizontalSmoothVelocity,
                targetVelocity.sqrMagnitude > 0.01f ? 0.08f : 0.12f, Mathf.Infinity, Time.deltaTime);
            if (IsGrounded()) verticalVelocity = -1.8f;
            else verticalVelocity = Mathf.Max(verticalVelocity + Physics.gravity.y * 2.15f * Time.deltaTime, -24f);
            controller.Move((horizontalVelocity + Vector3.up * verticalVelocity) * Time.deltaTime);

            if (animator != null && animator.runtimeAnimatorController != null)
            {
                animator.SetFloat("Speed", Mathf.Clamp01(horizontalVelocity.magnitude / 4.7f) * 0.62f);
                animator.SetBool("Grounded", IsGrounded());
            }

            if (player.position.y < -3f)
            {
                controller.enabled = false;
                player.position = new Vector3(0f, 0.08f, -10.5f);
                player.rotation = Quaternion.identity;
                controller.enabled = true;
                verticalVelocity = -1.8f;
                horizontalVelocity = Vector3.zero;
            }
        }

        private void UpdateCamera()
        {
            Mouse mouse = Mouse.current;
            if (Time.unscaledTime >= introUntil && mouse != null && Cursor.lockState == CursorLockMode.Locked &&
                Time.unscaledTime >= lookBlockedUntil)
            {
                Vector2 raw = Vector2.ClampMagnitude(mouse.delta.ReadValue(), 28f);
                smoothedLook = Vector2.Lerp(smoothedLook, raw, 1f - Mathf.Exp(-20f * Time.unscaledDeltaTime));
                yaw += smoothedLook.x * 0.082f;
                pitch = Mathf.Clamp(pitch - smoothedLook.y * 0.070f, 10f, 42f);
                cameraDistance = Mathf.Clamp(cameraDistance - mouse.scroll.ReadValue().y * 0.004f, 4.8f, 8.6f);
            }
            else
            {
                smoothedLook = Vector2.Lerp(smoothedLook, Vector2.zero, 1f - Mathf.Exp(-24f * Time.unscaledDeltaTime));
            }

            Vector3 pivot = player.position + Vector3.up * 1.48f;
            Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
            Vector3 desired = ResolveCameraCollision(pivot, pivot - rotation * Vector3.forward * cameraDistance);
            if (Time.unscaledTime < introUntil)
            {
                float t = Mathf.Clamp01(1f - (introUntil - Time.unscaledTime) / 3.55f);
                float eased = t * t * (3f - 2f * t);
                Vector3 establishingPosition = new Vector3(10.8f, 6.5f, -7.8f);
                Vector3 establishingLook = new Vector3(0f, 3.1f, 8.2f);
                cameraRig.position = Vector3.Lerp(establishingPosition, desired, eased);
                Vector3 lookPoint = Vector3.Lerp(establishingLook, pivot, eased);
                cameraRig.rotation = Quaternion.LookRotation(lookPoint - cameraRig.position, Vector3.up);
                return;
            }

            cameraRig.position = Vector3.SmoothDamp(cameraRig.position, desired, ref cameraVelocity, 0.095f);
            Quaternion targetRotation = Quaternion.LookRotation(pivot - cameraRig.position, Vector3.up);
            cameraRig.rotation = Quaternion.Slerp(cameraRig.rotation, targetRotation, 1f - Mathf.Exp(-18f * Time.unscaledDeltaTime));
        }

        private bool IsGrounded()
        {
            if (controller.isGrounded) return true;
            Vector3 origin = player.position + Vector3.up * 0.24f;
            return Physics.SphereCast(origin, 0.25f, Vector3.down, out RaycastHit hit, 0.38f, ~0,
                       QueryTriggerInteraction.Ignore) && hit.collider != controller &&
                   Vector3.Dot(hit.normal, Vector3.up) > 0.48f;
        }

        private Vector3 ResolveCameraCollision(Vector3 pivot, Vector3 desired)
        {
            Vector3 cast = desired - pivot;
            float length = cast.magnitude;
            if (length < 0.01f) return desired;
            int count = Physics.SphereCastNonAlloc(pivot, 0.26f, cast / length, cameraHits, length, ~0,
                QueryTriggerInteraction.Ignore);
            float nearest = length;
            for (int i = 0; i < count; i++)
            {
                Collider hit = cameraHits[i].collider;
                if (hit == null || hit == controller || hit.transform.IsChildOf(player)) continue;
                if (hit.GetComponentInParent<TheatreInstrument>() != null) continue;
                if (hit.bounds.max.y < pivot.y - 0.18f) continue;
                nearest = Mathf.Min(nearest, cameraHits[i].distance);
            }
            return pivot + cast.normalized * Mathf.Max(0.62f, nearest - 0.18f);
        }

        private void PositionCameraImmediately()
        {
            Vector3 pivot = player.position + Vector3.up * 1.48f;
            Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
            cameraRig.position = ResolveCameraCollision(pivot, pivot - rotation * Vector3.forward * cameraDistance);
            cameraRig.rotation = Quaternion.LookRotation(pivot - cameraRig.position, Vector3.up);
        }

        private void UpdateInteraction()
        {
            nearbyInstruments.RemoveAll(item => item == null || !item.IsNearby(player.position));
            TheatreInstrument current = null;
            float bestDistance = float.PositiveInfinity;
            foreach (TheatreInstrument item in nearbyInstruments)
            {
                float distance = Vector3.SqrMagnitude(item.transform.position - player.position);
                if (distance >= bestDistance) continue;
                bestDistance = distance;
                current = item;
            }

            if (current == null)
            {
                hint.text = "WASD  行走   ·   鼠标  环绕镜头   ·   E  演奏乐器   ·   ESC  返回梅溪村";
                return;
            }
            hint.text = "[ E ]  演奏「" + current.DisplayName + "」\n" + current.Description;
            if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame) current.Play();
        }

        public void RegisterNearby(TheatreInstrument instrument)
        {
            if (instrument != null && !nearbyInstruments.Contains(instrument)) nearbyInstruments.Add(instrument);
        }

        public void NotifyInstrumentPlayed(TheatreInstrument instrument)
        {
            if (instrument == null) return;
            playedInstruments.Add(instrument);
            int total = FindObjectsByType<TheatreInstrument>().Length;
            objective.text = playedInstruments.Count >= total
                ? "旧曲的四个声部已经归位，古戏台重新拥有了回响"
                : $"已唤醒 {playedInstruments.Count} / {Mathf.Max(1, total)} 个声部 · 继续寻找发光的传统乐器";
            cameraDistance = Mathf.Max(5.4f, cameraDistance - 0.18f);
        }

        private void CreateHud()
        {
            canvas = new GameObject("第二章舞台界面").AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvas.gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            GameObject questCard = CreatePanel("章节目标卡", canvas.transform, new Color(0.018f, 0.045f, 0.050f, 0.91f),
                new Vector2(0.025f, 0.82f), new Vector2(0.36f, 0.965f));
            AddOutline(questCard, new Color(0.92f, 0.55f, 0.19f, 0.75f));
            Text chapter = CreateText("章节标题", questCard.transform, "第 二 章  ·  古 厝 里 的 旧 曲", 28,
                TextAnchor.MiddleLeft, new Color(1f, 0.76f, 0.34f));
            SetRect(chapter.rectTransform, new Vector2(0.06f, 0.52f), new Vector2(0.95f, 0.90f));
            objective = CreateText("章节目标", questCard.transform, "登上古戏台，依次唤醒编钟、大鼓与古弦的声纹", 18,
                TextAnchor.MiddleLeft, new Color(0.76f, 0.92f, 0.94f));
            SetRect(objective.rectTransform, new Vector2(0.06f, 0.10f), new Vector2(0.95f, 0.50f));

            GameObject hintCard = CreatePanel("交互提示卡", canvas.transform, new Color(0.008f, 0.025f, 0.030f, 0.90f),
                new Vector2(0.20f, 0.025f), new Vector2(0.80f, 0.15f));
            AddOutline(hintCard, new Color(0.32f, 0.82f, 0.88f, 0.62f));
            hint = CreateText("舞台提示文字", hintCard.transform, string.Empty, 20, TextAnchor.MiddleCenter,
                new Color(0.94f, 0.96f, 0.92f));
            SetRect(hint.rectTransform, new Vector2(0.04f, 0.08f), new Vector2(0.96f, 0.92f));

            introPanel = CreatePanel("第二章转场", canvas.transform, new Color(0.008f, 0.018f, 0.022f, 0.76f),
                Vector2.zero, Vector2.one);
            Text eyebrow = CreateText("转场眉题", introPanel.transform, "MEIXI VOICE ARCHIVE  /  THE OLD THEATRE", 19,
                TextAnchor.MiddleCenter, new Color(0.36f, 0.82f, 0.88f));
            SetRect(eyebrow.rectTransform, new Vector2(0.20f, 0.62f), new Vector2(0.80f, 0.70f));
            Text title = CreateText("转场标题", introPanel.transform, "第 二 章\n古 厝 里 的 旧 曲", 64,
                TextAnchor.MiddleCenter, new Color(1f, 0.72f, 0.28f));
            SetRect(title.rectTransform, new Vector2(0.15f, 0.37f), new Vector2(0.85f, 0.62f));
            AddOutline(title.gameObject, new Color(0f, 0f, 0f, 0.95f), 3f);
            Text subtitle = CreateText("转场副题", introPanel.transform, "风穿过百年木梁，墙壁开始归还被遗忘的旋律", 23,
                TextAnchor.MiddleCenter, new Color(0.86f, 0.91f, 0.90f));
            SetRect(subtitle.rectTransform, new Vector2(0.18f, 0.28f), new Vector2(0.82f, 0.38f));
        }

        private static GameObject CreatePanel(string name, Transform parent, Color color, Vector2 min, Vector2 max)
        {
            GameObject panel = new GameObject(name);
            panel.transform.SetParent(parent, false);
            Image image = panel.AddComponent<Image>();
            image.color = color;
            SetRect(image.rectTransform, min, max);
            return panel;
        }

        private static Text CreateText(string name, Transform parent, string value, int size, TextAnchor alignment, Color color)
        {
            Text text = new GameObject(name).AddComponent<Text>();
            text.transform.SetParent(parent, false);
            text.font = Font.CreateDynamicFontFromOSFont(new[] { "Microsoft YaHei", "SimHei", "Arial" }, size);
            text.text = value;
            text.fontSize = size;
            text.alignment = alignment;
            text.color = color;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            return text;
        }

        private static void SetRect(RectTransform rect, Vector2 min, Vector2 max)
        {
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void AddOutline(GameObject target, Color color, float size = 1.2f)
        {
            Outline outline = target.AddComponent<Outline>();
            outline.effectColor = color;
            outline.effectDistance = new Vector2(size, -size);
        }

        private void OnApplicationFocus(bool focused)
        {
            if (!focused) return;
            Time.timeScale = 1f;
            if (controller != null) controller.enabled = true;
            smoothedLook = Vector2.zero;
            lookBlockedUntil = Time.unscaledTime + 0.30f;
            if (Time.unscaledTime >= introUntil)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }

        private void TryStartCaptureMode()
        {
            string[] arguments = Environment.GetCommandLineArgs();
            if (!Array.Exists(arguments, value => string.Equals(value, "-meixiTheatreCapture", StringComparison.OrdinalIgnoreCase))) return;
            introUntil = Time.unscaledTime + 0.25f;
            StartCoroutine(CaptureTheatre());
        }

        private IEnumerator CaptureTheatre()
        {
            yield return new WaitForSecondsRealtime(1.7f);
            string root = Directory.GetParent(Application.dataPath)?.FullName ?? Application.dataPath;
            string path = Path.Combine(root, "MeixiTheatreCapture.png");
            CaptureFrame(path);
            Debug.Log("[MeixiTheatre] Capture written: " + path);
            yield return new WaitForSecondsRealtime(1.1f);
            Application.Quit();
        }

        private void CaptureFrame(string path)
        {
            if (stageCamera == null) return;
            RenderTexture target = new RenderTexture(1280, 720, 24, RenderTextureFormat.ARGB32);
            RenderTexture previousTarget = stageCamera.targetTexture;
            RenderTexture previousActive = RenderTexture.active;
            RenderMode previousMode = canvas.renderMode;
            Camera previousCanvasCamera = canvas.worldCamera;
            float previousPlaneDistance = canvas.planeDistance;
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = stageCamera;
            canvas.planeDistance = 0.55f;
            Canvas.ForceUpdateCanvases();
            stageCamera.targetTexture = target;
            RenderTexture.active = target;
            stageCamera.Render();
            Texture2D texture = new Texture2D(target.width, target.height, TextureFormat.RGB24, false);
            texture.ReadPixels(new Rect(0f, 0f, target.width, target.height), 0, 0);
            texture.Apply();
            File.WriteAllBytes(path, texture.EncodeToPNG());
            stageCamera.targetTexture = previousTarget;
            RenderTexture.active = previousActive;
            canvas.renderMode = previousMode;
            canvas.worldCamera = previousCanvasCamera;
            canvas.planeDistance = previousPlaneDistance;
            Destroy(texture);
            target.Release();
            Destroy(target);
        }
    }

    [RequireComponent(typeof(SphereCollider))]
    public sealed class TheatreInstrument : MonoBehaviour
    {
        [SerializeField] private string displayName;
        [SerializeField] private string description;
        [SerializeField] private float tonePitch = 1f;
        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? gameObject.name : displayName;
        public string Description => description;
        private AudioSource audioSource;
        private Renderer[] renderers;
        private Color[] originalColors;
        private float pulseUntil;
        private bool initialized;

        public void Configure(string nameValue, string descriptionValue, float pitchValue)
        {
            displayName = nameValue;
            description = descriptionValue;
            tonePitch = pitchValue;
            Initialize();
            // AddComponent invokes Awake before Configure. Refresh the runtime tone
            // after the requested instrument pitch has been assigned.
            if (audioSource != null) audioSource.clip = CreateTone(0.72f, 196f * tonePitch);
        }

        private void Awake() => Initialize();

        private void Initialize()
        {
            if (initialized) return;
            SphereCollider sphere = GetComponent<SphereCollider>();
            sphere.isTrigger = true;
            sphere.radius = 1.9f;
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0.72f;
            audioSource.minDistance = 1.5f;
            audioSource.maxDistance = 15f;
            audioSource.clip = CreateTone(0.72f, 196f * tonePitch);
            renderers = GetComponentsInChildren<Renderer>();
            originalColors = new Color[renderers.Length];
            for (int i = 0; i < renderers.Length; i++) originalColors[i] = renderers[i].material.color;
            initialized = true;
        }

        public bool IsNearby(Vector3 position)
        {
            Vector3 delta = position - transform.position;
            delta.y = 0f;
            return delta.sqrMagnitude < 8.4f;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.GetComponentInParent<CharacterController>() == null) return;
            FindAnyObjectByType<TheatreStageRuntime>()?.RegisterNearby(this);
        }

        public void Play()
        {
            if (audioSource == null) Initialize();
            audioSource.Play();
            pulseUntil = Time.time + 0.72f;
            FindAnyObjectByType<TheatreStageRuntime>()?.NotifyInstrumentPlayed(this);
        }

        private void Update()
        {
            if (renderers == null) return;
            float glow = Mathf.Clamp01((pulseUntil - Time.time) / 0.72f);
            float wave = 0.55f + Mathf.Sin(Time.time * 18f) * 0.45f;
            for (int i = 0; i < renderers.Length; i++)
            {
                Material material = renderers[i].material;
                Color color = Color.Lerp(originalColors[i], new Color(1f, 0.67f, 0.16f), glow * (0.48f + wave * 0.32f));
                material.color = color;
                if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
                if (material.HasProperty("_EmissionColor"))
                    material.SetColor("_EmissionColor", new Color(1f, 0.30f, 0.05f) * glow * 1.8f);
            }
        }

        private static AudioClip CreateTone(float seconds, float frequency)
        {
            int rate = 44100;
            int count = Mathf.CeilToInt(seconds * rate);
            AudioClip clip = AudioClip.Create("古戏台乐器互动音", count, 1, rate, false);
            float[] samples = new float[count];
            for (int i = 0; i < count; i++)
            {
                float t = i / (float)rate;
                float envelope = Mathf.Exp(-3.3f * t);
                samples[i] = (Mathf.Sin(t * frequency * Mathf.PI * 2f) +
                              Mathf.Sin(t * frequency * 2.01f * Mathf.PI * 2f) * 0.28f) * envelope * 0.32f;
            }
            clip.SetData(samples, 0);
            return clip;
        }
    }
}
