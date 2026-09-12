using UnityEngine;
using UnityEngine.Video;

namespace MeixiEcho
{
    public enum ChapterThreeInteractionKind
    {
        TruthClue,
        HousingClue,
        FundingClue,
        TaskStation,
        Meal,
        Bed,
        LightSwitch,
        Television,
        BedsideLamp,
        Wardrobe,
        Clock,
        Departure,
        FinalBook,
        GuestDoor,
        AirConditioner,
        NoticeMedia,
        TeaService
    }

    [DisallowMultipleComponent]
    public sealed class ChapterThreeInteractable : MonoBehaviour
    {
        [SerializeField] private ChapterThreeInteractionKind kind;
        [SerializeField] private int id;
        [SerializeField] private int minimumDay = 1;
        [SerializeField] private string displayName = string.Empty;
        [TextArea(2, 8)] [SerializeField] private string story = string.Empty;
        [SerializeField] private ChapterThreeTaskKind taskKind;
        [SerializeField] private bool toggleState;
        private bool consumed;
        private float fixtureBlend;
        private Transform wardrobeLeft;
        private Transform wardrobeRight;
        private BoxCollider wardrobeInteractionProxy;
        private Renderer[] fixtureRenderers;
        private Light[] fixtureLights;
        private float[] fixtureLightBaseIntensities;
        private bool focused;
        private Transform guestDoorPivot;
        private Quaternion guestDoorClosedRotation;
        private Quaternion guestDoorOpenRotation;
        [SerializeField] private float guestDoorOpenAngle = 84f;
        [SerializeField] private bool slidingDoor;
        [SerializeField] private float slidingDoorTravel = 1.48f;
        private Transform slidingDoorLeaf;
        private Vector3 slidingDoorClosedPosition;
        private Vector3 slidingDoorOpenPosition;
        private BoxCollider slidingDoorCollider;
        private BoxCollider slidingDoorInteractionProxy;
        private Transform airConditionerFlap;
        private Quaternion airConditionerFlapClosedRotation;
        private ParticleSystem[] airConditionerParticles;
        private VideoPlayer televisionPlayer;
        private AudioSource televisionAudioSource;
        private RenderTexture televisionTexture;
        private Transform televisionBootLine;
        private Renderer televisionBootLineRenderer;
        private bool televisionBooting;
        private bool televisionPlaybackRequested;
        private bool televisionEventsBound;
        private float televisionBootProgress;
        private float televisionNextPlaybackRetry;
        private float televisionManualFrameAccumulator;
        private const float TelevisionStartupDuration = .72f;

        private void OnDestroy()
        {
            if (televisionPlayer != null && televisionEventsBound)
            {
                televisionPlayer.prepareCompleted -= OnTelevisionPrepared;
                televisionPlayer.errorReceived -= OnTelevisionError;
            }
            if (televisionTexture == null) return;
            if (televisionTexture.IsCreated()) televisionTexture.Release();
            Destroy(televisionTexture);
            televisionTexture = null;
        }

        public ChapterThreeInteractionKind Kind => kind;
        public int Id => id;
        public int MinimumDay => minimumDay;
        public string DisplayName => displayName;
        public string Story => story;
        public ChapterThreeTaskKind TaskKind => taskKind;
        public bool Consumed => consumed;
        public bool ToggleState => toggleState;

        public float GuestDoorOpenAngle => guestDoorOpenAngle;

        public void SetFocused(bool value)
        {
            if (focused == value) return;
            focused = value;
            CacheFixtureParts();
            // Beds use the mattress/frame itself as the aim target. Do not tint
            // or draw an interaction rectangle over the sleeping surface.
            if (kind == ChapterThreeInteractionKind.Bed) return;
            if (kind == ChapterThreeInteractionKind.Wardrobe ||
                kind == ChapterThreeInteractionKind.Television ||
                kind == ChapterThreeInteractionKind.BedsideLamp ||
                kind == ChapterThreeInteractionKind.GuestDoor)
            {
                ApplyFixtureVisuals(false);
                return;
            }

            Renderer renderer = GetComponent<Renderer>() ?? GetComponentInChildren<Renderer>(true);
            if (renderer == null) return;
            MaterialPropertyBlock block = new();
            renderer.GetPropertyBlock(block);
            Color baseEmission = Color.black;
            Material sharedMaterial = renderer.sharedMaterial;
            if (sharedMaterial != null && sharedMaterial.HasProperty("_EmissionColor"))
                baseEmission = sharedMaterial.GetColor("_EmissionColor");
            Color focusEmission = focused ? new Color(.02f, .62f, .46f, 1f) * .48f : Color.black;
            block.SetColor("_EmissionColor", baseEmission + focusEmission);
            renderer.SetPropertyBlock(block);
        }

