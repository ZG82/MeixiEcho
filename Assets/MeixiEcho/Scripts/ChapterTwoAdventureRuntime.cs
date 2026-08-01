using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MeixiEcho
{
    /// <summary>
    /// Chapter two's playable spine after the Min Opera quiz: theatre puzzle,
    /// six-digit gate, blackout transition, puppet chase and mirror maze.
    /// </summary>
    public sealed class ChapterTwoAdventureRuntime : MonoBehaviour
    {
        private enum Phase { WaitingForQuiz, Puzzle, GateSequence, Chase, Ending }

        private const int InventoryCapacity = 5;

        private const string CorrectCode = "582471";
        private static readonly Vector3[] PuppetVisibilityOffsets =
        {
            new(0f, .28f, 0f), new(0f, .82f, 0f), new(0f, 1.30f, 0f), new(0f, 1.92f, 0f),
            new(-.43f, 1.45f, 0f), new(.43f, 1.45f, 0f), new(-.22f, .45f, 0f), new(.22f, .45f, 0f)
        };
        private readonly string[] instrumentOrder = { "古筝", "编钟", "堂鼓", "扬琴" };
        private readonly List<InventoryItem> inventory = new();
        private readonly List<DroppedItemPickup> droppedPickups = new();
        private readonly List<TheatreInstrument> instruments = new();
        private readonly List<ChapterTwoSetProp> setProps = new();
        private readonly List<PuppetAgent> puppets = new();
        private readonly List<CeilingPuppetEasterEgg> ceilingPuppets = new();
        private readonly List<LightState> theatreLights = new();
        private readonly List<Vector3> chaseTrail = new();
        private readonly List<Vector3> primaryPuppetPath = new();
        private readonly List<Vector3> alternatePuppetPath = new();
        private readonly List<ChapterTwoInteractableHighlight> interactableMarkers = new();
        private readonly List<TheatreSwingDoor> swingDoors = new();
        private readonly Dictionary<ChapterTwoInteractableHighlight, Collider> interactableFocusColliders = new();

        private TheatreStageRuntime stageRuntime;
        private CharacterController controller;
        private Transform player;
        private Camera viewCamera;
        private Canvas canvas;
        private Text objectiveText;
        private Text hintText;
        private Text inventoryText;
        private Text inventoryFullText;
        private Text inventorySelectionTitle;
        private Text inventorySelectionKind;
        private Text inspectTitle;
        private Text inspectBody;
        private Text keypadDisplay;
        private Text keypadStatus;
        private Text defeatReturnText;
        private Text staminaText;
        private Image staminaFill;
        private Image inventorySelectionIcon;
        private Image inspectIcon;
        private RawImage inspectBlurBackground;
        private RawImage inspectObjectViewport;
        private RawImage inspectPaperBackground;
        private Font inspectDefaultFont;
        private Font inspectHandwritingFont;
        private readonly Button[] inventorySlotButtons = new Button[InventoryCapacity];
        private readonly Image[] inventorySlotIcons = new Image[InventoryCapacity];
        private readonly Image[] inventorySlotFrames = new Image[InventoryCapacity];
        private readonly Image[] inventorySlotSelectionLines = new Image[InventoryCapacity];
        private readonly Text[] inventorySlotTitles = new Text[InventoryCapacity];
        private readonly Image[] hudSlotIcons = new Image[InventoryCapacity];
        private readonly Image[] hudSlotFrames = new Image[InventoryCapacity];
        private readonly Text[] keypadDigits = new Text[6];
        private readonly List<KeypadPointerBinding> keypadPointerBindings = new();
        private readonly RaycastHit[] inspectRayHits = new RaycastHit[16];
        private readonly Dictionary<string, Sprite> inventoryIconCache = new();
        private CanvasGroup fadeGroup;
        private CanvasGroup hintGroup;
        private CanvasGroup inventoryHudGroup;
        private GameObject hintCard;
        private GameObject inventoryHud;
        private GameObject inventoryPanel;
        private GameObject inspectPanel;
        private GameObject keypadPanel;
        private GameObject defeatPanel;
        private GameObject staminaPanel;
        private Button inventoryCloseButton;
        private Button inventoryInspectButton;
        private Button inspectCloseButton;
        private GameObject sheetObject;
        private GameObject chairObject;
        private GameObject wallClueObject;
        private GameObject heritageMuralObject;
        private GameObject heritageMuralHighlightFrame;
        private GameObject flashlightObject;
        private GameObject lubanLockObject;
        private GameObject doorObject;
        private GameObject doorLockVisual;
        private Transform doorAssemblyRoot;
        private GameObject mazeExitObject;
        private readonly List<GameObject> mazeWorldLabels = new();
        private TextMesh wallClueText;
        private Renderer heritageReplacedMuralRenderer;
        private Texture2D heritageMuralTexture;
        private Light flashlight;
        private Light flashlightFill;
        private AudioSource footstepSource;
        private AudioClip[] footstepClips;
        private AudioSource effectsSource;
        private AudioSource heartbeatSource;
        private AudioSource puppetBgmSource;
        private AudioClip heartbeatClip;
        private AudioClip earthquakeClip;
        private AudioClip blackoutClip;
        private AudioClip doorClip;
        private AudioClip flashlightSwitchClip;
        private AudioClip puppetBgmClip;
        private readonly HashSet<AudioSource> fadingBgmSources = new();
        private RawImage heartbeatVignette;
        private Texture2D heartbeatVignetteTexture;
        private Phase phase;
        private bool armed;
        private bool suppressPuppetCatchForValidation;
        private bool inputModal;
        private bool sheetCollected;
        private bool chairSolved;
        private bool wallSolved;
        private bool flashlightCollected;
        private bool flashlightRevealPlaying;
        private bool heritageMuralVisible;
        private bool heritageAchievementClaimed;
        private bool mazeLabelsVisible;
        private bool instrumentsSolved;
        private bool lubanCollected;
        private bool lubanSolved;
        private int instrumentProgress;
        private int selectedInventoryIndex = -1;
        private GameObject focusedTarget;
        private ChapterTwoInteractableHighlight focusedHighlight;
        private Transform heldItemRoot;
        private GameObject heldItemVisual;
        private Vector3 heldItemBasePosition;
        private Quaternion heldItemBaseRotation;
        private Camera inspectModelCamera;
        private Transform inspectModelRoot;
        private Light inspectKeyLight;
        private Light inspectFillLight;
        private Light inspectRimLight;
        private RenderTexture inspectModelTexture;
        private RenderTexture inspectBlurTexture;
        private Texture2D inspectBlurImage;
        private Material inspectBlurMaterial;
        private string inspectingItemId = string.Empty;
        private int lubanProgress;
        private float inspectYaw;
        private float inspectPitch;
        private float inspectDistance = 5.4f;
        private float inspectTargetDistance = 5.4f;
        private float inspectMinimumDistance = 2.2f;
        private float inspectMaximumDistance = 7.2f;
        private Vector3 inspectFocusPoint;
        private Vector2 inspectAngularVelocity;
        private Vector2 inspectDragStart;
        private Vector2 inspectDragPrevious;
        private bool inspectDragging;
        private ChapterTwoLubanBar hoveredLubanBar;
        private Transform inspectLubanLidPivot;
        private Coroutine inspectLubanSolveAnimation;
        private string keypadValue = string.Empty;
        private Button lastKeypadPointerButton;
        private float lastKeypadPointerActionAt = float.NegativeInfinity;
        private string transientMessage = string.Empty;
        private float messageUntil;
        private float inventoryHudVisibleUntil;
        private bool gameplayCursorResumePending;
        private float stamina = 1f;
        private float stepClock;
        private float yaw;
        private float pitch;
        private float verticalVelocity;
        private Vector3 cameraShake;
        private Vector3 lastChaseStepPosition;
        private float hudRefreshClock;
        private Vector3 firstPersonCameraLocal;
        private Transform firstPersonHead;
        private Animator playerAnimator;
        private readonly RaycastHit[] chaseCameraHits = new RaycastHit[10];
        private Vector3 lastTrailPosition;
        private MazeGraph maze;
        private ReflectionProbe mazeReflectionProbe;
        private Camera mazeReflectionCamera;
        private const int MazeMirrorOverlayLayer = 29;
        private const int PlayerVisualLayer = 28;
        private readonly List<MazeMirrorSurface> mazeMirrorSurfaces = new();
        private int mazeReflectionCursor;
        private float mazeReflectionNextUpdateAt;
        private const int MazeReflectionResolution = 192;
        private const int MazeReflectionRecursionResolution = 80;
        private const int MazeReflectionUpdatesPerFrame = 1;
        private const float MazeReflectionUpdateInterval = .016f;
        private const float MazeReflectionPrewarmInterval = .085f;
        private const float MazeReflectionPrewarmDistance = 20f;
        private const float MazeReflectionRecursiveInterval = .14f;
        private const int MazeReflectionRecursionDepth = 2;
        private int mazeReflectionBaseMask;
        private float mazeReflectionNextPrewarmAt;
        private ChapterTwoMazeSoulEchoPickup mazeSoulEcho;
        private bool mazeExitVisited;
        private bool mazeSoulCollected;
        private float mazeExitHintNextAt;
        private float staminaLastDrainAt = float.NegativeInfinity;
        private float puppetPathRefreshAt;
        private readonly Vector3[] puppetEntryLastPositions = new Vector3[2];
        private readonly float[] puppetEntryStalledSince =
            { float.NegativeInfinity, float.NegativeInfinity };
        private readonly Vector3[] puppetLowerStairLastPositions = new Vector3[2];
        private readonly float[] puppetLowerStairStalledSince =
            { float.NegativeInfinity, float.NegativeInfinity };

        private sealed class InventoryItem
        {
            public string id;
            public string title;
            public string kind;
            public string description;
            public Sprite icon;
        }

        private sealed class DroppedItemPickup
        {
            public GameObject world;
            public InventoryItem item;
        }

        private sealed class KeypadPointerBinding
        {
            public Button button;
            public Action action;
        }

        public bool IsArmed => armed;
        public bool IsTakingOverLegacyInteraction => armed && phase == Phase.Puzzle;
        public bool IsInputModalOpen => inputModal;
        public bool IsFirstPersonActive => phase == Phase.Chase;

        private struct LightState
        {
            public Light light;
            public float intensity;
            public bool enabled;
        }

        private sealed class MazeMirrorSurface
        {
            public Renderer renderer;
            public Transform transform;
            public Vector3 normal;
            public RenderTexture texture;
            public RenderTexture[] recursionTextures;
            public Material material;
            public float lastRenderedAt = float.NegativeInfinity;
            public float lastRecursiveRenderedAt = float.NegativeInfinity;
        }

        private void Awake()
        {
            phase = Phase.WaitingForQuiz;
            stageRuntime = GetComponent<TheatreStageRuntime>();
            controller = FindAnyObjectByType<CharacterController>();
            player = controller != null ? controller.transform : null;
            viewCamera = Camera.main ?? FindAnyObjectByType<Camera>();
            footstepSource = gameObject.AddComponent<AudioSource>();
            footstepSource.playOnAwake = false;
            footstepSource.spatialBlend = .12f;
            effectsSource = gameObject.AddComponent<AudioSource>();
            effectsSource.playOnAwake = false;
            effectsSource.spatialBlend = 0f;
            heartbeatSource = gameObject.AddComponent<AudioSource>();
            heartbeatSource.playOnAwake = false;
            heartbeatSource.loop = true;
            heartbeatSource.spatialBlend = 0f;
            heartbeatSource.ignoreListenerPause = true;
            puppetBgmSource = gameObject.AddComponent<AudioSource>();
            puppetBgmSource.playOnAwake = false;
            puppetBgmSource.loop = true;
            puppetBgmSource.spatialBlend = 0f;
            LoadChapterTwoAudio();
            EnsureAdventureSet();
            BuildAdventureUi();
            foreach (TheatreInstrument item in FindObjectsByType<TheatreInstrument>())
            {
                instruments.Add(item);
                EnsureInteractableMarker(item.gameObject, "试奏" + item.DisplayName);
            }
            foreach (TheatreSwingDoor door in FindObjectsByType<TheatreSwingDoor>())
            {
                swingDoors.Add(door);
                EnsureInteractableMarker(door.gameObject, door.InteractionPrompt);
            }
            foreach (ChapterTwoSetProp prop in FindObjectsByType<ChapterTwoSetProp>())
            {
                if (prop == null || string.IsNullOrEmpty(prop.Prompt)) continue;
                setProps.Add(prop);
                EnsureInteractableMarker(prop.gameObject, prop.Prompt);
            }
            EnsureInteractableMarker(flashlightObject, "拾取手电筒");
            EnsureInteractableMarker(sheetObject, "拾取残谱");
            EnsureInteractableMarker(chairObject, "检查椅背");
            EnsureInteractableMarker(wallClueObject, "检查墙面");
            EnsureInteractableMarker(lubanLockObject, "拾取榫匣");
            EnsureInteractableMarker(doorObject, "操作六位门锁");
            EnsureInteractableMarker(doorLockVisual, "操作六位门锁");
            if (IsAdventureValidationRun())
            {
                StartCoroutine(ArmNextFrame());
                StartCoroutine(CaptureValidationFrame());
            }
        }

        private void Start()
        {
            // TheatreStageRuntime can create the legacy theatre shell before or
            // after this component's Awake. Reapply the gate facade cleanup once
            // all scene Awake methods have completed so late-created lintels and
            // front-wall segments cannot remain visible behind the lift door.
            OpenLegacyFrontWallForAdventureGate();
            if (MeixiLocalProgress.IsCheckpoint("ChapterTwoGateOpened"))
                StartCoroutine(ResumeGateCheckpointRoutine());
        }

        private IEnumerator ResumeGateCheckpointRoutine()
        {
            yield return null;
            if (phase == Phase.Ending || player == null) yield break;

            armed = true;
            phase = Phase.Puzzle;
            inputModal = true;
            if (stageRuntime != null)
            {
                stageRuntime.HideLegacyPresentationForAdventure();
                stageRuntime.SetSharedGameplayControlsActive(false);
                stageRuntime.enabled = false;
            }
            if (doorLockVisual != null) doorLockVisual.SetActive(false);
            if (doorAssemblyRoot != null)
            {
                doorAssemblyRoot.localPosition += Vector3.up * 4.6f;
                foreach (Collider collider in doorAssemblyRoot.GetComponentsInChildren<Collider>(true))
                    collider.enabled = false;
            }
            Physics.SyncTransforms();
            SpawnPuppets();
            BlackoutTheatre();
            PlacePlayerAtStairEntry();
            BeginChase();
            SetMessage("已读取大门开启存档 · 无需重新输入密码", 3.6f);
            Debug.Log("[ChapterTwoAdventure] Gate checkpoint restored; chase resumed without keypad.");
        }

        private IEnumerator ArmNextFrame()
        {
            yield return null;
            ArmAfterQuiz();
        }

        public void ArmAfterQuiz()
        {
            if (armed || player == null) return;
            armed = true;
            phase = Phase.Puzzle;
            RevealInventoryHud(2.8f);
            objectiveText.text = "二楼 · 搜查观众席与后台，拼合守谱人的记录";
            SetMessage("闽剧的余音散去，侧廊深处亮起了几盏旧灯", 3.8f);
            MeixiCursorState.SetGameplay();
            Debug.Log("[ChapterTwoAdventure] Puzzle phase armed after Min Opera quiz.");
        }

        private static bool IsAdventureValidationRun()
        {
            foreach (string argument in Environment.GetCommandLineArgs())
                if (argument.Contains("ChapterTwoAdventure", StringComparison.OrdinalIgnoreCase) ||
                    argument.Contains("ChapterTwoMaze", StringComparison.OrdinalIgnoreCase) ||
                    argument.Contains("ChapterTwoInventory", StringComparison.OrdinalIgnoreCase) ||
                    argument.Contains("ChapterTwoInspect", StringComparison.OrdinalIgnoreCase) ||
                    argument.Contains("ChapterTwoLuban", StringComparison.OrdinalIgnoreCase) ||
                    argument.Contains("ChapterTwoHandwriting", StringComparison.OrdinalIgnoreCase) ||
                    argument.Contains("ChapterTwoFlashlight", StringComparison.OrdinalIgnoreCase) ||
                    argument.Contains("ChapterTwoRooms", StringComparison.OrdinalIgnoreCase) ||
                    argument.Contains("ChapterTwoPoster", StringComparison.OrdinalIgnoreCase) ||
                    argument.Contains("ChapterTwoShell", StringComparison.OrdinalIgnoreCase) ||
                    argument.Contains("ChapterTwoPuppet", StringComparison.OrdinalIgnoreCase) ||
                    argument.Contains("ChapterTwoKeypad", StringComparison.OrdinalIgnoreCase) ||
                    argument.Contains("ChapterTwoStair", StringComparison.OrdinalIgnoreCase) ||
                    argument.Contains("ChapterTwoGate", StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }

        private IEnumerator CaptureValidationFrame()
        {
            string[] arguments = Environment.GetCommandLineArgs();
            bool mazeCapture = Array.Exists(arguments, value =>
                string.Equals(value, "-meixiChapterTwoMazeCapture", StringComparison.OrdinalIgnoreCase));
            bool mazeSoulCapture = Array.Exists(arguments, value =>
                string.Equals(value, "-meixiChapterTwoMazeSoulCapture", StringComparison.OrdinalIgnoreCase));
            bool inventoryCapture = Array.Exists(arguments, value =>
                string.Equals(value, "-meixiChapterTwoInventoryCapture", StringComparison.OrdinalIgnoreCase));
            bool keypadCapture = Array.Exists(arguments, value =>
                string.Equals(value, "-meixiChapterTwoKeypadCapture", StringComparison.OrdinalIgnoreCase));
            bool keypadPointerSmoke = Array.Exists(arguments, value =>
                string.Equals(value, "-meixiChapterTwoKeypadPointerSmoke", StringComparison.OrdinalIgnoreCase));
            bool inspectCapture = Array.Exists(arguments, value =>
                string.Equals(value, "-meixiChapterTwoInspectCapture", StringComparison.OrdinalIgnoreCase));
            bool lubanCapture = Array.Exists(arguments, value =>
                string.Equals(value, "-meixiChapterTwoLubanCapture", StringComparison.OrdinalIgnoreCase));
            bool handwritingCapture = Array.Exists(arguments, value =>
                string.Equals(value, "-meixiChapterTwoHandwritingCapture", StringComparison.OrdinalIgnoreCase));
            bool flashlightCapture = Array.Exists(arguments, value =>
                string.Equals(value, "-meixiChapterTwoFlashlightCapture", StringComparison.OrdinalIgnoreCase));
            bool roomsCapture = Array.Exists(arguments, value =>
                string.Equals(value, "-meixiChapterTwoRoomsCapture", StringComparison.OrdinalIgnoreCase));
            bool posterCapture = Array.Exists(arguments, value =>
                string.Equals(value, "-meixiChapterTwoPosterCapture", StringComparison.OrdinalIgnoreCase));
            bool shellCapture = Array.Exists(arguments, value =>
                string.Equals(value, "-meixiChapterTwoShellCapture", StringComparison.OrdinalIgnoreCase));
            bool stairCapture = Array.Exists(arguments, value =>
                string.Equals(value, "-meixiChapterTwoStairCapture", StringComparison.OrdinalIgnoreCase));
            bool gateCapture = Array.Exists(arguments, value =>
                string.Equals(value, "-meixiChapterTwoGateCapture", StringComparison.OrdinalIgnoreCase));
            bool puppetCapture = Array.Exists(arguments, value =>
                string.Equals(value, "-meixiChapterTwoPuppetCapture", StringComparison.OrdinalIgnoreCase));
            bool puppetStairCapture = Array.Exists(arguments, value =>
                string.Equals(value, "-meixiChapterTwoPuppetStairCapture", StringComparison.OrdinalIgnoreCase));
            float validationDelay = flashlightCapture ? 5.0f :
                mazeCapture || inventoryCapture || keypadCapture || keypadPointerSmoke || inspectCapture ||
                mazeSoulCapture || lubanCapture || handwritingCapture || roomsCapture || posterCapture || shellCapture || stairCapture || gateCapture || puppetCapture || puppetStairCapture ? 2.0f : 4.5f;
            yield return new WaitForSecondsRealtime(validationDelay);
            if (puppetStairCapture)
            {
                flashlightCollected = true;
                SpawnPuppets();
                BlackoutTheatre();
                controller.enabled = false;
                player.position = new Vector3(0f, -7.23f, -26.58f);
                // Face away from the test route; otherwise the validation camera
                // legitimately freezes the puppets by looking directly at them.
                player.rotation = Quaternion.Euler(0f, 270f, 0f);
                if (puppets.Count > 0)
                    puppets[0].root.position = new Vector3(-1.55f, -.24f, -14.18f);
                if (puppets.Count > 1)
                    puppets[1].root.position = new Vector3(1.55f, -3.66f, -21.62f);
                controller.enabled = true;
                BeginChase();
                suppressPuppetCatchForValidation = true;
                pitch = 80f;
                viewCamera.enabled = false;
                Physics.SyncTransforms();
                yield return new WaitForSecondsRealtime(9.0f);
                bool firstReachedMaze = puppets.Count > 0 && puppets[0].root.position.z < -26.0f &&
                    puppets[0].root.position.y < -5.0f;
                bool secondReachedMaze = puppets.Count > 1 && puppets[1].root.position.z < -26.0f &&
                    puppets[1].root.position.y < -5.0f;
                bool passed = firstReachedMaze && secondReachedMaze;
                Debug.Log("[ChapterTwoPuppetStair] first=" +
                          (puppets.Count > 0 ? puppets[0].root.position.ToString() : "missing") +
                          " second=" + (puppets.Count > 1 ? puppets[1].root.position.ToString() : "missing") +
                          " result=" + (passed ? "PASS" : "FAIL"));
                if (!Application.isEditor) Application.Quit(passed ? 0 : 4);
                yield break;
            }
            else if (puppetCapture)
            {
                flashlightCollected = true;
                SpawnPuppets();
                BlackoutTheatre();
                controller.enabled = false;
                player.position = new Vector3(0f, .08f, 4.65f);
                player.rotation = Quaternion.identity;
                controller.enabled = true;
                BeginChase();
                yield return new WaitForSecondsRealtime(1.1f);
            }
            else if (mazeCapture)
            {
                sheetCollected = chairSolved = wallSolved = flashlightCollected = instrumentsSolved = true;
                SpawnPuppets();
                BlackoutTheatre();
                controller.enabled = false;
                // Start at the north-cell threshold so the validation frame
                // covers the actual stair-to-maze handoff and its opening.
                player.position = new Vector3(0f, -7.16f, -26.72f);
                player.rotation = Quaternion.Euler(0f, 180f, 0f);
                controller.enabled = true;
                BeginChase();
                yield return new WaitForSecondsRealtime(1.2f);
            }
            else if (mazeSoulCapture)
            {
                sheetCollected = chairSolved = wallSolved = flashlightCollected = instrumentsSolved = true;
                SpawnPuppets();
                BlackoutTheatre();
                controller.enabled = false;
                Vector3 exitPosition = mazeExitObject != null ? mazeExitObject.transform.position :
                    new Vector3(-10.08f, -6.03f, -52.82f);
                player.position = new Vector3(exitPosition.x, maze.floorY + .35f, exitPosition.z - .42f);
                player.rotation = Quaternion.Euler(0f, 180f, 0f);
                controller.enabled = true;
                BeginChase();
                yield return new WaitForSecondsRealtime(.25f);
                CheckMazeExit();
                bool firstExitTriggered = mazeExitVisited && mazeSoulEcho != null && !mazeSoulCollected;
                if (mazeSoulEcho != null) CollectMazeSoulEcho(mazeSoulEcho);
                bool soulCollected = mazeSoulCollected;
                CheckMazeExit();
                Debug.Log("[ChapterTwoAdventure] Maze soul validation firstExit=" + firstExitTriggered +
                          " collected=" + soulCollected + " secondExitUnlock=" + (phase == Phase.Ending));
                yield return new WaitForSecondsRealtime(.35f);
            }
            else if (inventoryCapture)
            {
                PopulateValidationInventory();
                ToggleInventory();
                yield return new WaitForSecondsRealtime(.8f);
            }
            else if (keypadCapture || keypadPointerSmoke)
            {
                keypadValue = "582";
                OpenKeypad();
                yield return new WaitForSecondsRealtime(.8f);
                if (keypadPointerSmoke)
                {
                    Debug.Log("[ChapterTwoAdventure] Keypad pointer smoke mode ready; window will remain open for UI automation.");
                    yield break;
                }
            }
            else if (inspectCapture)
            {
                PopulateValidationInventory();
                ShowInspection(inventory[Mathf.Min(4, inventory.Count - 1)]);
                yield return new WaitForSecondsRealtime(.8f);
            }
            else if (lubanCapture)
            {
                PopulateValidationInventory();
                ShowInspection(inventory.Find(value => value.id == "luban"));
                yield return new WaitForSecondsRealtime(.8f);
                for (int expected = 0; expected < 4; expected++)
                {
                    Vector2 click = GetLubanValidationScreenPoint(expected);
                    ChapterTwoLubanBar resolved = FindInspectBar(click);
                    if (resolved == null || resolved.Index != expected)
                    {
                        Debug.LogError("[ChapterTwoAdventure] Luban click mapping failed at bar " + (expected + 1));
                        break;
                    }
                    TryInspectObjectClick(click);
                    yield return new WaitForSecondsRealtime(.32f);
                }
                yield return new WaitForSecondsRealtime(1.05f);
                Debug.Log("[ChapterTwoAdventure] Luban validation solved=" + lubanSolved + " progress=" + lubanProgress);
            }
            else if (handwritingCapture)
            {
                PopulateValidationInventory();
                ShowInspection(inventory[Mathf.Min(1, inventory.Count - 1)]);
                yield return new WaitForSecondsRealtime(.8f);
            }
            else if (flashlightCapture)
            {
                BeginFlashlightPickup();
                yield return new WaitForSecondsRealtime(2.50f);
            }
            else if (roomsCapture)
            {
                foreach (ChapterTwoSetProp prop in setProps)
                    if (prop != null) prop.Interact();
                Debug.Log("[ChapterTwoAdventure] Interactive set props registered: " + setProps.Count);
                yield return new WaitForSecondsRealtime(3.8f);
                TheatreSwingDoor validationDoor = null;
                float validationDoorDistance = float.PositiveInfinity;
                foreach (TheatreSwingDoor door in swingDoors)
                {
                    if (door == null) continue;
                    float distance = Vector3.SqrMagnitude(door.transform.position - new Vector3(-8.52f, 1.10f, 12.20f));
                    if (distance < validationDoorDistance)
                    {
                        validationDoorDistance = distance;
                        validationDoor = door;
                    }
                }
                if (validationDoor != null && !validationDoor.IsOpen) validationDoor.Toggle();
                if (stageRuntime != null)
                {
                    stageRuntime.SetSharedGameplayControlsActive(false);
                    stageRuntime.enabled = false;
                }
                viewCamera.transform.SetParent(null, true);
                viewCamera.fieldOfView = 70f;
                viewCamera.transform.position = new Vector3(-8.72f, 2.72f, 8.45f);
                viewCamera.transform.rotation = Quaternion.LookRotation(
                    new Vector3(-9.85f, 1.72f, 12.80f) - viewCamera.transform.position, Vector3.up);
                if (focusedHighlight != null) focusedHighlight.SetHighlighted(false);
                focusedHighlight = null;
                focusedTarget = null;
                SetHintVisible(false, string.Empty);
            }
            else if (posterCapture || shellCapture)
            {
                if (stageRuntime != null)
                {
                    stageRuntime.SetSharedGameplayControlsActive(false);
                    stageRuntime.enabled = false;
                }
                viewCamera.transform.SetParent(null, true);
                viewCamera.fieldOfView = shellCapture ? 56f : 48f;
                viewCamera.transform.position = shellCapture
                    ? new Vector3(22.5f, 10.4f, 23.8f)
                    : new Vector3(3.8f, 4.35f, -4.2f);
                Vector3 target = shellCapture
                    ? new Vector3(11.5f, 4.1f, 13.6f)
                    : new Vector3(9.62f, 4.45f, -.4f);
                viewCamera.transform.rotation = Quaternion.LookRotation(target - viewCamera.transform.position, Vector3.up);
                if (focusedHighlight != null) focusedHighlight.SetHighlighted(false);
                focusedHighlight = null;
                focusedTarget = null;
                SetHintVisible(false, string.Empty);
                yield return new WaitForSecondsRealtime(1.0f);
            }
            else if (stairCapture)
            {
                if (stageRuntime != null)
                {
                    stageRuntime.SetSharedGameplayControlsActive(false);
                    stageRuntime.enabled = false;
                }
                // Stair validation uses work light so the collision geometry and
                // sealed shaft can be inspected; normal gameplay still calls
                // BlackoutTheatre from the gate sequence.
                RenderSettings.ambientIntensity = .42f;
                viewCamera.transform.SetParent(null, true);
                viewCamera.fieldOfView = 62f;
                viewCamera.transform.position = new Vector3(0f, 2.05f, -13.78f);
                Vector3 target = new Vector3(-1.55f, -1.55f, -17.75f);
                viewCamera.transform.rotation = Quaternion.LookRotation(target - viewCamera.transform.position, Vector3.up);
                if (focusedHighlight != null) focusedHighlight.SetHighlighted(false);
                focusedHighlight = null;
                focusedTarget = null;
                SetHintVisible(false, string.Empty);
                yield return new WaitForSecondsRealtime(1.0f);
            }
            else if (gateCapture)
            {
                if (stageRuntime != null)
                {
                    stageRuntime.SetSharedGameplayControlsActive(false);
                    stageRuntime.enabled = false;
                }
                viewCamera.transform.SetParent(null, true);
                viewCamera.fieldOfView = 58f;
                viewCamera.transform.position = new Vector3(0f, 3.15f, -14.55f);
                viewCamera.transform.rotation = Quaternion.LookRotation(
                    new Vector3(0f, 3.15f, -12.20f) - viewCamera.transform.position, Vector3.up);
                if (focusedHighlight != null) focusedHighlight.SetHighlighted(false);
                focusedHighlight = null;
                focusedTarget = null;
                SetHintVisible(false, string.Empty);
                yield return new WaitForSecondsRealtime(1.0f);
            }
            yield return SampleValidationPerformance(roomsCapture ? "rooms" : lubanCapture ? "luban" : mazeSoulCapture ? "maze-soul" : posterCapture ? "posters" :
                shellCapture ? "shell" : stairCapture ? "staircase" : puppetCapture ? "puppets" :
                gateCapture ? "gate" : mazeCapture ? "maze" : "chapter-two", .9f);
            string fileName = puppetCapture ? "ChapterTwoPuppetValidation.png" :
                mazeCapture ? "ChapterTwoMazeValidation.png" :
                mazeSoulCapture ? "ChapterTwoMazeSoulValidation.png" :
                inventoryCapture ? "ChapterTwoInventoryValidation.png" :
                keypadCapture ? "ChapterTwoKeypadValidation.png" :
                inspectCapture ? "ChapterTwoInspectValidation.png" :
                lubanCapture ? "ChapterTwoLubanValidation.png" :
                handwritingCapture ? "ChapterTwoHandwritingValidation.png" :
                flashlightCapture ? "ChapterTwoFlashlightValidation.png" :
                posterCapture ? "ChapterTwoPosterValidation.png" :
                shellCapture ? "ChapterTwoShellValidation.png" :
                stairCapture ? "ChapterTwoStairValidation.png" :
                gateCapture ? "ChapterTwoGateValidation.png" :
                roomsCapture ? "ChapterTwoRoomsValidation.png" : "ChapterTwoPuzzleValidation.png";
            string path = Path.GetFullPath(Path.Combine(Application.dataPath, "..", fileName));
            if (mazeCapture || mazeSoulCapture || stairCapture || gateCapture) CaptureMazeCameraToFile(path);
            else CaptureCameraToFile(path);
            Debug.Log("[ChapterTwoAdventure] Validation screenshot requested: " + path);
            yield return new WaitForSecondsRealtime(1.2f);
            if (!Application.isEditor) Application.Quit(0);
        }

        private void CaptureCameraToFile(string path)
        {
            ScreenCapture.CaptureScreenshot(path, 1);
        }

        private void CaptureMazeCameraToFile(string path)
        {
            Camera captureCamera = viewCamera != null ? viewCamera : Camera.main;
            if (captureCamera == null)
            {
                Debug.LogError("[ChapterTwoAdventure] Maze validation capture has no camera.");
                return;
            }

            const int width = 1920;
            const int height = 1080;
            RenderTexture previousTarget = captureCamera.targetTexture;
            RenderTexture previousActive = RenderTexture.active;
            RenderTexture target = RenderTexture.GetTemporary(width, height, 24, RenderTextureFormat.ARGB32);
            Texture2D image = new(width, height, TextureFormat.RGB24, false);
            try
            {
                captureCamera.targetTexture = target;
                captureCamera.Render();
                RenderTexture.active = target;
                image.ReadPixels(new Rect(0f, 0f, width, height), 0, 0, false);
                image.Apply(false, false);
                File.WriteAllBytes(path, image.EncodeToPNG());
            }
            finally
            {
                captureCamera.targetTexture = previousTarget;
                RenderTexture.active = previousActive;
                RenderTexture.ReleaseTemporary(target);
                Destroy(image);
            }
        }

        private IEnumerator SampleValidationPerformance(string label, float duration)
        {
            float started = Time.realtimeSinceStartup;
            float elapsed = 0f;
            float accumulated = 0f;
            float worst = 0f;
            int frames = 0;
            while (elapsed < duration)
            {
                yield return null;
                float frame = Mathf.Min(Time.unscaledDeltaTime, .25f);
                accumulated += frame;
                worst = Mathf.Max(worst, frame);
                frames++;
                elapsed = Time.realtimeSinceStartup - started;
            }
            float averageMilliseconds = frames > 0 ? accumulated / frames * 1000f : 0f;
            float averageFps = accumulated > .0001f ? frames / accumulated : 0f;
            Debug.Log($"[ChapterTwoPerformance] {label} avg={averageMilliseconds:F2}ms " +
                      $"fps={averageFps:F1} worst={worst * 1000f:F2}ms frames={frames}");
        }

        private void PopulateValidationInventory()
        {
            if (inventory.Count > 0) return;
            Collect("flashlight", "手电筒", "工具", "一支沉甸甸的旧式手电。电池仓里留着新近更换过的痕迹。", false);
            Collect("score", "残谱页", "关键物件", "民国二十四年 · 七月初九\n\n今晚仍按旧次序试音：\n古筝 → 编钟 → 堂鼓 → 扬琴\n\n四枚褪色朱印压在折痕上。右下角的 M-17，与溪流边发现的异常声纹相同。", false);
            Collect("chair", "椅背纸片", "线索", "从铜扣后抽出的薄纸。\n\n正面只有被反复描粗的：\n5 · 8\n\n背面写着：\n“木梁下，坐过的人先开口。不要相信谢幕后还站着的影子。”", false);
            Collect("wall", "墙面拓记", "线索", "手电斜光穿过旧纸，露出被烟灰遮住的两道数痕：\n2 · 4\n\n旁边极浅的小字写着：\n“我守的从来不是曲子，是那些被戏台带走、却无人敢认领的记忆。”", false);
            Collect("luban", "卯榫匣", "机关物件", "四根互锁木梁组成的鲁班锁。榫头没有明显的起点，拆解顺序只能从结构本身找。", false);
        }

        private void Update()
        {
            if (!armed) return;
            if (flashlightCollected)
            {
                RevealHeritageMural();
                UpdateCeilingPuppets();
            }
            Keyboard keyboard = Keyboard.current;
            if (inputModal)
            {
                bool pointerModal = (defeatPanel != null && defeatPanel.activeSelf) ||
                    (phase == Phase.Puzzle && keypadPanel != null && keypadPanel.activeSelf) ||
                    (phase == Phase.Puzzle && inventoryPanel != null &&
                     (inventoryPanel.activeSelf || inspectPanel.activeSelf));
                if (pointerModal) MeixiCursorState.SetPointer();
                else MeixiCursorState.SetGameplay();
                HandleModalInput(keyboard);
                return;
            }
            if (phase == Phase.Ending) return;
            MeixiCursorState.SetGameplay();
            if (phase == Phase.Puzzle && keyboard != null && keyboard.iKey.wasPressedThisFrame) ToggleInventory();
            if (phase == Phase.Puzzle && keyboard != null && keyboard.tabKey.wasPressedThisFrame && inventory.Count > 0)
                ShowInspection(inventory[Mathf.Clamp(selectedInventoryIndex, 0, inventory.Count - 1)]);
            if (inputModal) return;

            if (phase == Phase.Puzzle)
            {
                ResumePuzzleCursorFromSceneClick();
                UpdateFocusedInteractable();
                UpdateInventoryInput(keyboard);
                UpdatePuzzleInteraction(keyboard);
                UpdateWallClueGlow();
                UpdatePuzzleHud();
                UpdateInventoryHudFade();
            }
            else if (phase == Phase.Chase)
            {
                UpdateFirstPersonMovement(keyboard);
                UpdatePuppets();
                UpdateHeartbeatThreat();
                UpdateChaseHud();
                UpdateChaseSetPropInteraction(keyboard);
                CheckMazeExit();
            }
        }

        private void LateUpdate()
        {
            AnimateHeldItem();
            if (phase == Phase.Puzzle) return;
            if (phase != Phase.Chase || viewCamera == null) return;
            viewCamera.transform.localPosition = ResolveFirstPersonCameraLocal() + cameraShake;
            cameraShake = Vector3.Lerp(cameraShake, Vector3.zero,
                1f - Mathf.Exp(-18f * Time.unscaledDeltaTime));
            UpdateMazeMirrorReflections();
        }

        private void OnDestroy()
        {
            foreach (MazeMirrorSurface surface in mazeMirrorSurfaces)
            {
                if (surface == null) continue;
                if (surface.recursionTextures != null)
                {
                    foreach (RenderTexture texture in surface.recursionTextures)
                    {
                        if (texture == null) continue;
                        texture.Release();
                        Destroy(texture);
                    }
                }
                else if (surface.texture != null)
                {
                    surface.texture.Release();
                    Destroy(surface.texture);
                }
                if (surface.material != null) Destroy(surface.material);
            }
            mazeMirrorSurfaces.Clear();
            if (mazeReflectionCamera != null) Destroy(mazeReflectionCamera.gameObject);
        }

        private void UpdateInventoryInput(Keyboard keyboard)
        {
            if (inventory.Count == 0)
            {
                selectedInventoryIndex = -1;
                return;
            }
            Mouse mouse = Mouse.current;
            if (mouse != null)
            {
                float wheel = mouse.scroll.ReadValue().y;
                if (Mathf.Abs(wheel) > .01f)
                {
                    int direction = wheel > 0f ? -1 : 1;
                    selectedInventoryIndex = (selectedInventoryIndex + direction + inventory.Count) % inventory.Count;
                    RefreshInventory();
                    RevealInventoryHud(2.8f);
                    SetMessage("手持物件 · " + inventory[selectedInventoryIndex].title, 1.25f);
                }
            }
            if (keyboard == null) return;
            for (int i = 0; i < inventory.Count && i < InventoryCapacity; i++)
            {
                Key key = (Key)((int)Key.Digit1 + i);
                if (keyboard[key].wasPressedThisFrame)
                {
                    selectedInventoryIndex = i;
                    RefreshInventory();
                    RevealInventoryHud(2.8f);
                    SetMessage("手持物件 · " + inventory[i].title, 1.25f);
                }
            }
            if (keyboard.gKey.wasPressedThisFrame) { RevealInventoryHud(2.8f); DropSelectedItem(); }
            if (keyboard.fKey.wasPressedThisFrame && selectedInventoryIndex >= 0 && selectedInventoryIndex < inventory.Count)
            {
                RevealInventoryHud(4.0f);
                InventoryItem item = inventory[selectedInventoryIndex];
                if (item.id == "flashlight") ToggleFlashlight();
                else ShowInspection(item);
            }
            UpdateHeldItemVisual();
        }

        private void RevealInventoryHud(float duration)
        {
            if (inventoryHud == null || inventoryHudGroup == null || inventory.Count == 0) return;
            inventoryHud.SetActive(true);
            inventoryHudVisibleUntil = Time.unscaledTime + duration;
            inventoryHudGroup.alpha = 1f;
        }

        private void UpdateInventoryHudFade()
        {
            if (inventoryHud == null || inventoryHudGroup == null) return;
            bool visible = inventory.Count > 0 && Time.unscaledTime < inventoryHudVisibleUntil &&
                !inventoryPanel.activeSelf && !inspectPanel.activeSelf && !keypadPanel.activeSelf;
            float target = visible ? 1f : 0f;
            inventoryHudGroup.alpha = Mathf.MoveTowards(inventoryHudGroup.alpha, target, Time.unscaledDeltaTime * 5.5f);
            if (inventoryHudGroup.alpha <= .01f && inventoryHud.activeSelf) inventoryHud.SetActive(false);
            else if (visible && !inventoryHud.activeSelf) inventoryHud.SetActive(true);
        }

        private void DropSelectedItem()
        {
            if (selectedInventoryIndex < 0 || selectedInventoryIndex >= inventory.Count || player == null) return;
            InventoryItem item = inventory[selectedInventoryIndex];
            if (item.id != "flashlight")
            {
                if (heldItemVisual != null) heldItemVisual.SetActive(false);
                return;
            }
            inventory.RemoveAt(selectedInventoryIndex);
            selectedInventoryIndex = inventory.Count == 0 ? -1 : Mathf.Clamp(selectedInventoryIndex, 0, inventory.Count - 1);
            if (item.id == "flashlight" && flashlight != null)
            {
                flashlight.enabled = false;
                if (flashlightFill != null) flashlightFill.enabled = false;
            }
            if (item.id == "luban") lubanCollected = false;
            Vector3 dropPosition = player.position + player.forward * 1.15f + Vector3.up * .42f;
            GameObject dropped = CreateWorldItemVisual(item, dropPosition, player.eulerAngles.y + 25f);
            DroppedItemPickup pickup = new() { world = dropped, item = item };
            droppedPickups.Add(pickup);
            EnsureInteractableMarker(dropped, "拾取" + item.title);
            RefreshInventory();
            SetMessage("已丢弃 · " + item.title);
        }

        private void UpdatePuzzleInteraction(Keyboard keyboard)
        {
            if (player == null) return;
            // Gameplay keeps the cursor locked for camera look, so a left click
            // is the centred world-interaction equivalent of pressing E.
            Mouse mouse = Mouse.current;
            bool interactPressed = (keyboard != null && keyboard.eKey.wasPressedThisFrame) ||
                                   (mouse != null && mouse.leftButton.wasPressedThisFrame);
            if (!interactPressed) return;
            bool heritageFocused = IsFocused(heritageMuralObject) || IsHeritageMuralCentered();
            if (!IsFocused(flashlightObject) && !IsFocused(sheetObject) && !IsFocused(chairObject) &&
                !IsFocused(wallClueObject) && !IsFocused(lubanLockObject) && !IsDoorFocused() &&
                NearestFocusedInstrument() == null && NearestFocusedSwingDoor() == null &&
                NearestFocusedSetProp() == null && NearestFocusedDroppedPickup() == null &&
                !heritageFocused) return;
            if (heritageMuralVisible && heritageFocused)
            {
                if (!heritageAchievementClaimed)
                {
                    heritageAchievementClaimed = true;
                    bool firstUnlock = MeixiLocalProgress.UnlockAchievement("heritage", true);
                    Debug.Log("[ChapterTwoAchievement] heritage mural interaction " +
                              (firstUnlock ? "unlocked" : "already-unlocked"));
                    SetMessage("你在旧壁画中看见了仍未熄灭的传承", 4.2f);
                }
                return;
            }
            DroppedItemPickup dropped = NearestFocusedDroppedPickup();
            if (dropped != null)
            {
                if (inventory.Count >= InventoryCapacity)
                {
                    SetMessage("物品栏已满，先丢弃一件物品");
                    return;
                }
                Collect(dropped.item.id, dropped.item.title, dropped.item.kind, dropped.item.description, false);
                if (dropped.item.id == "luban") lubanCollected = true;
                if (dropped.world != null) Destroy(dropped.world);
                droppedPickups.Remove(dropped);
                SetMessage("拾取物件 · " + dropped.item.title);
                return;
            }
            ChapterTwoSetProp focusedProp = NearestFocusedSetProp();
            if (focusedProp != null)
            {
                string response = focusedProp.Interact();
                if (!string.IsNullOrEmpty(response)) SetMessage(response, 4.0f);
                return;
            }
            if (!flashlightCollected && IsFocused(flashlightObject))
            {
                BeginFlashlightPickup();
                return;
            }
            if (!sheetCollected && IsFocused(sheetObject))
            {
                sheetCollected = true;
                sheetObject.SetActive(false);
                Collect("score", "残谱页", "关键物件",
                    "民国二十四年 · 七月初九\n\n今晚仍按旧次序试音：\n古筝 → 编钟 → 堂鼓 → 扬琴\n\n四枚褪色朱印压在折痕上。右下角的 M-17，与溪流边发现的异常声纹相同。", true);
                SetMessage("获得关键物件 · 残谱页");
                return;
            }
            if (!chairSolved && IsFocused(chairObject))
            {
                chairSolved = true;
                Collect("chair", "椅背纸片", "线索",
                    "从铜扣后抽出的薄纸。\n\n正面只有被反复描粗的：\n5 · 8\n\n背面写着：\n“木梁下，坐过的人先开口。不要相信谢幕后还站着的影子。”", true);
                SetMessage("铜扣松开，椅背里掉出一张折纸");
                return;
            }
            if (!wallSolved && IsFocused(wallClueObject))
            {
                if (!flashlightCollected || flashlight == null || !flashlight.enabled)
                {
                    SetMessage("烟尘下似乎压着旧刻痕，普通灯光看不清");
                    return;
                }
                wallSolved = true;
                Collect("wall", "墙面拓记", "线索",
                    "手电斜光穿过旧纸，露出被烟灰遮住的两道数痕：\n2 · 4\n\n旁边极浅的小字写着：\n“我守的从来不是曲子，是那些被戏台带走、却无人敢认领的记忆。”", true);
                SetMessage("手电斜光下，墙灰显出一层更早的刻写");
                return;
            }

            TheatreSwingDoor swingDoor = NearestFocusedSwingDoor();
            if (swingDoor != null)
            {
                swingDoor.Toggle();
                PlayChapterTwoEffect(doorClip, .78f);
                ChapterTwoInteractableHighlight marker = swingDoor.GetComponent<ChapterTwoInteractableHighlight>();
                if (marker != null) marker.Prompt = swingDoor.InteractionPrompt;
                SetMessage(swingDoor.IsOpen ? "后台木门缓缓打开" : "后台木门已经关好", 1.5f);
                return;
            }

            TheatreInstrument focusedInstrument = NearestFocusedInstrument();
            if (focusedInstrument != null)
            {
                PlayInstrumentPuzzle(focusedInstrument);
                return;
            }
            if (IsFocused(lubanLockObject) && !lubanCollected && instrumentsSolved)
            {
                lubanCollected = true;
                lubanLockObject.SetActive(false);
                Collect("luban", "卯榫匣", "机关物件",
                    "四根互锁木梁组成的鲁班锁。榫头没有明显的起点，拆解顺序只能从结构本身找。", true);
                SetMessage("四声合奏后，舞台暗格吐出一只卯榫匣");
                return;
            }
            if (IsDoorFocused())
            {
                OpenKeypad();
            }
        }

        private void UpdateFocusedInteractable()
        {
            if (viewCamera == null) return;
            ChapterTwoInteractableHighlight next = null;
            if (!TryGetCenteredHeritageMural(out next))
            {
                Ray ray = viewCamera.ViewportPointToRay(new Vector3(.5f, .5f, 0f));
                if (Physics.SphereCast(ray, .24f, out RaycastHit hit, 10.8f, ~0, QueryTriggerInteraction.Collide))
                    next = hit.transform.GetComponentInParent<ChapterTwoInteractableHighlight>();
            }
            if (next == null)
            {
                float bestScore = float.PositiveInfinity;
                foreach (ChapterTwoInteractableHighlight marker in interactableMarkers)
                {
                    if (marker == null || !marker.isActiveAndEnabled) continue;
                    Vector3 focus = marker.transform.position + Vector3.up * .42f;
                    if (interactableFocusColliders.TryGetValue(marker, out Collider focusCollider) &&
                        focusCollider != null && focusCollider.enabled) focus = focusCollider.bounds.center;
                    Vector3 viewport = viewCamera.WorldToViewportPoint(focus);
                    if (viewport.z <= 0f || viewport.x < .27f || viewport.x > .73f || viewport.y < .25f || viewport.y > .75f) continue;
                    Vector3 direction = focus - viewCamera.transform.position;
                    float distance = direction.magnitude;
                    if (distance > 10.8f) continue;
                    if (Physics.Raycast(viewCamera.transform.position, direction.normalized, out RaycastHit blocker,
                        distance, ~0, QueryTriggerInteraction.Ignore))
                    {
                        ChapterTwoInteractableHighlight blockerMarker =
                            blocker.transform.GetComponentInParent<ChapterTwoInteractableHighlight>();
                        bool insideInteractionVolume = interactableFocusColliders.TryGetValue(marker, out Collider volume) &&
                            volume != null && volume.bounds.Intersects(blocker.collider.bounds);
                        if (blockerMarker != marker && !insideInteractionVolume) continue;
                    }
                    float score = Vector2.Distance(new Vector2(.5f, .5f), new Vector2(viewport.x, viewport.y)) * 16f + distance * .015f;
                    if (score < bestScore) { bestScore = score; next = marker; }
                }
            }
            if (next == focusedHighlight) return;
            if (focusedHighlight != null)
            {
                focusedHighlight.SetHighlighted(false);
                if (focusedHighlight.gameObject == heritageMuralObject) SetHeritageMuralFrame(false);
            }
            focusedHighlight = next;
            focusedTarget = next != null ? next.gameObject : null;
            if (focusedHighlight != null)
            {
                focusedHighlight.SetHighlighted(true);
                if (focusedHighlight.gameObject == heritageMuralObject) SetHeritageMuralFrame(true);
            }
        }

        private bool IsHeritageMuralCentered()
        {
            return TryGetCenteredHeritageMural(out _);
        }

        private bool TryGetCenteredHeritageMural(out ChapterTwoInteractableHighlight marker)
        {
            marker = null;
            if (!heritageMuralVisible || heritageMuralObject == null ||
                !heritageMuralObject.activeInHierarchy || viewCamera == null) return false;
            marker = heritageMuralObject.GetComponent<ChapterTwoInteractableHighlight>();
            if (marker == null || !marker.isActiveAndEnabled) return false;

            Vector3 focus = heritageMuralObject.transform.position;
            if (interactableFocusColliders.TryGetValue(marker, out Collider volume) &&
                volume != null && volume.enabled) focus = volume.bounds.center;
            Vector3 direction = focus - viewCamera.transform.position;
            float distance = direction.magnitude;
            if (distance < .10f || distance > 13.5f) return false;
            Vector3 viewport = viewCamera.WorldToViewportPoint(focus);
            if (viewport.z <= 0f || viewport.x < .22f || viewport.x > .78f ||
                viewport.y < .22f || viewport.y > .78f) return false;
            if (Vector3.Angle(viewCamera.transform.forward, direction) > 16f) return false;

            // The emblem is mounted flush to the plaster wall. The wall's own
            // collider may be the first non-trigger hit, so the dedicated mural
            // volume is intentionally treated as the authoritative interaction
            // surface once the player is looking at its centre.
            return true;
        }

        private bool IsFocused(GameObject target)
        {
            return target != null && focusedTarget != null &&
                   (focusedTarget == target || focusedTarget.transform.IsChildOf(target.transform));
        }

        private bool IsDoorFocused()
        {
            return IsFocused(doorObject) || IsFocused(doorLockVisual);
        }

        private TheatreInstrument NearestFocusedInstrument()
        {
            foreach (TheatreInstrument instrument in instruments)
                if (instrument != null && IsFocused(instrument.gameObject)) return instrument;
            return null;
        }

        private TheatreSwingDoor NearestFocusedSwingDoor()
        {
            foreach (TheatreSwingDoor door in swingDoors)
                if (door != null && IsFocused(door.gameObject)) return door;
            return null;
        }

        private ChapterTwoSetProp NearestFocusedSetProp()
        {
            ChapterTwoSetProp best = null;
            float bestDistance = float.PositiveInfinity;
            foreach (ChapterTwoSetProp prop in setProps)
            {
                if (prop == null || !prop.isActiveAndEnabled || !IsFocused(prop.gameObject)) continue;
                float distance = Vector3.SqrMagnitude(prop.transform.position - player.position);
                if (distance < bestDistance) { bestDistance = distance; best = prop; }
            }
            return best;
        }

        private DroppedItemPickup NearestFocusedDroppedPickup()
        {
            for (int i = droppedPickups.Count - 1; i >= 0; i--)
            {
                DroppedItemPickup pickup = droppedPickups[i];
                if (pickup == null || pickup.world == null)
                {
                    droppedPickups.RemoveAt(i);
                    continue;
                }
                if (IsFocused(pickup.world)) return pickup;
            }
            return null;
        }

        private void PlayInstrumentPuzzle(TheatreInstrument instrument)
        {
            if (instrumentsSolved) { instrument.Play(); SetMessage("机关里的四枚铜片一同轻响"); return; }
            if (!sheetCollected)
            {
                instrument.Play();
                instrumentProgress = 0;
                SetMessage("梁柱深处传来一声短促回响，随即沉寂");
                return;
            }
            string expected = instrumentOrder[instrumentProgress];
            if (!instrument.DisplayName.Contains(expected, StringComparison.Ordinal))
            {
                instrumentProgress = 0;
                instrument.Play();
                SetMessage("木梁里的铜片逆向滑落，机关回到了起点");
                return;
            }
            instrument.Play();
            instrumentProgress++;
            if (instrumentProgress >= instrumentOrder.Length)
            {
                instrumentsSolved = true;
                if (lubanLockObject != null) lubanLockObject.SetActive(true);
                SetMessage("四道声纹终于在木梁间同时共鸣，舞台下传来榫卯滑动声");
            }
            else SetMessage("梁间亮起第 " + instrumentProgress + " 枚铜片");
        }

        private TheatreInstrument NearestInstrument()
        {
            TheatreInstrument best = null;
            float bestDistance = float.PositiveInfinity;
            foreach (TheatreInstrument item in instruments)
            {
                if (item == null) continue;
                float distance = Vector3.SqrMagnitude(item.transform.position - player.position);
                if (distance < bestDistance) { bestDistance = distance; best = item; }
            }
            return best;
        }

        public void NotifyLegacyInstrumentPlayed(TheatreInstrument instrument) { }

        private void UpdatePuzzleHud()
        {
            string prompt = Time.unscaledTime < messageUntil ? transientMessage : GetContextPrompt();
            SetHintVisible(!string.IsNullOrEmpty(prompt), prompt);
            if (objectiveText.text != "二楼 · 搜查观众席与后台，拼合守谱人的记录")
                objectiveText.text = "二楼 · 搜查观众席与后台，拼合守谱人的记录";
        }

        private string GetContextPrompt()
        {
            if (focusedHighlight == null) return string.Empty;
            return "[ E / 鼠标左键 ]  " + focusedHighlight.Prompt;
        }

        private void UpdateWallClueGlow()
        {
            if (wallClueText == null || wallClueObject == null || viewCamera == null) return;
            Vector3 toClue = wallClueObject.transform.position - viewCamera.transform.position;
            bool facing = toClue.sqrMagnitude > .01f &&
                          Vector3.Dot(viewCamera.transform.forward, toClue.normalized) > .72f;
            bool reveal = flashlightCollected && flashlight != null && flashlight.enabled && facing;
            if (wallClueText.gameObject.activeSelf != reveal) wallClueText.gameObject.SetActive(reveal);
        }

        private void SetMazeLabelsVisible(bool visible)
        {
            if (mazeLabelsVisible == visible) return;
            mazeLabelsVisible = visible;
            foreach (GameObject label in mazeWorldLabels)
                if (label != null) label.SetActive(visible);
        }

        private bool Collect(string id, string item, string kind, string description, bool inspect)
        {
            if (inventory.Count >= InventoryCapacity)
            {
                SetMessage("物品栏已满，先按 G 丢弃一件物品");
                return false;
            }
            InventoryItem entry = new()
            {
                id = id,
                title = item,
                kind = kind,
                description = description,
                icon = CreateInventoryIcon(id)
            };
            inventory.Add(entry);
            selectedInventoryIndex = inventory.Count - 1;
            RefreshInventory();
            RevealInventoryHud(3.2f);
            UpdateHeldItemVisual();
            if (inspect) ShowInspection(entry);
            return true;
        }

        private void SetMessage(string value, float duration = 3.2f)
        {
            transientMessage = value;
            messageUntil = Time.unscaledTime + duration;
            SetHintVisible(true, value);
        }

        private void SetHintVisible(bool visible, string value)
        {
            if (hintCard == null || hintText == null) return;
            if (visible && !hintCard.activeSelf) hintCard.SetActive(true);
            if (visible)
            {
                if (hintText.text != value) hintText.text = value;
                if (hintGroup != null) hintGroup.alpha = 1f;
                if (hintCard.transform.parent != null &&
                    hintCard.transform.GetSiblingIndex() != hintCard.transform.parent.childCount - 1)
                    hintCard.transform.SetAsLastSibling();
            }
            else if (hintCard.activeSelf) hintCard.SetActive(false);
        }

        private bool Near(GameObject target, float radius)
        {
            if (target == null || !target.activeInHierarchy || player == null) return false;
            Vector3 delta = target.transform.position - player.position;
            delta.y *= .35f;
            return delta.sqrMagnitude <= radius * radius;
        }

        private void ToggleFlashlight()
        {
            if (flashlightRevealPlaying) return;
            EnsureFlashlight();
            flashlight.enabled = !flashlight.enabled;
            if (flashlightFill != null) flashlightFill.enabled = flashlight.enabled;
            PlayChapterTwoEffect(flashlightSwitchClip, .78f);
            SetMessage(flashlight.enabled ? "手电筒已打开" : "手电筒已关闭");
        }

        private void BeginFlashlightPickup()
        {
            if (flashlightCollected || flashlightRevealPlaying) return;
            flashlightCollected = true;
            flashlightObject.SetActive(false);
            EnsureFlashlight();
            Collect("flashlight", "手电筒", "工具",
                "一支沉甸甸的旧式手电。电池仓里留着新近更换过的痕迹。", false);
            RevealHeritageMural();
            SpawnCeilingPuppets();
            StartCoroutine(PlayFlashlightRevealSequence());
        }

        private IEnumerator PlayFlashlightRevealSequence()
        {
            flashlightRevealPlaying = true;
            inputModal = true;
            if (stageRuntime != null) stageRuntime.SetSharedGameplayControlsActive(false);
            if (controller != null) controller.enabled = true;
            MeixiCursorState.SetGameplay();

            Transform quakeRoot = GameObject.Find("Theatre_Set_RuntimeGenerated")?.transform;
            Vector3 quakeStartPosition = quakeRoot != null ? quakeRoot.localPosition : Vector3.zero;
            Quaternion quakeStartRotation = quakeRoot != null ? quakeRoot.localRotation : Quaternion.identity;
            Quaternion playerStartRotation = player.rotation;
            Vector3 playerAnchor = player.position;
            Vector3 forward = Vector3.ProjectOnPlane(player.forward, Vector3.up).normalized;
            if (forward.sqrMagnitude < .01f) forward = Vector3.forward;
            Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;

            Transform cameraParent = viewCamera.transform.parent;
            Vector3 cameraLocalPosition = viewCamera.transform.localPosition;
            Quaternion cameraLocalRotation = viewCamera.transform.localRotation;
            float cameraFov = viewCamera.fieldOfView;
            viewCamera.transform.SetParent(null, true);
            viewCamera.fieldOfView = 47f;
            flashlight.enabled = false;
            if (flashlightFill != null) flashlightFill.enabled = false;
            BlackoutTheatre();
            PlayChapterTwoEffect(earthquakeClip, .88f);
            PlayChapterTwoEffect(blackoutClip, .82f);
            SetMessage("咔哒——整座戏台突然断电", 2.0f);

            float elapsed = 0f;
            const float duration = 4.15f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float envelope = Mathf.Sin(t * Mathf.PI);
                float quake = envelope * (.018f + Mathf.SmoothStep(0f, .052f, Mathf.Clamp01(t * 2.2f)));
                if (quakeRoot != null)
                {
                    quakeRoot.localPosition = quakeStartPosition + new Vector3(
                        Mathf.Sin(elapsed * 23f) * quake,
                        Mathf.Sin(elapsed * 31f + .7f) * quake * .28f,
                        Mathf.Cos(elapsed * 19f) * quake * .72f);
                    quakeRoot.localRotation = quakeStartRotation * Quaternion.Euler(
                        Mathf.Sin(elapsed * 17f) * quake * 2.2f,
                        Mathf.Sin(elapsed * 11f) * quake * 1.4f,
                        Mathf.Cos(elapsed * 21f) * quake * 2.8f);
                }

                float lookYaw;
                if (t < .24f) lookYaw = Mathf.Lerp(0f, -34f, Mathf.SmoothStep(0f, 1f, t / .24f));
                else if (t < .58f) lookYaw = Mathf.Lerp(-34f, 42f, Mathf.SmoothStep(0f, 1f, (t - .24f) / .34f));
                else lookYaw = Mathf.Lerp(42f, 0f, Mathf.SmoothStep(0f, 1f, (t - .58f) / .42f));
                player.rotation = playerStartRotation * Quaternion.Euler(0f, lookYaw, 0f);

                Vector3 cameraBase = playerAnchor - forward * 2.45f + right * 1.42f + Vector3.up * 1.62f;
                Vector3 cameraJitter = right * Mathf.Sin(elapsed * 37f) * quake * 2.2f +
                                       Vector3.up * Mathf.Cos(elapsed * 29f) * quake * 1.45f;
                viewCamera.transform.position = cameraBase + cameraJitter;
                Vector3 lookPoint = playerAnchor + Vector3.up * 1.30f + forward * .10f;
                viewCamera.transform.rotation = Quaternion.LookRotation(lookPoint - viewCamera.transform.position, Vector3.up);

                if (elapsed > 2.45f)
                {
                    bool steady = elapsed > 3.30f;
                    flashlight.enabled = steady || Mathf.Sin(elapsed * 31f) > .25f;
                    if (flashlightFill != null) flashlightFill.enabled = flashlight.enabled;
                    if (steady && elapsed < 3.38f) SetMessage("手电筒亮起了，四周只剩下光束中的东西", 2.4f);
                }
                yield return null;
            }

            if (quakeRoot != null)
            {
                quakeRoot.localPosition = quakeStartPosition;
                quakeRoot.localRotation = quakeStartRotation;
            }
            player.rotation = playerStartRotation;
            viewCamera.transform.SetParent(cameraParent, false);
            viewCamera.transform.localPosition = cameraLocalPosition;
            viewCamera.transform.localRotation = cameraLocalRotation;
            viewCamera.fieldOfView = cameraFov;
            flashlight.enabled = true;
            if (flashlightFill != null) flashlightFill.enabled = true;
            PlayChapterTwoEffect(flashlightSwitchClip, .70f);
            StartPuppetBgm();
            Physics.SyncTransforms();
            inputModal = false;
            flashlightRevealPlaying = false;
            if (stageRuntime != null) stageRuntime.SetSharedGameplayControlsActive(true);
            SetMessage("环境已经彻底陷入黑暗 · F 开关手电筒", 3.5f);
        }

        private void EnsureInteractableMarker(GameObject target, string prompt)
        {
            if (target == null) return;
            ChapterTwoInteractableHighlight marker = target.GetComponent<ChapterTwoInteractableHighlight>();
            if (marker == null) marker = target.AddComponent<ChapterTwoInteractableHighlight>();
            marker.Prompt = prompt;
            marker.Initialize();
            if (!interactableMarkers.Contains(marker)) interactableMarkers.Add(marker);
            interactableFocusColliders[marker] = EnsureInteractionVolume(target);
        }

        private Collider EnsureInteractionVolume(GameObject target)
        {
            Transform volumeTransform = target.transform.Find("InteractionVolume");
            GameObject volumeObject = volumeTransform != null ? volumeTransform.gameObject : new GameObject("InteractionVolume");
            if (volumeTransform == null) volumeObject.transform.SetParent(target.transform, false);
            BoxCollider volume = volumeObject.GetComponent<BoxCollider>() ?? volumeObject.AddComponent<BoxCollider>();
            Bounds worldBounds = new(target.transform.position, Vector3.one * .35f);
            bool foundRenderer = false;
            foreach (Renderer renderer in target.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer.gameObject == volumeObject) continue;
                if (!foundRenderer) { worldBounds = renderer.bounds; foundRenderer = true; }
                else worldBounds.Encapsulate(renderer.bounds);
            }
            Vector3 lossy = target.transform.lossyScale;
            Vector3 localCenter = target.transform.InverseTransformPoint(worldBounds.center);
            Vector3 localSize = new(
                (worldBounds.size.x + .32f) / Mathf.Max(.001f, Mathf.Abs(lossy.x)),
                (worldBounds.size.y + .32f) / Mathf.Max(.001f, Mathf.Abs(lossy.y)),
                (worldBounds.size.z + .32f) / Mathf.Max(.001f, Mathf.Abs(lossy.z)));
            if (target == chairObject)
            {
                localCenter = new Vector3(0f, .48f, .02f);
                localSize = new Vector3(1.30f, 2.30f, 1.20f);
            }
            else if (target == sheetObject)
            {
                localCenter = Vector3.zero;
                localSize = new Vector3(1.80f, .72f, 1.52f);
            }
            volume.center = localCenter;
            volume.size = Vector3.Max(localSize, new Vector3(.65f, .65f, .65f));
            volume.isTrigger = true;
            return volume;
        }

        private void UpdateHeldItemVisual()
        {
            if (player == null || inventory.Count == 0 || selectedInventoryIndex < 0 || selectedInventoryIndex >= inventory.Count)
            {
                if (heldItemVisual != null) heldItemVisual.SetActive(false);
                return;
            }
            if (heldItemRoot == null)
            {
                heldItemRoot = new GameObject("玩家当前手持物件").transform;
                heldItemRoot.SetParent(player, false);
                heldItemBasePosition = new Vector3(.42f, .94f, .28f);
                heldItemBaseRotation = Quaternion.Euler(-10f, 16f, -6f);
                heldItemRoot.localPosition = heldItemBasePosition;
                heldItemRoot.localRotation = heldItemBaseRotation;
            }
            InventoryItem item = inventory[selectedInventoryIndex];
            if (item.id != "flashlight")
            {
                if (heldItemVisual != null) heldItemVisual.SetActive(false);
                return;
            }
            if (heldItemVisual == null || heldItemVisual.name != "手持_" + item.id)
            {
                if (heldItemVisual != null) Destroy(heldItemVisual);
                heldItemVisual = CreateItemVisual(item, heldItemRoot, false);
                heldItemVisual.name = "手持_" + item.id;
            }
            heldItemVisual.transform.localScale = item.id == "flashlight" ? Vector3.one * .58f : Vector3.one;
            heldItemVisual.SetActive(true);
        }

        private void AnimateHeldItem()
        {
            if (heldItemRoot == null || heldItemVisual == null || !heldItemVisual.activeSelf) return;
            float time = Time.unscaledTime;
            bool moving = controller != null && controller.velocity.sqrMagnitude > .08f;
            float frequency = moving ? 8.4f : 1.7f;
            float amplitude = moving ? .018f : .006f;
            float bob = Mathf.Sin(time * frequency) * amplitude;
            float sway = Mathf.Sin(time * frequency * .5f + 1.2f) * amplitude * .72f;
            heldItemRoot.localPosition = heldItemBasePosition + new Vector3(sway, bob, 0f);
            heldItemRoot.localRotation = heldItemBaseRotation * Quaternion.Euler(-bob * 55f, sway * 45f, sway * 70f);
        }

        private GameObject CreateWorldItemVisual(InventoryItem item, Vector3 position, float rotation)
        {
            GameObject root = new("掉落物件_" + item.id);
            root.transform.position = position;
            root.transform.rotation = Quaternion.Euler(0f, rotation, 0f);
            CreateItemVisual(item, root.transform, true);
            return root;
        }

        private GameObject CreateItemVisual(InventoryItem item, Transform parent, bool collider)
        {
            Transform root = new GameObject("物件模型_" + item.id).transform;
            root.SetParent(parent, false);
            Material wood = MakeMaterial("HeldWood_" + item.id, new Color(.22f, .075f, .038f), .28f, .03f);
            Material brass = MakeMaterial("HeldBrass_" + item.id, new Color(.61f, .36f, .12f), .55f, .32f);
            Material paper = MakeMaterial("HeldPaper_" + item.id, new Color(.72f, .56f, .30f), .14f, 0f);
            Material flashlightBody = MakeMaterial("HeldFlashlightBody_" + item.id, new Color(.075f, .105f, .105f), .42f, .56f);
            Material flashlightRubber = MakeMaterial("HeldFlashlightRubber_" + item.id, new Color(.018f, .025f, .025f), .18f, .04f);
            Material flashlightLens = MakeEmissionMaterial("HeldFlashlightLens_" + item.id,
                new Color(.62f, .86f, .78f), 1.15f);
            switch (item.id)
            {
                case "flashlight":
                    GameObject barrel = Cylinder("手持灯筒", root, Vector3.zero,
                        new Vector3(.065f, .19f, .065f), flashlightBody, collider);
                    barrel.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                    GameObject tail = Cylinder("手持灯尾盖", root, new Vector3(0f, 0f, -.215f),
                        new Vector3(.071f, .028f, .071f), flashlightRubber, false);
                    tail.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                    GameObject bezel = Cylinder("手持灯头压环", root, new Vector3(0f, 0f, .215f),
                        new Vector3(.090f, .035f, .090f), brass, false);
                    bezel.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                    Cylinder("手持灯镜片", root, new Vector3(0f, 0f, .255f),
                        new Vector3(.064f, .012f, .064f), flashlightLens, false)
                        .transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                    Box("手持灯侧键", root, new Vector3(.073f, .015f, -.035f),
                        new Vector3(.018f, .035f, .070f), brass, false);
                    for (int grip = -1; grip <= 1; grip++)
                    {
                        Cylinder("手持灯握持环_" + grip, root, new Vector3(0f, 0f, grip * .115f),
                            new Vector3(.070f, .008f, .070f), flashlightRubber, false)
                            .transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                    }
                    break;
                case "score":
                case "chair":
                case "wall":
                    Box("手持纸片", root, Vector3.zero, new Vector3(.48f, .035f, .34f), paper, collider).transform.localRotation = Quaternion.Euler(-8f, 12f, 6f);
                    Box("手持墨痕", root, new Vector3(0f, .025f, 0f), new Vector3(.30f, .012f, .025f), wood, false);
                    break;
                case "luban":
                    Material grain = MakeMaterial("HeldLubanGrain", new Color(.07f, .018f, .008f), .22f, 0f);
                    Box("榫匣横梁", root, new Vector3(0f, .12f, 0f), new Vector3(.64f, .12f, .12f), wood, collider);
                    Box("榫匣纵梁", root, new Vector3(0f, -.12f, 0f), new Vector3(.12f, .12f, .64f), brass, false);
                    Box("榫匣铜芯", root, new Vector3(0f, 0f, 0f), new Vector3(.19f, .19f, .19f), paper, false);
                    Box("HeldLubanGrainH", root, new Vector3(0f, .185f, 0f), new Vector3(.42f, .012f, .026f), grain, false);
                    Box("HeldLubanGrainV", root, new Vector3(.185f, -.12f, 0f), new Vector3(.026f, .026f, .40f), grain, false);
                    for (int pin = 0; pin < 4; pin++)
                    {
                        float angle = pin * 90f * Mathf.Deg2Rad;
                        Sphere("HeldLubanPin_" + pin, root,
                            new Vector3(Mathf.Cos(angle) * .28f, .12f, Mathf.Sin(angle) * .28f),
                            new Vector3(.055f, .055f, .055f), brass, false);
                    }
                    break;
            }
            return root.gameObject;
        }

        private void EnsureFlashlight()
        {
            if (flashlight != null || viewCamera == null) return;
            const int playerVisualLayer = PlayerVisualLayer;
            if (player != null)
            {
                foreach (Renderer renderer in player.GetComponentsInChildren<Renderer>(true))
                    if (renderer != null) renderer.gameObject.layer = playerVisualLayer;
            }
            GameObject lightObject = new("玩家手电筒光束");
            lightObject.transform.SetParent(viewCamera.transform, false);
            lightObject.transform.localPosition = new Vector3(.16f, -.12f, .20f);
            flashlight = lightObject.AddComponent<Light>();
            flashlight.type = LightType.Spot;
            flashlight.color = new Color(.82f, .91f, 1f);
            flashlight.range = 18f;
            flashlight.intensity = 108f;
            flashlight.spotAngle = 40f;
            flashlight.innerSpotAngle = 22f;
            flashlight.shadows = LightShadows.Soft;
            flashlight.shadowStrength = .68f;
            flashlight.cullingMask = ~(1 << playerVisualLayer);

            GameObject fillObject = new("PlayerFlashlightBounceFill");
            fillObject.transform.SetParent(viewCamera.transform, false);
            fillObject.transform.localPosition = new Vector3(.04f, -.20f, .42f);
            flashlightFill = fillObject.AddComponent<Light>();
            flashlightFill.type = LightType.Point;
            flashlightFill.color = new Color(.31f, .48f, .50f);
            flashlightFill.range = 3.1f;
            flashlightFill.intensity = .62f;
            flashlightFill.shadows = LightShadows.None;
            flashlightFill.cullingMask = ~(1 << playerVisualLayer);
            flashlightFill.enabled = flashlight.enabled;
        }

        private void OpenKeypad()
        {
            inputModal = true;
            keypadPanel.SetActive(true);
            keypadPanel.transform.SetAsLastSibling();
            if (inventoryHud != null) inventoryHud.SetActive(false);
            RefreshKeypad();
            MeixiCursorState.SetPointer();
            if (keypadStatus != null)
            {
                keypadStatus.text = "LOCKED  /  六位机械锁";
                keypadStatus.color = new Color(.46f, .72f, .65f);
            }
            SetHintVisible(false, string.Empty);
        }

        private void HandleModalInput(Keyboard keyboard)
        {
            if (defeatPanel.activeSelf)
            {
                if (keyboard != null && (keyboard.enterKey.wasPressedThisFrame || keyboard.escapeKey.wasPressedThisFrame))
                    ReturnToChapterOne();
                return;
            }
            if (inventoryPanel.activeSelf || inspectPanel.activeSelf)
            {
                HandleInventoryPointerFallback(Mouse.current);
                if (inspectPanel.activeSelf && keyboard != null &&
                    (keyboard.escapeKey.wasPressedThisFrame || keyboard.iKey.wasPressedThisFrame))
                {
                    ReturnToInventoryFromInspection();
                    return;
                }
                if (!inspectPanel.activeSelf && keyboard != null &&
                    (keyboard.escapeKey.wasPressedThisFrame || keyboard.iKey.wasPressedThisFrame))
                {
                    CloseInventoryUi();
                    return;
                }
                if (inspectPanel.activeSelf)
                {
                    UpdateInspectionInput();
                    if (keyboard != null && keyboard.tabKey.wasPressedThisFrame) ReturnToInventoryFromInspection();
                    return;
                }
                if (keyboard == null) return;
                for (int i = 0; i < Mathf.Min(InventoryCapacity, inventory.Count); i++)
                {
                    Key key = (Key)((int)Key.Digit1 + i);
                    if (keyboard[key].wasPressedThisFrame) SelectInventoryItem(i);
                }
                if ((keyboard.tabKey.wasPressedThisFrame || keyboard.enterKey.wasPressedThisFrame) &&
                    selectedInventoryIndex >= 0 && selectedInventoryIndex < inventory.Count)
                    ShowInspection(inventory[selectedInventoryIndex]);
                return;
            }
            if (!keypadPanel.activeSelf) return;
            HandleKeypadPointerFallback(Mouse.current);
            if (keyboard == null) return;
            if (keyboard.escapeKey.wasPressedThisFrame) { CloseKeypad(); return; }
            if (keyboard.backspaceKey.wasPressedThisFrame && keypadValue.Length > 0)
            {
                keypadValue = keypadValue.Substring(0, keypadValue.Length - 1);
                RefreshKeypad();
            }
            if (keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame) SubmitKeypad();
            Key[] keys = { Key.Digit0, Key.Digit1, Key.Digit2, Key.Digit3, Key.Digit4,
                Key.Digit5, Key.Digit6, Key.Digit7, Key.Digit8, Key.Digit9 };
            for (int i = 0; i < keys.Length; i++) if (keyboard[keys[i]].wasPressedThisFrame) AppendKeypad(i.ToString());
        }

        private void HandleKeypadPointerFallback(Mouse mouse)
        {
            if (mouse == null || !mouse.leftButton.wasPressedThisFrame) return;
            Vector2 screenPoint = mouse.position.ReadValue();
            for (int i = keypadPointerBindings.Count - 1; i >= 0; i--)
            {
                KeypadPointerBinding binding = keypadPointerBindings[i];
                if (binding?.button == null || !binding.button.IsActive() || !binding.button.IsInteractable()) continue;
                RectTransform rect = binding.button.transform as RectTransform;
                if (rect == null || !RectTransformUtility.RectangleContainsScreenPoint(rect, screenPoint, null)) continue;
                EventSystem.current?.SetSelectedGameObject(binding.button.gameObject);
                InvokeKeypadPointerBinding(binding);
                break;
            }
        }

        private void RegisterKeypadPointerButton(Button button, Action action)
        {
            if (button == null || action == null) return;
            KeypadPointerBinding binding = new() { button = button, action = action };
            keypadPointerBindings.Add(binding);
            button.onClick.AddListener(() => InvokeKeypadPointerBinding(binding));
        }

        private void InvokeKeypadPointerBinding(KeypadPointerBinding binding)
        {
            if (binding?.button == null || binding.action == null) return;
            if (lastKeypadPointerButton == binding.button &&
                Time.unscaledTime - lastKeypadPointerActionAt < .24f) return;
            lastKeypadPointerButton = binding.button;
            lastKeypadPointerActionAt = Time.unscaledTime;
            binding.action.Invoke();
        }

        private void AppendKeypad(string value)
        {
            if (keypadValue.Length >= 6) return;
            keypadValue += value;
            if (keypadStatus != null)
            {
                keypadStatus.text = "LOCKED  /  六位机械锁";
                keypadStatus.color = new Color(.46f, .72f, .65f);
            }
            RefreshKeypad();
        }

        private void RefreshKeypad()
        {
            if (keypadDisplay != null) keypadDisplay.text = keypadValue.Length + " / 6";
            for (int i = 0; i < keypadDigits.Length; i++)
            {
                if (keypadDigits[i] == null) continue;
                keypadDigits[i].text = i < keypadValue.Length ? keypadValue[i].ToString() : "·";
                keypadDigits[i].color = i < keypadValue.Length
                    ? new Color(.92f, .77f, .39f)
                    : new Color(.31f, .43f, .43f);
            }
        }

        private void SubmitKeypad()
        {
            if (keypadValue == "071029")
            {
                bool firstUnlock = MeixiLocalProgress.UnlockAchievement("bearbug_bite", true);
                if (keypadStatus != null)
                {
                    keypadStatus.text = "EASTER EGG  /  ARCHIVE MARKED";
                    keypadStatus.color = new Color(.98f, .72f, .27f);
                }
                CloseKeypad();
                SetMessage(firstUnlock ? "隐藏成就已记录：小熊虫" : "隐藏成就已经记录过了", 3.2f);
                return;
            }
            if (keypadValue != CorrectCode)
            {
                if (keypadStatus != null)
                {
                    keypadStatus.text = "DENIED  /  机芯拒绝转动";
                    keypadStatus.color = new Color(.91f, .32f, .20f);
                }
                RefreshKeypad();
                SetMessage("锁芯里传来六次空响", 2.4f);
                return;
            }
            if (keypadStatus != null)
            {
                keypadStatus.text = "UNLOCKED";
                keypadStatus.color = new Color(.37f, .92f, .67f);
            }
            CloseKeypad();
            StartCoroutine(OpenDoorSequence());
        }

        private void CloseKeypad()
        {
            inputModal = false;
            keypadPanel.SetActive(false);
            if (phase == Phase.Puzzle) RevealInventoryHud(2.4f);
            MeixiCursorState.SetGameplay();
            UpdatePuzzleHud();
        }

        private void ToggleInventory()
        {
            if (inventoryPanel.activeSelf || inspectPanel.activeSelf) { CloseInventoryUi(); return; }
            inputModal = true;
            inventoryPanel.SetActive(true);
            inventoryPanel.transform.SetAsLastSibling();
            if (inventoryHud != null) inventoryHud.SetActive(false);
            if (selectedInventoryIndex < 0 && inventory.Count > 0) selectedInventoryIndex = 0;
            RefreshInventory();
            MeixiCursorState.SetPointer();
            SetHintVisible(false, string.Empty);
        }

        private void ShowInspection(InventoryItem item)
        {
            inputModal = true;
            if (inventoryHud != null) inventoryHud.SetActive(false);
            selectedInventoryIndex = inventory.IndexOf(item);
            inspectTitle.text = item.title;
            inspectBody.text = item.description;
            if (item.id == "luban" && !lubanSolved)
                inspectBody.text += "\n\n榫梁编号：1 → 2 → 3 → 4\n按编号顺序点击可滑动的木梁。";
            bool handwritten = item.id == "score" || item.id == "chair" || item.id == "wall";
            if (inspectPaperBackground != null) inspectPaperBackground.gameObject.SetActive(handwritten);
            inspectBody.font = handwritten && inspectHandwritingFont != null ? inspectHandwritingFont : inspectDefaultFont;
            inspectBody.fontSize = handwritten ? 19 : 21;
            inspectBody.fontStyle = handwritten ? FontStyle.Italic : FontStyle.Normal;
            inspectBody.color = handwritten ? new Color(.16f, .105f, .065f) : new Color(.84f, .88f, .81f);
            inspectBody.lineSpacing = handwritten ? 1.42f : 1.30f;
            if (inspectIcon != null) inspectIcon.sprite = item.icon;
            inspectingItemId = item.id;
            EnsureInspectionRig();
            BuildInspectionModel(item);
            CaptureInspectBackground();
            inspectPanel.SetActive(true);
            inventoryPanel.SetActive(false);
            inspectPanel.transform.SetAsLastSibling();
            MeixiCursorState.SetPointer();
        }

        private void ReturnToInventoryFromInspection()
        {
            inspectPanel.SetActive(false);
            CloseInspectionModel();
            inventoryPanel.SetActive(true);
            inventoryPanel.transform.SetAsLastSibling();
            RefreshInventory();
        }

        private void CloseInventoryUi()
        {
            inventoryPanel.SetActive(false);
            inspectPanel.SetActive(false);
            CloseInspectionModel();
            inputModal = false;
            if (inventoryHud != null && phase == Phase.Puzzle) inventoryHud.SetActive(true);
            if (phase == Phase.Puzzle)
            {
                // Assigning Locked recenters the pointer in Unity. Keep the current UI position
                // and resume gameplay look only after the player clicks back into the world.
                gameplayCursorResumePending = false;
                MeixiCursorState.SetGameplay();
            }
            if (phase == Phase.Puzzle) UpdatePuzzleHud();
        }

        private void ResumePuzzleCursorFromSceneClick()
        {
            if (!gameplayCursorResumePending) return;
            Mouse mouse = Mouse.current;
            if (mouse == null || !mouse.leftButton.wasPressedThisFrame) return;
            gameplayCursorResumePending = false;
            MeixiCursorState.SetGameplay();
        }

        private void HandleInventoryPointerFallback(Mouse mouse)
        {
            if (mouse == null || !mouse.leftButton.wasPressedThisFrame) return;
            Vector2 screenPoint = mouse.position.ReadValue();

            // Generated haze and blur images can swallow GraphicRaycaster hits. This direct
            // screen-space test keeps the visible inventory controls usable in that case.
            if (inspectPanel.activeSelf)
            {
                TryInvokeInventoryButton(inspectCloseButton, screenPoint);
                return;
            }
            if (!inventoryPanel.activeSelf) return;
            if (TryInvokeInventoryButton(inventoryCloseButton, screenPoint)) return;
            if (TryInvokeInventoryButton(inventoryInspectButton, screenPoint)) return;
            for (int i = inventorySlotButtons.Length - 1; i >= 0; i--)
                if (TryInvokeInventoryButton(inventorySlotButtons[i], screenPoint)) return;
        }

        private static bool TryInvokeInventoryButton(Button button, Vector2 screenPoint)
        {
            if (button == null || !button.gameObject.activeInHierarchy || !button.interactable) return false;
            RectTransform rect = button.transform as RectTransform;
            if (rect == null || !RectTransformUtility.RectangleContainsScreenPoint(rect, screenPoint, null)) return false;
            button.onClick.Invoke();
            return true;
        }

        private void RefreshInventory()
        {
            if (inventoryText != null) inventoryText.text = inventory.Count > 0 ? inventory.Count + " / " + InventoryCapacity : "I";
            for (int i = 0; i < InventoryCapacity; i++)
            {
                bool occupied = i < inventory.Count;
                Sprite icon = occupied ? inventory[i].icon : CreateInventoryIcon("empty");
                if (hudSlotIcons[i] != null)
                {
                    hudSlotIcons[i].sprite = icon;
                    hudSlotIcons[i].color = occupied ? Color.white : new Color(.33f, .39f, .37f, .44f);
                }
                if (inventorySlotIcons[i] != null)
                {
                    inventorySlotIcons[i].sprite = icon;
                    inventorySlotIcons[i].color = occupied ? Color.white : new Color(.30f, .36f, .35f, .32f);
                }
                bool selected = occupied && i == selectedInventoryIndex;
                if (hudSlotFrames[i] != null)
                    hudSlotFrames[i].color = selected ? new Color(.78f, .46f, .15f, .78f) :
                        occupied ? new Color(.02f, .10f, .10f, .58f) : new Color(.01f, .025f, .028f, .26f);
                if (inventorySlotFrames[i] != null)
                    inventorySlotFrames[i].color = selected ? new Color(.075f, .22f, .20f, .96f) :
                        occupied ? new Color(.018f, .075f, .073f, .86f) : new Color(.008f, .026f, .028f, .56f);
                if (inventorySlotSelectionLines[i] != null)
                {
                    inventorySlotSelectionLines[i].enabled = selected;
                    inventorySlotSelectionLines[i].color = selected
                        ? new Color(.96f, .66f, .28f, .94f)
                        : Color.clear;
                }
                if (inventorySlotTitles[i] != null)
                {
                    inventorySlotTitles[i].text = occupied ? inventory[i].title : "未发现";
                    inventorySlotTitles[i].color = occupied ? new Color(.86f, .89f, .83f) : new Color(.34f, .42f, .40f);
                }
                if (inventorySlotButtons[i] != null) inventorySlotButtons[i].interactable = occupied;
            }
            if (selectedInventoryIndex >= 0 && selectedInventoryIndex < inventory.Count)
            {
                InventoryItem selected = inventory[selectedInventoryIndex];
                if (inventorySelectionIcon != null) inventorySelectionIcon.sprite = selected.icon;
                if (inventorySelectionTitle != null) inventorySelectionTitle.text = selected.title;
                if (inventorySelectionKind != null) inventorySelectionKind.text = selected.kind.ToUpperInvariant();
                if (inventoryFullText != null) inventoryFullText.text = selected.description;
            }
            else
            {
                if (inventorySelectionIcon != null) inventorySelectionIcon.sprite = CreateInventoryIcon("empty");
                if (inventorySelectionTitle != null) inventorySelectionTitle.text = "尚无记录";
                if (inventorySelectionKind != null) inventorySelectionKind.text = "EVIDENCE ARCHIVE";
                if (inventoryFullText != null) inventoryFullText.text = "戏台仍保持着沉默。";
            }
        }

        private void SelectInventoryItem(int index)
        {
            if (index < 0 || index >= inventory.Count) return;
            selectedInventoryIndex = index;
            RefreshInventory();
            UpdateHeldItemVisual();
        }

        private void UpdateInspectionInput()
        {
            Mouse mouse = Mouse.current;
            if (mouse == null) return;
            Vector2 screenPosition = mouse.position.ReadValue();
            bool overViewport = inspectObjectViewport != null &&
                                RectTransformUtility.RectangleContainsScreenPoint(
                                    inspectObjectViewport.rectTransform, screenPosition, null);
            if (mouse.leftButton.wasPressedThisFrame && overViewport)
            {
                inspectDragStart = screenPosition;
                inspectDragPrevious = screenPosition;
                inspectDragging = true;
                inspectAngularVelocity = Vector2.zero;
            }
            if (inspectDragging && mouse.leftButton.isPressed && inspectModelRoot != null)
            {
                Vector2 delta = screenPosition - inspectDragPrevious;
                inspectDragPrevious = screenPosition;
                Vector2 angularDelta = new(delta.x * .24f, -delta.y * .20f);
                inspectYaw += angularDelta.x;
                inspectPitch = Mathf.Clamp(inspectPitch + angularDelta.y, -72f, 72f);
                float dt = Mathf.Max(.001f, Time.unscaledDeltaTime);
                inspectAngularVelocity = Vector2.Lerp(inspectAngularVelocity, angularDelta / dt, .48f);
            }
            if (inspectDragging && mouse.leftButton.wasReleasedThisFrame)
            {
                inspectDragging = false;
                if ((screenPosition - inspectDragStart).sqrMagnitude < 225f)
                {
                    inspectAngularVelocity = Vector2.zero;
                    TryInspectObjectClick(screenPosition);
                }
            }
            if (!inspectDragging)
            {
                float dt = Time.unscaledDeltaTime;
                inspectYaw += inspectAngularVelocity.x * dt;
                inspectPitch = Mathf.Clamp(inspectPitch + inspectAngularVelocity.y * dt, -72f, 72f);
                inspectAngularVelocity *= Mathf.Exp(-5.8f * dt);
            }
            float scroll = mouse.scroll.ReadValue().y;
            if (overViewport && Mathf.Abs(scroll) > .01f)
                inspectTargetDistance = Mathf.Clamp(inspectTargetDistance - Mathf.Sign(scroll) * .34f,
                    inspectMinimumDistance, inspectMaximumDistance);
            inspectDistance = Mathf.Lerp(inspectDistance, inspectTargetDistance,
                1f - Mathf.Exp(-12f * Time.unscaledDeltaTime));
            UpdateInspectionHover(overViewport ? screenPosition : new Vector2(-10000f, -10000f));
            RefreshInspectionCamera();
        }

        private void UpdateInspectionHover(Vector2 screenPosition)
        {
            ChapterTwoLubanBar next = inspectingItemId == "luban" ? FindInspectBar(screenPosition) : null;
            if (next == hoveredLubanBar) return;
            if (hoveredLubanBar != null) hoveredLubanBar.SetHovered(false);
            hoveredLubanBar = next;
            if (hoveredLubanBar != null) hoveredLubanBar.SetHovered(true);
        }

        private void TryInspectObjectClick(Vector2 screenPosition)
        {
            ChapterTwoLubanBar bestBar = FindInspectBar(screenPosition);
            if (bestBar != null) TryMoveLubanBar(bestBar);
        }

        private ChapterTwoLubanBar FindInspectBar(Vector2 screenPosition)
        {
            if (screenPosition.x < -1000f || inspectObjectViewport == null || inspectModelCamera == null) return null;
            RectTransform rect = inspectObjectViewport.rectTransform;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rect, screenPosition, null, out Vector2 local)) return null;
            Rect area = rect.rect;
            if (!area.Contains(local)) return null;
            Vector3 viewport = new(
                Mathf.InverseLerp(area.xMin, area.xMax, local.x),
                Mathf.InverseLerp(area.yMin, area.yMax, local.y), 0f);
            Ray ray = inspectModelCamera.ViewportPointToRay(viewport);
            // The inspection rig is normally isolated on layer 30, but a
            // stale layer assignment must not make the physical click path
            // disappear. Raycast all layers and accept only this rig's bars.
            int hitCount = Physics.RaycastNonAlloc(ray, inspectRayHits, 20f, ~0,
                QueryTriggerInteraction.Collide);
            ChapterTwoLubanBar bestBar = null;
            float bestDistance = float.PositiveInfinity;
            for (int i = 0; i < hitCount; i++)
            {
                RaycastHit hit = inspectRayHits[i];
                 ChapterTwoLubanBar candidate = hit.transform.GetComponentInParent<ChapterTwoLubanBar>();
                 if (candidate != null && inspectModelRoot != null &&
                     !candidate.transform.IsChildOf(inspectModelRoot)) candidate = null;
                if (candidate != null && hit.distance < bestDistance)
                {
                    bestDistance = hit.distance;
                    bestBar = candidate;
                }
            }
            ChapterTwoLubanBar projected = FindProjectedLubanBar(viewport, area, true);
            if (projected != null && (bestBar == null || bestBar.Index != lubanProgress)) return projected;
            return bestBar ?? FindProjectedLubanBar(viewport, area, false);
        }

        private ChapterTwoLubanBar FindProjectedLubanBar(Vector3 clickViewport, Rect viewportArea, bool expectedOnly)
        {
            if (inspectModelRoot == null || inspectModelCamera == null) return null;
            ChapterTwoLubanBar closest = null;
            float closestPixels = float.PositiveInfinity;
            float threshold = Mathf.Max(42f, Mathf.Min(viewportArea.width, viewportArea.height) * .14f);
            foreach (ChapterTwoLubanBar bar in inspectModelRoot.GetComponentsInChildren<ChapterTwoLubanBar>(true))
            {
                if (bar == null || bar.IsUnlocked || expectedOnly && bar.Index != lubanProgress) continue;
                Renderer[] barRenderers = bar.GetComponentsInChildren<Renderer>(true);
                if (barRenderers.Length == 0) continue;
                Bounds bounds = barRenderers[0].bounds;
                for (int i = 1; i < barRenderers.Length; i++) bounds.Encapsulate(barRenderers[i].bounds);
                Vector3 projected = inspectModelCamera.WorldToViewportPoint(bounds.center);
                if (projected.z <= 0f) continue;
                float dx = (projected.x - clickViewport.x) * viewportArea.width;
                float dy = (projected.y - clickViewport.y) * viewportArea.height;
                float pixels = Mathf.Sqrt(dx * dx + dy * dy);
                if (pixels < closestPixels)
                {
                    closestPixels = pixels;
                    closest = bar;
                }
            }
            return closestPixels <= threshold ? closest : null;
        }

        private Vector2 GetLubanValidationScreenPoint(int index)
        {
            if (inspectModelRoot == null || inspectModelCamera == null || inspectObjectViewport == null)
                return new Vector2(Screen.width * .5f, Screen.height * .5f);
            ChapterTwoLubanBar bar = null;
            foreach (ChapterTwoLubanBar candidate in inspectModelRoot.GetComponentsInChildren<ChapterTwoLubanBar>(true))
                if (candidate != null && candidate.Index == index) { bar = candidate; break; }
            if (bar == null) return new Vector2(Screen.width * .5f, Screen.height * .5f);
            Renderer[] renderers = bar.GetComponentsInChildren<Renderer>(true);
            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
            Vector3 viewport = inspectModelCamera.WorldToViewportPoint(bounds.center);
            Rect rect = inspectObjectViewport.rectTransform.rect;
            Vector2 local = new(
                Mathf.Lerp(rect.xMin, rect.xMax, viewport.x),
                Mathf.Lerp(rect.yMin, rect.yMax, viewport.y));
            return RectTransformUtility.WorldToScreenPoint(null,
                inspectObjectViewport.rectTransform.TransformPoint(local));
        }

        private void TryMoveLubanBar(ChapterTwoLubanBar bar)
        {
            if (inspectingItemId != "luban" || bar == null) return;
            if (bar.Index != lubanProgress)
            {
                inspectBody.text = "榫头被另一根梁卡住了。\n\n旋转木匣，寻找当前能真正滑动的那一根。";
                return;
            }
            int releasedIndex = lubanProgress + 1;
            bar.Unlock();
            lubanProgress++;
            if (lubanProgress >= 4)
            {
                lubanSolved = true;
                InventoryItem item = inventory.Find(value => value.id == "luban");
                if (item != null)
                    item.description = "四根木梁依次退出，底板露出两枚铜片：7 · 1。边缘刻着一个字：声。";
                inspectBody.text = "四根榫梁依次退出。\n\n底板露出两枚铜片：7 · 1。\n这是门锁最后一组声纹。";
                if (inspectLubanSolveAnimation != null) StopCoroutine(inspectLubanSolveAnimation);
                inspectLubanSolveAnimation = StartCoroutine(PlayLubanSolvedAnimation());
            }
            else
            {
                inspectBody.text = "第 " + releasedIndex + " 根榫梁已经抽出。\n\n内部结构重新露出，还有 " +
                    (4 - lubanProgress) + " 根梁没有解除。";
            }
        }

        private IEnumerator PlayLubanSolvedAnimation()
        {
            if (inspectLubanLidPivot == null) yield break;
            Quaternion closed = inspectLubanLidPivot.localRotation;
            Quaternion opened = closed * Quaternion.Euler(0f, 0f, -112f);
            Transform copperCore = inspectModelRoot != null ? inspectModelRoot.Find("检视榫匣铜芯") : null;
            Vector3 coreScale = copperCore != null ? copperCore.localScale : Vector3.one;
            float elapsed = 0f;
            const float duration = 1.05f;
            while (elapsed < duration && inspectLubanLidPivot != null)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float eased = t * t * (3f - 2f * t);
                inspectLubanLidPivot.localRotation = Quaternion.SlerpUnclamped(closed, opened, eased);
                if (copperCore != null)
                {
                    float pulse = 1f + Mathf.Sin(t * Mathf.PI) * .18f;
                    copperCore.localScale = coreScale * pulse;
                }
                inspectYaw += Mathf.Sin(t * Mathf.PI) * Time.unscaledDeltaTime * 7f;
                yield return null;
            }
            if (inspectLubanLidPivot != null) inspectLubanLidPivot.localRotation = opened;
            if (copperCore != null) copperCore.localScale = coreScale;
            inspectLubanSolveAnimation = null;
        }

        private void EnsureInspectionRig()
        {
            if (inspectModelRoot == null)
            {
                GameObject root = new("物件检视模型舞台");
                root.transform.position = new Vector3(1000f, 1000f, 1000f);
                inspectModelRoot = root.transform;
                SetLayerRecursively(root, 30);
            }
            if (inspectModelCamera == null)
            {
                GameObject cameraObject = new("物件检视摄像机");
                inspectModelCamera = cameraObject.AddComponent<Camera>();
                inspectModelCamera.clearFlags = CameraClearFlags.SolidColor;
                inspectModelCamera.backgroundColor = new Color(.002f, .007f, .008f, 1f);
                inspectModelCamera.cullingMask = 1 << 30;
                inspectModelCamera.fieldOfView = 30f;
                inspectModelCamera.nearClipPlane = .03f;
                inspectModelCamera.farClipPlane = 30f;
                inspectModelCamera.allowHDR = true;
                inspectModelCamera.allowMSAA = true;
                GameObject keyLightObject = new("物件检视主光");
                inspectKeyLight = keyLightObject.AddComponent<Light>();
                inspectKeyLight.type = LightType.Spot;
                inspectKeyLight.color = new Color(1f, .73f, .43f);
                inspectKeyLight.intensity = 7.2f;
                inspectKeyLight.range = 15f;
                inspectKeyLight.spotAngle = 58f;
                inspectKeyLight.innerSpotAngle = 32f;
                inspectKeyLight.cullingMask = 1 << 30;
                inspectKeyLight.shadows = LightShadows.None;
                GameObject fillLightObject = new("物件检视冷光");
                inspectFillLight = fillLightObject.AddComponent<Light>();
                inspectFillLight.type = LightType.Point;
                inspectFillLight.color = new Color(.24f, .62f, .67f);
                inspectFillLight.intensity = 3.8f;
                inspectFillLight.range = 11f;
                inspectFillLight.cullingMask = 1 << 30;
                inspectFillLight.shadows = LightShadows.None;
                GameObject rimLightObject = new("物件检视轮廓光");
                inspectRimLight = rimLightObject.AddComponent<Light>();
                inspectRimLight.type = LightType.Spot;
                inspectRimLight.color = new Color(.30f, .80f, .69f);
                inspectRimLight.intensity = 5.4f;
                inspectRimLight.range = 14f;
                inspectRimLight.spotAngle = 72f;
                inspectRimLight.innerSpotAngle = 38f;
                inspectRimLight.cullingMask = 1 << 30;
                inspectRimLight.shadows = LightShadows.None;
            }
            if (inspectModelTexture == null)
            {
                inspectModelTexture = new RenderTexture(1024, 1024, 24, RenderTextureFormat.ARGB32)
                {
                    name = "ChapterTwoInspectModelRT",
                    filterMode = FilterMode.Bilinear,
                    antiAliasing = 4,
                    useMipMap = false,
                    autoGenerateMips = false
                };
                inspectModelTexture.Create();
            }
            inspectModelCamera.targetTexture = inspectModelTexture;
            inspectModelCamera.enabled = true;
            inspectObjectViewport.texture = inspectModelTexture;
        }

        private void BuildInspectionModel(InventoryItem item)
        {
            if (inspectModelRoot == null) return;
            if (inspectLubanSolveAnimation != null)
            {
                StopCoroutine(inspectLubanSolveAnimation);
                inspectLubanSolveAnimation = null;
            }
            inspectLubanLidPivot = null;
            for (int i = inspectModelRoot.childCount - 1; i >= 0; i--) Destroy(inspectModelRoot.GetChild(i).gameObject);
            if (hoveredLubanBar != null) hoveredLubanBar.SetHovered(false);
            hoveredLubanBar = null;
            lubanProgress = lubanSolved ? 4 : 0;
            inspectModelRoot.localRotation = Quaternion.identity;
            inspectYaw = item.id == "luban" ? -32f : item.id == "flashlight" ? -18f : 0f;
            inspectAngularVelocity = Vector2.zero;
            inspectPitch = item.id == "luban" ? 17f : item.id == "flashlight" ? 12f : 36f;
            Material wood = MakeMaterial("InspectWood_" + item.id, new Color(.18f, .045f, .020f), .30f, .03f);
            Material brass = MakeMaterial("InspectBrass_" + item.id, new Color(.54f, .30f, .085f), .61f, .52f);
            Material paper = MakeMaterial("InspectPaper_" + item.id, new Color(.69f, .54f, .31f), .16f, 0f);
            Material grain = MakeMaterial("InspectGrain_" + item.id, new Color(.038f, .012f, .008f), .20f, 0f);
            Material iron = MakeMaterial("InspectIron_" + item.id, new Color(.035f, .052f, .052f), .52f, .62f);
            Material patina = MakeMaterial("InspectPatina_" + item.id, new Color(.06f, .25f, .22f), .40f, .28f);
            ApplySurfaceTexture(wood, SurfaceKind.Wood, new Color(.18f, .045f, .020f));
            ApplySurfaceTexture(paper, SurfaceKind.Paper, new Color(.69f, .54f, .31f));
            ApplySurfaceTexture(iron, SurfaceKind.Stone, new Color(.035f, .052f, .052f));
            if (item.id == "luban")
                BuildInspectLubanModel(wood, brass, grain, iron, patina);
            else if (item.id == "flashlight")
                BuildInspectFlashlightModel(brass, iron, patina, paper);
            else
                BuildInspectPaperModel(item.id, paper, wood, brass, grain, patina);
            if (item.id == "luban" && lubanSolved && inspectLubanLidPivot != null)
                inspectLubanLidPivot.localRotation = Quaternion.Euler(0f, 0f, -112f);
            SetLayerRecursively(inspectModelRoot.gameObject, 30);
            Bounds bounds = CalculateInspectionBounds();
            inspectFocusPoint = bounds.center;
            float radius = Mathf.Max(.45f, bounds.extents.magnitude);
            float fittedDistance = radius / Mathf.Tan(inspectModelCamera.fieldOfView * .5f * Mathf.Deg2Rad) * .86f;
            inspectMinimumDistance = Mathf.Max(1.15f, fittedDistance * .58f);
            inspectMaximumDistance = Mathf.Max(inspectMinimumDistance + 2.4f, fittedDistance * 1.85f);
            inspectTargetDistance = Mathf.Clamp(fittedDistance, inspectMinimumDistance, inspectMaximumDistance);
            inspectDistance = inspectTargetDistance;
            AddInspectionPedestal(bounds, iron, brass);
            SetLayerRecursively(inspectModelRoot.gameObject, 30);
            inspectModelRoot.gameObject.SetActive(true);
            if (inspectIcon != null) inspectIcon.enabled = false;
            RefreshInspectionCamera();
        }

        private void BuildInspectLubanModel(Material wood, Material brass, Material grain,
            Material iron, Material patina)
        {
            Cylinder("检视榫匣底盘", inspectModelRoot, new Vector3(0f, -.74f, 0f),
                new Vector3(.78f, .045f, .78f), iron, false);
            Cylinder("检视榫匣铜环", inspectModelRoot, new Vector3(0f, -.685f, 0f),
                new Vector3(.59f, .018f, .59f), brass, false);
            Cylinder("检视榫匣内盘", inspectModelRoot, new Vector3(0f, -.655f, 0f),
                new Vector3(.42f, .018f, .42f), patina, false);
            inspectLubanLidPivot = new GameObject("检视榫匣开盖铰链").transform;
            inspectLubanLidPivot.SetParent(inspectModelRoot, false);
            inspectLubanLidPivot.localPosition = new Vector3(-.58f, -.56f, 0f);
            Box("检视榫匣机关盖", inspectLubanLidPivot, new Vector3(.58f, 0f, 0f),
                new Vector3(1.16f, .105f, 1.08f), wood, false);
            Box("检视榫匣盖铜扣", inspectLubanLidPivot, new Vector3(1.12f, .065f, 0f),
                new Vector3(.10f, .075f, .28f), brass, false);
            CreateInspectLubanBar(0, new Vector3(0f, .48f, 0f), new Vector3(1.88f, .22f, .30f), wood, grain, Vector3.right);
            CreateInspectLubanBar(1, new Vector3(.46f, 0f, -.02f), new Vector3(.27f, 1.84f, .31f), brass, grain, Vector3.up);
            CreateInspectLubanBar(2, new Vector3(-.37f, -.43f, 0f), new Vector3(.27f, .24f, 1.82f), wood, grain, Vector3.forward);
            CreateInspectLubanBar(3, new Vector3(0f, -.31f, .43f), new Vector3(1.72f, .23f, .27f), brass, grain, Vector3.left);
            Box("检视榫匣中央铁芯", inspectModelRoot, Vector3.zero, new Vector3(.38f, .38f, .38f), iron, false);
            Sphere("检视榫匣铜芯", inspectModelRoot, new Vector3(0f, .01f, -.22f),
                new Vector3(.28f, .28f, .16f), brass, false);
            GameObject braceA = Box("检视榫匣斜锁梁甲", inspectModelRoot, new Vector3(0f, .04f, -.34f),
                new Vector3(1.32f, .115f, .115f), patina, false);
            braceA.transform.localRotation = Quaternion.Euler(0f, 42f, 0f);
            GameObject braceB = Box("检视榫匣斜锁梁乙", inspectModelRoot, new Vector3(0f, -.05f, .34f),
                new Vector3(1.28f, .105f, .105f), iron, false);
            braceB.transform.localRotation = Quaternion.Euler(0f, -42f, 0f);
            for (int i = 0; i < 8; i++)
            {
                float angle = i * Mathf.PI * .25f;
                Vector3 pinPosition = new(Mathf.Cos(angle) * .63f, -.62f, Mathf.Sin(angle) * .63f);
                Cylinder("检视榫匣定位钉_" + i, inspectModelRoot, pinPosition,
                    new Vector3(.045f, .06f, .045f), i % 2 == 0 ? brass : patina, false);
            }
        }

        private void BuildInspectFlashlightModel(Material brass, Material iron, Material patina, Material lens)
        {
            GameObject body = Cylinder("检视手电筒筒身", inspectModelRoot, Vector3.zero,
                new Vector3(.28f, .82f, .28f), iron, false);
            body.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            GameObject grip = Cylinder("检视手电筒握柄", inspectModelRoot, new Vector3(-.65f, 0f, 0f),
                new Vector3(.32f, .38f, .32f), patina, false);
            grip.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            for (int i = 0; i < 6; i++)
            {
                GameObject ridge = Cylinder("检视手电筒防滑环_" + i, inspectModelRoot,
                    new Vector3(-.90f + i * .105f, 0f, 0f), new Vector3(.335f, .022f, .335f), iron, false);
                ridge.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            }
            GameObject collar = Cylinder("检视手电筒灯头环", inspectModelRoot, new Vector3(.72f, 0f, 0f),
                new Vector3(.49f, .22f, .49f), brass, false);
            collar.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            GameObject glass = Cylinder("检视手电筒玻璃", inspectModelRoot, new Vector3(.96f, 0f, 0f),
                new Vector3(.39f, .025f, .39f), MakeEmissionMaterial("InspectFlashlightLens", new Color(.45f, .78f, .66f), 1.8f), false);
            glass.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            Box("检视手电筒开关", inspectModelRoot, new Vector3(.02f, .31f, 0f),
                new Vector3(.26f, .08f, .18f), brass, false);
            Cylinder("检视手电筒尾盖", inspectModelRoot, new Vector3(-1.08f, 0f, 0f),
                new Vector3(.30f, .10f, .30f), brass, false).transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
        }

        private void BuildInspectPaperModel(string itemId, Material paper, Material wood,
            Material brass, Material grain, Material patina)
        {
            Vector3 paperScale = itemId == "chair" ? new Vector3(1.48f, .035f, .70f) : new Vector3(1.68f, .045f, 1.16f);
            Box("检视纸页底层", inspectModelRoot, new Vector3(.035f, -.028f, .025f),
                paperScale + new Vector3(.04f, .008f, .04f), grain, false);
            Box("检视纸页", inspectModelRoot, Vector3.zero, paperScale, paper, false);
            Box("检视纸页折痕", inspectModelRoot, new Vector3(0f, .052f, 0f),
                new Vector3(.025f, .008f, paperScale.z * .84f), wood, false);
            int lineCount = itemId == "score" ? 7 : 4;
            for (int i = 0; i < lineCount; i++)
            {
                float z = Mathf.Lerp(-paperScale.z * .32f, paperScale.z * .34f, i / Mathf.Max(1f, lineCount - 1f));
                Material inkMaterial = itemId == "wall" && (i & 1) == 0 ? patina : wood;
                Box("检视纸页墨线_" + i, inspectModelRoot, new Vector3(-.10f + (i % 2) * .07f, .058f, z),
                    new Vector3(paperScale.x * (i % 3 == 0 ? .68f : .78f), .008f, .018f), inkMaterial, false);
            }
            Cylinder("检视朱印", inspectModelRoot, new Vector3(paperScale.x * .31f, .065f, -paperScale.z * .31f),
                new Vector3(.15f, .014f, .15f), brass, false);
            Box("检视纸页缺角", inspectModelRoot, new Vector3(-paperScale.x * .42f, .064f, paperScale.z * .40f),
                new Vector3(.18f, .012f, .10f), grain, false).transform.localRotation = Quaternion.Euler(0f, 18f, 0f);
        }

        private Bounds CalculateInspectionBounds()
        {
            Renderer[] renderers = inspectModelRoot.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0) return new Bounds(inspectModelRoot.position, Vector3.one);
            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
            return bounds;
        }

        private void AddInspectionPedestal(Bounds modelBounds, Material iron, Material brass)
        {
            float radius = Mathf.Clamp(Mathf.Max(modelBounds.extents.x, modelBounds.extents.z) * 1.15f, .72f, 1.45f);
            float localY = modelBounds.min.y - inspectModelRoot.position.y - .10f;
            Cylinder("检视物件悬浮底台", inspectModelRoot, new Vector3(0f, localY, 0f),
                new Vector3(radius, .035f, radius), iron, false);
            Cylinder("检视物件底台细环", inspectModelRoot, new Vector3(0f, localY + .04f, 0f),
                new Vector3(radius * .82f, .012f, radius * .82f), brass, false);
        }

        private void CreateInspectLubanBar(int index, Vector3 position, Vector3 scale, Material material, Material grain, Vector3 exitDirection)
        {
            GameObject bar = Box("检视榫梁_" + index, inspectModelRoot, position, scale, material, true);
            bool alongX = scale.x >= scale.y && scale.x >= scale.z;
            bool alongY = scale.y >= scale.x && scale.y >= scale.z;
            Vector3 stripeSize = alongX ? new Vector3(scale.x * .66f, .022f, .026f) :
                alongY ? new Vector3(.026f, scale.y * .66f, .022f) : new Vector3(.026f, .022f, scale.z * .66f);
            Vector3 stripeScale = new(
                stripeSize.x / Mathf.Max(.01f, scale.x),
                stripeSize.y / Mathf.Max(.01f, scale.y),
                stripeSize.z / Mathf.Max(.01f, scale.z));
            for (int stripe = 0; stripe < 2; stripe++)
            {
                float offset = stripe == 0 ? -.40f : .40f;
                Vector3 localPosition = alongX ? new Vector3(0f, offset, .51f) :
                    alongY ? new Vector3(.51f, offset, 0f) : new Vector3(.51f, 0f, offset);
                Box("检视榫梁木纹_" + index + "_" + stripe, bar.transform, localPosition, stripeScale, grain, false);
            }
            for (int end = -1; end <= 1; end += 2)
            {
                Vector3 capPosition = alongX ? new Vector3(end * .49f, 0f, 0f) :
                    alongY ? new Vector3(0f, end * .49f, 0f) : new Vector3(0f, 0f, end * .49f);
                Vector3 capScale = alongX ? new Vector3(.035f / scale.x, .82f, .82f) :
                    alongY ? new Vector3(.82f, .035f / scale.y, .82f) : new Vector3(.82f, .82f, .035f / scale.z);
                Box("检视榫梁端榫_" + index + "_" + end, bar.transform, capPosition, capScale, grain, false);
            }
            Vector3 notchPosition = alongX ? new Vector3(.22f, .51f, 0f) :
                alongY ? new Vector3(.51f, -.18f, 0f) : new Vector3(0f, .51f, .20f);
            Vector3 notchScale = alongX ? new Vector3(.12f, .055f / scale.y, .46f) :
                alongY ? new Vector3(.055f / scale.x, .13f, .46f) : new Vector3(.46f, .055f / scale.y, .12f);
            Box("检视榫梁机关槽_" + index, bar.transform, notchPosition, notchScale, grain, false);
            Vector3 numberPosition = index switch
            {
                0 => new Vector3(0f, .15f, -.18f),
                1 => new Vector3(.18f, 0f, -.18f),
                2 => new Vector3(.17f, .13f, 0f),
                _ => new Vector3(.56f, .16f, -.18f)
            };
            TextMesh numberLabel = AddFloatingLabel(bar.transform, (index + 1).ToString(),
                numberPosition, new Vector3(0f, 180f, 0f), 40, new Color(.20f, .070f, .026f));
            if (numberLabel != null)
            {
                numberLabel.fontStyle = FontStyle.Normal;
                numberLabel.characterSize = .035f;
            }
            SetLayerRecursively(bar, 30);
            ChapterTwoLubanBar component = bar.AddComponent<ChapterTwoLubanBar>();
            component.Configure(this, index, exitDirection);
            if (lubanSolved) component.Unlock();
        }

        private void RefreshInspectionCamera()
        {
            if (inspectModelCamera == null || inspectModelRoot == null) return;
            Quaternion orbit = Quaternion.Euler(inspectPitch, inspectYaw, 0f);
            Vector3 target = inspectFocusPoint == Vector3.zero ? inspectModelRoot.position : inspectFocusPoint;
            inspectModelCamera.transform.position = target + orbit * (Vector3.back * inspectDistance);
            inspectModelCamera.transform.rotation = Quaternion.LookRotation(target - inspectModelCamera.transform.position, Vector3.up);
            if (inspectKeyLight != null)
            {
                inspectKeyLight.transform.position = target + new Vector3(-3.2f, 3.5f, -3.4f);
                inspectKeyLight.transform.rotation = Quaternion.LookRotation(target - inspectKeyLight.transform.position, Vector3.up);
            }
            if (inspectFillLight != null)
                inspectFillLight.transform.position = target + new Vector3(2.7f, .8f, -1.7f);
            if (inspectRimLight != null)
            {
                inspectRimLight.transform.position = target + new Vector3(2.4f, 2.2f, 3.0f);
                inspectRimLight.transform.rotation = Quaternion.LookRotation(target - inspectRimLight.transform.position, Vector3.up);
            }
            inspectModelCamera.Render();
        }

        private void CaptureInspectBackground()
        {
            if (viewCamera == null || inspectBlurBackground == null) return;
            int width = Mathf.Max(320, Screen.width / 4);
            int height = Mathf.Max(180, Screen.height / 4);
            if (inspectBlurTexture == null || inspectBlurTexture.width != width || inspectBlurTexture.height != height)
            {
                if (inspectBlurTexture != null) inspectBlurTexture.Release();
                inspectBlurTexture = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32)
                {
                    name = "ChapterTwoInspectBlurRT",
                    filterMode = FilterMode.Bilinear
                };
                inspectBlurTexture.Create();
            }
            RenderTexture previous = viewCamera.targetTexture;
            viewCamera.targetTexture = inspectBlurTexture;
            viewCamera.Render();
            viewCamera.targetTexture = previous;
            if (inspectBlurMaterial == null)
            {
                Shader shader = Shader.Find("Meixi/InventoryBlur");
                if (shader != null) inspectBlurMaterial = new Material(shader);
            }
            if (inspectBlurMaterial != null)
            {
                inspectBlurBackground.texture = inspectBlurTexture;
                inspectBlurBackground.material = inspectBlurMaterial;
            }
            else
            {
                RenderTexture active = RenderTexture.active;
                RenderTexture.active = inspectBlurTexture;
                if (inspectBlurImage == null || inspectBlurImage.width != width || inspectBlurImage.height != height)
                {
                    if (inspectBlurImage != null) Destroy(inspectBlurImage);
                    inspectBlurImage = new Texture2D(width, height, TextureFormat.RGB24, false)
                    {
                        name = "ChapterTwoInspectBlurFallback",
                        filterMode = FilterMode.Bilinear
                    };
                }
                inspectBlurImage.ReadPixels(new Rect(0f, 0f, width, height), 0, 0, false);
                inspectBlurImage.Apply(false, false);
                RenderTexture.active = active;
                BlurFallbackImage(inspectBlurImage);
                inspectBlurBackground.texture = inspectBlurImage;
                inspectBlurBackground.material = null;
            }
        }

        private static void BlurFallbackImage(Texture2D image)
        {
            if (image == null) return;
            Color[] source = image.GetPixels();
            Color[] result = new Color[source.Length];
            int width = image.width;
            int height = image.height;
            for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                Color sum = Color.black;
                int count = 0;
                for (int oy = -2; oy <= 2; oy++)
                for (int ox = -2; ox <= 2; ox++)
                {
                    int sx = Mathf.Clamp(x + ox * 4, 0, width - 1);
                    int sy = Mathf.Clamp(y + oy * 4, 0, height - 1);
                    sum += source[sy * width + sx];
                    count++;
                }
                result[y * width + x] = sum / count;
            }
            image.SetPixels(result);
            image.Apply(false, false);
        }

        private void CloseInspectionModel()
        {
            inspectDragging = false;
            inspectAngularVelocity = Vector2.zero;
            if (inspectLubanSolveAnimation != null)
            {
                StopCoroutine(inspectLubanSolveAnimation);
                inspectLubanSolveAnimation = null;
            }
            inspectLubanLidPivot = null;
            if (hoveredLubanBar != null) hoveredLubanBar.SetHovered(false);
            hoveredLubanBar = null;
            if (inspectModelRoot != null) inspectModelRoot.gameObject.SetActive(false);
            if (inspectModelCamera != null) inspectModelCamera.enabled = false;
            if (inspectIcon != null) inspectIcon.enabled = true;
            inspectingItemId = string.Empty;
        }

        private IEnumerator OpenDoorSequence()
        {
            phase = Phase.GateSequence;
            inputModal = true;
            PlayChapterTwoEffect(doorClip, .92f);
            if (doorLockVisual != null) doorLockVisual.SetActive(false);
            if (stageRuntime != null)
            {
                stageRuntime.HideLegacyPresentationForAdventure();
                stageRuntime.enabled = false;
            }
            objectiveText.text = "大门正在开启 · 建筑结构开始震动";
            SetMessage("门轴被什么东西卡住了……你用尽力气推开沉重的大门");
            Transform movingDoor = doorAssemblyRoot != null ? doorAssemblyRoot : doorObject.transform;
            Vector3 start = movingDoor.localPosition;
            float elapsed = 0f;
            while (elapsed < 2.8f)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / 2.8f);
                movingDoor.localPosition = start + Vector3.up * Mathf.SmoothStep(0f, 4.6f, t);
                cameraShake = UnityEngine.Random.insideUnitSphere * (.01f + t * .045f);
                yield return null;
            }
            if (doorAssemblyRoot != null)
                foreach (Collider collider in doorAssemblyRoot.GetComponentsInChildren<Collider>(true))
                    collider.enabled = false;
            Physics.SyncTransforms();
            MeixiLocalProgress.SetCheckpoint("ChapterTwoGateOpened");
            SpawnPuppets();
            BlackoutTheatre();
            SetMessage("所有灯光熄灭了。两个木偶从舞台上站了起来");
            yield return new WaitForSecondsRealtime(2.1f);
            Quaternion startRotation = player.rotation;
            // The gate opens onto the stairwell on the negative-Z side. Turn the
            // player toward the first flight during the blackout so the chase
            // starts on the landing instead of facing back into the theatre.
            Quaternion stageRotation = Quaternion.LookRotation(new Vector3(-.58f, 0f, -.81f), Vector3.up);
            float turnElapsed = 0f;
            while (turnElapsed < .55f)
            {
                turnElapsed += Time.unscaledDeltaTime;
                player.rotation = Quaternion.Slerp(startRotation, stageRotation,
                    Mathf.SmoothStep(0f, 1f, turnElapsed / .55f));
                yield return null;
            }
            yield return FadeScreen(1f, .34f);
            PlacePlayerAtStairEntry();
            BeginChase();
            Physics.SyncTransforms();
            yield return null;
            yield return null;
            yield return FadeScreen(0f, .46f);
        }

        private void PlacePlayerAtStairEntry()
        {
            if (player == null) return;
            bool wasEnabled = controller != null && controller.enabled;
            if (controller != null) controller.enabled = false;
            player.position = new Vector3(0f, .08f, -13.55f);
            player.rotation = Quaternion.Euler(0f, -125f, 0f);
            verticalVelocity = -2f;
            if (controller != null) controller.enabled = wasEnabled;
            Physics.SyncTransforms();
        }

        private void BlackoutTheatre()
        {
            theatreLights.Clear();
            foreach (Light light in FindObjectsByType<Light>())
            {
                if ((flashlight != null && light == flashlight) ||
                    (flashlightFill != null && light == flashlightFill)) continue;
                theatreLights.Add(new LightState { light = light, intensity = light.intensity, enabled = light.enabled });
                if (light.name.Contains("Puppet", StringComparison.Ordinal))
                {
                    light.enabled = true;
                    continue;
                }
                bool mazeLight = light.name.Contains("迷宫", StringComparison.Ordinal);
                mazeLight |= light.transform.position.y < -2f ||
                             light.name.StartsWith("Maze", StringComparison.OrdinalIgnoreCase) ||
                             light.name.Contains("StairLight", StringComparison.OrdinalIgnoreCase);
                bool fixedMazeLight = light.name.StartsWith("MazeLantern", StringComparison.OrdinalIgnoreCase) ||
                                      light.name.StartsWith("MazeExitLight", StringComparison.OrdinalIgnoreCase);
                bool stairLight = light.name.Contains("StairLight", StringComparison.OrdinalIgnoreCase) ||
                                  light.transform.position.y < -2f;
                if (fixedMazeLight)
                {
                    // Keep only a weak fixed-light trace so the flashlight has
                    // clear visual priority inside the maze.
                    light.intensity *= .24f;
                    light.enabled = true;
                }
                else if (stairLight)
                {
                    light.intensity *= .52f;
                    light.enabled = true;
                }
                else
                {
                    light.intensity *= .012f;
                    light.enabled = false;
                }
            }
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(.0075f, .0105f, .0125f);
            RenderSettings.ambientEquatorColor = new Color(.0055f, .0080f, .0090f);
            RenderSettings.ambientGroundColor = new Color(.0025f, .0035f, .0042f);
            RenderSettings.ambientIntensity = .095f;
            RenderSettings.reflectionIntensity = .36f;
            RenderSettings.fogColor = new Color(.0045f, .0072f, .0082f);
            RenderSettings.fogDensity = .018f;
            foreach (Volume volume in FindObjectsByType<Volume>())
            {
                VolumeProfile profile = volume.profile;
                if (profile != null && profile.TryGet(out ColorAdjustments color))
                    color.postExposure.Override(.10f);
            }
            foreach (TextMesh worldLabel in FindObjectsByType<TextMesh>())
                if (worldLabel.transform.position.y > -2f) worldLabel.gameObject.SetActive(false);
            EnsureFlashlight();
            flashlight.enabled = false;
            if (flashlightFill != null) flashlightFill.enabled = false;
        }

        private void BeginChase()
        {
            inputModal = false;
            phase = Phase.Chase;
            if (stageRuntime != null)
            {
                stageRuntime.HideLegacyPresentationForAdventure();
                stageRuntime.StopChapterMusic();
                stageRuntime.SetSharedGameplayControlsActive(false);
                stageRuntime.enabled = false;
            }
            StopConflictingChapterTwoBgm();
            staminaPanel.SetActive(true);
            stamina = 1f;
            staminaLastDrainAt = float.NegativeInfinity;
            mazeExitVisited = false;
            mazeSoulCollected = false;
            mazeExitHintNextAt = 0f;
            if (mazeSoulEcho != null) Destroy(mazeSoulEcho.gameObject);
            mazeSoulEcho = null;
            if (inventoryHud != null) inventoryHud.SetActive(false);
            if (controller == null) controller = player.GetComponent<CharacterController>();
            if (viewCamera == null) viewCamera = Camera.main ?? FindAnyObjectByType<Camera>();
            EnsureFlashlight();
            // Keep the camera on the upright player root so animation clips cannot
            // roll the whole screen. The eye-height anchor is above the chest and
            // is collision-tested by ResolveFirstPersonCameraLocal.
            firstPersonHead = null;
            playerAnimator = player.GetComponentInChildren<Animator>(true);
            if (playerAnimator != null)
            {
                playerAnimator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                playerAnimator.SetFloat("Speed", 0f);
                playerAnimator.SetBool("Grounded", true);
            }
            viewCamera.transform.SetParent(player, false);
            firstPersonCameraLocal = new Vector3(0f, 1.76f, .02f);
            // Hide the local character mesh from first person, but keep it enabled
            // for the mirror reflection camera.
            viewCamera.cullingMask &= ~(1 << PlayerVisualLayer);
            // The mirror overlay is the visible reflection surface. It must be
            // rendered by the player camera; only the reflection camera excludes
            // this layer to prevent recursive mirror renders.
            viewCamera.cullingMask |= 1 << MazeMirrorOverlayLayer;
            viewCamera.transform.localPosition = firstPersonCameraLocal;
            viewCamera.transform.localRotation = Quaternion.identity;
            viewCamera.fieldOfView = 72f;
            viewCamera.nearClipPlane = .035f;
            yaw = player.eulerAngles.y;
            pitch = 0f;
            objectiveText.text = "逃离旧戏台 · 从大门下楼，穿过一楼镜像迷宫";
            SetMessage("直视木偶可令其停下 · Shift 奔跑 · F 手电筒");
            MeixiCursorState.SetGameplay();
            SeedPuppetChaseRoute();
            primaryPuppetPath.Clear();
            alternatePuppetPath.Clear();
            puppetPathRefreshAt = 0f;
            for (int i = 0; i < puppetEntryLastPositions.Length; i++)
            {
                puppetEntryLastPositions[i] = i < puppets.Count && puppets[i] != null && puppets[i].root != null
                    ? puppets[i].root.position : Vector3.zero;
                puppetEntryStalledSince[i] = float.NegativeInfinity;
                puppetLowerStairLastPositions[i] = puppetEntryLastPositions[i];
                puppetLowerStairStalledSince[i] = float.NegativeInfinity;
            }
            lastTrailPosition = player.position;
            lastChaseStepPosition = player.position;
            stepClock = 0f;
            SetMazeLabelsVisible(maze != null && player.position.y < maze.floorY + 2f);
            flashlight.enabled = flashlightCollected;
            if (flashlightFill != null) flashlightFill.enabled = flashlightCollected;
            BuildMazeReflectionSnapshot();
            if (flashlightCollected) StartPuppetBgm();
            Debug.Log("[ChapterTwoAdventure] Chase active from theatre gate through maze exit.");
        }

        private void SeedPuppetChaseRoute()
        {
            chaseTrail.Clear();
            // The stairwell is a switchback, so a direct stage-to-player line
            // collides with the landing walls.  These waypoints follow the
            // actual walkable center of both flights and overlap each landing.
            chaseTrail.Add(new Vector3(0f, 1.12f, 5.25f));
            chaseTrail.Add(new Vector3(0f, .20f, 3.45f));
            chaseTrail.Add(new Vector3(0f, .08f, -3.50f));
            chaseTrail.Add(new Vector3(0f, .08f, -10.80f));
            chaseTrail.Add(new Vector3(0f, .08f, -13.55f));

            const int flightSteps = 17;
            // Match the generated stair centers exactly.  The old route was
            // offset from both flights, so the puppet's sphere cast could catch
            // the rail at the turn and never reach the lower landing.
            const float upperX = -1.55f;
            const float upperStartZ = -14.18f;
            const float upperStartY = -.38f;
            const float upperTread = .48f;
            const float upperRise = .21f;
            for (int i = 0; i < flightSteps; i++)
                chaseTrail.Add(new Vector3(upperX, upperStartY - i * upperRise + .14f,
                    upperStartZ - i * upperTread));

            chaseTrail.Add(new Vector3(0f, -3.63f, -21.84f));

            const float lowerX = 1.55f;
            const float lowerStartZ = -21.62f;
            const float lowerStartY = -3.80f;
            const float lowerTread = .38f;
            const float lowerRise = .23f;
            for (int i = 0; i < flightSteps; i++)
                chaseTrail.Add(new Vector3(lowerX, lowerStartY - i * lowerRise + .14f,
                    lowerStartZ - i * lowerTread));

            chaseTrail.Add(new Vector3(0f, -7.33f, -27.62f));
            // End inside the maze's open north cell.  Appending the player's
            // initial stair position here made the route reverse back up the
            // stairs after reaching the entrance.
            chaseTrail.Add(new Vector3(0f, -7.23f, -27.62f));
        }

        private void UpdateFirstPersonMovement(Keyboard keyboard)
        {
            if (keyboard == null || controller == null || viewCamera == null) return;
            Mouse mouse = Mouse.current;
            if (mouse != null && !MeixiCursorState.IsPointerMode)
            {
                Vector2 look = Vector2.ClampMagnitude(mouse.delta.ReadValue(), 34f);
                yaw += look.x * .092f;
                pitch = Mathf.Clamp(pitch - look.y * .082f, -72f, 76f);
            }
            player.rotation = Quaternion.Euler(0f, yaw, 0f);
            viewCamera.transform.localRotation = Quaternion.Euler(pitch, 0f, 0f);

            float x = (keyboard.dKey.isPressed ? 1f : 0f) - (keyboard.aKey.isPressed ? 1f : 0f);
            float z = (keyboard.wKey.isPressed ? 1f : 0f) - (keyboard.sKey.isPressed ? 1f : 0f);
            Vector3 move = Vector3.ClampMagnitude(player.right * x + player.forward * z, 1f);
            bool sprinting = move.sqrMagnitude > .02f && (keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed) && stamina > .03f;
            float speed = sprinting ? 6.6f : 3.75f;
            float frameDelta = Mathf.Min(Time.deltaTime, 1f / 30f);
            if (sprinting)
            {
                stamina = Mathf.Clamp01(stamina - .285f * frameDelta);
                staminaLastDrainAt = Time.unscaledTime;
            }
            else if (Time.unscaledTime - staminaLastDrainAt > .65f)
            {
                stamina = Mathf.Clamp01(stamina + (move.sqrMagnitude > .02f ? .055f : .18f) * frameDelta);
            }
            if (controller.isGrounded) verticalVelocity = -2.0f;
            else verticalVelocity = Mathf.Max(verticalVelocity + Physics.gravity.y * 2f * Time.deltaTime, -25f);
            controller.Move((move * speed + Vector3.up * verticalVelocity) * frameDelta);
            if (!mazeLabelsVisible && maze != null && player.position.y < maze.floorY + 2f)
                SetMazeLabelsVisible(true);
            bool playerInMaze = maze != null && player.position.y < maze.floorY + 2f;
            if (!playerInMaze && (player.position - lastTrailPosition).sqrMagnitude > .24f)
            {
                chaseTrail.Add(player.position);
                lastTrailPosition = player.position;
            }

            Vector3 chaseStepDelta = player.position - lastChaseStepPosition;
            chaseStepDelta.y = 0f;
            lastChaseStepPosition = player.position;
            if (playerAnimator != null && playerAnimator.runtimeAnimatorController != null)
            {
                float actualSpeed = chaseStepDelta.magnitude / Mathf.Max(.0001f, frameDelta);
                playerAnimator.SetFloat("Speed", Mathf.Clamp01(actualSpeed / 5.2f) * .62f,
                    .060f, frameDelta);
                playerAnimator.SetBool("Grounded", controller.isGrounded);
            }
            if (move.sqrMagnitude > .02f && controller.isGrounded && chaseStepDelta.magnitude < .75f)
            {
                stepClock += chaseStepDelta.magnitude;
                float stride = sprinting ? 1.35f : 1.58f;
                if (stepClock >= stride)
                {
                    stepClock %= stride;
                    footstepSource.pitch = UnityEngine.Random.Range(.96f, 1.04f);
                    footstepSource.volume = sprinting ? .58f : .43f;
                    footstepSource.PlayOneShot(footstepClips[UnityEngine.Random.Range(0, footstepClips.Length)]);
                    cameraShake += new Vector3(0f, sprinting ? -.012f : -.006f, 0f);
                }
            }
            else stepClock = Mathf.Min(stepClock, .18f);
            if (keyboard.fKey.wasPressedThisFrame && flashlightCollected) ToggleFlashlight();
            if (player.position.y < -12f) CatchPlayer();
            else if (player.position.y < -8.55f)
                RecoverFromStairwellFall();
        }

        private void RecoverFromStairwellFall()
        {
            if (player == null) return;
            bool wasEnabled = controller != null && controller.enabled;
            if (controller != null) controller.enabled = false;
            player.position = new Vector3(0f, -7.27f, -26.72f);
            player.rotation = Quaternion.Euler(0f, -90f, 0f);
            verticalVelocity = -2f;
            if (controller != null) controller.enabled = wasEnabled;
            Physics.SyncTransforms();
            SetMessage("楼梯下方的安全层托住了你", 1.8f);
        }

        private void UpdatePuppets()
        {
            if (viewCamera == null || player == null) return;
            bool playerInMaze = maze != null && player.position.y < maze.floorY + 2f;
            bool primaryAtMaze = puppets.Count > 0 && CanPuppetUseMazeRoute(puppets[0], 0, playerInMaze);
            bool alternateAtMaze = puppets.Count > 1 && CanPuppetUseMazeRoute(puppets[1], 1, playerInMaze);
            if (playerInMaze && Time.unscaledTime >= puppetPathRefreshAt)
            {
                primaryPuppetPath.Clear();
                alternatePuppetPath.Clear();
                if (primaryAtMaze)
                    maze.BuildPath(puppets[0].root.position, player.position, false, primaryPuppetPath);
                if (alternateAtMaze)
                    maze.BuildPath(puppets[1].root.position, player.position, true, alternatePuppetPath,
                        primaryPuppetPath);
                puppetPathRefreshAt = Time.unscaledTime + .18f;
            }

            for (int index = 0; index < puppets.Count; index++)
            {
                PuppetAgent puppet = puppets[index];
                if (puppet == null || puppet.root == null) continue;
                bool watched = IsPuppetWatchedPrecise(puppet);
                bool useMazeRoute = index == 0 ? primaryAtMaze : alternateAtMaze;
                List<Vector3> route = useMazeRoute
                    ? (index == 0 ? primaryPuppetPath : alternatePuppetPath) : chaseTrail;
                bool harmless = puppet.Update(route, player.position, watched, Time.deltaTime, footstepClips);
                if (!harmless)
                    RecoverPuppetOnLowerStair(puppet, index);
                harmless |= playerInMaze && !useMazeRoute;
                if (!harmless && !suppressPuppetCatchForValidation &&
                    Vector3.Distance(puppet.root.position + Vector3.up * .8f,
                        player.position + Vector3.up * .8f) < 1.18f)
                {
                    CatchPlayer();
                    return;
                }
            }
        }

        private void RecoverPuppetOnLowerStair(PuppetAgent puppet, int index)
        {
            if (puppet == null || puppet.root == null || index < 0 || index >= puppetLowerStairLastPositions.Length)
                return;

            Vector3 position = puppet.root.position;
            // This volume covers only the second flight and its two joins.  A
            // recovery here cannot affect normal theatre or maze movement.
            bool onLowerFlight = position.x > -.15f && position.x < 3.18f &&
                position.y < -3.18f && position.y > -7.56f &&
                position.z < -21.20f && position.z > -27.52f;
            if (!onLowerFlight)
            {
                puppetLowerStairLastPositions[index] = position;
                puppetLowerStairStalledSince[index] = float.NegativeInfinity;
                return;
            }

            Vector3 planarDelta = position - puppetLowerStairLastPositions[index];
            planarDelta.y = 0f;
            if (planarDelta.sqrMagnitude > .0049f)
            {
                puppetLowerStairLastPositions[index] = position;
                puppetLowerStairStalledSince[index] = Time.unscaledTime;
                return;
            }
            if (float.IsNegativeInfinity(puppetLowerStairStalledSince[index]))
            {
                puppetLowerStairStalledSince[index] = Time.unscaledTime;
                return;
            }
            if (Time.unscaledTime - puppetLowerStairStalledSince[index] < .65f) return;

            // The lower stair is a measured 17-step flight. Move a genuinely
            // stalled puppet to the next clear pair of treads on its centerline;
            // this bypasses decorative edge colliders without teleporting it
            // through a wall or into the maze.
            float progress = Mathf.Max(0f, (-21.62f - position.z) / .38f);
            int safeStep = Mathf.Clamp(Mathf.FloorToInt(progress) + 2, 2, 16);
            Vector3 safePosition = new(1.55f, -3.66f - safeStep * .23f,
                -21.62f - safeStep * .38f);
            if (safeStep >= 16 || position.z < -27.12f)
                safePosition = new Vector3(0f, -7.23f, -27.62f);
            puppet.PlaceAtRouteHandoff(safePosition);
            puppetLowerStairLastPositions[index] = safePosition;
            puppetLowerStairStalledSince[index] = float.NegativeInfinity;
            Physics.SyncTransforms();
            Debug.Log("[ChapterTwoAdventure] Puppet " + index + " recovered on lower stair.");
        }

        private bool CanPuppetUseMazeRoute(PuppetAgent puppet, int index, bool playerInMaze)
        {
            if (!playerInMaze || maze == null || puppet == null || puppet.root == null) return false;
            Vector3 position = puppet.root.position;
            if (maze.CanEnterPathFrom(position))
            {
                puppetEntryStalledSince[index] = float.NegativeInfinity;
                puppetEntryLastPositions[index] = position;
                return true;
            }

            // The physical lower landing is intentionally narrow. If a moving
            // puppet touches an ornamental edge it should still walk normally;
            // only a genuine stall near the maze portal receives a small route
            // handoff into the open north cell.
            bool atPortal = position.y < maze.floorY + 1.70f && position.z < maze.startZ + 2.20f;
            if (!atPortal)
            {
                puppetEntryStalledSince[index] = float.NegativeInfinity;
                puppetEntryLastPositions[index] = position;
                return false;
            }

            if ((position - puppetEntryLastPositions[index]).sqrMagnitude > .0025f)
            {
                puppetEntryLastPositions[index] = position;
                puppetEntryStalledSince[index] = Time.unscaledTime;
                return false;
            }
            if (float.IsNegativeInfinity(puppetEntryStalledSince[index]))
            {
                puppetEntryStalledSince[index] = Time.unscaledTime;
                return false;
            }
            if (Time.unscaledTime - puppetEntryStalledSince[index] < 1.15f) return false;

            puppet.PlaceAtRouteHandoff(maze.EntryPoint);
            puppetEntryLastPositions[index] = maze.EntryPoint;
            puppetEntryStalledSince[index] = float.NegativeInfinity;
            Physics.SyncTransforms();
            Debug.Log("[ChapterTwoAdventure] Puppet " + index + " recovered into maze entry route.");
            return true;
        }

        private void UpdateChaseSetPropInteraction(Keyboard keyboard)
        {
            if (viewCamera == null || player == null) return;
            UpdateFocusedInteractable();
            ChapterTwoSetProp focusedProp = NearestFocusedSetProp();
            if (focusedProp == null) return;
            SetHintVisible(true, "[ E ]  " + focusedProp.Prompt);
            Mouse mouse = Mouse.current;
            bool interactPressed = (keyboard != null && keyboard.eKey.wasPressedThisFrame) ||
                                   (mouse != null && mouse.leftButton.wasPressedThisFrame);
            if (!interactPressed) return;
            string response = focusedProp.Interact();
            if (!string.IsNullOrEmpty(response)) SetMessage(response, 2.6f);
        }

        private bool IsPuppetWatched(PuppetAgent puppet)
        {
            Vector3 focus = puppet.root.position + Vector3.up * 1.35f;
            Vector3 viewport = viewCamera.WorldToViewportPoint(focus);
            if (viewport.z <= 0f || viewport.x < -.03f || viewport.x > 1.03f || viewport.y < -.05f || viewport.y > 1.05f) return false;
            Vector3 direction = focus - viewCamera.transform.position;
            return Vector3.Dot(viewCamera.transform.forward, direction.normalized) >= .48f;
        }

        private bool IsPuppetWatchedPrecise(PuppetAgent puppet)
        {
            if (puppet == null || puppet.root == null || viewCamera == null) return false;
            Vector3 cameraPosition = viewCamera.transform.position;
            foreach (Vector3 offset in PuppetVisibilityOffsets)
            {
                Vector3 point = puppet.root.TransformPoint(offset);
                Vector3 viewport = viewCamera.WorldToViewportPoint(point);
                if (viewport.z <= 0f || viewport.x < -.015f || viewport.x > 1.015f ||
                    viewport.y < -.025f || viewport.y > 1.025f) continue;
                Vector3 direction = point - cameraPosition;
                float distance = direction.magnitude;
                if (distance <= .01f || Vector3.Dot(viewCamera.transform.forward, direction / distance) < .08f) continue;
                if (!Physics.Raycast(cameraPosition, direction / distance, distance - .08f, ~0,
                        QueryTriggerInteraction.Ignore))
                    return true;
            }
            return false;
        }

        private void UpdateChaseHud()
        {
            hudRefreshClock -= Time.unscaledDeltaTime;
            if (hudRefreshClock > 0f) return;
            hudRefreshClock = .08f;
            staminaFill.fillAmount = stamina;
            staminaFill.color = stamina < .20f ? new Color(1f, .23f, .15f) : new Color(.28f, .91f, .76f);
            staminaText.text = "体 力  " + Mathf.RoundToInt(stamina * 100f) + "%";
            if (Time.unscaledTime >= messageUntil) SetHintVisible(false, string.Empty);
        }

        private void CheckMazeExit()
        {
            if (mazeExitObject == null || player == null) return;
            Vector3 delta = mazeExitObject.transform.position - player.position;
            delta.y = 0f;
            if (delta.sqrMagnitude >= 4.0f) return;
            if (!mazeExitVisited)
            {
                mazeExitVisited = true;
                SpawnMazeSoulEcho();
                return;
            }
            if (!mazeSoulCollected)
            {
                if (Time.unscaledTime >= mazeExitHintNextAt)
                {
                    mazeExitHintNextAt = Time.unscaledTime + 1.25f;
                    SetMessage("出口没有回应，迷宫中央还残留着一枚音魂", 2.0f);
                }
                return;
            }
            StartCoroutine(CompleteEscape());
        }

        private void SpawnMazeSoulEcho()
        {
            if (maze == null) return;
            if (mazeSoulEcho != null) Destroy(mazeSoulEcho.gameObject);

            Vector3 position = new Vector3(0f, maze.floorY + .92f, maze.startZ - maze.cellSize * 5f);
            Transform root = new GameObject("ChapterTwo_MazeSoulEcho").transform;
            root.SetParent(transform, false);
            root.position = position;
            Material soulMaterial = MakeEmissionMaterial("ChapterTwo_MazeSoulMaterial", new Color(.17f, .78f, .72f), 2.4f);
            Sphere("音魂核心", root, Vector3.zero, new Vector3(.24f, .24f, .24f), soulMaterial, false);
            Sphere("音魂外环", root, Vector3.zero, new Vector3(.62f, .045f, .62f), soulMaterial, false);
            Sphere("音魂上环", root, Vector3.up * .28f, new Vector3(.34f, .028f, .34f), soulMaterial, false);
            Light soulLight = root.gameObject.AddComponent<Light>();
            soulLight.type = LightType.Point;
            soulLight.color = new Color(.18f, .82f, .72f);
            soulLight.intensity = .95f;
            soulLight.range = 4.8f;
            soulLight.shadows = LightShadows.None;
            mazeSoulEcho = root.gameObject.AddComponent<ChapterTwoMazeSoulEchoPickup>();
            mazeSoulEcho.Configure(this, position);
            objectiveText.text = "音魂已在迷宫中央苏醒 · 收集后再次返回出口";
            SetMessage("第一次抵达出口只唤醒了迷宫中央的音魂", 3.2f);
            Debug.Log("[ChapterTwoAdventure] Maze exit visited; central soul echo spawned.");
        }

        public Vector3 MazePlayerPosition => player != null ? player.position : Vector3.zero;

        public void CollectMazeSoulEcho(ChapterTwoMazeSoulEchoPickup pickup)
        {
            if (pickup == null || pickup != mazeSoulEcho || mazeSoulCollected) return;
            mazeSoulCollected = true;
            mazeSoulEcho = null;
            Destroy(pickup.gameObject);
            objectiveText.text = "音魂已收集 · 返回迷宫出口";
            SetMessage("音魂回到掌心，出口的镜面开始松动", 3.0f);
            Debug.Log("[ChapterTwoAdventure] Central maze soul echo collected; exit unlocked.");
        }

        private IEnumerator CompleteEscape()
        {
            if (phase == Phase.Ending) yield break;
            phase = Phase.Ending;
            inputModal = true;
            objectiveText.text = "你逃出了镜像迷宫 · 木偶的脚步停在黑暗里";
            SetHintVisible(true, "余音把你送回最初的梅溪村……");
            yield return FadeScreen(1f, 1.15f);
            if (!Application.isBatchMode) MeixiLocalProgress.MarkChapterTwoCompleted();
            yield return LoadSceneBehindFade("MeixiEditable");
        }

        private void CatchPlayer()
        {
            if (phase == Phase.Ending) return;
            phase = Phase.Ending;
            inputModal = true;
            // The chase is only reachable after the gate sequence. Persist the
            // checkpoint again at the failure boundary so a respawn cannot fall
            // back to the chapter-two presentation or quiz, even if the scene
            // reload races the earlier gate-save operation.
            MeixiLocalProgress.SetCheckpoint("ChapterTwoGateOpened");
            if (!Application.isBatchMode) MeixiLocalProgress.RecordDeath();
            if (defeatReturnText != null)
            {
                defeatReturnText.text = MeixiLocalProgress.IsCheckpoint("ChapterTwoGateOpened")
                    ? "ENTER / ESC  返回大门存档"
                    : "ENTER / ESC  返回第一章";
            }
            defeatPanel.SetActive(true);
            defeatPanel.transform.SetAsLastSibling();
            MeixiCursorState.SetPointer();
            Debug.Log("[ChapterTwoAdventure] Player caught; practice terminated.");
        }

        private void ReturnToChapterOne()
        {
            Time.timeScale = 1f;
            if (fadeGroup != null && fadeGroup.blocksRaycasts) return;
            StartCoroutine(ReturnToChapterOneRoutine());
        }

        private IEnumerator ReturnToChapterOneRoutine()
        {
            inputModal = true;
            yield return FadeScreen(1f, .42f);
            if (MeixiLocalProgress.IsCheckpoint("ChapterTwoGateOpened"))
            {
                yield return LoadSceneBehindFade("MeixiTheatre");
                yield break;
            }
            MeixiLocalProgress.SetGameplayChapter(1);
            yield return LoadSceneBehindFade("MeixiEditable");
        }

        private IEnumerator FadeScreen(float target, float duration)
        {
            if (fadeGroup == null) yield break;
            fadeGroup.gameObject.transform.SetAsLastSibling();
            fadeGroup.blocksRaycasts = true;
            float start = fadeGroup.alpha;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / Mathf.Max(.01f, duration)));
                fadeGroup.alpha = Mathf.Lerp(start, target, t);
                yield return null;
            }
            fadeGroup.alpha = target;
            fadeGroup.blocksRaycasts = target > .01f;
        }

        private IEnumerator LoadSceneBehindFade(string sceneName)
        {
            if (fadeGroup != null)
            {
                fadeGroup.alpha = 1f;
                fadeGroup.blocksRaycasts = true;
            }
            AsyncOperation load = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
            if (load == null) yield break;
            while (!load.isDone) yield return null;
        }

        private Vector3 ResolveFirstPersonCameraLocal()
        {
            if (player == null || viewCamera == null) return firstPersonCameraLocal;
            Transform cameraAnchor = firstPersonHead != null ? firstPersonHead : player;
            Vector3 desiredWorld = cameraAnchor.TransformPoint(firstPersonCameraLocal);
            Vector3 origin = firstPersonHead != null
                ? firstPersonHead.position
                : player.position + Vector3.up * 1.10f;
            Vector3 path = desiredWorld - origin;
            float distance = path.magnitude;
            if (distance > .01f)
            {
                int count = Physics.SphereCastNonAlloc(origin, .09f, path / distance, chaseCameraHits,
                    distance, ~0, QueryTriggerInteraction.Ignore);
                float nearest = distance;
                for (int i = 0; i < count; i++)
                {
                    Collider hit = chaseCameraHits[i].collider;
                    if (hit == null || hit.transform == player || hit.transform.IsChildOf(player)) continue;
                    nearest = Mathf.Min(nearest, chaseCameraHits[i].distance);
                }
                if (nearest < distance) desiredWorld = origin + path.normalized * Mathf.Max(.04f, nearest - .10f);
            }

            Vector3 forward = viewCamera.transform.forward;
            int forwardCount = Physics.RaycastNonAlloc(desiredWorld, forward, chaseCameraHits, .22f,
                ~0, QueryTriggerInteraction.Ignore);
            float forwardClearance = .22f;
            for (int i = 0; i < forwardCount; i++)
            {
                Collider hit = chaseCameraHits[i].collider;
                if (hit == null || hit.transform == player || hit.transform.IsChildOf(player)) continue;
                forwardClearance = Mathf.Min(forwardClearance, chaseCameraHits[i].distance);
            }
            if (forwardClearance < .22f) desiredWorld -= forward * (.22f - forwardClearance);
            return cameraAnchor.InverseTransformPoint(desiredWorld);
        }

        private void BuildHeritageMural(Transform parent)
        {
            heritageMuralTexture = LoadHeritageEmblemTexture();
            Material emblem = MakeMaterial("ChapterTwoHeritageEmblem", Color.white, .22f, .02f);
            if (heritageMuralTexture != null)
            {
                if (emblem.HasProperty("_BaseMap")) emblem.SetTexture("_BaseMap", heritageMuralTexture);
                if (emblem.HasProperty("_MainTex")) emblem.SetTexture("_MainTex", heritageMuralTexture);
            }

            GameObject mural = GameObject.CreatePrimitive(PrimitiveType.Quad);
            mural.name = "ChapterTwo_HeritageMural_可互动";
            mural.transform.SetParent(parent, false);
            // Replace the left wall panel near the backstage entrance.  The
            // existing frame remains, so the emblem reads as an old poster
            // mounted into the theatre rather than a floating UI image.
            mural.transform.localPosition = new Vector3(-9.61f, 4.45f, -3.90f);
            mural.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
            mural.transform.localScale = new Vector3(2.16f, 2.16f, 1f);
            mural.GetComponent<Renderer>().sharedMaterial = emblem;
            Collider collider = mural.GetComponent<Collider>();
            if (collider != null) Destroy(collider);
            heritageMuralObject = mural;
            heritageMuralObject.SetActive(false);

            float bestDistance = float.PositiveInfinity;
            foreach (Renderer renderer in FindObjectsByType<Renderer>())
            {
                if (renderer == null || renderer.gameObject.name != "ChapterTwoCampaignMural") continue;
                Vector3 position = renderer.transform.position;
                float distance = Mathf.Abs(position.x + 9.65f) + Mathf.Abs(position.z + 3.90f);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    heritageReplacedMuralRenderer = renderer;
                }
            }
            Debug.Log("[ChapterTwoEasterEgg] heritage mural prepared texture=" +
                      (heritageMuralTexture != null) + " replaced=" +
                      (heritageReplacedMuralRenderer != null));
        }

        private void RevealHeritageMural()
        {
            if (!flashlightCollected || heritageMuralVisible || heritageMuralObject == null) return;
            heritageMuralVisible = true;
            if (heritageReplacedMuralRenderer != null) heritageReplacedMuralRenderer.enabled = false;
            heritageMuralObject.SetActive(true);
            EnsureInteractableMarker(heritageMuralObject, "\u8c03\u67e5\u4f20\u627f\u5fbd\u8bb0");
            BuildHeritageMuralHighlightFrame();
            // The image quad has no physical surface of its own. Keep a stable
            // trigger volume on the mural so the centre-ray highlight and E
            // interaction remain reliable from either side of the wall frame.
            Transform volumeTransform = heritageMuralObject.transform.Find("InteractionVolume");
            if (volumeTransform != null)
            {
                BoxCollider volume = volumeTransform.GetComponent<BoxCollider>();
                if (volume != null)
                {
                    volume.center = Vector3.zero;
                    volume.size = new Vector3(1.18f, 1.18f, .44f);
                    volume.isTrigger = true;
                }
            }
        }

        private void BuildHeritageMuralHighlightFrame()
        {
            if (heritageMuralHighlightFrame != null || heritageMuralObject == null) return;
            heritageMuralHighlightFrame = new GameObject("ChapterTwo_HeritageMural_HighlightFrame");
            heritageMuralHighlightFrame.transform.SetParent(heritageMuralObject.transform, false);
            Material frameMaterial = MakeEmissionMaterial("ChapterTwo_HeritageMural_Highlight", Color.white, .72f);
            const float z = .018f;
            Box("HeritageMuralFrameTop", heritageMuralHighlightFrame.transform,
                new Vector3(0f, .535f, z), new Vector3(1.10f, .026f, .026f), frameMaterial, false);
            Box("HeritageMuralFrameBottom", heritageMuralHighlightFrame.transform,
                new Vector3(0f, -.535f, z), new Vector3(1.10f, .026f, .026f), frameMaterial, false);
            Box("HeritageMuralFrameLeft", heritageMuralHighlightFrame.transform,
                new Vector3(-.535f, 0f, z), new Vector3(.026f, 1.10f, .026f), frameMaterial, false);
            Box("HeritageMuralFrameRight", heritageMuralHighlightFrame.transform,
                new Vector3(.535f, 0f, z), new Vector3(.026f, 1.10f, .026f), frameMaterial, false);
            heritageMuralHighlightFrame.SetActive(false);
        }

        private void SetHeritageMuralFrame(bool visible)
        {
            if (heritageMuralHighlightFrame != null)
                heritageMuralHighlightFrame.SetActive(visible && heritageMuralVisible);
        }

        private static Texture2D LoadHeritageEmblemTexture()
        {
            Texture2D resource = Resources.Load<Texture2D>("Art/HeritageMural");
            if (resource != null) return resource;
            string buildRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            string[] candidates =
            {
                Path.Combine(buildRoot, "MeixiHeritageEmblem.png"),
                Path.Combine(buildRoot, "..", "MeixiHeritageEmblem.png")
            };
            foreach (string path in candidates)
            {
                if (!File.Exists(path)) continue;
                try
                {
                    Texture2D loaded = new(2, 2, TextureFormat.RGBA32, true)
                    {
                        name = "ChapterTwoHeritageEmblem_Runtime",
                        wrapMode = TextureWrapMode.Clamp,
                        filterMode = FilterMode.Trilinear,
                        anisoLevel = 4
                    };
                    if (loaded.LoadImage(File.ReadAllBytes(path), false)) return loaded;
                    Destroy(loaded);
                }
                catch (IOException)
                {
                    // A future packaged build may not ship the optional poster.
                }
            }
            return null;
        }

        private void SpawnCeilingPuppets()
        {
            if (ceilingPuppets.Count > 0) return;
            Transform parent = GameObject.Find("Theatre_Set_RuntimeGenerated")?.transform ?? transform;
            ceilingPuppets.Add(CeilingPuppetEasterEgg.Create("后台左侧缝隙坐像", parent,
                new Vector3(-9.40f, 3.82f, 13.42f)));
            ceilingPuppets.Add(CeilingPuppetEasterEgg.Create("后台右侧缝隙坐像", parent,
                new Vector3(9.40f, 3.82f, 13.42f)));
            Debug.Log("[ChapterTwoEasterEgg] ceiling puppets spawned count=" + ceilingPuppets.Count);
        }

        private void UpdateCeilingPuppets()
        {
            if (player == null || suppressPuppetCatchForValidation) return;
            for (int i = ceilingPuppets.Count - 1; i >= 0; i--)
            {
                CeilingPuppetEasterEgg puppet = ceilingPuppets[i];
                if (puppet == null || puppet.root == null)
                {
                    ceilingPuppets.RemoveAt(i);
                    continue;
                }
                puppet.UpdateLookAt(player, Time.unscaledDeltaTime);
            }
        }

        private void SpawnPuppets()
        {
            puppets.Clear();
            puppets.Add(PuppetAgent.Create("执灯木偶", new Vector3(-2.2f, 1.12f, 8.25f),
                new Color(.26f, .055f, .035f)));
            puppets.Add(PuppetAgent.Create("执扇木偶", new Vector3(2.2f, 1.12f, 8.55f),
                new Color(.045f, .16f, .16f)));
        }

        private void LoadChapterTwoAudio()
        {
            heartbeatClip = Resources.Load<AudioClip>("Audio/\u5fc3\u8df3");
            earthquakeClip = Resources.Load<AudioClip>("Audio/\u5730\u9707\u58f0");
            blackoutClip = Resources.Load<AudioClip>("Audio/\u65ad\u7535");
            doorClip = Resources.Load<AudioClip>("Audio/\u5f00\u95e8\u58f0");
            flashlightSwitchClip = Resources.Load<AudioClip>("Audio/\u624b\u7535\u7b52\u5f00\u5173");
            puppetBgmClip = Resources.Load<AudioClip>("Audio/\u6050\u6016bgm");
            AudioClip authoredFootstep = Resources.Load<AudioClip>("Audio/\u811a\u6b65\u58f0");
            footstepClips = authoredFootstep != null
                ? new[] { authoredFootstep }
                : new[] { CreateFootstepClip(0), CreateFootstepClip(1), CreateFootstepClip(2) };
            if (heartbeatSource != null) heartbeatSource.clip = heartbeatClip;
            if (puppetBgmSource != null) puppetBgmSource.clip = puppetBgmClip;
            Debug.Log("[MeixiAudio] Chapter-two authored audio loaded: heartbeat=" + (heartbeatClip != null) +
                      " earthquake=" + (earthquakeClip != null) + " blackout=" + (blackoutClip != null) +
                      " puppetBgm=" + (puppetBgmClip != null));
        }

        private void PlayChapterTwoEffect(AudioClip clip, float volume)
        {
            if (effectsSource == null || clip == null) return;
            effectsSource.PlayOneShot(clip, Mathf.Clamp01(volume));
        }

        private void StartPuppetBgm()
        {
            if (puppetBgmSource == null || puppetBgmClip == null) return;
            StopConflictingChapterTwoBgm();
            if (puppetBgmSource.isPlaying && puppetBgmSource.clip == puppetBgmClip) return;
            puppetBgmSource.Stop();
            puppetBgmSource.clip = puppetBgmClip;
            puppetBgmSource.volume = .30f;
            puppetBgmSource.pitch = 1f;
            puppetBgmSource.time = 0f;
            puppetBgmSource.Play();
            Debug.Log("[MeixiAudio] Puppet chase BGM started: " + puppetBgmClip.name);
        }

        private void StopConflictingChapterTwoBgm()
        {
            int stopped = 0;
            foreach (AudioSource source in FindObjectsByType<AudioSource>())
            {
                if (source == null || source == puppetBgmSource || source.clip == null) continue;
                string clipName = source.clip.name;
                bool isChapterTwoBgm = string.Equals(clipName, "ChapterTwoOpening", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(clipName, "木偶bgm", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(clipName, "恐怖bgm", StringComparison.OrdinalIgnoreCase);
                if (!isChapterTwoBgm) continue;
                if (source.isPlaying)
                    StartCoroutine(FadeOutBgmSource(source));
                else
                {
                    source.Stop();
                    source.volume = 0f;
                    source.clip = null;
                }
                stopped++;
            }
            if (stopped > 0)
                Debug.Log("[MeixiAudio] Stopped conflicting chapter-two BGM sources=" + stopped);
        }

        private IEnumerator FadeOutBgmSource(AudioSource source)
        {
            if (source == null || fadingBgmSources.Contains(source)) yield break;
            fadingBgmSources.Add(source);
            float startingVolume = source.volume;
            float elapsed = 0f;
            const float duration = .70f;
            while (source != null && source.isPlaying && elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                source.volume = Mathf.Lerp(startingVolume, 0f, Mathf.Clamp01(elapsed / duration));
                yield return null;
            }
            if (source != null)
            {
                source.Stop();
                source.volume = 0f;
                source.clip = null;
            }
            fadingBgmSources.Remove(source);
        }

        private void UpdateHeartbeatThreat()
        {
            if (heartbeatSource == null || heartbeatClip == null || player == null || phase != Phase.Chase)
            {
                if (heartbeatSource != null)
                {
                    heartbeatSource.volume = Mathf.MoveTowards(heartbeatSource.volume, 0f, Time.unscaledDeltaTime * 4.2f);
                    if (heartbeatSource.volume <= .001f) heartbeatSource.Stop();
                }
                if (heartbeatVignette != null) heartbeatVignette.color = new Color(1f, .015f, .015f, 0f);
                return;
            }

            float nearest = float.PositiveInfinity;
            foreach (PuppetAgent puppet in puppets)
            {
                if (puppet == null || puppet.root == null) continue;
                Vector3 delta = puppet.root.position - player.position;
                delta.y *= .42f;
                nearest = Mathf.Min(nearest, delta.magnitude);
            }
            const float triggerDistance = 13.5f;
            const float peakDistance = 1.65f;
            float threat = nearest < triggerDistance
                ? Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(triggerDistance, peakDistance, nearest))
                : 0f;
            float targetVolume = threat > .005f ? Mathf.Lerp(.055f, .96f, threat) : 0f;
            if (targetVolume > .005f && !heartbeatSource.isPlaying) heartbeatSource.Play();
            heartbeatSource.volume = Mathf.MoveTowards(heartbeatSource.volume, targetVolume,
                Time.unscaledDeltaTime * (targetVolume > heartbeatSource.volume ? 2.8f : 4.6f));
            heartbeatSource.pitch = Mathf.Lerp(.88f, 1.16f, threat);
            if (heartbeatSource.volume <= .001f) heartbeatSource.Stop();
            if (heartbeatVignette != null)
                heartbeatVignette.color = new Color(1f, .012f, .012f, threat * .82f);
        }

        private void BuildHeartbeatVignette()
        {
            if (canvas == null || heartbeatVignette != null) return;
            GameObject overlay = new("第二章心跳红色边缘");
            overlay.transform.SetParent(canvas.transform, false);
            heartbeatVignette = overlay.AddComponent<RawImage>();
            heartbeatVignette.texture = CreateHeartbeatVignetteTexture();
            heartbeatVignette.color = new Color(1f, .012f, .012f, 0f);
            heartbeatVignette.raycastTarget = false;
            SetRect(heartbeatVignette.rectTransform, Vector2.zero, Vector2.one);
            overlay.transform.SetAsFirstSibling();
        }

        private Texture2D CreateHeartbeatVignetteTexture()
        {
            if (heartbeatVignetteTexture != null) return heartbeatVignetteTexture;
            const int width = 256;
            const int height = 144;
            heartbeatVignetteTexture = new Texture2D(width, height, TextureFormat.RGBA32, true)
            {
                name = "ChapterTwoHeartbeatVignette",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };
            Color[] pixels = new Color[width * height];
            for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                float nx = Mathf.Abs(x / (width - 1f) * 2f - 1f);
                float ny = Mathf.Abs(y / (height - 1f) * 2f - 1f);
                float edge = Mathf.Max(nx, ny);
                float alpha = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(.53f, .98f, edge));
                pixels[y * width + x] = new Color(1f, .012f, .012f, alpha);
            }
            heartbeatVignetteTexture.SetPixels(pixels);
            heartbeatVignetteTexture.Apply(true, true);
            return heartbeatVignetteTexture;
        }

        private AudioClip CreateFootstepClip(int variant)
        {
            const int rate = 22050;
            const float duration = .19f;
            int count = Mathf.CeilToInt(rate * duration);
            float[] samples = new float[count];
            for (int i = 0; i < count; i++)
            {
                float t = i / (float)rate;
                float envelope = Mathf.Exp(-22f * t) * Mathf.Clamp01(t * 100f);
                float noise = Mathf.Sin(i * (12.3f + variant * 1.71f)) * Mathf.Sin(i * (.027f + variant * .009f));
                float thump = Mathf.Sin(t * (72f + variant * 13f) * Mathf.PI * 2f) * .42f;
                float wood = Mathf.Sin(t * (176f + variant * 19f) * Mathf.PI * 2f) * .18f;
                samples[i] = (thump + wood + noise * .18f) * envelope;
            }
            AudioClip clip = AudioClip.Create("黑暗木地板脚步_" + variant, count, 1, rate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private void EnsureAdventureSet()
        {
            if (GameObject.Find("ChapterTwoAdventure_Set") != null) return;
            Transform root = new GameObject("ChapterTwoAdventure_Set").transform;
            root.SetParent(transform, false);
            Material timber = MakeMaterial("AdventureDarkTimber", new Color(.16f, .042f, .025f), .28f, .06f);
            Material brass = MakeMaterial("AdventureAgedBrass", new Color(.39f, .23f, .075f), .54f, .48f);
            Material paper = MakeMaterial("AdventureOldPaper", new Color(.68f, .54f, .31f), .12f, 0f);
            Material stone = MakeMaterial("AdventureBasementStone", new Color(.055f, .065f, .074f), .18f, .04f);
            Material stairStone = MakeMaterial("AdventureStairStone", new Color(.13f, .18f, .20f), .32f, .08f);
            Material mirror = MakeMaterial("AdventureSmokedMirror", new Color(.30f, .42f, .44f, 1f), .97f, .76f);
            ConfigureSmokedMirror(mirror);
            if (mirror.HasProperty("_Surface")) mirror.SetFloat("_Surface", 0f);
            if (mirror.HasProperty("_ZWrite")) mirror.SetFloat("_ZWrite", 1f);
            mirror.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mirror.renderQueue = 2450;
            Material glow = MakeEmissionMaterial("AdventureColdGuide", new Color(.12f, .62f, .70f), 1.6f);
            ApplySurfaceTexture(timber, SurfaceKind.Wood, new Color(.16f, .042f, .025f));
            ApplySurfaceTexture(stone, SurfaceKind.Stone, new Color(.055f, .065f, .074f));
            ApplySurfaceTexture(stairStone, SurfaceKind.Stone, new Color(.13f, .18f, .20f));
            ApplySurfaceTexture(mirror, SurfaceKind.Mirror, new Color(.30f, .42f, .44f));
            SetSubtleEmission(mirror, new Color(.026f, .050f, .054f));
            ApplySurfaceTexture(paper, SurfaceKind.Paper, new Color(.68f, .54f, .31f));
            Texture2D diaryPaper = Resources.Load<Texture2D>("Art/MeixiDiaryPaperLandscapeV1");
            if (diaryPaper != null)
            {
                if (paper.HasProperty("_BaseMap")) paper.SetTexture("_BaseMap", diaryPaper);
                if (paper.HasProperty("_MainTex")) paper.SetTexture("_MainTex", diaryPaper);
            }
            BuildUpperFloorShell(root, stone, timber);
            OpenLegacyFrontWallForAdventureGate();

            doorAssemblyRoot = new GameObject("二楼大门完整总成").transform;
            doorAssemblyRoot.SetParent(root, false);
            doorObject = Box("二楼沉重升降门", doorAssemblyRoot, new Vector3(0f, 2.35f, -13.02f),
                new Vector3(3.55f, 4.7f, .28f), timber, true);
            Material doorInset = MakeMaterial("DoorInsetShadow", new Color(.055f, .014f, .010f), .24f, .02f);
            ApplySurfaceTexture(doorInset, SurfaceKind.Wood, new Color(.055f, .014f, .010f));
            Box("门面内嵌木作", doorAssemblyRoot, new Vector3(0f, 2.35f, -12.81f),
                new Vector3(2.86f, 3.64f, .045f), doorInset, false);
            for (int i = -1; i <= 1; i++)
                Box("门面纵向木作线_" + i, doorAssemblyRoot, new Vector3(i * .91f, 2.35f, -12.75f),
                    new Vector3(.055f, 3.18f, .035f), brass, false);
            Box("门面横向木作线", doorAssemblyRoot, new Vector3(0f, 2.35f, -12.75f),
                new Vector3(2.38f, .055f, .035f), brass, false);
            Sphere("门面铜钉", doorAssemblyRoot, new Vector3(0f, 4.05f, -12.72f),
                new Vector3(.18f, .18f, .08f), brass, false);
            Box("二楼门框左", root, new Vector3(-2.02f, 2.65f, -12.94f), new Vector3(.34f, 5.3f, .48f), brass, true);
            Box("二楼门框右", root, new Vector3(2.02f, 2.65f, -12.94f), new Vector3(.34f, 5.3f, .48f), brass, true);
            Box("二楼门框上", root, new Vector3(0f, 5.14f, -12.94f), new Vector3(4.38f, .34f, .48f), brass, false);
            doorLockVisual = Box("六位声纹密码锁", doorAssemblyRoot, new Vector3(1.18f, 2.18f, -12.62f),
                new Vector3(.68f, 1.02f, .18f), brass, false);
            AddFloatingLabel(doorLockVisual.transform, "坐  墙  声", new Vector3(0f, .56f, -.08f),
                new Vector3(0f, 180f, 0f), 34, new Color(.92f, .72f, .34f));
            Material lockGlass = MakeEmissionMaterial("AdventureLockGlass", new Color(.07f, .28f, .25f), .65f);
            for (int i = 0; i < 6; i++)
                Box("门锁数字窗_" + i, doorLockVisual.transform, new Vector3(-.34f + i * .136f, .08f, -.56f),
                    new Vector3(.095f, .13f, .05f), lockGlass, false);
            for (int i = -1; i <= 1; i++)
                Box("门板雕槽_" + i, doorObject.transform, new Vector3(i * .22f, 0f, -.54f),
                    new Vector3(.12f, .78f, .05f), brass, false);

            sheetObject = Box("残谱页_可收集", root, new Vector3(.58f, 1.79f, 16.10f),
                new Vector3(.72f, .035f, .52f), paper, false);
            sheetObject.transform.rotation = Quaternion.Euler(0f, -9f, 0f);
            Cylinder("残谱朱印", sheetObject.transform, new Vector3(.27f, .58f, -.12f),
                new Vector3(.09f, .018f, .09f), MakeMaterial("AdventureSeal", new Color(.43f, .055f, .035f), .22f, 0f), false);
            Box("残谱压角铜镇", root, new Vector3(.91f, 1.83f, 15.98f), new Vector3(.16f, .08f, .16f), brass, false);

            chairObject = Box("观众椅暗格_可调查", root, new Vector3(-4.36f, .82f, -7.63f),
                new Vector3(.72f, .18f, .52f), timber, false);
            Box("ChairBackFrame", root, new Vector3(-4.36f, 1.42f, -7.43f),
                new Vector3(.72f, 1.12f, .12f), timber, false);
            Box("ChairBackVelvet", root, new Vector3(-4.36f, 1.31f, -7.34f),
                new Vector3(.54f, .62f, .035f), MakeMaterial("ChairVelvet", new Color(.24f, .045f, .035f), .32f, .02f), false);
            for (int leg = 0; leg < 4; leg++)
            {
                float x = leg % 2 == 0 ? -.25f : .25f;
                float z = leg < 2 ? -.18f : .18f;
                Box("ChairLeg_" + leg, root, new Vector3(-4.36f + x, .30f, -7.63f + z),
                    new Vector3(.10f, 1.02f, .10f), timber, false);
            }
            Box("暗格铜扣", chairObject.transform, new Vector3(0f, .12f, -.29f),
                new Vector3(.18f, .12f, .04f), brass, false);
            Box("暗格纸边", chairObject.transform, new Vector3(.12f, .22f, -.24f),
                new Vector3(.33f, .018f, .18f), paper, false).transform.localRotation = Quaternion.Euler(4f, 14f, -5f);

            wallClueObject = Box("墙面手稿_可调查", root, new Vector3(15.76f, 2.28f, 1.15f),
                new Vector3(.025f, 1.45f, 2.02f), paper, false);
            Box("WallClueTopFrame", root, new Vector3(15.72f, 3.83f, 1.15f), new Vector3(.08f, .10f, 2.24f), brass, false);
            Box("WallClueBottomFrame", root, new Vector3(15.72f, .73f, 1.15f), new Vector3(.08f, .10f, 2.24f), brass, false);
            Box("WallClueLeftFrame", root, new Vector3(15.72f, 2.28f, -.02f), new Vector3(.08f, 3.02f, .10f), brass, false);
            Box("WallClueRightFrame", root, new Vector3(15.72f, 2.28f, 2.32f), new Vector3(.08f, 3.02f, .10f), brass, false);
            wallClueText = AddFloatingLabel(wallClueObject.transform, "二 · 四\n守谱人的记忆",
                new Vector3(-.075f, 0f, 0f), new Vector3(0f, 90f, 0f), 44,
                new Color(.15f, .08f, .045f));
            if (wallClueText != null)
            {
                wallClueText.fontStyle = FontStyle.Italic;
                wallClueText.color = new Color(.15f, .08f, .045f);
                wallClueText.gameObject.SetActive(false);
            }
            BuildHeritageMural(root);
            Material muralInk = MakeMaterial("WallMuralInk", new Color(.13f, .075f, .045f), .12f, 0f);
            for (int stroke = 0; stroke < 5; stroke++)
            {
                float offset = (stroke - 2) * .24f;
                GameObject muralLine = Box("WallMuralTideStroke_" + stroke, root,
                    new Vector3(15.68f, 1.45f + stroke * .27f, .58f + offset),
                    new Vector3(.028f, .045f, .92f - stroke * .10f), muralInk, false);
                muralLine.transform.localRotation = Quaternion.Euler(0f, 0f, -18f + stroke * 7f);
            }

            GameObject cabinet = Box("后台手电柜", root, new Vector3(-15.10f, 1.25f, -.55f),
                new Vector3(1.15f, 2.5f, .78f), timber, true);
            Box("手电柜内衬", cabinet.transform, new Vector3(.22f, 0f, -.52f),
                new Vector3(.42f, .78f, .04f), stone, false);
            for (int i = -1; i <= 1; i++)
                Box("手电柜铜条_" + i, cabinet.transform, new Vector3(i * .31f, 0f, -.56f),
                    new Vector3(.025f, .84f, .03f), brass, false);
            flashlightObject = Cylinder("手电筒_可收集", root, new Vector3(-14.48f, 1.16f, -.82f),
                new Vector3(.12f, .42f, .12f), brass, false);
            flashlightObject.transform.rotation = Quaternion.Euler(90f, 0f, 22f);

            lubanLockObject = CreateWorldItemVisual(new InventoryItem
            {
                id = "luban",
                title = "卯榫匣",
                kind = "机关物件",
                description = "四根互锁木梁组成的鲁班锁。榫头没有明显的起点，拆解顺序只能从结构本身找。",
                icon = CreateInventoryIcon("luban")
            }, new Vector3(0f, 2.10f, 16.20f), -18f);
            lubanLockObject.name = "舞台卯榫匣_待解";
            Box("卯榫匣展示台", root, new Vector3(0f, 1.78f, 16.20f), new Vector3(1.22f, .12f, .92f), brass, false);
            lubanLockObject.SetActive(false);

            BuildSwitchbackStaircase(root, stairStone, timber, brass);
            BuildStairwellInteractiveProps(root, timber, brass, stairStone);
            maze = BuildMirrorMaze(root, stone, mirror, timber, brass, glow);
        }

        private static void OpenLegacyFrontWallForAdventureGate()
        {
            // The theatre shell has two overlapping front-wall systems: the
            // exploration-room wall and the segmented audience-hall wall.  The
            // adventure gate owns this facade, so leaving either legacy system
            // enabled creates the white slab / black side seams seen after the
            // lift door opens.
            string[] legacyParts = { "WingFrontWall", "FrontWallLeft", "FrontWallRight", "FrontWallLintel" };
            foreach (string partName in legacyParts)
            {
                GameObject legacyPart = GameObject.Find(partName);
                if (legacyPart == null) continue;
                foreach (Collider collider in legacyPart.GetComponentsInChildren<Collider>(true))
                    collider.enabled = false;
                foreach (Renderer renderer in legacyPart.GetComponentsInChildren<Renderer>(true))
                    renderer.enabled = false;
            }
            // Runtime-generated theatre rooms can contain inactive duplicate
            // facade objects.  GameObject.Find only returns one active match,
            // so sweep scene transforms as well to guarantee no pale legacy
            // slab survives behind the new gate facade.
            foreach (Transform candidate in Resources.FindObjectsOfTypeAll<Transform>())
            {
                if (candidate == null || !candidate.gameObject.scene.IsValid()) continue;
                if (Array.IndexOf(legacyParts, candidate.name) < 0) continue;
                foreach (Collider collider in candidate.GetComponentsInChildren<Collider>(true))
                    collider.enabled = false;
                foreach (Renderer renderer in candidate.GetComponentsInChildren<Renderer>(true))
                    renderer.enabled = false;
            }
        }

        private static void BuildUpperFloorShell(Transform root, Material stone, Material timber)
        {
            // The stage remains open only at the gate. These broad, low-detail
            // bounds close the old side/floor seams without adding render-heavy
            // geometry to the playable area.
            // Keep the upper floor sealed, but leave a real rectangular stairwell
            // opening. The former single slab ran through the first flight and
            // made the steps visible only through the mesh while still blocking
            // the CharacterController.
            const float floorWest = -17.20f;
            const float floorEast = 17.20f;
            const float floorSouth = -16.40f;
            const float floorNorth = 22.00f;
            const float stairOpeningWest = -3.78f;
            const float stairOpeningEast = 3.78f;
            const float stairOpeningNorth = -13.72f;
            float frontDepth = floorNorth - stairOpeningNorth;
            Box("ChapterTwo_UpperFloorSealFront", root,
                new Vector3(0f, -.31f, (floorNorth + stairOpeningNorth) * .5f),
                new Vector3(floorEast - floorWest, .48f, frontDepth), stone, true);
            float sideDepth = stairOpeningNorth - floorSouth;
            float sideWidth = stairOpeningWest - floorWest;
            Box("ChapterTwo_UpperFloorSealWestReturn", root,
                new Vector3((floorWest + stairOpeningWest) * .5f, -.31f,
                    (floorSouth + stairOpeningNorth) * .5f),
                new Vector3(sideWidth, .48f, sideDepth), stone, true);
            Box("ChapterTwo_UpperFloorSealEastReturn", root,
                new Vector3((stairOpeningEast + floorEast) * .5f, -.31f,
                    (floorSouth + stairOpeningNorth) * .5f),
                new Vector3(sideWidth, .48f, sideDepth), stone, true);
            Box("ChapterTwo_BackstageCeilingSeal", root, new Vector3(0f, 5.68f, 16.15f),
                new Vector3(33.10f, .34f, 9.10f), timber, true);
            // The roof is also a camera obstruction.  Without a collider the
            // orbit camera can rise through the visible ceiling and expose the
            // empty space above the theatre shell.
            Box("ChapterTwo_UpperCeilingSeal", root, new Vector3(0f, 8.76f, 2.8f),
                new Vector3(34.40f, .42f, 38.40f), timber, true);
            Box("ChapterTwo_UpperWestSeal", root, new Vector3(-16.18f, 4.12f, 2.8f),
                new Vector3(.48f, 9.28f, 38.40f), timber, true);
            Box("ChapterTwo_UpperEastSeal", root, new Vector3(16.18f, 4.12f, 2.8f),
                new Vector3(.48f, 9.28f, 38.40f), timber, true);
            Box("ChapterTwo_UpperBackSeal", root, new Vector3(0f, 4.12f, 20.26f),
                new Vector3(34.40f, 9.28f, .48f), timber, true);
            Box("ChapterTwo_UpperBackWestCorner", root, new Vector3(-16.18f, 4.12f, 20.26f),
                new Vector3(.62f, 9.28f, .62f), timber, true);
            Box("ChapterTwo_UpperBackEastCorner", root, new Vector3(16.18f, 4.12f, 20.26f),
                new Vector3(.62f, 9.28f, .62f), timber, true);
            // The legacy front wall is hidden to make room for the gate. Replace
            // it with two solid returns so the stage never opens into exterior
            // space on either side of the door frame.
            Box("ChapterTwo_UpperFrontWallLeft", root, new Vector3(-9.20f, 4.12f, -12.72f),
                new Vector3(13.60f, 9.28f, .48f), timber, true);
            Box("ChapterTwo_UpperFrontWallRight", root, new Vector3(9.20f, 4.12f, -12.72f),
                new Vector3(13.60f, 9.28f, .48f), timber, true);
            // Close the remaining vertical and overhead seams around the gate.
            // The inner edges stop exactly at the 3.55m door leaf, so this adds
            // a sealed facade without narrowing the playable doorway.
            Box("ChapterTwo_UpperFrontGateReturnLeft", root, new Vector3(-2.18f, 2.65f, -12.72f),
                new Vector3(.82f, 5.30f, .64f), timber, true);
            Box("ChapterTwo_UpperFrontGateReturnRight", root, new Vector3(2.18f, 2.65f, -12.72f),
                new Vector3(.82f, 5.30f, .64f), timber, true);
            Box("ChapterTwo_UpperFrontGateHeader", root, new Vector3(0f, 7.28f, -12.72f),
                new Vector3(4.92f, 3.02f, .64f), timber, true);
            Box("ChapterTwo_GatePassageCeilingSeal", root, new Vector3(0f, 6.10f, -13.56f),
                new Vector3(4.34f, .42f, 1.18f), timber, true);
            // Close the two narrow offsets between the facade returns and the
            // stairwell wall.  They are intentionally outside the 3.55m door
            // leaf, leaving a clean walk-through opening after the lift.
            Box("ChapterTwo_GatePassageWestRevealSeal", root, new Vector3(-1.965f, 2.56f, -13.28f),
                new Vector3(.39f, 5.42f, 1.78f), timber, true);
            Box("ChapterTwo_GatePassageEastRevealSeal", root, new Vector3(1.965f, 2.56f, -13.28f),
                new Vector3(.39f, 5.42f, 1.78f), timber, true);
            Box("ChapterTwo_GatePassageUpperSeal", root, new Vector3(0f, 6.48f, -13.28f),
                new Vector3(4.32f, 2.46f, 1.78f), timber, true);
            // The lift door exposes the stairwell from a low, third-person
            // camera angle.  Leave the walking portal below this panel, but
            // close the high reveal so the theatre shell/skybox cannot appear
            // as a bright rectangular wall beyond the gate.
            Box("ChapterTwo_GateFacingUpperWallSeal", root, new Vector3(0f, 4.72f, -13.82f),
                new Vector3(4.56f, 2.18f, .34f), timber, true);
        }

        private void BuildStaircase(Transform root, Material stone, Material timber, Material brass)
        {
            Transform stairRoot = new GameObject("二楼至一楼楼梯").transform;
            stairRoot.SetParent(root, false);
            Vector3 center = new(0f, -.64f, -15.62f);
            const int count = 34;
            const float radius = 2.38f;
            const float turns = 1.85f;
            for (int i = 0; i < count; i++)
            {
                float t = i / (float)(count - 1);
                float angle = (-90f + t * turns * 360f) * Mathf.Deg2Rad;
                float y = Mathf.Lerp(-.22f, -7.22f, t);
                Vector3 position = center + new Vector3(Mathf.Cos(angle) * radius, y - center.y, Mathf.Sin(angle) * radius);
                GameObject step = Box("回旋楼梯踏步_" + i, stairRoot, position,
                    new Vector3(1.32f, .22f, .72f), stone, true);
                step.transform.rotation = Quaternion.Euler(0f, -angle * Mathf.Rad2Deg + 90f, 0f);
                if ((i & 1) == 0)
                {
                    GameObject edge = Box("回旋楼梯铜边_" + i, stairRoot, position + Vector3.up * .125f,
                        new Vector3(1.12f, .025f, .035f), brass, false);
                    edge.transform.rotation = step.transform.rotation;
                }
                if (i % 4 == 0)
                {
                    Vector3 railPosition = center + new Vector3(Mathf.Cos(angle) * (radius + .62f), y + .64f - center.y,
                        Mathf.Sin(angle) * (radius + .62f));
                    Cylinder("回旋楼梯扶手柱_" + i, stairRoot, railPosition,
                        new Vector3(.055f, .72f, .055f), timber, false);
                }
            }
            AddSpiralRail(stairRoot, center, radius + .62f, -.22f, -7.22f, turns, timber, "外侧");
            AddSpiralRail(stairRoot, center, radius - .62f, -.22f, -7.22f, turns, brass, "内侧");
            Cylinder("回旋楼梯中心柱", stairRoot, center + Vector3.down * 3.45f,
                new Vector3(.26f, 3.55f, .26f), timber, true);
            Cylinder("回旋楼梯中心铜环", stairRoot, center + Vector3.down * 1.15f,
                new Vector3(.39f, .035f, .39f), brass, false);
            Box("二楼门前封口平台", stairRoot, new Vector3(0f, -.18f, -13.02f),
                new Vector3(4.8f, .24f, 2.2f), stone, true);
            Box("一楼迷宫入口平台", stairRoot, center + Vector3.down * 6.98f,
                new Vector3(4.4f, .26f, 4.0f), stone, true);
            Box("门后左封墙", root, new Vector3(-4.55f, 2.70f, -13.48f),
                new Vector3(1.10f, 5.80f, 1.10f), timber, true);
            Box("门后右封墙", root, new Vector3(4.55f, 2.70f, -13.48f),
                new Vector3(1.10f, 5.80f, 1.10f), timber, true);
            Box("门后封墙顶梁", root, new Vector3(0f, 5.58f, -13.48f),
                new Vector3(8.20f, .55f, 1.10f), timber, true);
            Box("门框内侧左压条", root, new Vector3(-2.12f, 2.66f, -13.45f),
                new Vector3(.16f, 5.16f, .32f), brass, false);
            Box("门框内侧右压条", root, new Vector3(2.12f, 2.66f, -13.45f),
                new Vector3(.16f, 5.16f, .32f), brass, false);
        }

        private static void AddSpiralRail(Transform parent, Vector3 center, float radius, float topY, float bottomY,
            float turns, Material material, string label)
        {
            GameObject railObject = new("回旋楼梯连续扶手_" + label);
            railObject.transform.SetParent(parent, false);
            LineRenderer rail = railObject.AddComponent<LineRenderer>();
            rail.useWorldSpace = false;
            rail.loop = false;
            rail.positionCount = 56;
            rail.widthMultiplier = label == "外侧" ? .075f : .045f;
            rail.numCapVertices = 3;
            rail.numCornerVertices = 3;
            rail.sharedMaterial = material;
            rail.startColor = Color.white;
            rail.endColor = Color.white;
            for (int i = 0; i < rail.positionCount; i++)
            {
                float t = i / (float)(rail.positionCount - 1);
                float angle = (-90f + t * turns * 360f) * Mathf.Deg2Rad;
                rail.SetPosition(i, center + new Vector3(Mathf.Cos(angle) * radius,
                    Mathf.Lerp(topY, bottomY, t) + .69f, Mathf.Sin(angle) * radius));
            }
        }

        private void BuildSwitchbackStaircase(Transform root, Material stone, Material timber, Material brass)
        {
            Transform stairRoot = new GameObject("ChapterTwo_SwitchbackStairwell").transform;
            stairRoot.SetParent(root, false);

            // Keep the two flights on one measured profile.  Previously the
            // lower flight started 0.26m below the middle landing and used a
            // different final elevation, which made the transition look
            // offset and could make the CharacterController catch on the seam.
            const int flightSteps = 17;
            const float upperX = -1.55f;
            const float upperStartZ = -14.18f;
            const float upperStartY = -.38f;
            const float upperTread = .48f;
            const float upperRise = .21f;
            const float middleLandingZ = -21.84f;
            const float middleLandingY = -3.79f;
            const float lowerX = 1.55f;
            const float lowerStartZ = -21.62f;
            const float lowerStartY = -3.80f;
            const float lowerTread = .38f;
            const float lowerRise = .23f;
            const float mazeLandingZ = -27.62f;
            const float mazeLandingY = -7.48f;

            // Keep the stair core visually separate from the theatre: warm oak
            // treads, a lighter concrete shaft and a restrained metal guard make
            // the route readable during the blackout.
            Material stairWood = MakeMaterial("ChapterTwo_StairOak", new Color(.43f, .20f, .085f), .32f, .035f);
            ApplySurfaceTexture(stairWood, SurfaceKind.Wood, new Color(.43f, .20f, .085f));
            SetSubtleEmission(stairWood, new Color(.120f, .035f, .008f));
            Material stairWall = MakeMaterial("ChapterTwo_StairwellConcrete", new Color(.18f, .21f, .22f), .24f, .015f);
            ApplySurfaceTexture(stairWall, SurfaceKind.Stone, new Color(.18f, .21f, .22f));
            SetSubtleEmission(stairWall, new Color(.022f, .032f, .034f));
            Material stairMetal = MakeMaterial("ChapterTwo_StairwellMetal", new Color(.22f, .28f, .29f), .62f, .72f);
            Material stairTrim = MakeMaterial("ChapterTwo_StairwellTrim", new Color(.20f, .24f, .24f), .42f, .32f);

            // Widen the masonry immediately behind the gate so the doorway does
            // not expose the old exterior gap during the opening turn.
            Box("ChapterTwo_GateSealLeft", stairRoot, new Vector3(-3.08f, 2.70f, -13.48f),
                new Vector3(1.92f, 5.80f, 1.10f), stairWall, true);
            Box("ChapterTwo_GateSealRight", stairRoot, new Vector3(3.08f, 2.70f, -13.48f),
                new Vector3(1.92f, 5.80f, 1.10f), stairWall, true);
            Box("ChapterTwo_GateSealHeader", stairRoot, new Vector3(0f, 5.62f, -13.48f),
                new Vector3(8.20f, .48f, 1.10f), stairWall, true);

            // Overlapping landings keep the CharacterController grounded while it
            // changes direction. The first landing overlaps the gate threshold;
            // the final one overlaps the maze's open north cell.
            BuildStairFlight(stairRoot, "ChapterTwo_UpperFlight", upperX, upperStartZ, -1f, upperStartY,
                flightSteps, stairWood, stairMetal, brass, upperTread, upperRise,
                openRightAtEnd: true);
            Box("ChapterTwo_UpperLanding", stairRoot, new Vector3(0f, -.18f, -13.12f),
                new Vector3(6.30f, .24f, 1.15f), stairWall, true);
            Box("ChapterTwo_UpperWoodThreshold", stairRoot, new Vector3(0f, -.035f, -13.94f),
                new Vector3(4.30f, .10f, .24f), stairWood, true);
            Box("ChapterTwo_UpperFlightEntryBridge", stairRoot, new Vector3(0f, -.30f, -13.82f),
                new Vector3(2.86f, .16f, .42f), stairWood, true);
            Box("ChapterTwo_MidLanding", stairRoot, new Vector3(0f, middleLandingY, middleLandingZ),
                new Vector3(6.30f, .28f, .90f), stairWall, true);
            BuildStairFlight(stairRoot, "ChapterTwo_LowerFlight", lowerX, lowerStartZ, -1f, lowerStartY,
                flightSteps, stairWood, stairMetal, brass, lowerTread, lowerRise,
                openLeftAtStart: true);
            Box("ChapterTwo_MazeLanding", stairRoot, new Vector3(0f, mazeLandingY, mazeLandingZ),
                new Vector3(6.30f, .26f, 2.06f), stairWall, true);
            // The lower flight now descends toward the separate lower level. The
            // threshold is aligned to the maze's north-cell opening, not placed
            // through a mirror wall or under the old landing.
            Box("ChapterTwo_MazeEntryCorridor", stairRoot, new Vector3(0f, -7.38f, -26.58f),
                new Vector3(2.22f, .10f, .66f), stairWood, true);

            // A complete concrete shaft prevents exterior voids showing during the
            // chase. The opening at the bottom is only the maze's north cell.
            Box("ChapterTwo_StairwellLeftWall", stairRoot, new Vector3(-3.48f, -.85f, -19.90f),
                new Vector3(.48f, 13.60f, 13.55f), stairWall, true);
            Box("ChapterTwo_StairwellRightWall", stairRoot, new Vector3(3.48f, -.85f, -19.90f),
                new Vector3(.48f, 13.60f, 13.55f), stairWall, true);
            // The old back wall sat inside maze row 0 after the lower flight was
            // corrected, so it cut the entrance in half. The maze shell supplies
            // the lower-level enclosure; the stairwell remains open at that end.
            Box("ChapterTwo_StairwellCap", stairRoot, new Vector3(0f, 5.93f, -19.90f),
                new Vector3(7.44f, .34f, 13.55f), stairWall, true);
            Box("ChapterTwo_StairwellLeftTrim", stairRoot, new Vector3(-3.19f, -.85f, -19.90f),
                new Vector3(.06f, 12.95f, 12.92f), stairTrim, false);
            Box("ChapterTwo_StairwellRightTrim", stairRoot, new Vector3(3.19f, -.85f, -19.90f),
                new Vector3(.06f, 12.95f, 12.92f), stairTrim, false);
            // A continuous catcher slab closes every gap below the flights. It is
            // hidden below the playable floor but prevents a missed step or a
            // short controller frame from dropping the player into the void.
            Box("ChapterTwo_StairwellSafetyFloor", stairRoot, new Vector3(0f, -8.18f, -19.90f),
                new Vector3(7.44f, .34f, 13.55f), stairWall, true);
            // Keep the catch slab below the playable surfaces. At the old height
            // it overlapped the maze floor and behaved like an invisible raised
            // threshold at the bottom of the flight.
            Box("ChapterTwo_StairwellCatchFloor", stairRoot, new Vector3(0f, -7.82f, -19.90f),
                new Vector3(8.10f, .22f, 14.20f), stairWall, true);
            // Seal the rear of the shaft while preserving only the 2.6m-wide
            // opening that leads into the maze's north cell.  The side returns
            // overlap the shaft walls so no diagonal camera angle can reveal a
            // gap behind the lower landing.
            const float bottomWallZ = -26.78f;
            const float bottomOpeningHalf = 1.30f;
            const float bottomWallBottomY = -7.82f;
            const float bottomWallTopY = -3.22f;
            float bottomWallHeight = bottomWallTopY - bottomWallBottomY;
            float bottomReturnWidth = 3.48f - bottomOpeningHalf;
            float bottomReturnCenterX = (3.48f + bottomOpeningHalf) * .5f;
            float bottomWallCenterY = (bottomWallBottomY + bottomWallTopY) * .5f;
            Box("ChapterTwo_StairwellBottomWestReturn", stairRoot,
                new Vector3(-bottomReturnCenterX, bottomWallCenterY, bottomWallZ),
                new Vector3(bottomReturnWidth, bottomWallHeight, .34f), stairWall, true);
            Box("ChapterTwo_StairwellBottomEastReturn", stairRoot,
                new Vector3(bottomReturnCenterX, bottomWallCenterY, bottomWallZ),
                new Vector3(bottomReturnWidth, bottomWallHeight, .34f), stairWall, true);
            Box("ChapterTwo_StairwellBottomHeader", stairRoot,
                new Vector3(0f, bottomWallTopY + .12f, bottomWallZ),
                new Vector3(bottomOpeningHalf * 2f + .34f, .34f, .34f), stairWall, true);
            // The maze north wall stops at x=+/-1.26m.  Extend the lower
            // returns past that edge and carry them through a short threshold
            // tunnel so no diagonal view can expose the void between the shaft
            // and the maze shell.
            const float entryHalf = 1.18f;
            float entryReturnWidth = 3.48f - entryHalf;
            float entryReturnCenter = (3.48f + entryHalf) * .5f;
            Box("ChapterTwo_MazeEntryWestRevealSeal", stairRoot,
                new Vector3(-entryReturnCenter, -5.63f, -26.60f),
                new Vector3(entryReturnWidth, 4.18f, 1.82f), stairWall, true);
            Box("ChapterTwo_MazeEntryEastRevealSeal", stairRoot,
                new Vector3(entryReturnCenter, -5.63f, -26.60f),
                new Vector3(entryReturnWidth, 4.18f, 1.82f), stairWall, true);
            Box("ChapterTwo_MazeEntryUpperSeal", stairRoot,
                new Vector3(0f, -3.34f, -26.60f), new Vector3(2.70f, .42f, 1.82f), stairWall, true);
            BuildStairEdgeCurb(stairRoot, "ChapterTwo_UpperFlight", upperX, upperStartZ, -1f,
                upperStartY, flightSteps, stairMetal, upperTread, upperRise,
                openRightAtEnd: true);
            BuildStairEdgeCurb(stairRoot, "ChapterTwo_LowerFlight", lowerX, lowerStartZ, -1f,
                lowerStartY, flightSteps, stairMetal, lowerTread, lowerRise,
                openLeftAtStart: true);
            CreateStairwellGuideLight(stairRoot, "ChapterTwo_StairLight_Upper", new Vector3(0f, -.02f, -13.05f),
                new Color(.62f, .84f, .90f), 1.65f, 5.2f);
            CreateStairwellGuideLight(stairRoot, "ChapterTwo_StairLight_UpperA", new Vector3(-3.18f, 1.56f, -15.85f),
                new Color(.45f, .75f, .86f), 1.35f, 5.0f);
            CreateStairwellGuideLight(stairRoot, "ChapterTwo_StairLight_UpperB", new Vector3(-3.18f, .12f, -18.10f),
                new Color(.45f, .75f, .86f), 1.35f, 5.0f);
            CreateStairwellGuideLight(stairRoot, "ChapterTwo_StairLight_Mid", new Vector3(3.18f, -2.04f, -20.72f),
                new Color(.48f, .78f, .76f), 1.35f, 5.0f);
            CreateStairwellGuideLight(stairRoot, "ChapterTwo_StairLight_LowerA", new Vector3(3.18f, -4.78f, -23.25f),
                new Color(.42f, .72f, .78f), 1.35f, 5.0f);
            CreateStairwellGuideLight(stairRoot, "ChapterTwo_StairLight_LowerB", new Vector3(3.18f, -6.35f, -26.05f),
                new Color(.40f, .70f, .74f), 1.35f, 5.0f);
            CreateStairwellGuideLight(stairRoot, "ChapterTwo_StairLight_Fill", new Vector3(0f, -1.55f, -22.65f),
                new Color(.34f, .58f, .62f), 1.15f, 7.0f);
            StaticBatchingUtility.Combine(stairRoot.gameObject);
        }

        private void BuildStairwellInteractiveProps(Transform root, Material timber, Material brass,
            Material stairStone)
        {
            Material landingWood = MakeMaterial("ChapterTwo_LandingOak", new Color(.29f, .105f, .038f), .34f, .06f);
            Material darkIron = MakeMaterial("ChapterTwo_StairIron", new Color(.055f, .075f, .078f), .72f, .78f);
            Material paper = MakeMaterial("ChapterTwo_StairNotice", new Color(.46f, .36f, .21f), .16f, 0f);
            ApplySurfaceTexture(landingWood, SurfaceKind.Wood, new Color(.29f, .105f, .038f));
            ApplySurfaceTexture(paper, SurfaceKind.Paper, new Color(.46f, .36f, .21f));

            // The turn is a real landing, with a timber inset and edge trim
            // covering the seam between the two flights.
            Box("ChapterTwo_MidLandingDeck", root, new Vector3(0f, -3.625f, -21.84f),
                new Vector3(5.92f, .055f, 2.42f), landingWood, false);
            for (int i = -2; i <= 2; i++)
                Box("ChapterTwo_MidLandingPlankLine_" + i, root,
                    new Vector3(i * 1.05f, -3.585f, -21.84f), new Vector3(.035f, .018f, 2.28f), brass, false);
            Box("ChapterTwo_MidLandingFrontTrim", root, new Vector3(0f, -3.47f, -23.19f),
                new Vector3(6.34f, .25f, .12f), stairStone, false);
            Box("ChapterTwo_MidLandingRearTrim", root, new Vector3(0f, -3.47f, -20.49f),
                new Vector3(6.34f, .25f, .12f), stairStone, false);

            // Continuous outer skins close the views through the handrails.
            // They are offset just outside the structural walls, so the route
            // remains the same width while the golden exterior cannot show.
            for (int side = -1; side <= 1; side += 2)
            {
                float x = side * 3.82f;
                Box("ChapterTwo_StairwellOuterSkin_" + side, root,
                    new Vector3(x, -.84f, -19.90f), new Vector3(.20f, 12.72f, 13.10f), stairStone, false);
                Box("ChapterTwo_StairwellOuterCap_" + side, root,
                    new Vector3(x, 5.34f, -19.90f), new Vector3(.32f, .18f, 13.30f), timber, false);
            }

            // A deeper canopy closes the empty volume above the maze entrance.
            // The north wall bands continue up to it so the camera sees a real
            // ceiling and a finished threshold instead of an open skybox.
            Box("ChapterTwo_MazeEntryCanopy", root, new Vector3(0f, -2.98f, -26.48f),
                new Vector3(6.70f, .26f, 1.82f), stairStone, true);
            Box("ChapterTwo_MazeEntryCanopyTrim", root, new Vector3(0f, -2.80f, -26.48f),
                new Vector3(6.94f, .10f, 1.98f), timber, false);
            // The canopy previously ended against open air. Looking straight
            // through the gate exposed the unbounded space above the maze north
            // wall. This upper facade overlaps the canopy, threshold lintel and
            // maze roof while keeping the full walk-through portal below it open.
            Box("ChapterTwo_MazeEntryFacingWallSeal", root, new Vector3(0f, 1.18f, -26.48f),
                new Vector3(6.74f, 9.30f, .40f), stairStone, true);
            Box("ChapterTwo_MazeEntryFacingWallTrim", root, new Vector3(0f, 1.08f, -26.25f),
                new Vector3(6.98f, 9.16f, .08f), timber, false);
            Box("ChapterTwo_MazeEntryLintelOverlapSeal", root, new Vector3(0f, -3.08f, -26.48f),
                new Vector3(2.94f, .42f, 2.02f), stairStone, true);

            // Small props make the transition a place to inspect rather than a
            // purely functional shaft.  Their interaction volumes are added by
            // the normal ChapterTwo marker pass in Awake.
            Transform crateRoot = new GameObject("ChapterTwo_StairLandingCrate_可互动").transform;
            crateRoot.SetParent(root, false);
            crateRoot.localPosition = new Vector3(2.05f, -3.48f, -21.88f);
            Box("CrateBody", crateRoot, new Vector3(0f, .30f, 0f), new Vector3(.94f, .56f, .78f), landingWood, false);
            for (int i = -1; i <= 1; i++)
                Box("CrateBand_" + i, crateRoot, new Vector3(i * .28f, .30f, -.405f),
                    new Vector3(.055f, .62f, .035f), brass, false);
            GameObject crateLid = Box("CrateLid", crateRoot, new Vector3(0f, .62f, 0f),
                new Vector3(1.00f, .10f, .84f), landingWood, false);
            crateRoot.gameObject.AddComponent<ChapterTwoSetProp>().ConfigureHinge(crateLid.transform,
                new Vector3(-72f, 0f, 0f), "检查楼梯边旧木箱", "箱盖弹开，里面只有一截断裂的绳扣",
                "你把木箱盖合上", ChapterTwoSetProp.SoundKind.Wood);

            Transform crankRoot = new GameObject("ChapterTwo_StairWallCrank_可互动").transform;
            crankRoot.SetParent(root, false);
            crankRoot.localPosition = new Vector3(-3.02f, -1.10f, -17.05f);
            Box("CrankMount", crankRoot, Vector3.zero, new Vector3(.20f, .44f, .12f), darkIron, false);
            Cylinder("CrankPivot", crankRoot, new Vector3(0f, .05f, -.10f), new Vector3(.08f, .16f, .08f), brass, false);
            GameObject crankHandle = Box("CrankHandle", crankRoot, new Vector3(0f, .05f, -.30f),
                new Vector3(.10f, .10f, .48f), darkIron, false);
            crankRoot.gameObject.AddComponent<ChapterTwoSetProp>().ConfigureTurn(crankHandle.transform,
                new Vector3(0f, 0f, -62f), "转动楼梯壁上的旧摇柄", "远处的迷宫门框亮起一盏冷灯",
                "摇柄回到了原位", ChapterTwoSetProp.SoundKind.Metal);

            Transform bellRoot = new GameObject("ChapterTwo_StairLandingBell_可互动").transform;
            bellRoot.SetParent(root, false);
            bellRoot.localPosition = new Vector3(2.85f, -1.95f, -21.20f);
            Cylinder("BellBody", bellRoot, Vector3.zero, new Vector3(.22f, .26f, .22f), brass, false);
            GameObject bellClapper = Sphere("BellClapper", bellRoot, new Vector3(0f, -.20f, 0f),
                new Vector3(.10f, .10f, .10f), darkIron, false);
            bellRoot.gameObject.AddComponent<ChapterTwoSetProp>().ConfigurePulse(bellClapper.transform,
                "轻敲转角铜铃", "铜铃只响了一声，楼梯下方传来回声", ChapterTwoSetProp.SoundKind.Metal);

            Transform noticeRoot = new GameObject("ChapterTwo_MazeThresholdNotice_可互动").transform;
            noticeRoot.SetParent(root, false);
            noticeRoot.localPosition = new Vector3(-2.35f, -6.48f, -26.25f);
            Box("NoticeBoard", noticeRoot, Vector3.zero, new Vector3(.08f, .92f, 1.32f), timber, false);
            Box("NoticePaper", noticeRoot, new Vector3(.055f, 0f, 0f), new Vector3(.018f, .68f, 1.02f), paper, false);
            GameObject noticeLatch = Box("NoticeLatch", noticeRoot, new Vector3(.08f, -.30f, 0f),
                new Vector3(.055f, .13f, .22f), brass, false);
            noticeRoot.gameObject.AddComponent<ChapterTwoSetProp>().ConfigureSlide(noticeLatch.transform,
                new Vector3(0f, .16f, 0f), "拨动迷宫入口的铜扣", "纸牌背面露出一行新的手写标记",
                "铜扣回到原处", ChapterTwoSetProp.SoundKind.Metal);
        }

        private static void BuildStairEdgeCurb(Transform parent, string label, float x, float zStart,
            float direction, float yStart, int count, Material timber,
            float tread = .48f, float rise = .21f, bool openLeftAtStart = false,
            bool openRightAtEnd = false)
        {
            for (int side = -1; side <= 1; side += 2)
            {
                int startIndex = side < 0 && openLeftAtStart ? 3 : 0;
                int endIndex = side > 0 && openRightAtEnd ? count - 4 : count - 1;
                Vector3 start = new(x + side * 1.43f, yStart - startIndex * rise + .12f,
                    zStart + direction * startIndex * tread);
                Vector3 end = new(x + side * 1.43f, yStart - endIndex * rise + .12f,
                    zStart + direction * endIndex * tread);
                Vector3 delta = end - start;
                GameObject curb = Box(label + (side < 0 ? "_LeftCurb" : "_RightCurb"), parent,
                    (start + end) * .5f + Vector3.up * .15f,
                    new Vector3(.20f, .52f, delta.magnitude), timber, true);
                curb.transform.localRotation = Quaternion.FromToRotation(Vector3.forward, delta.normalized);
            }
        }

        private static void CreateStairwellGuideLight(Transform parent, string name, Vector3 position,
            Color color, float intensity, float range)
        {
            GameObject fixture = new(name);
            fixture.transform.SetParent(parent, false);
            fixture.transform.localPosition = position;
            Light light = fixture.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = color;
            light.intensity = intensity;
            light.range = range;
            light.shadows = LightShadows.None;
            Material glow = MakeEmissionMaterial(name + "_Glow", color, 3.20f);
            Box(name + "_LED", parent, position, new Vector3(.22f, .14f, .055f), glow, false);
        }

        private static void BuildStairFlight(Transform parent, string label, float x, float zStart,
            float direction, float yStart, int count, Material stone, Material timber, Material brass,
            float tread = .48f, float rise = .21f, bool openLeftAtStart = false,
            bool openRightAtEnd = false)
        {
            const float stairWidth = 2.86f;
            Material stepGuide = MakeEmissionMaterial(label + "_StepGuide", new Color(.48f, .18f, .055f), .72f);
            Material riserWood = MakeMaterial(label + "_RiserWood", new Color(.20f, .070f, .025f), .24f, .02f);
            ApplySurfaceTexture(riserWood, SurfaceKind.Wood, new Color(.20f, .070f, .025f));
            for (int i = 0; i < count; i++)
            {
                Vector3 position = new(x, yStart - i * rise, zStart + direction * i * tread);
                Box(label + "_Step_" + i, parent, position,
                    new Vector3(stairWidth, .24f, .46f), stone, true);
                Box(label + "_Riser_" + i, parent,
                    position + new Vector3(0f, -.11f, direction * .22f),
                    new Vector3(stairWidth, .22f, .045f), riserWood, false);
                if ((i & 1) == 1)
                    Box(label + "_WoodNosingGuide_" + i, parent, position + Vector3.up * .13f,
                        new Vector3(stairWidth * .82f, .018f, .045f), stepGuide, false);
                if ((i & 1) == 0)
                    Box(label + "_BrassEdge_" + i, parent, position + Vector3.up * .125f,
                        new Vector3(stairWidth * .90f, .025f, .035f), brass, false);
                if (i % 4 == 0 || i == count - 1)
                {
                    if (!(openLeftAtStart && i == 0))
                        Cylinder(label + "_LeftPost_" + i, parent,
                            position + new Vector3(-1.50f, .62f, 0f), new Vector3(.045f, .62f, .045f), timber, false);
                    if (!(openRightAtEnd && i == count - 1))
                        Cylinder(label + "_RightPost_" + i, parent,
                            position + new Vector3(1.50f, .62f, 0f), new Vector3(.045f, .62f, .045f), timber, false);
                }
            }
            int leftStartIndex = openLeftAtStart ? 3 : 0;
            int rightEndIndex = openRightAtEnd ? count - 4 : count - 1;
            AddStairRail(parent, label + "_LeftRail", x - 1.50f,
                new Vector3(zStart + direction * leftStartIndex * tread,
                    yStart - leftStartIndex * rise + .98f, 0f),
                new Vector3(zStart + direction * (count - 1) * tread,
                    yStart - (count - 1) * rise + .98f, 0f), timber);
            AddStairRail(parent, label + "_RightRail", x + 1.50f,
                new Vector3(zStart, yStart + .98f, 0f),
                new Vector3(zStart + direction * rightEndIndex * tread,
                    yStart - rightEndIndex * rise + .98f, 0f), timber);
            AddStairBeam(parent, label + "_LeftRailCollider", x - 1.50f,
                new Vector3(zStart + direction * leftStartIndex * tread,
                    yStart - leftStartIndex * rise + .98f, 0f),
                new Vector3(zStart + direction * (count - 1) * tread,
                    yStart - (count - 1) * rise + .98f, 0f), .08f, timber);
            AddStairBeam(parent, label + "_RightRailCollider", x + 1.50f,
                new Vector3(zStart, yStart + .98f, 0f),
                new Vector3(zStart + direction * rightEndIndex * tread,
                    yStart - rightEndIndex * rise + .98f, 0f), .08f, timber);

            // A hidden continuous ramp sits just below the wooden treads. It is
            // the reliable walkable surface; the individual step colliders remain
            // for visual grounding and to stop the player slipping sideways.
            Vector3 rampStart = new(x, yStart + .09f, zStart);
            Vector3 rampEnd = new(x, yStart - (count - 1) * rise + .09f,
                zStart + direction * (count - 1) * tread);
            Vector3 rampDelta = rampEnd - rampStart;
            GameObject ramp = Box(label + "_ContinuousWalkableRamp", parent,
                (rampStart + rampEnd) * .5f - Vector3.up * .12f,
                new Vector3(stairWidth - .08f, .20f, rampDelta.magnitude), timber, true);
            ramp.transform.localRotation = Quaternion.FromToRotation(Vector3.forward, rampDelta.normalized);
            Renderer rampRenderer = ramp.GetComponent<Renderer>();
            if (rampRenderer != null) rampRenderer.enabled = false;
        }

        private static void AddStairBeam(Transform parent, string name, float x,
            Vector3 start, Vector3 end, float width, Material material)
        {
            Vector3 localStart = new Vector3(x, start.y, start.x);
            Vector3 localEnd = new Vector3(x, end.y, end.x);
            Vector3 delta = localEnd - localStart;
            GameObject beam = Box(name, parent, (localStart + localEnd) * .5f,
                new Vector3(width, width, delta.magnitude), material, true);
            beam.transform.localRotation = Quaternion.LookRotation(delta.normalized, Vector3.up);
        }

        private static void AddStairRail(Transform parent, string name, float x, Vector3 start,
            Vector3 end, Material material)
        {
            GameObject railObject = new(name);
            railObject.transform.SetParent(parent, false);
            LineRenderer rail = railObject.AddComponent<LineRenderer>();
            rail.useWorldSpace = false;
            rail.positionCount = 2;
            rail.widthMultiplier = .075f;
            rail.numCapVertices = 3;
            rail.numCornerVertices = 3;
            rail.sharedMaterial = material;
            rail.startColor = Color.white;
            rail.endColor = Color.white;
            rail.SetPosition(0, new Vector3(x, start.y, start.x));
            rail.SetPosition(1, new Vector3(x, end.y, end.x));
        }

        private MazeGraph BuildMirrorMaze(Transform root, Material stone, Material mirror,
            Material timber, Material brass, Material guide)
        {
            string[] map =
            {
                "#####.#####",
                "#...#.....#",
                "#.#.#####.#",
                "#.#.......#",
                "#.#######.#",
                "#.......#.#",
                "#######.#.#",
                "#.....#...#",
                "#.###.###.#",
                "#.........#",
                "#.#########"
            };
            const float cell = 2.52f;
            const float floorY = -7.58f;
            // The lower level sits beyond the complete switchback flight. This
            // keeps the maze's north wall out of the stairwell and leaves a real
            // landing at the bottom step.
            const float startZ = -27.62f;
            Transform mazeRoot = new GameObject("一楼镜像迷宫").transform;
            mazeRoot.SetParent(root, false);
            Material backing = MakeMaterial("MazeMirrorBacking", new Color(.038f, .052f, .055f), .18f, .04f);
            Material floorCool = MakeMaterial("MazeFloorBlueStone", new Color(.10f, .14f, .14f), .34f, .07f);
            Material floorWarm = MakeMaterial("MazeFloorCinnabar", new Color(.16f, .060f, .043f), .30f, .05f);
            Material ceiling = MakeMaterial("MazeCeilingLacquer", new Color(.058f, .027f, .022f), .24f, .03f);
            Material grout = MakeMaterial("MazeWetGrout", new Color(.018f, .027f, .030f), .16f, .02f);
            Material plinth = MakeMaterial("MazeBlackenedBrass", new Color(.18f, .12f, .058f), .62f, .48f);
            Material dimGuide = MakeEmissionMaterial("MazeFloorInlay", new Color(.028f, .11f, .10f), .055f);
            Material warmGuide = MakeEmissionMaterial("MazeWarmLantern", new Color(.42f, .11f, .028f), .42f);
            Material coolGuide = MakeEmissionMaterial("MazeCoolLantern", new Color(.045f, .23f, .21f), .36f);
            Material mirrorMist = MakeMaterial("MazeMirrorOxidation", new Color(.12f, .20f, .19f, .26f), .30f, .04f);
            ConfigureSmokedMirror(mirrorMist);
            mirrorMist.renderQueue = 3001;
            Material mirrorGhost = MakeMaterial("MazeMirrorReflectionTrace", new Color(.12f, .26f, .28f, .24f), .97f, .72f);
            ConfigureSmokedMirror(mirrorGhost);
            mirrorGhost.renderQueue = 3003;
            Material mirrorRefraction = CreateMirrorRefractionMaterial();
            Material floorSheen = MakeMaterial("MazeFloorMoisture", new Color(.11f, .19f, .18f, .20f), .88f, .18f);
            ConfigureSmokedMirror(floorSheen);
            floorSheen.renderQueue = 3002;
            SetSubtleEmission(backing, new Color(.010f, .017f, .017f));
            SetSubtleEmission(floorCool, new Color(.008f, .014f, .013f));
            SetSubtleEmission(floorWarm, new Color(.012f, .004f, .002f));
            SetSubtleEmission(ceiling, new Color(.004f, .002f, .001f));
            SetSubtleEmission(plinth, new Color(.012f, .006f, .001f));
            SetSubtleEmission(mirrorMist, new Color(.014f, .026f, .024f));
            SetSubtleEmission(mirrorGhost, new Color(.026f, .064f, .068f));
            SetSubtleEmission(floorSheen, new Color(.010f, .019f, .018f));
            ApplySurfaceTexture(floorCool, SurfaceKind.Stone, new Color(.10f, .14f, .14f));
            ApplySurfaceTexture(floorWarm, SurfaceKind.Stone, new Color(.16f, .060f, .043f));
            ApplySurfaceTexture(backing, SurfaceKind.Stone, new Color(.038f, .052f, .055f));
            Box("迷宫地面基层", mazeRoot, new Vector3(0f, floorY, startZ - cell * 5f),
                new Vector3(cell * 11f, .30f, cell * 11f), backing, true);
            // Keep the maze roof in the obstruction mask.  It is above the
            // playable height, but the third-person camera can otherwise orbit
            // through it when the player reaches the stair transition.
            Box("迷宫天花", mazeRoot, new Vector3(0f, floorY + 4.62f, startZ - cell * 5f),
                new Vector3(cell * 11.80f, .28f, cell * 11.80f), ceiling, true);
            for (int band = -4; band <= 4; band += 4)
            {
                Box("MazeCeilingBeamNS_" + band, mazeRoot,
                    new Vector3(band * cell, floorY + 4.18f, startZ - cell * 5f),
                    new Vector3(.18f, .22f, cell * 10.86f), timber, false);
                Box("MazeCeilingBeamEW_" + band, mazeRoot,
                    new Vector3(0f, floorY + 4.16f, startZ - cell * (5f + band)),
                    new Vector3(cell * 10.86f, .16f, .12f), plinth, false);
            }
            float shellY = floorY + 2.02f;
            float northZ = startZ + cell * .52f;
            float southZ = startZ - cell * 10f - cell * .52f;
            float westX = -cell * 5f - cell * .52f;
            float eastX = cell * 5f + cell * .52f;
            Box("迷宫外壳西墙", mazeRoot, new Vector3(westX, shellY, startZ - cell * 5f),
                new Vector3(.34f, 4.10f, cell * 11f + .68f), backing, true);
            Box("迷宫外壳东墙", mazeRoot, new Vector3(eastX, shellY, startZ - cell * 5f),
                new Vector3(.34f, 4.10f, cell * 11f + .68f), backing, true);
            Box("迷宫外壳北墙左", mazeRoot, new Vector3(-cell * 3f, shellY, northZ),
                new Vector3(cell * 5f, 4.10f, .34f), backing, true);
            Box("迷宫外壳北墙右", mazeRoot, new Vector3(cell * 3f, shellY, northZ),
                new Vector3(cell * 5f, 4.10f, .34f), backing, true);
            Box("MazeNorthRoofBandLeft", mazeRoot, new Vector3(-cell * 3f, floorY + 4.28f, northZ),
                new Vector3(cell * 5f, .62f, .38f), backing, true);
            Box("MazeNorthRoofBandRight", mazeRoot, new Vector3(cell * 3f, floorY + 4.28f, northZ),
                new Vector3(cell * 5f, .62f, .38f), backing, true);
            Box("迷宫外壳南墙左", mazeRoot, new Vector3(-cell * 5f, shellY, southZ),
                new Vector3(cell, 4.10f, .34f), backing, true);
            Box("迷宫外壳南墙右", mazeRoot, new Vector3(cell, shellY, southZ),
                new Vector3(cell * 9f, 4.10f, .34f), backing, true);

            bool[,] blocked = new bool[11, 11];
            for (int row = 0; row < map.Length; row++)
            for (int col = 0; col < map[row].Length; col++)
                blocked[col, row] = map[row][col] == '#';

            for (int row = 0; row < map.Length; row++)
            for (int col = 0; col < map[row].Length; col++)
            {
                Vector3 position = MazePosition(col, row, cell, floorY, startZ);
                if (blocked[col, row])
                {
                    Box("镜墙暗背_" + col + "_" + row, mazeRoot, position,
                        new Vector3(cell * .91f, 4.08f, cell * .91f), backing, true);
                    if (col > 0 && !blocked[col - 1, row])
                        AddMirrorFace(mazeRoot, position + Vector3.left * cell * .46f, Quaternion.Euler(0f, -90f, 0f), cell, mirror, mirrorMist, mirrorGhost, mirrorRefraction, brass, timber, plinth);
                    if (col < 10 && !blocked[col + 1, row])
                        AddMirrorFace(mazeRoot, position + Vector3.right * cell * .46f, Quaternion.Euler(0f, 90f, 0f), cell, mirror, mirrorMist, mirrorGhost, mirrorRefraction, brass, timber, plinth);
                    if (row > 0 && !blocked[col, row - 1])
                        AddMirrorFace(mazeRoot, position + Vector3.forward * cell * .46f, Quaternion.identity, cell, mirror, mirrorMist, mirrorGhost, mirrorRefraction, brass, timber, plinth);
                    if (row < 10 && !blocked[col, row + 1])
                        AddMirrorFace(mazeRoot, position + Vector3.back * cell * .46f, Quaternion.Euler(0f, 180f, 0f), cell, mirror, mirrorMist, mirrorGhost, mirrorRefraction, brass, timber, plinth);
                    continue;
                }

                Material tile = ((row + col) & 3) == 0 ? floorWarm : floorCool;
                Box("迷宫地砖_" + col + "_" + row, mazeRoot,
                    new Vector3(position.x, floorY + .175f, position.z),
                    new Vector3(cell * .94f, .050f, cell * .94f), tile, false);
                Box("MazeTileJointX_" + col + "_" + row, mazeRoot,
                    new Vector3(position.x, floorY + .205f, position.z + cell * .475f),
                    new Vector3(cell * .96f, .010f, .032f), grout, false);
                Box("MazeTileJointZ_" + col + "_" + row, mazeRoot,
                    new Vector3(position.x + cell * .475f, floorY + .205f, position.z),
                    new Vector3(.032f, .010f, cell * .96f), grout, false);
                Box("地砖铜钉_" + col + "_" + row, mazeRoot,
                    new Vector3(position.x, floorY + .205f, position.z),
                    new Vector3(.075f, .018f, .075f), ((row + col) & 1) == 0 ? plinth : dimGuide, false);
                if ((row * 3 + col * 5) % 7 == 0)
                {
                    float side = ((row + col) & 1) == 0 ? 1f : -1f;
                    GameObject moisture = Sphere("MazeFloorMoisture_" + col + "_" + row, mazeRoot,
                        new Vector3(position.x + side * .22f, floorY + .224f, position.z - side * .15f),
                        new Vector3(cell * .34f, .009f, cell * .17f), floorSheen, false);
                    moisture.transform.rotation = Quaternion.Euler(0f, (row * 19f + col * 31f) % 70f - 35f, 0f);
                    Box("MazeFloorEdgeInlay_" + col + "_" + row, mazeRoot,
                        new Vector3(position.x - side * cell * .31f, floorY + .222f, position.z + side * cell * .33f),
                        new Vector3(cell * .23f, .011f, .024f), dimGuide, false);
                }
                if ((row + col) % 4 == 0)
                    Box("天花短梁_" + col + "_" + row, mazeRoot,
                        new Vector3(position.x, floorY + 4.18f, position.z),
                        new Vector3(cell * .82f, .14f, .18f), timber, false);
            }

            int[,] lamps = { { 5, 1 }, { 5, 3 }, { 8, 3 }, { 1, 5 }, { 6, 5 }, { 9, 7 }, { 3, 7 }, { 5, 9 }, { 1, 10 } };
            for (int i = 0; i < lamps.GetLength(0); i++)
            {
                Vector3 position = MazePosition(lamps[i, 0], lamps[i, 1], cell, floorY, startZ);
                Color lampColor = i % 3 == 1 ? new Color(.64f, .25f, .12f) : new Color(.16f, .55f, .52f);
                Material lampMaterial = i % 3 == 1 ? warmGuide : coolGuide;
                GameObject lampObject = Sphere("迷宫低照明_" + i, mazeRoot,
                    position + Vector3.up * 2.72f, new Vector3(.16f, .16f, .16f), lampMaterial, false);
                Cylinder("迷宫灯笼铜罩_" + i, mazeRoot, position + Vector3.up * 2.72f,
                    new Vector3(.24f, .32f, .24f), mirrorMist, false);
                Box("迷宫灯笼悬杆_" + i, mazeRoot, position + Vector3.up * 3.47f,
                    new Vector3(.030f, 1.18f, .030f), plinth, false);
                Cylinder("MazeLanternCap_" + i, mazeRoot, position + Vector3.up * 3.08f,
                    new Vector3(.29f, .055f, .29f), plinth, false);
                Cylinder("MazeLanternFoot_" + i, mazeRoot, position + Vector3.up * 2.36f,
                    new Vector3(.26f, .045f, .26f), plinth, false);
                for (int cage = 0; cage < 4; cage++)
                {
                    float angle = cage * Mathf.PI * .5f;
                    Box("MazeLanternCage_" + i + "_" + cage, mazeRoot,
                        position + Vector3.up * 2.72f + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * .19f,
                        new Vector3(.025f, .62f, .025f), plinth, false);
                }
                Light light = lampObject.AddComponent<Light>();
                lampObject.name = "MazeLantern_" + i;
                light.type = LightType.Point;
                light.color = lampColor;
                light.intensity = .92f;
                light.range = 5.0f;
                light.shadows = LightShadows.None;
            }

            Vector3 exitPosition = MazePosition(1, 10, cell, floorY, startZ);
            Vector3 exitCenter = new(exitPosition.x, floorY + 1.55f, exitPosition.z - cell * .18f);
            mazeExitObject = Box("迷宫出口", mazeRoot, exitCenter,
                new Vector3(1.82f, 2.75f, .12f), MakeEmissionMaterial("MazeExitVeil", new Color(.18f, .47f, .39f), .38f), false);
            Box("出口左柱", mazeRoot, exitCenter + new Vector3(-1.12f, .12f, 0f), new Vector3(.28f, 3.25f, .34f), brass, false);
            Box("出口右柱", mazeRoot, exitCenter + new Vector3(1.12f, .12f, 0f), new Vector3(.28f, 3.25f, .34f), brass, false);
            Box("出口横梁", mazeRoot, exitCenter + new Vector3(0f, 1.65f, 0f), new Vector3(2.52f, .28f, .34f), brass, false);
            AddFloatingLabel(mazeExitObject.transform, "归", new Vector3(0f, .06f, -.09f),
                new Vector3(0f, 180f, 0f), 72, new Color(.82f, .68f, .35f));
            GameObject exitLightObject = new("迷宫出口余光");
            exitLightObject.transform.SetParent(mazeRoot, false);
            exitLightObject.transform.position = exitCenter + Vector3.up * .55f;
            Light exitLight = exitLightObject.AddComponent<Light>();
            exitLightObject.name = "MazeExitLight";
            exitLight.type = LightType.Point;
            exitLight.color = new Color(.24f, .61f, .48f);
            exitLight.intensity = 1.05f;
            exitLight.range = 4.8f;
            exitLight.shadows = LightShadows.None;

            mazeWorldLabels.Clear();
            mazeLabelsVisible = false;
            foreach (TextMesh label in mazeRoot.GetComponentsInChildren<TextMesh>(true))
            {
                mazeWorldLabels.Add(label.gameObject);
                label.gameObject.SetActive(false);
            }

            mazeReflectionProbe = new GameObject("迷宫动态反射探针").AddComponent<ReflectionProbe>();
            mazeReflectionProbe.transform.SetParent(mazeRoot, false);
            mazeReflectionProbe.transform.position = new Vector3(0f, floorY + 1.9f, startZ - cell * 5f);
            mazeReflectionProbe.mode = UnityEngine.Rendering.ReflectionProbeMode.Realtime;
            mazeReflectionProbe.refreshMode = UnityEngine.Rendering.ReflectionProbeRefreshMode.OnAwake;
            mazeReflectionProbe.timeSlicingMode = UnityEngine.Rendering.ReflectionProbeTimeSlicingMode.IndividualFaces;
            mazeReflectionProbe.resolution = 64;
            mazeReflectionProbe.boxProjection = true;
            mazeReflectionProbe.blendDistance = 1.35f;
            mazeReflectionProbe.size = new Vector3(cell * 10f, 4.2f, cell * 10f);
            mazeReflectionProbe.intensity = 1.45f;
            mazeReflectionProbe.importance = 3;
            mazeReflectionProbe.renderDynamicObjects = false;
            // Realtime cube captures are too expensive for the chase on low-end GPUs.
            // The mirror's lit/specular layer provides the reflection cue without a
            // six-face render hitch when the player enters the maze.
            mazeReflectionProbe.enabled = false;
            StaticBatchingUtility.Combine(mazeRoot.gameObject);
            return new MazeGraph(blocked, cell, floorY, startZ);
        }

        private void AddMirrorFace(Transform root, Vector3 center, Quaternion rotation, float cell,
            Material mirror, Material mirrorMist, Material mirrorGhost, Material mirrorRefraction,
            Material brass, Material timber, Material plinth)
        {
            GameObject face = Box("烟熏镜面", root, center, new Vector3(cell * .80f, 3.34f, .055f), mirror, false);
            face.transform.rotation = rotation;
            Vector3 right = rotation * Vector3.right;
            Vector3 up = Vector3.up;
            Vector3 normal = rotation * Vector3.forward;
            Box("镜框上", root, center + up * 1.78f - normal * .018f,
                new Vector3(cell * .92f, .10f, .10f), brass, false).transform.rotation = rotation;
            Box("镜框下", root, center - up * 1.78f - normal * .018f,
                new Vector3(cell * .92f, .16f, .13f), plinth, false).transform.rotation = rotation;
            GameObject left = Box("镜框左", root, center - right * cell * .43f - normal * .018f,
                new Vector3(.10f, 3.66f, .10f), brass, false);
            left.transform.rotation = rotation;
            GameObject rightFrame = Box("镜框右", root, center + right * cell * .43f - normal * .018f,
                new Vector3(.10f, 3.66f, .10f), brass, false);
            rightFrame.transform.rotation = rotation;
            Box("镜面腰线", root, center - normal * .035f,
                new Vector3(cell * .82f, .035f, .065f), brass, false).transform.rotation = rotation;
            GameObject midRail = Box("MirrorMidRail", root, center + up * .22f - normal * .055f,
                new Vector3(cell * .70f, .032f, .045f), brass, false);
            midRail.transform.rotation = rotation;
            GameObject lowerRail = Box("MirrorLowerRail", root, center - up * .88f - normal * .055f,
                new Vector3(cell * .70f, .020f, .035f), timber, false);
            lowerRail.transform.rotation = rotation;
            GameObject plinthPanel = Box("MazeMirrorPlinth", root,
                center - up * 1.53f - normal * .055f,
                new Vector3(cell * .82f, .29f, .075f), plinth, false);
            plinthPanel.transform.rotation = rotation;
            GameObject ageBloom = Sphere("MazeMirrorAgeBloom", root,
                center + right * cell * .22f - up * .68f - normal * .086f,
                new Vector3(cell * .30f, .13f, .012f), mirrorMist, false);
            ageBloom.transform.rotation = rotation * Quaternion.Euler(0f, 0f, 8f);
            GameObject upperMist = Sphere("MazeMirrorUpperMist", root,
                center - right * cell * .17f + up * 1.16f - normal * .086f,
                new Vector3(cell * .40f, .055f, .010f), mirrorMist, false);
            upperMist.transform.rotation = rotation * Quaternion.Euler(0f, 0f, -4f);
            GameObject verticalAge = Capsule("MazeMirrorVerticalAge", root,
                center - right * cell * .31f + up * .18f - normal * .087f,
                new Vector3(.040f, .52f, .010f), mirrorMist, false);
            verticalAge.transform.rotation = rotation;
            // A Quad is intentional here. A thin cube puts the reflection
            // texture on its sides and back as well, creating black wedges when
            // the player approaches a mirror at an angle.
            GameObject reflectionGlass = GameObject.CreatePrimitive(PrimitiveType.Quad);
            reflectionGlass.name = "MazeMirrorReflectionGlass";
            reflectionGlass.transform.SetParent(root, false);
            reflectionGlass.transform.localPosition = center + normal * .032f;
            reflectionGlass.transform.localScale = new Vector3(cell * .80f, 3.34f, 1f);
            reflectionGlass.GetComponent<Renderer>().sharedMaterial = mirrorRefraction;
            reflectionGlass.transform.rotation = rotation;
            SetLayerRecursively(reflectionGlass, MazeMirrorOverlayLayer);
            Renderer reflectionRenderer = reflectionGlass.GetComponent<Renderer>();
            if (reflectionRenderer != null)
            {
                mazeMirrorSurfaces.Add(new MazeMirrorSurface
                {
                    renderer = reflectionRenderer,
                    transform = reflectionGlass.transform,
                    normal = (rotation * Vector3.forward).normalized
                });
            }
            GameObject ghostTop = Box("MazeMirrorGhostTop", root,
                center + up * 1.18f + normal * .074f,
                new Vector3(cell * .62f, .038f, .018f), mirrorGhost, false);
            ghostTop.transform.rotation = rotation;
            GameObject ghostSide = Box("MazeMirrorGhostSide", root,
                center + right * cell * .27f + up * .22f + normal * .074f,
                new Vector3(.032f, 1.42f, .018f), mirrorGhost, false);
            ghostSide.transform.rotation = rotation;
            int reflectionSeed = Mathf.Abs(Mathf.RoundToInt(center.x * 10f + center.z * 7f));
            if (reflectionSeed % 3 == 0)
            {
                GameObject ghostLamp = Sphere("MazeMirrorGhostLamp", root,
                    center - right * cell * .16f + up * .53f + normal * .078f,
                    new Vector3(.12f, .07f, .014f), mirrorGhost, false);
                ghostLamp.transform.rotation = rotation * Quaternion.Euler(0f, 0f, -10f);
            }
            for (int side = -1; side <= 1; side += 2)
            for (int corner = -1; corner <= 1; corner += 2)
            {
                GameObject rivet = Sphere("MirrorFrameRivet", root,
                    center + right * side * cell * .43f + up * corner * 1.60f - normal * .09f,
                    new Vector3(.055f, .055f, .055f), brass, false);
                rivet.transform.rotation = rotation;
            }
        }

        private void BuildMazeReflectionSnapshot()
        {
            if (viewCamera == null || mazeMirrorSurfaces.Count == 0)
                return;
            if (mazeReflectionCamera == null)
            {
                GameObject cameraObject = new("迷宫动态平面反射相机");
                cameraObject.transform.SetParent(transform, false);
                mazeReflectionCamera = cameraObject.AddComponent<Camera>();
                mazeReflectionCamera.enabled = false;
                mazeReflectionCamera.clearFlags = CameraClearFlags.SolidColor;
                mazeReflectionCamera.backgroundColor = new Color(.002f, .006f, .007f, 1f);
                mazeReflectionCamera.nearClipPlane = .035f;
                mazeReflectionCamera.farClipPlane = 80f;
                mazeReflectionCamera.allowHDR = false;
                mazeReflectionCamera.allowMSAA = false;
                mazeReflectionCamera.useOcclusionCulling = false;
                mazeReflectionBaseMask =
                    (viewCamera.cullingMask | (1 << PlayerVisualLayer)) & ~(1 << MazeMirrorOverlayLayer);
                mazeReflectionCamera.cullingMask = mazeReflectionBaseMask;
            }

            foreach (MazeMirrorSurface surface in mazeMirrorSurfaces)
            {
                if (surface == null || surface.renderer == null || surface.transform == null) continue;
                if (surface.recursionTextures == null || surface.recursionTextures.Length != MazeReflectionRecursionDepth)
                {
                    surface.recursionTextures = new RenderTexture[MazeReflectionRecursionDepth];
                    for (int depth = 0; depth < surface.recursionTextures.Length; depth++)
                    {
                        int resolution = depth == 0 ? MazeReflectionResolution : MazeReflectionRecursionResolution;
                        RenderTexture texture = new RenderTexture(resolution, resolution, 16,
                            RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear)
                        {
                            name = "ChapterTwoMazeMirrorReflection_" + mazeMirrorSurfaces.IndexOf(surface) + "_Depth" + depth,
                            filterMode = FilterMode.Bilinear,
                            wrapMode = TextureWrapMode.Clamp,
                            antiAliasing = 1,
                            useMipMap = false,
                            autoGenerateMips = false
                        };
                        texture.Create();
                        surface.recursionTextures[depth] = texture;
                    }
                    surface.texture = surface.recursionTextures[0];
                }
                if (surface.material == null)
                {
                    surface.material = CreateMirrorRefractionMaterial();
                    if (surface.material.HasProperty("_BaseMap"))
                        surface.material.SetTexture("_BaseMap", surface.texture);
                    if (surface.material.HasProperty("_MainTex"))
                        surface.material.SetTexture("_MainTex", surface.texture);
                    surface.renderer.sharedMaterial = surface.material;
                }
            }

            mazeReflectionCursor = 0;
            mazeReflectionNextUpdateAt = 0f;
            mazeReflectionNextPrewarmAt = 0f;
            SetMazeMirrorTextureLevel(0);
            UpdateMazeMirrorReflections(true);
        }

        private void UpdateMazeMirrorReflections()
        {
            UpdateMazeMirrorReflections(false);
        }

        private void UpdateMazeMirrorReflections(bool force)
        {
            if (mazeReflectionCamera == null || viewCamera == null || mazeMirrorSurfaces.Count == 0 ||
                Time.unscaledTime < mazeReflectionNextUpdateAt) return;
            mazeReflectionNextUpdateAt = Time.unscaledTime + MazeReflectionUpdateInterval;

            MazeMirrorSurface nearest = null;
            MazeMirrorSurface prewarm = null;
            float nearestScore = float.PositiveInfinity;
            float prewarmScore = float.PositiveInfinity;
            for (int i = 0; i < mazeMirrorSurfaces.Count; i++)
            {
                MazeMirrorSurface candidate = mazeMirrorSurfaces[i];
                if (candidate == null || candidate.renderer == null || candidate.transform == null) continue;
                bool visible = IsMirrorVisible(candidate);
                bool ready = candidate.lastRenderedAt > float.NegativeInfinity;
                candidate.renderer.enabled = visible && ready;
                float distance = (candidate.transform.position - viewCamera.transform.position).sqrMagnitude;
                float playerDistance = player != null
                    ? (candidate.transform.position - player.position).sqrMagnitude : distance;
                if (visible)
                {
                    float freshness = Mathf.Clamp01((Time.unscaledTime - candidate.lastRenderedAt) * .12f);
                    float score = distance * (1f - freshness * .55f);
                    if (score < nearestScore)
                    {
                        nearestScore = score;
                        nearest = candidate;
                    }
                }
                if (playerDistance <= MazeReflectionPrewarmDistance * MazeReflectionPrewarmDistance &&
                    candidate != nearest && (!ready || Time.unscaledTime - candidate.lastRenderedAt > .24f))
                {
                    float score = playerDistance + (visible ? .5f : 0f);
                    if (score < prewarmScore)
                    {
                        prewarmScore = score;
                        prewarm = candidate;
                    }
                }
            }

            if (nearest != null)
            {
                RenderMazeMirrorSurface(nearest);
                if (nearest.renderer != null && IsMirrorVisible(nearest)) nearest.renderer.enabled = true;
            }
            // Nearby mirrors are rendered ahead of the player. This pass is
            // deliberately separate from the visible-mirror budget so a turn
            // into a new corridor never reveals an uninitialized mirror.
            if (prewarm != null && (force ||
                (nearest == null && Time.unscaledTime >= mazeReflectionNextPrewarmAt)))
            {
                PrewarmMazeMirrorSurface(prewarm);
                mazeReflectionNextPrewarmAt = Time.unscaledTime + MazeReflectionPrewarmInterval;
            }
            mazeReflectionCursor = (mazeReflectionCursor + 1) % Mathf.Max(1, mazeMirrorSurfaces.Count);
        }

        private bool IsMirrorVisible(MazeMirrorSurface surface)
        {
            if (surface == null || surface.renderer == null || surface.transform == null)
                return false;
            Vector3 toCamera = viewCamera.transform.position - surface.transform.position;
            float distance = toCamera.magnitude;
            if (distance < .08f || distance > 52f) return false;
            if (Vector3.Dot(surface.normal, toCamera / distance) < .035f) return false;
            Vector3 viewport = viewCamera.WorldToViewportPoint(surface.transform.position);
            if (viewport.z <= 0f || viewport.x <= -.42f || viewport.x >= 1.42f ||
                viewport.y <= -.42f || viewport.y >= 1.42f) return false;
            Vector3 toSurface = (surface.transform.position - viewCamera.transform.position) / distance;
            // Do not let a mirror that is almost edge-on to the view become a
            // full-screen sliver when the player is close to a maze corner.
            return Vector3.Dot(viewCamera.transform.forward, toSurface) > .12f;
        }

        private void RenderMazeMirrorSurface(MazeMirrorSurface surface, bool requireVisible = true)
        {
            if (surface == null || surface.recursionTextures == null ||
                surface.recursionTextures.Length < MazeReflectionRecursionDepth ||
                (requireVisible && !IsMirrorVisible(surface))) return;
            Vector3 normal = surface.normal.normalized;
            Vector3 mirrorPoint = surface.transform.position;
            Vector4 plane = new(normal.x, normal.y, normal.z, -Vector3.Dot(normal, mirrorPoint));
            Matrix4x4 reflection = Matrix4x4.zero;
            CalculateReflectionMatrix(ref reflection, plane);

            bool previousRendererEnabled = surface.renderer != null && surface.renderer.enabled;
            if (surface.renderer != null) surface.renderer.enabled = false;
            try
            {
                // Depth 1 is a clean scene pass. Depth 0 then samples that pass
                // on the other mirror quads, producing a real two-bounce mutual
                // reflection without recursively rendering forever.
                if (Time.unscaledTime - surface.lastRecursiveRenderedAt >= MazeReflectionRecursiveInterval)
                {
                    SetMazeMirrorTextureLevel(-1);
                    mazeReflectionCamera.cullingMask = mazeReflectionBaseMask;
                    RenderMazeMirrorPass(surface, surface.recursionTextures[1], mirrorPoint, normal, reflection);
                    surface.lastRecursiveRenderedAt = Time.unscaledTime;
                }

                SetMazeMirrorTextureLevel(1);
                mazeReflectionCamera.cullingMask = mazeReflectionBaseMask | (1 << MazeMirrorOverlayLayer);
                RenderMazeMirrorPass(surface, surface.recursionTextures[0], mirrorPoint, normal, reflection);

                SetMazeMirrorTextureLevel(0);
            }
            finally
            {
                if (surface.renderer != null) surface.renderer.enabled = previousRendererEnabled;
                mazeReflectionCamera.targetTexture = null;
                mazeReflectionCamera.cullingMask = mazeReflectionBaseMask;
            }
            surface.lastRenderedAt = Time.unscaledTime;
        }

        private void PrewarmMazeMirrorSurface(MazeMirrorSurface surface)
        {
            if (surface == null || surface.renderer == null || surface.recursionTextures == null ||
                surface.recursionTextures.Length < MazeReflectionRecursionDepth) return;
            Vector3 normal = surface.normal.normalized;
            Vector3 mirrorPoint = surface.transform.position;
            Vector4 plane = new(normal.x, normal.y, normal.z, -Vector3.Dot(normal, mirrorPoint));
            Matrix4x4 reflection = Matrix4x4.zero;
            CalculateReflectionMatrix(ref reflection, plane);

            bool previousRendererEnabled = surface.renderer.enabled;
            surface.renderer.enabled = false;
            try
            {
                SetMazeMirrorTextureLevel(-1);
                mazeReflectionCamera.cullingMask = mazeReflectionBaseMask;
                RenderMazeMirrorPass(surface, surface.recursionTextures[1], mirrorPoint, normal, reflection);
                Graphics.Blit(surface.recursionTextures[1], surface.recursionTextures[0]);
                SetMazeMirrorTextureLevel(0);
            }
            finally
            {
                surface.renderer.enabled = previousRendererEnabled;
                mazeReflectionCamera.targetTexture = null;
                mazeReflectionCamera.cullingMask = mazeReflectionBaseMask;
            }
            surface.lastRenderedAt = Time.unscaledTime;
            surface.lastRecursiveRenderedAt = Time.unscaledTime;
        }

        private void SetMazeMirrorTextureLevel(int depth)
        {
            foreach (MazeMirrorSurface surface in mazeMirrorSurfaces)
            {
                if (surface == null || surface.material == null) continue;
                Texture texture = depth >= 0 && surface.recursionTextures != null &&
                                  depth < surface.recursionTextures.Length
                    ? surface.recursionTextures[depth] : null;
                if (surface.material.HasProperty("_BaseMap")) surface.material.SetTexture("_BaseMap", texture);
                if (surface.material.HasProperty("_MainTex")) surface.material.SetTexture("_MainTex", texture);
            }
        }

        private void RenderMazeMirrorPass(MazeMirrorSurface surface, RenderTexture target,
            Vector3 mirrorPoint, Vector3 normal, Matrix4x4 reflection)
        {
            mazeReflectionCamera.fieldOfView = viewCamera.fieldOfView;
            mazeReflectionCamera.aspect = 1f;
            mazeReflectionCamera.targetTexture = target;
            mazeReflectionCamera.worldToCameraMatrix = viewCamera.worldToCameraMatrix * reflection;
            Vector3 reflectedPosition = reflection.MultiplyPoint(viewCamera.transform.position);
            Vector3 reflectedForward = reflection.MultiplyVector(viewCamera.transform.forward).normalized;
            Vector3 reflectedUp = reflection.MultiplyVector(viewCamera.transform.up).normalized;
            mazeReflectionCamera.transform.SetPositionAndRotation(reflectedPosition,
                Quaternion.LookRotation(reflectedForward, reflectedUp));
            Vector4 clipPlane = CameraSpacePlane(mazeReflectionCamera, mirrorPoint, normal, -1f);
            mazeReflectionCamera.projectionMatrix = viewCamera.CalculateObliqueMatrix(clipPlane);

            bool previousInvertCulling = GL.invertCulling;
            GL.invertCulling = true;
            try
            {
                mazeReflectionCamera.Render();
            }
            finally
            {
                GL.invertCulling = previousInvertCulling;
            }
        }

        private static Vector4 CameraSpacePlane(Camera camera, Vector3 position, Vector3 normal, float sideSign)
        {
            Vector3 offsetPosition = position + normal * .025f;
            Matrix4x4 matrix = camera.worldToCameraMatrix;
            Vector3 cameraPosition = matrix.MultiplyPoint(offsetPosition);
            Vector3 cameraNormal = matrix.MultiplyVector(normal).normalized * sideSign;
            return new Vector4(cameraNormal.x, cameraNormal.y, cameraNormal.z,
                -Vector3.Dot(cameraPosition, cameraNormal));
        }

        private static void CalculateReflectionMatrix(ref Matrix4x4 matrix, Vector4 plane)
        {
            matrix.m00 = 1f - 2f * plane.x * plane.x;
            matrix.m01 = -2f * plane.x * plane.y;
            matrix.m02 = -2f * plane.x * plane.z;
            matrix.m03 = -2f * plane.x * plane.w;
            matrix.m10 = -2f * plane.y * plane.x;
            matrix.m11 = 1f - 2f * plane.y * plane.y;
            matrix.m12 = -2f * plane.y * plane.z;
            matrix.m13 = -2f * plane.y * plane.w;
            matrix.m20 = -2f * plane.z * plane.x;
            matrix.m21 = -2f * plane.z * plane.y;
            matrix.m22 = 1f - 2f * plane.z * plane.z;
            matrix.m23 = -2f * plane.z * plane.w;
            matrix.m30 = 0f;
            matrix.m31 = 0f;
            matrix.m32 = 0f;
            matrix.m33 = 1f;
        }

        private static Material CreateMirrorRefractionMaterial()
        {
            // Use URP Lit for the mirror image itself. The old water shader made the
            // overlay look like tinted glass and did not expose a reliable texture
            // slot for the planar reflection render texture.
            Shader shader = Shader.Find("UI/Default") ?? Shader.Find("Universal Render Pipeline/Unlit") ??
                            Shader.Find("Unlit/Texture") ?? Shader.Find("Universal Render Pipeline/Lit") ??
                            Shader.Find("Standard") ?? Shader.Find("MeixiEcho/Creek Water");
            if (shader == null)
            {
                Material fallback = MakeMaterial("MazeMirrorReflectionGlassFallback",
                    new Color(.10f, .18f, .19f, 1f), .95f, .55f);
                ConfigureSmokedMirror(fallback);
                fallback.renderQueue = 3000;
                return fallback;
            }

            Material material = new(shader) { name = "MazeMirrorReflectionGlass" };
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", new Color(.78f, .90f, .88f, 1f));
            if (material.HasProperty("_Color")) material.SetColor("_Color", new Color(.78f, .90f, .88f, 1f));
            if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", .24f);
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", .96f);
            if (material.HasProperty("_Surface")) material.SetFloat("_Surface", 0f);
            if (material.HasProperty("_AlphaClip")) material.SetFloat("_AlphaClip", 0f);
            if (material.HasProperty("_Cull")) material.SetFloat("_Cull", 0f);
            material.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.renderQueue = 3000;
            material.enableInstancing = true;
            return material;
        }

        private static void AddMazeHaze(Transform root, Vector3 center, Vector3 size)
        {
            GameObject hazeObject = new("MazeSuspendedDust");
            hazeObject.transform.SetParent(root, false);
            hazeObject.transform.position = center;
            ParticleSystem particles = hazeObject.AddComponent<ParticleSystem>();
            ParticleSystem.MainModule main = particles.main;
            main.loop = true;
            main.prewarm = true;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.startLifetime = new ParticleSystem.MinMaxCurve(9f, 16f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(.012f, .045f);
            main.startSize = new ParticleSystem.MinMaxCurve(.018f, .060f);
            main.startColor = new ParticleSystem.MinMaxGradient(
                new Color(.20f, .34f, .33f, .018f), new Color(.56f, .32f, .19f, .042f));
            main.maxParticles = 72;

            ParticleSystem.EmissionModule emission = particles.emission;
            emission.rateOverTime = 4.5f;
            ParticleSystem.ShapeModule shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = size;
            ParticleSystem.NoiseModule noise = particles.noise;
            noise.enabled = true;
            noise.strength = .055f;
            noise.frequency = .12f;
            noise.scrollSpeed = .025f;

            ParticleSystemRenderer renderer = hazeObject.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.sortMode = ParticleSystemSortMode.Distance;
            renderer.sharedMaterial = CreateMazeDustMaterial();
        }

        private static Material CreateMazeDustMaterial()
        {
            const int size = 24;
            Texture2D texture = new(size, size, TextureFormat.RGBA32, false)
            {
                name = "MazeDustSoftDisc",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            Color[] pixels = new Color[size * size];
            Vector2 center = new((size - 1) * .5f, (size - 1) * .5f);
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center) / (size * .5f);
                float alpha = Mathf.Pow(Mathf.Clamp01(1f - distance), 2.6f);
                pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
            }
            texture.SetPixels(pixels);
            texture.Apply(false, true);

            Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit") ??
                            Shader.Find("Particles/Standard Unlit") ?? Shader.Find("Unlit/Transparent") ??
                            Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            Material material = new(shader) { name = "MazeDustSoftParticle" };
            if (material.HasProperty("_BaseMap")) material.SetTexture("_BaseMap", texture);
            if (material.HasProperty("_MainTex")) material.SetTexture("_MainTex", texture);
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", new Color(1f, 1f, 1f, .08f));
            if (material.HasProperty("_Color")) material.SetColor("_Color", new Color(1f, 1f, 1f, .08f));
            if (material.HasProperty("_Surface")) material.SetFloat("_Surface", 1f);
            if (material.HasProperty("_SrcBlend")) material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            if (material.HasProperty("_DstBlend")) material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            if (material.HasProperty("_ZWrite")) material.SetFloat("_ZWrite", 0f);
            if (material.HasProperty("_EnvironmentReflections")) material.SetFloat("_EnvironmentReflections", 1f);
            if (material.HasProperty("_SpecularHighlights")) material.SetFloat("_SpecularHighlights", 1f);
            if (material.HasProperty("_ReceiveShadows")) material.SetFloat("_ReceiveShadows", 1f);
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.renderQueue = 3000;
            return material;
        }

        private static Vector3 MazePosition(int col, int row, float cell, float floorY, float startZ)
        {
            return new Vector3((col - 5) * cell, floorY + 2.12f, startZ - row * cell);
        }

        private void BuildAdventureUi()
        {
            objectiveText = FindText("章节目标");
            hintText = FindText("舞台提示文字");
            canvas = objectiveText != null ? objectiveText.GetComponentInParent<Canvas>() : FindAnyObjectByType<Canvas>();
            if (canvas == null)
            {
                canvas = new GameObject("第二章冒险界面").AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                CanvasScaler scaler = canvas.gameObject.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920f, 1080f);
                scaler.matchWidthOrHeight = .5f;
                canvas.gameObject.AddComponent<GraphicRaycaster>();
            }
            canvas.overrideSorting = true;
            canvas.sortingOrder = Mathf.Max(canvas.sortingOrder, 700);
            if (canvas.GetComponent<GraphicRaycaster>() == null) canvas.gameObject.AddComponent<GraphicRaycaster>();
            BuildHeartbeatVignette();
            EnsureAdventureEventSystem();
            if (objectiveText == null) objectiveText = CreateText("章节目标", canvas.transform, string.Empty, 19,
                TextAnchor.MiddleLeft, new Color(.78f, .93f, .94f), new Vector2(.03f, .86f), new Vector2(.40f, .96f));
            if (hintText == null)
            {
                hintCard = CreatePanel("第二章顶部提示横幅", canvas.transform, Color.white,
                    new Vector2(.37f, .76f), new Vector2(.88f, .92f));
                hintText = CreateText("舞台提示文字", hintCard.transform, string.Empty, 26,
                    TextAnchor.MiddleCenter, Color.white, new Vector2(.17f, .26f), new Vector2(.83f, .74f));
            }
            else
            {
                hintCard = hintText.transform.parent.gameObject;
                SetRect(hintCard.GetComponent<RectTransform>(), new Vector2(.70f, .80f), new Vector2(.985f, .945f));
                SetRect(hintText.rectTransform, new Vector2(.10f, .14f), new Vector2(.92f, .86f));
                hintText.fontSize = 18;
                hintText.alignment = TextAnchor.MiddleRight;
                hintText.color = Color.white;
            }
            SetRect(hintCard.GetComponent<RectTransform>(), new Vector2(.70f, .80f), new Vector2(.985f, .945f));
            SetRect(hintText.rectTransform, new Vector2(.10f, .14f), new Vector2(.92f, .86f));
            hintText.fontSize = 18;
            hintText.alignment = TextAnchor.MiddleRight;
            hintText.raycastTarget = false;
            Image hintImage = hintCard.GetComponent<Image>();
            hintImage.sprite = CreateSoftHazeSprite("ChapterTwoHintHaze", 320, 96);
            hintImage.color = new Color(.002f, .006f, .008f, .76f);
            hintImage.raycastTarget = false;
            AddUiShadow(hintText.gameObject, new Color(0f, 0f, 0f, .90f), new Vector2(2f, -2f));
            hintGroup = hintCard.GetComponent<CanvasGroup>() ?? hintCard.AddComponent<CanvasGroup>();
            hintCard.SetActive(false);

            inventoryHud = CreatePanel("冒险图形道具栏", canvas.transform,
                new Color(.002f, .006f, .008f, .80f), new Vector2(.27f, .012f), new Vector2(.73f, .155f));
            Image inventoryHaze = inventoryHud.GetComponent<Image>();
            inventoryHaze.sprite = CreateSoftHazeSprite("ChapterTwoInventoryHaze", 420, 112);
            inventoryHaze.color = new Color(.002f, .006f, .008f, .82f);
            inventoryHaze.raycastTarget = false;
            inventoryHudGroup = inventoryHud.AddComponent<CanvasGroup>();
            inventoryHudGroup.alpha = 0f;
            inventoryHudGroup.blocksRaycasts = false;
            CreateText("道具栏热键", inventoryHud.transform, "I", 15, TextAnchor.MiddleCenter,
                new Color(.78f, .57f, .25f), new Vector2(.008f, .18f), new Vector2(.055f, .82f));
            inventoryText = CreateText("道具栏计数", inventoryHud.transform, "I", 13,
                TextAnchor.MiddleCenter, new Color(.53f, .73f, .69f), new Vector2(.945f, .16f), new Vector2(.995f, .84f));
            for (int i = 0; i < InventoryCapacity; i++)
            {
                float left = .134f + i * .147f;
                GameObject slot = CreatePanel("快捷物件槽_" + (i + 1), inventoryHud.transform,
                    new Color(.004f, .012f, .014f, .70f), new Vector2(left, .06f), new Vector2(left + .126f, .94f));
                hudSlotFrames[i] = slot.GetComponent<Image>();
                hudSlotFrames[i].sprite = CreateSoftHazeSprite("ChapterTwoInventorySlot", 96, 96);
                hudSlotFrames[i].color = new Color(.004f, .012f, .014f, .68f);
                Image icon = CreateImage("物件缩略图", slot.transform, new Vector2(.10f, .10f), new Vector2(.90f, .90f));
                icon.preserveAspect = true;
                icon.raycastTarget = false;
                hudSlotIcons[i] = icon;
                Text number = CreateText("槽位号", slot.transform, (i + 1).ToString(), 11,
                    TextAnchor.UpperLeft, new Color(.74f, .70f, .54f), new Vector2(.05f, .68f), new Vector2(.33f, .96f));
                number.raycastTarget = false;
            }

            inventoryPanel = CreatePanel("道具栏全页", canvas.transform, new Color(.001f, .004f, .005f, .91f),
                Vector2.zero, Vector2.one);
            PromoteModalCanvas(inventoryPanel, 920);
            Image inventoryOverlay = inventoryPanel.GetComponent<Image>();
            inventoryOverlay.sprite = null;
            inventoryOverlay.color = new Color(.001f, .004f, .005f, .91f);
            Image inventoryFog = CreateImage("道具栏中央柔雾", inventoryPanel.transform, Vector2.zero, Vector2.one);
            inventoryFog.sprite = CreateSoftHazeSprite("ChapterTwoInventoryOverlay", 640, 360);
            inventoryFog.color = new Color(.035f, .22f, .20f, .16f);
            inventoryFog.raycastTarget = false;
            GameObject archive = CreatePanel("物证档案台", inventoryPanel.transform, new Color(.004f, .016f, .017f, .92f),
                new Vector2(.085f, .075f), new Vector2(.915f, .925f));
            Image archiveHaze = archive.GetComponent<Image>();
            archiveHaze.sprite = null;
            archiveHaze.color = new Color(.005f, .021f, .022f, .95f);
            Image archiveFog = CreateImage("档案台内层柔雾", archive.transform, Vector2.zero, Vector2.one);
            archiveFog.sprite = CreateSoftHazeSprite("ChapterTwoArchiveHaze", 720, 480);
            archiveFog.color = new Color(.06f, .31f, .28f, .13f);
            archiveFog.raycastTarget = false;
            AddUiOutline(archive, new Color(.32f, .58f, .52f, .34f), 1f);
            AddUiShadow(archive, new Color(0f, 0f, 0f, .88f), new Vector2(0f, -14f));
            CreatePanel("档案左侧铜线", archive.transform, new Color(.78f, .50f, .20f, .72f),
                new Vector2(.022f, .19f), new Vector2(.024f, .84f));
            CreatePanel("档案顶部青线", archive.transform, new Color(.22f, .58f, .53f, .52f),
                new Vector2(.055f, .962f), new Vector2(.84f, .964f));
            Text archiveEyebrow = CreateText("档案眉题", archive.transform, "MEIXI THEATRE  /  EVIDENCE ARCHIVE", 14,
                TextAnchor.MiddleLeft, new Color(.37f, .72f, .68f), new Vector2(.055f, .895f), new Vector2(.70f, .955f));
            archiveEyebrow.raycastTarget = false;
            CreateText("道具栏标题", archive.transform, "调查物件", 36, TextAnchor.MiddleLeft,
                new Color(.94f, .76f, .40f), new Vector2(.055f, .80f), new Vector2(.45f, .90f));
            inventoryCloseButton = CreateButton("关闭物证档案", archive.transform, "×",
                new Vector2(.915f, .875f), new Vector2(.965f, .945f));
            inventoryCloseButton.onClick.AddListener(CloseInventoryUi);

            GameObject slotRail = CreatePanel("物件格陈列", archive.transform, new Color(.002f, .010f, .012f, .46f),
                new Vector2(.045f, .14f), new Vector2(.42f, .77f));
            Image slotRailHaze = slotRail.GetComponent<Image>();
            slotRailHaze.sprite = null;
            slotRailHaze.color = new Color(.003f, .015f, .017f, .82f);
            AddUiOutline(slotRail, new Color(.16f, .38f, .36f, .34f));
            for (int i = 0; i < InventoryCapacity; i++)
            {
                int index = i;
                int row = i / 2;
                int col = i % 2;
                bool wideLast = i == InventoryCapacity - 1;
                float x = wideLast ? .055f : .055f + col * .475f;
                float width = wideLast ? .89f : .415f;
                float yTop = .955f - row * .318f;
                GameObject slot = CreatePanel("档案物件槽_" + (i + 1), slotRail.transform,
                    new Color(.018f, .075f, .073f, .86f), new Vector2(x, yTop - .275f), new Vector2(x + width, yTop));
                inventorySlotFrames[i] = slot.GetComponent<Image>();
                inventorySlotFrames[i].sprite = CreateSoftHazeSprite("ChapterTwoArchiveSlot_" + i, 180, 110);
                AddUiOutline(slot, new Color(.34f, .40f, .27f, .28f));
                Button button = slot.AddComponent<Button>();
                button.targetGraphic = slot.GetComponent<Image>();
                button.onClick.AddListener(() => SelectInventoryItem(index));
                inventorySlotButtons[i] = button;
                Image icon = CreateImage("档案图标", slot.transform, new Vector2(.17f, .25f), new Vector2(.83f, .92f));
                icon.preserveAspect = true;
                icon.raycastTarget = false;
                inventorySlotIcons[i] = icon;
                inventorySlotTitles[i] = CreateText("档案物件名", slot.transform, "未发现", 13,
                    TextAnchor.MiddleCenter, new Color(.34f, .42f, .40f), new Vector2(.06f, .02f), new Vector2(.94f, .25f));
                inventorySlotTitles[i].raycastTarget = false;
                inventorySlotSelectionLines[i] = CreateImage("档案选中光线", slot.transform,
                    new Vector2(.12f, .015f), new Vector2(.88f, .035f));
                inventorySlotSelectionLines[i].color = Color.clear;
                inventorySlotSelectionLines[i].raycastTarget = false;
                CreateText("档案序号", slot.transform, (i + 1).ToString("00"), 10,
                    TextAnchor.UpperLeft, new Color(.60f, .49f, .28f), new Vector2(.04f, .76f), new Vector2(.23f, .96f)).raycastTarget = false;
            }

            GameObject detail = CreatePanel("物件检视摘要", archive.transform, new Color(.010f, .032f, .031f, .74f),
                new Vector2(.455f, .14f), new Vector2(.955f, .77f));
            Image detailHaze = detail.GetComponent<Image>();
            detailHaze.sprite = null;
            detailHaze.color = new Color(.008f, .030f, .029f, .92f);
            AddUiOutline(detail, new Color(.37f, .51f, .38f, .30f));
            inventorySelectionKind = CreateText("物件类别", detail.transform, "EVIDENCE ARCHIVE", 12,
                TextAnchor.MiddleLeft, new Color(.39f, .77f, .70f), new Vector2(.07f, .87f), new Vector2(.72f, .95f));
            inventorySelectionTitle = CreateText("选中物件名", detail.transform, "尚无记录", 32,
                TextAnchor.MiddleLeft, new Color(.95f, .78f, .43f), new Vector2(.07f, .74f), new Vector2(.93f, .88f));
            Image selectionGlow = CreateImage("选中物件柔光", detail.transform, new Vector2(.035f, .31f), new Vector2(.415f, .75f));
            selectionGlow.sprite = CreateSoftHazeSprite("ChapterTwoSelectionGlow", 220, 220);
            selectionGlow.color = new Color(.10f, .42f, .37f, .26f);
            selectionGlow.raycastTarget = false;
            inventorySelectionIcon = CreateImage("选中物件大图", detail.transform, new Vector2(.07f, .36f), new Vector2(.38f, .70f));
            inventorySelectionIcon.preserveAspect = true;
            inventoryFullText = CreateText("道具栏内容", detail.transform, "戏台仍保持着沉默。", 18,
                TextAnchor.UpperLeft, new Color(.79f, .87f, .81f), new Vector2(.43f, .34f), new Vector2(.94f, .70f));
            inventoryFullText.lineSpacing = 1.28f;
            inventoryInspectButton = CreateButton("全屏检视物件", detail.transform, "◎  检视",
                new Vector2(.60f, .10f), new Vector2(.92f, .24f));
            inventoryInspectButton.onClick.AddListener(() =>
            {
                if (selectedInventoryIndex >= 0 && selectedInventoryIndex < inventory.Count)
                    ShowInspection(inventory[selectedInventoryIndex]);
            });
            CreateText("档案页脚", archive.transform, "MEIXI LOCAL EVIDENCE ARCHIVE", 13,
                TextAnchor.MiddleRight, new Color(.43f, .65f, .61f), new Vector2(.43f, .055f), new Vector2(.955f, .115f));
            inventoryPanel.SetActive(false);

            inspectPanel = CreatePanel("道具全屏检查", canvas.transform, new Color(.001f, .004f, .005f, .94f),
                Vector2.zero, Vector2.one);
            PromoteModalCanvas(inspectPanel, 930);
            Image inspectionOverlay = inspectPanel.GetComponent<Image>();
            inspectionOverlay.sprite = CreateSoftHazeSprite("ChapterTwoInspectionOverlay", 640, 360);
            inspectionOverlay.color = new Color(.001f, .004f, .005f, .96f);
            inspectBlurBackground = CreateRawImage("检视背景虚化", inspectPanel.transform, Vector2.zero, Vector2.one);
            inspectBlurBackground.color = new Color(.54f, .62f, .60f, .42f);
            inspectBlurBackground.raycastTarget = false;
            GameObject inspectField = CreatePanel("物件检视台", inspectPanel.transform, new Color(.004f, .014f, .015f, .92f),
                new Vector2(.055f, .055f), new Vector2(.945f, .945f));
            Image inspectFieldHaze = inspectField.GetComponent<Image>();
            inspectFieldHaze.sprite = CreateSoftHazeSprite("ChapterTwoInspectFieldHaze", 760, 480);
            AddUiOutline(inspectField, new Color(.53f, .42f, .22f, .38f), 1.2f);
            AddUiShadow(inspectField, new Color(0f, 0f, 0f, .88f), new Vector2(0f, -16f));
            CreatePanel("检视顶部铜线", inspectField.transform, new Color(.86f, .57f, .25f, .62f),
                new Vector2(.045f, .965f), new Vector2(.84f, .967f));
            CreatePanel("检视信息分隔线", inspectField.transform, new Color(.20f, .52f, .48f, .34f),
                new Vector2(.705f, .17f), new Vector2(.7065f, .80f));
            CreateText("检查眉题", inspectField.transform, "OBJECT EXAMINATION  /  物证检视", 13,
                TextAnchor.MiddleLeft, new Color(.37f, .73f, .68f), new Vector2(.055f, .91f), new Vector2(.70f, .965f));
            inspectTitle = CreateText("检查道具标题", inspectField.transform, string.Empty, 43,
                TextAnchor.MiddleLeft, new Color(.95f, .76f, .39f), new Vector2(.055f, .81f), new Vector2(.69f, .91f));
            GameObject objectStage = CreatePanel("物件暗室", inspectField.transform, new Color(.001f, .008f, .009f, .86f),
                new Vector2(.045f, .11f), new Vector2(.685f, .79f));
            Image objectStageHaze = objectStage.GetComponent<Image>();
            objectStageHaze.sprite = CreateSoftHazeSprite("ChapterTwoObjectStageHaze", 560, 560);
            AddUiOutline(objectStage, new Color(.20f, .48f, .44f, .34f));
            CreatePanel("检视左上角线", objectStage.transform, new Color(.82f, .55f, .24f, .70f),
                new Vector2(.025f, .955f), new Vector2(.17f, .959f));
            CreatePanel("检视右下角线", objectStage.transform, new Color(.28f, .67f, .60f, .58f),
                new Vector2(.83f, .04f), new Vector2(.975f, .044f));
            inspectIcon = CreateImage("检查物件图像", objectStage.transform, new Vector2(.10f, .08f), new Vector2(.90f, .92f));
            inspectIcon.preserveAspect = true;
            inspectObjectViewport = CreateRawImage("检查物件三维视口", objectStage.transform,
                new Vector2(.04f, .04f), new Vector2(.96f, .96f));
            inspectObjectViewport.color = Color.white;
            inspectObjectViewport.raycastTarget = false;
            inspectPaperBackground = CreateRawImage("手写档案纸背景", inspectField.transform,
                new Vector2(.718f, .245f), new Vector2(.958f, .79f));
            inspectPaperBackground.texture = Resources.Load<Texture2D>("Art/MeixiDiaryPaperV1");
            inspectPaperBackground.color = new Color(.95f, .91f, .82f, .98f);
            inspectPaperBackground.raycastTarget = false;
            inspectPaperBackground.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -.8f);
            inspectBody = CreateText("检查道具正文", inspectField.transform, string.Empty, 21,
                TextAnchor.UpperLeft, new Color(.84f, .88f, .81f), new Vector2(.752f, .32f), new Vector2(.923f, .71f));
            inspectBody.lineSpacing = 1.30f;
            inspectDefaultFont = inspectBody.font;
            inspectHandwritingFont = Font.CreateDynamicFontFromOSFont(
                new[] { "FZKai-Z03", "KaiTi", "STKaiti", "SimKai", "Microsoft YaHei" }, 24);
            inspectPaperBackground.gameObject.SetActive(false);
            CreateText("检查道具提示", inspectField.transform, "OBJECT RECORD  /  M-17", 14,
                TextAnchor.MiddleRight, new Color(.42f, .66f, .62f), new Vector2(.73f, .15f), new Vector2(.94f, .23f));
            inspectCloseButton = CreateButton("退出全屏检视", inspectField.transform, "×",
                new Vector2(.925f, .90f), new Vector2(.97f, .965f));
            inspectCloseButton.onClick.AddListener(ReturnToInventoryFromInspection);
            inspectPanel.SetActive(false);

            BuildKeypadUi();
            BuildChaseUi();
            BuildDefeatUi();
            BuildFadeUi();
            RefreshInventory();
            inventoryHud.SetActive(false);
        }

        private void BuildKeypadUi()
        {
            keypadPanel = CreatePanel("六位密码锁界面", canvas.transform, new Color(.001f, .004f, .006f, .94f),
                Vector2.zero, Vector2.one);
            PromoteModalCanvas(keypadPanel, 940);
            GameObject device = CreatePanel("声纹锁机身", keypadPanel.transform, new Color(.018f, .035f, .033f, .995f),
                new Vector2(.325f, .055f), new Vector2(.675f, .945f));
            AddUiOutline(device, new Color(.68f, .48f, .20f, .84f), 3f);
            AddUiShadow(device, new Color(0f, 0f, 0f, .86f), new Vector2(0f, -12f));
            CreateText("门锁眉题", device.transform, "MECHANICAL RESONANCE LOCK", 12, TextAnchor.MiddleCenter,
                new Color(.37f, .68f, .63f), new Vector2(.10f, .925f), new Vector2(.90f, .975f));
            CreateText("密码锁标题", device.transform, "声纹门锁", 32, TextAnchor.MiddleCenter,
                new Color(.95f, .75f, .38f), new Vector2(.10f, .84f), new Vector2(.90f, .925f));
            keypadStatus = CreateText("门锁状态", device.transform, "LOCKED  /  六位机械锁", 12, TextAnchor.MiddleLeft,
                new Color(.46f, .72f, .65f), new Vector2(.12f, .785f), new Vector2(.72f, .835f));
            keypadDisplay = CreateText("密码位数", device.transform, "0 / 6", 12, TextAnchor.MiddleRight,
                new Color(.46f, .64f, .60f), new Vector2(.75f, .785f), new Vector2(.91f, .835f));
            for (int i = 0; i < 6; i++)
            {
                float left = .09f + i * .137f;
                GameObject cell = CreatePanel("密码窗_" + (i + 1), device.transform, new Color(.004f, .012f, .012f, 1f),
                    new Vector2(left, .665f), new Vector2(left + .112f, .765f));
                AddUiOutline(cell, new Color(.52f, .40f, .21f, .75f), 1.5f);
                keypadDigits[i] = CreateText("密码数字", cell.transform, "·", 32, TextAnchor.MiddleCenter,
                    new Color(.31f, .43f, .43f), Vector2.zero, Vector2.one);
            }
            for (int digit = 1; digit <= 9; digit++)
            {
                int value = digit;
                int row = (digit - 1) / 3;
                int col = (digit - 1) % 3;
                Button button = CreateButton("密码键_" + digit, device.transform, digit.ToString(),
                    new Vector2(.13f + col * .255f, .51f - row * .145f),
                    new Vector2(.35f + col * .255f, .625f - row * .145f));
                PolishKeyButton(button);
                RegisterKeypadPointerButton(button, () => AppendKeypad(value.ToString()));
            }
            Button zero = CreateButton("密码键_0", device.transform, "0", new Vector2(.39f, .075f), new Vector2(.61f, .185f));
            PolishKeyButton(zero);
            RegisterKeypadPointerButton(zero, () => AppendKeypad("0"));
            Button submit = CreateButton("密码确认", device.transform, "确认", new Vector2(.64f, .075f), new Vector2(.89f, .185f));
            PolishKeyButton(submit, true);
            RegisterKeypadPointerButton(submit, SubmitKeypad);
            Button clear = CreateButton("密码退格", device.transform, "←", new Vector2(.11f, .075f), new Vector2(.36f, .185f));
            PolishKeyButton(clear);
            RegisterKeypadPointerButton(clear, () =>
            {
                if (keypadValue.Length > 0) keypadValue = keypadValue.Substring(0, keypadValue.Length - 1);
                if (keypadStatus != null)
                {
                    keypadStatus.text = "LOCKED  /  六位机械锁";
                    keypadStatus.color = new Color(.46f, .72f, .65f);
                }
                RefreshKeypad();
            });
            Button close = CreateButton("关闭密码锁", device.transform, "×", new Vector2(.90f, .925f), new Vector2(.965f, .975f));
            RegisterKeypadPointerButton(close, CloseKeypad);
            keypadPanel.SetActive(false);
        }

        private void BuildChaseUi()
        {
            GameObject staminaRoot = CreatePanel("追逐体力条", canvas.transform, new Color(.003f, .012f, .018f, .90f),
                new Vector2(.73f, .16f), new Vector2(.96f, .23f));
            staminaPanel = staminaRoot;
            staminaText = CreateText("体力文字", staminaRoot.transform, "体 力 100%", 15, TextAnchor.MiddleLeft,
                new Color(.78f, .92f, .90f), new Vector2(.05f, .08f), new Vector2(.34f, .92f));
            GameObject track = CreatePanel("体力轨道", staminaRoot.transform, new Color(.08f, .12f, .13f, 1f),
                new Vector2(.36f, .30f), new Vector2(.95f, .70f));
            GameObject fill = CreatePanel("体力填充", track.transform, new Color(.28f, .91f, .76f, 1f),
                Vector2.zero, Vector2.one);
            staminaFill = fill.GetComponent<Image>();
            staminaFill.type = Image.Type.Filled;
            staminaFill.fillMethod = Image.FillMethod.Horizontal;
            staminaFill.fillOrigin = 0;
            staminaFill.fillAmount = 1f;
            staminaRoot.SetActive(false);
        }

        private void BuildDefeatUi()
        {
            defeatPanel = CreatePanel("实践终止界面", canvas.transform, new Color(.006f, .005f, .008f, .985f),
                Vector2.zero, Vector2.one);
            CreateText("实践终止标题", defeatPanel.transform, "实 践 被 迫 停 止", 52, TextAnchor.MiddleCenter,
                new Color(1f, .29f, .19f), new Vector2(.15f, .58f), new Vector2(.85f, .72f));
            CreateText("实践终止正文", defeatPanel.transform, "你在实践中受伤了。\n木偶的脚步停在了离你很近的地方。", 27,
                TextAnchor.MiddleCenter, new Color(.90f, .88f, .82f), new Vector2(.18f, .37f), new Vector2(.82f, .56f));
            defeatReturnText = CreateText("实践终止返回", defeatPanel.transform, "ENTER / ESC  返回存档点", 18,
                TextAnchor.MiddleCenter, new Color(.49f, .77f, .75f), new Vector2(.27f, .20f), new Vector2(.73f, .28f));
            defeatPanel.SetActive(false);
        }

        private void BuildFadeUi()
        {
            GameObject fade = CreatePanel("章节回流黑幕", canvas.transform, Color.black, Vector2.zero, Vector2.one);
            fadeGroup = fade.AddComponent<CanvasGroup>();
            fadeGroup.alpha = 0f;
            fadeGroup.blocksRaycasts = false;
            fadeGroup.interactable = false;
        }

        private Sprite CreateInventoryIcon(string id)
        {
            if (inventoryIconCache.TryGetValue(id, out Sprite cached) && cached != null) return cached;
            const int size = 160;
            Texture2D texture = new(size, size, TextureFormat.RGBA32, false)
            {
                name = "ChapterTwoIcon_" + id,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            Color clear = new(0f, 0f, 0f, 0f);
            Color[] pixels = new Color[size * size];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = clear;
            texture.SetPixels(pixels);

            Color brass = new(.72f, .48f, .20f, 1f);
            Color brassLight = new(.95f, .76f, .39f, 1f);
            Color teal = new(.20f, .48f, .45f, 1f);
            Color darkTeal = new(.055f, .16f, .16f, 1f);
            Color paper = new(.82f, .72f, .52f, 1f);
            Color ink = new(.19f, .16f, .12f, .94f);
            Color red = new(.53f, .12f, .08f, 1f);
            DrawCircle(texture, 80, 78, 63, new Color(.12f, .31f, .30f, .20f));

            switch (id)
            {
                case "score":
                    DrawRoundedRect(texture, 38, 24, 124, 139, 8, new Color(.22f, .14f, .08f, .72f));
                    DrawRoundedRect(texture, 32, 29, 118, 144, 8, paper);
                    FillRect(texture, 39, 40, 111, 45, new Color(.46f, .34f, .19f, .72f));
                    for (int y = 57; y <= 116; y += 14) DrawLine(texture, 42, y, 107, y, 2, new Color(.34f, .26f, .17f, .78f));
                    DrawLine(texture, 47, 55, 47, 120, 2, ink);
                    DrawLine(texture, 63, 55, 63, 120, 2, ink);
                    DrawLine(texture, 83, 55, 83, 120, 2, ink);
                    DrawLine(texture, 102, 55, 102, 120, 2, ink);
                    DrawCircle(texture, 96, 43, 10, red);
                    DrawRing(texture, 96, 43, 7, 2, new Color(.88f, .48f, .31f, 1f));
                    break;
                case "flashlight":
                    DrawLine(texture, 42, 55, 105, 105, 24, new Color(.16f, .12f, .08f, .80f));
                    DrawLine(texture, 45, 58, 108, 108, 18, brass);
                    DrawLine(texture, 54, 67, 98, 102, 5, brassLight);
                    DrawCircle(texture, 113, 112, 28, darkTeal);
                    DrawCircle(texture, 113, 112, 23, brass);
                    DrawCircle(texture, 113, 112, 16, new Color(.72f, .89f, .80f, 1f));
                    DrawRing(texture, 43, 55, 11, 4, teal);
                    break;
                case "chair":
                    FillRect(texture, 38, 32, 48, 132, brass);
                    FillRect(texture, 111, 32, 121, 132, brass);
                    DrawRoundedRect(texture, 43, 88, 116, 130, 8, new Color(.22f, .085f, .05f, 1f));
                    FillRect(texture, 48, 102, 111, 108, new Color(.56f, .25f, .12f, 1f));
                    DrawRoundedRect(texture, 51, 42, 108, 86, 7, new Color(.13f, .07f, .05f, 1f));
                    DrawRoundedRect(texture, 61, 52, 101, 80, 4, paper);
                    DrawLine(texture, 69, 61, 93, 72, 3, ink);
                    DrawCircle(texture, 80, 89, 7, brassLight);
                    break;
                case "wall":
                    DrawRoundedRect(texture, 27, 27, 133, 135, 10, new Color(.17f, .20f, .19f, 1f));
                    for (int i = 0; i < 8; i++)
                    {
                        int x = 39 + i * 11;
                        DrawLine(texture, x, 35 + (i % 3) * 4, x - 14, 128 - (i % 2) * 9, 3, new Color(.29f, .35f, .32f, .45f));
                    }
                    DrawLine(texture, 55, 50, 55, 112, 5, brassLight);
                    DrawLine(texture, 55, 83, 75, 52, 5, brassLight);
                    DrawLine(texture, 82, 54, 113, 54, 5, brassLight);
                    DrawLine(texture, 111, 55, 88, 84, 5, brassLight);
                    DrawLine(texture, 88, 84, 112, 84, 5, brassLight);
                    DrawLine(texture, 112, 84, 112, 114, 5, brassLight);
                    break;
                case "resonance":
                    DrawRing(texture, 80, 80, 55, 5, brass);
                    DrawRing(texture, 80, 80, 39, 4, teal);
                    DrawRing(texture, 80, 80, 22, 3, brassLight);
                    for (int i = 0; i < 4; i++)
                    {
                        float angle = Mathf.Deg2Rad * (45f + i * 90f);
                        int x = Mathf.RoundToInt(80 + Mathf.Cos(angle) * 47f);
                        int y = Mathf.RoundToInt(80 + Mathf.Sin(angle) * 47f);
                        DrawCircle(texture, x, y, 10, new Color(.24f, .12f, .07f, 1f));
                        DrawCircle(texture, x, y, 6, brassLight);
                    }
                    DrawCircle(texture, 80, 80, 8, red);
                    break;
                case "luban":
                    DrawRoundedRect(texture, 35, 67, 125, 93, 8, new Color(.22f, .08f, .04f, 1f));
                    FillRect(texture, 53, 36, 71, 124, brass);
                    FillRect(texture, 89, 38, 107, 122, paper);
                    FillRect(texture, 42, 70, 118, 89, teal);
                    DrawLine(texture, 58, 42, 66, 118, 2, new Color(.36f, .17f, .06f, .84f));
                    DrawLine(texture, 94, 44, 102, 116, 2, new Color(.52f, .36f, .18f, .72f));
                    DrawCircle(texture, 54, 70, 5, brassLight);
                    DrawCircle(texture, 106, 89, 5, brassLight);
                    DrawCircle(texture, 80, 80, 10, brassLight);
                    DrawRing(texture, 80, 80, 20, 3, new Color(.48f, .26f, .12f, 1f));
                    break;
                default:
                    DrawRing(texture, 80, 78, 47, 4, new Color(.30f, .43f, .40f, .70f));
                    DrawRoundedRect(texture, 57, 67, 103, 112, 7, new Color(.17f, .26f, .25f, .75f));
                    DrawRing(texture, 80, 68, 23, 6, new Color(.35f, .48f, .44f, .66f));
                    DrawCircle(texture, 80, 89, 6, new Color(.50f, .50f, .38f, .65f));
                    FillRect(texture, 77, 89, 83, 103, new Color(.50f, .50f, .38f, .65f));
                    break;
            }
            texture.Apply(false, false);
            Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(.5f, .5f), 100f);
            sprite.name = texture.name;
            inventoryIconCache[id] = sprite;
            return sprite;
        }

        private static void DrawRoundedRect(Texture2D texture, int x0, int y0, int x1, int y1, int radius, Color color)
        {
            FillRect(texture, x0 + radius, y0, x1 - radius, y1, color);
            FillRect(texture, x0, y0 + radius, x1, y1 - radius, color);
            DrawCircle(texture, x0 + radius, y0 + radius, radius, color);
            DrawCircle(texture, x1 - radius, y0 + radius, radius, color);
            DrawCircle(texture, x0 + radius, y1 - radius, radius, color);
            DrawCircle(texture, x1 - radius, y1 - radius, radius, color);
        }

        private static void FillRect(Texture2D texture, int x0, int y0, int x1, int y1, Color color)
        {
            for (int y = Mathf.Max(0, y0); y <= Mathf.Min(texture.height - 1, y1); y++)
            for (int x = Mathf.Max(0, x0); x <= Mathf.Min(texture.width - 1, x1); x++) BlendPixel(texture, x, y, color);
        }

        private static void DrawCircle(Texture2D texture, int cx, int cy, int radius, Color color)
        {
            int radiusSq = radius * radius;
            for (int y = -radius; y <= radius; y++)
            for (int x = -radius; x <= radius; x++)
                if (x * x + y * y <= radiusSq) BlendPixel(texture, cx + x, cy + y, color);
        }

        private static void DrawRing(Texture2D texture, int cx, int cy, int radius, int thickness, Color color)
        {
            int outer = radius * radius;
            int innerRadius = Mathf.Max(0, radius - thickness);
            int inner = innerRadius * innerRadius;
            for (int y = -radius; y <= radius; y++)
            for (int x = -radius; x <= radius; x++)
            {
                int d = x * x + y * y;
                if (d <= outer && d >= inner) BlendPixel(texture, cx + x, cy + y, color);
            }
        }

        private static void DrawLine(Texture2D texture, int x0, int y0, int x1, int y1, int thickness, Color color)
        {
            int steps = Mathf.Max(Mathf.Abs(x1 - x0), Mathf.Abs(y1 - y0));
            if (steps == 0) { DrawCircle(texture, x0, y0, thickness, color); return; }
            for (int i = 0; i <= steps; i++)
            {
                float t = i / (float)steps;
                DrawCircle(texture, Mathf.RoundToInt(Mathf.Lerp(x0, x1, t)),
                    Mathf.RoundToInt(Mathf.Lerp(y0, y1, t)), thickness, color);
            }
        }

        private static void BlendPixel(Texture2D texture, int x, int y, Color color)
        {
            if (x < 0 || y < 0 || x >= texture.width || y >= texture.height) return;
            Color under = texture.GetPixel(x, y);
            float alpha = color.a + under.a * (1f - color.a);
            if (alpha <= .0001f) { texture.SetPixel(x, y, Color.clear); return; }
            Color blended = (color * color.a + under * under.a * (1f - color.a)) / alpha;
            blended.a = alpha;
            texture.SetPixel(x, y, blended);
        }

        private static Image CreateImage(string name, Transform parent, Vector2 min, Vector2 max)
        {
            GameObject value = new(name);
            value.transform.SetParent(parent, false);
            Image image = value.AddComponent<Image>();
            image.color = Color.white;
            SetRect(image.rectTransform, min, max);
            return image;
        }

        private static RawImage CreateRawImage(string name, Transform parent, Vector2 min, Vector2 max)
        {
            GameObject value = new(name);
            value.transform.SetParent(parent, false);
            RawImage image = value.AddComponent<RawImage>();
            image.color = Color.white;
            SetRect(image.rectTransform, min, max);
            return image;
        }

        private static void SetLayerRecursively(GameObject target, int layer)
        {
            if (target == null) return;
            target.layer = layer;
            foreach (Transform child in target.transform) SetLayerRecursively(child.gameObject, layer);
        }

        private static void SetRect(RectTransform rect, Vector2 min, Vector2 max)
        {
            if (rect == null) return;
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void ApplyResourceTexture(Image image, string resourcePath)
        {
            if (image == null) return;
            Texture2D texture = Resources.Load<Texture2D>(resourcePath);
            if (texture == null) return;
            image.sprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(.5f, .5f), 100f);
            image.color = Color.white;
            image.preserveAspect = false;
        }

        private static void AddUiOutline(GameObject target, Color color, float distance = 1f)
        {
            Outline outline = target.GetComponent<Outline>() ?? target.AddComponent<Outline>();
            outline.effectColor = color;
            outline.effectDistance = new Vector2(distance, -distance);
            outline.useGraphicAlpha = true;
        }

        private static void AddUiShadow(GameObject target, Color color, Vector2 distance)
        {
            Shadow shadow = target.AddComponent<Shadow>();
            shadow.effectColor = color;
            shadow.effectDistance = distance;
            shadow.useGraphicAlpha = true;
        }

        private static void PromoteModalCanvas(GameObject panel, int sortingOrder)
        {
            if (panel == null) return;
            Canvas modalCanvas = panel.GetComponent<Canvas>() ?? panel.AddComponent<Canvas>();
            modalCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            modalCanvas.overrideSorting = true;
            modalCanvas.sortingOrder = sortingOrder;
            if (panel.GetComponent<GraphicRaycaster>() == null) panel.AddComponent<GraphicRaycaster>();
        }

        private static void EnsureAdventureEventSystem()
        {
            EventSystem eventSystem = EventSystem.current ?? FindAnyObjectByType<EventSystem>();
            if (eventSystem == null)
            {
                GameObject eventSystemObject = new("第二章界面事件系统");
                eventSystem = eventSystemObject.AddComponent<EventSystem>();
            }
            InputSystemUIInputModule uiInput = eventSystem.GetComponent<InputSystemUIInputModule>();
            bool createdInputModule = uiInput == null;
            if (createdInputModule) uiInput = eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();
            uiInput.enabled = true;
            if (createdInputModule || uiInput.actionsAsset == null) uiInput.AssignDefaultActions();
            eventSystem.sendNavigationEvents = true;
            foreach (BaseInputModule module in eventSystem.GetComponents<BaseInputModule>())
                if (module != uiInput) module.enabled = false;
        }

        private static void PolishKeyButton(Button button, bool confirm = false)
        {
            if (button == null) return;
            Image image = button.targetGraphic as Image;
            if (image != null) image.color = confirm ? new Color(.37f, .24f, .08f, 1f) : new Color(.025f, .09f, .085f, 1f);
            AddUiOutline(button.gameObject, confirm ? new Color(.95f, .65f, .24f, .86f) : new Color(.34f, .60f, .53f, .64f), 1.2f);
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = confirm ? new Color(1f, .82f, .48f) : new Color(.64f, .91f, .82f);
            colors.pressedColor = new Color(.92f, .57f, .20f);
            colors.selectedColor = colors.highlightedColor;
            button.colors = colors;
        }

        private Text FindText(string objectName)
        {
            foreach (Text text in FindObjectsByType<Text>(FindObjectsInactive.Include))
                if (text.name == objectName) return text;
            return null;
        }

        private static Sprite CreateSoftHazeSprite(string name, int width, int height)
        {
            Texture2D texture = new(width, height, TextureFormat.RGBA32, false)
            {
                name = name + "Texture",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            Color[] pixels = new Color[width * height];
            for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                float nx = (x + .5f) / width * 2f - 1f;
                float ny = (y + .5f) / height * 2f - 1f;
                float distance = Mathf.Sqrt(nx * nx + ny * ny);
                float alpha = Mathf.SmoothStep(1f, 0f, Mathf.Clamp01(distance));
                alpha *= alpha * .92f;
                pixels[y * width + x] = new Color(1f, 1f, 1f, alpha);
            }
            texture.SetPixels(pixels);
            texture.Apply(false, true);
            Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, width, height), new Vector2(.5f, .5f), 100f);
            sprite.name = name;
            return sprite;
        }

        private static GameObject CreatePanel(string name, Transform parent, Color color, Vector2 min, Vector2 max)
        {
            GameObject value = new(name);
            value.transform.SetParent(parent, false);
            Image image = value.AddComponent<Image>();
            image.color = color;
            RectTransform rect = image.rectTransform;
            rect.anchorMin = min; rect.anchorMax = max; rect.offsetMin = Vector2.zero; rect.offsetMax = Vector2.zero;
            return value;
        }

        private static Text CreateText(string name, Transform parent, string value, int size,
            TextAnchor anchor, Color color, Vector2 min, Vector2 max)
        {
            GameObject textObject = new(name);
            textObject.transform.SetParent(parent, false);
            Text text = textObject.AddComponent<Text>();
            text.text = value; text.fontSize = size; text.alignment = anchor; text.color = color;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            RectTransform rect = text.rectTransform;
            rect.anchorMin = min; rect.anchorMax = max; rect.offsetMin = Vector2.zero; rect.offsetMax = Vector2.zero;
            return text;
        }

        private static Button CreateButton(string name, Transform parent, string label, Vector2 min, Vector2 max)
        {
            GameObject root = CreatePanel(name, parent, new Color(.055f, .18f, .20f, .98f), min, max);
            Button button = root.AddComponent<Button>();
            button.targetGraphic = root.GetComponent<Image>();
            ColorBlock colors = button.colors;
            colors.highlightedColor = new Color(.12f, .36f, .36f);
            colors.pressedColor = new Color(.55f, .31f, .10f);
            button.colors = colors;
            Text buttonLabel = CreateText("标签", root.transform, label, 22, TextAnchor.MiddleCenter, Color.white, Vector2.zero, Vector2.one);
            buttonLabel.raycastTarget = false;
            return button;
        }

        private enum SurfaceKind { Wood, Stone, Mirror, Paper }

        private static void ApplySurfaceTexture(Material material, SurfaceKind kind, Color baseColor)
        {
            if (material == null) return;
            const int size = 48;
            Texture2D texture = new(size, size, TextureFormat.RGBA32, false)
            {
                name = material.name + "_ProceduralSurface",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Repeat
            };
            Color[] pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float wave = Mathf.Sin((x + y * .43f) * .52f) * .5f + .5f;
                float fine = Mathf.Sin(x * 2.71f + y * 1.83f) * Mathf.Sin(y * 1.17f + x * .37f) * .5f + .5f;
                float value = kind == SurfaceKind.Mirror ? Mathf.Lerp(.92f, 1.05f, wave) : Mathf.Lerp(.82f, 1.13f, fine);
                Color color = baseColor * value;
                if (kind == SurfaceKind.Wood)
                {
                    float grain = Mathf.Sin(y * .64f + Mathf.Sin(x * .18f) * 2.2f) * .5f + .5f;
                    color = Color.Lerp(color, baseColor * 1.18f, grain * .22f);
                }
                else if (kind == SurfaceKind.Stone)
                {
                    float fleck = Mathf.SmoothStep(.35f, .80f, fine);
                    color = Color.Lerp(color, baseColor * .62f, fleck * .18f);
                }
                else if (kind == SurfaceKind.Mirror)
                {
                    float verticalTint = Mathf.Sin(x * .24f) * .5f + .5f;
                    color = Color.Lerp(color, new Color(.20f, .34f, .35f), verticalTint * .07f);
                }
                pixels[y * size + x] = new Color(color.r, color.g, color.b, 1f);
            }
            texture.SetPixels(pixels);
            texture.Apply(false, true);
            if (material.HasProperty("_BaseMap")) material.SetTexture("_BaseMap", texture);
            if (material.HasProperty("_MainTex")) material.SetTexture("_MainTex", texture);
            if (material.HasProperty("_Glossiness")) material.SetFloat("_Glossiness", material.HasProperty("_Smoothness") ? material.GetFloat("_Smoothness") : .35f);
        }

        private static Material MakeMaterial(string name, Color color, float smoothness, float metallic)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            Material material = new(shader) { name = name, color = color };
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", smoothness);
            if (material.HasProperty("_Glossiness")) material.SetFloat("_Glossiness", smoothness);
            if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", metallic);
            material.enableInstancing = true;
            return material;
        }

        private static Material MakeEmissionMaterial(string name, Color color, float intensity)
        {
            Material material = MakeMaterial(name, color, .42f, .12f);
            if (material.HasProperty("_EmissionColor"))
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", color * intensity);
            }
            return material;
        }

        private static void SetSubtleEmission(Material material, Color emission)
        {
            if (material == null || !material.HasProperty("_EmissionColor")) return;
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", emission);
        }

        private static void ConfigureSmokedMirror(Material material)
        {
            if (material == null) return;
            if (material.HasProperty("_Surface")) material.SetFloat("_Surface", 1f);
            if (material.HasProperty("_Blend")) material.SetFloat("_Blend", 0f);
            if (material.HasProperty("_SrcBlend")) material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            if (material.HasProperty("_DstBlend")) material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            if (material.HasProperty("_ZWrite")) material.SetFloat("_ZWrite", 0f);
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            material.renderQueue = 3000;
        }

        private static GameObject Box(string name, Transform parent, Vector3 position, Vector3 scale,
            Material material, bool collider) => Primitive(PrimitiveType.Cube, name, parent, position, scale, material, collider);

        private static GameObject Cylinder(string name, Transform parent, Vector3 position, Vector3 scale,
            Material material, bool collider) => Primitive(PrimitiveType.Cylinder, name, parent, position, scale, material, collider);

        private static GameObject Capsule(string name, Transform parent, Vector3 position, Vector3 scale,
            Material material, bool collider) => Primitive(PrimitiveType.Capsule, name, parent, position, scale, material, collider);

        private static GameObject Sphere(string name, Transform parent, Vector3 position, Vector3 scale,
            Material material, bool collider) => Primitive(PrimitiveType.Sphere, name, parent, position, scale, material, collider);

        private static GameObject Primitive(PrimitiveType type, string name, Transform parent, Vector3 position,
            Vector3 scale, Material material, bool collider)
        {
            GameObject value = GameObject.CreatePrimitive(type);
            value.name = name;
            value.transform.SetParent(parent, false);
            value.transform.localPosition = position;
            value.transform.localScale = scale;
            Renderer renderer = value.GetComponent<Renderer>();
            renderer.sharedMaterial = material;
            if (!collider || name.StartsWith("镜墙") || name.StartsWith("迷宫外壳") || name.StartsWith("回旋楼梯踏步"))
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            if (!collider) Destroy(value.GetComponent<Collider>());
            return value;
        }

        private static TextMesh AddFloatingLabel(Transform parent, string value, Vector3 position,
            Vector3 eulerAngles, int size, Color color)
        {
            GameObject labelObject = new("线索铭文");
            Transform stableParent = parent.parent != null ? parent.parent : parent;
            labelObject.transform.SetParent(stableParent, true);
            // Primitive transforms carry non-uniform scale. Keeping text outside
            // that hierarchy prevents thin walls and lock plates distorting glyphs.
            labelObject.transform.position = parent.position + parent.rotation * position;
            labelObject.transform.rotation = parent.rotation * Quaternion.Euler(eulerAngles);
            labelObject.transform.localScale = Vector3.one;
            TextMesh text = labelObject.AddComponent<TextMesh>();
            text.text = value;
            text.fontSize = size;
            text.characterSize = .045f;
            text.anchor = TextAnchor.MiddleCenter;
            text.alignment = TextAlignment.Center;
            text.color = color;
            Font font = Font.CreateDynamicFontFromOSFont(
                new[] { "FZKai-Z03", "KaiTi", "STKaiti", "SimKai", "Microsoft YaHei", "Arial" }, size);
            if (font != null) text.font = font;
            MeshRenderer textRenderer = text.GetComponent<MeshRenderer>();
            Shader depthShader = Shader.Find("MeixiEcho/World Text Depth");
            if (textRenderer != null && depthShader != null)
            {
                Material material = new(depthShader) { name = "ChapterTwo_DepthText_" + value };
                Texture atlas = text.font != null ? text.font.material.mainTexture : null;
                if (atlas != null) material.SetTexture("_MainTex", atlas);
                material.SetColor("_Color", Color.white);
                material.renderQueue = 2995;
                textRenderer.sharedMaterial = material;
                textRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                textRenderer.receiveShadows = false;
            }
            return text;
        }

        private sealed class MazeGraph
        {
            private static readonly Vector2Int[] Neighbors =
            {
                new(1, 0), new(0, 1), new(-1, 0), new(0, -1)
            };
            public readonly bool[,] blocked;
            public readonly float cellSize;
            public readonly float floorY;
            public readonly float startZ;
            public Vector3 EntryPoint => new(0f, floorY + .35f, startZ);

            public MazeGraph(bool[,] blockedValue, float cellValue, float floorValue, float startValue)
            {
                blocked = blockedValue; cellSize = cellValue; floorY = floorValue; startZ = startValue;
            }

            public bool CanEnterPathFrom(Vector3 position)
            {
                // Include the short threshold corridor in the maze handoff. The
                // puppet should switch to the grid path before it reaches the
                // north-cell center, otherwise it can stop on the stair landing.
                return position.y < floorY + 2.80f && position.z < startZ + 1.25f;
            }

            public Vector3 ClampWaypoint(Vector3 current, Vector3 playerPosition)
            {
                if (playerPosition.y > floorY + 2f) return playerPosition;
                int col = Mathf.Clamp(Mathf.RoundToInt(playerPosition.x / cellSize) + 5, 0, 10);
                int row = Mathf.Clamp(Mathf.RoundToInt((startZ - playerPosition.z) / cellSize), 0, 10);
                if (!blocked[col, row]) return new Vector3((col - 5) * cellSize, floorY + .35f, startZ - row * cellSize);
                return playerPosition;
            }

            public void BuildPath(Vector3 from, Vector3 to, bool alternate, List<Vector3> result,
                List<Vector3> preferred = null)
            {
                result.Clear();
                if (to.y > floorY + 2f) return;
                bool approachingEntrance = from.z > startZ + .08f;
                if (approachingEntrance)
                    result.Add(new Vector3(0f, floorY + .35f, startZ + .68f));
                Vector2Int start = FindNearestOpen(WorldToCell(from));
                Vector2Int goal = FindNearestOpen(WorldToCell(to));
                if (start == goal)
                {
                    result.Add(new Vector3(to.x, floorY + .35f, to.z));
                    return;
                }

                int[] distance = ComputeDistanceToGoal(goal);
                if (distance[CellIndex(start)] < 0)
                {
                    result.Add(new Vector3(to.x, floorY + .35f, to.z));
                    return;
                }
                HashSet<int> preferredCells = new();
                if (alternate && preferred != null)
                {
                    foreach (Vector3 point in preferred)
                    {
                        Vector2Int cell = WorldToCell(point);
                        if (IsOpen(cell)) preferredCells.Add(CellIndex(cell));
                    }
                }

                Vector2Int current = start;
                result.Add(CellToWorld(current));
                int guard = 0;
                while (current != goal && guard++ < 160)
                {
                    int currentDistance = distance[CellIndex(current)];
                    bool hasNearbyUnusedBranch = false;
                    if (alternate)
                    {
                        for (int i = 0; i < Neighbors.Length; i++)
                        {
                            Vector2Int candidate = current + Neighbors[i];
                            if (!IsOpen(candidate) || preferredCells.Contains(CellIndex(candidate))) continue;
                            int candidateDistance = distance[CellIndex(candidate)];
                            if (candidateDistance >= 0 && candidateDistance <= currentDistance + 4)
                            {
                                hasNearbyUnusedBranch = true;
                                break;
                            }
                        }
                    }
                    Vector2Int best = current;
                    float bestScore = float.PositiveInfinity;
                    for (int i = 0; i < Neighbors.Length; i++)
                    {
                        Vector2Int candidate = current + Neighbors[i];
                        if (!IsOpen(candidate)) continue;
                        int candidateDistance = distance[CellIndex(candidate)];
                        if (candidateDistance < 0) continue;
                        float score = candidateDistance;
                        if (alternate && preferredCells.Contains(CellIndex(candidate)) && candidate != goal &&
                            hasNearbyUnusedBranch)
                            score += 8f;
                        // Stable tie-breaking keeps both agents deterministic and
                        // avoids frame-to-frame route oscillation near junctions.
                        score += i * .001f;
                        if (score < bestScore)
                        {
                            best = candidate;
                            bestScore = score;
                        }
                    }
                    if (best == current) break;
                    current = best;
                    result.Add(CellToWorld(current));
                }

                if (current != goal)
                {
                    result.Clear();
                    BuildShortestPath(start, goal, result);
                }
                if (result.Count > 0)
                    result[result.Count - 1] = new Vector3(to.x, floorY + .35f, to.z);
                else
                    result.Add(new Vector3(to.x, floorY + .35f, to.z));
            }

            private void BuildShortestPath(Vector2Int start, Vector2Int goal, List<Vector3> result)
            {
                int[] distance = ComputeDistanceToGoal(goal);
                Vector2Int current = start;
                result.Add(CellToWorld(current));
                int guard = 0;
                while (current != goal && guard++ < 160)
                {
                    Vector2Int best = current;
                    int bestDistance = int.MaxValue;
                    for (int i = 0; i < Neighbors.Length; i++)
                    {
                        Vector2Int candidate = current + Neighbors[i];
                        if (!IsOpen(candidate)) continue;
                        int value = distance[CellIndex(candidate)];
                        if (value >= 0 && value < bestDistance)
                        {
                            best = candidate;
                            bestDistance = value;
                        }
                    }
                    if (best == current) break;
                    current = best;
                    result.Add(CellToWorld(current));
                }
            }

            private int[] ComputeDistanceToGoal(Vector2Int goal)
            {
                int[] distance = new int[121];
                for (int i = 0; i < distance.Length; i++) distance[i] = -1;
                Vector2Int[] queue = new Vector2Int[121];
                int head = 0;
                int tail = 0;
                distance[CellIndex(goal)] = 0;
                queue[tail++] = goal;
                while (head < tail)
                {
                    Vector2Int current = queue[head++];
                    int nextDistance = distance[CellIndex(current)] + 1;
                    for (int i = 0; i < Neighbors.Length; i++)
                    {
                        Vector2Int candidate = current + Neighbors[i];
                        if (!IsOpen(candidate)) continue;
                        int index = CellIndex(candidate);
                        if (distance[index] >= 0) continue;
                        distance[index] = nextDistance;
                        queue[tail++] = candidate;
                    }
                }
                return distance;
            }

            private Vector2Int WorldToCell(Vector3 world)
            {
                return new Vector2Int(
                    Mathf.Clamp(Mathf.RoundToInt(world.x / cellSize) + 5, 0, 10),
                    Mathf.Clamp(Mathf.RoundToInt((startZ - world.z) / cellSize), 0, 10));
            }

            private Vector2Int FindNearestOpen(Vector2Int cell)
            {
                if (IsOpen(cell)) return cell;
                for (int radius = 1; radius <= 10; radius++)
                for (int y = -radius; y <= radius; y++)
                for (int x = -radius; x <= radius; x++)
                {
                    if (Mathf.Abs(x) != radius && Mathf.Abs(y) != radius) continue;
                    Vector2Int candidate = cell + new Vector2Int(x, y);
                    if (IsOpen(candidate)) return candidate;
                }
                return new Vector2Int(5, 0);
            }

            private bool IsOpen(Vector2Int cell)
            {
                return cell.x >= 0 && cell.x <= 10 && cell.y >= 0 && cell.y <= 10 && !blocked[cell.x, cell.y];
            }

            private int CellIndex(Vector2Int cell) => cell.y * 11 + cell.x;

            private Vector3 CellToWorld(Vector2Int cell)
            {
                return new Vector3((cell.x - 5) * cellSize, floorY + .35f, startZ - cell.y * cellSize);
            }
        }

        private sealed class CeilingPuppetEasterEgg
        {
            public Transform root;
            private Transform head;

            public static CeilingPuppetEasterEgg Create(string name, Transform parent, Vector3 localPosition)
            {
                CeilingPuppetEasterEgg puppet = new();
                GameObject rootObject = new(name);
                puppet.root = rootObject.transform;
                puppet.root.SetParent(parent, false);
                puppet.root.localPosition = localPosition;
                puppet.root.localRotation = Quaternion.identity;

                Material darkWood = MakeMaterial(name + "_DarkWood", new Color(.028f, .018f, .018f), .30f, .06f);
                Material oldWood = MakeMaterial(name + "_OldWood", new Color(.17f, .105f, .070f), .22f, .02f);
                Material cloth = MakeMaterial(name + "_Cloth", new Color(.020f, .028f, .030f), .12f, 0f);
                Material joint = MakeMaterial(name + "_Joint", new Color(.010f, .014f, .015f), .58f, .48f);
                Material eye = MakeEmissionMaterial(name + "_RedEyes", new Color(.95f, .008f, .004f), 3.8f);
                ApplySurfaceTexture(oldWood, SurfaceKind.Wood, new Color(.17f, .105f, .070f));
                ApplySurfaceTexture(darkWood, SurfaceKind.Wood, new Color(.028f, .018f, .018f));

                Box("CeilingPuppetSeat", puppet.root, new Vector3(0f, .04f, -.02f),
                    new Vector3(1.02f, .12f, .72f), darkWood, false);
                Box("CeilingPuppetBackrest", puppet.root, new Vector3(0f, .42f, .25f),
                    new Vector3(.78f, .78f, .10f), darkWood, false);
                for (int side = -1; side <= 1; side += 2)
                    Box("CeilingPuppetSeatPost", puppet.root, new Vector3(side * .39f, .24f, -.08f),
                        new Vector3(.08f, .42f, .08f), oldWood, false);

                Sphere("CeilingPuppetPelvis", puppet.root, new Vector3(0f, .68f, .01f),
                    new Vector3(.26f, .18f, .20f), cloth, false);
                Box("CeilingPuppetChest", puppet.root, new Vector3(0f, .96f, .02f),
                    new Vector3(.36f, .48f, .24f), oldWood, false);
                Box("CeilingPuppetChestCloth", puppet.root, new Vector3(0f, .96f, -.13f),
                    new Vector3(.30f, .40f, .025f), cloth, false);

                Transform leftThigh = new GameObject("CeilingPuppetLeftThigh").transform;
                leftThigh.SetParent(puppet.root, false);
                leftThigh.localPosition = new Vector3(-.15f, .68f, -.30f);
                leftThigh.localRotation = Quaternion.Euler(72f, 0f, 0f);
                Capsule("CeilingPuppetLeftThighWood", leftThigh, Vector3.zero,
                    new Vector3(.105f, .34f, .105f), oldWood, false);
                Transform rightThigh = new GameObject("CeilingPuppetRightThigh").transform;
                rightThigh.SetParent(puppet.root, false);
                rightThigh.localPosition = new Vector3(.15f, .68f, -.30f);
                rightThigh.localRotation = Quaternion.Euler(72f, 0f, 0f);
                Capsule("CeilingPuppetRightThighWood", rightThigh, Vector3.zero,
                    new Vector3(.105f, .34f, .105f), oldWood, false);
                Box("CeilingPuppetLeftShin", puppet.root, new Vector3(-.16f, .38f, -.54f),
                    new Vector3(.11f, .44f, .11f), oldWood, false).transform.localRotation = Quaternion.Euler(-12f, 0f, 0f);
                Box("CeilingPuppetRightShin", puppet.root, new Vector3(.16f, .38f, -.54f),
                    new Vector3(.11f, .44f, .11f), oldWood, false).transform.localRotation = Quaternion.Euler(-12f, 0f, 0f);
                Box("CeilingPuppetLeftFoot", puppet.root, new Vector3(-.16f, .17f, -.69f),
                    new Vector3(.16f, .10f, .34f), darkWood, false);
                Box("CeilingPuppetRightFoot", puppet.root, new Vector3(.16f, .17f, -.69f),
                    new Vector3(.16f, .10f, .34f), darkWood, false);

                puppet.head = new GameObject("CeilingPuppetHead").transform;
                puppet.head.SetParent(puppet.root, false);
                puppet.head.localPosition = new Vector3(0f, 1.36f, .01f);
                Sphere("CeilingPuppetBackOfHead", puppet.head, new Vector3(0f, 0f, -.10f),
                    new Vector3(.28f, .34f, .24f), darkWood, false);
                Sphere("CeilingPuppetFace", puppet.head, new Vector3(0f, 0f, .12f),
                    new Vector3(.25f, .30f, .16f), oldWood, false);
                Box("CeilingPuppetFaceCloth", puppet.head, new Vector3(0f, -.02f, .20f),
                    new Vector3(.22f, .25f, .025f), cloth, false);
                Sphere("CeilingPuppetEyeLeft", puppet.head, new Vector3(-.075f, .035f, .245f),
                    new Vector3(.026f, .030f, .016f), eye, false);
                Sphere("CeilingPuppetEyeRight", puppet.head, new Vector3(.075f, .035f, .245f),
                    new Vector3(.026f, .030f, .016f), eye, false);
                Box("CeilingPuppetMouth", puppet.head, new Vector3(0f, -.11f, .245f),
                    new Vector3(.10f, .014f, .012f), joint, false);
                AddCeilingEyeLight(puppet.head, new Vector3(-.075f, .035f, .29f));
                AddCeilingEyeLight(puppet.head, new Vector3(.075f, .035f, .29f));

                BuildCeilingPuppetArm(puppet.root, -1f, oldWood, cloth, joint);
                BuildCeilingPuppetArm(puppet.root, 1f, oldWood, cloth, joint);
                return puppet;
            }

            public void UpdateLookAt(Transform target, float deltaTime)
            {
                if (target == null || head == null || root == null) return;
                Vector3 targetLocal = root.InverseTransformPoint(target.position + Vector3.up * 1.08f);
                Vector3 toTarget = targetLocal - head.localPosition;
                float horizontal = Mathf.Max(.02f, new Vector2(toTarget.x, toTarget.z).magnitude);
                float yaw = Mathf.Clamp(Mathf.Atan2(toTarget.x, toTarget.z) * Mathf.Rad2Deg, -68f, 68f);
                float pitch = Mathf.Clamp(-Mathf.Atan2(toTarget.y, horizontal) * Mathf.Rad2Deg, -30f, 24f);
                Quaternion desired = Quaternion.Euler(pitch, yaw, 0f);
                float blend = 1f - Mathf.Exp(-9.5f * Mathf.Max(0f, deltaTime));
                head.localRotation = Quaternion.Slerp(head.localRotation, desired, blend);
            }

            private static void BuildCeilingPuppetArm(Transform parent, float side,
                Material oldWood, Material cloth, Material joint)
            {
                Transform shoulder = new GameObject("CeilingPuppetShoulder").transform;
                shoulder.SetParent(parent, false);
                shoulder.localPosition = new Vector3(side * .27f, 1.12f, .01f);
                Sphere("CeilingPuppetShoulderJoint", shoulder, Vector3.zero,
                    new Vector3(.11f, .11f, .11f), joint, false);
                Capsule("CeilingPuppetUpperArm", shoulder, new Vector3(0f, -.22f, 0f),
                    new Vector3(.08f, .25f, .08f), oldWood, false);
                Cylinder("CeilingPuppetSleeve", shoulder, new Vector3(0f, -.41f, -.01f),
                    new Vector3(.10f, .07f, .10f), cloth, false);
                Sphere("CeilingPuppetHand", shoulder, new Vector3(0f, -.52f, -.04f),
                    new Vector3(.09f, .12f, .07f), oldWood, false);
            }

            private static void AddCeilingEyeLight(Transform parent, Vector3 position)
            {
                GameObject eyeLight = new("CeilingPuppetEyeLight");
                eyeLight.transform.SetParent(parent, false);
                eyeLight.transform.localPosition = position;
                Light light = eyeLight.AddComponent<Light>();
                light.type = LightType.Point;
                light.color = new Color(1f, .012f, .006f);
                light.intensity = .22f;
                light.range = .52f;
                light.shadows = LightShadows.None;
            }
        }

        private sealed class PuppetAgent
        {
            public Transform root;
            private Transform torso;
            private Transform head;
            private Transform leftShoulder;
            private Transform rightShoulder;
            private Transform leftElbow;
            private Transform rightElbow;
            private Transform leftWrist;
            private Transform rightWrist;
            private Transform leftHip;
            private Transform rightHip;
            private Transform leftKnee;
            private Transform rightKnee;
            private Transform leftAnkle;
            private Transform rightAnkle;
            private AudioSource source;
            private Vector3 smoothedDirection = Vector3.forward;
            private float travelled;
            private float currentSpeed;
            private float speed;
            private float poseOffset;
            private int trailIndex;
            private List<Vector3> activeTrail;
            private int activeTrailCount = -1;
            private Vector3 activeTrailEnd;
            private float watchedGrace;
            private readonly RaycastHit[] motionHits = new RaycastHit[10];

            public static PuppetAgent Create(string name, Vector3 position, Color robeColor)
            {
                PuppetAgent agent = new();
                GameObject rootObject = new(name);
                agent.root = rootObject.transform;
                agent.root.position = position;
                agent.root.rotation = Quaternion.Euler(0f, 180f, 0f);
                Material paleWood = MakeMaterial(name + "_旧木", new Color(.235f, .185f, .145f), .20f, .02f);
                Material darkWood = MakeMaterial(name + "_焦木", new Color(.052f, .033f, .029f), .25f, .04f);
                Material joint = MakeMaterial(name + "_关节铁", new Color(.025f, .035f, .037f), .54f, .58f);
                Material wrap = MakeMaterial(name + "_旧布", Color.Lerp(robeColor, new Color(.025f, .038f, .041f), .48f), .09f, 0f);
                Material mask = MakeMaterial(name + "_面具", new Color(.385f, .335f, .275f), .15f, .01f);
                Material eye = MakeEmissionMaterial(name + "_眼", new Color(.80f, .010f, .006f), 2.7f);
                AddSubtlePuppetEmission(paleWood, new Color(.225f, .120f, .055f));
                AddSubtlePuppetEmission(mask, new Color(.205f, .142f, .078f));
                AddSubtlePuppetEmission(wrap, Color.Lerp(robeColor, new Color(.055f, .035f, .030f), .42f) * .62f);
                ApplySurfaceTexture(paleWood, SurfaceKind.Wood, new Color(.235f, .185f, .145f));
                ApplySurfaceTexture(darkWood, SurfaceKind.Wood, new Color(.075f, .040f, .031f));
                ApplySurfaceTexture(mask, SurfaceKind.Wood, new Color(.385f, .335f, .275f));

                Sphere("木偶骨盆", agent.root, new Vector3(0f, .78f, 0f), new Vector3(.29f, .20f, .22f), wrap, false);
                Box("木偶骨盆腹板", agent.root, new Vector3(0f, .80f, .16f), new Vector3(.24f, .18f, .045f), paleWood, false);
                Sphere("木偶腰关节", agent.root, new Vector3(0f, .94f, 0f), new Vector3(.17f, .15f, .15f), joint, false);
                agent.torso = CreatePuppetPivot(agent.root, "木偶胸腔骨", new Vector3(0f, 1.21f, 0f));
                CreateTaperedPuppetBody("木偶窄胸板", agent.torso, paleWood);
                Box("木偶胸腔裂隙", agent.torso, new Vector3(-.018f, .07f, .184f), new Vector3(.025f, .31f, .012f), darkWood, false)
                    .transform.localRotation = Quaternion.Euler(0f, 0f, -4f);
                GameObject clavicleWrap = Capsule("木偶肩披", agent.torso, new Vector3(0f, .30f, -.015f),
                    new Vector3(.13f, .265f, .14f), wrap, false);
                clavicleWrap.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
                Sphere("木偶腹带", agent.torso, new Vector3(0f, -.245f, .01f), new Vector3(.23f, .085f, .20f), wrap, false);
                for (int ring = 0; ring < 3; ring++)
                    Cylinder("木偶颈环_" + ring, agent.root, new Vector3(0f, 1.56f + ring * .065f, 0f),
                        new Vector3(.13f - ring * .012f, .035f, .13f - ring * .012f), ring == 1 ? wrap : joint, false);
                Sphere("木偶颈部旧布", agent.root, new Vector3(0f, 1.69f, -.015f),
                    new Vector3(.19f, .19f, .17f), wrap, false);

                agent.head = CreatePuppetPivot(agent.root, "木偶头骨", new Vector3(0f, 1.93f, 0f));
                Sphere("木偶后颅", agent.head, new Vector3(0f, .005f, -.045f), new Vector3(.255f, .350f, .235f), darkWood, false);
                Sphere("木偶旧面具", agent.head, new Vector3(-.024f, .004f, .154f), new Vector3(.225f, .310f, .090f), mask, false);
                Sphere("木偶面具黑半", agent.head, new Vector3(.116f, .010f, .194f), new Vector3(.125f, .302f, .075f), darkWood, false);
                Box("木偶面具裂纹", agent.head, new Vector3(-.035f, .02f, .267f), new Vector3(.018f, .245f, .012f), joint, false).transform.localRotation = Quaternion.Euler(0f, 0f, 11f);
                Sphere("木偶左眼窝", agent.head, new Vector3(-.085f, .055f, .245f), new Vector3(.060f, .070f, .036f), joint, false);
                Sphere("木偶右眼窝", agent.head, new Vector3(.086f, .036f, .247f), new Vector3(.063f, .076f, .038f), joint, false);
                GameObject leftEye = Sphere("左眼", agent.head, new Vector3(-.085f, .055f, .276f), new Vector3(.020f, .023f, .014f), eye, false);
                GameObject rightEye = Sphere("右眼", agent.head, new Vector3(.086f, .036f, .280f), new Vector3(.022f, .025f, .015f), eye, false);
                Box("木偶无声嘴缝", agent.head, new Vector3(-.018f, -.135f, .255f),
                    new Vector3(.105f, .012f, .010f), joint, false).transform.localRotation = Quaternion.Euler(0f, 0f, -5f);
                AddPuppetEyeGlow(leftEye);
                AddPuppetEyeGlow(rightEye);
                AddPuppetBodyRim(agent.root, name.Contains("执灯", StringComparison.Ordinal));

                BuildPuppetArm(agent.root, true, paleWood, darkWood, joint, wrap,
                    out agent.leftShoulder, out agent.leftElbow, out agent.leftWrist);
                BuildPuppetArm(agent.root, false, paleWood, darkWood, joint, wrap,
                    out agent.rightShoulder, out agent.rightElbow, out agent.rightWrist);
                BuildPuppetLeg(agent.root, true, paleWood, darkWood, joint, wrap,
                    out agent.leftHip, out agent.leftKnee, out agent.leftAnkle);
                BuildPuppetLeg(agent.root, false, paleWood, darkWood, joint, wrap,
                    out agent.rightHip, out agent.rightKnee, out agent.rightAnkle);

                SphereCollider trigger = rootObject.AddComponent<SphereCollider>();
                trigger.radius = .46f; trigger.center = new Vector3(0f, .94f, 0f); trigger.isTrigger = true;
                agent.source = rootObject.AddComponent<AudioSource>();
                agent.source.spatialBlend = 1f; agent.source.minDistance = 1.2f; agent.source.maxDistance = 24f;
                agent.speed = name.Contains("执灯", StringComparison.Ordinal) ? 2.55f : 2.35f;
                agent.poseOffset = name.Contains("执灯", StringComparison.Ordinal) ? .35f : 2.1f;
                agent.SetRestPose();
                return agent;
            }

            private static void CreateTaperedPuppetBody(string name, Transform parent, Material material)
            {
                const int segments = 12;
                float[] heights = { -.34f, -.20f, .02f, .24f, .36f };
                float[] widths = { .18f, .215f, .245f, .305f, .245f };
                float[] depths = { .145f, .165f, .185f, .195f, .155f };
                Vector3[] vertices = new Vector3[heights.Length * segments];
                int[] triangles = new int[(heights.Length - 1) * segments * 6];
                for (int ring = 0; ring < heights.Length; ring++)
                for (int segment = 0; segment < segments; segment++)
                {
                    float angle = segment * Mathf.PI * 2f / segments;
                    vertices[ring * segments + segment] = new Vector3(
                        Mathf.Cos(angle) * widths[ring], heights[ring], Mathf.Sin(angle) * depths[ring]);
                }
                int triangle = 0;
                for (int ring = 0; ring < heights.Length - 1; ring++)
                for (int segment = 0; segment < segments; segment++)
                {
                    int next = (segment + 1) % segments;
                    int a = ring * segments + segment;
                    int b = ring * segments + next;
                    int c = (ring + 1) * segments + segment;
                    int d = (ring + 1) * segments + next;
                    triangles[triangle++] = a; triangles[triangle++] = b; triangles[triangle++] = c;
                    triangles[triangle++] = b; triangles[triangle++] = d; triangles[triangle++] = c;
                }
                Mesh mesh = new() { name = name + "Mesh" };
                mesh.vertices = vertices;
                mesh.triangles = triangles;
                mesh.RecalculateNormals();
                mesh.RecalculateBounds();
                GameObject body = new(name);
                body.transform.SetParent(parent, false);
                body.AddComponent<MeshFilter>().sharedMesh = mesh;
                body.AddComponent<MeshRenderer>().sharedMaterial = material;
            }

            private static Transform CreatePuppetPivot(Transform parent, string name, Vector3 localPosition)
            {
                Transform pivot = new GameObject(name).transform;
                pivot.SetParent(parent, false);
                pivot.localPosition = localPosition;
                return pivot;
            }

            private static void BuildPuppetArm(Transform root, bool left, Material paleWood, Material darkWood,
                Material joint, Material wrap, out Transform shoulder, out Transform elbow, out Transform wrist)
            {
                float side = left ? -1f : 1f;
                shoulder = CreatePuppetPivot(root, left ? "左肩骨" : "右肩骨", new Vector3(side * .34f, 1.48f, 0f));
                Sphere(left ? "左肩关节" : "右肩关节", shoulder, Vector3.zero,
                    new Vector3(.15f, .15f, .15f), joint, false);
                Capsule(left ? "左上臂木段" : "右上臂木段", shoulder, new Vector3(0f, -.27f, 0f),
                    new Vector3(.12f, .31f, .12f), paleWood, false);
                Sphere(left ? "左肩布片" : "右肩布片", shoulder, new Vector3(0f, -.045f, 0f),
                    new Vector3(.16f, .13f, .15f), wrap, false);
                elbow = CreatePuppetPivot(shoulder, left ? "左肘骨" : "右肘骨", new Vector3(0f, -.56f, 0f));
                Sphere(left ? "左肘关节" : "右肘关节", elbow, Vector3.zero,
                    new Vector3(.115f, .115f, .115f), joint, false);
                Capsule(left ? "左前臂木段" : "右前臂木段", elbow, new Vector3(0f, -.245f, 0f),
                    new Vector3(.090f, .275f, .090f), paleWood, false);
                Cylinder(left ? "左前臂旧布箍" : "右前臂旧布箍", elbow, new Vector3(0f, -.41f, 0f),
                    new Vector3(.10f, .035f, .10f), wrap, false);
                wrist = CreatePuppetPivot(elbow, left ? "左腕骨" : "右腕骨", new Vector3(0f, -.49f, 0f));
                Sphere(left ? "左腕关节" : "右腕关节", wrist, Vector3.zero,
                    new Vector3(.085f, .075f, .085f), joint, false);
                Sphere(left ? "左木掌" : "右木掌", wrist, new Vector3(0f, -.105f, .02f),
                    new Vector3(.10f, .14f, .07f), paleWood, false);
                for (int finger = 0; finger < 3; finger++)
                {
                    float fingerX = (finger - 1) * .048f;
                    Capsule((left ? "左" : "右") + "木指_" + finger, wrist,
                        new Vector3(fingerX, -.235f, .03f), new Vector3(.026f, .095f, .026f), paleWood, false);
                }
            }

            private static void BuildPuppetLeg(Transform root, bool left, Material paleWood, Material darkWood,
                Material joint, Material wrap, out Transform hip, out Transform knee, out Transform ankle)
            {
                float side = left ? -1f : 1f;
                hip = CreatePuppetPivot(root, left ? "左髋骨" : "右髋骨", new Vector3(side * .18f, .74f, 0f));
                Sphere(left ? "左髋关节" : "右髋关节", hip, Vector3.zero,
                    new Vector3(.14f, .14f, .14f), joint, false);
                Capsule(left ? "左大腿木段" : "右大腿木段", hip, new Vector3(0f, -.25f, 0f),
                    new Vector3(.135f, .29f, .135f), paleWood, false);
                Sphere(left ? "左髋布片" : "右髋布片", hip, new Vector3(0f, -.045f, 0f),
                    new Vector3(.15f, .13f, .15f), wrap, false);
                knee = CreatePuppetPivot(hip, left ? "左膝骨" : "右膝骨", new Vector3(0f, -.50f, 0f));
                Sphere(left ? "左膝关节" : "右膝关节", knee, Vector3.zero,
                    new Vector3(.125f, .12f, .13f), joint, false);
                Capsule(left ? "左小腿木段" : "右小腿木段", knee, new Vector3(0f, -.22f, 0f),
                    new Vector3(.100f, .26f, .100f), paleWood, false);
                Cylinder(left ? "左小腿旧布箍" : "右小腿旧布箍", knee, new Vector3(0f, -.36f, 0f),
                    new Vector3(.11f, .035f, .11f), wrap, false);
                ankle = CreatePuppetPivot(knee, left ? "左踝骨" : "右踝骨", new Vector3(0f, -.44f, 0f));
                Sphere(left ? "左踝关节" : "右踝关节", ankle, Vector3.zero,
                    new Vector3(.09f, .08f, .09f), joint, false);
                Box(left ? "左木足" : "右木足", ankle, new Vector3(0f, -.055f, .11f),
                    new Vector3(.17f, .11f, .32f), darkWood, false);
            }

            private static void AddPuppetEyeGlow(GameObject eye)
            {
                if (eye == null) return;
                Light glow = eye.AddComponent<Light>();
                glow.name = "PuppetEyeGlow";
                glow.type = LightType.Point;
                glow.color = new Color(1f, .025f, .01f);
                glow.intensity = .38f;
                glow.range = .90f;
                glow.shadows = LightShadows.None;
            }

            private static void AddSubtlePuppetEmission(Material material, Color emission)
            {
                if (material == null || !material.HasProperty("_EmissionColor")) return;
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", emission);
            }

            private static void AddPuppetBodyRim(Transform root, bool warm)
            {
                GameObject rimObject = new("PuppetBodyRim");
                rimObject.transform.SetParent(root, false);
                rimObject.transform.localPosition = new Vector3(warm ? -.18f : .18f, 1.12f, -.38f);
                Light rim = rimObject.AddComponent<Light>();
                rim.name = "PuppetBodyRim";
                rim.type = LightType.Point;
                rim.color = warm ? new Color(.38f, .10f, .055f) : new Color(.10f, .25f, .28f);
                rim.intensity = .46f;
                rim.range = 2.65f;
                rim.shadows = LightShadows.None;
            }

            private void SetRestPose()
            {
                leftHip.localRotation = Quaternion.Euler(-3f, 0f, -2f);
                rightHip.localRotation = Quaternion.Euler(4f, 0f, 2f);
                leftKnee.localRotation = Quaternion.Euler(7f, 0f, 0f);
                rightKnee.localRotation = Quaternion.Euler(4f, 0f, 0f);
                leftAnkle.localRotation = Quaternion.Euler(2f, 0f, 0f);
                rightAnkle.localRotation = Quaternion.Euler(-2f, 0f, 0f);
                leftShoulder.localRotation = Quaternion.Euler(-9f, 1f, -8f);
                rightShoulder.localRotation = Quaternion.Euler(-3f, -2f, 7f);
                leftElbow.localRotation = Quaternion.Euler(16f, 0f, -2f);
                rightElbow.localRotation = Quaternion.Euler(11f, 0f, 3f);
                leftWrist.localRotation = Quaternion.Euler(2f, 0f, -8f);
                rightWrist.localRotation = Quaternion.Euler(-2f, 0f, 7f);
                torso.localRotation = Quaternion.Euler(5f, 0f, poseOffset > 1f ? 2.8f : -2.2f);
                head.localRotation = Quaternion.Euler(-7f, poseOffset > 1f ? -5f : 4f,
                    poseOffset > 1f ? 7f : -6f);
            }

            public bool Update(List<Vector3> trail, Vector3 playerPosition, bool watched, float delta,
                AudioClip[] stepClips)
            {
                watchedGrace = watched ? .28f : Mathf.Max(0f, watchedGrace - delta);
                if (watchedGrace > 0f)
                {
                    currentSpeed = 0f;
                    return true;
                }
                RefreshRouteCursor(trail);
                Vector3 target = playerPosition;
                if (trail != null && trail.Count > 0)
                {
                    while (trailIndex < trail.Count - 1 &&
                           Vector3.Distance(root.position, trail[trailIndex]) < .42f)
                        trailIndex++;
                    target = trail[Mathf.Clamp(trailIndex, 0, trail.Count - 1)];
                }
                Vector3 direction = target - root.position;
                if (direction.sqrMagnitude < .01f) return false;
                currentSpeed = Mathf.MoveTowards(currentSpeed, speed, 4.8f * delta);
                float stepTime = Mathf.Min(delta, 1f / 30f);
                Vector3 planarDirection = Vector3.ProjectOnPlane(direction, Vector3.up);
                Vector3 requestedMove = planarDirection.sqrMagnitude > .0001f
                    ? planarDirection.normalized * currentSpeed * stepTime
                    : Vector3.zero;
                if (requestedMove.sqrMagnitude > planarDirection.sqrMagnitude) requestedMove = planarDirection;
                Vector3 move = ResolvePlanarMotion(requestedMove);
                root.position += move;
                float verticalStep = Mathf.Max(.08f, currentSpeed * stepTime * .82f);
                root.position = new Vector3(root.position.x,
                    Mathf.MoveTowards(root.position.y, target.y, verticalStep), root.position.z);
                SnapToWalkSurface(stepTime);
                Vector3 planar = Vector3.ProjectOnPlane(direction, Vector3.up);
                if (planar.sqrMagnitude > .01f)
                {
                    smoothedDirection = Vector3.Slerp(smoothedDirection, planar.normalized,
                        1f - Mathf.Exp(-8.5f * delta));
                    root.rotation = Quaternion.Slerp(root.rotation, Quaternion.LookRotation(smoothedDirection),
                        1f - Mathf.Exp(-7.5f * delta));
                }
                float previousTravelled = travelled;
                travelled += Vector3.ProjectOnPlane(move, Vector3.up).magnitude;
                UpdateRigPose(playerPosition);
                int previousStep = Mathf.FloorToInt(previousTravelled / .66f);
                int currentStep = Mathf.FloorToInt(travelled / .66f);
                if (currentStep != previousStep && stepClips != null && stepClips.Length > 0)
                {
                    source.pitch = UnityEngine.Random.Range(.68f, .82f);
                    source.volume = .72f;
                    source.PlayOneShot(stepClips[UnityEngine.Random.Range(0, stepClips.Length)]);
                }
                return false;
            }

            public void PlaceAtRouteHandoff(Vector3 position)
            {
                if (root == null) return;
                root.position = position;
                currentSpeed = 0f;
                activeTrail = null;
                activeTrailCount = -1;
                trailIndex = 0;
                watchedGrace = 0f;
            }

            private void RefreshRouteCursor(List<Vector3> trail)
            {
                if (trail == null || trail.Count == 0)
                {
                    activeTrail = trail;
                    activeTrailCount = 0;
                    trailIndex = 0;
                    return;
                }
                Vector3 routeEnd = trail[trail.Count - 1];
                bool changed = !ReferenceEquals(activeTrail, trail) || activeTrailCount != trail.Count ||
                    (routeEnd - activeTrailEnd).sqrMagnitude > .035f;
                if (!changed)
                {
                    trailIndex = Mathf.Clamp(trailIndex, 0, trail.Count - 1);
                    return;
                }

                activeTrail = trail;
                activeTrailCount = trail.Count;
                activeTrailEnd = routeEnd;
                int nearestIndex = 0;
                float nearestDistance = float.PositiveInfinity;
                for (int i = 0; i < trail.Count; i++)
                {
                    Vector3 delta = trail[i] - root.position;
                    delta.y *= .35f;
                    float distance = delta.sqrMagnitude;
                    if (distance >= nearestDistance) continue;
                    nearestDistance = distance;
                    nearestIndex = i;
                }
                trailIndex = nearestIndex;
            }

            private static bool IsStairWalkSurface(Collider collider)
            {
                if (collider == null) return false;
                string name = collider.name;
                return name.StartsWith("ChapterTwo_UpperFlight_", StringComparison.Ordinal) ||
                       name.StartsWith("ChapterTwo_LowerFlight_", StringComparison.Ordinal) ||
                       name == "ChapterTwo_UpperLanding" ||
                       name == "ChapterTwo_UpperWoodThreshold" ||
                       name == "ChapterTwo_UpperFlightEntryBridge" ||
                       name == "ChapterTwo_MidLanding" ||
                       name == "ChapterTwo_MazeLanding" ||
                       name == "ChapterTwo_MazeEntryCorridor";
            }

            private static bool IsPuppetRouteSeal(Collider collider)
            {
                if (collider == null) return false;
                // These are visual reveal returns beside the 2.36m maze portal.
                // The player must collide with them, while the puppet route uses
                // the central opening and must not be pushed onto the wall edge.
                if (collider.name == "ChapterTwo_MazeEntryEastRevealSeal" ||
                    collider.name == "ChapterTwo_MazeEntryWestRevealSeal") return true;
                Bounds bounds = collider.bounds;
                Vector3 center = bounds.center;
                // The authored north shell intentionally has two wall pieces
                // around the center opening.  Once a puppet reaches the lower
                // landing, keep its sphere cast inside that opening band so the
                // wall edge cannot push it sideways into the reveal return.
                return center.z > -27.90f && center.z < -25.45f &&
                       bounds.min.x < 1.55f && bounds.max.x > .20f && center.y < -3.0f;
            }

            private Vector3 ResolvePlanarMotion(Vector3 requestedMove)
            {
                float distance = requestedMove.magnitude;
                if (distance <= .0001f) return Vector3.zero;
                Vector3 direction = requestedMove / distance;
                Vector3 origin = root.position + Vector3.up * .84f;
                int count = Physics.SphereCastNonAlloc(origin, .255f, direction, motionHits,
                    distance + .055f, ~0, QueryTriggerInteraction.Ignore);
                float nearest = distance + .055f;
                Vector3 blockingNormal = Vector3.zero;
                for (int i = 0; i < count; i++)
                {
                    Collider hitCollider = motionHits[i].collider;
                    if (hitCollider == null || hitCollider.transform == root || hitCollider.transform.IsChildOf(root)) continue;
                    if (IsStairWalkSurface(hitCollider)) continue;
                    if (IsPuppetRouteSeal(hitCollider)) continue;
                    if (motionHits[i].distance < nearest)
                    {
                        nearest = motionHits[i].distance;
                        blockingNormal = motionHits[i].normal;
                    }
                }
                if (blockingNormal.sqrMagnitude < .01f || nearest >= distance + .05f)
                    return requestedMove;

                Vector3 firstMove = direction * Mathf.Max(0f, nearest - .06f);
                Vector3 remaining = requestedMove - firstMove;
                Vector3 slide = Vector3.ProjectOnPlane(remaining, blockingNormal);
                slide.y = 0f;
                float slideDistance = slide.magnitude;
                if (slideDistance <= .0001f) return firstMove;
                int slideCount = Physics.SphereCastNonAlloc(origin + firstMove, .255f, slide / slideDistance,
                    motionHits, slideDistance + .04f, ~0, QueryTriggerInteraction.Ignore);
                float allowedSlide = slideDistance;
                for (int i = 0; i < slideCount; i++)
                {
                    Collider hitCollider = motionHits[i].collider;
                    if (hitCollider == null || hitCollider.transform == root || hitCollider.transform.IsChildOf(root)) continue;
                    if (IsStairWalkSurface(hitCollider)) continue;
                    if (IsPuppetRouteSeal(hitCollider)) continue;
                    allowedSlide = Mathf.Min(allowedSlide, Mathf.Max(0f, motionHits[i].distance - .05f));
                }
                return firstMove + slide.normalized * allowedSlide;
            }

            private void SnapToWalkSurface(float delta)
            {
                Vector3 origin = root.position + Vector3.up * .72f;
                int count = Physics.RaycastNonAlloc(origin, Vector3.down, motionHits, 1.55f,
                    ~0, QueryTriggerInteraction.Ignore);
                float nearest = float.PositiveInfinity;
                float groundY = root.position.y;
                for (int i = 0; i < count; i++)
                {
                    Collider hitCollider = motionHits[i].collider;
                    if (hitCollider == null || hitCollider.transform == root || hitCollider.transform.IsChildOf(root) ||
                        motionHits[i].normal.y < .42f || motionHits[i].distance >= nearest) continue;
                    nearest = motionHits[i].distance;
                    groundY = motionHits[i].point.y + .02f;
                }
                if (nearest < float.PositiveInfinity)
                    root.position = new Vector3(root.position.x,
                        Mathf.MoveTowards(root.position.y, groundY, Mathf.Max(.10f, speed * delta)), root.position.z);
            }

            private void UpdateRigPose(Vector3 playerPosition)
            {
                float phase = travelled * 8.4f + poseOffset;
                float stride = Mathf.Sin(phase);
                float opposite = Mathf.Sin(phase + Mathf.PI);
                float leftKneeBend = Mathf.Max(0f, opposite) * 34f + 4f;
                float rightKneeBend = Mathf.Max(0f, stride) * 34f + 4f;
                leftHip.localRotation = Quaternion.Euler(stride * 30f, 0f, -2.5f);
                rightHip.localRotation = Quaternion.Euler(opposite * 30f, 0f, 2.5f);
                leftKnee.localRotation = Quaternion.Euler(leftKneeBend, 0f, 0f);
                rightKnee.localRotation = Quaternion.Euler(rightKneeBend, 0f, 0f);
                leftAnkle.localRotation = Quaternion.Euler(-stride * 10f, 0f, 0f);
                rightAnkle.localRotation = Quaternion.Euler(-opposite * 10f, 0f, 0f);

                leftShoulder.localRotation = Quaternion.Euler(opposite * 23f - 5f, 0f, -7f);
                rightShoulder.localRotation = Quaternion.Euler(stride * 23f - 5f, 0f, 7f);
                leftElbow.localRotation = Quaternion.Euler(12f + Mathf.Max(0f, stride) * 18f, 0f, 0f);
                rightElbow.localRotation = Quaternion.Euler(12f + Mathf.Max(0f, opposite) * 18f, 0f, 0f);
                leftWrist.localRotation = Quaternion.Euler(0f, 0f, -6f + stride * 5f);
                rightWrist.localRotation = Quaternion.Euler(0f, 0f, 6f + opposite * 5f);
                torso.localRotation = Quaternion.Euler(4f + Mathf.Abs(stride) * 2f, 0f, stride * 2.2f);

                Vector3 localTarget = root.InverseTransformDirection(playerPosition - head.position);
                float headYaw = Mathf.Clamp(Mathf.Atan2(localTarget.x, localTarget.z) * Mathf.Rad2Deg, -38f, 38f);
                float headRoll = Mathf.Sin(travelled * 2.7f + poseOffset) * 4.2f + (poseOffset > 1f ? 5f : -4f);
                head.localRotation = Quaternion.Euler(-6f + Mathf.Sin(phase * .5f) * 2f, headYaw, headRoll);
            }
        }
    }

    public sealed class ChapterTwoMazeSoulEchoPickup : MonoBehaviour
    {
        private ChapterTwoAdventureRuntime runtime;
        private Vector3 basePosition;
        private bool collected;

        public void Configure(ChapterTwoAdventureRuntime owner, Vector3 position)
        {
            runtime = owner;
            basePosition = position;
        }

        private void Update()
        {
            if (runtime == null || collected) return;
            float wave = Mathf.Sin(Time.unscaledTime * 2.6f) * .12f;
            transform.position = basePosition + Vector3.up * wave;
            transform.Rotate(0f, 62f * Time.unscaledDeltaTime, 18f * Time.unscaledDeltaTime, Space.Self);
            Vector3 delta = runtime.MazePlayerPosition - transform.position;
            delta.y *= .45f;
            if (delta.sqrMagnitude > 1.65f * 1.65f) return;
            collected = true;
            runtime.CollectMazeSoulEcho(this);
        }
    }
}
