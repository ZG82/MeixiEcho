using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
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
        private Font displayFont;
        private Text hint;
        private Text objective;
        private GameObject theatreQuestCard;
        private GameObject theatreHintCard;
        private GameObject introPanel;
        private GameObject endingPanel;
        private GameObject stagePausePanel;
        private GameObject stageSettingsPanel;
        private GameObject stageTerminalPanel;
        private bool stagePaused;
        private bool stageSettingsOpen;
        private Button stageResumeButton;
        private Button stageSettingsButton;
        private Button stageReturnButton;
        private Button stageQuitButton;
        private Button stageSettingsBackButton;
        private Button stageVolumeDownButton;
        private Button stageVolumeUpButton;
        private Button stageTerminalButton;
        private Button stageTerminalSubmitButton;
        private Button stageTerminalCloseButton;
        private InputField stageTerminalInput;
        private Text stageTerminalOutput;
        private Text stageVolumeText;
        private Text stageAchievementText;
        private bool stageTerminalExecuting;
        private Text endingPageTitle;
        private Text endingBody;
        private Text endingPageNumber;
        private Text endingNavigation;
        private CanvasGroup sceneFadeGroup;
        private AudioSource chapterMusicSource;
        private Coroutine chapterMusicFadeRoutine;
        private AudioSource endingMusicSource;
        private GameObject endingBook;
        private Vector3 endingBookBasePosition;
        private bool sceneTransitionActive;
        private bool endingShown;
        private bool chapterMusicDucked;
        private float verticalVelocity;
        private float yaw;
        private float pitch = 19f;
        private float cameraDistance = 5.45f;
        private float introUntil;
        private bool culturalPreludeActive;
        private GameObject culturalPreludeRoot;
        private CanvasGroup culturalPreludeGroup;
        private Image culturalPreludeImage;
        private Text culturalPreludeCaption;
        private Text culturalPreludeCounter;
        private Sprite[] culturalPreludeSprites = Array.Empty<Sprite>();
        private Renderer[] culturalPreludePlayerRenderers = Array.Empty<Renderer>();
        private bool[] culturalPreludeRendererStates = Array.Empty<bool>();
        private float culturalPreludeCameraFov;
        private int culturalPreludeCameraMode;
        private float culturalSeatedPoseWeight;
        private float culturalStandingY;
        private NaturalHumanoidLocomotionPolish culturalLocomotionPolish;
        private GameObject culturalQuizRoot;
        private CanvasGroup culturalQuizGroup;
        private Text culturalQuizProgress;
        private Text culturalQuizQuestion;
        private Text culturalQuizFeedback;
        private readonly Button[] culturalQuizAnswerButtons = new Button[3];
        private int culturalQuizQuestionIndex;
        private bool culturalQuizPassed;
        private readonly string[] culturalQuizQuestions =
        {
            "闽剧又称什么？",
            "闽剧主要使用哪一种语言演唱和念白？",
            "闽剧在哪一年列入第一批国家级非物质文化遗产名录？",
            "短片中，舞台动作的节奏主要由什么共同支撑？"
        };
        private readonly string[][] culturalQuizAnswers =
        {
            new[] { "福州戏", "梨园戏", "高甲戏" },
            new[] { "普通话", "福州方言", "闽南方言" },
            new[] { "2020 年", "2012 年", "2006 年" },
            new[] { "身段与锣鼓管弦", "灯光与布景", "观众掌声" }
        };
        private readonly int[] culturalQuizCorrectAnswers = { 0, 1, 2, 0 };
        private float interactionGuideUntil;
        private float endingInputReadyAt;
        private int endingPageIndex;
        private string[] endingPageTitles = Array.Empty<string>();
        private string[] endingPageBodies = Array.Empty<string>();
        private float lookBlockedUntil;
        private Vector3 horizontalVelocity;
        private Vector3 horizontalSmoothVelocity;
        private Vector3 cameraVelocity;
        private Vector2 smoothedLook;
        private float movementBlockedUntil;
        private readonly RaycastHit[] cameraHits = new RaycastHit[24];
        private readonly List<TheatreInstrument> nearbyInstruments = new();
        private readonly HashSet<TheatreInstrument> playedInstruments = new();
        private static readonly Dictionary<string, Sprite> UiSprites = new(StringComparer.Ordinal);
        private ChapterTwoAdventureRuntime chapterTwoAdventure;
        private MeixiThirdPersonPlayer sharedPlayerController;
        private ThirdPersonOrbitCamera sharedGameplayCamera;

        public bool AllowsSharedPlayerMovement => enabled && !sceneTransitionActive && !culturalPreludeActive &&
                                                  !stagePaused && !stageSettingsOpen && !endingShown &&
                                                  Time.unscaledTime >= introUntil &&
                                                  (chapterTwoAdventure == null ||
                                                   (!chapterTwoAdventure.IsInputModalOpen &&
                                                    !chapterTwoAdventure.IsFirstPersonActive));

        private void Awake()
        {
            MeixiLocalProgress.Initialize();
            MeixiLocalProgress.ApplyAudioSetting();
            // The saved scene contains an EditorOnly visual preview so artists can
            // inspect chapter two without entering Play Mode. Disable that copy
            // before creating the lightweight runtime set to prevent duplicates.
            GameObject editorPreview = GameObject.Find("MeixiTheatre_EditorPreview");
            if (editorPreview != null) editorPreview.SetActive(false);
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
            EnsurePlayerFillLight();
            animator = player.GetComponentInChildren<Animator>(true);
            ConfigureSharedGameplayControls();
            yaw = player.eulerAngles.y;
            verticalVelocity = -1.8f;
            horizontalVelocity = Vector3.zero;
            horizontalSmoothVelocity = Vector3.zero;
            cameraVelocity = Vector3.zero;
            smoothedLook = Vector2.zero;
            lookBlockedUntil = Time.unscaledTime + 0.40f;
            introUntil = Time.unscaledTime + 3.55f;
            displayFont = Font.CreateDynamicFontFromOSFont(
                new[] { "STKaiti", "华文楷体", "KaiTi", "楷体", "FZKai-Z03", "SimSun", "Microsoft YaHei" }, 72);
            PrepareChapterMusic();
            PrepareEndingMusic();
            CreateHud();
            BuildCulturalPrelude();
            chapterTwoAdventure = gameObject.GetComponent<ChapterTwoAdventureRuntime>();
            if (chapterTwoAdventure == null) chapterTwoAdventure = gameObject.AddComponent<ChapterTwoAdventureRuntime>();
            BuildSceneFadeOverlay();
            MeixiCursorState.SetGameplay();
            PositionCameraImmediately();
            if (!ShouldSkipCulturalPrelude()) StartCoroutine(PlayCulturalPrelude());
            else SetSharedGameplayControlsActive(true);
            TryStartCaptureMode();
            TryStartSharedMovementValidation();
            StartCoroutine(LogSettledSceneStats());
            Debug.Log("[MeixiTheatre] Runtime reset; third-person stage controller ready.");
        }

        private void TryStartSharedMovementValidation()
        {
            if (!Array.Exists(Environment.GetCommandLineArgs(), value =>
                    string.Equals(value, "-meixiTheatreMovementValidation", StringComparison.OrdinalIgnoreCase))) return;
            StartCoroutine(ValidateSharedMovement());
        }

        private IEnumerator ValidateSharedMovement()
        {
            yield return new WaitForSecondsRealtime(1.0f);
            introUntil = Time.unscaledTime;
            if (introPanel != null) introPanel.SetActive(false);
            yield return new WaitForSecondsRealtime(.85f);
            if (sharedPlayerController == null || stageCamera == null)
            {
                Debug.LogError("[MeixiTheatreMovement] Shared chapter-one controller or camera missing.");
                if (!Application.isEditor) Application.Quit(2);
                yield break;
            }

            Vector3 start = player.position;
            sharedPlayerController.InjectValidationMove(Vector2.left, .08f);
            yield return new WaitForSecondsRealtime(.38f);
            Vector3 afterLeft = player.position;
            sharedPlayerController.InjectValidationMove(Vector2.right, .08f);
            yield return new WaitForSecondsRealtime(.38f);
            Vector3 afterRight = player.position;
            yield return new WaitForSecondsRealtime(.28f);
            Vector3 viewport = stageCamera.WorldToViewportPoint(player.position + Vector3.up * 1.75f);
            float leftTravel = Vector3.ProjectOnPlane(afterLeft - start, Vector3.up).magnitude;
            float rightTravel = Vector3.ProjectOnPlane(afterRight - afterLeft, Vector3.up).magnitude;
            bool centered = viewport.z > 0f && Mathf.Abs(viewport.x - .5f) < .055f && Mathf.Abs(viewport.y - .5f) < .055f;
            bool stable = leftTravel < .85f && rightTravel < .85f &&
                          Vector3.ProjectOnPlane(afterRight - start, Vector3.up).magnitude < .85f;
            string message = $"[MeixiTheatreMovement] left={leftTravel:F3} right={rightTravel:F3} " +
                             $"net={Vector3.ProjectOnPlane(afterRight - start, Vector3.up).magnitude:F3} " +
                             $"viewport=({viewport.x:F3},{viewport.y:F3},{viewport.z:F3})";
            if (stable && centered) Debug.Log(message + " PASS");
            else Debug.LogError(message + " FAIL");
            yield return new WaitForSecondsRealtime(.35f);
            if (!Application.isEditor) Application.Quit(stable && centered ? 0 : 3);
        }

        private void ConfigureSharedGameplayControls()
        {
            sharedGameplayCamera = stageCamera.GetComponent<ThirdPersonOrbitCamera>();
            if (sharedGameplayCamera == null) sharedGameplayCamera = stageCamera.gameObject.AddComponent<ThirdPersonOrbitCamera>();
            sharedGameplayCamera.Configure(player);
            sharedGameplayCamera.ConfigureInteriorMode(true);
            sharedGameplayCamera.enabled = false;

            sharedPlayerController = player.GetComponent<MeixiThirdPersonPlayer>();
            if (sharedPlayerController == null) sharedPlayerController = player.gameObject.AddComponent<MeixiThirdPersonPlayer>();
            Transform visual = player.Find("林默_舞台模型");
            if (visual == null && animator != null) visual = animator.transform;
            sharedPlayerController.ConfigureTheatre(this, sharedGameplayCamera, visual);
            sharedPlayerController.enabled = false;

            NaturalHumanoidLocomotionPolish polish = player.GetComponentInChildren<NaturalHumanoidLocomotionPolish>(true);
            if (polish != null)
            {
                polish.enabled = false;
                polish.ConfigurePlayerCamera(stageCamera);
                polish.enabled = true;
            }
        }

        public void SetSharedGameplayControlsActive(bool active)
        {
            if (sharedPlayerController != null)
            {
                if (active) sharedPlayerController.ResetForNewSession();
                sharedPlayerController.enabled = active;
            }
            if (sharedGameplayCamera != null)
            {
                sharedGameplayCamera.Configure(player);
                sharedGameplayCamera.ConfigureInteriorMode(true);
                if (active)
                {
                    sharedGameplayCamera.ResetGameplayView();
                    sharedGameplayCamera.ClearInputAfterFocus();
                }
                sharedGameplayCamera.enabled = active;
            }
        }

        private void BuildSceneFadeOverlay()
        {
            GameObject fadeRoot = new("戏台场景渐入");
            Canvas fadeCanvas = fadeRoot.AddComponent<Canvas>();
            fadeCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            fadeCanvas.sortingOrder = 900;
            CanvasScaler scaler = fadeRoot.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = .5f;
            sceneFadeGroup = fadeRoot.AddComponent<CanvasGroup>();
            sceneFadeGroup.alpha = 1f;
            sceneFadeGroup.blocksRaycasts = true;

            GameObject imageObject = new("戏台黑幕");
            imageObject.transform.SetParent(fadeRoot.transform, false);
            Image image = imageObject.AddComponent<Image>();
            image.color = new Color(.003f, .008f, .012f, 1f);
            image.raycastTarget = true;
            RectTransform rect = image.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            StartCoroutine(FadeStageIn());
        }

        private IEnumerator FadeStageIn()
        {
            sceneTransitionActive = true;
            float elapsed = 0f;
            const float duration = .72f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                sceneFadeGroup.alpha = 1f - t * t * (3f - 2f * t);
                yield return null;
            }
            sceneFadeGroup.alpha = 0f;
            sceneFadeGroup.blocksRaycasts = false;
            sceneTransitionActive = false;
        }

        private IEnumerator ReturnToVillageRoutine()
        {
            sceneTransitionActive = true;
            sceneFadeGroup.blocksRaycasts = true;
            float elapsed = 0f;
            const float duration = .48f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                sceneFadeGroup.alpha = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
                yield return null;
            }
            Time.timeScale = 1f;
            SceneManager.LoadScene("MeixiEditable");
        }

        private IEnumerator LogSettledSceneStats()
        {
            yield return null;
            yield return null;
            Renderer[] playerRenderers = player.GetComponentsInChildren<Renderer>(true);
            Bounds playerBounds = default;
            bool hasBounds = false;
            foreach (Renderer renderer in playerRenderers)
            {
                if (!renderer.enabled) continue;
                if (!hasBounds) { playerBounds = renderer.bounds; hasBounds = true; }
                else playerBounds.Encapsulate(renderer.bounds);
            }
            Vector3 screen = stageCamera.WorldToScreenPoint(player.position + Vector3.up);
            Debug.Log($"[MeixiTheatre] Settled scene renderers={FindObjectsByType<Renderer>().Length} " +
                      $"colliders={FindObjectsByType<Collider>().Length} player={player.position} " +
                      $"playerRenderers={playerRenderers.Length} bounds={(hasBounds ? playerBounds.ToString() : "none")} " +
                      $"camera={stageCamera.transform.position} playerScreen={screen}");
        }

        private void EnsurePlayerFillLight()
        {
            if (player.Find("林默_柔光") != null) return;
            GameObject lightObject = new GameObject("林默_柔光");
            lightObject.transform.SetParent(player, false);
            lightObject.transform.localPosition = new Vector3(0f, 2.7f, -.8f);
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(.72f, .84f, 1f);
            light.range = 5.8f;
            light.intensity = 1.25f;
            light.shadows = LightShadows.None;
        }

        private void EnsurePlayerVisual()
        {
            Transform synchronizedVisual = player.Find("林默_舞台模型");
            if (synchronizedVisual != null)
            {
                NaturalHumanoidLocomotionPolish existingPolish = synchronizedVisual.GetComponent<NaturalHumanoidLocomotionPolish>();
                if (existingPolish == null) existingPolish = synchronizedVisual.gameObject.AddComponent<NaturalHumanoidLocomotionPolish>();
                existingPolish.ConfigurePlayerCamera(stageCamera);
                existingPolish.enabled = true;
                return;
            }

            Transform placeholder = player.Find("舞台角色加载占位");
            if (placeholder != null) placeholder.gameObject.SetActive(false);
            GameObject synchronizedSource = MeixiRealisticCast.Load(MeixiCastRole.LinMo);
            GameObject source = synchronizedSource ??
                                Resources.Load<GameObject>("Models/Characters/RealisticMale/Models/DefaultMale") ??
                                Resources.Load<GameObject>("Models/Characters/LinMo");
            if (source == null) return;
            GameObject visual = Instantiate(source, player);
            visual.name = "林默_舞台模型";
            visual.SetActive(true);
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.identity;
            Renderer[] renderers = visual.GetComponentsInChildren<Renderer>(true);
            if (synchronizedSource != null) MeixiRealisticCast.PrepareVisual(visual, MeixiCastRole.LinMo, true);
            Material realisticMaterial = synchronizedSource == null ? CreateRealisticCharacterMaterial() : null;
            foreach (Renderer renderer in renderers)
            {
                renderer.gameObject.SetActive(true);
                renderer.enabled = true;
                renderer.forceRenderingOff = false;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                renderer.receiveShadows = true;
                if (realisticMaterial != null)
                {
                    Material[] slots = renderer.sharedMaterials;
                    if (slots.Length == 0) renderer.sharedMaterial = realisticMaterial;
                    else
                    {
                        for (int i = 0; i < slots.Length; i++) slots[i] = realisticMaterial;
                        renderer.sharedMaterials = slots;
                    }
                }
                if (renderer is SkinnedMeshRenderer skinned) skinned.updateWhenOffscreen = true;
            }
            if (renderers.Length > 0)
            {
                Bounds bounds = renderers[0].bounds;
                for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
                float scale = 1.90f / Mathf.Max(0.01f, bounds.size.y);
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

            foreach (Animator candidate in player.GetComponentsInChildren<Animator>(true))
                if (candidate != null && candidate != visualAnimator) candidate.gameObject.SetActive(false);

            NaturalHumanoidLocomotionPolish polish = visual.GetComponent<NaturalHumanoidLocomotionPolish>();
            if (polish == null) polish = visual.AddComponent<NaturalHumanoidLocomotionPolish>();
            polish.ConfigurePlayerCamera(stageCamera);
            polish.enabled = true;
        }

        private static Material CreateRealisticCharacterMaterial()
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            if (shader == null) return null;
            const string root = "Models/Characters/RealisticMale/Textures/";
            Texture2D albedo = Resources.Load<Texture2D>(root + "DefaultMale_Albedo");
            Texture2D normal = Resources.Load<Texture2D>(root + "DefaultMale_Normal");
            Texture2D occlusion = Resources.Load<Texture2D>(root + "DefaultMale_Occlusion");
            Texture2D specular = Resources.Load<Texture2D>(root + "DefaultMale_SpecSmooth");
            Material material = new Material(shader) { name = "林默_第二章写实材质", color = Color.white };
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", Color.white);
            if (albedo != null)
            {
                material.mainTexture = albedo;
                if (material.HasProperty("_BaseMap")) material.SetTexture("_BaseMap", albedo);
            }
            if (normal != null && material.HasProperty("_BumpMap"))
            {
                material.SetTexture("_BumpMap", normal);
                material.SetFloat("_BumpScale", .62f);
                material.EnableKeyword("_NORMALMAP");
            }
            if (occlusion != null && material.HasProperty("_OcclusionMap"))
            {
                material.SetTexture("_OcclusionMap", occlusion);
                material.SetFloat("_OcclusionStrength", .78f);
            }
            if (specular != null && material.HasProperty("_SpecGlossMap")) material.SetTexture("_SpecGlossMap", specular);
            if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", 0f);
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", .26f);
            return material;
        }

        private void Update()
        {
            if (player == null || controller == null) return;
            if (sceneTransitionActive || culturalPreludeActive) return;
            Keyboard keyboard = Keyboard.current;
            Mouse mouse = Mouse.current;
            if (chapterTwoAdventure != null && chapterTwoAdventure.IsInputModalOpen) return;
            if (stageTerminalPanel != null && stageTerminalPanel.activeSelf)
            {
                if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame) CloseStageTerminal();
                else if (keyboard != null && (keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame))
                    SubmitStageTerminalCommand();
                else if (mouse != null && mouse.leftButton.wasPressedThisFrame)
                {
                    Vector2 pointer = mouse.position.ReadValue();
                    if (IsPointerInside(stageTerminalSubmitButton, pointer)) SubmitStageTerminalCommand();
                    else if (IsPointerInside(stageTerminalCloseButton, pointer)) CloseStageTerminal();
                }
                return;
            }
            if (stageSettingsOpen)
            {
                if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame) CloseStageSettings();
                else if (mouse != null && mouse.leftButton.wasPressedThisFrame)
                {
                    Vector2 pointer = mouse.position.ReadValue();
                    if (IsPointerInside(stageSettingsBackButton, pointer)) CloseStageSettings();
                    else if (IsPointerInside(stageVolumeDownButton, pointer)) AdjustStageVolume(-.10f);
                    else if (IsPointerInside(stageVolumeUpButton, pointer)) AdjustStageVolume(.10f);
                    else if (IsPointerInside(stageTerminalButton, pointer)) OpenStageTerminal();
                }
                return;
            }
            if (stagePaused)
            {
                if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame) SetStagePaused(false);
                else if (mouse != null && mouse.leftButton.wasPressedThisFrame)
                {
                    Vector2 pointer = mouse.position.ReadValue();
                    if (IsPointerInside(stageResumeButton, pointer)) SetStagePaused(false);
                    else if (IsPointerInside(stageSettingsButton, pointer)) OpenStageSettings();
                    else if (IsPointerInside(stageReturnButton, pointer)) ReturnToVillageFromPause();
                    else if (IsPointerInside(stageQuitButton, pointer)) QuitStageGame();
                }
                return;
            }
            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
            {
                SetStagePaused(true);
                return;
            }
            if (endingShown)
            {
                horizontalVelocity = Vector3.zero;
                if (animator != null && animator.runtimeAnimatorController != null) animator.SetFloat("Speed", 0f);
                UpdateEndingPages(keyboard);
                return;
            }

            bool introActive = Time.unscaledTime < introUntil;
            if (introActive && keyboard != null &&
                (keyboard.enterKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame))
            {
                introUntil = Time.unscaledTime;
                introActive = false;
                lookBlockedUntil = Time.unscaledTime + 0.12f;
                smoothedLook = Vector2.zero;
            }
            if (introPanel != null && introPanel.activeSelf && !introActive)
            {
                introPanel.SetActive(false);
                DuckChapterMusic();
                interactionGuideUntil = Time.unscaledTime + 6.5f;
                lookBlockedUntil = Time.unscaledTime + 0.22f;
                smoothedLook = Vector2.zero;
            }
            if (sharedPlayerController == null || !sharedPlayerController.enabled)
                UpdateMovement(!introActive);
            if (chapterTwoAdventure == null || !chapterTwoAdventure.IsTakingOverLegacyInteraction)
                UpdateInteraction();
            UpdateEndingBookVisual();
        }

        private void LateUpdate()
        {
            if (player == null || cameraRig == null) return;
            if (chapterTwoAdventure != null && chapterTwoAdventure.IsFirstPersonActive) return;
            if (culturalPreludeActive)
            {
                UpdateCulturalPreludeCamera();
                ApplyCulturalSeatedPose();
            }
            else if (sharedGameplayCamera == null || !sharedGameplayCamera.enabled) UpdateCamera();
        }

        private void BuildCulturalPrelude()
        {
            culturalPreludeRoot = CreatePanel("闽剧文化序幕", canvas.transform,
                new Color(.004f, .007f, .009f, .78f), Vector2.zero, Vector2.one);
            culturalPreludeGroup = culturalPreludeRoot.AddComponent<CanvasGroup>();
            culturalPreludeGroup.alpha = 0f;
            culturalPreludeGroup.blocksRaycasts = true;
            culturalPreludeGroup.interactable = false;

            GameObject imageFrame = CreatePanel("闽剧档案放映框", culturalPreludeRoot.transform,
                new Color(.11f, .055f, .026f, 1f), new Vector2(.105f, .145f), new Vector2(.895f, .835f));
            AddOutline(imageFrame, new Color(.94f, .60f, .20f, .92f), 2.2f);
            GameObject imageObject = CreatePanel("闽剧档案影像", imageFrame.transform, Color.white,
                new Vector2(.012f, .018f), new Vector2(.988f, .982f));
            culturalPreludeImage = imageObject.GetComponent<Image>();
            culturalPreludeImage.preserveAspect = true;
            culturalPreludeImage.raycastTarget = false;

            Text archive = CreateText("闽剧序幕眉题", culturalPreludeRoot.transform,
                "MEIXI CULTURAL ARCHIVE  /  MIN OPERA", 16, TextAnchor.MiddleCenter,
                new Color(.54f, .88f, .86f));
            SetRect(archive.rectTransform, new Vector2(.20f, .925f), new Vector2(.80f, .965f));
            Text title = CreateText("闽剧序幕标题", culturalPreludeRoot.transform,
                "一 声 入 闽 · 戏 从 乡 音 起", 34, TextAnchor.MiddleCenter,
                new Color(1f, .77f, .36f));
            title.font = displayFont;
            SetRect(title.rectTransform, new Vector2(.16f, .858f), new Vector2(.84f, .925f));
            AddOutline(title.gameObject, new Color(0f, 0f, 0f, .82f), 1.5f);

            culturalPreludeCaption = CreateText("闽剧序幕说明", culturalPreludeRoot.transform, string.Empty, 22,
                TextAnchor.MiddleCenter, new Color(.95f, .93f, .86f));
            culturalPreludeCaption.font = displayFont;
            SetRect(culturalPreludeCaption.rectTransform, new Vector2(.13f, .055f), new Vector2(.87f, .135f));
            culturalPreludeCounter = CreateText("闽剧序幕页码", culturalPreludeRoot.transform, "01 / 05", 14,
                TextAnchor.MiddleRight, new Color(.52f, .82f, .78f));
            SetRect(culturalPreludeCounter.rectTransform, new Vector2(.78f, .105f), new Vector2(.89f, .14f));

            string[] resources =
            {
                "Art/MinOperaIntro02",
                "Art/MinOperaPerformance01",
                "Art/MinOperaPerformance02",
                "Art/MinOperaPerformance03",
                "Art/MinOperaIntro04"
            };
            List<Sprite> loaded = new(resources.Length);
            foreach (string path in resources)
            {
                Texture2D texture = Resources.Load<Texture2D>(path);
                if (texture == null)
                {
                    Debug.LogWarning("[MeixiTheatre] Cultural prelude image missing: " + path);
                    continue;
                }
                loaded.Add(Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height),
                    new Vector2(.5f, .5f), 100f));
            }
            culturalPreludeSprites = loaded.ToArray();
            culturalPreludeRoot.SetActive(false);
            BuildCulturalQuiz();
        }

        private void BuildCulturalQuiz()
        {
            culturalQuizRoot = CreatePanel("闽剧文化问答", canvas.transform,
                new Color(.003f, .010f, .014f, .985f), Vector2.zero, Vector2.one);
            culturalQuizGroup = culturalQuizRoot.AddComponent<CanvasGroup>();
            culturalQuizGroup.alpha = 1f;
            culturalQuizGroup.blocksRaycasts = true;
            culturalQuizGroup.interactable = true;

            Text eyebrow = CreateText("闽剧问答眉题", culturalQuizRoot.transform,
                "MIN OPERA CULTURAL ARCHIVE  /  入场测验", 17, TextAnchor.MiddleCenter,
                new Color(.44f, .86f, .84f));
            SetRect(eyebrow.rectTransform, new Vector2(.18f, .86f), new Vector2(.82f, .92f));
            Text title = CreateText("闽剧问答标题", culturalQuizRoot.transform,
                "听 见 闽 剧", 46, TextAnchor.MiddleCenter, new Color(1f, .76f, .32f));
            title.font = displayFont;
            AddTextShadow(title, Color.black, new Vector2(2f, -2f), true);
            SetRect(title.rectTransform, new Vector2(.18f, .75f), new Vector2(.82f, .86f));

            culturalQuizProgress = CreateText("闽剧问答进度", culturalQuizRoot.transform,
                "01 / 04", 15, TextAnchor.MiddleCenter, new Color(.52f, .78f, .78f));
            SetRect(culturalQuizProgress.rectTransform, new Vector2(.40f, .70f), new Vector2(.60f, .75f));
            culturalQuizQuestion = CreateText("闽剧问答题目", culturalQuizRoot.transform,
                string.Empty, 30, TextAnchor.MiddleCenter, new Color(.94f, .94f, .88f));
            culturalQuizQuestion.font = displayFont;
            SetRect(culturalQuizQuestion.rectTransform, new Vector2(.20f, .56f), new Vector2(.80f, .70f));

            for (int i = 0; i < culturalQuizAnswerButtons.Length; i++)
            {
                int answerIndex = i;
                float top = .53f - i * .14f;
                culturalQuizAnswerButtons[i] = CreateButton($"闽剧问答选项{i + 1}", culturalQuizRoot.transform,
                    string.Empty, new Color(.055f, .19f, .22f, .99f),
                    new Vector2(.27f, top - .11f), new Vector2(.73f, top));
                culturalQuizAnswerButtons[i].onClick.AddListener(() => ChooseCulturalQuizAnswer(answerIndex));
            }

            culturalQuizFeedback = CreateText("闽剧问答反馈", culturalQuizRoot.transform,
                string.Empty, 18, TextAnchor.MiddleCenter, new Color(.62f, .88f, .84f));
            SetRect(culturalQuizFeedback.rectTransform, new Vector2(.20f, .055f), new Vector2(.80f, .12f));
            culturalQuizRoot.SetActive(false);
        }

        private void ShowCulturalQuiz()
        {
            culturalQuizQuestionIndex = 0;
            culturalQuizPassed = false;
            culturalQuizGroup.alpha = 1f;
            culturalQuizGroup.blocksRaycasts = true;
            culturalQuizGroup.interactable = true;
            culturalQuizRoot.SetActive(true);
            culturalQuizRoot.transform.SetAsLastSibling();
            MeixiCursorState.SetPointer();
            RefreshCulturalQuizQuestion();
        }

        private void RefreshCulturalQuizQuestion()
        {
            culturalQuizProgress.text = $"{culturalQuizQuestionIndex + 1:00} / {culturalQuizQuestions.Length:00}";
            culturalQuizQuestion.text = culturalQuizQuestions[culturalQuizQuestionIndex];
            culturalQuizFeedback.text = "请选择答案";
            culturalQuizFeedback.color = new Color(.62f, .88f, .84f);
            for (int i = 0; i < culturalQuizAnswerButtons.Length; i++)
            {
                Button button = culturalQuizAnswerButtons[i];
                button.interactable = true;
                Text label = button.GetComponentInChildren<Text>(true);
                if (label != null) label.text = culturalQuizAnswers[culturalQuizQuestionIndex][i];
            }
            EventSystem.current?.SetSelectedGameObject(culturalQuizAnswerButtons[0].gameObject);
        }

        private void ChooseCulturalQuizAnswer(int answerIndex)
        {
            if (culturalQuizPassed || answerIndex < 0 || answerIndex >= culturalQuizAnswerButtons.Length) return;
            if (answerIndex != culturalQuizCorrectAnswers[culturalQuizQuestionIndex])
            {
                culturalQuizFeedback.text = "还不对，再回想一下刚才的短片内容。";
                culturalQuizFeedback.color = new Color(1f, .48f, .34f);
                return;
            }

            foreach (Button button in culturalQuizAnswerButtons) button.interactable = false;
            culturalQuizFeedback.text = culturalQuizQuestionIndex == culturalQuizQuestions.Length - 1
                ? "全部答对，第二章已解锁。"
                : "回答正确，继续下一题。";
            culturalQuizFeedback.color = new Color(.48f, 1f, .72f);
            StartCoroutine(AdvanceCulturalQuiz());
        }

        private IEnumerator AdvanceCulturalQuiz()
        {
            yield return new WaitForSecondsRealtime(.68f);
            if (culturalQuizQuestionIndex >= culturalQuizQuestions.Length - 1)
            {
                culturalQuizPassed = true;
                yield break;
            }
            culturalQuizQuestionIndex++;
            RefreshCulturalQuizQuestion();
        }

        private IEnumerator FadeOutCulturalQuiz()
        {
            float elapsed = 0f;
            const float duration = .48f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                culturalQuizGroup.alpha = 1f - Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
                yield return null;
            }
            culturalQuizGroup.alpha = 0f;
            culturalQuizGroup.blocksRaycasts = false;
            culturalQuizGroup.interactable = false;
            culturalQuizRoot.SetActive(false);
        }

        private IEnumerator PlayCulturalPrelude()
        {
            SetSharedGameplayControlsActive(false);
            culturalPreludeActive = true;
            culturalPreludeCameraMode = 0;
            culturalSeatedPoseWeight = 0f;
            culturalStandingY = player.position.y;
            culturalLocomotionPolish = player.GetComponentInChildren<NaturalHumanoidLocomotionPolish>(true);
            horizontalVelocity = Vector3.zero;
            horizontalSmoothVelocity = Vector3.zero;
            smoothedLook = Vector2.zero;
            if (animator != null && animator.runtimeAnimatorController != null) animator.SetFloat("Speed", 0f);
            if (introPanel != null) introPanel.SetActive(false);
            if (theatreQuestCard != null) theatreQuestCard.SetActive(false);
            if (theatreHintCard != null) theatreHintCard.SetActive(false);
            MeixiCursorState.SetGameplay();
            culturalPreludeCameraFov = stageCamera != null ? stageCamera.fieldOfView : 60f;
            if (stageCamera != null) stageCamera.fieldOfView = 48f;

            yield return new WaitForSecondsRealtime(.82f);
            yield return WalkToCulturalPreludeSeat();
            culturalPreludeCameraMode = 1;
            yield return SitForCulturalPrelude();
            yield return new WaitForSecondsRealtime(.72f);

            culturalPreludeCameraMode = 2;
            HidePlayerForFirstPersonPrelude();
            culturalPreludeRoot.SetActive(true);
            culturalPreludeRoot.transform.SetAsLastSibling();
            float fadeElapsed = 0f;
            while (fadeElapsed < .55f)
            {
                fadeElapsed += Time.unscaledDeltaTime;
                culturalPreludeGroup.alpha = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(fadeElapsed / .55f));
                yield return null;
            }
            culturalPreludeGroup.alpha = 1f;

            string[] captions =
            {
                "闽剧，又称福州戏，是以福州方言演唱、念白的地方戏曲。",
                "它在闽中、闽东、闽北流传，也随乡音传播到台湾和东南亚。",
                "水袖、眼神与程式化身段，把人物的情绪写进一方戏台。",
                "锣鼓定节，管弦托腔；台上身段与台下乐师共同完成节奏。",
                "2006 年，闽剧作为传统戏剧列入第一批国家级非物质文化遗产名录。"
            };
            float[] durations = { 3.65f, 3.85f, 3.95f, 3.85f, 3.75f };
            int frameCount = Mathf.Min(culturalPreludeSprites.Length, captions.Length);
            for (int i = 0; i < frameCount; i++)
                yield return PresentCulturalFrame(culturalPreludeSprites[i], captions[i], i, frameCount, durations[i]);

            float exitElapsed = 0f;
            while (exitElapsed < .72f)
            {
                exitElapsed += Time.unscaledDeltaTime;
                culturalPreludeGroup.alpha = 1f - Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(exitElapsed / .72f));
                yield return null;
            }
            culturalPreludeGroup.alpha = 0f;
            culturalPreludeRoot.SetActive(false);
            ShowCulturalQuiz();
            while (!culturalQuizPassed) yield return null;
            yield return new WaitForSecondsRealtime(.82f);
            yield return FadeOutCulturalQuiz();

            RestorePlayerAfterFirstPersonPrelude();
            yield return RestorePlayerAfterCulturalSeat();
            if (stageCamera != null) stageCamera.fieldOfView = culturalPreludeCameraFov;
            introUntil = Time.unscaledTime + 3.55f;
            culturalPreludeActive = false;
            SetSharedGameplayControlsActive(true);
            if (introPanel != null) introPanel.SetActive(true);
            if (theatreQuestCard != null) theatreQuestCard.SetActive(true);
            if (theatreHintCard != null) theatreHintCard.SetActive(true);
            objective.text = "登上古戏台，依次唤醒编钟、大鼓与古弦的声纹";
            cameraVelocity = Vector3.zero;
            lookBlockedUntil = Time.unscaledTime + .35f;
            smoothedLook = Vector2.zero;
            chapterTwoAdventure?.ArmAfterQuiz();
            Debug.Log("[MeixiTheatre] Min Opera cultural prelude completed; third-person control will resume after chapter card.");
        }

        private IEnumerator PresentCulturalFrame(Sprite sprite, string caption, int index, int count, float duration)
        {
            culturalPreludeImage.sprite = sprite;
            culturalPreludeCaption.text = caption;
            culturalPreludeCounter.text = $"{index + 1:00} / {count:00}";
            RectTransform imageRect = culturalPreludeImage.rectTransform;
            float direction = (index & 1) == 0 ? 1f : -1f;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float visibility = Mathf.Min(Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / .13f)),
                    1f - Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((t - .84f) / .16f)));
                culturalPreludeImage.color = new Color(1f, .965f, .89f, visibility);
                culturalPreludeCaption.color = new Color(.95f, .93f, .86f, visibility);
                imageRect.localScale = Vector3.one * Mathf.Lerp(1.045f, 1.005f, t);
                imageRect.anchoredPosition = new Vector2(Mathf.Lerp(-10f, 10f, t) * direction,
                    Mathf.Sin(t * Mathf.PI) * 3f);
                yield return null;
            }
        }

        private IEnumerator WalkToCulturalPreludeSeat()
        {
            Vector3[] waypoints =
            {
                new(0f, culturalStandingY, -5.55f),
                new(1.20f, culturalStandingY, -5.55f)
            };
            foreach (Vector3 target in waypoints)
            {
                while (Vector3.ProjectOnPlane(target - player.position, Vector3.up).sqrMagnitude > .018f)
                {
                    Vector3 delta = Vector3.ProjectOnPlane(target - player.position, Vector3.up);
                    Vector3 direction = delta.normalized;
                    player.rotation = Quaternion.RotateTowards(player.rotation,
                        Quaternion.LookRotation(direction, Vector3.up), 420f * Time.unscaledDeltaTime);
                    float distanceBlend = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(delta.magnitude / 1.35f));
                    float travelSpeed = Mathf.Lerp(.34f, 2.15f, distanceBlend);
                    float travel = Mathf.Min(delta.magnitude, travelSpeed * Time.unscaledDeltaTime);
                    if (controller.enabled) controller.Move(direction * travel + Vector3.down * .03f);
                    if (animator != null && animator.runtimeAnimatorController != null)
                    {
                        animator.SetFloat("Speed", .43f, .08f, Time.unscaledDeltaTime);
                        animator.SetBool("Grounded", true);
                    }
                    yield return null;
                }
            }
            if (animator != null && animator.runtimeAnimatorController != null)
                animator.SetFloat("Speed", 0f, .10f, Time.unscaledDeltaTime);
        }

        private IEnumerator SitForCulturalPrelude()
        {
            if (culturalLocomotionPolish != null) culturalLocomotionPolish.enabled = false;
            if (controller.enabled) controller.enabled = false;
            Vector3 startPosition = player.position;
            Quaternion startRotation = player.rotation;
            Vector3 seatedPosition = new(2.18f, culturalStandingY - .25f, -5.48f);
            Quaternion seatedRotation = Quaternion.LookRotation(Vector3.forward, Vector3.up);
            float elapsed = 0f;
            const float duration = 1.05f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
                player.position = Vector3.Lerp(startPosition, seatedPosition, t);
                player.rotation = Quaternion.Slerp(startRotation, seatedRotation, t);
                culturalSeatedPoseWeight = t;
                yield return null;
            }
            player.position = seatedPosition;
            player.rotation = seatedRotation;
            culturalSeatedPoseWeight = 1f;
        }

        private void UpdateCulturalPreludeCamera()
        {
            Vector3 targetPosition;
            Vector3 lookPoint;
            float smooth;
            if (culturalPreludeCameraMode == 0)
            {
                targetPosition = player.position + new Vector3(-3.15f, 2.35f, -3.65f);
                lookPoint = player.position + Vector3.up * 1.18f + player.forward * .55f;
                smooth = .22f;
            }
            else if (culturalPreludeCameraMode == 1)
            {
                targetPosition = player.position + new Vector3(-3.05f, 2.20f, -2.45f);
                lookPoint = player.position + Vector3.up * 1.02f + Vector3.forward * .32f;
                smooth = .24f;
            }
            else
            {
                targetPosition = player.position + Vector3.up * 1.42f - Vector3.forward * .18f;
                lookPoint = new Vector3(0f, 2.8f, 8.4f);
                smooth = .16f;
            }
            cameraRig.position = Vector3.SmoothDamp(cameraRig.position, targetPosition, ref cameraVelocity, smooth,
                Mathf.Infinity, Time.unscaledDeltaTime);
            Quaternion rotation = Quaternion.LookRotation(lookPoint - cameraRig.position, Vector3.up);
            cameraRig.rotation = Quaternion.Slerp(cameraRig.rotation, rotation,
                1f - Mathf.Exp(-6.5f * Time.unscaledDeltaTime));
        }

        private void ApplyCulturalSeatedPose()
        {
            if (culturalSeatedPoseWeight <= .001f || animator == null || !animator.isHuman) return;
            float weight = Mathf.SmoothStep(0f, 1f, culturalSeatedPoseWeight);
            Vector3 forward = player.forward;
            Vector3 right = player.right;
            Vector3 down = Vector3.down;
            Transform leftUpperLeg = animator.GetBoneTransform(HumanBodyBones.LeftUpperLeg);
            Transform rightUpperLeg = animator.GetBoneTransform(HumanBodyBones.RightUpperLeg);
            Transform leftLowerLeg = animator.GetBoneTransform(HumanBodyBones.LeftLowerLeg);
            Transform rightLowerLeg = animator.GetBoneTransform(HumanBodyBones.RightLowerLeg);
            Transform leftFoot = animator.GetBoneTransform(HumanBodyBones.LeftFoot);
            Transform rightFoot = animator.GetBoneTransform(HumanBodyBones.RightFoot);
            Transform leftUpperArm = animator.GetBoneTransform(HumanBodyBones.LeftUpperArm);
            Transform rightUpperArm = animator.GetBoneTransform(HumanBodyBones.RightUpperArm);
            Transform leftLowerArm = animator.GetBoneTransform(HumanBodyBones.LeftLowerArm);
            Transform rightLowerArm = animator.GetBoneTransform(HumanBodyBones.RightLowerArm);
            Transform leftHand = animator.GetBoneTransform(HumanBodyBones.LeftHand);
            Transform rightHand = animator.GetBoneTransform(HumanBodyBones.RightHand);
            AimCulturalBone(leftUpperLeg, leftLowerLeg, forward * .78f + down * .20f - right * .08f, weight);
            AimCulturalBone(rightUpperLeg, rightLowerLeg, forward * .78f + down * .20f + right * .08f, weight);
            AimCulturalBone(leftLowerLeg, leftFoot, down + forward * .08f, weight);
            AimCulturalBone(rightLowerLeg, rightFoot, down + forward * .08f, weight);
            AimCulturalBone(leftUpperArm, leftLowerArm, down + forward * .14f - right * .04f, weight * .86f);
            AimCulturalBone(rightUpperArm, rightLowerArm, down + forward * .14f + right * .04f, weight * .86f);
            AimCulturalBone(leftLowerArm, leftHand, forward * .58f + down * .34f + right * .08f, weight * .78f);
            AimCulturalBone(rightLowerArm, rightHand, forward * .58f + down * .34f - right * .08f, weight * .78f);
        }

        private static void AimCulturalBone(Transform bone, Transform child, Vector3 direction, float weight)
        {
            if (bone == null || child == null || direction.sqrMagnitude < .0001f) return;
            Vector3 current = child.position - bone.position;
            if (current.sqrMagnitude < .0001f) return;
            Quaternion target = Quaternion.FromToRotation(current.normalized, direction.normalized) * bone.rotation;
            bone.rotation = Quaternion.Slerp(bone.rotation, target, Mathf.Clamp01(weight));
        }

        private IEnumerator RestorePlayerAfterCulturalSeat()
        {
            if (culturalLocomotionPolish != null) culturalLocomotionPolish.enabled = false;
            if (controller.enabled) controller.enabled = false;
            Vector3 startPosition = player.position;
            Quaternion startRotation = player.rotation;
            Vector3 standingPosition = new(0f, culturalStandingY, -5.52f);
            Quaternion standingRotation = Quaternion.LookRotation(Vector3.forward, Vector3.up);
            culturalPreludeCameraMode = 1;
            float elapsed = 0f;
            const float duration = 1.05f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
                player.position = Vector3.Lerp(startPosition, standingPosition, t);
                player.rotation = Quaternion.Slerp(startRotation, standingRotation, t);
                culturalSeatedPoseWeight = 1f - t;
                if (animator != null && animator.runtimeAnimatorController != null)
                    animator.SetFloat("Speed", t < .72f ? 0f : .18f);
                yield return null;
            }

            player.position = standingPosition;
            player.rotation = standingRotation;
            culturalSeatedPoseWeight = 0f;
            if (animator != null && animator.runtimeAnimatorController != null)
            {
                animator.Rebind();
                animator.Update(0f);
                animator.SetFloat("Speed", 0f);
                animator.SetBool("Grounded", true);
            }
            Physics.SyncTransforms();
            controller.enabled = true;
            verticalVelocity = -1.6f;
            horizontalVelocity = Vector3.zero;
            horizontalSmoothVelocity = Vector3.zero;
            movementBlockedUntil = Time.unscaledTime + .45f;
            yaw = player.eulerAngles.y;
            pitch = 19f;
            cameraDistance = 5.45f;
            smoothedLook = Vector2.zero;
            if (culturalLocomotionPolish != null) culturalLocomotionPolish.enabled = true;
            MeixiCursorState.SetGameplay();
        }

        private void HidePlayerForFirstPersonPrelude()
        {
            culturalPreludePlayerRenderers = player.GetComponentsInChildren<Renderer>(true);
            culturalPreludeRendererStates = new bool[culturalPreludePlayerRenderers.Length];
            for (int i = 0; i < culturalPreludePlayerRenderers.Length; i++)
            {
                Renderer renderer = culturalPreludePlayerRenderers[i];
                culturalPreludeRendererStates[i] = renderer != null && renderer.enabled;
                if (renderer != null) renderer.enabled = false;
            }
        }

        private void RestorePlayerAfterFirstPersonPrelude()
        {
            for (int i = 0; i < culturalPreludePlayerRenderers.Length; i++)
                if (culturalPreludePlayerRenderers[i] != null)
                    culturalPreludePlayerRenderers[i].enabled = i < culturalPreludeRendererStates.Length &&
                                                                culturalPreludeRendererStates[i];
        }

        private static bool ShouldSkipCulturalPrelude()
        {
            // A chapter-two gate checkpoint means the player has already passed
            // the Min Opera presentation and quiz. Reloading after a puppet
            // catch must restore the chase directly instead of replaying the
            // introduction and blocking on the quiz again.
            if (MeixiLocalProgress.IsCheckpoint("ChapterTwoGateOpened"))
            {
                Debug.Log("[MeixiTheatre] Skipping Min Opera prelude: gate checkpoint is active.");
                return true;
            }
            foreach (string argument in Environment.GetCommandLineArgs())
            {
                if (!argument.StartsWith("-meixi", StringComparison.OrdinalIgnoreCase)) continue;
                if (string.Equals(argument, "-meixiMinOperaPreludeCapture", StringComparison.OrdinalIgnoreCase))
                    continue;
                if (argument.Contains("Capture", StringComparison.OrdinalIgnoreCase) ||
                    argument.Contains("Smoke", StringComparison.OrdinalIgnoreCase) ||
                    argument.Contains("Validation", StringComparison.OrdinalIgnoreCase)) return true;
            }
            return false;
        }

        private void UpdateMovement(bool acceptsInput)
        {
            Keyboard keyboard = Keyboard.current;
            // Keep one stalled frame from turning a held A/D key into a large
            // CharacterController displacement after the opening transition.
            float frameDelta = Mathf.Min(Time.deltaTime, 1f / 30f);
            acceptsInput &= Time.unscaledTime >= movementBlockedUntil;
            float x = 0f;
            float z = 0f;
            if (acceptsInput && keyboard != null)
            {
                x = (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed ? 1f : 0f) -
                    (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed ? 1f : 0f);
                z = (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed ? 1f : 0f) -
                    (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed ? 1f : 0f);
            }

            // Read the heading that is actually on screen. The stored yaw can
            // be ahead of the smoothed camera during an opening or focus blend,
            // which makes a short A/D press move on an unexpected world axis.
            Vector3 cameraForward = cameraRig != null
                ? Vector3.ProjectOnPlane(cameraRig.forward, Vector3.up)
                : Quaternion.Euler(0f, yaw, 0f) * Vector3.forward;
            if (cameraForward.sqrMagnitude < .001f) cameraForward = Vector3.forward;
            cameraForward.Normalize();
            Vector3 cameraRight = Vector3.Cross(Vector3.up, cameraForward).normalized;
            Vector3 move = Vector3.ClampMagnitude(cameraForward * z + cameraRight * x, 1f);
            if (move.sqrMagnitude > 0.01f)
                player.rotation = Quaternion.RotateTowards(player.rotation,
                    Quaternion.LookRotation(move, Vector3.up), 720f * frameDelta);

            Vector3 targetVelocity = move * 5.2f;
            horizontalVelocity = Vector3.SmoothDamp(horizontalVelocity, targetVelocity, ref horizontalSmoothVelocity,
                targetVelocity.sqrMagnitude > 0.01f ? 0.060f : 0.085f, Mathf.Infinity, frameDelta);
            if (IsGrounded()) verticalVelocity = -1.8f;
            else verticalVelocity = Mathf.Max(verticalVelocity + Physics.gravity.y * 2.15f * frameDelta, -24f);
            Vector3 beforeMove = player.position;
            controller.Move((horizontalVelocity + Vector3.up * verticalVelocity) * frameDelta);
            Vector3 actualMove = player.position - beforeMove;
            float maxExpectedHorizontal = Mathf.Max(.24f, horizontalVelocity.magnitude * frameDelta + .08f);
            if (actualMove.x * actualMove.x + actualMove.z * actualMove.z > maxExpectedHorizontal * maxExpectedHorizontal)
            {
                controller.enabled = false;
                player.position = beforeMove;
                controller.enabled = true;
                horizontalVelocity = Vector3.zero;
                horizontalSmoothVelocity = Vector3.zero;
                Debug.LogWarning($"[MeixiTheatre] Ignored unexpected one-frame player displacement: {actualMove}");
            }

            if (animator != null && animator.runtimeAnimatorController != null)
            {
                animator.SetFloat("Speed", Mathf.Clamp01(horizontalVelocity.magnitude / 5.2f) * 0.62f);
                animator.SetBool("Grounded", IsGrounded());
            }

            if (player.position.y < -3f)
            {
                controller.enabled = false;
                player.position = new Vector3(0f, 0.08f, -7.4f);
                player.rotation = Quaternion.identity;
                controller.enabled = true;
                verticalVelocity = -1.8f;
                horizontalVelocity = Vector3.zero;
            }
        }

        private void UpdateCamera()
        {
            Mouse mouse = Mouse.current;
            if (Time.unscaledTime >= introUntil && mouse != null && !MeixiCursorState.IsPointerMode &&
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
            if (endingBook != null)
            {
                Vector3 toBook = endingBook.transform.position - player.position;
                toBook.y = 0f;
                if (toBook.sqrMagnitude < 10.2f)
                {
                    hint.text = "[ E ]  拾取「余音之书」\n四件乐器的回响，正停留在翻开的书页里";
                    if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
                    {
                        if (!IsAutomatedPresentationRun()) MeixiLocalProgress.RecordFinalBookCollected();
                        Destroy(endingBook);
                        endingBook = null;
                        ShowEndingMessage();
                    }
                    return;
                }
            }
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
                hint.text = Time.unscaledTime < interactionGuideUntil
                    ? "场域提示  ·  戏台中的每一件乐器都可以互动\n靠近发光的乐器，按 E 聆听并演奏"
                    : "WASD  行走   ·   鼠标  环绕镜头   ·   E  演奏乐器   ·   ESC  暂停 / 设置";
                return;
            }
            hint.text = "[ E ]  演奏「" + current.DisplayName + "」\n" + current.Description;
            if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame) current.Play();
        }

        private static bool IsAutomatedPresentationRun()
        {
            return Array.Exists(Environment.GetCommandLineArgs(), value =>
                value.StartsWith("-meixi", StringComparison.OrdinalIgnoreCase));
        }

        public void RegisterNearby(TheatreInstrument instrument)
        {
            if (instrument != null && !nearbyInstruments.Contains(instrument)) nearbyInstruments.Add(instrument);
        }

        public void NotifyInstrumentPlayed(TheatreInstrument instrument)
        {
            if (instrument == null) return;
            if (chapterTwoAdventure != null && chapterTwoAdventure.IsArmed)
            {
                chapterTwoAdventure.NotifyLegacyInstrumentPlayed(instrument);
                return;
            }
            playedInstruments.Add(instrument);
            int total = FindObjectsByType<TheatreInstrument>().Length;
            bool completed = total > 0 && playedInstruments.Count >= total;
            objective.text = completed
                ? "旧曲的四个声部已经归位，古戏台重新拥有了回响"
                : $"已唤醒 {playedInstruments.Count} / {Mathf.Max(1, total)} 个声部 · 继续寻找发光的传统乐器";
            cameraDistance = Mathf.Max(5.4f, cameraDistance - 0.18f);
            if (completed && !endingShown && endingBook == null) SpawnEndingBook();
        }

        public void HideLegacyPresentationForAdventure()
        {
            introUntil = Time.unscaledTime;
            if (introPanel != null) introPanel.SetActive(false);
            if (culturalPreludeRoot != null) culturalPreludeRoot.SetActive(false);
            if (culturalQuizRoot != null) culturalQuizRoot.SetActive(false);
            if (endingPanel != null) endingPanel.SetActive(false);
        }

        private void SpawnEndingBook()
        {
            endingBook = new GameObject("舞台中央_余音之书");
            endingBookBasePosition = new Vector3(0f, 1.66f, 6.55f);
            endingBook.transform.position = endingBookBasePosition;
            endingBook.transform.rotation = Quaternion.Euler(-8f, 0f, 0f);

            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            Material cover = new(shader) { name = "余音之书_青黑封面", color = new Color(.035f, .15f, .15f) };
            Material paper = new(shader) { name = "余音之书_旧纸", color = new Color(.88f, .78f, .57f) };
            Material gold = new(shader) { name = "余音之书_金线", color = new Color(.75f, .48f, .14f) };
            if (cover.HasProperty("_Smoothness")) cover.SetFloat("_Smoothness", .34f);
            if (paper.HasProperty("_Smoothness")) paper.SetFloat("_Smoothness", .08f);
            if (gold.HasProperty("_Metallic")) gold.SetFloat("_Metallic", .62f);
            if (gold.HasProperty("_Smoothness")) gold.SetFloat("_Smoothness", .58f);

            CreateBookPart("下封面", new Vector3(0f, -.10f, 0f), new Vector3(1.12f, .09f, .82f), cover);
            CreateBookPart("书页", new Vector3(0f, 0f, 0f), new Vector3(1.03f, .15f, .74f), paper);
            CreateBookPart("上封面", new Vector3(0f, .105f, 0f), new Vector3(1.12f, .075f, .82f), cover);
            CreateBookPart("金色书脊", new Vector3(-.55f, .01f, 0f), new Vector3(.075f, .24f, .84f), gold);
            for (int i = -2; i <= 2; i++)
                CreateBookPart("封面声纹_" + i, new Vector3(.12f + i * .14f, .15f, 0f),
                    new Vector3(.018f, .012f, .52f - Mathf.Abs(i) * .055f), gold);

            Light glow = endingBook.AddComponent<Light>();
            glow.type = LightType.Point;
            glow.color = new Color(.30f, .88f, .78f);
            glow.range = 4.6f;
            glow.intensity = 2.2f;
            glow.shadows = LightShadows.None;
            objective.text = "四个声部已经归位 · 舞台中央出现了一本承载余音的旧书";
            hint.text = "场域回响  ·  前往舞台中央，拾取余音之书";

            void CreateBookPart(string partName, Vector3 localPosition, Vector3 localScale, Material material)
            {
                GameObject part = GameObject.CreatePrimitive(PrimitiveType.Cube);
                part.name = partName;
                part.transform.SetParent(endingBook.transform, false);
                part.transform.localPosition = localPosition;
                part.transform.localScale = localScale;
                part.GetComponent<Renderer>().sharedMaterial = material;
                Collider collider = part.GetComponent<Collider>();
                if (collider != null) Destroy(collider);
            }
        }

        private void UpdateEndingBookVisual()
        {
            if (endingBook == null) return;
            float wave = Mathf.Sin(Time.unscaledTime * 1.65f);
            endingBook.transform.position = endingBookBasePosition + Vector3.up * (wave * .08f);
            endingBook.transform.Rotate(Vector3.up, Time.unscaledDeltaTime * 13f, Space.World);
            Light glow = endingBook.GetComponent<Light>();
            if (glow != null) glow.intensity = 1.9f + (wave + 1f) * .35f;
        }

        private void ShowEndingMessage()
        {
            endingShown = true;
            horizontalVelocity = Vector3.zero;
            horizontalSmoothVelocity = Vector3.zero;
            if (controller != null) controller.Move(Vector3.zero);
            if (animator != null && animator.runtimeAnimatorController != null) animator.SetFloat("Speed", 0f);
            if (endingPanel != null) endingPanel.SetActive(true);
            endingPageIndex = 0;
            endingInputReadyAt = Time.unscaledTime + .55f;
            PresentEndingPage();
            if (endingMusicSource != null && endingMusicSource.clip != null)
            {
                StartChapterMusicFade(.14f, 1.4f);
                endingMusicSource.volume = 0f;
                endingMusicSource.Play();
                StartCoroutine(FadeInEndingMusic());
            }
            MeixiCursorState.SetPointer();
            Debug.Log("[MeixiTheatre] All instruments restored; ending message opened.");
        }

        private void PrepareChapterMusic()
        {
            chapterMusicSource = gameObject.AddComponent<AudioSource>();
            chapterMusicSource.playOnAwake = false;
            chapterMusicSource.loop = true;
            chapterMusicSource.spatialBlend = 0f;
            chapterMusicSource.dopplerLevel = 0f;
            chapterMusicSource.ignoreListenerPause = true;
            chapterMusicSource.clip = Resources.Load<AudioClip>("Audio/ChapterTwoOpening");
            if (chapterMusicSource.clip == null)
            {
                Debug.LogWarning("[MeixiTheatre] Chapter-two opening BGM was not found at Resources/Audio/ChapterTwoOpening.");
                return;
            }

            chapterMusicSource.volume = 0f;
            chapterMusicSource.Play();
            StartChapterMusicFade(.64f, 1.15f);
            Debug.Log($"[MeixiTheatre] Chapter-two opening BGM started: {chapterMusicSource.clip.name}");
        }

        public void StopChapterMusic()
        {
            if (chapterMusicFadeRoutine != null)
            {
                StopCoroutine(chapterMusicFadeRoutine);
                chapterMusicFadeRoutine = null;
            }
            foreach (AudioSource source in GetComponents<AudioSource>())
            {
                if (source == null || source.clip == null ||
                    !string.Equals(source.clip.name, "ChapterTwoOpening", StringComparison.OrdinalIgnoreCase)) continue;
                source.Stop();
                source.volume = 0f;
                source.clip = null;
            }
        }

        private void DuckChapterMusic()
        {
            if (chapterMusicDucked) return;
            chapterMusicDucked = true;
            StartChapterMusicFade(.27f, 1.8f);
        }

        private void StartChapterMusicFade(float targetVolume, float duration)
        {
            if (chapterMusicSource == null || chapterMusicSource.clip == null) return;
            if (chapterMusicFadeRoutine != null) StopCoroutine(chapterMusicFadeRoutine);
            chapterMusicFadeRoutine = StartCoroutine(FadeChapterMusicTo(targetVolume, duration));
        }

        private IEnumerator FadeChapterMusicTo(float targetVolume, float duration)
        {
            if (chapterMusicSource == null || chapterMusicSource.clip == null) yield break;
            float startVolume = chapterMusicSource.volume;
            float elapsed = 0f;
            duration = Mathf.Max(.01f, duration);
            while (elapsed < duration && chapterMusicSource != null)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                t = t * t * (3f - 2f * t);
                chapterMusicSource.volume = Mathf.Lerp(startVolume, targetVolume, t);
                yield return null;
            }
            if (chapterMusicSource != null) chapterMusicSource.volume = targetVolume;
            chapterMusicFadeRoutine = null;
        }

        private void PrepareEndingMusic()
        {
            endingMusicSource = gameObject.AddComponent<AudioSource>();
            endingMusicSource.playOnAwake = false;
            endingMusicSource.loop = false;
            endingMusicSource.spatialBlend = 0f;
            endingMusicSource.dopplerLevel = 0f;
            endingMusicSource.clip = Resources.Load<AudioClip>("Audio/EndingTheme");
        }

        private IEnumerator FadeInEndingMusic()
        {
            const float duration = 2.4f;
            float elapsed = 0f;
            while (elapsed < duration && endingMusicSource != null)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                endingMusicSource.volume = Mathf.SmoothStep(0f, .62f, t);
                yield return null;
            }
            if (endingMusicSource != null) endingMusicSource.volume = .62f;
        }

        private void LoadEndingPages()
        {
            TextAsset endingAsset = Resources.Load<TextAsset>("EndingMessage");
            string source = endingAsset != null ? endingAsset.text.Trim() :
                "余音未散\n音乐并没有真正消失，它只是沉睡在仍被记得的声音里。";
            string[] sections = source.Replace("\r\n", "\n").Split(
                new[] { "\n===PAGE===\n" }, StringSplitOptions.RemoveEmptyEntries);
            List<string> titles = new();
            List<string> bodies = new();
            foreach (string rawSection in sections)
            {
                string section = rawSection.Trim();
                int lineBreak = section.IndexOf('\n');
                if (lineBreak < 0)
                {
                    titles.Add("余音");
                    bodies.Add(section);
                    continue;
                }
                titles.Add(section[..lineBreak].Trim());
                bodies.Add(section[(lineBreak + 1)..].Trim());
            }
            endingPageTitles = titles.ToArray();
            endingPageBodies = bodies.ToArray();
        }

        private void UpdateEndingPages(Keyboard keyboard)
        {
            if (Time.unscaledTime < endingInputReadyAt || endingPageBodies.Length == 0) return;
            bool previous = keyboard != null && (keyboard.leftArrowKey.wasPressedThisFrame ||
                                                   keyboard.upArrowKey.wasPressedThisFrame ||
                                                   keyboard.aKey.wasPressedThisFrame);
            bool next = keyboard != null && (keyboard.rightArrowKey.wasPressedThisFrame ||
                                               keyboard.downArrowKey.wasPressedThisFrame ||
                                               keyboard.dKey.wasPressedThisFrame ||
                                               keyboard.spaceKey.wasPressedThisFrame ||
                                               keyboard.enterKey.wasPressedThisFrame ||
                                               keyboard.eKey.wasPressedThisFrame);
            Mouse mouse = Mouse.current;
            if (mouse != null && mouse.leftButton.wasPressedThisFrame) next = true;
            int nextPage = endingPageIndex + (next ? 1 : previous ? -1 : 0);
            nextPage = Mathf.Clamp(nextPage, 0, endingPageBodies.Length - 1);
            if (nextPage == endingPageIndex) return;
            endingPageIndex = nextPage;
            PresentEndingPage();
        }

        private void PresentEndingPage()
        {
            if (endingPageBodies.Length == 0) LoadEndingPages();
            if (endingPageBodies.Length == 0) return;
            endingPageIndex = Mathf.Clamp(endingPageIndex, 0, endingPageBodies.Length - 1);
            endingPageTitle.text = endingPageTitles[endingPageIndex];
            endingBody.text = endingPageBodies[endingPageIndex];
            endingBody.fontSize = endingPageIndex == endingPageBodies.Length - 1 ? 20 :
                endingPageIndex == 3 ? 23 : 27;
            endingPageNumber.text = $"{endingPageIndex + 1:00}  /  {endingPageBodies.Length:00}";
            endingNavigation.text = endingPageIndex >= endingPageBodies.Length - 1
                ? "←  回看     ·     ESC  返回梅溪村"
                : "←  上一页     ·     SPACE / ENTER / 点击  下一页  →";
        }

        private void CreateHud()
        {
            canvas = new GameObject("第二章舞台界面").AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvas.gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            canvas.gameObject.AddComponent<GraphicRaycaster>();
            if (EventSystem.current == null)
            {
                GameObject eventSystem = new("第二章界面事件系统");
                eventSystem.AddComponent<EventSystem>();
                eventSystem.AddComponent<InputSystemUIInputModule>();
            }

            GameObject questCard = CreatePanel("章节目标卡", canvas.transform, new Color(0.018f, 0.045f, 0.050f, 0.91f),
                new Vector2(0.025f, 0.82f), new Vector2(0.36f, 0.965f));
            AddOutline(questCard, new Color(0.92f, 0.55f, 0.19f, 0.75f));
            theatreQuestCard = questCard;
            Text chapter = CreateText("章节标题", questCard.transform, "第 二 章  ·  古 厝 里 的 旧 曲", 28,
                TextAnchor.MiddleLeft, new Color(1f, 0.76f, 0.34f));
            SetRect(chapter.rectTransform, new Vector2(0.06f, 0.52f), new Vector2(0.95f, 0.90f));
            objective = CreateText("章节目标", questCard.transform, "登上古戏台，依次唤醒编钟、大鼓与古弦的声纹", 18,
                TextAnchor.MiddleLeft, new Color(0.76f, 0.92f, 0.94f));
            SetRect(objective.rectTransform, new Vector2(0.06f, 0.10f), new Vector2(0.95f, 0.50f));

            GameObject hintCard = CreatePanel("交互提示卡", canvas.transform, new Color(0.008f, 0.025f, 0.030f, 0.90f),
                new Vector2(0.20f, 0.025f), new Vector2(0.80f, 0.15f));
            AddOutline(hintCard, new Color(0.32f, 0.82f, 0.88f, 0.62f));
            theatreHintCard = hintCard;
            hint = CreateText("舞台提示文字", hintCard.transform, string.Empty, 20, TextAnchor.MiddleCenter,
                new Color(0.94f, 0.96f, 0.92f));
            SetRect(hint.rectTransform, new Vector2(0.04f, 0.08f), new Vector2(0.96f, 0.92f));

            introPanel = CreatePanel("第二章转场", canvas.transform, new Color(0.008f, 0.018f, 0.022f, 0.76f),
                Vector2.zero, Vector2.one);
            Text eyebrow = CreateText("转场眉题", introPanel.transform, "MEIXI VOICE ARCHIVE  /  THE OLD THEATRE", 19,
                TextAnchor.MiddleCenter, new Color(0.36f, 0.82f, 0.88f));
            SetRect(eyebrow.rectTransform, new Vector2(0.20f, 0.675f), new Vector2(0.80f, 0.72f));
            Text chapterMark = CreateText("转场章节题签", introPanel.transform, "第  二  章", 22,
                TextAnchor.MiddleCenter, new Color(.48f, .84f, .82f));
            chapterMark.font = displayFont;
            SetRect(chapterMark.rectTransform, new Vector2(.36f, .60f), new Vector2(.64f, .67f));
            Text title = CreateText("转场标题", introPanel.transform, "古 厝 里 的 旧 曲", 58,
                TextAnchor.MiddleCenter, new Color(.96f, .81f, .52f));
            title.font = displayFont;
            SetRect(title.rectTransform, new Vector2(0.15f, 0.43f), new Vector2(0.85f, 0.59f));
            AddOutline(title.gameObject, new Color(0f, 0f, 0f, 0.78f), 2f);
            Text subtitle = CreateText("转场副题", introPanel.transform, "风穿过百年木梁，墙壁开始归还被遗忘的旋律", 23,
                TextAnchor.MiddleCenter, new Color(0.86f, 0.91f, 0.90f));
            subtitle.font = displayFont;
            SetRect(subtitle.rectTransform, new Vector2(0.18f, 0.335f), new Vector2(0.82f, 0.415f));
            Text ornament = CreateText("转场余音纹", introPanel.transform, "—  ◇  —", 18,
                TextAnchor.MiddleCenter, new Color(.48f, .72f, .67f, .78f));
            ornament.font = displayFont;
            SetRect(ornament.rectTransform, new Vector2(.40f, .27f), new Vector2(.60f, .325f));

            endingPanel = CreatePanel("Demo结束语", canvas.transform, new Color(0.004f, 0.010f, 0.013f, 0.985f),
                Vector2.zero, Vector2.one);
            Texture2D endingBookTexture = Resources.Load<Texture2D>("Art/EndingOpenBook");
            if (endingBookTexture != null)
            {
                Image endingBackground = endingPanel.GetComponent<Image>();
                endingBackground.sprite = Sprite.Create(endingBookTexture,
                    new Rect(0f, 0f, endingBookTexture.width, endingBookTexture.height), new Vector2(.5f, .5f), 100f);
                endingBackground.color = Color.white;
            }
            Text endingEyebrow = CreateText("结束语眉题", endingPanel.transform,
                "M E I X I   ·   V O I C E   A R C H I V E", 14, TextAnchor.MiddleLeft,
                new Color(.18f, .38f, .36f));
            SetRect(endingEyebrow.rectTransform, new Vector2(0.175f, 0.755f), new Vector2(0.455f, 0.805f));

            Text endingBrand = CreateText("结束语作品名", endingPanel.transform, "梅 溪 回 响", 36,
                TextAnchor.MiddleLeft, new Color(.28f, .18f, .09f));
            endingBrand.font = displayFont;
            SetRect(endingBrand.rectTransform, new Vector2(.175f, .655f), new Vector2(.455f, .755f));

            Text sectionMark = CreateText("结束语章节印记", endingPanel.transform, "终 章\nEPILOGUE", 17,
                TextAnchor.MiddleCenter, new Color(.22f, .40f, .36f, .92f));
            sectionMark.font = displayFont;
            SetRect(sectionMark.rectTransform, new Vector2(.255f, .43f), new Vector2(.39f, .58f));
            GameObject verticalLine = CreatePanel("结束语纵向声纹", endingPanel.transform,
                new Color(.30f, .26f, .18f, .28f), new Vector2(.498f, .22f), new Vector2(.499f, .78f));
            verticalLine.GetComponent<Image>().raycastTarget = false;

            endingPageTitle = CreateText("结束语页标题", endingPanel.transform, "余音未散", 42,
                TextAnchor.MiddleLeft, new Color(.30f, .18f, .08f));
            endingPageTitle.font = displayFont;
            SetRect(endingPageTitle.rectTransform, new Vector2(.555f, .69f), new Vector2(.855f, .79f));

            endingBody = CreateText("结束语正文", endingPanel.transform, string.Empty, 24,
                TextAnchor.UpperLeft, new Color(.10f, .078f, .052f));
            endingBody.font = displayFont;
            endingBody.lineSpacing = 1.32f;
            SetRect(endingBody.rectTransform, new Vector2(0.555f, 0.285f), new Vector2(0.855f, 0.675f));

            endingPageNumber = CreateText("结束语页码", endingPanel.transform, "01  /  05", 15,
                TextAnchor.MiddleRight, new Color(.25f, .38f, .34f));
            SetRect(endingPageNumber.rectTransform, new Vector2(.735f, .79f), new Vector2(.855f, .835f));
            Text closingMark = CreateText("结束语收束印记", endingPanel.transform, "◇", 20,
                TextAnchor.MiddleCenter, new Color(.32f, .79f, .76f));
            closingMark.font = displayFont;
            closingMark.color = new Color(.20f, .43f, .39f);
            SetRect(closingMark.rectTransform, new Vector2(.835f, .45f), new Vector2(.87f, .51f));

            endingNavigation = CreateText("结束语操作提示", endingPanel.transform,
                "SPACE / ENTER / 点击  下一页  →", 16, TextAnchor.MiddleRight,
                new Color(.14f, .32f, .29f));
            SetRect(endingNavigation.rectTransform, new Vector2(0.53f, 0.205f), new Vector2(0.855f, 0.255f));
            LoadEndingPages();
            PresentEndingPage();
            endingPanel.SetActive(false);
            BuildStagePauseUi();
        }

        private void BuildStagePauseUi()
        {
            stagePausePanel = CreatePanel("第二章暂停页", canvas.transform,
                new Color(.003f, .012f, .020f, .82f), Vector2.zero, Vector2.one);
            GameObject pauseCard = CreatePanel("第二章暂停主卡", stagePausePanel.transform,
                new Color(.018f, .055f, .080f, .98f), new Vector2(.34f, .16f), new Vector2(.66f, .84f));
            PolishStagePanel(pauseCard, new Color(1f, .55f, .17f, .65f));
            pauseCard.AddComponent<MeixiUiPanelReveal>().Configure(.24f, .90f);

            Text pauseEyebrow = CreateText("暂停眉题", pauseCard.transform, "MEIXI RESONANCE", 16,
                TextAnchor.MiddleCenter, new Color(.32f, .82f, .92f));
            SetRect(pauseEyebrow.rectTransform, new Vector2(.12f, .84f), new Vector2(.88f, .92f));
            Text pauseTitle = CreateText("暂停标题", pauseCard.transform, "魂 音 暂 歇", 48,
                TextAnchor.MiddleCenter, new Color(1f, .78f, .34f));
            AddTextShadow(pauseTitle, Color.black, new Vector2(3f, -3f), true);
            SetRect(pauseTitle.rectTransform, new Vector2(.08f, .68f), new Vector2(.92f, .84f));
            Text practice = CreateText("暂停实践记录", pauseCard.transform,
                $"乡 村 数 字 文 化 实 践  ·  第 {MeixiLocalProgress.PracticeNumber:00} 次\n" +
                $"累 计 倒 下  {MeixiLocalProgress.TotalDeaths:00}  次  ·  无伤重启不计入实践次数", 15,
                TextAnchor.MiddleCenter, new Color(.48f, .90f, .88f));
            practice.lineSpacing = 1.12f;
            SetRect(practice.rectTransform, new Vector2(.10f, .615f), new Vector2(.90f, .685f));
            Text controls = CreateText("第二章暂停操作", pauseCard.transform,
                "WASD  行走     鼠标  环绕镜头\nE  演奏古乐     ESC  返回游戏\n靠近发光乐器，聆听并恢复古戏台声纹", 19,
                TextAnchor.MiddleCenter, new Color(.74f, .87f, .93f));
            SetRect(controls.rectTransform, new Vector2(.09f, .445f), new Vector2(.91f, .595f));

            stageResumeButton = CreateButton("第二章继续", pauseCard.transform, "继 续 聆 听",
                new Color(.62f, .18f, .07f, .98f), new Vector2(.18f, .33f), new Vector2(.82f, .44f));
            stageResumeButton.onClick.AddListener(() => SetStagePaused(false));
            stageSettingsButton = CreateButton("第二章设置", pauseCard.transform, "设 置 · 成 就",
                new Color(.13f, .16f, .34f, .98f), new Vector2(.18f, .20f), new Vector2(.49f, .31f));
            stageSettingsButton.onClick.AddListener(OpenStageSettings);
            stageReturnButton = CreateButton("返回梅溪村", pauseCard.transform, "返 回 梅 溪 村",
                new Color(.06f, .24f, .31f, .98f), new Vector2(.51f, .20f), new Vector2(.82f, .31f));
            stageReturnButton.onClick.AddListener(ReturnToVillageFromPause);
            stageQuitButton = CreateButton("第二章退出游戏", pauseCard.transform, "退 出 游 戏",
                new Color(.30f, .07f, .08f, .98f), new Vector2(.18f, .07f), new Vector2(.82f, .18f));
            stageQuitButton.onClick.AddListener(QuitStageGame);
            Text pauseHint = CreateText("暂停提示", pauseCard.transform, "ESC  返回游戏", 14,
                TextAnchor.MiddleCenter, new Color(.48f, .72f, .79f));
            SetRect(pauseHint.rectTransform, new Vector2(.15f, .005f), new Vector2(.85f, .06f));

            stageSettingsPanel = CreatePanel("第二章设置页", canvas.transform,
                new Color(.002f, .009f, .014f, .96f), Vector2.zero, Vector2.one);
            GameObject settingsCard = CreatePanel("第二章设置主卡", stageSettingsPanel.transform,
                new Color(.008f, .035f, .046f, .998f), new Vector2(.27f, .14f), new Vector2(.73f, .86f));
            AddOutline(settingsCard, new Color(.30f, .84f, .90f, .72f), 2f);
            Text settingsTitle = CreateText("第二章设置标题", settingsCard.transform, "设 置 · 成 就 档 案", 38,
                TextAnchor.MiddleCenter, new Color(1f, .76f, .30f));
            settingsTitle.font = displayFont;
            SetRect(settingsTitle.rectTransform, new Vector2(.08f, .82f), new Vector2(.92f, .94f));
            GameObject volumeCard = CreatePanel("第二章音量卡", settingsCard.transform,
                new Color(.012f, .072f, .084f, .98f), new Vector2(.08f, .66f), new Vector2(.92f, .79f));
            AddOutline(volumeCard, new Color(.27f, .72f, .76f, .46f));
            stageVolumeText = CreateText("第二章音量值", volumeCard.transform, string.Empty, 18,
                TextAnchor.MiddleCenter, new Color(.84f, .94f, .94f));
            SetRect(stageVolumeText.rectTransform, new Vector2(.22f, .08f), new Vector2(.78f, .92f));
            stageVolumeDownButton = CreateButton("第二章降低音量", volumeCard.transform, "－",
                new Color(.07f, .21f, .28f, .99f), new Vector2(.04f, .14f), new Vector2(.18f, .86f));
            stageVolumeDownButton.onClick.AddListener(() => AdjustStageVolume(-.10f));
            stageVolumeUpButton = CreateButton("第二章提高音量", volumeCard.transform, "＋",
                new Color(.10f, .28f, .27f, .99f), new Vector2(.82f, .14f), new Vector2(.96f, .86f));
            stageVolumeUpButton.onClick.AddListener(() => AdjustStageVolume(.10f));
            stageAchievementText = CreateText("第二章成就摘要", settingsCard.transform, string.Empty, 18,
                TextAnchor.MiddleCenter, new Color(.55f, .91f, .95f));
            SetRect(stageAchievementText.rectTransform, new Vector2(.08f, .53f), new Vector2(.92f, .64f));
            Text terminalHint = CreateText("第二章终端说明", settingsCard.transform,
                "本地白名单终端  ·  可安全修改测试章节状态", 15,
                TextAnchor.MiddleCenter, new Color(.61f, .78f, .79f));
            SetRect(terminalHint.rectTransform, new Vector2(.10f, .43f), new Vector2(.90f, .51f));
            stageTerminalButton = CreateButton("第二章打开终端", settingsCard.transform, "声 纹 终 端",
                new Color(.20f, .10f, .34f, .99f), new Vector2(.16f, .27f), new Vector2(.84f, .42f));
            stageTerminalButton.onClick.AddListener(OpenStageTerminal);
            stageSettingsBackButton = CreateButton("第二章设置返回", settingsCard.transform, "返 回 暂 停 页",
                new Color(.055f, .30f, .34f, .99f), new Vector2(.16f, .08f), new Vector2(.84f, .23f));
            stageSettingsBackButton.onClick.AddListener(CloseStageSettings);

            stageTerminalPanel = CreatePanel("第二章终端模态层", canvas.transform,
                new Color(.001f, .005f, .010f, .97f), Vector2.zero, Vector2.one);
            GameObject terminalCard = CreatePanel("第二章终端主卡", stageTerminalPanel.transform,
                new Color(.006f, .024f, .031f, .998f), new Vector2(.24f, .19f), new Vector2(.76f, .81f));
            AddOutline(terminalCard, new Color(.32f, .96f, .82f, .76f), 2f);
            Text terminalTitle = CreateText("第二章终端标题", terminalCard.transform, "声 纹 终 端", 40,
                TextAnchor.MiddleCenter, new Color(1f, .75f, .28f));
            terminalTitle.font = displayFont;
            SetRect(terminalTitle.rectTransform, new Vector2(.08f, .78f), new Vector2(.92f, .92f));
            Text terminalHelp = CreateText("第二章终端帮助", terminalCard.transform,
                "exit/zjh  退出游戏    ·    game-two/zjh  保持第二章并重新载入", 16,
                TextAnchor.MiddleCenter, new Color(.72f, .84f, .86f));
            SetRect(terminalHelp.rectTransform, new Vector2(.08f, .64f), new Vector2(.92f, .76f));
            GameObject inputPlate = CreatePanel("第二章终端输入框", terminalCard.transform,
                new Color(.008f, .070f, .068f, .98f), new Vector2(.09f, .46f), new Vector2(.91f, .60f));
            AddOutline(inputPlate, new Color(.30f, .90f, .76f, .52f));
            stageTerminalInput = inputPlate.AddComponent<InputField>();
            stageTerminalInput.targetGraphic = inputPlate.GetComponent<Image>();
            stageTerminalInput.lineType = InputField.LineType.SingleLine;
            stageTerminalInput.characterLimit = 64;
            stageTerminalInput.caretColor = new Color(.72f, 1f, .88f);
            Text inputText = CreateText("第二章终端输入文字", inputPlate.transform, string.Empty, 20,
                TextAnchor.MiddleLeft, new Color(.78f, 1f, .88f));
            SetRect(inputText.rectTransform, new Vector2(.04f, .08f), new Vector2(.96f, .92f));
            Text placeholder = CreateText("第二章终端输入提示", inputPlate.transform, "请输入指令…", 20,
                TextAnchor.MiddleLeft, new Color(.40f, .66f, .61f, .82f));
            SetRect(placeholder.rectTransform, new Vector2(.04f, .08f), new Vector2(.96f, .92f));
            stageTerminalInput.textComponent = inputText;
            stageTerminalInput.placeholder = placeholder;
            stageTerminalOutput = CreateText("第二章终端输出", terminalCard.transform, "等待输入授权指令。", 15,
                TextAnchor.MiddleCenter, new Color(.54f, .84f, .79f));
            SetRect(stageTerminalOutput.rectTransform, new Vector2(.08f, .31f), new Vector2(.92f, .43f));
            stageTerminalSubmitButton = CreateButton("第二章执行终端", terminalCard.transform, "执 行",
                new Color(.045f, .32f, .27f, .99f), new Vector2(.10f, .10f), new Vector2(.47f, .26f));
            stageTerminalSubmitButton.onClick.AddListener(SubmitStageTerminalCommand);
            stageTerminalCloseButton = CreateButton("第二章关闭终端", terminalCard.transform, "返 回",
                new Color(.15f, .14f, .25f, .99f), new Vector2(.53f, .10f), new Vector2(.90f, .26f));
            stageTerminalCloseButton.onClick.AddListener(CloseStageTerminal);

            stagePausePanel.SetActive(false);
            stageSettingsPanel.SetActive(false);
            stageTerminalPanel.SetActive(false);
        }

        private void SetStagePaused(bool value)
        {
            stagePaused = value;
            if (!stagePaused)
            {
                CloseStageTerminal();
                CloseStageSettings();
            }
            Time.timeScale = stagePaused ? 0f : 1f;
            AudioListener.pause = stagePaused;
            if (stagePaused)
            {
                chapterMusicSource?.Pause();
                endingMusicSource?.Pause();
            }
            else
            {
                chapterMusicSource?.UnPause();
                endingMusicSource?.UnPause();
            }
            if (stagePausePanel != null)
            {
                stagePausePanel.SetActive(stagePaused);
                if (stagePaused) stagePausePanel.transform.SetAsLastSibling();
            }
            if (stagePaused) MeixiCursorState.SetPointer();
            else MeixiCursorState.SetGameplay();
            horizontalVelocity = Vector3.zero;
            smoothedLook = Vector2.zero;
        }

        private void OpenStageSettings()
        {
            if (!stagePaused || stageSettingsPanel == null) return;
            stageSettingsOpen = true;
            RefreshStageSettings();
            stageSettingsPanel.SetActive(true);
            stageSettingsPanel.transform.SetAsLastSibling();
        }

        private void CloseStageSettings()
        {
            CloseStageTerminal();
            stageSettingsOpen = false;
            if (stageSettingsPanel != null) stageSettingsPanel.SetActive(false);
            if (stagePaused && stagePausePanel != null) stagePausePanel.transform.SetAsLastSibling();
        }

        private void RefreshStageSettings()
        {
            if (stageVolumeText != null)
                stageVolumeText.text = $"主 音 量    {Mathf.RoundToInt(MeixiLocalProgress.MasterVolume * 100f)}%";
            if (stageAchievementText != null)
                stageAchievementText.text = $"成 就 档 案    {MeixiLocalProgress.UnlockedAchievementCount:00} / {MeixiLocalProgress.AchievementDefinitions.Count:00}";
        }

        private void AdjustStageVolume(float delta)
        {
            MeixiLocalProgress.SetMasterVolume(MeixiLocalProgress.MasterVolume + delta);
            RefreshStageSettings();
        }

        private void OpenStageTerminal()
        {
            if (!stageSettingsOpen || stageTerminalPanel == null) return;
            stageTerminalExecuting = false;
            stageTerminalPanel.SetActive(true);
            stageTerminalPanel.transform.SetAsLastSibling();
            stageTerminalInput.text = string.Empty;
            stageTerminalOutput.text = "等待输入授权指令。";
            stageTerminalOutput.color = new Color(.54f, .84f, .79f);
            StartCoroutine(FocusStageTerminalNextFrame());
        }

        private IEnumerator FocusStageTerminalNextFrame()
        {
            yield return null;
            if (stageTerminalPanel == null || !stageTerminalPanel.activeSelf) yield break;
            EventSystem.current?.SetSelectedGameObject(stageTerminalInput.gameObject);
            stageTerminalInput.Select();
            stageTerminalInput.ActivateInputField();
        }

        private void CloseStageTerminal()
        {
            stageTerminalInput?.DeactivateInputField();
            if (stageTerminalPanel != null) stageTerminalPanel.SetActive(false);
        }

        private void SubmitStageTerminalCommand()
        {
            if (stageTerminalExecuting || stageTerminalInput == null || stageTerminalOutput == null) return;
            string command = (stageTerminalInput.text ?? string.Empty).Trim().ToLowerInvariant();
            if (command == "exit/zjh")
            {
                stageTerminalExecuting = true;
                stageTerminalOutput.text = "指令已接受：正在安全退出游戏……";
                stageTerminalOutput.color = new Color(.54f, 1f, .80f);
                StartCoroutine(QuitStageFromTerminal());
                return;
            }
            if (command == "game-two/zjh")
            {
                stageTerminalExecuting = true;
                MeixiLocalProgress.SetGameplayChapter(2);
                stageTerminalOutput.text = "指令已接受：第二章状态已保存，正在重新载入……";
                stageTerminalOutput.color = new Color(.54f, 1f, .80f);
                StartCoroutine(ReloadStageFromTerminal());
                return;
            }
            stageTerminalOutput.text = command.Length == 0 ? "请输入指令后再执行。" : "未知指令。终端未修改任何游戏数据。";
            stageTerminalOutput.color = command.Length == 0 ? new Color(1f, .70f, .34f) : new Color(1f, .45f, .36f);
            stageTerminalInput.Select();
            stageTerminalInput.ActivateInputField();
            stageTerminalInput.MoveTextEnd(false);
        }

        private IEnumerator QuitStageFromTerminal()
        {
            yield return new WaitForSecondsRealtime(.20f);
            Time.timeScale = 1f;
            AudioListener.pause = false;
            Application.Quit(0);
        }

        private IEnumerator ReloadStageFromTerminal()
        {
            yield return new WaitForSecondsRealtime(.28f);
            SetStagePaused(false);
            SceneManager.LoadScene("MeixiTheatre");
        }

        private void ReturnToVillageFromPause()
        {
            SetStagePaused(false);
            StartCoroutine(ReturnToVillageRoutine());
        }

        private void QuitStageGame()
        {
            Time.timeScale = 1f;
            AudioListener.pause = false;
            MeixiCursorState.SetPointer();
            Application.Quit(0);
        }

        private static bool IsPointerInside(Button button, Vector2 position)
        {
            return button != null && button.interactable && button.gameObject.activeInHierarchy &&
                   RectTransformUtility.RectangleContainsScreenPoint(button.GetComponent<RectTransform>(), position);
        }

        private static Button CreateButton(string name, Transform parent, string label, Color color,
            Vector2 min, Vector2 max)
        {
            GameObject buttonObject = CreatePanel(name, parent, color, min, max);
            Image buttonImage = buttonObject.GetComponent<Image>();
            ApplyUiTexture(buttonImage, "Art/QuestFrameV2");
            buttonImage.color = Color.white;
            Button button = buttonObject.AddComponent<Button>();
            button.targetGraphic = buttonImage;
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(.72f, .93f, 1f, 1f);
            colors.pressedColor = new Color(1f, .67f, .28f, 1f);
            colors.selectedColor = colors.highlightedColor;
            button.colors = colors;
            Text text = CreateText(name + "文字", buttonObject.transform, label, 28, TextAnchor.MiddleCenter, Color.white);
            AddTextShadow(text, Color.black, new Vector2(2f, -2f), false);
            SetRect(text.rectTransform, Vector2.zero, Vector2.one);
            return button;
        }

        private static void PolishStagePanel(GameObject panel, Color edgeColor)
        {
            Shadow shadow = panel.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, .72f);
            shadow.effectDistance = new Vector2(7f, -7f);
            Outline outline = panel.AddComponent<Outline>();
            outline.effectColor = edgeColor;
            outline.effectDistance = new Vector2(1.2f, -1.2f);
        }

        private static void AddTextShadow(Text text, Color color, Vector2 distance, bool outline)
        {
            Shadow shadow = text.gameObject.AddComponent<Shadow>();
            shadow.effectColor = color;
            shadow.effectDistance = distance;
            if (!outline) return;
            Outline edge = text.gameObject.AddComponent<Outline>();
            edge.effectColor = new Color(.04f, .015f, 0f, .90f);
            edge.effectDistance = new Vector2(1.5f, -1.5f);
        }

        private static void ApplyUiTexture(Image image, string resourcePath)
        {
            if (image == null || string.IsNullOrWhiteSpace(resourcePath)) return;
            if (!UiSprites.TryGetValue(resourcePath, out Sprite sprite) || sprite == null)
            {
                Texture2D texture = Resources.Load<Texture2D>(resourcePath);
                if (texture == null) return;
                sprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height),
                    new Vector2(.5f, .5f), 100f);
                UiSprites[resourcePath] = sprite;
            }
            image.sprite = sprite;
            image.preserveAspect = false;
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
            if (stagePaused)
            {
                Time.timeScale = 0f;
                MeixiCursorState.SetPointer();
                return;
            }
            Time.timeScale = 1f;
            if (controller != null) controller.enabled = true;
            smoothedLook = Vector2.zero;
            lookBlockedUntil = Time.unscaledTime + 0.30f;
            if (Time.unscaledTime >= introUntil)
            {
                MeixiCursorState.SetGameplay();
            }
        }

        private void TryStartCaptureMode()
        {
            string[] arguments = Environment.GetCommandLineArgs();
            bool theatreCapture = Array.Exists(arguments, value =>
                string.Equals(value, "-meixiTheatreCapture", StringComparison.OrdinalIgnoreCase));
            bool culturalPreludeCapture = Array.Exists(arguments, value =>
                string.Equals(value, "-meixiMinOperaPreludeCapture", StringComparison.OrdinalIgnoreCase));
            bool endingCapture = Array.Exists(arguments, value =>
                string.Equals(value, "-meixiEndingCapture", StringComparison.OrdinalIgnoreCase));
            bool settingsCapture = Array.Exists(arguments, value =>
                string.Equals(value, "-meixiTheatreSettingsCapture", StringComparison.OrdinalIgnoreCase));
            bool pauseCapture = Array.Exists(arguments, value =>
                string.Equals(value, "-meixiTheatrePauseCapture", StringComparison.OrdinalIgnoreCase));
            if (!theatreCapture && !culturalPreludeCapture && !endingCapture && !settingsCapture && !pauseCapture) return;
            if (culturalPreludeCapture)
            {
                StartCoroutine(CaptureTheatre("MeixiMinOperaPreludeCapture.png", 6.0f));
                return;
            }
            introUntil = Time.unscaledTime + 0.25f;
            if (pauseCapture)
            {
                SetStagePaused(true);
                StartCoroutine(CaptureTheatre("MeixiTheatrePauseCapture.png", .8f));
                return;
            }
            if (settingsCapture)
            {
                SetStagePaused(true);
                OpenStageSettings();
                StartCoroutine(CaptureTheatre("MeixiTheatreSettingsCapture.png", .8f));
                return;
            }
            if (endingCapture)
            {
                ShowEndingMessage();
                StartCoroutine(CaptureTheatre("MeixiEndingCapture.png", 1.1f));
            }
            else StartCoroutine(CaptureTheatre("MeixiTheatreCapture.png", 1.7f));
        }

        private IEnumerator CaptureTheatre(string fileName, float delay)
        {
            yield return new WaitForSecondsRealtime(delay);
            string root = Directory.GetParent(Application.dataPath)?.FullName ?? Application.dataPath;
            string path = Path.Combine(root, fileName);
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
        private int toneProfile;
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
            toneProfile = nameValue.Contains("堂鼓", StringComparison.Ordinal) ? 1 :
                nameValue.Contains("古筝", StringComparison.Ordinal) ? 2 :
                nameValue.Contains("扬琴", StringComparison.Ordinal) ? 3 : 0;
            Initialize();
            // AddComponent invokes Awake before Configure. Refresh the runtime tone
            // after the requested instrument pitch has been assigned.
            if (audioSource != null) audioSource.clip = CreateInstrumentTone(toneProfile, 196f * tonePitch);
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
            audioSource.rolloffMode = AudioRolloffMode.Logarithmic;
            audioSource.dopplerLevel = 0f;
            audioSource.reverbZoneMix = .68f;
            audioSource.volume = .88f;
            audioSource.clip = CreateInstrumentTone(toneProfile, 196f * tonePitch);
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

        private static AudioClip CreateInstrumentTone(int profile, float frequency)
        {
            const int rate = 44100;
            float seconds = profile switch { 0 => 2.8f, 1 => 1.55f, 2 => 2.15f, _ => 1.85f };
            int count = Mathf.CeilToInt(seconds * rate);
            AudioClip clip = AudioClip.Create("古戏台乐器互动音", count, 1, rate, false);
            float[] samples = new float[count];
            float maximum = .001f;
            for (int i = 0; i < count; i++)
            {
                float t = i / (float)rate;
                float attack = Mathf.Clamp01(t * (profile == 1 ? 180f : 95f));
                float phase = t * frequency * Mathf.PI * 2f;
                float sample;
                if (profile == 1)
                {
                    float fallingPitch = phase * (1.12f - Mathf.Min(.18f, t * .22f));
                    float noise = Mathf.Sin(i * 12.9898f + 78.233f) * Mathf.Sin(i * .0173f);
                    sample = (Mathf.Sin(fallingPitch) * .82f + Mathf.Sin(fallingPitch * .49f) * .28f) *
                             Mathf.Exp(-3.6f * t) + noise * Mathf.Exp(-18f * t) * .22f;
                }
                else if (profile == 2)
                {
                    float pluck = Mathf.Exp(-2.15f * t) * attack;
                    sample = (Mathf.Sin(phase) * .62f + Mathf.Sin(phase * 2.01f) * .27f * Mathf.Exp(-1.4f * t) +
                              Mathf.Sin(phase * 3.02f) * .14f * Mathf.Exp(-2.2f * t) +
                              Mathf.Sin(phase * 4.98f) * .07f * Mathf.Exp(-3.1f * t)) * pluck;
                }
                else if (profile == 3)
                {
                    float strike = Mathf.Exp(-2.75f * t) * attack;
                    sample = (Mathf.Sin(phase) * .48f + Mathf.Sin(phase * 2.03f) * .32f +
                              Mathf.Sin(phase * 3.98f) * .18f + Mathf.Sin(phase * 6.08f) * .09f) * strike;
                }
                else
                {
                    float ring = Mathf.Exp(-1.15f * t) * attack;
                    sample = (Mathf.Sin(phase) * .50f + Mathf.Sin(phase * 2.01f) * .25f +
                              Mathf.Sin(phase * 2.72f) * .18f + Mathf.Sin(phase * 4.16f) * .10f) * ring;
                }
                float release = Mathf.Clamp01((seconds - t) * 18f);
                samples[i] = sample * release;
                maximum = Mathf.Max(maximum, Mathf.Abs(samples[i]));
            }
            float normalization = .72f / maximum;
            for (int i = 0; i < samples.Length; i++) samples[i] *= normalization;
            clip.SetData(samples, 0);
            return clip;
        }

    }
}