        private void Awake()
        {
            CacheFixtureParts();
            fixtureBlend = toggleState ? 1f : 0f;
            ApplyFixtureVisuals(true);
        }

        private void Update()
        {
            if (kind != ChapterThreeInteractionKind.Wardrobe &&
                kind != ChapterThreeInteractionKind.Television &&
                kind != ChapterThreeInteractionKind.BedsideLamp &&
                kind != ChapterThreeInteractionKind.AirConditioner &&
                kind != ChapterThreeInteractionKind.GuestDoor) return;
            float target = toggleState ? 1f : 0f;
            float speed = kind == ChapterThreeInteractionKind.Wardrobe ? 3.4f :
                kind == ChapterThreeInteractionKind.GuestDoor ? 2.8f :
                kind == ChapterThreeInteractionKind.AirConditioner ? 4.2f : 6.5f;
            float next = Mathf.MoveTowards(fixtureBlend, target, Time.unscaledDeltaTime * speed);
            bool fixtureChanged = !Mathf.Approximately(next, fixtureBlend);
            if (fixtureChanged) fixtureBlend = next;
            if (kind == ChapterThreeInteractionKind.Television && televisionBooting)
            {
                televisionBootProgress += Time.unscaledDeltaTime;
                if (televisionBootProgress >= TelevisionStartupDuration)
                {
                    televisionBooting = false;
                    televisionBootProgress = TelevisionStartupDuration;
                    televisionPlaybackRequested = true;
                    StartTelevisionPlayback();
                }
            }
            if (kind == ChapterThreeInteractionKind.Television && toggleState && !televisionBooting &&
                televisionPlaybackRequested && televisionPlayer != null && televisionPlayer.isPrepared &&
                !televisionPlayer.isPlaying)
            {
                if (Time.unscaledTime >= televisionNextPlaybackRetry)
                {
                    // Windows' video backend can occasionally finish Prepare without
                    // honoring the first Play call. Retrying remains the preferred path.
                    televisionNextPlaybackRetry = Time.unscaledTime + .35f;
                    televisionPlayer.Play();
                }

                // Unity 6 / Media Foundation may report the player as prepared yet
                // leave it paused at frame zero on some Windows installations. Frame
                // stepping uses the same decoder and is a dependable visual fallback.
                // The accumulator preserves the clip's native frame rate and keeps
                // interaction/UI animations independent of Time.timeScale.
                float nativeRate = Mathf.Clamp((float)televisionPlayer.frameRate, 1f, 60f);
                televisionManualFrameAccumulator += Time.unscaledDeltaTime * nativeRate;
                int steps = Mathf.Min(3, Mathf.FloorToInt(televisionManualFrameAccumulator));
                if (steps > 0)
                {
                    televisionManualFrameAccumulator -= steps;
                    ulong totalFrames = televisionPlayer.frameCount;
                    if (totalFrames > 1 && televisionPlayer.frame >= (long)totalFrames - 2)
                    {
                        televisionPlayer.Stop();
                        televisionPlayer.Prepare();
                        televisionManualFrameAccumulator = 0f;
                    }
                    else if (televisionPlayer.canStep)
                    {
                        for (int i = 0; i < steps; i++) televisionPlayer.StepForward();
                    }
                }
            }
            else if (kind == ChapterThreeInteractionKind.Television && televisionPlayer != null && televisionPlayer.isPlaying)
                televisionManualFrameAccumulator = 0f;
            if (!fixtureChanged &&
                !(kind == ChapterThreeInteractionKind.Television && televisionBooting)) return;
            ApplyFixtureVisuals(false);
        }

        public void Configure(ChapterThreeInteractionKind interactionKind, int interactionId, string title,
            string body, int availableFromDay = 1, ChapterThreeTaskKind stationTask = ChapterThreeTaskKind.Carry)
        {
            kind = interactionKind;
            id = interactionId;
            displayName = title ?? string.Empty;
            story = body ?? string.Empty;
            minimumDay = Mathf.Clamp(availableFromDay, 1, 9);
            taskKind = stationTask;
            // AddComponent invokes Awake before the authoring/director Configure
            // call, so cache pivots again once the interaction kind is known.
            CacheFixtureParts();
            fixtureBlend = toggleState ? 1f : 0f;
            ApplyFixtureVisuals(true);
        }

        public void ConfigureGuestDoor(float openAngle)
        {
            slidingDoor = false;
            guestDoorOpenAngle = Mathf.Clamp(openAngle, -120f, 120f);
            Configure(ChapterThreeInteractionKind.GuestDoor, 0, "开合房门", "推开或关上客房门");
        }

        public void ConfigureSlidingDoor(float slideDistance)
        {
            // Reuse the same interaction kind and UI text, but animate the leaf
            // along the partition instead of rotating it through the player.
            slidingDoor = true;
            slidingDoorTravel = Mathf.Max(.25f, Mathf.Abs(slideDistance));
            guestDoorOpenAngle = 0f;
            Configure(ChapterThreeInteractionKind.GuestDoor, 0, "推拉卫浴门", "左右推拉打开或关闭卫浴隔间门");
            CacheFixtureParts();
            if (slidingDoorLeaf != null)
            {
                slidingDoorClosedPosition = slidingDoorLeaf.localPosition;
                slidingDoorOpenPosition = slidingDoorClosedPosition + Vector3.right * slidingDoorTravel;
                slidingDoorCollider = slidingDoorLeaf.GetComponentInChildren<BoxCollider>(true);
            }
            slidingDoorInteractionProxy = GetComponent<BoxCollider>();
            if (slidingDoorInteractionProxy != null)
            {
                slidingDoorInteractionProxy.isTrigger = true;
            }
            ApplyFixtureVisuals(true);
        }

        public void SetToggleState(bool value)
        {
            toggleState = value;
            if (kind == ChapterThreeInteractionKind.Television)
            {
                televisionBooting = false;
                televisionBootProgress = value ? TelevisionStartupDuration : 0f;
                CacheTelevisionPlayer();
                if (televisionPlayer != null)
                {
                    televisionPlaybackRequested = value;
                    if (value) StartTelevisionPlayback();
                    else televisionPlayer.Stop();
                }
            }
            fixtureBlend = value ? 1f : 0f;
            if (Application.isPlaying)
            {
                CacheFixtureParts();
                ApplyFixtureVisuals(true);
            }
        }

        public string ToggleFixture()
        {
            if (kind != ChapterThreeInteractionKind.Television &&
                kind != ChapterThreeInteractionKind.BedsideLamp &&
                kind != ChapterThreeInteractionKind.Wardrobe &&
                kind != ChapterThreeInteractionKind.GuestDoor &&
                kind != ChapterThreeInteractionKind.AirConditioner) return string.Empty;
            toggleState = !toggleState;
            CacheFixtureParts();
            CacheTelevisionPlayer();
            if (televisionPlayer != null)
            {
                if (toggleState)
                {
                    // Hold the first frame until the scan-line boot animation has
                    // completed, avoiding an abrupt blue flash or a black bezel
                    // covering the display while the clip is preparing.
                    televisionBooting = true;
                    televisionBootProgress = 0f;
                    televisionManualFrameAccumulator = 0f;
                    televisionPlaybackRequested = true;
                    televisionPlayer.Stop();
                    ClearTelevisionTexture();
                    // Decode while the boot scan line is running so the first
                    // frame appears as soon as the animation completes.
                    televisionPlayer.Prepare();
                }
                else
                {
                    televisionBooting = false;
                    televisionBootProgress = 0f;
                    televisionManualFrameAccumulator = 0f;
                    televisionPlaybackRequested = false;
                    televisionPlayer.Stop();
                    ClearTelevisionTexture();
                }
            }
            // Leave the blend at its current value so Update can animate the
            // transition instead of snapping the fixture to its final state.
            ApplyFixtureVisuals(false);
            return kind switch
            {
                ChapterThreeInteractionKind.Television => toggleState ? "电视正在开机 · 正在准备实践影像" : "电视已关闭",
                ChapterThreeInteractionKind.BedsideLamp => toggleState ? "床头灯已打开" : "床头灯已关闭",
                ChapterThreeInteractionKind.GuestDoor => toggleState ? "房门已打开" : "房门已关闭",
                ChapterThreeInteractionKind.AirConditioner => toggleState ? "空调已开启 · 静音送风" : "空调已关闭",
                _ => toggleState ? "衣柜已打开" : "衣柜已关闭"
            };
        }

        public void SetConsumed(bool value)
        {
            consumed = value;
            Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++) renderers[i].enabled = !value;
            Collider[] colliders = GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < colliders.Length; i++) colliders[i].enabled = !value;
        }

        public bool CanUse(ChapterThreeGameDirector director)
        {
            return !consumed && director != null && director.CurrentDay >= minimumDay;
        }

        public void Interact(ChapterThreeGameDirector director)
        {
            if (CanUse(director)) director.HandleInteraction(this);
        }

        private void CacheFixtureParts()
        {
            // Authored fixtures add their visual/light children immediately after
            // this component, so a one-time Awake cache can be empty. Refresh the
            // small arrays whenever the fixture is configured or toggled.
            fixtureRenderers = GetComponentsInChildren<Renderer>(true);
            Light[] discoveredLights = GetComponentsInChildren<Light>(true);
            bool lightSetChanged = fixtureLights == null || fixtureLights.Length != discoveredLights.Length;
            fixtureLights = discoveredLights;
            if (lightSetChanged)
            {
                fixtureLightBaseIntensities = new float[fixtureLights.Length];
                for (int i = 0; i < fixtureLights.Length; i++)
                    fixtureLightBaseIntensities[i] = fixtureLights[i] != null ? fixtureLights[i].intensity : 0f;
            }
            if (kind == ChapterThreeInteractionKind.GuestDoor)
            {
                if (guestDoorPivot == null)
                {
                    guestDoorPivot = transform;
                    guestDoorClosedRotation = Quaternion.identity;
                    guestDoorOpenRotation = Quaternion.Euler(0f, guestDoorOpenAngle, 0f);
                    transform.localRotation = guestDoorClosedRotation;
                }
                slidingDoorLeaf = transform.Find("BathroomDoorLeaf");
                slidingDoorCollider = slidingDoorLeaf != null ? slidingDoorLeaf.GetComponentInChildren<BoxCollider>(true) : null;
                slidingDoorInteractionProxy = GetComponent<BoxCollider>();
                if (slidingDoor && slidingDoorLeaf != null && slidingDoorOpenPosition == Vector3.zero)
                {
                    slidingDoorClosedPosition = slidingDoorLeaf.localPosition;
                    slidingDoorOpenPosition = slidingDoorClosedPosition + Vector3.right * slidingDoorTravel;
                }
                return;
            }
            CacheTelevisionPlayer();
            if (kind == ChapterThreeInteractionKind.AirConditioner)
            {
                if (airConditionerFlap == null)
                {
                    Transform[] parts = GetComponentsInChildren<Transform>(true);
                    for (int i = 0; i < parts.Length; i++)
                    {
                        if (parts[i].name != "AirConditionerFlap") continue;
                        airConditionerFlap = parts[i];
                        airConditionerFlapClosedRotation = airConditionerFlap.localRotation;
                        break;
                    }
                }
                airConditionerParticles = GetComponentsInChildren<ParticleSystem>(true);
                return;
            }
            if (kind != ChapterThreeInteractionKind.Wardrobe || wardrobeLeft != null) return;
            wardrobeInteractionProxy = GetComponent<BoxCollider>();
            Transform[] children = GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < children.Length; i++)
            {
                if (children[i].name == "WardrobeDoorLeftPivot") wardrobeLeft = children[i];
                else if (children[i].name == "WardrobeDoorRightPivot") wardrobeRight = children[i];
            }
        }

        private void CacheTelevisionPlayer()
        {
            if (kind != ChapterThreeInteractionKind.Television) return;
            if (televisionBootLine == null)
            {
                televisionBootLine = transform.Find("TelevisionBootLine");
                televisionBootLineRenderer = televisionBootLine != null ? televisionBootLine.GetComponent<Renderer>() : null;
                if (televisionBootLine != null) televisionBootLine.gameObject.SetActive(televisionBooting);
            }
            if (televisionPlayer != null) return;
            televisionPlayer = GetComponent<VideoPlayer>() ?? GetComponentInChildren<VideoPlayer>(true);
            if (televisionPlayer == null) return;
            if (televisionPlayer.clip == null)
                televisionPlayer.clip = Resources.Load<VideoClip>("ChapterThree/TVPractice");
            televisionPlayer.playOnAwake = false;
            televisionPlayer.isLooping = true;
            televisionPlayer.waitForFirstFrame = false;
            televisionPlayer.aspectRatio = VideoAspectRatio.FitInside;
            televisionPlayer.targetCameraAlpha = 1f;
            televisionPlayer.skipOnDrop = true;
            // Use an AudioSource target instead of VideoAudioOutputMode.None so
            // the embedded soundtrack follows the same on/off/loop state as the
            // picture. This runtime setup also repairs older baked scene objects.
            televisionAudioSource = GetComponent<AudioSource>() ?? gameObject.AddComponent<AudioSource>();
            televisionAudioSource.playOnAwake = false;
            televisionAudioSource.loop = true;
            televisionAudioSource.volume = .78f;
            televisionAudioSource.spatialBlend = .72f;
            televisionAudioSource.minDistance = 1.4f;
            televisionAudioSource.maxDistance = 12f;
            televisionAudioSource.rolloffMode = AudioRolloffMode.Logarithmic;
            televisionPlayer.audioOutputMode = VideoAudioOutputMode.AudioSource;
            televisionPlayer.controlledAudioTrackCount = 1;
            televisionPlayer.EnableAudioTrack(0, true);
            televisionPlayer.SetTargetAudioSource(0, televisionAudioSource);
            // MaterialOverride is unreliable with URP's Lit property blocks on
            // some standalone GPUs. A dedicated 16:9 render texture keeps the
            // video on the screen in both Editor and the Windows build.
            if (televisionTexture == null)
            {
                televisionTexture = new RenderTexture(1280, 720, 0, RenderTextureFormat.ARGB32)
                {
                    name = "TelevisionRenderTexture"
                };
                televisionTexture.Create();
            }
            televisionPlayer.renderMode = VideoRenderMode.RenderTexture;
            televisionPlayer.targetTexture = televisionTexture;
            televisionPlayer.prepareCompleted += OnTelevisionPrepared;
            televisionPlayer.errorReceived += OnTelevisionError;
            televisionEventsBound = true;
            Renderer screenRenderer = transform.Find("TelevisionGlass")?.GetComponent<Renderer>();
            if (screenRenderer != null)
            {
                MaterialPropertyBlock screenBlock = new();
                screenRenderer.GetPropertyBlock(screenBlock);
                screenBlock.SetTexture("_BaseMap", televisionTexture);
                screenBlock.SetTexture("_MainTex", televisionTexture);
                screenRenderer.SetPropertyBlock(screenBlock);
            }
        }

        private void StartTelevisionPlayback()
        {
            if (televisionPlayer == null || !toggleState || televisionBooting || !televisionPlaybackRequested) return;
            if (televisionPlayer.isPrepared)
            {
                televisionNextPlaybackRetry = Time.unscaledTime + .35f;
                televisionPlayer.Play();
            }
            else televisionPlayer.Prepare();
        }

        private void OnTelevisionPrepared(VideoPlayer source)
        {
            if (source == null || source != televisionPlayer) return;
            StartTelevisionPlayback();
        }

        private void OnTelevisionError(VideoPlayer source, string message)
        {
            Debug.LogWarning($"[ChapterThree] TV video error on {name}: {message}");
        }

        private void ClearTelevisionTexture()
        {
            if (televisionTexture == null || !televisionTexture.IsCreated()) return;
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = televisionTexture;
            GL.Clear(true, true, new Color(.004f, .006f, .008f, 1f));
            RenderTexture.active = previous;
        }

        private void ApplyFixtureVisuals(bool immediate)
        {
            if (kind == ChapterThreeInteractionKind.Wardrobe)
            {
                if (immediate) fixtureBlend = toggleState ? 1f : 0f;
                float eased = Mathf.SmoothStep(0f, 1f, fixtureBlend);
                // The carcass now faces the bed; both leaves swing into the clear
                // bedroom aisle without changing their authored hinge sides.
                if (wardrobeLeft != null) wardrobeLeft.localRotation = Quaternion.Euler(0f, 74f * eased, 0f);
                if (wardrobeRight != null) wardrobeRight.localRotation = Quaternion.Euler(0f, -74f * eased, 0f);
                // This is a trigger-only interaction volume. It must stay enabled
                // after opening or the player has no target with which to close it.
                if (wardrobeInteractionProxy != null)
                {
                    wardrobeInteractionProxy.isTrigger = true;
                    wardrobeInteractionProxy.enabled = true;
                    // Closed: the whole front is easy to aim at. Open: keep a
                    // generous close target on the left jamb while clearing the
                    // centre sightline so evidence inside wins the ray test.
                    if (eased < .45f)
                    {
                        wardrobeInteractionProxy.center = new Vector3(0f, 1.28f, -.47f);
                        wardrobeInteractionProxy.size = new Vector3(1.42f, 2.52f, .20f);
                    }
                    else
                    {
                        wardrobeInteractionProxy.center = new Vector3(-.60f, 1.28f, -.43f);
                        wardrobeInteractionProxy.size = new Vector3(.52f, 2.52f, .44f);
                    }
                }
                ApplyWardrobeFocus();
                return;
            }

            if (kind == ChapterThreeInteractionKind.GuestDoor)
            {
                if (guestDoorPivot == null) guestDoorPivot = transform;
                float eased = Mathf.SmoothStep(0f, 1f, fixtureBlend);
                if (slidingDoor && slidingDoorLeaf != null)
                {
                    slidingDoorLeaf.localPosition = Vector3.Lerp(slidingDoorClosedPosition, slidingDoorOpenPosition, eased);
                    // Collision rides with the leaf into its wall pocket. It must
                    // not remain as an invisible blocker in the open doorway.
                    if (slidingDoorCollider != null) slidingDoorCollider.enabled = true;
                }
                else
                    guestDoorPivot.localRotation = Quaternion.Slerp(guestDoorClosedRotation, guestDoorOpenRotation, eased);
                return;
            }

            if (fixtureLights != null)
                for (int i = 0; i < fixtureLights.Length; i++)
                {
                    Light light = fixtureLights[i];
                    if (light == null) continue;
                    float eased = Mathf.SmoothStep(0f, 1f, fixtureBlend);
                    light.enabled = eased > .001f || toggleState;
                    if (fixtureLightBaseIntensities != null && i < fixtureLightBaseIntensities.Length)
                        light.intensity = fixtureLightBaseIntensities[i] * eased;
                }
            if (kind == ChapterThreeInteractionKind.AirConditioner)
            {
                float eased = Mathf.SmoothStep(0f, 1f, fixtureBlend);
                if (airConditionerFlap != null)
                    airConditionerFlap.localRotation = airConditionerFlapClosedRotation * Quaternion.Euler(22f * eased, 0f, 0f);
                if (airConditionerParticles != null)
                {
                    for (int i = 0; i < airConditionerParticles.Length; i++)
                    {
                        ParticleSystem particles = airConditionerParticles[i];
                        if (particles == null) continue;
                        if (toggleState && eased > .15f)
                        {
                            if (!particles.isPlaying) particles.Play(true);
                        }
                        else if (particles.isPlaying || particles.particleCount > 0)
                            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                    }
                }
            }
            if (fixtureRenderers == null) return;
            for (int i = 0; i < fixtureRenderers.Length; i++)
            {
                Renderer renderer = fixtureRenderers[i];
                bool televisionScreen = kind == ChapterThreeInteractionKind.Television && renderer.name == "TelevisionGlass";
                bool lampGlow = kind == ChapterThreeInteractionKind.BedsideLamp && renderer.name == "BedsideLampGlow";
                bool acDisplay = kind == ChapterThreeInteractionKind.AirConditioner && renderer.name == "AcStatusDisplay";
                if (!televisionScreen && !lampGlow && !acDisplay) continue;
                MaterialPropertyBlock block = new();
                renderer.GetPropertyBlock(block);
                float eased = Mathf.SmoothStep(0f, 1f, fixtureBlend);
                Color offColor = televisionScreen
                    ? new Color(.008f, .01f, .012f, 1f)
                    : acDisplay ? new Color(.015f, .06f, .07f, 1f) : new Color(.16f, .11f, .07f, 1f);
                Color onColor = televisionScreen
                    ? Color.white
                    : acDisplay ? new Color(.16f, .82f, .96f, 1f) : new Color(1f, .62f, .27f, 1f);
                Color color = Color.Lerp(offColor, onColor, eased);
                if (televisionScreen)
                {
                    float boot = televisionBooting
                        ? Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(televisionBootProgress / TelevisionStartupDuration))
                        : (toggleState ? 1f : 0f);
                    float pulse = televisionBooting
                        ? (Mathf.Sin(televisionBootProgress * 42f) * .5f + .5f) * .18f
                        : 0f;
                    color = Color.Lerp(offColor, onColor, eased * boot);
                    color += new Color(.08f, .20f, .24f, 1f) * pulse;
                }
                block.SetColor("_BaseColor", color);
                block.SetColor("_Color", color);
                Color focusEmission = focused ? new Color(.02f, .62f, .46f, 1f) * .34f : Color.black;
                // The screen should read as a contained panel, not a room-sized
                // blue light source. Keep the emissive contribution subtle and
                // let the actual display geometry carry the color.
                block.SetColor("_EmissionColor", onColor * (televisionScreen ? .72f : 1.6f) * eased + focusEmission);
                renderer.SetPropertyBlock(block);
            }

            if (kind == ChapterThreeInteractionKind.Television && televisionBootLine != null)
            {
                bool visible = televisionBooting;
                televisionBootLine.gameObject.SetActive(visible);
                if (visible)
                {
                    float progress = Mathf.Clamp01(televisionBootProgress / TelevisionStartupDuration);
                    float scan = Mathf.SmoothStep(0f, 1f, progress);
                    televisionBootLine.localPosition = new Vector3(-.64f, Mathf.Lerp(-.24f, .24f, scan), 0f);
                    float widthPulse = .72f + (Mathf.Sin(televisionBootProgress * 34f) * .5f + .5f) * .28f;
                    televisionBootLine.localScale = new Vector3(.012f / .14f, (.018f / .98f) * widthPulse, 1.08f / 1.56f);
                    if (televisionBootLineRenderer != null)
                    {
                        MaterialPropertyBlock lineBlock = new();
                        televisionBootLineRenderer.GetPropertyBlock(lineBlock);
                        Color glow = Color.Lerp(new Color(.12f, .50f, .60f, 1f), new Color(.64f, .96f, 1f, 1f), progress);
                        lineBlock.SetColor("_BaseColor", glow);
                        lineBlock.SetColor("_Color", glow);
                        lineBlock.SetColor("_EmissionColor", glow * (1.2f + progress * 2.2f));
                        televisionBootLineRenderer.SetPropertyBlock(lineBlock);
                    }
                }
            }

            if (focused || kind == ChapterThreeInteractionKind.Wardrobe) ApplyWardrobeFocus();
        }

        private void ApplyWardrobeFocus()
        {
            if (fixtureRenderers == null) return;
            for (int i = 0; i < fixtureRenderers.Length; i++)
            {
                Renderer renderer = fixtureRenderers[i];
                if (renderer == null || (!renderer.name.Contains("WardrobeHandle") && !renderer.name.Contains("WardrobeDoor"))) continue;
                MaterialPropertyBlock block = new();
                renderer.GetPropertyBlock(block);
                Color emission = focused ? new Color(.02f, .62f, .46f, 1f) * .34f : Color.black;
                block.SetColor("_EmissionColor", emission);
                renderer.SetPropertyBlock(block);
            }
        }
    }
}
