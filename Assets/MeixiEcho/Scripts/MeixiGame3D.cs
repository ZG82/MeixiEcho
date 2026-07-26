using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using UnityEngine.Networking;

namespace MeixiEcho
{
    public enum CombatBeatAction
    {
        None,
        AttackOne,
        AttackTwo,
        Dodge,
        JKWave,
        JDodgeStrike,
        KDodgeTripleArc,
        TripleJ,
        JKRiff,
        KJKRiff
    }

    public enum CombatInputKey
    {
        None,
        J,
        K,
        Space,
        Q,
        E
    }

    public sealed class MeixiGame3D : MonoBehaviour
    {
        [Header("Scene authoring")]
        [SerializeField, Tooltip("启用后使用场景中已经摆放的地图、角色和 NPC，不再由代码重复生成。")]
        private bool useSceneAuthoredWorld;

        private readonly string[] chapterNames =
        {
            "溪 流 里 的 节 拍"
        };

        private readonly string[] chapterSubtitles =
        {
            "听见自然留下的第一段魂音"
        };

        private readonly string[][] dialogues =
        {
            new[]
            {
                "系统｜检测到可收集魂音。",
                "系统｜需要在恰当的时机和位置，按下正确的按键完成相应的音乐节拍。",
                "系统｜全部节拍完成后，即可获取“魂音”。",
                "林默｜原来那个发光的东西，就是村民所说的魂音……"
            }
        };

        // L 轻击、H 蓄力重击、D 闪避、A/R 方向突击、B 双键合奏。
        // 节奏关卡采用动作编排，而不是传统的单键下落方块。
        private readonly string[] patterns =
        {
            "D-L-D-H-D-L-D-H"
        };

        private readonly float[] bpms = { 92f };
        private readonly Vector3[] nodePositions =
        {
            new(-8.5f, 1.15f, -6.8f)
        };

        private Font uiFont;
        private Font displayFont;
        private Font diaryFont;
        private Material earth;
        private Material grass;
        private Material stone;
        private Material wood;
        private Material roof;
        private Material leaves;
        private Material water;
        private Material gold;
        private Material cyan;
        private Material noise;
        private Material clay;
        private Material villageAtlas;
        private MeixiThirdPersonPlayer player;
        private Transform playerTransform;
        private ThirdPersonOrbitCamera orbitCamera;
        private CreekRhythmEnemy3D creekSpirit;
        private AudioSource audioSource;
        private AudioSource musicSource;
        private AudioClip lowTone;
        private AudioClip highTone;
        private AudioClip successTone;
        private AudioClip rhythmSong;
        private AudioClip chapterOneExplorationMusic;
        private AudioClip chapterOneMusic;
        private AudioClip chapterOnePhaseThreeMusic;
        private Canvas canvas;
        private GameObject titlePanel;
        private MeixiTitleScreenFx titleScreenFx;
        private Button titlePrimaryButton;
        private Button titleNewJourneyButton;
        private Button titleContinueButton;
        private Button titleDeleteSaveButton;
        private CanvasGroup titleSaveMenuGroup;
        private Text titleSaveSummaryText;
        private GameObject titleDeleteConfirmPanel;
        private Text titleConfirmationTitleText;
        private Text titleConfirmationBodyText;
        private Button titleConfirmationAcceptButton;
        private Action pendingTitleConfirmation;
        private GameObject hudPanel;
        private GameObject dialoguePanel;
        private GameObject rhythmPanel;
        private GameObject chapterPanel;
        private GameObject endingPanel;
        private GameObject pausePanel;
        private GameObject pauseSettingsPanel;
        private bool pauseSettingsOpen;
        private Button pauseResumeButton;
        private Button pauseSettingsButton;
        private Button pauseRestartButton;
        private Button pauseQuitButton;
        private Button pauseSettingsBackButton;
        private Button pauseVolumeDownButton;
        private Button pauseVolumeUpButton;
        private Button pauseTerminalButton;
        private GameObject terminalPanel;
        private InputField terminalInput;
        private Text terminalOutput;
        private Button terminalSubmitButton;
        private Button terminalCloseButton;
        private bool terminalCommandExecuting;
        private double pauseStartedDsp;
        private Text pausePracticeText;
        private Text settingsVolumeText;
        private Text settingsAchievementCountText;
        private Text settingsAchievementListText;
        private GameObject interactionCard;
        private GameObject openingPanel;
        private GameObject loadingPanel;
        private GameObject transitionPanel;
        private CanvasGroup transitionGroup;
        private Text transitionCaption;
        private Coroutine sceneTransitionRoutine;
        private bool sceneTransitionActive;
        private bool chapterCardTransitionPending;
        private Text chapterHudText;
        private Text objectiveText;
        private Text healthText;
        private RectTransform healthCardRect;
        private readonly Image[] healthBarSegments = new Image[5];
        private Image healthBarGlow;
        private Text healthDeltaText;
        private CanvasGroup healthFxGroup;
        private Coroutine healthFxRoutine;
        private int displayedHealth = 5;
        private Text soulCountText;
        private Text interactionText;
        private Text interactionTitle;
        private Text interactionSubtitle;
        private Text navigationText;
        private Text dialogueSpeaker;
        private Text dialogueText;
        private Text dialogueRole;
        private Text dialogueCounter;
        private Text storyProgressText;
        private Text archiveProgressText;
        private Image dialoguePortraitPlate;
        private Image dialoguePortraitImage;
        private Sprite npcDialoguePortraitSprite;
        private Sprite childDialoguePortraitSprite;
        private Sprite auntDialoguePortraitSprite;
        private Sprite playerDialoguePortraitSprite;
        private Text toastText;
        private GameObject toastPanel;
        private ToastBannerFx toastBannerFx;
        private Text chapterCardNumber;
        private Text chapterCardTitle;
        private Text chapterCardSubtitle;
        private Button endingReplayButton;
        private Button endingNextChapterButton;
        private Text rhythmTitle;
        private Text rhythmJudge;
        private Text rhythmScore;
        private RectTransform rhythmTrack;
        private RectTransform miniMapPlayer;
        private RectTransform navigationArrow;
        private Image rhythmFlash;
        private Text combatActionKey;
        private Text combatActionName;
        private Image combatEnemyHealthFill;
        private Image combatEnemyHealthChipFill;
        private GameObject leftHandCuePlate;
        private GameObject rightHandCuePlate;
        private Image leftHandRhythmFlash;
        private Image leftHandHoldHalo;
        private Image leftHandHoldProgressFill;
        private Text leftHandActionKey;
        private Text leftHandActionName;
        private readonly Image[] leftHandHoldRingSegments = new Image[28];
        private readonly Image[] combatPhaseSegments = new Image[3];
        private Text combatPhaseLabel;
        private GameObject combatPhaseBanner;
        private CanvasGroup combatPhaseBannerGroup;
        private Text combatPhaseBannerText;
        private GameObject defeatPanel;
        private Button defeatRetryButton;
        private Button defeatQuitButton;
        private Text defeatSummaryText;
        private GameObject gameplayGuidePanel;
        private Text gameplayGuideTitle;
        private Text gameplayGuideSubtitle;
        private Text gameplayGuideBody;
        private Button gameplayGuideContinueButton;
        private bool gameplayGuideOpen;
        private bool explorationGuideShown;
        private bool battleGuideShown;
        private Action gameplayGuideCallback;
        private float gameplayGuidePreviousTimeScale = 1f;
        private bool gameplayGuidePreviousAudioPause;
        private Sprite beatSigilSprite;
        private Texture2D finalSlashTexture;
        private readonly List<SoulNode3D> nodes = new();
        private readonly List<VillageNpc3D> villageNpcs = new();
        private readonly List<Image> soulOrbImages = new();
        private readonly List<RhythmNote> rhythmNotes = new();
        private readonly List<GameObject> rhythmVisuals = new();
        private readonly List<RectTransform> miniMapMarkers = new();
        private bool started;
        private bool dialogueOpen;
        private bool rhythmActive;
        private bool complete;
        private bool paused;
        private int completedMemories;
        private int defeatedEnemies;
        private int health = 5;
        private int dialogueLine;
        private string[] activeDialogue;
        private string fullDialogueLine;
        private float typedCharacters;
        private Action dialogueCallback;
        private SoulNode3D pendingNode;
        private float chapterCardUntil;
        private Action chapterCardCallback;
        private float toastUntil;
        private Vector3 respawnPoint = new(0f, 0.3f, 1f);
        private double rhythmStart;
        private float rhythmDuration;
        private int rhythmHits;
        private int rhythmMisses;
        private int rhythmCombo;
        private int rhythmBestCombo;
        private int activeRhythmChapter;
        private bool heavyCharging;
        private float heavyChargeStarted;
        private bool captureMode;
        private bool captureDone;
        private bool titleCaptureWhenReady;
        private bool openingCaptureWhenReady;
        private float captureAt;
        private string captureFileName = "MeixiCapture.png";
        private bool rhythmAutoValidation;
        private bool rhythmFinisherValidation;
        private bool meleeAutoValidation;
        private bool rangedCounterValidation;
        private bool wrongMeleeValidation;
        private bool autoActionIssuedForBeat;
        private bool openingActive;
        private int openingDiaryPage;
        private float openingPageStartedAt;
        private float openingPageUntil;
        private CanvasGroup openingPaperGroup;
        private Text openingDiaryDate;
        private Text openingDiaryTitle;
        private Text openingDiaryBody;
        private Text openingDiaryPageText;
        private bool secondChapterLoading;
        private bool secondChapterRouteActive;
        private float nextRouteIntegrityCheckAt;
        private float openingUntil;
        private bool chapterOneCombatActive;
        private float chapterOneCombatBpm = 92f;
        private double chapterOneBeatStart;
        private float chapterOneBeatPulse;
        private const float CombatInputWindow = 0.235f;
        private const float CombatChordWindow = 0.145f;
        private const float CombatSequenceMaxGap = 0.34f;
        private const float CombatSequenceStepSpacing = 0.19f;
        private const double CombatSequenceStepTolerance = 0.28d;
        private const double CombatHoldEarlyBuffer = 0.16d;
        private const double CombatPhaseRestSeconds = 1.80d;
        // Phase three's dual-hand score was already widened to 125% of the
        // chapter baseline. Give both voices another 40% reaction allowance:
        // 1.25 * 1.40 = 1.75. Phases one and two still use the untouched 1.00 path.
        private const float PhaseThreeJudgementScale = 1.75f;
        private float CurrentCombatInputWindow => CombatInputWindow *
            (chapterOneCombatPhase >= 3 ? PhaseThreeJudgementScale : 1f);
        private float CurrentCombatChordWindow => CombatChordWindow *
            (chapterOneCombatPhase >= 3 ? PhaseThreeJudgementScale : 1f);
        private float CurrentCombatSequenceMaxGap => CombatSequenceMaxGap *
            (chapterOneCombatPhase >= 3 ? PhaseThreeJudgementScale : 1f);
        private double CurrentCombatSequenceStepTolerance => CombatSequenceStepTolerance *
            (chapterOneCombatPhase >= 3 ? PhaseThreeJudgementScale : 1f);
        private double CurrentCombatHoldEarlyBuffer => CombatHoldEarlyBuffer *
            (chapterOneCombatPhase >= 3 ? PhaseThreeJudgementScale : 1f);
        private ChapterOneBattleTimeline chapterOneBattleTimeline;
        private ChapterOneBattleTimeline phaseThreeBattleTimeline;
        private CombatBeatAction expectedCombatAction;
        private double expectedCombatBeatDsp;
        private double currentThreatResolveDsp;
        private readonly CombatInputKey[] expectedCombatSteps = new CombatInputKey[3];
        private readonly double[] expectedCombatStepDsps = new double[3];
        private int expectedCombatStepCount;
        private int expectedCombatStepIndex;
        private CombatInputKey bufferedCombatKey;
        private double bufferedCombatKeyDsp;
        private double lastCombatSequenceInputDsp;
        private bool leftHandCueActive;
        private bool leftHandHoldRequired;
        private bool leftHandHolding;
        private bool leftHandSucceeded;
        private bool rightHandSucceeded;
        private bool rightHandDodgeResponse;
        private CombatInputKey expectedLeftHandKey;
        private double expectedLeftHandBeatDsp;
        private double leftHandHoldStartedDsp;
        private float expectedLeftHandHoldDuration;
        private float leftHandHoldProgress;
        private bool dualHandHoldPreviewActive;
        private int lastDualHandHudPhase = -1;
        private bool combatBeatPending;
        private bool combatBeatSucceeded;
        private bool threatDamageResolved;
        private int currentThreatType = -1;
        private int threatSequence;
        private int activeThreatId;
        private int dodgedThreatId = -1;
        private int counteredThreatId = -1;
        private int resolvedThreatId = -1;
        private double playerDamageImmuneUntilDsp;
        private int chapterOneCombatPhase = 1;
        private double combatPhaseRestUntilDsp;
        private Coroutine combatPhaseBannerRoutine;
        private bool phaseThreeGuideOpen;
        private float phaseThreeGuidePreviousTimeScale = 1f;
        private bool phaseThreeGuidePreviousAudioPause;
        private int phaseSuccessfulBeats;
        private bool finishingStrike;
        private bool defeatOpen;
        private float cinematicInputLockedUntil;
        private int chapterOneCombatCombo;
        private int chapterOneCombatBestCombo;
        private int chapterOneCombatSuccesses;
        private int chapterOneCombatMisses;
        private double meleeCounterUntilDsp;
        private int meleeCounterThreatId = -1;
        private Coroutine finalStrikeRoutine;
        private ChapterOneSoulEchoPickup chapterOneSoulPickup;
        private bool chapterOneSoulCollected;
        private int gameplaySessionRevision;
        private float finalStrikeRestoreTimeScale = 1f;
        private float finalStrikeRestorePitch = 1f;
        private readonly HashSet<string> collectedNpcMemories = new(StringComparer.Ordinal);

        private readonly string[] diaryDates =
        {
            "山间旧闻 · 无法考证的年代",
            "实践日志 · 2030 年夏",
            "残缺卷纸 · 一"
        };

        private readonly string[] diaryTitles =
        {
            "关于回响力与魂音的传闻",
            "今年，音乐似乎越来越少见了",
            "村子曾经发生过一场大事变"
        };

        private readonly string[] diaryBodies =
        {
            "据山间某个村子流传已久的传闻所说，音乐拥有着无穷的力量。\n它能唤醒沉睡的山林，让干涸的溪流重新歌唱；也能穿过层层云雾，抵达人们心中最柔软的地方。\n\n有人说，只要奏响那段遗失已久的旋律，便能听见土地的记忆，看见被岁月掩埋的故事。\n\n村民们将这股力量称为“回响力”，将音乐的源头称为“魂音”。",
            "2030 年，一支由学长学姐介绍、以音乐为主题的“三下乡”实践队伍，来到了福建闽清。\n\n但是今年和往年有所不同。\n\n一路上，能够被听见的音乐似乎越来越少了……",
            "据记载，几年前村子发生过一场大事变。\n\n纸张在这里被撕去，余下的字迹已经模糊。\n那场事变与消失的音乐，究竟有什么关系？"
        };

        private enum RhythmActionType
        {
            LightStrike,
            HeavyStrike,
            Dodge,
            LeftStrike,
            RightStrike,
            EchoBurst
        }

        private sealed class RhythmNote
        {
            public RhythmActionType Action;
            public float Time;
            public bool Resolved;
            public RectTransform Visual;
        }

        public bool CinematicInputLocked => Time.unscaledTime < cinematicInputLockedUntil;
        public bool CanPlayerMove => started && !openingActive && !paused && !defeatOpen && !gameplayGuideOpen && !phaseThreeGuideOpen && !finishingStrike && !sceneTransitionActive && !CinematicInputLocked &&
                                     (!rhythmActive || chapterOneCombatActive) && (!complete || secondChapterRouteActive);
        public bool GameplayRunning => started && !openingActive && !dialogueOpen && !complete && !paused && !defeatOpen && !gameplayGuideOpen && !phaseThreeGuideOpen && !sceneTransitionActive && !CinematicInputLocked;
        public bool ChapterOneCombatActive => chapterOneCombatActive && !complete && !paused && !defeatOpen && !phaseThreeGuideOpen && !sceneTransitionActive && !CinematicInputLocked;
        public bool LeftHandCombatInputEnabled => ChapterOneCombatActive && chapterOneCombatPhase >= 3 && leftHandCueActive;
        public bool PlayerIsDodging => player != null && player.IsInvulnerable;
        public bool SecondChapterRouteActive => secondChapterRouteActive;
        public Vector3 CombatTargetPosition => creekSpirit != null ? creekSpirit.transform.position : PlayerPosition + Vector3.forward * 3f;
        public Vector3 PlayerPosition => playerTransform != null ? playerTransform.position : Vector3.zero;
        public bool EnemyThreatPending => chapterOneCombatActive && combatBeatPending;
        public bool CombatPhaseResting => chapterOneCombatActive && (phaseThreeGuideOpen || AudioSettings.dspTime < combatPhaseRestUntilDsp);
        public double CurrentCombatThreatResolveDsp => currentThreatResolveDsp;
        public float AnalyzedBattleBpm => chapterOneBattleTimeline != null ? chapterOneBattleTimeline.Bpm : chapterOneCombatBpm;
        public bool MeleeCounterWindowOpen => chapterOneCombatActive && !finishingStrike &&
                                              meleeCounterThreatId >= 0 && AudioSettings.dspTime <= meleeCounterUntilDsp;

        private void Awake()
        {
            Application.targetFrameRate = 120;
            Application.runInBackground = true;
            AudioListener.pause = false;
            MeixiLocalProgress.Initialize();
            MeixiLocalProgress.ApplyAudioSetting();
            uiFont = Font.CreateDynamicFontFromOSFont(new[] { "Microsoft YaHei", "SimHei", "Arial" }, 34);
            displayFont = Font.CreateDynamicFontFromOSFont(
                new[] { "STKaiti", "华文楷体", "KaiTi", "楷体", "FZKai-Z03", "SimSun", "Microsoft YaHei" }, 72);
            BuildMaterials();
            LoadGeneratedVfxAssets();
            BuildAudio();
            if (useSceneAuthoredWorld)
            {
                BindSceneAuthoredWorld();
            }
            else
            {
                BuildWorld();
            }
            BuildUI();
            ShowTitle();
            // Startup flow is deliberately player-paced:
            // ZJH logo -> title/loading screen -> click Start -> handwritten diary.
            // The title panel is already active behind the opaque logo, so its fade
            // cannot expose a transient 3D scene frame.
            TryStartCaptureMode();
            TryStartSecondChapterSmokeTest();
        }

        private void TryStartSecondChapterSmokeTest()
        {
            if (Array.Exists(Environment.GetCommandLineArgs(), value =>
                    string.Equals(value, "-meixiPhaseSmoke", StringComparison.OrdinalIgnoreCase)))
            {
                StartCoroutine(ValidateCombatPhaseRest());
                return;
            }
            if (Array.Exists(Environment.GetCommandLineArgs(), value =>
                    string.Equals(value, "-meixiMovementSmoke", StringComparison.OrdinalIgnoreCase)))
            {
                StartCoroutine(ValidateFreeMovement());
                return;
            }
            if (Array.Exists(Environment.GetCommandLineArgs(), value =>
                    string.Equals(value, "-meixiEndingPointerSmoke", StringComparison.OrdinalIgnoreCase)))
            {
                StartCoroutine(ValidateEndingPointerClick());
                return;
            }
            if (!Array.Exists(Environment.GetCommandLineArgs(), value =>
                    string.Equals(value, "-meixiSecondChapterSmoke", StringComparison.OrdinalIgnoreCase))) return;
            StartCoroutine(ValidateInputAndSecondChapterTransition());
        }

        private IEnumerator ValidateCombatPhaseRest()
        {
            yield return null;
            CompleteTransitionImmediatelyForValidation();
            started = true;
            titlePanel.SetActive(false);
            openingActive = false;
            if (openingPanel != null) openingPanel.SetActive(false);
            if (chapterPanel != null) chapterPanel.SetActive(false);
            if (dialoguePanel != null) dialoguePanel.SetActive(false);
            hudPanel.SetActive(true);
            BeginChapterOneCombatCore();
            creekSpirit?.PreparePhaseValidation(2);
            int healthBefore = health;
            yield return new WaitForSecondsRealtime(.72f);
            bool restingAtMidpoint = CombatPhaseResting;
            bool pendingAtMidpoint = combatBeatPending;
            string keyAtMidpoint = combatActionKey != null ? combatActionKey.text : "<missing>";
            DamagePlayer();
            bool damageIgnored = health == healthBefore;
            Debug.Log($"[MeixiPhaseSmoke] midpoint resting={restingAtMidpoint} pending={pendingAtMidpoint} " +
                      $"key={keyAtMidpoint} damageIgnored={damageIgnored}");
            yield return new WaitForSecondsRealtime(1.75f);
            Debug.Log($"[MeixiPhaseSmoke] resumed resting={CombatPhaseResting} pending={combatBeatPending} " +
                      $"key={(combatActionKey != null ? combatActionKey.text : "<missing>")} health={health}");
            yield return new WaitForSecondsRealtime(.20f);
            Application.Quit();
        }

        private IEnumerator ValidateFreeMovement()
        {
            yield return null;
            CompleteTransitionImmediatelyForValidation();
            BeginAdventurePresentation();
            FinishOpeningSequence(true);
            if (chapterPanel.activeSelf) FinishChapterCardImmediately();
            while (dialogueOpen) AdvanceDialogue();
            yield return null;
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
            {
                Debug.LogError("[MeixiMovementSmoke] Keyboard missing.");
                yield break;
            }
            Vector3 explorationStart = playerTransform.position;
            Collider[] spawnOverlaps = Physics.OverlapCapsule(explorationStart + Vector3.up * .22f,
                explorationStart + Vector3.up * 1.72f, .39f, ~0, QueryTriggerInteraction.Ignore);
            foreach (Collider overlap in spawnOverlaps)
            {
                if (overlap == null || overlap.transform == playerTransform || overlap.transform.IsChildOf(playerTransform)) continue;
                Debug.Log($"[MeixiMovementSmoke] spawn overlap={overlap.name} path={overlap.transform.name} " +
                          $"bounds={overlap.bounds.center}/{overlap.bounds.size} enabled={overlap.enabled}");
            }
            player.InjectValidationMove(Vector2.up, .65f);
            yield return new WaitForSecondsRealtime(.65f);
            yield return null;
            float explorationDistance = HorizontalDistance(explorationStart, playerTransform.position);
            Debug.Log($"[MeixiMovementSmoke] exploration distance={explorationDistance:F3} canMove={CanPlayerMove}");

            ActivateChapterOneCombat();
            yield return new WaitForSecondsRealtime(.15f);
            Vector3 combatStart = playerTransform.position;
            player.InjectValidationMove(Vector2.right, .65f);
            yield return new WaitForSecondsRealtime(.65f);
            yield return null;
            float combatDistance = HorizontalDistance(combatStart, playerTransform.position);
            Debug.Log($"[MeixiMovementSmoke] combat distance={combatDistance:F3} canMove={CanPlayerMove}");
            yield return new WaitForSecondsRealtime(.25f);
            Application.Quit();
        }

        private IEnumerator ValidateEndingPointerClick()
        {
            yield return null;
            CompleteTransitionImmediatelyForValidation();
            started = true;
            PresentEnding();
            yield return null;
            Mouse mouse = Mouse.current;
            if (mouse == null || endingNextChapterButton == null)
            {
                Debug.LogError("[MeixiPointerSmoke] Mouse or second chapter button missing.");
                yield break;
            }
            RectTransform rect = endingNextChapterButton.GetComponent<RectTransform>();
            Vector2 center = RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(rect.rect.center));
            mouse.WarpCursorPosition(center);
            yield return null;
            InputSystem.QueueDeltaStateEvent(mouse.leftButton, 1f);
            yield return null;
            InputSystem.QueueDeltaStateEvent(mouse.leftButton, 0f);
            Debug.Log("[MeixiPointerSmoke] Queued pointer click at " + center);
        }

        private IEnumerator ValidateInputAndSecondChapterTransition()
        {
            yield return null;
            CompleteTransitionImmediatelyForValidation();
            BeginAdventurePresentation();
            FinishOpeningSequence(true);
            if (chapterPanel.activeSelf) FinishChapterCardImmediately();
            while (dialogueOpen) AdvanceDialogue();
            yield return null;
            CharacterController activeController = playerTransform != null
                ? playerTransform.GetComponent<CharacterController>()
                : null;
            Debug.Log($"[MeixiInputSmoke] keyboard={Keyboard.current != null} canMove={CanPlayerMove} controller={activeController != null && activeController.enabled}");
            yield return new WaitForSecondsRealtime(.25f);
            UnlockSecondChapterRoute();
            yield return null;
            Debug.Log($"[MeixiRouteSmoke] route={secondChapterRouteActive} canMove={CanPlayerMove}");
            StartSecondChapterSceneLoad();
        }

        private void BindSceneAuthoredWorld()
        {
            player = FindAnyObjectByType<MeixiThirdPersonPlayer>();
            playerTransform = player != null ? player.transform : null;
            orbitCamera = FindAnyObjectByType<ThirdPersonOrbitCamera>();
            if (orbitCamera == null)
            {
                Camera sceneCamera = Camera.main != null ? Camera.main : FindAnyObjectByType<Camera>();
                if (sceneCamera != null)
                {
                    orbitCamera = sceneCamera.GetComponent<ThirdPersonOrbitCamera>() ??
                                  sceneCamera.gameObject.AddComponent<ThirdPersonOrbitCamera>();
                    Debug.LogWarning("可编辑场景的相机组件已在运行时自动修复。请保存场景以永久保留该组件。");
                }
            }
            orbitCamera?.Configure(playerTransform);

            // Upgrade the scene-authored cast before binding the player Animator.
            // Binding the baked DefaultMale first produced a one-frame avatar swap
            // and was the source of first-launch / restart differences.
            MeixiChapterOneEnvironment.Ensure();
            if (player != null)
            {
                Animator visualAnimator = player.GetComponentInChildren<Animator>(true);
                Transform visual = visualAnimator != null
                    ? visualAnimator.transform
                    : player.transform.childCount > 0 ? player.transform.GetChild(0) : null;
                player.Configure(this, orbitCamera, visual);
            }

            nodes.Clear();
            foreach (SoulNode3D node in FindObjectsByType<SoulNode3D>())
            {
                node.Rebind(this);
                nodes.Add(node);
            }

            villageNpcs.Clear();
            foreach (VillageNpc3D npc in FindObjectsByType<VillageNpc3D>())
            {
                npc.Rebind(this, playerTransform);
                villageNpcs.Add(npc);
            }
            SyncChapterOneStoryNpcs();

            ReacquireCreekSpirit();

            if (playerTransform == null)
            {
                Debug.LogError("可编辑场景中没有找到 MeixiThirdPersonPlayer，请重新烘焙场景或放置玩家对象。");
            }
            MeixiSecondChapterRoute.Ensure(this);
        }

        private void ReacquireCreekSpirit()
        {
            CreekRhythmEnemy3D[] candidates = FindObjectsByType<CreekRhythmEnemy3D>(FindObjectsInactive.Include);
            CreekRhythmEnemy3D preferred = null;

            // The authored enemy is parented under the editable world. A root-level
            // enemy is only the emergency runtime fallback and must never win after
            // an Editor domain reload, otherwise two enemies attack simultaneously.
            foreach (CreekRhythmEnemy3D candidate in candidates)
            {
                if (candidate == null) continue;
                if (preferred == null || (preferred.transform.parent == null && candidate.transform.parent != null))
                    preferred = candidate;
            }

            creekSpirit = preferred;
            creekSpirit?.Rebind(this, playerTransform);

            foreach (CreekRhythmEnemy3D candidate in candidates)
            {
                if (candidate == null || candidate == creekSpirit) continue;
                candidate.gameObject.SetActive(false);
                Destroy(candidate.gameObject);
                Debug.LogWarning("[MeixiCombat] Removed duplicate runtime creek spirit.");
            }
        }

        private void Update()
        {
            var keyboard = Keyboard.current;
            var mouse = Mouse.current;

            // Runtime-created route objects can disappear when Unity recompiles
            // scripts while Play Mode is open. Keep the entrance deterministic in
            // the Editor as well as in a standalone build.
            if (Time.unscaledTime >= nextRouteIntegrityCheckAt)
            {
                nextRouteIntegrityCheckAt = Time.unscaledTime + 2f;
                MeixiSecondChapterRoute.Ensure(this);
            }

            if (toastPanel != null && Time.unscaledTime > toastUntil) toastPanel.SetActive(false);
            if (!started && titlePanel != null && titlePanel.activeSelf) UpdateTitleSaveMenuVisibility();

            if (titleCaptureWhenReady && titleScreenFx != null && titleScreenFx.IsReady)
            {
                titleCaptureWhenReady = false;
                captureAt = Time.unscaledTime + 0.12f;
            }
            if (openingCaptureWhenReady && titleScreenFx != null && titleScreenFx.IsReady)
            {
                openingCaptureWhenReady = false;
                StartAdventure();
                captureAt = Time.unscaledTime + 1.15f;
            }

            if (captureMode && Time.unscaledTime >= captureAt)
            {
                if (!captureDone)
                {
                    string screenshot = Path.Combine(Directory.GetParent(Application.dataPath)?.FullName ?? Application.dataPath, captureFileName);
                    CaptureFrame(screenshot);
                    captureDone = true;
                    captureAt = Time.unscaledTime + 1.5f;
                    Debug.Log("梅溪运行截图已写入：" + screenshot);
                }
                else
                {
                    Application.Quit();
                }
            }

            if (!paused && !sceneTransitionActive && chapterPanel.activeSelf && Time.unscaledTime >= chapterCardUntil)
            {
                FinishChapterCardWithTransition();
            }

            if (!paused && openingActive) UpdateOpeningDiary();
            // Keep the DSP-driven combat HUD frozen while paused. The combat
            // timeline is shifted when resuming so a beat cannot expire behind
            // the pause menu.
            if (!paused && chapterOneCombatActive) UpdateChapterOneCombatHud();
            if ((rhythmAutoValidation || rhythmFinisherValidation || meleeAutoValidation || rangedCounterValidation || wrongMeleeValidation) && chapterOneCombatActive)
                UpdateAutomatedRhythmValidation();

            if (started && hudPanel.activeSelf) UpdateNavigation();

            // Typewriter playback is visual state and must not depend on an attached
            // keyboard (batch captures, controllers-only setups and remote play can all
            // have Keyboard.current == null).
            if (!paused && dialogueOpen) UpdateTypewriter();

            if (sceneTransitionActive) return;

            if (terminalPanel != null && terminalPanel.activeSelf)
            {
                if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
                {
                    CloseTerminal();
                    return;
                }
                if (keyboard != null && (keyboard.enterKey.wasPressedThisFrame ||
                                         keyboard.numpadEnterKey.wasPressedThisFrame))
                {
                    SubmitTerminalCommand();
                    return;
                }
                if (mouse != null && mouse.leftButton.wasPressedThisFrame)
                {
                    Vector2 pointer = mouse.position.ReadValue();
                    if (IsPointerInsideButton(terminalSubmitButton, pointer)) SubmitTerminalCommand();
                    else if (IsPointerInsideButton(terminalCloseButton, pointer)) CloseTerminal();
                }
                return;
            }

            if (titleDeleteConfirmPanel != null && titleDeleteConfirmPanel.activeSelf)
            {
                if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame) CloseTitleConfirmation();
                return;
            }

            // Starting the game must not depend on EventSystem/UI module setup.
            // A direct Input System path also handles a title button whose visual
            // children intercept pointer raycasts in the authored scene.
            bool pointerInsideTitleButton = mouse != null &&
                                            (IsPointerInsideButton(titleNewJourneyButton, mouse.position.ReadValue()) ||
                                             IsPointerInsideButton(titleContinueButton, mouse.position.ReadValue()) ||
                                             IsPointerInsideButton(titleDeleteSaveButton, mouse.position.ReadValue()));
            bool startRequested = !started && (titleScreenFx == null || titleScreenFx.IsReady) &&
                                  ((keyboard != null && (keyboard.enterKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame)) ||
                                   (mouse != null && mouse.leftButton.wasPressedThisFrame && !pointerInsideTitleButton));
            if (startRequested)
            {
                HandleTitlePrimaryAction();
                return;
            }

            if (gameplayGuideOpen)
            {
                if (keyboard != null && (keyboard.enterKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame ||
                                         keyboard.eKey.wasPressedThisFrame))
                {
                    DismissGameplayGuide();
                    return;
                }
                if (mouse != null && mouse.leftButton.wasPressedThisFrame &&
                    IsPointerInsideButton(gameplayGuideContinueButton, mouse.position.ReadValue()))
                {
                    DismissGameplayGuide();
                }
                return;
            }

            if (phaseThreeGuideOpen)
            {
                bool confirmPressed = keyboard != null &&
                    (keyboard.enterKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame ||
                     keyboard.eKey.wasPressedThisFrame);
                if (confirmPressed || (mouse != null && mouse.leftButton.wasPressedThisFrame))
                    DismissPhaseThreeGuide();
                return;
            }

            // Pause input must run before any full-screen presentation or pointer
            // fallback. While paused, only the pause menu's EventSystem buttons are
            // allowed to receive clicks; the scene UI underneath stays inert.
            if (keyboard != null && started && !finishingStrike && keyboard.escapeKey.wasPressedThisFrame)
            {
                if (pauseSettingsOpen)
                {
                    ClosePauseSettings();
                    return;
                }
                TogglePause();
                return;
            }
            if (paused && mouse != null && mouse.leftButton.wasPressedThisFrame)
            {
                Vector2 pointer = mouse.position.ReadValue();
                // Direct pointer fallback: the pause menu remains usable even if
                // the InputSystem UI module loses its mouse bindings.
                if (pauseSettingsOpen)
                {
                    if (IsPointerInsideButton(pauseSettingsBackButton, pointer)) ClosePauseSettings();
                    else if (IsPointerInsideButton(pauseVolumeDownButton, pointer)) AdjustMasterVolume(-.10f);
                    else if (IsPointerInsideButton(pauseVolumeUpButton, pointer)) AdjustMasterVolume(.10f);
                    else if (IsPointerInsideButton(pauseTerminalButton, pointer)) OpenTerminal();
                }
                else
                {
                    if (IsPointerInsideButton(pauseResumeButton, pointer)) TogglePause();
                    else if (IsPointerInsideButton(pauseSettingsButton, pointer)) OpenPauseSettings();
                    else if (IsPointerInsideButton(pauseRestartButton, pointer)) RestartFromPause();
                    else if (IsPointerInsideButton(pauseQuitButton, pointer)) QuitGame();
                }
                return;
            }
            if (paused) return;
            if (defeatOpen)
            {
                if (keyboard != null && keyboard.enterKey.wasPressedThisFrame)
                {
                    RestartAfterDefeat();
                    return;
                }
                if (mouse != null && mouse.leftButton.wasPressedThisFrame)
                {
                    Vector2 pointer = mouse.position.ReadValue();
                    if (IsPointerInsideButton(defeatRetryButton, pointer)) RestartAfterDefeat();
                    else if (IsPointerInsideButton(defeatQuitButton, pointer)) QuitGame();
                }
                return;
            }

            // Independent pointer path for the ending screen. It deliberately does
            // not rely on EventSystem so the visible buttons remain clickable even
            // if a runtime UI action module loses its mouse bindings.
            if (complete && endingPanel.activeSelf && mouse != null && mouse.leftButton.wasPressedThisFrame)
            {
                Vector2 pointer = mouse.position.ReadValue();
                if (IsPointerInsideButton(endingNextChapterButton, pointer))
                {
                    Debug.Log("[MeixiUI] Direct pointer accepted: second chapter.");
                    LoadSecondChapter();
                    return;
                }
                if (IsPointerInsideButton(endingReplayButton, pointer))
                {
                    Debug.Log("[MeixiUI] Direct pointer accepted: replay.");
                    RestartFirstChapter();
                    return;
                }
            }

            if (openingActive && mouse != null && mouse.leftButton.wasPressedThisFrame)
            {
                AdvanceOpeningDiary();
                return;
            }

            if (keyboard == null) return;

            // Every blocking presentation can be advanced directly from the
            // keyboard. This prevents the opening/card sequence from looking like
            // a frozen controller when the player immediately presses movement.
            if (openingActive && AnyPresentationAdvancePressed(keyboard))
            {
                AdvanceOpeningDiary();
                return;
            }
            if (chapterPanel.activeSelf && AnyPresentationAdvancePressed(keyboard))
            {
                FinishChapterCardWithTransition();
                return;
            }

            if (complete && endingPanel.activeSelf)
            {
                if (keyboard.enterKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame ||
                    keyboard.eKey.wasPressedThisFrame)
                    LoadSecondChapter();
                return;
            }

            if (CinematicInputLocked) return;

            if (dialogueOpen)
            {
                if (keyboard.eKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame || keyboard.enterKey.wasPressedThisFrame)
                {
                    AdvanceDialogue();
                }
                return;
            }

            if (rhythmActive)
            {
                UpdateRhythm(keyboard);
                return;
            }

            if (CanPlayerMove && !chapterOneCombatActive) UpdateInteraction(keyboard);
        }

        private void OnApplicationFocus(bool focused)
        {
            if (!focused) return;
            if (!paused && !finishingStrike) Time.timeScale = 1f;
            if (!finishingStrike) cinematicInputLockedUntil = Mathf.Min(cinematicInputLockedUntil, Time.unscaledTime + .12f);
            CharacterController activeController = playerTransform != null
                ? playerTransform.GetComponent<CharacterController>()
                : null;
            if (activeController != null && !activeController.enabled) activeController.enabled = true;

            bool needsPointer = !started || dialogueOpen || complete || paused;
            Cursor.lockState = needsPointer ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = needsPointer;
            player?.ResetInputTransientState();
            orbitCamera?.ClearInputAfterFocus();
        }

        private void BuildMaterials()
        {
            earth = MakeMaterial("湿润泥土", new Color(0.25f, 0.19f, 0.12f), 0.02f);
            grass = MakeMaterial("梅溪草地", new Color(0.16f, 0.34f, 0.20f), 0.03f);
            stone = MakeMaterial("青石", new Color(0.28f, 0.34f, 0.36f), 0.28f);
            wood = MakeMaterial("古木", new Color(0.30f, 0.14f, 0.07f), 0.08f);
            roof = MakeMaterial("青瓦", new Color(0.10f, 0.16f, 0.19f), 0.32f);
            leaves = MakeMaterial("竹叶", new Color(0.11f, 0.40f, 0.21f), 0.05f);
            water = MakeMaterial("溪水", new Color(0.08f, 0.46f, 0.62f, 0.78f), 0.88f, false, true);
            gold = MakeMaterial("魂音金", new Color(1f, 0.56f, 0.12f), 0.46f, true);
            cyan = MakeMaterial("声纹青", new Color(0.14f, 0.80f, 0.94f), 0.42f, true, true);
            noise = MakeMaterial("噪音紫", new Color(0.30f, 0.07f, 0.42f), 0.22f, true);
            clay = MakeMaterial("陶土", new Color(0.56f, 0.20f, 0.10f), 0.08f);
            villageAtlas = MakeMaterial("开源古村模型图集", Color.white, 0.18f);
            Texture2D atlasTexture = Resources.Load<Texture2D>("Models/Village/hexagons_medieval");
            if (atlasTexture != null)
            {
                villageAtlas.mainTexture = atlasTexture;
                if (villageAtlas.HasProperty("_BaseMap")) villageAtlas.SetTexture("_BaseMap", atlasTexture);
            }

            ApplySurfaceTexture(grass, "Textures/Realistic/Generated/MeixiDampVillageGround", new Vector2(6.4f, 6.4f), new Color(0.82f, 0.88f, 0.78f));
            ApplySurfaceTexture(earth, "Textures/Realistic/Generated/MeixiDampVillageGround", new Vector2(4.8f, 4.8f), new Color(0.76f, 0.66f, 0.52f));
            ApplySurfaceTexture(stone, "Textures/Realistic/MossyBluestone", new Vector2(2.2f, 2.2f), new Color(0.80f, 0.88f, 0.90f));
        }

        private static void ApplySurfaceTexture(Material material, string resourcePath, Vector2 tiling, Color tint)
        {
            Texture2D texture = Resources.Load<Texture2D>(resourcePath);
            if (material == null || texture == null) return;
            texture.wrapMode = TextureWrapMode.Repeat;
            texture.filterMode = FilterMode.Trilinear;
            texture.anisoLevel = 8;
            material.color = tint;
            material.mainTexture = texture;
            material.mainTextureScale = tiling;
            if (material.HasProperty("_BaseMap")) material.SetTexture("_BaseMap", texture);
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", tint);
        }

        private void LoadGeneratedVfxAssets()
        {
            Texture2D beatTexture = Resources.Load<Texture2D>("Textures/Generated/MeixiBeatSigil");
            finalSlashTexture = Resources.Load<Texture2D>("Textures/Generated/MeixiFinalSlash");
            if (beatTexture != null)
                beatSigilSprite = Sprite.Create(beatTexture, new Rect(0f, 0f, beatTexture.width, beatTexture.height), new Vector2(0.5f, 0.5f), 100f);
        }

        private Material MakeMaterial(string label, Color color, float smoothness, bool emission = false, bool transparent = false)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            if (shader == null)
            {
                var probe = GameObject.CreatePrimitive(PrimitiveType.Cube);
                shader = probe.GetComponent<Renderer>().sharedMaterial.shader;
                Destroy(probe);
            }
            var material = new Material(shader) { name = label, color = color };
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", smoothness);
            if (emission && material.HasProperty("_EmissionColor"))
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", color * 2.1f);
            }
            if (transparent)
            {
                if (material.HasProperty("_Surface")) material.SetFloat("_Surface", 1f);
                if (material.HasProperty("_SrcBlend")) material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
                if (material.HasProperty("_DstBlend")) material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                material.renderQueue = 3000;
            }
            return material;
        }

        private void BuildAudio()
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.spatialBlend = 0f;
            musicSource = gameObject.AddComponent<AudioSource>();
            musicSource.spatialBlend = 0f;
            musicSource.loop = true;
            musicSource.volume = 0.36f;
            lowTone = CreateTone("鼓点", 196f, 0.18f, 0.38f);
            highTone = CreateTone("铃音", 440f, 0.15f, 0.30f);
            successTone = CreateTone("魂音共鸣", 660f, 0.72f, 0.30f);
            rhythmSong = CreateMeixiRhythmSong();
            chapterOneExplorationMusic = Resources.Load<AudioClip>("Audio/ChapterOneExploration");
            chapterOneMusic = Resources.Load<AudioClip>("Audio/ChapterOneBattle") ?? rhythmSong;
            chapterOnePhaseThreeMusic = Resources.Load<AudioClip>("Audio/ChapterOnePhaseThree") ?? chapterOneMusic;
            chapterOneBattleTimeline = ChapterOneBattleTimeline.Load();
            phaseThreeBattleTimeline = ChapterOneBattleTimeline.Load("Audio/ChapterOnePhaseThreeBeatMap");
            if (phaseThreeBattleTimeline == null || !phaseThreeBattleTimeline.IsValid)
                phaseThreeBattleTimeline = ChapterOneBattleTimeline.LoadFromFile(
                    Path.Combine(Application.streamingAssetsPath, "ChapterOnePhaseThreeBeatMap.json"));
            if (chapterOnePhaseThreeMusic == chapterOneMusic)
                StartCoroutine(LoadPhaseThreeAudioFromStreamingAssets());
        }

        private IEnumerator LoadPhaseThreeAudioFromStreamingAssets()
        {
            string filePath = Path.Combine(Application.streamingAssetsPath, "ChapterOnePhaseThree.mp3");
            if (!File.Exists(filePath)) yield break;
            using (UnityWebRequest request = UnityWebRequestMultimedia.GetAudioClip(new Uri(filePath).AbsoluteUri, AudioType.MPEG))
            {
                yield return request.SendWebRequest();
                if (request.result != UnityWebRequest.Result.Success) yield break;
                AudioClip loaded = DownloadHandlerAudioClip.GetContent(request);
                if (loaded != null)
                {
                    loaded.name = "ChapterOnePhaseThree";
                    chapterOnePhaseThreeMusic = loaded;
                    Debug.Log("[MeixiAudio] Phase-three BGM loaded from StreamingAssets.");
                }
            }
        }

        private void PlayChapterOneExplorationMusic()
        {
            if (musicSource == null || chapterOneExplorationMusic == null || chapterOneCombatActive) return;
            if (musicSource.isPlaying && musicSource.clip == chapterOneExplorationMusic) return;
            musicSource.Stop();
            musicSource.clip = chapterOneExplorationMusic;
            musicSource.loop = true;
            musicSource.pitch = 1f;
            musicSource.volume = 0.30f;
            musicSource.time = 0f;
            musicSource.Play();
            Debug.Log($"[MeixiAudio] Chapter-one exploration BGM started: {chapterOneExplorationMusic.name}");
        }

        private AudioClip CreateTone(string label, float frequency, float duration, float volume)
        {
            const int rate = 44100;
            int length = Mathf.CeilToInt(rate * duration);
            float[] data = new float[length];
            for (int i = 0; i < length; i++)
            {
                float t = (float)i / rate;
                float envelope = Mathf.Pow(Mathf.Sin(Mathf.PI * i / length), 1.4f);
                data[i] = (Mathf.Sin(2f * Mathf.PI * frequency * t) + Mathf.Sin(2f * Mathf.PI * frequency * 1.5f * t) * 0.18f) * envelope * volume;
            }
            var clip = AudioClip.Create(label, length, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip CreateMeixiRhythmSong()
        {
            const int rate = 44100;
            const float baseBpm = 105f;
            const float duration = 32f;
            int length = Mathf.CeilToInt(rate * duration);
            float[] data = new float[length];
            float beatLength = 60f / baseBpm;
            float[] pentatonic = { 220f, 247f, 294f, 330f, 392f, 440f, 392f, 330f, 294f, 247f, 220f, 294f, 330f, 392f, 440f, 494f };
            for (int i = 0; i < length; i++)
            {
                float t = (float)i / rate;
                float beat = t / beatLength;
                int step = Mathf.FloorToInt(beat * 2f) % pentatonic.Length;
                float halfBeatPhase = beat * 2f - Mathf.Floor(beat * 2f);
                float beatPhase = beat - Mathf.Floor(beat);
                float barPhase = beat / 4f - Mathf.Floor(beat / 4f);

                float melodyEnvelope = Mathf.Exp(-halfBeatPhase * 3.8f);
                float melody = Mathf.Sin(2f * Mathf.PI * pentatonic[step] * t) * melodyEnvelope * 0.19f;
                melody += Mathf.Sin(2f * Mathf.PI * pentatonic[step] * 2f * t) * melodyEnvelope * 0.045f;

                float kickFrequency = Mathf.Lerp(88f, 48f, beatPhase);
                float kick = Mathf.Sin(2f * Mathf.PI * kickFrequency * beatPhase * beatLength) * Mathf.Exp(-beatPhase * 16f) * 0.34f;

                bool backBeat = Mathf.FloorToInt(beat) % 4 is 1 or 3;
                float noise = Mathf.Sin(i * 0.731f) * Mathf.Sin(i * 0.173f + 1.7f);
                float clap = backBeat ? noise * Mathf.Exp(-beatPhase * 28f) * 0.12f : 0f;

                float bambooPhase = halfBeatPhase;
                float bamboo = Mathf.Sin(2f * Mathf.PI * 1180f * t) * Mathf.Exp(-bambooPhase * 42f) * 0.055f;
                float streamDrone = (Mathf.Sin(2f * Mathf.PI * 110f * t) + Mathf.Sin(2f * Mathf.PI * 165f * t) * 0.4f) * (0.035f + Mathf.Sin(barPhase * Mathf.PI) * 0.012f);
                data[i] = Mathf.Clamp((melody + kick + clap + bamboo + streamDrone) * 0.82f, -0.92f, 0.92f);
            }
            var clip = AudioClip.Create("梅溪五声音阶·电子山歌", length, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private void BuildWorld()
        {
            foreach (var existing in FindObjectsByType<Camera>()) existing.gameObject.SetActive(false);
            foreach (var existing in FindObjectsByType<Light>()) existing.gameObject.SetActive(false);

            RenderSettings.ambientLight = new Color(0.30f, 0.36f, 0.40f);
            RenderSettings.fog = true;
            RenderSettings.fogColor = new Color(0.18f, 0.27f, 0.31f);
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = 0.0065f;
            QualitySettings.shadows = UnityEngine.ShadowQuality.All;
            QualitySettings.shadowDistance = 72f;

            var sunObject = new GameObject("梅溪晨光");
            var sun = sunObject.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = new Color(1f, 0.76f, 0.52f);
            sun.intensity = 1.08f;
            sun.shadows = LightShadows.Soft;
            sunObject.transform.rotation = Quaternion.Euler(42f, -32f, 0f);
            CreateDawnSkybox();
            SetupPostProcessing();

            CreateCube("梅溪村地面", new Vector3(0f, -0.65f, 0f), new Vector3(48f, 1.2f, 48f), grass, true);
            CreateCube("中心土坪", new Vector3(0f, -0.02f, 0f), new Vector3(18f, 0.08f, 16f), earth, false);
            CreateBoundary();
            CreateVillagePaths();
            CreateCreekAndBridge();
            CreateHouses();
            CreateTerraces();
            CreateVegetation();
            CreateVillageSetDressing();
            CreateLanterns();
            CreateAmbientFireflies();
            try
            {
                CreatePlayer();
            }
            catch (Exception exception)
            {
                Debug.LogError("林默角色初始化失败，已启用安全相机与备用角色：" + exception);
                GameObject existingPlayer = GameObject.Find("林默_第三人称角色");
                if (existingPlayer != null) playerTransform = existingPlayer.transform;
            }
            if (playerTransform == null) CreateEmergencyPlayer();
            CreateCamera();
            CreateVillageNpcs();

            CreateSoulNode(0, nodePositions[0]);
            CreateCreekSpirit(new Vector3(-12.4f, 0.05f, -5.6f));
            MeixiSecondChapterRoute.Ensure(this);
            MeixiChapterOneEnvironment.Ensure();
        }

        private void CreateBoundary()
        {
            CreateInvisibleBoundary("北山界", new Vector3(0f, 2f, 24f), new Vector3(50f, 5f, 1f));
            CreateInvisibleBoundary("南山界", new Vector3(0f, 2f, -24f), new Vector3(50f, 5f, 1f));
            CreateInvisibleBoundary("东山界", new Vector3(24f, 2f, 0f), new Vector3(1f, 5f, 50f));
            CreateInvisibleBoundary("西山界", new Vector3(-24f, 2f, 0f), new Vector3(1f, 5f, 50f));
            CreateBackdropMountains();
        }

        private void CreateVillagePaths()
        {
            Vector3[] waypoints =
            {
                Vector3.zero, new(-3.2f, 0f, -2.4f), new(-8.5f, 0f, -6.8f), new(-10.5f, 0f, -10.3f)
            };
            for (int segment = 0; segment < waypoints.Length - 1; segment++)
            {
                float distance = Vector3.Distance(waypoints[segment], waypoints[segment + 1]);
                int count = Mathf.CeilToInt(distance / 1.55f);
                for (int i = 0; i <= count; i++)
                {
                    float t = (float)i / count;
                    Vector3 p = Vector3.Lerp(waypoints[segment], waypoints[segment + 1], t);
                    p.y = 0.03f;
                    p.x += Mathf.Sin(i * 2.7f + segment) * 0.18f;
                    var slab = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    slab.name = "圆润青石踏步";
                    slab.transform.position = p;
                    slab.transform.localScale = new Vector3(0.72f, 0.052f, 0.54f);
                    slab.transform.rotation = Quaternion.Euler(0f, (segment * 19f + i * 13f) % 360f, 0f);
                    slab.GetComponent<Renderer>().material = stone;
                    slab.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.On;
                }
            }

            // 村口与生活区使用连续但不规则的湿青石，避免路线像漂浮的按钮。
            for (int i = 0; i < 12; i++)
            {
                float x = -1.5f + i * 1.28f;
                float z = 4.3f + Mathf.Sin(i * 0.82f) * 0.42f;
                var slab = GameObject.CreatePrimitive(PrimitiveType.Cube);
                slab.name = "村口湿青石";
                slab.transform.position = new Vector3(x, 0.035f, z);
                slab.transform.localScale = new Vector3(1.16f + (i % 3) * 0.10f, 0.07f, 0.88f);
                slab.transform.rotation = Quaternion.Euler(0f, Mathf.Sin(i * 1.7f) * 7f, 0f);
                slab.GetComponent<Renderer>().material = stone;
            }

        }

        private void CreateCreekAndBridge()
        {
            for (int i = 0; i < 8; i++)
            {
                var stream = CreateCube("梅溪水面", new Vector3(-15f + i * 4.3f, 0.025f, -11f + Mathf.Sin(i * 0.9f) * 2f), new Vector3(5.2f, 0.045f, 5.1f), water, false);
                stream.transform.rotation = Quaternion.Euler(0f, Mathf.Sin(i) * 14f, 0f);
            }
            GameObject bridgeCollision = CreateCube("石拱桥隐形碰撞", new Vector3(-10.5f, 0.65f, -10.3f), new Vector3(3.2f, 0.45f, 8f), stone, true);
            bridgeCollision.GetComponent<Renderer>().enabled = false;
            CreateVillageModel("building_bridge_A", new Vector3(-10.5f, 0.32f, -10.3f), 0f, 2.15f, "梅溪石桥");
            for (int i = 0; i < 7; i++)
            {
                CreateVillageModel(i % 2 == 0 ? "waterplant_A" : "waterlily_A", new Vector3(-18f + i * 4.5f, 0f, -12f + Mathf.Sin(i) * 1.5f), i * 37f, 0.45f, "溪畔水草");
            }
            for (int i = 0; i < 18; i++)
            {
                float x = -20f + i * 2.45f;
                float centerZ = -11f + Mathf.Sin((x + 15f) * 0.21f) * 2f;
                float side = i % 2 == 0 ? -1f : 1f;
                Vector3 bank = new(x, 0f, centerZ + side * (2.65f + (i % 3) * 0.22f));
                CreateVillageModel(i % 3 == 0 ? "rock_single_B" : "rock_single_A", bank, i * 41f, 0.55f + (i % 4) * 0.11f, "溪岸苔石", stone);
            }
        }

        private void CreateHouses()
        {
            CreateHouse(new Vector3(-17f, 1.7f, 12f), new Vector3(7f, 4.2f, 5.5f), 20f);
            CreateHouse(new Vector3(-9f, 1.45f, 17f), new Vector3(5.5f, 3.7f, 4.7f), -12f);
            CreateHouse(new Vector3(-20f, 1.25f, -1f), new Vector3(5f, 3.4f, 4.4f), 70f);
        }

        private void CreateHouse(Vector3 position, Vector3 size, float yaw)
        {
            string[] models = { "building_home_A_red", "building_home_B_red", "building_watermill_red", "building_home_A_red", "building_blacksmith_red" };
            int index = Mathf.Abs(Mathf.RoundToInt(position.x + position.z)) % models.Length;
            GameObject imported = CreateVillageModel(models[index], new Vector3(position.x, 0f, position.z), yaw, size.y * 1.28f, "闽清古厝");
            if (imported != null)
            {
                Bounds worldBounds = CalculateBounds(imported);
                var collisionProxy = new GameObject("古厝独立碰撞代理");
                collisionProxy.transform.position = worldBounds.center;
                var collider = collisionProxy.AddComponent<BoxCollider>();
                collider.center = Vector3.zero;
                collider.size = new Vector3(worldBounds.size.x * 0.76f, worldBounds.size.y * 0.92f, worldBounds.size.z * 0.76f);
                foreach (Renderer renderer in imported.GetComponentsInChildren<Renderer>(true))
                    renderer.enabled = false;
                CreateVillageModel("barrel", new Vector3(position.x + 2f, 0f, position.z - 2f), yaw + 20f, 0.9f, "古厝陶缸");
                CreateVillageModel(index % 2 == 0 ? "wheelbarrow" : "resource_lumber", new Vector3(position.x - 2f, 0f, position.z + 1.8f), yaw - 18f, 0.9f, "村居生活物件");
                return;
            }

            var root = new GameObject("闽清古厝");
            root.transform.position = position;
            root.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            CreateChildCube("夯土墙", root.transform, Vector3.zero, size, clay, true);
            CreateChildCube("木门", root.transform, new Vector3(0f, -0.35f, -size.z * 0.51f), new Vector3(1.4f, 2.6f, 0.16f), wood, false);
            CreateChildCube("窗灯左", root.transform, new Vector3(-size.x * 0.28f, 0.2f, -size.z * 0.515f), new Vector3(1.15f, 1.3f, 0.12f), gold, false);
            CreateChildCube("窗灯右", root.transform, new Vector3(size.x * 0.28f, 0.2f, -size.z * 0.515f), new Vector3(1.15f, 1.3f, 0.12f), gold, false);
            var roofLeft = CreateChildCube("青瓦坡一", root.transform, new Vector3(-size.x * 0.24f, size.y * 0.60f, 0f), new Vector3(size.x * 0.58f, 0.28f, size.z * 1.25f), roof, false);
            roofLeft.transform.localRotation = Quaternion.Euler(0f, 0f, 16f);
            var roofRight = CreateChildCube("青瓦坡二", root.transform, new Vector3(size.x * 0.24f, size.y * 0.60f, 0f), new Vector3(size.x * 0.58f, 0.28f, size.z * 1.25f), roof, false);
            roofRight.transform.localRotation = Quaternion.Euler(0f, 0f, -16f);
        }

        private void CreateKiln()
        {
            var kiln = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            kiln.name = "陶瓷龙窑";
            kiln.transform.position = new Vector3(20f, 2.0f, -4f);
            kiln.transform.localScale = new Vector3(3.8f, 2.2f, 3.8f);
            kiln.GetComponent<Renderer>().material = stone;
            CreateCube("窑门火光", new Vector3(20f, 1.15f, -7.75f), new Vector3(2.0f, 2.0f, 0.18f), gold, false);
            var fire = new GameObject("陶火点光").AddComponent<Light>();
            fire.type = LightType.Point;
            fire.range = 6f;
            fire.intensity = 1.05f;
            fire.color = new Color(1f, 0.25f, 0.06f);
            fire.transform.position = new Vector3(20f, 1.4f, -7f);
        }

        private void CreateVegetation()
        {
            for (int i = 0; i < 18; i++)
            {
                float angle = i * 137.5f * Mathf.Deg2Rad;
                float radius = 20.5f + (i % 4) * 1.15f;
                Vector3 p = new(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
                if (i % 3 == 0) CreateOliveTree(p);
                else CreateBambooCluster(p, 2 + i % 2);
            }
            Vector3[] creekBamboo =
            {
                new(-19.0f, 0f, -3.5f), new(-18.0f, 0f, -11.8f), new(-6.2f, 0f, -14.8f),
                new(-2.8f, 0f, -10.8f), new(-18.5f, 0f, -16.2f), new(-10.0f, 0f, 4.2f)
            };
            foreach (Vector3 position in creekBamboo) CreateBambooCluster(position, 3);
        }

        private void CreateBambooCluster(Vector3 position, int count)
        {
            for (int i = 0; i < count; i++)
            {
                Vector3 p = position + new Vector3(Mathf.Sin(i * 2.3f) * 0.55f, 0f, Mathf.Cos(i * 1.7f) * 0.55f);
                var stem = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                stem.name = "竹林";
                stem.transform.position = p + Vector3.up * (2.2f + i * 0.18f);
                stem.transform.localScale = new Vector3(0.11f, 2.2f + i * 0.18f, 0.11f);
                stem.GetComponent<Renderer>().material = leaves;
                stem.GetComponent<Renderer>().enabled = false;
                Destroy(stem.GetComponent<Collider>());
                var crown = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                crown.name = "竹叶云";
                crown.transform.position = p + Vector3.up * (4.7f + i * 0.15f);
                crown.transform.localScale = new Vector3(1.1f, 0.55f, 1.1f);
                crown.GetComponent<Renderer>().material = leaves;
                crown.GetComponent<Renderer>().enabled = false;
                Destroy(crown.GetComponent<Collider>());
            }
        }

        private void CreateOliveTree(Vector3 position)
        {
            var root = new GameObject("闽清橄榄树组");
            root.transform.position = position;

            var trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            trunk.name = "闽清橄榄树";
            trunk.transform.SetParent(root.transform, false);
            trunk.transform.localPosition = Vector3.up * 1.18f;
            trunk.transform.localScale = new Vector3(0.38f, 1.18f, 0.38f);
            trunk.GetComponent<Renderer>().material = wood;

            var upperTrunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            upperTrunk.name = "橄榄树上段树干";
            upperTrunk.transform.SetParent(root.transform, false);
            upperTrunk.transform.localPosition = Vector3.up * 2.75f;
            upperTrunk.transform.localScale = new Vector3(.25f, .72f, .25f);
            upperTrunk.transform.localRotation = Quaternion.Euler(0f, 18f, 3f);
            upperTrunk.GetComponent<Renderer>().material = wood;
            Destroy(upperTrunk.GetComponent<Collider>());

            // Flared roots conceal the primitive trunk cut and visibly anchor the tree.
            for (int rootIndex = 0; rootIndex < 4; rootIndex++)
            {
                float angle = rootIndex * 90f + 18f;
                Vector3 direction = Quaternion.Euler(0f, angle, 0f) * new Vector3(0f, -.10f, .72f);
                var rootBranch = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                rootBranch.name = "橄榄树根系";
                rootBranch.transform.SetParent(root.transform, false);
                rootBranch.transform.localPosition = Vector3.up * .12f + direction * .45f;
                rootBranch.transform.localRotation = Quaternion.FromToRotation(Vector3.up, direction.normalized);
                rootBranch.transform.localScale = new Vector3(.10f, direction.magnitude * .50f, .10f);
                rootBranch.GetComponent<Renderer>().material = wood;
                Destroy(rootBranch.GetComponent<Collider>());
            }

            for (int i = 0; i < 8; i++)
            {
                var crown = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                crown.name = "橄榄树冠";
                crown.transform.SetParent(root.transform, false);
                float angle = i * 137.5f * Mathf.Deg2Rad;
                crown.transform.localPosition = new Vector3(Mathf.Cos(angle) * (1.02f + (i % 3) * .16f),
                    3.56f + (i % 4) * .31f, Mathf.Sin(angle) * (.82f + (i % 2) * .18f));
                crown.transform.localScale = new Vector3(1.42f + (i % 3) * .24f,
                    .72f + (i % 4) * .09f, 1.18f + ((i + 1) % 3) * .19f);
                crown.transform.localRotation = Quaternion.Euler(i * 7f, i * 29f, (i % 2 == 0 ? -1f : 1f) * 8f);
                crown.GetComponent<Renderer>().material = leaves;
                Destroy(crown.GetComponent<Collider>());

                Vector3 start = root.transform.position + Vector3.up * (2.10f + (i % 3) * .20f);
                Vector3 end = crown.transform.position - Vector3.up * .18f;
                Vector3 direction = end - start;
                var branch = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                branch.name = "橄榄树自然分枝";
                branch.transform.SetParent(root.transform, true);
                branch.transform.position = (start + end) * 0.5f;
                branch.transform.rotation = Quaternion.FromToRotation(Vector3.up, direction.normalized);
                branch.transform.localScale = new Vector3(0.082f + (i % 3) * .012f, direction.magnitude * 0.5f,
                    0.082f + (i % 3) * .012f);
                branch.GetComponent<Renderer>().material = wood;
                Destroy(branch.GetComponent<Collider>());
            }

            // The merged chapter-one shell owns the visible trunk, branches and
            // alpha-clipped foliage. This legacy hierarchy remains only as collision
            // compatibility for authored scenes and must never draw over that shell.
            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
                renderer.enabled = false;
        }

        private void CreateTerraces()
        {
            for (int i = 0; i < 4; i++)
            {
                float width = 13f - i * 1.5f;
                float y = i * 0.48f;
                float z = 15.8f + i * 1.62f;
                CreateCube("梯田湿土台", new Vector3(16.8f, y, z), new Vector3(width, 0.44f, 2.75f), earth, true);
                CreateCube("梯田浅水", new Vector3(16.8f, y + 0.25f, z), new Vector3(width - 0.55f, 0.035f, 2.22f), water, false);
                for (int rockIndex = 0; rockIndex < Mathf.FloorToInt(width / 1.25f); rockIndex++)
                {
                    float x = 16.8f - width * 0.46f + rockIndex * 1.18f;
                    CreateVillageModel(rockIndex % 2 == 0 ? "rock_single_A" : "rock_single_B",
                        new Vector3(x, y + 0.13f, z - 1.42f), rockIndex * 29f + i * 13f, 0.36f + (rockIndex % 3) * 0.05f, "梯田干砌石坎", stone);
                }
            }
        }

        private void CreateVillageSetDressing()
        {
            CreateVillageModel("building_well_red", new Vector3(7.6f, 0f, 7.4f), -18f, 1.45f, "梅溪古井");
            CreateVillageModel("wheelbarrow", new Vector3(4.9f, 0f, 8.7f), 32f, 0.95f, "采茶推车");
            CreateVillageModel("resource_lumber", new Vector3(-5.8f, 0f, 10.7f), 78f, 1.05f, "晒场木料");
            CreateVillageModel("barrel", new Vector3(5.1f, 0f, 6.9f), -12f, 0.92f, "井边水缸");
            CreateVillageModel("sack", new Vector3(6.0f, 0f, 7.0f), 19f, 0.78f, "谷物麻袋");
            CreateVillageModel("sack", new Vector3(6.6f, 0f, 6.8f), -11f, 0.68f, "谷物麻袋");
            CreateVillageModel("rock_single_B", new Vector3(1.8f, 0f, 8.8f), 41f, 1.25f, "村口卧石", stone);

            // 竹木围栏勾出生活区边界，不使用大面积隐形墙切断玩家视线。
            for (int i = 0; i < 8; i++)
            {
                Vector3 p = new Vector3(-4.7f + i * 1.35f, 0f, 11.7f + Mathf.Sin(i * 0.6f) * 0.2f);
                var post = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                post.name = "旧竹篱笆柱";
                post.transform.position = p + Vector3.up * 0.62f;
                post.transform.localScale = new Vector3(0.075f, 0.62f, 0.075f);
                post.transform.rotation = Quaternion.Euler(Mathf.Sin(i) * 4f, 0f, Mathf.Cos(i) * 5f);
                post.GetComponent<Renderer>().material = wood;
                Destroy(post.GetComponent<Collider>());
                if (i < 7)
                {
                    var rail = CreateCube("旧竹篱笆横杆", p + new Vector3(0.68f, 0.73f, 0f), new Vector3(1.45f, 0.07f, 0.07f), wood, false);
                    rail.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Sin(i * 1.8f) * 3f);
                }
            }

            // 短草与蕨类使用现有水草模型，形成溪岸过渡层。
            for (int i = 0; i < 22; i++)
            {
                float angle = i * 137.5f * Mathf.Deg2Rad;
                float radius = 6.5f + (i % 5) * 2.25f;
                Vector3 p = new(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius + 2f);
                if (Mathf.Abs(p.z + 10f) < 3.2f) continue;
                CreateVillageModel("waterplant_A", p, i * 47f, 0.24f + (i % 3) * 0.06f, "湿地蕨草");
            }
        }

        private static void AddTreeCollider(GameObject tree)
        {
            Bounds bounds = CalculateBounds(tree);
            float height = Mathf.Max(1.2f, bounds.size.y * 0.72f);
            var proxy = new GameObject("树干碰撞代理");
            proxy.transform.SetParent(tree.transform, true);
            proxy.transform.position = new Vector3(bounds.center.x, bounds.min.y + height * 0.5f, bounds.center.z);
            proxy.transform.rotation = Quaternion.identity;
            var capsule = proxy.AddComponent<CapsuleCollider>();
            capsule.direction = 1;
            capsule.center = Vector3.zero;
            capsule.height = height;
            capsule.radius = Mathf.Clamp(Mathf.Min(bounds.size.x, bounds.size.z) * 0.16f, 0.18f, 0.48f);
        }

        private void CreateLanterns()
        {
            // 把乡灯留在探索路径两端，避免挡住出生镜头和溪畔战斗视线。
            Vector3[] positions = { new(-7f, 0f, 4.2f), new(-18f, 0f, -9.5f), new(5.8f, 0f, 9.8f) };
            foreach (Vector3 p in positions)
            {
                var post = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                post.name = "旧木乡灯杆";
                post.transform.position = p + Vector3.up * 1.35f;
                post.transform.localScale = new Vector3(0.07f, 1.35f, 0.07f);
                post.GetComponent<Renderer>().material = wood;
                Destroy(post.GetComponent<Collider>());
                var lamp = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                lamp.name = "暖色竹编乡灯";
                lamp.transform.position = p + Vector3.up * 2.82f;
                lamp.transform.localScale = new Vector3(0.36f, 0.48f, 0.36f);
                lamp.GetComponent<Renderer>().material = gold;
                Destroy(lamp.GetComponent<Collider>());
                var cap = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                cap.name = "乡灯青瓦帽";
                cap.transform.position = p + Vector3.up * 3.28f;
                cap.transform.localScale = new Vector3(0.28f, 0.035f, 0.28f);
                cap.GetComponent<Renderer>().material = roof;
                Destroy(cap.GetComponent<Collider>());
                var point = new GameObject("乡灯光").AddComponent<Light>();
                point.type = LightType.Point;
                point.range = 5.5f;
                point.intensity = 0.75f;
                point.color = new Color(1f, 0.54f, 0.18f);
                point.transform.position = p + Vector3.up * 2.8f;
            }
        }

        private void CreateAmbientFireflies()
        {
            var root = new GameObject("漂浮魂音微粒");
            root.transform.position = new Vector3(0f, 1.5f, 0f);
            var ps = root.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.startLifetime = 9f;
            main.startSpeed = 0.18f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.025f, 0.065f);
            main.startColor = new Color(0.74f, 0.88f, 0.68f, 0.48f);
            main.maxParticles = 90;
            var emission = ps.emission;
            emission.rateOverTime = 5.5f;
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(42f, 5f, 42f);
            ConfigureShardParticleRenderer(ps, gold);
        }

        private void CreatePlayer()
        {
            var root = new GameObject("林默_第三人称角色");
            root.transform.position = respawnPoint;
            playerTransform = root.transform;
            var controller = root.AddComponent<CharacterController>();
            controller.height = 2.05f;
            controller.radius = 0.40f;
            controller.center = new Vector3(0f, 1.02f, 0f);
            controller.stepOffset = 0.38f;

            GameObject characterAsset = LoadPreferredCharacterModel();
            if (characterAsset != null)
            {
                var characterModel = Instantiate(characterAsset, root.transform);
                characterModel.name = "林默_模型";
                characterModel.transform.localPosition = Vector3.zero;
                characterModel.transform.localRotation = Quaternion.identity;
                characterModel.transform.localScale = Vector3.one;
                bool rocketboxPlayer = characterAsset.name == "Male_Adult_20";
                if (rocketboxPlayer) MeixiRealisticCast.PrepareVisual(characterModel, MeixiCastRole.LinMo, true);
                Bounds initialBounds = rocketboxPlayer
                    ? MeixiRealisticCast.CalculateVisibleBounds(characterModel)
                    : CalculateBounds(characterModel);
                float characterScale = initialBounds.size.y > 0.01f ? 1.92f / initialBounds.size.y : 1f;
                characterModel.transform.localScale = Vector3.one * characterScale;
                Bounds scaledBounds = rocketboxPlayer
                    ? MeixiRealisticCast.CalculateVisibleBounds(characterModel)
                    : CalculateBounds(characterModel);
                Vector3 modelCorrection = new(
                    root.transform.position.x - scaledBounds.center.x,
                    root.transform.position.y - scaledBounds.min.y,
                    root.transform.position.z - scaledBounds.center.z);
                characterModel.transform.position += modelCorrection;
                if (!rocketboxPlayer) ApplyCharacterMaterial(characterModel, characterAsset.name == "DefaultMale");

                Animator sourceAnimator = characterAsset.GetComponent<Animator>();
                Animator animator = characterModel.GetComponent<Animator>();
                // UnityEngine.Object can be a "fake null" after an FBX reimport;
                // null-coalescing does not use Unity's overloaded null check.
                if (animator == null) animator = characterModel.AddComponent<Animator>();
                if (sourceAnimator != null && animator.avatar == null) animator.avatar = sourceAnimator.avatar;
                if (animator != null)
                {
                    animator.runtimeAnimatorController = Resources.Load<RuntimeAnimatorController>("Animations/LinMoCombat");
                    animator.applyRootMotion = false;
                    animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                    animator.enabled = animator.runtimeAnimatorController != null;
                }
                // 始终启用骨骼动作保障层：Animator 提供完整动作，保障层在 LateUpdate
                // 明确强化走路摆臂、迈步、攻击和闪避，防止重定向片段失效后角色定格。
                if (characterModel.GetComponent<ProceduralHumanoidFallback>() == null)
                    characterModel.AddComponent<ProceduralHumanoidFallback>();
                CreateSoundCollector(characterModel.transform, animator);
                return;
            }

            var model = new GameObject("林默_模型");
            model.transform.SetParent(root.transform, false);
            var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "外套";
            body.transform.SetParent(model.transform, false);
            body.transform.localPosition = new Vector3(0f, 1.05f, 0f);
            body.transform.localScale = new Vector3(0.62f, 0.82f, 0.62f);
            body.GetComponent<Renderer>().material = MakeMaterial("林默靛蓝外套", new Color(0.10f, 0.28f, 0.48f), 0.22f);
            Destroy(body.GetComponent<Collider>());
            var head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            head.name = "头部";
            head.transform.SetParent(model.transform, false);
            head.transform.localPosition = new Vector3(0f, 2.0f, 0f);
            head.transform.localScale = Vector3.one * 0.52f;
            head.GetComponent<Renderer>().material = MakeMaterial("肤色", new Color(0.76f, 0.54f, 0.40f), 0.16f);
            Destroy(head.GetComponent<Collider>());
            var hair = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            hair.name = "头发";
            hair.transform.SetParent(model.transform, false);
            hair.transform.localPosition = new Vector3(0f, 2.20f, -0.02f);
            hair.transform.localScale = new Vector3(0.54f, 0.32f, 0.54f);
            hair.GetComponent<Renderer>().material = roof;
            Destroy(hair.GetComponent<Collider>());
            CreateChildCube("声音采集器", model.transform, new Vector3(0.34f, 1.05f, -0.30f), new Vector3(0.12f, 0.18f, 0.08f), cyan, false);

        }

        private void CreateSoundCollector(Transform model, Animator animator)
        {
            Transform hand = animator != null && animator.avatar != null && animator.avatar.isHuman
                ? animator.GetBoneTransform(HumanBodyBones.RightHand)
                : null;
            if (hand == null)
            {
                foreach (Transform candidate in model.GetComponentsInChildren<Transform>(true))
                {
                    if (!candidate.name.EndsWith("RightHand", StringComparison.OrdinalIgnoreCase)) continue;
                    hand = candidate;
                    break;
                }
            }
            if (hand == null) return;
            var sampler = new GameObject("腕式声音采集器_世界尺度");
            sampler.AddComponent<WristSamplerFx3D>().Configure(hand, cyan, gold);
        }

        private void CreateVillageNpcs()
        {
            CreateVillageNpc("老村长", "村口老人", new Vector3(2.7f, 0f, 5.8f), 198f, 1.76f,
                new Color(0.72f, 0.77f, 0.70f), VillageNpcStyle.Elder,
                new[]
                {
                    "林默｜您好！我是刚到这里的大学生，是来这边社会实践的，想问一下樟洋村怎么走？",
                    "老村长｜emmmm，我们之前见过吧？",
                    "林默｜没有吧……是不是您记错了？",
                    "老村长｜哦哦哦，抱歉！我的眼睛看不太清了……",
                    "老村长｜樟洋村？我记不太清了……",
                    "林默｜好吧，感谢您，我再问问其他人吧。"
                }, "老村长｜樟洋村……我确实记不太清了。", "村");

            CreateVillageNpc("小孩", "溪边玩耍的孩子", new Vector3(-4.0f, 0f, 2.6f), 34f, 1.46f,
                new Color(0.70f, 0.83f, 0.66f), VillageNpcStyle.Youth,
                new[]
                {
                    "小孩｜溪边有一个亮亮的、会发光的东西！"
                }, "小孩｜就在溪边！它一直亮亮的。", "童");

            CreateVillageNpc("石婶", "沉默的村民", new Vector3(-6.6f, 0f, 8.3f), 145f, 1.64f,
                new Color(0.72f, 0.70f, 0.86f), VillageNpcStyle.Aunt,
                new[]
                {
                    "林默｜您好！我是刚到这里的大学生，是来这边社会实践的，想问一下樟洋村怎么走？",
                    "石婶｜……",
                    "林默｜？您好？",
                    "石婶｜……"
                }, "石婶｜……", "石");
        }

        public void SyncChapterOneStoryNpcs()
        {
            if (villageNpcs.Count == 0)
            {
                villageNpcs.AddRange(FindObjectsByType<VillageNpc3D>(FindObjectsInactive.Include));
            }

            Transform target = playerTransform;
            if (target == null)
            {
                MeixiThirdPersonPlayer scenePlayer = FindAnyObjectByType<MeixiThirdPersonPlayer>(FindObjectsInactive.Include);
                target = scenePlayer != null ? scenePlayer.transform : null;
            }

            foreach (VillageNpc3D npc in villageNpcs)
            {
                if (npc == null) continue;
                string originalName = npc.DisplayName ?? npc.gameObject.name;
                if (originalName.Contains("老村长"))
                {
                    ConfigureStoryNpc(npc, target, "老村长", "村口老人", new[]
                    {
                        "林默｜您好！我是刚到这里的大学生，是来这边社会实践的，想问一下樟洋村怎么走？",
                        "老村长｜emmmm，我们之前见过吧？",
                        "林默｜没有吧……是不是您记错了？",
                        "老村长｜哦哦哦，抱歉！我的眼睛看不太清了……",
                        "老村长｜樟洋村？我记不太清了……",
                        "林默｜好吧，感谢您，我再问问其他人吧。"
                    }, "老村长｜樟洋村……我确实记不太清了。", "村");
                }
                else if (originalName.Contains("阿禾") || originalName.Contains("小孩"))
                {
                    ConfigureStoryNpc(npc, target, "小孩", "溪边玩耍的孩子",
                        new[] { "小孩｜溪边有一个亮亮的、会发光的东西！" },
                        "小孩｜就在溪边！它一直亮亮的。", "童");
                }
                else if (originalName.Contains("桂婶") || originalName.Contains("石婶"))
                {
                    ConfigureStoryNpc(npc, target, "石婶", "沉默的村民", new[]
                    {
                        "林默｜您好！我是刚到这里的大学生，是来这边社会实践的，想问一下樟洋村怎么走？",
                        "石婶｜……",
                        "林默｜？您好？",
                        "石婶｜……"
                    }, "石婶｜……", "石");
                }
            }
        }

        private void ConfigureStoryNpc(VillageNpc3D npc, Transform target, string displayName, string role,
            string[] lines, string repeatLine, string glyph)
        {
            npc.gameObject.name = displayName + "_可交互NPC";
            npc.Configure(this, target, displayName, role, lines, repeatLine, glyph);
            foreach (Text label in npc.GetComponentsInChildren<Text>(true))
            {
                if (label.name == "地标名") label.text = displayName + "  ·  " + role;
            }
        }

        private void CreateVillageNpc(string displayName, string role, Vector3 position, float yaw, float height,
            Color tint, VillageNpcStyle style, string[] firstDialogue, string repeatDialogue, string glyph)
        {
            var root = new GameObject(displayName + "_可交互NPC");
            root.transform.position = position;
            root.transform.rotation = Quaternion.Euler(0f, yaw, 0f);

            MeixiCastRole castRole = style switch
            {
                VillageNpcStyle.Elder => MeixiCastRole.VillageElder,
                VillageNpcStyle.Youth => MeixiCastRole.VillageChild,
                _ => MeixiCastRole.VillageAunt
            };
            GameObject characterAsset = MeixiRealisticCast.Load(castRole) ?? LoadPreferredCharacterModel();
            if (characterAsset != null)
            {
                GameObject model = Instantiate(characterAsset, root.transform);
                model.name = displayName + "_写实人物模型";
                model.transform.localPosition = Vector3.zero;
                model.transform.localRotation = Quaternion.identity;
                model.transform.localScale = Vector3.one;
                bool rocketboxNpc = characterAsset.name is "Male_Adult_03" or "Male_Child_02" or "Female_Adult_12";
                if (rocketboxNpc) MeixiRealisticCast.PrepareVisual(model, castRole, false);
                Bounds initialBounds = rocketboxNpc
                    ? MeixiRealisticCast.CalculateVisibleBounds(model)
                    : CalculateBounds(model);
                float scale = initialBounds.size.y > 0.01f ? height / initialBounds.size.y : 1f;
                model.transform.localScale = Vector3.one * scale;
                Bounds scaledBounds = rocketboxNpc
                    ? MeixiRealisticCast.CalculateVisibleBounds(model)
                    : CalculateBounds(model);
                model.transform.position += new Vector3(position.x - scaledBounds.center.x, position.y - scaledBounds.min.y, position.z - scaledBounds.center.z);
                if (!rocketboxNpc) ApplyNpcCharacterMaterial(model, tint, displayName, characterAsset.name == "DefaultMale");

                Animator sourceAnimator = characterAsset.GetComponent<Animator>();
                Animator animator = model.GetComponent<Animator>();
                if (animator == null) animator = model.AddComponent<Animator>();
                if (sourceAnimator != null && animator.avatar == null) animator.avatar = sourceAnimator.avatar;
                animator.runtimeAnimatorController = Resources.Load<RuntimeAnimatorController>("Animations/LinMoCombat");
                animator.applyRootMotion = false;
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                animator.speed = 0.72f + villageNpcs.Count * 0.05f;
                if (model.GetComponent<ProceduralHumanoidFallback>() == null)
                    model.AddComponent<ProceduralHumanoidFallback>();
                // DefaultMale is a valid Humanoid. Disabling this Animator left every
                // NPC in bind pose in the first Windows frame and on scene restart.
                animator.enabled = animator.runtimeAnimatorController != null;
                if (animator.enabled)
                {
                    animator.Rebind();
                    if (animator.isActiveAndEnabled) animator.Update(0f);
                }
            }

            VillageNpc3D npc = root.AddComponent<VillageNpc3D>();
            npc.Configure(this, playerTransform, displayName, role, firstDialogue, repeatDialogue, glyph);
            villageNpcs.Add(npc);
        }

        private void AddNpcAccessories(Transform root, VillageNpcStyle style, float height)
        {
            if (style is VillageNpcStyle.Elder or VillageNpcStyle.Aunt)
            {
                var brim = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                brim.name = style == VillageNpcStyle.Elder ? "旧毡帽帽檐" : "竹编斗笠";
                brim.transform.SetParent(root, false);
                brim.transform.localPosition = new Vector3(0f, height * 0.94f, 0f);
                brim.transform.localScale = style == VillageNpcStyle.Elder ? new Vector3(0.24f, 0.025f, 0.24f) : new Vector3(0.37f, 0.025f, 0.37f);
                brim.GetComponent<Renderer>().material = style == VillageNpcStyle.Elder ? roof : wood;
                Destroy(brim.GetComponent<Collider>());
            }
            if (style == VillageNpcStyle.Elder)
            {
                var cane = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                cane.name = "老村长竹杖";
                cane.transform.SetParent(root, false);
                cane.transform.localPosition = new Vector3(0.38f, 0.70f, 0.10f);
                cane.transform.localRotation = Quaternion.Euler(6f, 0f, -7f);
                cane.transform.localScale = new Vector3(0.025f, 0.70f, 0.025f);
                cane.GetComponent<Renderer>().material = wood;
                Destroy(cane.GetComponent<Collider>());
            }
            else if (style == VillageNpcStyle.Youth)
            {
                for (int side = -1; side <= 1; side += 2)
                {
                    var ear = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    ear.name = "阿禾耳机";
                    ear.transform.SetParent(root, false);
                    ear.transform.localPosition = new Vector3(side * 0.15f, height * 0.78f, 0.05f);
                    ear.transform.localScale = new Vector3(0.09f, 0.12f, 0.06f);
                    ear.GetComponent<Renderer>().material = cyan;
                    Destroy(ear.GetComponent<Collider>());
                }
            }
            else
            {
                var basket = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                basket.name = "桂婶竹篮";
                basket.transform.SetParent(root, false);
                basket.transform.localPosition = new Vector3(-0.34f, 0.62f, 0.18f);
                basket.transform.localScale = new Vector3(0.23f, 0.22f, 0.23f);
                basket.GetComponent<Renderer>().material = wood;
                Destroy(basket.GetComponent<Collider>());
            }
        }

        private enum VillageNpcStyle
        {
            Elder,
            Youth,
            Aunt
        }

        private void CreateEmergencyPlayer()
        {
            var root = new GameObject("林默_备用角色");
            root.transform.position = respawnPoint;
            var controller = root.AddComponent<CharacterController>();
            controller.height = 2.05f;
            controller.radius = 0.40f;
            controller.center = new Vector3(0f, 1.02f, 0f);
            var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "林默_备用可视模型";
            body.transform.SetParent(root.transform, false);
            body.transform.localPosition = Vector3.up;
            body.GetComponent<Renderer>().material = MakeMaterial("林默备用外套", new Color(0.10f, 0.28f, 0.48f), 0.22f);
            Destroy(body.GetComponent<Collider>());
            playerTransform = root.transform;
        }

        private void CreateCamera()
        {
            var cameraObject = new GameObject("第三人称自由相机");
            var camera = cameraObject.AddComponent<Camera>();
            cameraObject.tag = "MainCamera";
            camera.clearFlags = CameraClearFlags.Skybox;
            camera.backgroundColor = new Color(0.055f, 0.12f, 0.19f);
            camera.fieldOfView = 56f;
            camera.nearClipPlane = 0.08f;
            camera.farClipPlane = 120f;
            camera.allowHDR = true;
            cameraObject.AddComponent<AudioListener>();
            orbitCamera = cameraObject.AddComponent<ThirdPersonOrbitCamera>();
            orbitCamera.Configure(playerTransform);
            cameraObject.transform.position = playerTransform.position + new Vector3(-5f, 4.5f, -6f);

            player = playerTransform.gameObject.AddComponent<MeixiThirdPersonPlayer>();
            player.Configure(this, orbitCamera, playerTransform.Find("林默_模型"));
        }

        private void CreateSoulNode(int index, Vector3 position)
        {
            var root = new GameObject("魂音地标_" + chapterNames[index]);
            root.transform.position = position;
            var core = GameObject.CreatePrimitive(index == 3 ? PrimitiveType.Capsule : PrimitiveType.Sphere);
            core.name = "魂音核心";
            core.transform.SetParent(root.transform, false);
            core.transform.localScale = Vector3.one * 0.34f;
            var renderer = core.GetComponent<Renderer>();
            renderer.material = new Material(index % 2 == 0 ? gold : cyan);
            Destroy(core.GetComponent<Collider>());
            Transform haloA = CreateHalo(root.transform, 0.72f, gold);
            Transform haloB = CreateHalo(root.transform, 0.96f, cyan);
            var node = root.AddComponent<SoulNode3D>();
            node.Configure(this, index, renderer, haloA, haloB);
            nodes.Add(node);
            CreateWorldLabel(root.transform, "聆听溪流", new Vector3(0f, 1.75f, 0f));
            CreateNodeParticles(root.transform);
        }

        private Transform CreateHalo(Transform parent, float radius, Material material)
        {
            var halo = new GameObject("魂音环");
            halo.name = "魂音环";
            halo.transform.SetParent(parent, false);
            for (int i = 0; i < 12; i++)
            {
                float angle = i / 12f * Mathf.PI * 2f;
                var bead = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                bead.name = "环形音粒";
                bead.transform.SetParent(halo.transform, false);
                bead.transform.localPosition = new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0f);
                bead.transform.localScale = Vector3.one * 0.10f;
                bead.GetComponent<Renderer>().material = new Material(material);
                Destroy(bead.GetComponent<Collider>());
            }
            return halo.transform;
        }

        private void CreateNodeParticles(Transform parent)
        {
            var particles = new GameObject("魂音粒子");
            particles.transform.SetParent(parent, false);
            var ps = particles.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.startLifetime = 2.2f;
            main.startSpeed = 0.45f;
            main.startSize = 0.09f;
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.45f, 0.08f), new Color(0.15f, 0.90f, 1f));
            main.maxParticles = 80;
            var emission = ps.emission;
            emission.rateOverTime = 18f;
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 1.25f;
            ConfigureShardParticleRenderer(ps, gold);
        }

        private void CreateWorldLabel(Transform parent, string label, Vector3 offset)
        {
            var labelRoot = new GameObject("地标文字");
            labelRoot.transform.SetParent(parent, false);
            labelRoot.transform.localPosition = offset;
            labelRoot.AddComponent<BillboardLabel>();
            var worldCanvas = labelRoot.AddComponent<Canvas>();
            worldCanvas.renderMode = RenderMode.WorldSpace;
            worldCanvas.sortingOrder = 2;
            var rect = worldCanvas.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(520f, 90f);
            rect.localScale = Vector3.one * 0.004f;
            var text = CreateText("地标名", labelRoot.transform, label, 34, TextAnchor.MiddleCenter, new Color(1f, 0.84f, 0.45f));
            SetRect(text.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            DecorateText(text, new Color(0f, 0f, 0f, 0.85f), new Vector2(2f, -2f), true);
        }

        private void CreateNoiseBeast(Vector3 position, float phase)
        {
            var root = new GameObject("噪音兽_3D");
            root.transform.position = position;
            var core = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            core.transform.SetParent(root.transform, false);
            core.transform.localScale = new Vector3(1.15f, 0.85f, 1.15f);
            var renderer = core.GetComponent<Renderer>();
            renderer.material = new Material(noise);
            Destroy(core.GetComponent<Collider>());
            for (int i = 0; i < 4; i++)
            {
                var spike = GameObject.CreatePrimitive(PrimitiveType.Cube);
                spike.name = "噪音尖刺";
                spike.transform.SetParent(root.transform, false);
                spike.transform.localPosition = Quaternion.Euler(0f, i * 90f, 0f) * new Vector3(0f, 0f, 0.9f);
                spike.transform.localRotation = Quaternion.Euler(25f, i * 90f, 35f);
                spike.transform.localScale = new Vector3(0.22f, 0.22f, 0.85f);
                spike.GetComponent<Renderer>().material = noise;
                Destroy(spike.GetComponent<Collider>());
            }
            root.AddComponent<NoiseBeast3D>().Configure(this, renderer, phase);
        }

        private void CreateCreekSpirit(Vector3 position)
        {
            // 直接克隆已经完成尺寸归一化的主角视觉实例，绕过 Unity 6 对第二个 SkinnedMesh
            // 实例在首帧求值前给出错误 Renderer.bounds 的问题。
            Transform verifiedVisual = playerTransform != null ? playerTransform.Find("林默_模型") : null;
            if (verifiedVisual == null && playerTransform != null)
            {
                Animator playerAnimator = playerTransform.GetComponentInChildren<Animator>(true);
                verifiedVisual = playerAnimator != null ? playerAnimator.transform : null;
            }
            if (verifiedVisual == null)
            {
                Debug.LogError("梅溪回响：主角人形实例未建立，无法创建第一章溪灵。");
                return;
            }

            var root = new GameObject("失调溪灵_三维节拍敌人");
            root.transform.position = position;
            var controller = root.AddComponent<CharacterController>();
            controller.height = 1.92f;
            controller.radius = 0.40f;
            controller.center = new Vector3(0f, 0.96f, 0f);
            controller.stepOffset = 0.30f;
            controller.skinWidth = 0.06f;

            GameObject model = Instantiate(verifiedVisual.gameObject, root.transform);
            model.name = "失调溪灵_水纹化身";
            model.transform.localPosition = verifiedVisual.localPosition;
            model.transform.localRotation = verifiedVisual.localRotation;
            model.transform.localScale = verifiedVisual.localScale * 1.04f;

            Texture2D texture = Resources.Load<Texture2D>("Models/Characters/100Avatars_069_Kyle");
            Material spiritMaterial = MakeMaterial("溪灵水纹化身", new Color(0.24f, 0.72f, 0.86f), 0.42f, true);
            if (texture != null)
            {
                spiritMaterial.mainTexture = texture;
                if (spiritMaterial.HasProperty("_BaseMap")) spiritMaterial.SetTexture("_BaseMap", texture);
            }
            Renderer[] renderers = model.GetComponentsInChildren<Renderer>(true);
            foreach (Renderer renderer in renderers)
            {
                Material[] materials = renderer.sharedMaterials;
                for (int i = 0; i < materials.Length; i++) materials[i] = spiritMaterial;
                renderer.sharedMaterials = materials;
                renderer.shadowCastingMode = ShadowCastingMode.On;
                renderer.receiveShadows = true;
            }

            Animator animator = model.GetComponent<Animator>();
            if (animator == null) animator = model.AddComponent<Animator>();
            // 主角控制器已经通过实机验证；溪灵复用同一稳定控制器，再由战斗脚本映射不同招式。
            animator.runtimeAnimatorController = Resources.Load<RuntimeAnimatorController>("Animations/LinMoCombat");
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

            creekSpirit = root.AddComponent<CreekRhythmEnemy3D>();
            creekSpirit.Configure(this, playerTransform, controller, animator, renderers, cyan, gold);
            CreateCreekSpiritAura(root.transform);
            root.SetActive(false);
        }

        private void CreateCreekSpiritAura(Transform parent)
        {
            var aura = new GameObject("溪灵环流微粒");
            aura.transform.SetParent(parent, false);
            aura.transform.localPosition = Vector3.up * 1.0f;
            var particles = aura.AddComponent<ParticleSystem>();
            var main = particles.main;
            main.loop = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.75f, 1.25f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.10f, 0.34f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.022f, 0.055f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.20f, 0.92f, 1f, 0.72f), new Color(1f, 0.68f, 0.22f, 0.74f));
            main.maxParticles = 20;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            var emission = particles.emission;
            emission.rateOverTime = 7f;
            var shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.radius = 0.48f;
            shape.angle = 8f;
            shape.length = 0.25f;
            var velocity = particles.velocityOverLifetime;
            velocity.enabled = true;
            // Unity validates every assignment immediately, so switching a single
            // axis to TwoConstants briefly leaves the three axes in mixed modes and
            // logs an error. Keep all axes in Constant mode; visual variation still
            // comes from the emitter's randomized start speed and rotation.
            velocity.orbitalX = new ParticleSystem.MinMaxCurve(0f);
            velocity.orbitalY = new ParticleSystem.MinMaxCurve(1.55f);
            velocity.orbitalZ = new ParticleSystem.MinMaxCurve(0f);
            ConfigureShardParticleRenderer(particles, cyan);
        }

        private GameObject CreateCube(string label, Vector3 position, Vector3 scale, Material material, bool collider)
        {
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = label;
            cube.transform.position = position;
            cube.transform.localScale = scale;
            cube.GetComponent<Renderer>().material = material;
            if (!collider) Destroy(cube.GetComponent<Collider>());
            return cube;
        }

        private GameObject CreateChildCube(string label, Transform parent, Vector3 localPosition, Vector3 scale, Material material, bool collider)
        {
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = label;
            cube.transform.SetParent(parent, false);
            cube.transform.localPosition = localPosition;
            cube.transform.localScale = scale;
            cube.GetComponent<Renderer>().material = material;
            if (!collider) Destroy(cube.GetComponent<Collider>());
            return cube;
        }

        private GameObject CreateVillageModel(string resourceName, Vector3 position, float yaw, float targetHeight, string label, Material overrideMaterial = null)
        {
            GameObject asset = Resources.Load<GameObject>("Models/Village/" + resourceName);
            if (asset == null) return null;
            var root = new GameObject(label);
            root.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            GameObject model = Instantiate(asset, root.transform);
            model.name = resourceName;
            foreach (Collider importedCollider in model.GetComponentsInChildren<Collider>()) Destroy(importedCollider);
            foreach (Renderer renderer in model.GetComponentsInChildren<Renderer>())
            {
                renderer.sharedMaterial = overrideMaterial != null ? overrideMaterial : villageAtlas;
                renderer.shadowCastingMode = ShadowCastingMode.On;
                renderer.receiveShadows = true;
            }
            Bounds bounds = CalculateBounds(root);
            float scale = bounds.size.y > 0.001f ? targetHeight / bounds.size.y : 1f;
            root.transform.localScale = Vector3.one * scale;
            Bounds scaledBounds = CalculateBounds(root);
            root.transform.position = position + Vector3.up * -scaledBounds.min.y;
            return root;
        }

        private static Bounds CalculateBounds(GameObject root)
        {
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0) return new Bounds(root.transform.position, Vector3.one);
            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
            return bounds;
        }

        private static GameObject LoadPreferredCharacterModel()
        {
            return MeixiRealisticCast.Load(MeixiCastRole.LinMo) ??
                   Resources.Load<GameObject>("Models/Characters/LinMo") ??
                   Resources.Load<GameObject>("Models/Characters/RealisticMale/Models/DefaultMale");
        }

        private Material CreateRealisticHumanMaterial(string label, Color tint)
        {
            const string textureRoot = "Models/Characters/RealisticMale/Textures/";
            Texture2D albedo = Resources.Load<Texture2D>(textureRoot + "DefaultMale_Albedo");
            Texture2D normal = Resources.Load<Texture2D>(textureRoot + "DefaultMale_Normal");
            Texture2D occlusion = Resources.Load<Texture2D>(textureRoot + "DefaultMale_Occlusion");
            Texture2D specSmooth = Resources.Load<Texture2D>(textureRoot + "DefaultMale_SpecSmooth");
            Material material = MakeMaterial(label, Color.Lerp(Color.white, tint, 0.12f), 0.32f);
            if (albedo != null)
            {
                albedo.filterMode = FilterMode.Trilinear;
                albedo.anisoLevel = 8;
                material.mainTexture = albedo;
                if (material.HasProperty("_BaseMap")) material.SetTexture("_BaseMap", albedo);
            }
            if (normal != null && material.HasProperty("_BumpMap"))
            {
                material.SetTexture("_BumpMap", normal);
                material.EnableKeyword("_NORMALMAP");
            }
            if (occlusion != null && material.HasProperty("_OcclusionMap"))
            {
                material.SetTexture("_OcclusionMap", occlusion);
                material.SetFloat("_OcclusionStrength", 0.88f);
            }
            if (specSmooth != null && material.HasProperty("_SpecGlossMap"))
            {
                material.SetTexture("_SpecGlossMap", specSmooth);
                material.EnableKeyword("_SPECGLOSSMAP");
            }
            if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", 0f);
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", 0.30f);
            return material;
        }

        private void ApplyCharacterMaterial(GameObject model, bool realisticModel = false)
        {
            Texture2D texture = Resources.Load<Texture2D>("Models/Characters/100Avatars_069_Kyle");
            Material characterMaterial = realisticModel
                ? CreateRealisticHumanMaterial("林默_写实人物材质", new Color(0.84f, 0.91f, 1f))
                : MakeMaterial("林默写实纹理", new Color(0.96f, 0.98f, 1f), 0.24f);
            if (!realisticModel && texture != null)
            {
                texture.filterMode = FilterMode.Trilinear;
                texture.anisoLevel = 8;
                characterMaterial.mainTexture = texture;
                if (characterMaterial.HasProperty("_BaseMap")) characterMaterial.SetTexture("_BaseMap", texture);
            }
            if (characterMaterial.HasProperty("_Metallic")) characterMaterial.SetFloat("_Metallic", 0f);
            foreach (Renderer renderer in model.GetComponentsInChildren<Renderer>())
            {
                Material[] materials = renderer.sharedMaterials;
                for (int i = 0; i < materials.Length; i++) materials[i] = characterMaterial;
                renderer.sharedMaterials = materials;
                renderer.shadowCastingMode = ShadowCastingMode.On;
                renderer.receiveShadows = true;
            }
        }

        private void ApplyNpcCharacterMaterial(GameObject model, Color tint, string displayName, bool realisticModel = false)
        {
            Texture2D texture = Resources.Load<Texture2D>("Models/Characters/100Avatars_069_Kyle");
            Color subtleTint = Color.Lerp(Color.white, tint, 0.16f);
            Material material = realisticModel
                ? CreateRealisticHumanMaterial(displayName + "_写实人物材质", subtleTint)
                : MakeMaterial(displayName + "_织物与肤色", subtleTint, 0.20f);
            if (!realisticModel && texture != null)
            {
                texture.filterMode = FilterMode.Trilinear;
                texture.anisoLevel = 8;
                material.mainTexture = texture;
                if (material.HasProperty("_BaseMap")) material.SetTexture("_BaseMap", texture);
            }
            if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", 0f);
            foreach (Renderer renderer in model.GetComponentsInChildren<Renderer>(true))
            {
                Material[] slots = renderer.sharedMaterials;
                for (int i = 0; i < slots.Length; i++) slots[i] = material;
                renderer.sharedMaterials = slots;
                renderer.shadowCastingMode = ShadowCastingMode.On;
                renderer.receiveShadows = true;
            }
        }

        private static void PoseHumanoid(Animator animator, Transform model)
        {
            if (animator == null || animator.avatar == null || !animator.avatar.isHuman) return;
            Vector3 relaxedArm = (Vector3.down + model.forward * 0.10f).normalized;
            Vector3 relaxedForearm = (Vector3.down + model.forward * 0.24f).normalized;
            PoseBoneTowards(animator.GetBoneTransform(HumanBodyBones.LeftUpperArm), animator.GetBoneTransform(HumanBodyBones.LeftLowerArm), relaxedArm);
            PoseBoneTowards(animator.GetBoneTransform(HumanBodyBones.RightUpperArm), animator.GetBoneTransform(HumanBodyBones.RightLowerArm), relaxedArm);
            PoseBoneTowards(animator.GetBoneTransform(HumanBodyBones.LeftLowerArm), animator.GetBoneTransform(HumanBodyBones.LeftHand), relaxedForearm);
            PoseBoneTowards(animator.GetBoneTransform(HumanBodyBones.RightLowerArm), animator.GetBoneTransform(HumanBodyBones.RightHand), relaxedForearm);
        }

        private static void PoseBoneTowards(Transform bone, Transform child, Vector3 direction)
        {
            if (bone == null || child == null) return;
            Vector3 current = child.position - bone.position;
            if (current.sqrMagnitude < 0.0001f) return;
            bone.rotation = Quaternion.FromToRotation(current.normalized, direction.normalized) * bone.rotation;
        }

        private void CreateInvisibleBoundary(string label, Vector3 position, Vector3 scale)
        {
            GameObject boundary = CreateCube(label, position, scale, stone, true);
            boundary.GetComponent<Renderer>().enabled = false;
        }

        private void CreateBackdropMountains()
        {
            for (int i = 0; i < 14; i++)
            {
                float angle = i / 14f * Mathf.PI * 2f;
                Vector3 position = new(Mathf.Cos(angle) * 27f, -0.5f, Mathf.Sin(angle) * 27f);
                CreateVillageModel(i % 2 == 0 ? "mountain_A_grass_trees" : "mountain_B_grass_trees", position, -angle * Mathf.Rad2Deg + 90f, 8.5f + i % 3 * 1.2f, "梅溪远山");
            }
        }

        private static void CreateDawnSkybox()
        {
            Shader skyShader = Shader.Find("Skybox/Procedural");
            if (skyShader == null) return;
            var sky = new Material(skyShader) { name = "梅溪写实晨雾天空" };
            if (sky.HasProperty("_SkyTint")) sky.SetColor("_SkyTint", new Color(0.28f, 0.42f, 0.52f));
            if (sky.HasProperty("_GroundColor")) sky.SetColor("_GroundColor", new Color(0.12f, 0.17f, 0.16f));
            if (sky.HasProperty("_AtmosphereThickness")) sky.SetFloat("_AtmosphereThickness", 1.15f);
            if (sky.HasProperty("_SunSize")) sky.SetFloat("_SunSize", 0.035f);
            if (sky.HasProperty("_SunSizeConvergence")) sky.SetFloat("_SunSizeConvergence", 4.5f);
            if (sky.HasProperty("_Exposure")) sky.SetFloat("_Exposure", 0.86f);
            RenderSettings.skybox = sky;
            DynamicGI.UpdateEnvironment();
        }

        private void SetupPostProcessing()
        {
            var volumeObject = new GameObject("梅溪电影色调");
            var volume = volumeObject.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 10f;
            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            volume.profile = profile;

            Bloom bloom = profile.Add<Bloom>(true);
            bloom.intensity.Override(0.28f);
            bloom.threshold.Override(1.10f);
            bloom.scatter.Override(0.56f);
            Vignette vignette = profile.Add<Vignette>(true);
            vignette.intensity.Override(0.22f);
            vignette.smoothness.Override(0.58f);
            ColorAdjustments color = profile.Add<ColorAdjustments>(true);
            color.postExposure.Override(-0.08f);
            color.contrast.Override(9f);
            color.saturation.Override(4f);
            Tonemapping tonemapping = profile.Add<Tonemapping>(true);
            tonemapping.mode.Override(TonemappingMode.ACES);
        }

        private void BuildUI()
        {
            if (uiFont == null) uiFont = Font.CreateDynamicFontFromOSFont(new[] { "Microsoft YaHei", "SimHei", "Arial" }, 34);
            if (diaryFont == null) diaryFont = CreateDiaryHandwritingFont();
            var root = new GameObject("梅溪回响_华丽界面");
            canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            var scaler = root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            root.AddComponent<GraphicRaycaster>();
            EventSystem eventSystem = FindAnyObjectByType<EventSystem>();
            if (eventSystem == null)
            {
                var eventRoot = new GameObject("EventSystem");
                eventSystem = eventRoot.AddComponent<EventSystem>();
            }
            var uiInput = eventSystem.GetComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>() ??
                          eventSystem.gameObject.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            uiInput.AssignDefaultActions();
            eventSystem.sendNavigationEvents = true;
            foreach (BaseInputModule module in eventSystem.GetComponents<BaseInputModule>())
                if (module != uiInput) module.enabled = false;

            BuildTitleUI();
            BuildOpeningUI();
            BuildHudUI();
            BuildDialogueUI();
            BuildRhythmUI();
            BuildChapterUI();
            BuildEndingUI();
            BuildLoadingUI();
            BuildPauseUI();
            BuildDefeatUI();
            BuildGameplayGuideUI();

            toastPanel = CreatePanel("山水声纹提示横幅", canvas.transform, Color.white, new Vector2(0.20f, 0.70f), new Vector2(0.80f, 0.90f));
            ApplyTexture(toastPanel, "Art/PromptBannerFrame");
            Image toastFrame = toastPanel.GetComponent<Image>();
            toastFrame.raycastTarget = false;
            toastText = CreateText("提示正文", toastPanel.transform, "", 27, TextAnchor.MiddleCenter, Color.white);
            SetRect(toastText.rectTransform, new Vector2(0.17f, 0.28f), new Vector2(0.83f, 0.72f), Vector2.zero, Vector2.zero);
            DecorateText(toastText, new Color(0f, 0f, 0f, 0.9f), new Vector2(3f, -3f), true);
            toastBannerFx = toastPanel.AddComponent<ToastBannerFx>();
            toastBannerFx.Configure(toastFrame);
            toastPanel.SetActive(false);
            BuildTransitionUI();
        }

        private void BuildTransitionUI()
        {
            transitionPanel = CreatePanel("电影式场景转场", canvas.transform,
                new Color(0.006f, 0.012f, 0.015f, 1f), Vector2.zero, Vector2.one);
            transitionPanel.transform.SetAsLastSibling();
            transitionGroup = transitionPanel.AddComponent<CanvasGroup>();
            transitionGroup.alpha = 0f;
            transitionGroup.blocksRaycasts = false;
            transitionGroup.interactable = false;

            Text eyebrow = CreateText("转场眉题", transitionPanel.transform,
                "M E I X I   ·   M E M O R Y   I N   M O T I O N", 14,
                TextAnchor.MiddleCenter, new Color(.46f, .69f, .69f, .82f));
            SetRect(eyebrow.rectTransform, new Vector2(.24f, .585f), new Vector2(.76f, .63f), Vector2.zero, Vector2.zero);

            transitionCaption = CreateText("转场字幕", transitionPanel.transform, "梅 溪 回 响", 40,
                TextAnchor.MiddleCenter, new Color(.92f, .84f, .63f, .98f));
            transitionCaption.font = displayFont != null ? displayFont : uiFont;
            SetRect(transitionCaption.rectTransform, new Vector2(.19f, .455f), new Vector2(.81f, .585f), Vector2.zero, Vector2.zero);
            DecorateText(transitionCaption, new Color(0f, 0f, 0f, .72f), new Vector2(2f, -2f), false);

            Text seal = CreateText("转场声纹印记", transitionPanel.transform, "◇", 20,
                TextAnchor.MiddleCenter, new Color(.28f, .78f, .79f, .88f));
            seal.font = displayFont != null ? displayFont : uiFont;
            SetRect(seal.rectTransform, new Vector2(.485f, .393f), new Vector2(.515f, .445f), Vector2.zero, Vector2.zero);
            var leftLine = CreatePanel("左侧余音线", transitionPanel.transform,
                new Color(.42f, .67f, .64f, .50f), new Vector2(.315f, .417f), new Vector2(.474f, .419f));
            var rightLine = CreatePanel("右侧余音线", transitionPanel.transform,
                new Color(.42f, .67f, .64f, .50f), new Vector2(.526f, .417f), new Vector2(.685f, .419f));
            leftLine.GetComponent<Image>().raycastTarget = false;
            rightLine.GetComponent<Image>().raycastTarget = false;
            transitionCaption.raycastTarget = false;
            transitionPanel.SetActive(true);
        }

        private void BeginScreenTransition(string caption, Action swap, float fadeOut = .42f,
            float hold = .10f, float fadeIn = .58f, Action completed = null)
        {
            if (sceneTransitionRoutine != null) StopCoroutine(sceneTransitionRoutine);
            sceneTransitionRoutine = StartCoroutine(ScreenTransitionRoutine(caption, swap, fadeOut, hold, fadeIn, completed));
        }

        private IEnumerator ScreenTransitionRoutine(string caption, Action swap, float fadeOut,
            float hold, float fadeIn, Action completed)
        {
            sceneTransitionActive = true;
            chapterCardTransitionPending = true;
            if (transitionPanel != null)
            {
                transitionPanel.SetActive(true);
                transitionPanel.transform.SetAsLastSibling();
                transitionGroup.blocksRaycasts = true;
                transitionCaption.text = caption ?? string.Empty;
            }

            float restoreVolume = AudioListener.volume;
            yield return FadeTransitionWithAudio(transitionGroup != null ? transitionGroup.alpha : 0f, 1f,
                restoreVolume, restoreVolume * .28f, fadeOut);
            swap?.Invoke();
            if (hold > 0f) yield return new WaitForSecondsRealtime(hold);
            yield return FadeTransitionWithAudio(1f, 0f, restoreVolume * .28f, restoreVolume, fadeIn);

            if (transitionGroup != null)
            {
                transitionGroup.blocksRaycasts = false;
                transitionGroup.interactable = false;
            }
            sceneTransitionActive = false;
            chapterCardTransitionPending = false;
            sceneTransitionRoutine = null;
            completed?.Invoke();
        }

        private IEnumerator FadeTransitionWithAudio(float visualFrom, float visualTo,
            float audioFrom, float audioTo, float duration)
        {
            if (duration <= 0f)
            {
                if (transitionGroup != null) transitionGroup.alpha = visualTo;
                AudioListener.volume = audioTo;
                yield break;
            }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float eased = t * t * (3f - 2f * t);
                if (transitionGroup != null) transitionGroup.alpha = Mathf.Lerp(visualFrom, visualTo, eased);
                AudioListener.volume = Mathf.Lerp(audioFrom, audioTo, eased);
                yield return null;
            }
            if (transitionGroup != null) transitionGroup.alpha = visualTo;
            AudioListener.volume = audioTo;
        }

        private IEnumerator FadeTransition(float from, float to, float duration)
        {
            if (transitionGroup == null) yield break;
            if (duration <= 0f)
            {
                transitionGroup.alpha = to;
                yield break;
            }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float eased = t * t * (3f - 2f * t);
                transitionGroup.alpha = Mathf.Lerp(from, to, eased);
                yield return null;
            }
            transitionGroup.alpha = to;
        }

        private void FadeBackToGame(float duration = .65f)
        {
            if (transitionGroup == null) return;
            if (sceneTransitionRoutine != null) StopCoroutine(sceneTransitionRoutine);
            sceneTransitionRoutine = StartCoroutine(FadeBackToGameRoutine(duration));
        }

        private IEnumerator FadeBackToGameRoutine(float duration)
        {
            sceneTransitionActive = true;
            transitionPanel.SetActive(true);
            transitionPanel.transform.SetAsLastSibling();
            transitionGroup.blocksRaycasts = true;
            yield return FadeTransition(1f, 0f, duration);
            transitionGroup.blocksRaycasts = false;
            sceneTransitionActive = false;
            sceneTransitionRoutine = null;
        }

        private void CompleteTransitionImmediatelyForValidation()
        {
            if (sceneTransitionRoutine != null)
            {
                StopCoroutine(sceneTransitionRoutine);
                sceneTransitionRoutine = null;
            }
            if (transitionGroup != null)
            {
                transitionGroup.alpha = 0f;
                transitionGroup.blocksRaycasts = false;
            }
            AudioListener.volume = 1f;
            sceneTransitionActive = false;
            chapterCardTransitionPending = false;
        }

        private void BuildTitleUI()
        {
            titlePanel = CreatePanel("主标题界面", canvas.transform, new Color(0.006f, 0.012f, 0.018f, 1f), Vector2.zero, Vector2.one);
            Image titleInputSurface = titlePanel.GetComponent<Image>();
            var backgroundObject = CreatePanel("动态溪水背景", titlePanel.transform, Color.white, new Vector2(-0.02f, -0.02f), new Vector2(1.02f, 1.02f));
            ApplyTexture(backgroundObject, "Art/TitleBackgroundV2");
            Image backgroundImage = backgroundObject.GetComponent<Image>();
            backgroundImage.color = new Color(0.82f, 0.90f, 0.96f, 1f);
            backgroundImage.raycastTarget = false;
            Shader waterShader = Resources.Load<Shader>("Shaders/MeixiTitleWaterFlow");
            if (waterShader != null)
                backgroundImage.material = new Material(waterShader) { name = "梅溪标题溪水流动材质" };

            titlePrimaryButton = titlePanel.AddComponent<Button>();
            titlePrimaryButton.transition = Selectable.Transition.None;
            titlePrimaryButton.targetGraphic = titleInputSurface;
            titlePrimaryButton.interactable = false;
            titlePrimaryButton.onClick.AddListener(HandleTitlePrimaryAction);

            var cinematicVeil = CreatePanel("电影暗幕", titlePanel.transform, new Color(0.002f, 0.008f, 0.015f, 0.46f), Vector2.zero, Vector2.one);
            cinematicVeil.GetComponent<Image>().raycastTarget = false;

            var sparkleRoot = new GameObject("标题流萤").AddComponent<RectTransform>();
            sparkleRoot.SetParent(titlePanel.transform, false);
            SetRect(sparkleRoot, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            var titleRoot = new GameObject("标题主体").AddComponent<RectTransform>();
            titleRoot.SetParent(titlePanel.transform, false);
            SetRect(titleRoot, new Vector2(0.10f, 0.37f), new Vector2(0.90f, 0.77f), Vector2.zero, Vector2.zero);
            var titleGroup = titleRoot.gameObject.AddComponent<CanvasGroup>();
            titleGroup.alpha = 0f;

            var wordmarkObject = CreatePanel("梅溪回响艺术字", titleRoot, Color.white, new Vector2(0.01f, 0.20f), new Vector2(0.99f, 0.96f));
            ApplyTexture(wordmarkObject, "Art/MeixiTitleWordmark");
            Image wordmark = wordmarkObject.GetComponent<Image>();
            wordmark.preserveAspect = true;
            wordmark.raycastTarget = false;

            var leftLine = CreatePanel("标题左金线", titleRoot, new Color(1f, 0.70f, 0.26f, 0.66f), new Vector2(0.20f, 0.15f), new Vector2(0.465f, 0.156f)).GetComponent<Image>();
            leftLine.raycastTarget = false;
            var rightLine = CreatePanel("标题右金线", titleRoot, new Color(1f, 0.70f, 0.26f, 0.66f), new Vector2(0.535f, 0.15f), new Vector2(0.80f, 0.156f)).GetComponent<Image>();
            rightLine.raycastTarget = false;
            RectTransform centerDiamond = CreatePanel("标题中心菱形", titleRoot, new Color(0.52f, 0.93f, 1f, 0.88f), new Vector2(0.5f, 0.153f), new Vector2(0.5f, 0.153f)).GetComponent<RectTransform>();
            centerDiamond.sizeDelta = new Vector2(12f, 12f);
            centerDiamond.localRotation = Quaternion.Euler(0f, 0f, 45f);
            centerDiamond.GetComponent<Image>().raycastTarget = false;

            var subtitle = CreateText("开场副标题", titleRoot, "第三人称剧情动作音游", 20, TextAnchor.MiddleCenter, new Color(0.84f, 0.94f, 1f, 0.92f));
            SetRect(subtitle.rectTransform, new Vector2(0.20f, 0.01f), new Vector2(0.80f, 0.13f), Vector2.zero, Vector2.zero);
            DecorateText(subtitle, new Color(0f, 0f, 0f, 0.76f), new Vector2(2f, -2f), false);

            var loadingRoot = new GameObject("加载区域").AddComponent<RectTransform>();
            loadingRoot.SetParent(titlePanel.transform, false);
            SetRect(loadingRoot, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var loadingGroup = loadingRoot.gameObject.AddComponent<CanvasGroup>();

            var status = CreateText("加载状态", loadingRoot, "正在读取世界数据", 19, TextAnchor.MiddleCenter, new Color(1f, 0.76f, 0.34f));
            SetRect(status.rectTransform, new Vector2(0.34f, 0.145f), new Vector2(0.66f, 0.185f), Vector2.zero, Vector2.zero);

            var loadingRail = CreatePanel("声纹加载轨道", loadingRoot, new Color(0.42f, 0.70f, 0.80f, 0.30f), new Vector2(0.27f, 0.116f), new Vector2(0.73f, 0.121f));
            loadingRail.GetComponent<Image>().raycastTarget = false;
            var loadingFillObject = CreatePanel("真实加载进度", loadingRail.transform, new Color(0.20f, 0.90f, 1f, 0.96f), Vector2.zero, Vector2.one);
            Image loadingFill = loadingFillObject.GetComponent<Image>();
            loadingFill.type = Image.Type.Filled;
            loadingFill.fillMethod = Image.FillMethod.Horizontal;
            loadingFill.fillOrigin = 0;
            loadingFill.fillAmount = 0f;
            loadingFill.raycastTarget = false;

            RectTransform leftCap = CreatePanel("加载左菱形", loadingRoot, new Color(1f, 0.71f, 0.27f, 0.96f), new Vector2(0.262f, 0.118f), new Vector2(0.262f, 0.118f)).GetComponent<RectTransform>();
            leftCap.sizeDelta = new Vector2(15f, 15f);
            leftCap.localRotation = Quaternion.Euler(0f, 0f, 45f);
            leftCap.GetComponent<Image>().raycastTarget = false;
            RectTransform rightCap = CreatePanel("加载右菱形", loadingRoot, new Color(1f, 0.71f, 0.27f, 0.96f), new Vector2(0.738f, 0.118f), new Vector2(0.738f, 0.118f)).GetComponent<RectTransform>();
            rightCap.sizeDelta = new Vector2(15f, 15f);
            rightCap.localRotation = Quaternion.Euler(0f, 0f, 45f);
            rightCap.GetComponent<Image>().raycastTarget = false;

            var percent = CreateText("加载百分比", loadingRoot, "0%", 15, TextAnchor.MiddleRight, new Color(0.68f, 0.88f, 0.94f));
            SetRect(percent.rectTransform, new Vector2(0.73f, 0.101f), new Vector2(0.79f, 0.136f), Vector2.zero, Vector2.zero);

            var ready = CreateText("开始游戏", titlePanel.transform, "选 择 旅 途", 22, TextAnchor.MiddleCenter, new Color(1f, 0.76f, 0.34f));
            SetRect(ready.rectTransform, new Vector2(0.36f, 0.245f), new Vector2(0.64f, 0.290f), Vector2.zero, Vector2.zero);

            var press = CreateText("开始提示", titlePanel.transform, "ENTER / SPACE  进入推荐选项", 16, TextAnchor.MiddleCenter, new Color(0.82f, 0.96f, 1f, 0.92f));
            SetRect(press.rectTransform, new Vector2(0.32f, 0.205f), new Vector2(0.68f, 0.238f), Vector2.zero, Vector2.zero);
            DecorateText(press, Color.black, new Vector2(2f, -2f), false);

            titleScreenFx = titlePanel.AddComponent<MeixiTitleScreenFx>();
            titleScreenFx.Configure(backgroundImage.rectTransform, titleRoot, titleGroup, loadingGroup, loadingFill,
                status, percent, ready, press, titlePrimaryButton, sparkleRoot,
                leftLine, rightLine, centerDiamond.GetComponent<Image>());

            GameObject saveMenu = new("标题存档菜单");
            saveMenu.transform.SetParent(titlePanel.transform, false);
            RectTransform saveMenuRect = saveMenu.AddComponent<RectTransform>();
            SetRect(saveMenuRect, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            titleSaveMenuGroup = saveMenu.AddComponent<CanvasGroup>();
            titleSaveMenuGroup.alpha = 0f;
            titleSaveMenuGroup.interactable = false;
            titleSaveMenuGroup.blocksRaycasts = false;

            titleContinueButton = CreateButton("开始游戏", saveMenu.transform, "开 始 游 戏",
                new Color(.045f, .29f, .34f, .98f));
            SetRect(titleContinueButton.GetComponent<RectTransform>(), new Vector2(.285f, .105f), new Vector2(.445f, .175f), Vector2.zero, Vector2.zero);
            titleContinueButton.onClick.AddListener(ContinueSavedJourney);
            titleNewJourneyButton = CreateButton("新旅途", saveMenu.transform, "新 旅 途",
                new Color(.48f, .14f, .055f, .98f));
            SetRect(titleNewJourneyButton.GetComponent<RectTransform>(), new Vector2(.455f, .105f), new Vector2(.615f, .175f), Vector2.zero, Vector2.zero);
            titleNewJourneyButton.onClick.AddListener(OpenNewJourneyConfirmation);
            titleDeleteSaveButton = CreateButton("删除本地存档", saveMenu.transform, "删 除 存 档",
                new Color(.25f, .055f, .065f, .98f));
            SetRect(titleDeleteSaveButton.GetComponent<RectTransform>(), new Vector2(.625f, .105f), new Vector2(.735f, .175f), Vector2.zero, Vector2.zero);
            titleDeleteSaveButton.onClick.AddListener(OpenDeleteSaveConfirmation);
            titleSaveSummaryText = CreateText("本地存档摘要", saveMenu.transform, string.Empty, 14,
                TextAnchor.MiddleCenter, new Color(.68f, .88f, .92f));
            SetRect(titleSaveSummaryText.rectTransform, new Vector2(.27f, .050f), new Vector2(.74f, .095f), Vector2.zero, Vector2.zero);

            titleDeleteConfirmPanel = CreatePanel("旅途操作确认", titlePanel.transform,
                new Color(.003f, .010f, .015f, .90f), Vector2.zero, Vector2.one);
            GameObject deleteCard = CreatePanel("旅途操作确认卡", titleDeleteConfirmPanel.transform,
                new Color(.012f, .045f, .058f, .99f), new Vector2(.31f, .29f), new Vector2(.69f, .71f));
            PolishPanel(deleteCard, new Color(1f, .30f, .22f, .75f));
            titleConfirmationTitleText = CreateText("确认操作标题", deleteCard.transform, string.Empty, 34,
                TextAnchor.MiddleCenter, new Color(1f, .68f, .28f));
            SetRect(titleConfirmationTitleText.rectTransform, new Vector2(.08f, .66f), new Vector2(.92f, .86f), Vector2.zero, Vector2.zero);
            titleConfirmationBodyText = CreateText("确认操作说明", deleteCard.transform, string.Empty, 18,
                TextAnchor.MiddleCenter, new Color(.84f, .92f, .92f));
            SetRect(titleConfirmationBodyText.rectTransform, new Vector2(.10f, .38f), new Vector2(.90f, .64f), Vector2.zero, Vector2.zero);
            titleConfirmationAcceptButton = CreateButton("确认旅途操作", deleteCard.transform, "确 认",
                new Color(.42f, .045f, .055f, .99f));
            SetRect(titleConfirmationAcceptButton.GetComponent<RectTransform>(), new Vector2(.12f, .13f), new Vector2(.47f, .31f), Vector2.zero, Vector2.zero);
            titleConfirmationAcceptButton.onClick.AddListener(ConfirmTitleAction);
            Button cancelDelete = CreateButton("取消旅途操作", deleteCard.transform, "取 消",
                new Color(.045f, .28f, .32f, .99f));
            SetRect(cancelDelete.GetComponent<RectTransform>(), new Vector2(.53f, .13f), new Vector2(.88f, .31f), Vector2.zero, Vector2.zero);
            cancelDelete.onClick.AddListener(CloseTitleConfirmation);
            titleDeleteConfirmPanel.SetActive(false);
            RefreshTitleSaveUi();
        }

        private void BuildOpeningUI()
        {
            openingPanel = CreatePanel("序章日记放映", canvas.transform, new Color(0f, 0f, 0f, 1f), Vector2.zero, Vector2.one);
            CreatePanel("放映暗幕左", openingPanel.transform, new Color(.015f, .012f, .009f, 1f), Vector2.zero, new Vector2(.23f, 1f));
            CreatePanel("放映暗幕右", openingPanel.transform, new Color(.015f, .012f, .009f, 1f), new Vector2(.77f, 0f), Vector2.one);

            GameObject paper = CreatePanel("泛黄调研日记", openingPanel.transform, Color.white,
                new Vector2(.145f, .155f), new Vector2(.855f, .845f));
            ApplyTexture(paper, "Art/MeixiDiaryPaperLandscapeV1");
            Image paperImage = paper.GetComponent<Image>();
            paperImage.preserveAspect = true;
            paperImage.raycastTarget = false;
            PolishPanel(paper, new Color(.78f, .57f, .28f, .50f));
            openingPaperGroup = paper.AddComponent<CanvasGroup>();

            openingDiaryDate = CreateText("日记日期", paper.transform, diaryDates[0], 18,
                TextAnchor.MiddleLeft, new Color(.20f, .22f, .19f, .82f));
            SetRect(openingDiaryDate.rectTransform, new Vector2(.12f, .79f), new Vector2(.88f, .87f), Vector2.zero, Vector2.zero);
            openingDiaryTitle = CreateText("日记标题", paper.transform, diaryTitles[0], 35,
                TextAnchor.MiddleCenter, new Color(.13f, .19f, .18f, .96f));
            SetRect(openingDiaryTitle.rectTransform, new Vector2(.12f, .64f), new Vector2(.88f, .78f), Vector2.zero, Vector2.zero);
            DecorateText(openingDiaryTitle, new Color(.92f, .83f, .63f, .45f), new Vector2(1f, -1f), false);
            CreatePanel("日记墨线", paper.transform, new Color(.11f, .24f, .25f, .48f),
                new Vector2(.14f, .625f), new Vector2(.86f, .629f));
            openingDiaryBody = CreateText("日记正文", paper.transform, diaryBodies[0], 23,
                TextAnchor.UpperLeft, new Color(.16f, .17f, .14f, .94f));
            openingDiaryBody.lineSpacing = 1.28f;
            SetRect(openingDiaryBody.rectTransform, new Vector2(.13f, .18f), new Vector2(.87f, .59f), Vector2.zero, Vector2.zero);
            openingDiaryPageText = CreateText("日记页码", paper.transform, "— 01 / 03 —", 15,
                TextAnchor.MiddleCenter, new Color(.18f, .25f, .23f, .70f));
            SetRect(openingDiaryPageText.rectTransform, new Vector2(.34f, .08f), new Vector2(.66f, .14f), Vector2.zero, Vector2.zero);

            if (diaryFont != null)
            {
                openingDiaryDate.font = diaryFont;
                openingDiaryTitle.font = diaryFont;
                openingDiaryBody.font = diaryFont;
                openingDiaryPageText.font = diaryFont;
            }

            var prologue = CreateText("序章标记", openingPanel.transform, "序 章  ·  回 乡 之 前", 18,
                TextAnchor.MiddleCenter, new Color(.82f, .66f, .38f, .88f));
            SetRect(prologue.rectTransform, new Vector2(.02f, .86f), new Vector2(.25f, .92f), Vector2.zero, Vector2.zero);
            var hint = CreateText("日记翻页提示", openingPanel.transform, "SPACE / ENTER / 点击  ·  翻页",
                16, TextAnchor.MiddleCenter, new Color(.77f, .70f, .57f, .82f));
            SetRect(hint.rectTransform, new Vector2(.76f, .08f), new Vector2(.98f, .14f), Vector2.zero, Vector2.zero);
            hint.gameObject.AddComponent<FancyTextPulse>().Fade = true;
            openingPanel.SetActive(false);
        }

        private static Font CreateDiaryHandwritingFont()
        {
            // 华文行楷是真正的中文手写/行书字形；同时保留英文内部名与
            // 其他书写体作为不同 Windows 语言环境下的安全回退。
            string[] preferred = { "华文行楷", "STXingkai", "方正舒体", "FZShuTi", "华文新魏", "STXinwei", "KaiTi", "STKaiti" };
            HashSet<string> installed = new(Font.GetOSInstalledFontNames(), StringComparer.OrdinalIgnoreCase);
            foreach (string fontName in preferred)
            {
                if (!installed.Contains(fontName)) continue;
                Font selected = Font.CreateDynamicFontFromOSFont(fontName, 42);
                if (selected == null) continue;
                Debug.Log($"[MeixiUI] Diary handwriting font selected: {fontName}");
                return selected;
            }

            Debug.LogWarning("[MeixiUI] Chinese handwriting font not found; using system fallback.");
            return Font.CreateDynamicFontFromOSFont(new[] { "KaiTi", "SimSun" }, 42);
        }

        private void BuildHudUI()
        {
            hudPanel = CreatePanel("游戏HUD", canvas.transform, Color.clear, Vector2.zero, Vector2.one);
            var quest = CreatePanel("水纹任务签", hudPanel.transform, Color.white, new Vector2(0.018f, 0.80f), new Vector2(0.39f, 0.965f));
            ApplyTexture(quest, "Art/QuestFrameV2");
            storyProgressText = CreateText("主线推进", quest.transform, "主线 01 / 04  ·  询问道路", 14,
                TextAnchor.MiddleLeft, new Color(.48f, .86f, .92f));
            SetRect(storyProgressText.rectTransform, new Vector2(.15f, .76f), new Vector2(.91f, .94f), Vector2.zero, Vector2.zero);
            chapterHudText = CreateText("当前章节", quest.transform, "第一章 · 没有声音的村庄", 27, TextAnchor.MiddleLeft, new Color(1f, 0.78f, 0.35f));
            SetRect(chapterHudText.rectTransform, new Vector2(0.15f, 0.43f), new Vector2(0.91f, 0.76f), Vector2.zero, Vector2.zero);
            DecorateText(chapterHudText, Color.black, new Vector2(2f, -2f), false);
            objectiveText = CreateText("当前目标", quest.transform, "在村口找到老村长，询问樟洋村怎么走", 19, TextAnchor.MiddleLeft, Color.white);
            SetRect(objectiveText.rectTransform, new Vector2(0.15f, 0.10f), new Vector2(0.91f, 0.46f), Vector2.zero, Vector2.zero);

            GameObject healthCard = CreatePanel("回响力状态卡", hudPanel.transform,
                new Color(.008f, .035f, .048f, .88f), new Vector2(.715f, .900f), new Vector2(.972f, .978f));
            PolishPanel(healthCard, new Color(.24f, .88f, 1f, .50f));
            healthCardRect = healthCard.GetComponent<RectTransform>();
            healthText = CreateText("回响力", healthCard.transform, "回响力  5 / 5", 21,
                TextAnchor.MiddleRight, new Color(1f, 0.58f, 0.34f));
            SetRect(healthText.rectTransform, new Vector2(.05f, .43f), new Vector2(.95f, .97f), Vector2.zero, Vector2.zero);
            DecorateText(healthText, Color.black, new Vector2(2f, -2f), false);

            GameObject healthTrack = CreatePanel("回响力能量轨", healthCard.transform,
                new Color(.012f, .075f, .090f, .96f), new Vector2(.34f, .12f), new Vector2(.95f, .35f));
            PolishPanel(healthTrack, new Color(.20f, .76f, .88f, .46f));
            for (int segment = 0; segment < healthBarSegments.Length; segment++)
            {
                float x0 = .025f + segment * .194f;
                healthBarSegments[segment] = CreatePanel("回响力矩形格_" + (segment + 1), healthTrack.transform,
                    new Color(.18f, .92f, 1f, .98f), new Vector2(x0, .14f), new Vector2(x0 + .166f, .86f)).GetComponent<Image>();
                healthBarSegments[segment].raycastTarget = false;
            }

            GameObject healthFx = CreatePanel("回响力增减脉冲", healthCard.transform,
                new Color(.18f, .95f, 1f, .20f), Vector2.zero, Vector2.one);
            healthBarGlow = healthFx.GetComponent<Image>();
            healthFxGroup = healthFx.AddComponent<CanvasGroup>();
            healthFxGroup.alpha = 0f;
            healthFxGroup.blocksRaycasts = false;
            healthDeltaText = CreateText("回响力增减值", healthCard.transform, "+2 回响", 20,
                TextAnchor.MiddleLeft, new Color(.38f, 1f, .72f));
            SetRect(healthDeltaText.rectTransform, new Vector2(.045f, .05f), new Vector2(.36f, .43f), Vector2.zero, Vector2.zero);
            DecorateText(healthDeltaText, Color.black, new Vector2(2f, -2f), true);
            healthDeltaText.gameObject.SetActive(false);

            displayedHealth = health;
            soulCountText = CreateText("魂音数量", hudPanel.transform, "收集魂音：0 / 3", 19, TextAnchor.UpperRight, new Color(0.42f, 0.90f, 1f));
            SetRect(soulCountText.rectTransform, new Vector2(0.72f, 0.850f), new Vector2(0.965f, 0.900f), Vector2.zero, Vector2.zero);
            archiveProgressText = CreateText("村民线索数量", hudPanel.transform, "村民线索  0 / 3", 17,
                TextAnchor.UpperRight, new Color(.92f, .76f, .44f));
            SetRect(archiveProgressText.rectTransform, new Vector2(.72f, .808f), new Vector2(.965f, .854f), Vector2.zero, Vector2.zero);

            Text arrowText = CreateText("方向箭头", hudPanel.transform, "◇", 34, TextAnchor.MiddleCenter, new Color(1f, 0.66f, 0.22f));
            SetRect(arrowText.rectTransform, new Vector2(0.46f, 0.91f), new Vector2(0.50f, 0.97f), Vector2.zero, Vector2.zero);
            navigationArrow = arrowText.rectTransform;
            navigationText = CreateText("导航距离", hudPanel.transform, "寻找溪流魂音", 17, TextAnchor.MiddleLeft, new Color(0.86f, 0.94f, 1f));
            SetRect(navigationText.rectTransform, new Vector2(0.50f, 0.91f), new Vector2(0.64f, 0.97f), Vector2.zero, Vector2.zero);
            DecorateText(navigationText, Color.black, new Vector2(2f, -2f), false);

            interactionCard = CreatePanel("水纹交互签", hudPanel.transform, Color.white, new Vector2(0.33f, 0.045f), new Vector2(0.67f, 0.165f));
            ApplyTexture(interactionCard, "Art/QuestFrameV2");
            var keyBadge = CreatePanel("E键徽章", interactionCard.transform, new Color(0.90f, 0.38f, 0.09f, 0.98f), new Vector2(0.035f, 0.18f), new Vector2(0.17f, 0.82f));
            keyBadge.GetComponent<RectTransform>().localRotation = Quaternion.Euler(0f, 0f, 45f);
            var keyText = CreateText("E键", keyBadge.transform, "E", 31, TextAnchor.MiddleCenter, Color.white);
            SetRect(keyText.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            keyText.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -45f);
            DecorateText(keyText, Color.black, new Vector2(2f, -2f), false);
            interactionTitle = CreateText("交互主标题", interactionCard.transform, "聆 听 魂 音", 27, TextAnchor.MiddleLeft, new Color(1f, 0.82f, 0.40f));
            SetRect(interactionTitle.rectTransform, new Vector2(0.21f, 0.43f), new Vector2(0.96f, 0.88f), Vector2.zero, Vector2.zero);
            interactionSubtitle = CreateText("交互副标题", interactionCard.transform, "", 17, TextAnchor.MiddleLeft, new Color(0.66f, 0.88f, 0.96f));
            SetRect(interactionSubtitle.rectTransform, new Vector2(0.21f, 0.10f), new Vector2(0.96f, 0.48f), Vector2.zero, Vector2.zero);
            interactionTitle.gameObject.AddComponent<FancyTextPulse>().Amount = 0.035f;
            interactionText = interactionTitle;
            interactionCard.AddComponent<MeixiUiPanelReveal>().Configure(.18f, .94f);
            interactionCard.SetActive(false);
            hudPanel.SetActive(false);
        }

        private void BuildMiniMap(Transform parent)
        {
            var mapPanel = CreatePanel("梅溪声纹地图", parent, new Color(0.012f, 0.045f, 0.065f, 0.92f), new Vector2(0.79f, 0.57f), new Vector2(0.975f, 0.825f));
            PolishPanel(mapPanel, new Color(0.18f, 0.78f, 0.92f, 0.42f));
            var mapTitle = CreateText("地图标题", mapPanel.transform, "梅 溪 村 · 声 纹 图", 18, TextAnchor.MiddleCenter, new Color(1f, 0.75f, 0.34f));
            SetRect(mapTitle.rectTransform, new Vector2(0.06f, 0.82f), new Vector2(0.94f, 0.98f), Vector2.zero, Vector2.zero);
            var mapArea = CreatePanel("地图范围", mapPanel.transform, new Color(0.04f, 0.11f, 0.13f, 0.86f), new Vector2(0.07f, 0.08f), new Vector2(0.93f, 0.81f));
            for (int i = 1; i < 4; i++)
            {
                float p = i / 4f;
                CreatePanel("地图横线", mapArea.transform, new Color(0.28f, 0.58f, 0.61f, 0.18f), new Vector2(0f, p), new Vector2(1f, p + 0.006f));
                CreatePanel("地图纵线", mapArea.transform, new Color(0.28f, 0.58f, 0.61f, 0.18f), new Vector2(p, 0f), new Vector2(p + 0.006f, 1f));
            }
            for (int i = 0; i < nodePositions.Length; i++)
            {
                Vector2 point = MapPoint(nodePositions[i]);
                var marker = CreatePanel("魂音地图点_" + i, mapArea.transform, new Color(0.24f, 0.42f, 0.48f, 0.95f), point, point).GetComponent<RectTransform>();
                marker.sizeDelta = new Vector2(14f, 14f);
                marker.anchoredPosition = Vector2.zero;
                miniMapMarkers.Add(marker);
            }
            Vector2 start = MapPoint(respawnPoint);
            miniMapPlayer = CreatePanel("玩家地图点", mapArea.transform, new Color(1f, 0.76f, 0.24f, 1f), start, start).GetComponent<RectTransform>();
            miniMapPlayer.sizeDelta = new Vector2(10f, 10f);
            miniMapPlayer.anchoredPosition = Vector2.zero;
            var pulse = CreatePanel("玩家外环", miniMapPlayer, new Color(0.18f, 0.86f, 1f, 0.36f), new Vector2(-0.65f, -0.65f), new Vector2(1.65f, 1.65f));
            pulse.transform.SetAsFirstSibling();
        }

        private static Vector2 MapPoint(Vector3 world)
        {
            return new Vector2(Mathf.InverseLerp(-24f, 24f, world.x), Mathf.InverseLerp(-24f, 24f, world.z));
        }

        private void BuildPauseUI()
        {
            pausePanel = CreatePanel("暂停界面", canvas.transform, new Color(0.003f, 0.012f, 0.020f, 0.82f), Vector2.zero, Vector2.one);
            var card = CreatePanel("暂停主卡", pausePanel.transform, new Color(0.018f, 0.055f, 0.080f, 0.98f), new Vector2(0.34f, 0.16f), new Vector2(0.66f, 0.84f));
            PolishPanel(card, new Color(1f, 0.55f, 0.17f, 0.65f));
            card.AddComponent<MeixiUiPanelReveal>().Configure(.24f, .90f);
            var eyebrow = CreateText("暂停眉题", card.transform, "MEIXI RESONANCE", 16, TextAnchor.MiddleCenter, new Color(0.32f, 0.82f, 0.92f));
            SetRect(eyebrow.rectTransform, new Vector2(0.12f, 0.84f), new Vector2(0.88f, 0.92f), Vector2.zero, Vector2.zero);
            var title = CreateText("暂停标题", card.transform, "魂 音 暂 歇", 48, TextAnchor.MiddleCenter, new Color(1f, 0.78f, 0.34f));
            SetRect(title.rectTransform, new Vector2(0.08f, 0.68f), new Vector2(0.92f, 0.84f), Vector2.zero, Vector2.zero);
            DecorateText(title, Color.black, new Vector2(3f, -3f), true);
            pausePracticeText = CreateText("实践记录", card.transform, string.Empty, 15,
                TextAnchor.MiddleCenter, new Color(.48f, .90f, .88f));
            pausePracticeText.lineSpacing = 1.12f;
            SetRect(pausePracticeText.rectTransform, new Vector2(.10f, .615f), new Vector2(.90f, .685f), Vector2.zero, Vector2.zero);
            var controls = CreateText("暂停操作", card.transform, "WASD  行走     鼠标  环绕镜头\nSPACE  跳跃     左 / 右键（J / K）声纹打击\nE  聆听魂音（战斗开始后按提示回应）", 19, TextAnchor.MiddleCenter, new Color(0.74f, 0.87f, 0.93f));
            SetRect(controls.rectTransform, new Vector2(0.09f, 0.445f), new Vector2(0.91f, 0.595f), Vector2.zero, Vector2.zero);
            pauseResumeButton = CreateButton("继续游戏", card.transform, "继 续 聆 听", new Color(0.62f, 0.18f, 0.07f, 0.98f));
            SetRect(pauseResumeButton.GetComponent<RectTransform>(), new Vector2(0.18f, 0.33f), new Vector2(0.82f, 0.44f), Vector2.zero, Vector2.zero);
            pauseResumeButton.onClick.AddListener(TogglePause);
            pauseSettingsButton = CreateButton("设置与成就", card.transform, "设 置 · 成 就", new Color(.13f, .16f, .34f, .98f));
            SetRect(pauseSettingsButton.GetComponent<RectTransform>(), new Vector2(.18f, .20f), new Vector2(.49f, .31f), Vector2.zero, Vector2.zero);
            pauseSettingsButton.onClick.AddListener(OpenPauseSettings);
            pauseRestartButton = CreateButton("重新开始", card.transform, "重 新 启 程", new Color(0.06f, 0.24f, 0.31f, 0.98f));
            SetRect(pauseRestartButton.GetComponent<RectTransform>(), new Vector2(.51f, .20f), new Vector2(.82f, .31f), Vector2.zero, Vector2.zero);
            pauseRestartButton.onClick.AddListener(RestartFromPause);
            pauseQuitButton = CreateButton("退出游戏", card.transform, "退 出 游 戏", new Color(0.30f, 0.07f, 0.08f, 0.98f));
            SetRect(pauseQuitButton.GetComponent<RectTransform>(), new Vector2(0.18f, 0.07f), new Vector2(0.82f, 0.18f), Vector2.zero, Vector2.zero);
            pauseQuitButton.onClick.AddListener(QuitGame);
            var hint = CreateText("暂停提示", card.transform, "ESC  返回游戏", 14, TextAnchor.MiddleCenter, new Color(0.48f, 0.72f, 0.79f));
            SetRect(hint.rectTransform, new Vector2(0.15f, 0.005f), new Vector2(0.85f, 0.06f), Vector2.zero, Vector2.zero);
            BuildPauseSettingsUI();
            pausePanel.SetActive(false);
        }

        private void BuildPauseSettingsUI()
        {
            pauseSettingsPanel = CreatePanel("暂停设置与成就页", pausePanel.transform,
                new Color(.003f, .010f, .016f, .94f), Vector2.zero, Vector2.one);
            GameObject card = CreatePanel("设置成就主卡", pauseSettingsPanel.transform,
                new Color(.010f, .042f, .055f, .99f), new Vector2(.18f, .075f), new Vector2(.82f, .925f));
            PolishPanel(card, new Color(.30f, .84f, .90f, .70f));
            Text eyebrow = CreateText("设置成就眉题", card.transform, "OFFLINE ARCHIVE  /  LOCAL USER DATA", 14,
                TextAnchor.MiddleCenter, new Color(.42f, .82f, .88f));
            SetRect(eyebrow.rectTransform, new Vector2(.10f, .89f), new Vector2(.90f, .95f), Vector2.zero, Vector2.zero);
            Text title = CreateText("设置成就标题", card.transform, "设 置 · 成 就 档 案", 40,
                TextAnchor.MiddleCenter, new Color(1f, .76f, .30f));
            SetRect(title.rectTransform, new Vector2(.08f, .79f), new Vector2(.92f, .89f), Vector2.zero, Vector2.zero);

            GameObject volumeCard = CreatePanel("本地音量设置", card.transform,
                new Color(.015f, .076f, .091f, .94f), new Vector2(.075f, .675f), new Vector2(.925f, .785f));
            PolishPanel(volumeCard, new Color(.26f, .68f, .74f, .40f));
            settingsVolumeText = CreateText("主音量文字", volumeCard.transform, string.Empty, 18,
                TextAnchor.MiddleCenter, new Color(.84f, .94f, .94f));
            SetRect(settingsVolumeText.rectTransform, new Vector2(.22f, .08f), new Vector2(.78f, .92f), Vector2.zero, Vector2.zero);
            pauseVolumeDownButton = CreateButton("降低音量", volumeCard.transform, "－", new Color(.07f, .21f, .28f, .98f));
            SetRect(pauseVolumeDownButton.GetComponent<RectTransform>(), new Vector2(.04f, .14f), new Vector2(.18f, .86f), Vector2.zero, Vector2.zero);
            pauseVolumeDownButton.onClick.AddListener(() => AdjustMasterVolume(-.10f));
            pauseVolumeUpButton = CreateButton("提高音量", volumeCard.transform, "＋", new Color(.10f, .28f, .27f, .98f));
            SetRect(pauseVolumeUpButton.GetComponent<RectTransform>(), new Vector2(.82f, .14f), new Vector2(.96f, .86f), Vector2.zero, Vector2.zero);
            pauseVolumeUpButton.onClick.AddListener(() => AdjustMasterVolume(.10f));

            settingsAchievementCountText = CreateText("成就完成度", card.transform, string.Empty, 18,
                TextAnchor.MiddleLeft, new Color(.44f, .92f, 1f));
            SetRect(settingsAchievementCountText.rectTransform, new Vector2(.09f, .605f), new Vector2(.91f, .665f), Vector2.zero, Vector2.zero);
            GameObject achievementCard = CreatePanel("成就列表底板", card.transform,
                new Color(.008f, .028f, .038f, .94f), new Vector2(.075f, .18f), new Vector2(.925f, .605f));
            PolishPanel(achievementCard, new Color(.25f, .58f, .64f, .38f));
            settingsAchievementListText = CreateText("成就列表", achievementCard.transform, string.Empty, 15,
                TextAnchor.UpperLeft, new Color(.80f, .90f, .91f));
            settingsAchievementListText.supportRichText = true;
            settingsAchievementListText.lineSpacing = 1.18f;
            SetRect(settingsAchievementListText.rectTransform, new Vector2(.045f, .04f), new Vector2(.955f, .96f), Vector2.zero, Vector2.zero);

            Text storageHint = CreateText("存档位置说明", card.transform,
                "进度保存在当前 Windows 用户的 LocalLow / 梅溪回响 / SaveData 中 · 本地原子存档", 13,
                TextAnchor.MiddleCenter, new Color(.48f, .70f, .75f));
            SetRect(storageHint.rectTransform, new Vector2(.08f, .11f), new Vector2(.92f, .17f), Vector2.zero, Vector2.zero);
            pauseTerminalButton = CreateButton("打开终端", card.transform, "终 端",
                new Color(.18f, .105f, .30f, .99f));
            SetRect(pauseTerminalButton.GetComponent<RectTransform>(), new Vector2(.10f, .025f), new Vector2(.47f, .105f), Vector2.zero, Vector2.zero);
            pauseTerminalButton.onClick.AddListener(OpenTerminal);
            pauseSettingsBackButton = CreateButton("返回暂停页", card.transform, "返 回 暂 停 页",
                new Color(.055f, .30f, .34f, .99f));
            SetRect(pauseSettingsBackButton.GetComponent<RectTransform>(), new Vector2(.53f, .025f), new Vector2(.90f, .105f), Vector2.zero, Vector2.zero);
            pauseSettingsBackButton.onClick.AddListener(ClosePauseSettings);
            BuildTerminalUI();
            pauseSettingsPanel.SetActive(false);
        }

        private void BuildTerminalUI()
        {
            terminalPanel = CreatePanel("开发终端模态层", pauseSettingsPanel.transform,
                new Color(.001f, .005f, .010f, .94f), Vector2.zero, Vector2.one);
            GameObject card = CreatePanel("终端主卡", terminalPanel.transform,
                new Color(.006f, .024f, .031f, .995f), new Vector2(.205f, .19f), new Vector2(.795f, .81f));
            PolishPanel(card, new Color(.32f, .96f, .82f, .72f));

            Text eyebrow = CreateText("终端眉题", card.transform, "MEIXI LOCAL COMMAND CONSOLE  /  WHITELIST MODE", 13,
                TextAnchor.MiddleCenter, new Color(.38f, .88f, .76f));
            SetRect(eyebrow.rectTransform, new Vector2(.08f, .87f), new Vector2(.92f, .95f), Vector2.zero, Vector2.zero);
            Text title = CreateText("终端标题", card.transform, "声 纹 终 端", 38,
                TextAnchor.MiddleCenter, new Color(1f, .75f, .28f));
            SetRect(title.rectTransform, new Vector2(.08f, .73f), new Vector2(.92f, .87f), Vector2.zero, Vector2.zero);
            Text help = CreateText("终端帮助", card.transform,
                "输入授权指令后按 Enter 执行\nexit/zjh  退出游戏    ·    game-two/zjh  保存为第二章并重新载入", 15,
                TextAnchor.MiddleCenter, new Color(.72f, .84f, .86f));
            help.lineSpacing = 1.12f;
            SetRect(help.rectTransform, new Vector2(.08f, .57f), new Vector2(.92f, .73f), Vector2.zero, Vector2.zero);

            GameObject inputPlate = CreatePanel("终端输入框", card.transform,
                new Color(.008f, .070f, .068f, .98f), new Vector2(.085f, .41f), new Vector2(.915f, .55f));
            PolishPanel(inputPlate, new Color(.30f, .90f, .76f, .48f));
            terminalInput = inputPlate.AddComponent<InputField>();
            terminalInput.targetGraphic = inputPlate.GetComponent<Image>();
            terminalInput.lineType = InputField.LineType.SingleLine;
            terminalInput.contentType = InputField.ContentType.Standard;
            terminalInput.characterLimit = 64;
            terminalInput.caretColor = new Color(.72f, 1f, .88f);
            terminalInput.selectionColor = new Color(.18f, .72f, .64f, .55f);

            Text inputText = CreateText("终端输入文字", inputPlate.transform, string.Empty, 20,
                TextAnchor.MiddleLeft, new Color(.78f, 1f, .88f));
            SetRect(inputText.rectTransform, new Vector2(.04f, .08f), new Vector2(.96f, .92f), Vector2.zero, Vector2.zero);
            inputText.raycastTarget = true;
            Text placeholder = CreateText("终端输入提示", inputPlate.transform, "请输入指令…", 20,
                TextAnchor.MiddleLeft, new Color(.40f, .66f, .61f, .82f));
            placeholder.fontStyle = FontStyle.Italic;
            SetRect(placeholder.rectTransform, new Vector2(.04f, .08f), new Vector2(.96f, .92f), Vector2.zero, Vector2.zero);
            terminalInput.textComponent = inputText;
            terminalInput.placeholder = placeholder;

            terminalOutput = CreateText("终端输出", card.transform, "等待输入授权指令。", 15,
                TextAnchor.MiddleCenter, new Color(.54f, .84f, .79f));
            SetRect(terminalOutput.rectTransform, new Vector2(.08f, .27f), new Vector2(.92f, .39f), Vector2.zero, Vector2.zero);
            terminalSubmitButton = CreateButton("执行终端指令", card.transform, "执 行",
                new Color(.045f, .32f, .27f, .99f));
            SetRect(terminalSubmitButton.GetComponent<RectTransform>(), new Vector2(.10f, .08f), new Vector2(.47f, .23f), Vector2.zero, Vector2.zero);
            terminalSubmitButton.onClick.AddListener(SubmitTerminalCommand);
            terminalCloseButton = CreateButton("关闭终端", card.transform, "返 回",
                new Color(.15f, .14f, .25f, .99f));
            SetRect(terminalCloseButton.GetComponent<RectTransform>(), new Vector2(.53f, .08f), new Vector2(.90f, .23f), Vector2.zero, Vector2.zero);
            terminalCloseButton.onClick.AddListener(CloseTerminal);
            terminalPanel.SetActive(false);
        }

        private void BuildDefeatUI()
        {
            defeatPanel = CreatePanel("回响力耗尽界面", canvas.transform, Color.white, Vector2.zero, Vector2.one);
            ApplyTexture(defeatPanel, "Art/ChapterBackdrop");
            Canvas defeatCanvas = defeatPanel.AddComponent<Canvas>();
            defeatCanvas.overrideSorting = true;
            defeatCanvas.sortingOrder = 600;
            defeatPanel.AddComponent<GraphicRaycaster>();
            var veil = CreatePanel("失败背景雾幕", defeatPanel.transform,
                new Color(0.004f, 0.012f, 0.018f, 0.84f), Vector2.zero, Vector2.one);
            veil.GetComponent<Image>().raycastTarget = false;
            var cyanGlow = CreatePanel("溪蓝余响", defeatPanel.transform,
                new Color(0.02f, 0.42f, 0.48f, 0.16f), new Vector2(0f, 0.16f), new Vector2(0.56f, 0.84f));
            cyanGlow.GetComponent<Image>().raycastTarget = false;
            var redGlow = CreatePanel("失声暗红", defeatPanel.transform,
                new Color(0.52f, 0.018f, 0.035f, 0.14f), new Vector2(0.44f, 0.16f), new Vector2(1f, 0.84f));
            redGlow.GetComponent<Image>().raycastTarget = false;
            var card = CreatePanel("旅途终止卡片", defeatPanel.transform,
                new Color(0.010f, 0.035f, 0.047f, 0.975f), new Vector2(0.255f, 0.135f), new Vector2(0.745f, 0.865f));
            PolishPanel(card, new Color(0.28f, 0.82f, 0.90f, 0.74f));
            var topLine = CreatePanel("失声金线", card.transform, new Color(1f, .60f, .18f, .90f),
                new Vector2(.09f, .904f), new Vector2(.91f, .909f));
            topLine.GetComponent<Image>().raycastTarget = false;
            var sigil = CreatePanel("破碎魂音印记", card.transform, new Color(.62f, .90f, .94f, .72f),
                new Vector2(.43f, .76f), new Vector2(.57f, .91f)).GetComponent<Image>();
            sigil.sprite = beatSigilSprite;
            sigil.preserveAspect = true;
            sigil.raycastTarget = false;
            var eyebrow = CreateText("失败眉题", card.transform, "MEIXI VOICE ARCHIVE  /  SIGNAL LOST", 14,
                TextAnchor.MiddleCenter, new Color(0.42f, 0.82f, 0.88f));
            SetRect(eyebrow.rectTransform, new Vector2(.08f, .70f), new Vector2(.92f, .77f), Vector2.zero, Vector2.zero);
            var title = CreateText("失败标题", card.transform, "回 响 暂 时 沉 寂", 46,
                TextAnchor.MiddleCenter, new Color(1f, .73f, .30f));
            title.font = displayFont != null ? displayFont : uiFont;
            SetRect(title.rectTransform, new Vector2(.06f, .57f), new Vector2(.94f, .70f), Vector2.zero, Vector2.zero);
            DecorateText(title, new Color(0f, .05f, .07f, .96f), new Vector2(4f, -4f), true);
            var body = CreateText("失败说明", card.transform,
                "溪灵的噪声暂时压过了你的节拍。\n这不是终点——重新聆听提示，再把动作落在光圈收束的一刻。", 19,
                TextAnchor.MiddleCenter, new Color(.84f, .92f, .92f));
            SetRect(body.rectTransform, new Vector2(.10f, .43f), new Vector2(.90f, .57f), Vector2.zero, Vector2.zero);
            var archiveCard = CreatePanel("本次回响记录", card.transform,
                new Color(.015f, .075f, .090f, .92f), new Vector2(.12f, .30f), new Vector2(.88f, .43f));
            PolishPanel(archiveCard, new Color(.26f, .68f, .74f, .44f));
            defeatSummaryText = CreateText("失败统计", archiveCard.transform,
                "最佳连击  0    ·    成功回应  0    ·    失拍  0", 17,
                TextAnchor.MiddleCenter, new Color(.72f, .92f, .92f));
            SetRect(defeatSummaryText.rectTransform, new Vector2(.04f, .08f), new Vector2(.96f, .92f), Vector2.zero, Vector2.zero);
            defeatRetryButton = CreateButton("重新旅途", card.transform, "重 新 聆 听 · 从 本 章 开 始", new Color(.06f, .32f, .36f, .98f));
            SetRect(defeatRetryButton.GetComponent<RectTransform>(), new Vector2(.12f, .15f), new Vector2(.58f, .265f), Vector2.zero, Vector2.zero);
            defeatRetryButton.onClick.AddListener(RestartAfterDefeat);
            defeatQuitButton = CreateButton("失败退出", card.transform, "返 回 寂 静", new Color(.26f, .065f, .075f, .98f));
            SetRect(defeatQuitButton.GetComponent<RectTransform>(), new Vector2(.61f, .15f), new Vector2(.88f, .265f), Vector2.zero, Vector2.zero);
            defeatQuitButton.onClick.AddListener(QuitGame);
            var hint = CreateText("失败快捷提示", card.transform, "ENTER  重新聆听    ·    鼠标选择", 13,
                TextAnchor.MiddleCenter, new Color(.42f, .70f, .75f));
            SetRect(hint.rectTransform, new Vector2(.20f, .055f), new Vector2(.80f, .12f), Vector2.zero, Vector2.zero);
            defeatPanel.SetActive(false);
        }

        private void BuildGameplayGuideUI()
        {
            gameplayGuidePanel = CreatePanel("玩法与面板说明", canvas.transform,
                new Color(.004f, .012f, .018f, .90f), Vector2.zero, Vector2.one);
            Canvas guideCanvas = gameplayGuidePanel.AddComponent<Canvas>();
            guideCanvas.overrideSorting = true;
            guideCanvas.sortingOrder = 560;
            gameplayGuidePanel.AddComponent<GraphicRaycaster>();
            var card = CreatePanel("玩法说明主卡", gameplayGuidePanel.transform,
                new Color(.010f, .045f, .060f, .985f), new Vector2(.17f, .12f), new Vector2(.83f, .88f));
            PolishPanel(card, new Color(.28f, .86f, .92f, .72f));
            var line = CreatePanel("教学金线", card.transform, new Color(1f, .62f, .18f, .90f),
                new Vector2(.08f, .90f), new Vector2(.92f, .905f));
            line.GetComponent<Image>().raycastTarget = false;
            var archive = CreateText("教学档案标", card.transform, "MEIXI FIELD TERMINAL  /  QUICK GUIDE", 14,
                TextAnchor.MiddleCenter, new Color(.44f, .82f, .88f));
            SetRect(archive.rectTransform, new Vector2(.12f, .82f), new Vector2(.88f, .89f), Vector2.zero, Vector2.zero);
            gameplayGuideTitle = CreateText("教学标题", card.transform, "调 研 终 端", 45,
                TextAnchor.MiddleCenter, new Color(1f, .76f, .30f));
            gameplayGuideTitle.font = displayFont != null ? displayFont : uiFont;
            SetRect(gameplayGuideTitle.rectTransform, new Vector2(.08f, .69f), new Vector2(.92f, .82f), Vector2.zero, Vector2.zero);
            gameplayGuideSubtitle = CreateText("教学副标题", card.transform, "面板数据与探索方式", 19,
                TextAnchor.MiddleCenter, new Color(.64f, .90f, .94f));
            SetRect(gameplayGuideSubtitle.rectTransform, new Vector2(.10f, .62f), new Vector2(.90f, .69f), Vector2.zero, Vector2.zero);
            var dataCard = CreatePanel("面板数据说明底", card.transform,
                new Color(.014f, .076f, .091f, .92f), new Vector2(.08f, .25f), new Vector2(.92f, .61f));
            PolishPanel(dataCard, new Color(.24f, .64f, .70f, .38f));
            gameplayGuideBody = CreateText("教学正文", dataCard.transform, string.Empty, 19,
                TextAnchor.MiddleLeft, new Color(.86f, .94f, .94f));
            SetRect(gameplayGuideBody.rectTransform, new Vector2(.055f, .08f), new Vector2(.945f, .92f), Vector2.zero, Vector2.zero);
            gameplayGuideContinueButton = CreateButton("教学继续", card.transform, "我 已 了 解 · 开 始 聆 听",
                new Color(.055f, .34f, .38f, .98f));
            SetRect(gameplayGuideContinueButton.GetComponent<RectTransform>(), new Vector2(.27f, .11f), new Vector2(.73f, .21f), Vector2.zero, Vector2.zero);
            gameplayGuideContinueButton.onClick.AddListener(DismissGameplayGuide);
            var footer = CreateText("教学快捷提示", card.transform, "ENTER / SPACE / E  继续", 13,
                TextAnchor.MiddleCenter, new Color(.44f, .72f, .78f));
            SetRect(footer.rectTransform, new Vector2(.28f, .035f), new Vector2(.72f, .095f), Vector2.zero, Vector2.zero);
            gameplayGuidePanel.SetActive(false);
        }

        private static bool IsAutomatedPresentationRun()
        {
            foreach (string argument in Environment.GetCommandLineArgs())
            {
                if (!argument.StartsWith("-meixi", StringComparison.OrdinalIgnoreCase)) continue;
                if (argument.EndsWith("Capture", StringComparison.OrdinalIgnoreCase) ||
                    argument.EndsWith("Smoke", StringComparison.OrdinalIgnoreCase) ||
                    argument.Contains("Auto", StringComparison.OrdinalIgnoreCase)) return true;
            }
            return Application.isBatchMode;
        }

        private void ShowGameplayGuide(string title, string subtitle, string body, string buttonLabel, Action callback,
            bool forceForCapture = false)
        {
            if (gameplayGuidePanel == null || (!forceForCapture && IsAutomatedPresentationRun()))
            {
                callback?.Invoke();
                return;
            }

            gameplayGuideCallback = callback;
            gameplayGuidePreviousTimeScale = Time.timeScale > 0f ? Time.timeScale : 1f;
            gameplayGuidePreviousAudioPause = AudioListener.pause;
            gameplayGuideOpen = true;
            gameplayGuideTitle.text = title;
            gameplayGuideSubtitle.text = subtitle;
            gameplayGuideBody.text = body;
            Text buttonText = gameplayGuideContinueButton != null
                ? gameplayGuideContinueButton.GetComponentInChildren<Text>(true) : null;
            if (buttonText != null) buttonText.text = buttonLabel;
            gameplayGuidePanel.SetActive(true);
            gameplayGuidePanel.transform.SetAsLastSibling();
            Time.timeScale = 0f;
            AudioListener.pause = true;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            player?.ResetInputTransientState();
            orbitCamera?.ClearInputAfterFocus();
        }

        private void DismissGameplayGuide()
        {
            if (!gameplayGuideOpen) return;
            gameplayGuideOpen = false;
            if (gameplayGuidePanel != null) gameplayGuidePanel.SetActive(false);
            Time.timeScale = gameplayGuidePreviousTimeScale > 0f ? gameplayGuidePreviousTimeScale : 1f;
            AudioListener.pause = gameplayGuidePreviousAudioPause;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            player?.ResetInputTransientState();
            orbitCamera?.ClearInputAfterFocus();
            Action callback = gameplayGuideCallback;
            gameplayGuideCallback = null;
            callback?.Invoke();
        }

        private void ShowExplorationGuide(Action callback, bool forceForCapture = false)
        {
            if (explorationGuideShown)
            {
                callback?.Invoke();
                return;
            }
            explorationGuideShown = true;
            ShowGameplayGuide("调 研 终 端", "面板数据与乡村探索",
                "【左上】主线章节、当前任务与推进进度\n" +
                "【右上】回响力、魂音数量、已取得的村民线索\n" +
                "【中央指引】目标方向与距离；靠近人物或魂音时出现 E 提示\n\n" +
                "WASD  行走    ·    鼠标  环绕镜头    ·    E  对话 / 采集\n" +
                "SPACE  跳跃    ·    ESC  暂停与查看操作",
                "进 入 第 一 章", callback, forceForCapture);
        }

        private void ShowBattleGuide(Action callback, bool forceForCapture = false)
        {
            if (battleGuideShown)
            {
                callback?.Invoke();
                return;
            }
            battleGuideShown = true;
            ShowGameplayGuide("溪 灵 对 拍", "先看指令，再听光圈收束",
                "【中央魂音印记】由外向内收束；最亮的一刻就是有效拍点\n" +
                "【中央操作提示】实心按键代表短按；出现环形进度时需要持续按住直至充满\n" +
                "【底部水纹签】敌人阶段、阶段血量、判定结果与组合键说明\n\n" +
                "J  近战突进：必须处于有效距离    ·    K  远程音符波 / 破谱\n" +
                "SPACE  瞬身：红色锁定区收束时离开；落点过近仍会受伤\n" +
                "后续会出现左右声部：短按跟随收束点，持续声部不要提前松开。\n" +
                "阶段切换时会进入无伤休止，看到新提示后再开始下一拍。",
                "同 步 节 拍 · 开 始 战 斗", callback, forceForCapture);
        }

        private static void PolishPanel(GameObject panel, Color edgeColor)
        {
            var shadow = panel.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.72f);
            shadow.effectDistance = new Vector2(7f, -7f);
            var outline = panel.AddComponent<Outline>();
            outline.effectColor = edgeColor;
            outline.effectDistance = new Vector2(1.2f, -1.2f);
        }

        private void BuildDialogueUI()
        {
            dialoguePanel = CreatePanel("电影式水纹对话", canvas.transform, Color.white, new Vector2(0.035f, 0.025f), new Vector2(0.965f, 0.335f));
            ApplyTexture(dialoguePanel, "Art/QuestFrameV2");
            var archive = CreateText("声纹档案标记", dialoguePanel.transform, "MEIXI VOICE ARCHIVE  ·  乡村声音档案", 13, TextAnchor.MiddleLeft, new Color(0.42f, 0.76f, 0.80f));
            SetRect(archive.rectTransform, new Vector2(0.205f, 0.86f), new Vector2(0.67f, 0.96f), Vector2.zero, Vector2.zero);
            var portrait = CreatePanel("NPC肖像框", dialoguePanel.transform, new Color(0.012f, 0.045f, 0.060f, 0.98f), new Vector2(0.025f, 0.10f), new Vector2(0.185f, 0.91f));
            PolishPanel(portrait, new Color(0.24f, 0.82f, 0.92f, 0.48f));
            dialoguePortraitPlate = portrait.GetComponent<Image>();

            var portraitImageObject = CreatePanel("NPC真实肖像", portrait.transform, Color.white, new Vector2(0.035f, 0.035f), new Vector2(0.965f, 0.965f));
            ApplyTexture(portraitImageObject, "Art/NpcVillageElderPortrait");
            dialoguePortraitImage = portraitImageObject.GetComponent<Image>();
            dialoguePortraitImage.preserveAspect = true;
            dialoguePortraitImage.raycastTarget = false;
            npcDialoguePortraitSprite = dialoguePortraitImage.sprite;
            childDialoguePortraitSprite = LoadPortraitSprite("Art/NpcVillageChildPortrait");
            auntDialoguePortraitSprite = LoadPortraitSprite("Art/NpcVillageAuntPortrait");

            playerDialoguePortraitSprite = LoadPortraitSprite("Art/LinMoDialoguePortrait");

            var portraitShade = CreatePanel("肖像底部渐暗", portrait.transform, new Color(0.005f, 0.018f, 0.025f, 0.30f), new Vector2(0.035f, 0.035f), new Vector2(0.965f, 0.22f));
            portraitShade.GetComponent<Image>().raycastTarget = false;
            var portraitTag = CreateText("肖像标签", portrait.transform, "VOICE MEMORY", 11, TextAnchor.MiddleCenter, new Color(0.74f, 0.90f, 0.92f, 0.90f));
            SetRect(portraitTag.rectTransform, new Vector2(0.08f, 0.04f), new Vector2(0.92f, 0.18f), Vector2.zero, Vector2.zero);

            var speakerDivider = CreatePanel("说话者金线", dialoguePanel.transform, new Color(1f, 0.65f, 0.20f, 0.72f), new Vector2(0.198f, 0.18f), new Vector2(0.200f, 0.85f));
            speakerDivider.GetComponent<Image>().raycastTarget = false;
            dialogueSpeaker = CreateText("说话者", dialoguePanel.transform, "林默", 28, TextAnchor.MiddleLeft, new Color(1f, 0.75f, 0.30f));
            SetRect(dialogueSpeaker.rectTransform, new Vector2(0.22f, 0.67f), new Vector2(0.48f, 0.85f), Vector2.zero, Vector2.zero);
            dialogueRole = CreateText("说话者身份", dialoguePanel.transform, "城市调研员", 15, TextAnchor.MiddleLeft, new Color(0.48f, 0.82f, 0.88f));
            SetRect(dialogueRole.rectTransform, new Vector2(0.22f, 0.57f), new Vector2(0.58f, 0.70f), Vector2.zero, Vector2.zero);
            dialogueCounter = CreateText("对话进度", dialoguePanel.transform, "01 / 01", 14, TextAnchor.MiddleRight, new Color(0.52f, 0.76f, 0.80f));
            SetRect(dialogueCounter.rectTransform, new Vector2(0.78f, 0.69f), new Vector2(0.94f, 0.84f), Vector2.zero, Vector2.zero);
            dialogueText = CreateText("对白", dialoguePanel.transform, "", 25, TextAnchor.UpperLeft, new Color(0.96f, 0.98f, 0.97f));
            SetRect(dialogueText.rectTransform, new Vector2(0.22f, 0.14f), new Vector2(0.94f, 0.57f), Vector2.zero, Vector2.zero);
            var hint = CreateText("继续提示", dialoguePanel.transform, "E / SPACE  继续  ◆", 17, TextAnchor.LowerRight, new Color(0.55f, 0.90f, 1f));
            SetRect(hint.rectTransform, new Vector2(0.70f, 0.01f), new Vector2(0.94f, 0.17f), Vector2.zero, Vector2.zero);
            hint.gameObject.AddComponent<FancyTextPulse>().Fade = true;
            dialoguePanel.AddComponent<MeixiUiPanelReveal>().Configure(.20f, .965f);
            dialoguePanel.SetActive(false);
        }

        private static Sprite LoadPortraitSprite(string resourcePath)
        {
            Texture2D texture = Resources.Load<Texture2D>(resourcePath);
            return texture == null ? null : Sprite.Create(texture,
                new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
        }

        private void BuildRhythmUI()
        {
            rhythmPanel = CreatePanel("三维节拍战斗HUD", canvas.transform, Color.clear, Vector2.zero, Vector2.one);
            leftHandCuePlate = CreatePanel("第三阶段左手节拍台", rhythmPanel.transform,
                new Color(.018f, .055f, .082f, .82f), new Vector2(.225f, .642f), new Vector2(.425f, .710f));
            PolishPanel(leftHandCuePlate, new Color(.22f, .82f, 1f, .70f));
            rightHandCuePlate = CreatePanel("第三阶段右手节拍台", rhythmPanel.transform,
                new Color(.075f, .028f, .055f, .82f), new Vector2(.575f, .642f), new Vector2(.775f, .710f));
            PolishPanel(rightHandCuePlate, new Color(1f, .42f, .22f, .70f));
            Text leftTrackMark = CreateText("左手轨道标", leftHandCuePlate.transform, "", 13,
                TextAnchor.MiddleCenter, new Color(.42f, .88f, 1f));
            SetRect(leftTrackMark.rectTransform, new Vector2(.08f, .84f), new Vector2(.92f, .96f), Vector2.zero, Vector2.zero);
            Text rightTrackMark = CreateText("右手轨道标", rightHandCuePlate.transform, "", 13,
                TextAnchor.MiddleCenter, new Color(1f, .64f, .30f));
            SetRect(rightTrackMark.rectTransform, new Vector2(.08f, .84f), new Vector2(.92f, .96f), Vector2.zero, Vector2.zero);
            leftHandHoldHalo = CreatePanel("左手长按外层蓄力光圈", rhythmPanel.transform,
                new Color(1f, .70f, .08f, .28f), new Vector2(.185f, .275f), new Vector2(.465f, .745f)).GetComponent<Image>();
            leftHandHoldHalo.sprite = beatSigilSprite;
            leftHandHoldHalo.preserveAspect = true;
            leftHandHoldHalo.raycastTarget = false;
            leftHandRhythmFlash = CreatePanel("左手魂音节拍印记", rhythmPanel.transform,
                new Color(.42f, .92f, 1f, .82f), new Vector2(.215f, .315f), new Vector2(.435f, .695f)).GetComponent<Image>();
            leftHandRhythmFlash.sprite = beatSigilSprite;
            leftHandRhythmFlash.preserveAspect = true;
            leftHandRhythmFlash.raycastTarget = false;
            leftHandHoldProgressFill = CreatePanel("左手长按径向充能", rhythmPanel.transform,
                new Color(1f, .76f, .10f, .52f), new Vector2(.205f, .30f), new Vector2(.445f, .71f)).GetComponent<Image>();
            leftHandHoldProgressFill.sprite = beatSigilSprite;
            leftHandHoldProgressFill.preserveAspect = true;
            leftHandHoldProgressFill.type = Image.Type.Filled;
            leftHandHoldProgressFill.fillMethod = Image.FillMethod.Radial360;
            leftHandHoldProgressFill.fillOrigin = 2;
            leftHandHoldProgressFill.fillClockwise = true;
            leftHandHoldProgressFill.fillAmount = 0f;
            leftHandHoldProgressFill.raycastTarget = false;
            leftHandActionName = CreateText("左手节拍动作", rhythmPanel.transform, "左 手 声 部", 23,
                TextAnchor.MiddleCenter, new Color(.48f, .92f, 1f));
            SetRect(leftHandActionName.rectTransform, new Vector2(.195f, .685f), new Vector2(.455f, .755f), Vector2.zero, Vector2.zero);
            leftHandActionName.fontStyle = FontStyle.Bold;
            DecorateText(leftHandActionName, Color.black, new Vector2(3f, -3f), true);
            leftHandActionKey = CreateText("左手节拍按键", rhythmPanel.transform, "Q", 70,
                TextAnchor.MiddleCenter, Color.white);
            SetRect(leftHandActionKey.rectTransform, new Vector2(.265f, .425f), new Vector2(.385f, .575f), Vector2.zero, Vector2.zero);
            leftHandActionKey.fontStyle = FontStyle.Bold;
            DecorateText(leftHandActionKey, new Color(0f, .08f, .12f, .98f), new Vector2(4f, -4f), true);
            for (int segment = 0; segment < leftHandHoldRingSegments.Length; segment++)
            {
                float angle = segment / (float)leftHandHoldRingSegments.Length * Mathf.PI * 2f;
                Image pip = CreatePanel("长按进度环_" + segment, rhythmPanel.transform,
                    new Color(.38f, .28f, .08f, .76f), new Vector2(.325f, .51f), new Vector2(.325f, .51f)).GetComponent<Image>();
                RectTransform pipRect = pip.rectTransform;
                pipRect.sizeDelta = new Vector2(14f, 34f);
                pipRect.anchoredPosition = new Vector2(Mathf.Sin(angle) * 132f, Mathf.Cos(angle) * 132f);
                pipRect.localRotation = Quaternion.Euler(0f, 0f, -angle * Mathf.Rad2Deg);
                pip.raycastTarget = false;
                leftHandHoldRingSegments[segment] = pip;
            }
            rhythmFlash = CreatePanel("梅溪魂音节拍印记", rhythmPanel.transform, new Color(1f, 1f, 1f, 0.72f), new Vector2(0.415f, 0.34f), new Vector2(0.585f, 0.64f)).GetComponent<Image>();
            rhythmFlash.sprite = beatSigilSprite;
            rhythmFlash.preserveAspect = true;
            rhythmFlash.raycastTarget = false;
            combatActionName = CreateText("当前节拍动作", rhythmPanel.transform, "聆 听 敌 人", 22, TextAnchor.MiddleCenter, new Color(0.50f, 0.94f, 1f));
            SetRect(combatActionName.rectTransform, new Vector2(0.38f, 0.64f), new Vector2(0.62f, 0.70f), Vector2.zero, Vector2.zero);
            DecorateText(combatActionName, Color.black, new Vector2(2f, -2f), true);
            combatActionKey = CreateText("节拍回应按键", rhythmPanel.transform, "·", 58, TextAnchor.MiddleCenter, Color.white);
            SetRect(combatActionKey.rectTransform, new Vector2(0.44f, 0.43f), new Vector2(0.56f, 0.56f), Vector2.zero, Vector2.zero);
            DecorateText(combatActionKey, new Color(0f, 0.08f, 0.12f, 0.95f), new Vector2(3f, -3f), true);
            var ribbon = CreatePanel("战斗水纹签", rhythmPanel.transform, Color.white, new Vector2(0.10f, 0.012f), new Vector2(0.90f, 0.255f));
            ApplyTexture(ribbon, "Art/QuestFrameV2");
            rhythmTitle = CreateText("节奏标题", ribbon.transform, "BOSS  ·  失 调 溪 灵", 27, TextAnchor.MiddleCenter, new Color(1f, 0.78f, 0.30f));
            SetRect(rhythmTitle.rectTransform, new Vector2(0.08f, 0.73f), new Vector2(0.92f, 0.95f), Vector2.zero, Vector2.zero);
            combatPhaseLabel = CreateText("敌人阶段标签", ribbon.transform, "第一阶段 · 听潮", 17,
                TextAnchor.MiddleCenter, new Color(0.62f, 0.94f, 1f));
            SetRect(combatPhaseLabel.rectTransform, new Vector2(0.08f, 0.58f), new Vector2(0.92f, 0.74f), Vector2.zero, Vector2.zero);
            var healthTrack = CreatePanel("敌人大型阶段进度槽", ribbon.transform,
                new Color(0.015f, 0.030f, 0.038f, 0.96f), new Vector2(0.075f, 0.39f), new Vector2(0.925f, 0.56f));
            PolishPanel(healthTrack, new Color(0.28f, 0.88f, 1f, 0.82f));
            combatEnemyHealthChipFill = CreatePanel("敌人阶段延迟损伤条", healthTrack.transform,
                new Color(1f, .88f, .52f, .72f), new Vector2(0.015f, 0.15f), new Vector2(0.985f, 0.85f)).GetComponent<Image>();
            combatEnemyHealthChipFill.type = Image.Type.Filled;
            combatEnemyHealthChipFill.fillMethod = Image.FillMethod.Horizontal;
            combatEnemyHealthChipFill.fillOrigin = 0;
            combatEnemyHealthChipFill.fillAmount = 1f;
            combatEnemyHealthFill = CreatePanel("敌人阶段进度填充", healthTrack.transform,
                new Color(0.12f, 0.88f, 1f, 0.98f), new Vector2(0.015f, 0.15f), new Vector2(0.985f, 0.85f)).GetComponent<Image>();
            combatEnemyHealthFill.type = Image.Type.Filled;
            combatEnemyHealthFill.fillMethod = Image.FillMethod.Horizontal;
            combatEnemyHealthFill.fillOrigin = 0;
            combatEnemyHealthFill.fillAmount = 1f;
            for (int phase = 0; phase < combatPhaseSegments.Length; phase++)
            {
                float x0 = 0.075f + phase * 0.285f;
                combatPhaseSegments[phase] = CreatePanel("阶段指示_" + (phase + 1), ribbon.transform,
                    phase == 0 ? new Color(0.22f, 0.94f, 1f, 1f) : new Color(0.16f, 0.25f, 0.30f, 0.94f),
                    new Vector2(x0, 0.275f), new Vector2(x0 + 0.255f, 0.365f)).GetComponent<Image>();
                Text phaseMark = CreateText("阶段标记_" + (phase + 1), combatPhaseSegments[phase].transform,
                    phase == 0 ? "阶 段 Ⅰ" : phase == 1 ? "阶 段 Ⅱ" : "阶 段 Ⅲ", 12,
                    TextAnchor.MiddleCenter, Color.white);
                SetRect(phaseMark.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
                DecorateText(phaseMark, Color.black, new Vector2(1f, -1f), false);
            }
            rhythmJudge = CreateText("动作判定", ribbon.transform, "听节拍 · 看指令 · 在魂音归一时回应", 18, TextAnchor.MiddleCenter, new Color(0.45f, 0.95f, 1f));
            SetRect(rhythmJudge.rectTransform, new Vector2(0.08f, 0.14f), new Vector2(0.92f, 0.32f), Vector2.zero, Vector2.zero);
            rhythmScore = CreateText("动作提示", ribbon.transform, "SPACE 瞬身    ·    J 近战突进    ·    K 远程音符波", 14, TextAnchor.MiddleCenter, new Color(0.78f, 0.91f, 1f));
            SetRect(rhythmScore.rectTransform, new Vector2(0.06f, 0.015f), new Vector2(0.94f, 0.15f), Vector2.zero, Vector2.zero);

            combatPhaseBanner = CreatePanel("阶段切换大字幕", canvas.transform,
                new Color(0.008f, 0.015f, 0.022f, 0.90f), new Vector2(.285f, .405f), new Vector2(.715f, .595f));
            Canvas phaseCanvas = combatPhaseBanner.AddComponent<Canvas>();
            phaseCanvas.overrideSorting = true;
            phaseCanvas.sortingOrder = 500;
            combatPhaseBannerGroup = combatPhaseBanner.AddComponent<CanvasGroup>();
            combatPhaseBannerGroup.alpha = 0f;
            combatPhaseBannerText = CreateText("阶段切换文字", combatPhaseBanner.transform, "进入第二阶段", 40,
                TextAnchor.MiddleCenter, new Color(1f, 0.68f, 0.20f));
            combatPhaseBannerText.font = displayFont != null ? displayFont : uiFont;
            SetRect(combatPhaseBannerText.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            DecorateText(combatPhaseBannerText, new Color(0f, 0f, 0f, 0.98f), new Vector2(5f, -5f), true);
            Button phaseGuideButton = combatPhaseBanner.AddComponent<Button>();
            phaseGuideButton.targetGraphic = combatPhaseBanner.GetComponent<Image>();
            phaseGuideButton.onClick.AddListener(DismissPhaseThreeGuide);
            combatPhaseBanner.SetActive(false);
            leftHandCuePlate.SetActive(false);
            rightHandCuePlate.SetActive(false);
            leftHandHoldHalo.gameObject.SetActive(false);
            leftHandHoldProgressFill.gameObject.SetActive(false);
            leftHandRhythmFlash.gameObject.SetActive(false);
            leftHandActionKey.gameObject.SetActive(false);
            leftHandActionName.gameObject.SetActive(false);
            foreach (Image segment in leftHandHoldRingSegments)
                if (segment != null) segment.gameObject.SetActive(false);
            rhythmTrack = CreatePanel("兼容节拍轨", rhythmPanel.transform, Color.clear, new Vector2(0.49f, 0.49f), new Vector2(0.51f, 0.51f)).GetComponent<RectTransform>();
            rhythmPanel.SetActive(false);
        }

        private void BuildChapterUI()
        {
            chapterPanel = CreatePanel("章节转场", canvas.transform, Color.white, Vector2.zero, Vector2.one);
            ApplyTexture(chapterPanel, "Art/ChapterBackdrop");
            Text archiveMark = CreateText("章节题签", chapterPanel.transform, "MEIXI  ·  VOICE MEMORY", 14,
                TextAnchor.MiddleCenter, new Color(.42f, .76f, .78f, .86f));
            SetRect(archiveMark.rectTransform, new Vector2(.24f, .695f), new Vector2(.76f, .74f), Vector2.zero, Vector2.zero);
            chapterCardNumber = CreateText("章节编号", chapterPanel.transform, "第 一 章", 23, TextAnchor.MiddleCenter, new Color(0.55f, 0.87f, 0.87f));
            chapterCardNumber.font = displayFont != null ? displayFont : uiFont;
            SetRect(chapterCardNumber.rectTransform, new Vector2(0.34f, 0.62f), new Vector2(0.66f, 0.69f), Vector2.zero, Vector2.zero);
            chapterCardTitle = CreateText("章节大标题", chapterPanel.transform, chapterNames[0], 60, TextAnchor.MiddleCenter, new Color(.96f, .82f, .53f));
            chapterCardTitle.font = displayFont != null ? displayFont : uiFont;
            SetRect(chapterCardTitle.rectTransform, new Vector2(0.16f, 0.445f), new Vector2(0.84f, 0.62f), Vector2.zero, Vector2.zero);
            DecorateText(chapterCardTitle, new Color(0f, 0f, 0f, .78f), new Vector2(3f, -3f), false);
            chapterCardTitle.gameObject.AddComponent<FancyTextPulse>().Amount = 0.012f;
            chapterCardSubtitle = CreateText("章节副题", chapterPanel.transform, chapterSubtitles[0], 22, TextAnchor.MiddleCenter, new Color(0.88f, 0.92f, 0.90f));
            chapterCardSubtitle.font = displayFont != null ? displayFont : uiFont;
            SetRect(chapterCardSubtitle.rectTransform, new Vector2(0.22f, 0.345f), new Vector2(0.78f, 0.42f), Vector2.zero, Vector2.zero);
            Text chapterOrnament = CreateText("章节余音纹", chapterPanel.transform, "—  ◇  —", 19,
                TextAnchor.MiddleCenter, new Color(.55f, .78f, .72f, .72f));
            chapterOrnament.font = displayFont != null ? displayFont : uiFont;
            SetRect(chapterOrnament.rectTransform, new Vector2(.40f, .285f), new Vector2(.60f, .335f), Vector2.zero, Vector2.zero);
            chapterPanel.AddComponent<MeixiUiPanelReveal>().Configure(.34f, 1.035f);
            chapterPanel.SetActive(false);
        }

        private void BuildEndingUI()
        {
            endingPanel = CreatePanel("结局", canvas.transform, Color.white, Vector2.zero, Vector2.one);
            ApplyTexture(endingPanel, "Art/ChapterBackdrop");
            var title = CreateText("结局标题", endingPanel.transform, "第 一 章 · 溪 声 归 来", 62, TextAnchor.MiddleCenter, new Color(1f, 0.76f, 0.30f));
            SetRect(title.rectTransform, new Vector2(0.12f, 0.66f), new Vector2(0.88f, 0.79f), Vector2.zero, Vector2.zero);
            DecorateText(title, Color.black, new Vector2(4f, -4f), true);
            var story = CreateText("结局文字", endingPanel.transform,
                "溪灵散作水滴，沉默多年的泉声重新穿过石桥。\n\n林默第一次明白：声音并不只是可计算的波形。\n它记得孩子如何醒来，也记得土地如何被人聆听。\n\n——《梅溪回响》第一章体验结束——",
                29, TextAnchor.MiddleCenter, Color.white);
            SetRect(story.rectTransform, new Vector2(0.18f, 0.28f), new Vector2(0.82f, 0.64f), Vector2.zero, Vector2.zero);
            endingReplayButton = CreateButton("再次聆听", endingPanel.transform, "再 次 聆 听", new Color(0.62f, 0.18f, 0.07f, 0.96f));
            SetRect(endingReplayButton.GetComponent<RectTransform>(), new Vector2(0.31f, 0.12f), new Vector2(0.48f, 0.20f), Vector2.zero, Vector2.zero);
            endingReplayButton.onClick.AddListener(RestartFirstChapter);
            endingNextChapterButton = CreateButton("继续旅程", endingPanel.transform, "继 续 · 前 往 古 戏 台", new Color(0.06f, 0.30f, 0.34f, 0.98f));
            SetRect(endingNextChapterButton.GetComponent<RectTransform>(), new Vector2(0.52f, 0.12f), new Vector2(0.69f, 0.20f), Vector2.zero, Vector2.zero);
            endingNextChapterButton.onClick.AddListener(LoadSecondChapter);
            endingPanel.SetActive(false);
            if (loadingPanel != null) loadingPanel.SetActive(false);
        }

        private void BuildLoadingUI()
        {
            loadingPanel = CreatePanel("前往第二章加载页", canvas.transform, Color.white, Vector2.zero, Vector2.one);
            ApplyTexture(loadingPanel, "Art/ChapterBackdrop");
            var veil = CreatePanel("加载页暗幕", loadingPanel.transform, new Color(0.005f, 0.012f, 0.018f, .55f), Vector2.zero, Vector2.one);
            veil.GetComponent<Image>().raycastTarget = true;
            var eyebrow = CreateText("加载页眉题", loadingPanel.transform, "MEIXI VOICE ARCHIVE  /  OLD THEATRE", 18,
                TextAnchor.MiddleCenter, new Color(.40f, .86f, .92f));
            SetRect(eyebrow.rectTransform, new Vector2(.20f, .62f), new Vector2(.80f, .70f), Vector2.zero, Vector2.zero);
            var title = CreateText("加载标题", loadingPanel.transform, "第 二 章 · 古 厝 里 的 旧 曲", 54,
                TextAnchor.MiddleCenter, new Color(1f, .75f, .30f));
            title.font = displayFont != null ? displayFont : uiFont;
            SetRect(title.rectTransform, new Vector2(.12f, .42f), new Vector2(.88f, .61f), Vector2.zero, Vector2.zero);
            DecorateText(title, Color.black, new Vector2(4f, -4f), true);
            var hint = CreateText("加载提示", loadingPanel.transform, "正在穿过村口长廊，读取古戏台声纹……", 22,
                TextAnchor.MiddleCenter, new Color(.88f, .93f, .93f));
            SetRect(hint.rectTransform, new Vector2(.18f, .32f), new Vector2(.82f, .42f), Vector2.zero, Vector2.zero);
            hint.gameObject.AddComponent<FancyTextPulse>().Fade = true;
            var line = CreatePanel("加载金线", loadingPanel.transform, new Color(1f, .58f, .18f, .92f),
                new Vector2(.37f, .27f), new Vector2(.63f, .274f));
            line.GetComponent<Image>().raycastTarget = false;
            loadingPanel.SetActive(false);
        }

        private static bool IsPointerInsideButton(Button button, Vector2 screenPosition)
        {
            if (button == null || !button.gameObject.activeInHierarchy || !button.interactable) return false;
            return RectTransformUtility.RectangleContainsScreenPoint(
                button.GetComponent<RectTransform>(), screenPosition, null);
        }

        private void RestartFirstChapter()
        {
            if (sceneTransitionActive) return;
            MeixiLocalProgress.BeginNewJourney();
            BeginScreenTransition("重 新 聆 听", ReloadFirstChapterScene, .48f, .08f, 0f);
        }

        private void ReloadFirstChapterScene()
        {
            Time.timeScale = 1f;
            UnityEngine.SceneManagement.SceneManager.LoadScene(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
        }

        private GameObject CreatePanel(string label, Transform parent, Color color, Vector2 min, Vector2 max)
        {
            var obj = new GameObject(label);
            obj.transform.SetParent(parent, false);
            var image = obj.AddComponent<Image>();
            image.color = color;
            SetRect(image.rectTransform, min, max, Vector2.zero, Vector2.zero);
            return obj;
        }

        private void ApplyTexture(GameObject panel, string resourcePath)
        {
            var texture = Resources.Load<Texture2D>(resourcePath);
            if (texture == null) return;
            panel.GetComponent<Image>().sprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
        }

        private Text CreateText(string label, Transform parent, string content, int size, TextAnchor alignment, Color color)
        {
            var obj = new GameObject(label);
            obj.transform.SetParent(parent, false);
            var text = obj.AddComponent<Text>();
            text.font = uiFont;
            text.text = content;
            text.fontSize = size;
            text.alignment = alignment;
            text.color = color;
            text.supportRichText = true;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            return text;
        }

        private void DecorateText(Text text, Color shadowColor, Vector2 distance, bool outline)
        {
            var shadow = text.gameObject.AddComponent<Shadow>();
            shadow.effectColor = shadowColor;
            shadow.effectDistance = distance;
            if (outline)
            {
                var edge = text.gameObject.AddComponent<Outline>();
                edge.effectColor = new Color(0.04f, 0.015f, 0f, 0.9f);
                edge.effectDistance = new Vector2(1.5f, -1.5f);
            }
        }

        private Button CreateButton(string label, Transform parent, string content, Color color)
        {
            var obj = CreatePanel(label, parent, Color.white, Vector2.zero, Vector2.one);
            ApplyTexture(obj, "Art/QuestFrameV2");
            var button = obj.AddComponent<Button>();
            button.targetGraphic = obj.GetComponent<Image>();
            var colors = button.colors;
            colors.highlightedColor = new Color(0.72f, 0.93f, 1f, 1f);
            colors.pressedColor = new Color(1f, 0.67f, 0.28f, 1f);
            button.colors = colors;
            var text = CreateText("按钮文字", obj.transform, content, 28, TextAnchor.MiddleCenter, Color.white);
            text.raycastTarget = false;
            SetRect(text.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            DecorateText(text, Color.black, new Vector2(2f, -2f), false);
            return button;
        }

        private static void SetRect(RectTransform rect, Vector2 min, Vector2 max, Vector2 offsetMin, Vector2 offsetMax)
        {
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
        }

        private void ShowTitle()
        {
            paused = false;
            Time.timeScale = 1f;
            MeixiLocalProgress.ApplyAudioSetting();
            sceneTransitionActive = false;
            chapterCardTransitionPending = false;
            titlePanel.SetActive(true);
            hudPanel.SetActive(false);
            dialoguePanel.SetActive(false);
            rhythmPanel.SetActive(false);
            chapterPanel.SetActive(false);
            endingPanel.SetActive(false);
            if (loadingPanel != null) loadingPanel.SetActive(false);
            if (openingPanel != null) openingPanel.SetActive(false);
            if (pausePanel != null) pausePanel.SetActive(false);
            ClosePauseSettings();
            if (titleDeleteConfirmPanel != null) titleDeleteConfirmPanel.SetActive(false);
            RefreshTitleSaveUi();
            if (interactionCard != null) interactionCard.SetActive(false);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            if (transitionGroup != null)
            {
                transitionGroup.alpha = 1f;
                transitionGroup.blocksRaycasts = true;
                transitionCaption.text = "梅 溪 回 响";
                FadeBackToGame(.72f);
            }
        }

        private void TryStartCaptureMode()
        {
            string[] arguments = Environment.GetCommandLineArgs();
            bool titleRequested = Array.Exists(arguments, value => string.Equals(value, "-meixiTitleCapture", StringComparison.OrdinalIgnoreCase));
            bool settingsRequested = Array.Exists(arguments, value => string.Equals(value, "-meixiSettingsCapture", StringComparison.OrdinalIgnoreCase));
            bool openingRequested = Array.Exists(arguments, value => string.Equals(value, "-meixiOpeningCapture", StringComparison.OrdinalIgnoreCase));
            bool chapterRequested = Array.Exists(arguments, value => string.Equals(value, "-meixiChapterCapture", StringComparison.OrdinalIgnoreCase));
            bool villageRequested = Array.Exists(arguments, value => string.Equals(value, "-meixiVillageCapture", StringComparison.OrdinalIgnoreCase));
            bool dialogueRequested = Array.Exists(arguments, value => string.Equals(value, "-meixiDialogueCapture", StringComparison.OrdinalIgnoreCase));
            bool childRequested = Array.Exists(arguments, value => string.Equals(value, "-meixiChildCapture", StringComparison.OrdinalIgnoreCase));
            bool guideRequested = Array.Exists(arguments, value => string.Equals(value, "-meixiGuideCapture", StringComparison.OrdinalIgnoreCase));
            bool battleGuideRequested = Array.Exists(arguments, value => string.Equals(value, "-meixiBattleGuideCapture", StringComparison.OrdinalIgnoreCase));
            bool animationRequested = Array.Exists(arguments, value => string.Equals(value, "-meixiAnimationCapture", StringComparison.OrdinalIgnoreCase));
            bool animationAltRequested = Array.Exists(arguments, value => string.Equals(value, "-meixiAnimationAltCapture", StringComparison.OrdinalIgnoreCase));
            bool rhythmAutoRequested = Array.Exists(arguments, value => string.Equals(value, "-meixiRhythmAuto", StringComparison.OrdinalIgnoreCase));
            bool finisherRequested = Array.Exists(arguments, value => string.Equals(value, "-meixiFinisherCapture", StringComparison.OrdinalIgnoreCase));
            bool meleeRequested = Array.Exists(arguments, value => string.Equals(value, "-meixiMeleeCapture", StringComparison.OrdinalIgnoreCase));
            bool rangedCounterRequested = Array.Exists(arguments, value => string.Equals(value, "-meixiRangedCounterCapture", StringComparison.OrdinalIgnoreCase));
            bool wrongMeleeRequested = Array.Exists(arguments, value => string.Equals(value, "-meixiWrongMeleeCapture", StringComparison.OrdinalIgnoreCase));
            bool flowRequested = Array.Exists(arguments, value => string.Equals(value, "-meixiFlowCapture", StringComparison.OrdinalIgnoreCase));
            bool defeatRequested = Array.Exists(arguments, value => string.Equals(value, "-meixiDefeatCapture", StringComparison.OrdinalIgnoreCase));
            bool phaseRequested = Array.Exists(arguments, value => string.Equals(value, "-meixiPhaseCapture", StringComparison.OrdinalIgnoreCase));
            bool healthRequested = Array.Exists(arguments, value => string.Equals(value, "-meixiHealthCapture", StringComparison.OrdinalIgnoreCase));
            bool dualHandRequested = Array.Exists(arguments, value => string.Equals(value, "-meixiDualHandCapture", StringComparison.OrdinalIgnoreCase));
            bool phaseThreeGuideRequested = Array.Exists(arguments, value => string.Equals(value, "-meixiPhaseThreeGuideCapture", StringComparison.OrdinalIgnoreCase));
            bool dashRequested = Array.Exists(arguments, value => string.Equals(value, "-meixiEnemyDashCapture", StringComparison.OrdinalIgnoreCase));
            bool tripleSlashRequested = Array.Exists(arguments, value => string.Equals(value, "-meixiTripleSlashCapture", StringComparison.OrdinalIgnoreCase));
            bool requested = Application.isBatchMode || titleRequested || settingsRequested || openingRequested || chapterRequested || villageRequested || dialogueRequested || childRequested || guideRequested || battleGuideRequested || animationRequested || animationAltRequested || rhythmAutoRequested || finisherRequested || meleeRequested ||
                             rangedCounterRequested || wrongMeleeRequested || flowRequested || defeatRequested || phaseRequested || healthRequested || dualHandRequested || phaseThreeGuideRequested || dashRequested || tripleSlashRequested ||
                             Array.Exists(arguments, value => string.Equals(value, "-meixiCapture", StringComparison.OrdinalIgnoreCase));
            if (!requested) return;
            captureMode = true;
            if (titleRequested)
            {
                captureFileName = "MeixiTitleCapture.png";
                captureAt = float.PositiveInfinity;
                titleCaptureWhenReady = true;
                ShowTitle();
                return;
            }
            if (settingsRequested)
            {
                CompleteTransitionImmediatelyForValidation();
                captureFileName = "MeixiSettingsCapture.png";
                captureAt = Time.unscaledTime + .65f;
                started = true;
                titlePanel.SetActive(false);
                hudPanel.SetActive(true);
                openingActive = false;
                openingPanel.SetActive(false);
                dialoguePanel.SetActive(false);
                chapterPanel.SetActive(false);
                endingPanel.SetActive(false);
                SetPaused(true);
                OpenPauseSettings();
                return;
            }
            if (openingRequested)
            {
                captureFileName = "MeixiOpeningCapture.png";
                captureAt = float.PositiveInfinity;
                openingCaptureWhenReady = true;
                return;
            }
            if (chapterRequested)
            {
                CompleteTransitionImmediatelyForValidation();
                captureFileName = "MeixiChapterCapture.png";
                captureAt = Time.unscaledTime + .85f;
                started = true;
                titlePanel.SetActive(false);
                hudPanel.SetActive(false);
                openingActive = false;
                openingPanel.SetActive(false);
                dialoguePanel.SetActive(false);
                endingPanel.SetActive(false);
                pausePanel.SetActive(false);
                ShowChapterCard(-1, "第 一 章", "没 有 声 音 的 村 庄",
                    "城市的喧闹，在山路尽头悄然止息", null);
                return;
            }
            if (guideRequested || battleGuideRequested)
            {
                CompleteTransitionImmediatelyForValidation();
                captureFileName = battleGuideRequested ? "MeixiBattleGuideCapture.png" : "MeixiGuideCapture.png";
                captureAt = Time.unscaledTime + .85f;
                started = true;
                titlePanel.SetActive(false);
                hudPanel.SetActive(true);
                openingActive = false;
                openingPanel.SetActive(false);
                dialoguePanel.SetActive(false);
                chapterPanel.SetActive(false);
                endingPanel.SetActive(false);
                pausePanel.SetActive(false);
                if (battleGuideRequested) ShowBattleGuide(null, true);
                else ShowExplorationGuide(null, true);
                return;
            }
            if (flowRequested)
            {
                captureFileName = "MeixiInteractionCombatCapture.png";
                captureAt = Time.unscaledTime + 14f;
                started = true;
                titlePanel.SetActive(false);
                hudPanel.SetActive(true);
                openingActive = false;
                if (openingPanel != null) openingPanel.SetActive(false);
                dialoguePanel.SetActive(false);
                chapterPanel.SetActive(false);
                endingPanel.SetActive(false);
                pausePanel.SetActive(false);
                if (interactionCard != null) interactionCard.SetActive(false);
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                StartCoroutine(ValidateInteractionToCombatFlow());
                return;
            }
            if (childRequested)
            {
                CompleteTransitionImmediatelyForValidation();
                captureFileName = "MeixiChildCapture.png";
                captureAt = Time.unscaledTime + 1.4f;
                started = true;
                titlePanel.SetActive(false);
                hudPanel.SetActive(false);
                openingActive = false;
                if (openingPanel != null) openingPanel.SetActive(false);
                dialoguePanel.SetActive(false);
                chapterPanel.SetActive(false);
                endingPanel.SetActive(false);
                pausePanel.SetActive(false);
                if (interactionCard != null) interactionCard.SetActive(false);

                VillageNpc3D childNpc = null;
                foreach (VillageNpc3D npc in villageNpcs)
                {
                    if (npc != null && npc.DisplayName != null &&
                        (npc.DisplayName.Contains("小孩", StringComparison.Ordinal) ||
                         npc.DisplayName.Contains("阿禾", StringComparison.Ordinal)))
                    {
                        childNpc = npc;
                        break;
                    }
                }
                if (childNpc != null && orbitCamera != null)
                {
                    Camera previewCamera = orbitCamera.GetComponent<Camera>();
                    Vector3 childForward = childNpc.transform.forward;
                    childForward.y = 0f;
                    if (childForward.sqrMagnitude < .01f) childForward = Vector3.forward;
                    childForward.Normalize();
                    Vector3 focus = childNpc.transform.position + Vector3.up * .72f;
                    orbitCamera.enabled = false;
                    previewCamera.transform.position = focus + childForward * 2.35f + Vector3.up * .05f;
                    previewCamera.transform.rotation = Quaternion.LookRotation(focus - previewCamera.transform.position, Vector3.up);
                    previewCamera.fieldOfView = 32f;
                    previewCamera.nearClipPlane = .03f;
                }
                else Debug.LogError("[MeixiChildCapture] Child NPC or orbit camera was not available.");
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                return;
            }
            if (villageRequested || dialogueRequested)
            {
                // Environment review captures must not land on the startup/title
                // fade. Normal players still see the complete transition sequence.
                CompleteTransitionImmediatelyForValidation();
                captureFileName = dialogueRequested ? "MeixiDialogueCapture.png" : "MeixiVillageCapture.png";
                captureAt = dialogueRequested ? float.PositiveInfinity : Time.unscaledTime + 2.2f;
                started = true;
                titlePanel.SetActive(false);
                hudPanel.SetActive(!dialogueRequested);
                openingActive = false;
                openingPanel.SetActive(false);
                chapterPanel.SetActive(false);
                endingPanel.SetActive(false);
                pausePanel.SetActive(false);
                if (interactionCard != null) interactionCard.SetActive(false);
                CharacterController previewController = playerTransform != null ? playerTransform.GetComponent<CharacterController>() : null;
                if (previewController != null) previewController.enabled = false;
                if (playerTransform != null)
                {
                    playerTransform.position = new Vector3(0.2f, 0.30f, 1.2f);
                    playerTransform.rotation = Quaternion.Euler(0f, 24f, 0f);
                }
                if (previewController != null) previewController.enabled = true;
                if (villageRequested && orbitCamera != null)
                {
                    // Automated environment review needs a stable establishing shot.
                    // The regular follow camera eases from its authored scene position
                    // and can briefly pass through the player after the preview teleport.
                    Camera previewCamera = orbitCamera.GetComponent<Camera>();
                    orbitCamera.enabled = false;
                    previewCamera.transform.position = new Vector3(-18.5f, 11.8f, -20.5f);
                    previewCamera.transform.rotation = Quaternion.LookRotation(
                        new Vector3(0.5f, 2.2f, 6.5f) - previewCamera.transform.position,
                        Vector3.up);
                }
                if (dialogueRequested) StartCoroutine(PrepareDialogueCapture());
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                return;
            }
            if (animationRequested || animationAltRequested)
            {
                CompleteTransitionImmediatelyForValidation();
                captureFileName = animationAltRequested ? "MeixiAnimationAltCapture.png" : "MeixiAnimationCapture.png";
                // Capture inside the locomotion window (0.00-0.88s). The previous
                // 1.86s timestamp landed on the heavy-attack preview and could not
                // reveal shoulder snapping or an asymmetric arm swing.
                captureAt = float.PositiveInfinity;
                started = true;
                titlePanel.SetActive(false);
                hudPanel.SetActive(false);
                openingActive = false;
                if (openingPanel != null) openingPanel.SetActive(false);
                dialoguePanel.SetActive(false);
                chapterPanel.SetActive(false);
                endingPanel.SetActive(false);
                pausePanel.SetActive(false);
                if (interactionCard != null) interactionCard.SetActive(false);
                MeixiThirdPersonPlayer previewPlayer = player != null ? player : FindAnyObjectByType<MeixiThirdPersonPlayer>();
                if (previewPlayer != null)
                {
                    player = previewPlayer;
                    previewPlayer.BeginAnimationPreview();
                    StartCoroutine(ScheduleAnimationCapture());
                }
                else Debug.LogError("[MeixiAnimationPreview] No runtime player was available for locomotion validation.");
                orbitCamera?.BeginAnimationPreviewFocus();
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                return;
            }
            rhythmAutoValidation = rhythmAutoRequested;
            rhythmFinisherValidation = finisherRequested;
            meleeAutoValidation = meleeRequested;
            rangedCounterValidation = rangedCounterRequested;
            wrongMeleeValidation = wrongMeleeRequested;
            captureFileName = finisherRequested ? "MeixiFinisherCapture.png" : meleeRequested ? "MeixiMeleeCapture.png" :
                rangedCounterRequested ? "MeixiRangedCounterCapture.png" : wrongMeleeRequested ? "MeixiWrongMeleeCapture.png" :
                rhythmAutoRequested ? "MeixiRhythmSuccessCapture.png" : defeatRequested ? "MeixiDefeatCapture.png" :
                dualHandRequested ? "MeixiDualHandCapture.png" : dashRequested ? "MeixiEnemyDashCapture.png" :
                tripleSlashRequested ? "MeixiTripleSlashCapture.png" : phaseRequested ? "MeixiPhaseCapture.png" :
                phaseThreeGuideRequested ? "MeixiPhaseThreeGuideCapture.png" : healthRequested ? "MeixiHealthCapture.png" : "MeixiCapture.png";
            // 自动战斗验证由实际动作事件触发截图，避免机器帧率差异把画面截在动作之前。
            captureAt = Time.unscaledTime + (finisherRequested || rhythmAutoRequested || meleeRequested || rangedCounterRequested || wrongMeleeRequested ? 10f : 2.42f);
            started = true;
            titlePanel.SetActive(false);
            hudPanel.SetActive(true);
            openingActive = false;
            if (openingPanel != null) openingPanel.SetActive(false);
            dialoguePanel.SetActive(false);
            chapterPanel.SetActive(false);
            endingPanel.SetActive(false);
            pausePanel.SetActive(false);
            SetObjective(0);
            CharacterController captureController = playerTransform != null ? playerTransform.GetComponent<CharacterController>() : null;
            if (captureController != null) captureController.enabled = false;
            if (playerTransform != null)
            {
                playerTransform.position = new Vector3(-8.4f, 0.30f, -2.4f);
                playerTransform.forward = new Vector3(-0.72f, 0f, -0.69f);
                if ((meleeRequested || wrongMeleeRequested) && creekSpirit != null)
                {
                    Vector3 away = playerTransform.position - creekSpirit.transform.position;
                    away.y = 0f;
                    if (away.sqrMagnitude < 0.01f) away = Vector3.back;
                    Vector3 meleeCapturePosition = creekSpirit.transform.position + away.normalized * (wrongMeleeRequested ? 2.45f : 4.25f);
                    meleeCapturePosition.y = 0.30f;
                    playerTransform.position = meleeCapturePosition;
                    playerTransform.forward = -away.normalized;
                }
            }
            if (captureController != null) captureController.enabled = true;
            pendingNode = nodes.Count > 0 ? nodes[0] : null;
            ActivateChapterOneCombat();
            if (finisherRequested) creekSpirit?.PrepareFinisherValidation(chapterOneBeatStart);
            else if (dashRequested) creekSpirit?.PrepareSpecificAttackValidation(3, chapterOneBeatStart);
            else if (rangedCounterRequested || wrongMeleeRequested) creekSpirit?.PrepareSpecificAttackValidation(2, chapterOneBeatStart);
            else if (rhythmAutoRequested || meleeRequested) creekSpirit?.PrepareAttackValidation(chapterOneBeatStart);
            if (phaseRequested)
            {
                CompleteTransitionImmediatelyForValidation();
                creekSpirit?.PreparePhaseValidation(2);
                captureAt = Time.unscaledTime + 0.48f;
            }
            if (dualHandRequested)
            {
                CompleteTransitionImmediatelyForValidation();
                creekSpirit?.PreparePhaseValidation(3);
                captureAt = float.PositiveInfinity;
                StartCoroutine(ScheduleDualHandHoldReviewCapture());
            }
            if (phaseThreeGuideRequested)
            {
                CompleteTransitionImmediatelyForValidation();
                creekSpirit?.PreparePhaseValidation(3);
                captureAt = Time.unscaledTime + .35f;
            }
            if (dashRequested)
            {
                captureAt = float.PositiveInfinity;
                StartCoroutine(ScheduleCombatReviewCapture(1.72f));
            }
            if (tripleSlashRequested)
            {
                CompleteTransitionImmediatelyForValidation();
                creekSpirit?.PreparePhaseValidation(2);
                captureAt = float.PositiveInfinity;
                StartCoroutine(ValidateTriplePhaseSlashCapture());
            }
            if (defeatRequested)
            {
                health = 1;
                DamagePlayer();
                captureAt = Time.unscaledTime + 0.48f;
            }
            else if (healthRequested)
            {
                CompleteTransitionImmediatelyForValidation();
                health = Mathf.Max(0, health - 1);
                UpdateHealth();
                captureAt = Time.unscaledTime + 0.22f;
            }
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private IEnumerator ScheduleCombatReviewCapture(float delay)
        {
            // Heavy runtime scene generation can consume several seconds before the
            // first rendered Update. Cross two frame boundaries before starting the
            // review timer so captures cannot preserve the phase-one layout.
            yield return null;
            yield return null;
            yield return new WaitForSecondsRealtime(Mathf.Max(.1f, delay));
            captureAt = Time.unscaledTime;
        }

        private IEnumerator ScheduleDualHandHoldReviewCapture()
        {
            yield return null;
            yield return null;
            if (phaseThreeGuideOpen)
            {
                yield return new WaitForSecondsRealtime(.45f);
                DismissPhaseThreeGuide();
            }
            float deadline = Time.unscaledTime + 10f;
            while (Time.unscaledTime < deadline && (!leftHandCueActive || !leftHandHoldRequired))
                yield return null;
            if (leftHandCueActive && leftHandHoldRequired)
            {
                dualHandHoldPreviewActive = true;
                leftHandHolding = true;
                leftHandHoldStartedDsp = AudioSettings.dspTime;
                float previewDuration = Mathf.Max(.22f, expectedLeftHandHoldDuration * .56f);
                float startedAt = Time.unscaledTime;
                while (Time.unscaledTime - startedAt < previewDuration)
                {
                    leftHandHoldProgress = Mathf.Clamp01((Time.unscaledTime - startedAt) /
                        Mathf.Max(.05f, expectedLeftHandHoldDuration));
                    if (leftHandActionName != null)
                        leftHandActionName.text = $"长 按 中 · 不 要 松 开  {Mathf.RoundToInt(leftHandHoldProgress * 100f)}%";
                    yield return null;
                }
            }
            captureAt = Time.unscaledTime;
        }

        private IEnumerator ValidateTriplePhaseSlashCapture()
        {
            yield return null;
            while (CombatPhaseResting) yield return null;
            yield return new WaitForSecondsRealtime(.12f);
            EnemyMusicAttackVfx3D.ClearAll();
            TrackingAttackTelegraph3D.ClearAll();
            MeleeForbiddenRing3D.ClearAll(false);
            for (int step = 0; step < 3; step++)
            {
                player?.ExecuteAuthoredRhythmAction(CombatBeatAction.TripleJ, CombatInputKey.J, step);
                yield return new WaitForSecondsRealtime(step == 2 ? .16f : .17f);
            }
            captureAt = Time.unscaledTime;
        }

        private IEnumerator ScheduleAnimationCapture()
        {
            // Startup asset import can advance realtime before the first rendered
            // LateUpdate. Always cross a frame boundary, then sample stable gait.
            yield return null;
            yield return new WaitForSecondsRealtime(.72f);
            captureAt = Time.unscaledTime;
        }

        private IEnumerator PrepareDialogueCapture()
        {
            yield return null;
            ShowDialogue(new[]
            {
                "老村长｜人走远了，声音会替他认路。",
                "林默｜所以你们说的魂音，不是传说，而是所有人留下的记忆？"
            }, null);
            ShowToast("新目标 · 聆听村落留下的声音", new Color(0.42f, 0.92f, 1f));
            captureAt = Time.unscaledTime + 0.72f;
        }

        private IEnumerator ValidateInteractionToCombatFlow()
        {
            yield return null;

            int safety = 0;
            foreach (VillageNpc3D npc in villageNpcs)
            {
                if (npc == null) continue;
                npc.Interact();
                yield return null;
                Debug.Log($"[MeixiValidation] NPC interaction dialogueOpen={dialogueOpen} npc={npc.DisplayName}");
                safety = 0;
                while (dialogueOpen && safety++ < 24)
                {
                    AdvanceDialogue();
                    yield return new WaitForSecondsRealtime(0.035f);
                }
            }

            SoulNode3D node = nodes.Find(candidate => candidate != null && !candidate.Completed);
            if (node == null)
            {
                Debug.LogError("[MeixiValidation] No soul node available for chapter-one flow.");
                captureAt = Time.unscaledTime + 0.25f;
                yield break;
            }

            CharacterController flowController = playerTransform != null ? playerTransform.GetComponent<CharacterController>() : null;
            if (flowController != null) flowController.enabled = false;
            if (playerTransform != null)
            {
                Vector3 nearNode = node.transform.position + new Vector3(2.15f, 0f, 0.25f);
                nearNode.y = 0.30f;
                playerTransform.position = nearNode;
                Vector3 faceNode = node.transform.position - playerTransform.position;
                faceNode.y = 0f;
                if (faceNode.sqrMagnitude > 0.01f) playerTransform.forward = faceNode.normalized;
            }
            if (flowController != null) flowController.enabled = true;

            node.Interact();
            float presentationDeadline = Time.unscaledTime + 6f;
            while (!dialogueOpen && Time.unscaledTime < presentationDeadline) yield return null;
            while (sceneTransitionActive && Time.unscaledTime < presentationDeadline) yield return null;
            safety = 0;
            while (dialogueOpen && safety++ < 32)
            {
                AdvanceDialogue();
                yield return new WaitForSecondsRealtime(0.035f);
            }

            float deadline = Time.unscaledTime + 3f;
            while (!chapterOneCombatActive && Time.unscaledTime < deadline) yield return null;
            if (chapterOneCombatActive)
            {
                Debug.Log("[MeixiValidation] SUCCESS npc-dialogue -> soul-memory -> chapter-one-combat.");
                captureAt = Time.unscaledTime + 1.35f;
            }
            else
            {
                Debug.LogError("[MeixiValidation] FAILED chapter-one combat did not activate.");
                captureAt = Time.unscaledTime + 0.25f;
            }
        }

        private void CaptureFrame(string path)
        {
            Camera camera = orbitCamera != null ? orbitCamera.GetComponent<Camera>() : Camera.main;
            if (camera == null) return;
            var target = new RenderTexture(1280, 720, 24, RenderTextureFormat.ARGB32);
            RenderTexture previousTarget = camera.targetTexture;
            RenderTexture previousActive = RenderTexture.active;
            RenderMode previousMode = canvas.renderMode;
            Camera previousCanvasCamera = canvas.worldCamera;
            float previousPlaneDistance = canvas.planeDistance;

            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera;
            canvas.planeDistance = 0.55f;
            Canvas.ForceUpdateCanvases();
            camera.targetTexture = target;
            RenderTexture.active = target;
            camera.Render();
            var texture = new Texture2D(target.width, target.height, TextureFormat.RGB24, false);
            texture.ReadPixels(new Rect(0f, 0f, target.width, target.height), 0, 0);
            texture.Apply();
            File.WriteAllBytes(path, texture.EncodeToPNG());

            camera.targetTexture = previousTarget;
            RenderTexture.active = previousActive;
            canvas.renderMode = previousMode;
            canvas.worldCamera = previousCanvasCamera;
            canvas.planeDistance = previousPlaneDistance;
            Destroy(texture);
            target.Release();
            Destroy(target);
        }

        private void TogglePause()
        {
            if (!started || secondChapterLoading || finishingStrike || defeatOpen) return;
            SetPaused(!paused);
        }

        private void OpenPauseSettings()
        {
            if (!paused || pauseSettingsPanel == null) return;
            pauseSettingsOpen = true;
            RefreshPauseSettingsUi();
            pauseSettingsPanel.SetActive(true);
            pauseSettingsPanel.transform.SetAsLastSibling();
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private void ClosePauseSettings()
        {
            CloseTerminal();
            pauseSettingsOpen = false;
            if (pauseSettingsPanel != null) pauseSettingsPanel.SetActive(false);
            if (paused && pausePanel != null) pausePanel.transform.SetAsLastSibling();
        }

        private void OpenTerminal()
        {
            if (!paused || !pauseSettingsOpen || terminalPanel == null) return;
            terminalCommandExecuting = false;
            terminalPanel.SetActive(true);
            terminalPanel.transform.SetAsLastSibling();
            if (terminalInput != null) terminalInput.text = string.Empty;
            if (terminalOutput != null)
            {
                terminalOutput.text = "等待输入授权指令。";
                terminalOutput.color = new Color(.54f, .84f, .79f);
            }
            StartCoroutine(FocusTerminalInputNextFrame());
        }

        private IEnumerator FocusTerminalInputNextFrame()
        {
            yield return null;
            if (terminalPanel == null || !terminalPanel.activeSelf || terminalInput == null) yield break;
            EventSystem.current?.SetSelectedGameObject(terminalInput.gameObject);
            terminalInput.Select();
            terminalInput.ActivateInputField();
        }

        private void CloseTerminal()
        {
            if (terminalInput != null) terminalInput.DeactivateInputField();
            if (terminalPanel != null) terminalPanel.SetActive(false);
        }

        private void SubmitTerminalCommand()
        {
            if (terminalCommandExecuting || terminalInput == null || terminalOutput == null) return;
            string command = (terminalInput.text ?? string.Empty).Trim().ToLowerInvariant();
            if (command.Length == 0)
            {
                terminalOutput.text = "请输入指令后再执行。";
                terminalOutput.color = new Color(1f, .70f, .34f);
                ReactivateTerminalInput();
                return;
            }
            switch (command)
            {
                case "exit/zjh":
                    terminalCommandExecuting = true;
                    terminalOutput.text = "指令已接受：正在安全退出游戏……";
                    terminalOutput.color = new Color(.54f, 1f, .80f);
                    StartCoroutine(QuitFromTerminalRoutine());
                    break;
                case "game-two/zjh":
                    terminalCommandExecuting = true;
                    // Only the resumable chapter state changes here. Statistics,
                    // achievements and settings remain intact inside this atomic save.
                    MeixiLocalProgress.SetGameplayChapter(2);
                    terminalOutput.text = "指令已接受：进度已切换至第二章，正在重新载入……";
                    terminalOutput.color = new Color(.54f, 1f, .80f);
                    StartCoroutine(LoadChapterTwoFromTerminalRoutine());
                    break;
                default:
                    terminalOutput.text = "未知指令。终端未修改任何游戏数据。";
                    terminalOutput.color = new Color(1f, .45f, .36f);
                    ReactivateTerminalInput();
                    break;
            }
        }

        private void ReactivateTerminalInput()
        {
            if (terminalInput == null) return;
            terminalInput.Select();
            terminalInput.ActivateInputField();
            terminalInput.MoveTextEnd(false);
        }

        private IEnumerator QuitFromTerminalRoutine()
        {
            yield return new WaitForSecondsRealtime(.20f);
            QuitGame();
        }

        private IEnumerator LoadChapterTwoFromTerminalRoutine()
        {
            yield return new WaitForSecondsRealtime(.28f);
            CloseTerminal();
            SetPaused(false);
            started = true;
            BeginScreenTransition("终 端 · 载 入 第 二 章", () =>
            {
                Time.timeScale = 1f;
                AudioListener.pause = false;
                UnityEngine.SceneManagement.SceneManager.LoadScene("MeixiTheatre");
            }, .42f, .08f, 0f);
        }

        private void AdjustMasterVolume(float delta)
        {
            MeixiLocalProgress.SetMasterVolume(MeixiLocalProgress.MasterVolume + delta);
            RefreshPauseSettingsUi();
        }

        private void RefreshPauseSettingsUi()
        {
            if (pausePracticeText != null)
                pausePracticeText.text = $"乡 村 数 字 文 化 实 践  ·  第 {MeixiLocalProgress.PracticeNumber:00} 次\n" +
                    $"累 计 倒 下  {MeixiLocalProgress.TotalDeaths:00}  次  ·  无伤重启不计入实践次数";
            if (settingsVolumeText != null)
                settingsVolumeText.text = $"主 音 量    {Mathf.RoundToInt(MeixiLocalProgress.MasterVolume * 100f)}%";
            int unlocked = MeixiLocalProgress.UnlockedAchievementCount;
            IReadOnlyList<MeixiAchievementDefinition> definitions = MeixiLocalProgress.AchievementDefinitions;
            if (settingsAchievementCountText != null)
                settingsAchievementCountText.text = $"成 就 档 案    {unlocked:00} / {definitions.Count:00}";
            if (settingsAchievementListText == null) return;
            var earned = new List<MeixiAchievementDefinition>();
            for (int i = 0; i < definitions.Count; i++)
            {
                if (MeixiLocalProgress.IsAchievementUnlocked(definitions[i].Id)) earned.Add(definitions[i]);
            }
            earned.Sort((left, right) =>
            {
                int rank = right.Rank.CompareTo(left.Rank);
                return rank != 0 ? rank : string.Compare(left.Title, right.Title, StringComparison.Ordinal);
            });
            var builder = new StringBuilder(320);
            for (int i = 0; i < earned.Count; i++)
            {
                MeixiAchievementDefinition definition = earned[i];
                if (i > 0) builder.Append('\n');
                builder.Append("<color=#");
                builder.Append(MeixiLocalProgress.RankHexColor(definition.Rank));
                builder.Append(">【");
                builder.Append(MeixiLocalProgress.RankLabel(definition.Rank));
                builder.Append("】</color>  ");
                builder.Append(definition.Title);
            }
            settingsAchievementListText.text = earned.Count > 0 ? builder.ToString() : "暂 无 已 获 得 成 就";
        }

        private void SetPaused(bool value)
        {
            if (value && !paused)
            {
                pauseStartedDsp = AudioSettings.dspTime;
            }
            else if (!value && paused)
            {
                ShiftCombatTimeline(AudioSettings.dspTime - pauseStartedDsp);
            }
            paused = value;
            if (!paused) ClosePauseSettings();
            Time.timeScale = paused ? 0f : 1f;
            AudioListener.pause = paused;
            if (pausePanel != null)
            {
                pausePanel.SetActive(paused);
                if (paused)
                {
                    pausePanel.transform.SetAsLastSibling();
                    RefreshPauseSettingsUi();
                }
            }

            bool presentationNeedsPointer = openingActive || dialogueOpen || complete || secondChapterLoading ||
                                            (titlePanel != null && titlePanel.activeSelf);
            bool showPointer = paused || presentationNeedsPointer;
            Cursor.lockState = showPointer ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = showPointer;
            if (paused && interactionCard != null) interactionCard.SetActive(false);
            player?.ResetInputTransientState();
            orbitCamera?.ClearInputAfterFocus();
            Debug.Log($"[MeixiPause] state={(paused ? "paused" : "resumed")} timeScale={Time.timeScale:F1} audioPaused={AudioListener.pause}");
        }

        private void ShiftCombatTimeline(double delta)
        {
            if (delta <= 0d || !chapterOneCombatActive) return;
            chapterOneBeatStart += delta;
            expectedCombatBeatDsp += delta;
            currentThreatResolveDsp += delta;
            expectedLeftHandBeatDsp += delta;
            leftHandHoldStartedDsp += delta;
            bufferedCombatKeyDsp += delta;
            lastCombatSequenceInputDsp += delta;
            combatPhaseRestUntilDsp += delta;
            playerDamageImmuneUntilDsp += delta;
            meleeCounterUntilDsp += delta;
            for (int i = 0; i < expectedCombatStepDsps.Length; i++) expectedCombatStepDsps[i] += delta;
            creekSpirit?.ShiftCombatTimeline(delta);
        }

        private void RestartFromPause()
        {
            SetPaused(false);
            musicSource?.Stop();
            MeixiLocalProgress.BeginNewJourney();
            UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
        }

        private void RestartAfterDefeat()
        {
            CombatDefeatOverlay3D.Clear();
            defeatOpen = false;
            Time.timeScale = 1f;
            AudioListener.pause = false;
            if (defeatPanel != null) defeatPanel.SetActive(false);
            musicSource?.Stop();
            MeixiLocalProgress.BeginNewJourney();
            UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
        }

        public void RestartAfterDefeatFromOverlay() => RestartAfterDefeat();

        public void QuitAfterDefeatFromOverlay()
        {
            CombatDefeatOverlay3D.Clear();
            QuitGame();
        }

        private void ShowDefeatPanel()
        {
            if (defeatOpen) return;
            if (!IsAutomatedPresentationRun()) MeixiLocalProgress.RecordDeath();
            if (sceneTransitionRoutine != null)
            {
                StopCoroutine(sceneTransitionRoutine);
                sceneTransitionRoutine = null;
            }
            sceneTransitionActive = false;
            if (transitionPanel != null) transitionPanel.SetActive(false);
            defeatOpen = true;
            chapterOneCombatActive = false;
            combatBeatPending = false;
            combatBeatSucceeded = false;
            TrackingAttackTelegraph3D.ClearAll();
            EnemyMusicAttackVfx3D.ClearAll();
            MeleeForbiddenRing3D.ClearAll(false);
            player?.ResetInputTransientState();
            orbitCamera?.EndCombatFocus();
            if (rhythmPanel != null) rhythmPanel.SetActive(false);
            if (defeatPanel != null)
            {
                if (defeatSummaryText != null)
                    defeatSummaryText.text = $"最佳连击  {chapterOneCombatBestCombo}    ·    " +
                                             $"成功回应  {chapterOneCombatSuccesses}    ·    失拍  {chapterOneCombatMisses}";
                defeatPanel.SetActive(true);
                defeatPanel.transform.SetAsLastSibling();
            }
            Time.timeScale = 0f;
            AudioListener.pause = true;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            Debug.Log("[MeixiDefeat] Echo depleted. Waiting for retry or quit selection.");
        }

        private void QuitGame()
        {
            Time.timeScale = 1f;
            AudioListener.pause = false;
            musicSource?.Stop();
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            Debug.Log("[MeixiPause] Quit requested from pause menu.");
            Application.Quit(0);
        }

        private void UpdateNavigation()
        {
            Vector3 playerPosition = PlayerPosition;
            int targetIndex = Mathf.Clamp(completedMemories, 0, nodePositions.Length - 1);
            Vector3 targetPosition = secondChapterRouteActive
                ? MeixiSecondChapterRoute.EntrancePosition
                : nodePositions[targetIndex];
            Vector3 delta = targetPosition - playerPosition;
            float distance = new Vector2(delta.x, delta.z).magnitude;
            if (navigationText != null)
            {
                navigationText.text = secondChapterRouteActive
                    ? $"第二章入口  {distance:0}m"
                    : completedMemories >= nodePositions.Length ? "魂音已完整" : $"{chapterNames[targetIndex].Replace(" ", "")}  {distance:0}m";
            }
            if (navigationArrow != null && delta.sqrMagnitude > 0.01f)
            {
                float heading = Mathf.Atan2(delta.x, delta.z) * Mathf.Rad2Deg;
                navigationArrow.localRotation = Quaternion.Euler(0f, 0f, -heading);
            }
            if (miniMapPlayer != null)
            {
                Vector2 point = MapPoint(playerPosition);
                miniMapPlayer.anchorMin = point;
                miniMapPlayer.anchorMax = point;
                miniMapPlayer.anchoredPosition = Vector2.zero;
            }
            for (int i = 0; i < miniMapMarkers.Count; i++)
            {
                Color markerColor = i < completedMemories
                    ? new Color(0.25f, 0.92f, 0.62f, 1f)
                    : i == targetIndex ? new Color(1f, 0.60f, 0.16f, 1f) : new Color(0.28f, 0.48f, 0.56f, 0.82f);
                miniMapMarkers[i].GetComponent<Image>().color = markerColor;
                miniMapMarkers[i].localScale = i == targetIndex ? Vector3.one * (1f + Mathf.Sin(Time.unscaledTime * 4f) * 0.18f) : Vector3.one;
            }
        }

        private void SetInteractionCard(SoulNode3D node)
        {
            if (interactionCard == null) return;
            if (node == null)
            {
                interactionCard.SetActive(false);
                return;
            }
            bool locked = node.Index > completedMemories || (node.Index == 3 && defeatedEnemies < 3);
            interactionCard.SetActive(true);
            interactionTitle.text = locked ? "魂 音 尚 未 连 接" : "聆 听 魂 音";
            interactionTitle.color = locked ? new Color(0.65f, 0.72f, 0.75f) : new Color(1f, 0.82f, 0.40f);
            interactionSubtitle.text = locked ? "先完成前方记忆或净化附近噪音" : chapterNames[node.Index] + "  ·  按 E 进入共鸣";
        }

        private void SetNpcInteractionCard(VillageNpc3D npc)
        {
            if (interactionCard == null || npc == null) return;
            interactionCard.SetActive(true);
            interactionTitle.text = "与 " + npc.DisplayName + " 交 谈";
            interactionTitle.color = new Color(1f, 0.82f, 0.40f);
            interactionSubtitle.text = npc.Role + "  ·  按 E 聆听他的故事";
        }

        private void UpdateTitleSaveMenuVisibility()
        {
            bool ready = titleScreenFx == null || titleScreenFx.IsReady;
            if (titleSaveMenuGroup != null)
            {
                titleSaveMenuGroup.alpha = Mathf.MoveTowards(titleSaveMenuGroup.alpha, ready ? 1f : 0f,
                    Time.unscaledDeltaTime * 4.5f);
                bool available = ready && (titleDeleteConfirmPanel == null || !titleDeleteConfirmPanel.activeSelf);
                titleSaveMenuGroup.interactable = available;
                titleSaveMenuGroup.blocksRaycasts = available;
            }
            if (titlePrimaryButton != null && ready && (titleDeleteConfirmPanel == null || !titleDeleteConfirmPanel.activeSelf))
                titlePrimaryButton.interactable = true;
        }

        private void RefreshTitleSaveUi()
        {
            if (titleContinueButton != null) titleContinueButton.interactable = true;
            if (titleNewJourneyButton != null) titleNewJourneyButton.interactable = true;
            if (titleDeleteSaveButton != null) titleDeleteSaveButton.interactable = true;
            if (titleSaveSummaryText != null)
            {
                titleSaveSummaryText.text = "当前进度  ·  " + MeixiLocalProgress.GetContinueSummary();
                titleSaveSummaryText.color = new Color(.58f, .94f, .90f);
            }
        }

        private void HandleTitlePrimaryAction()
        {
            ContinueSavedJourney();
        }

        private void StartNewJourneyFromTitle()
        {
            if (started || sceneTransitionActive || (titleScreenFx != null && !titleScreenFx.IsReady)) return;
            MeixiLocalProgress.ResetGameplayToChapterOne();
            MeixiLocalProgress.PrepareGameplayLaunch();
            StartAdventure();
        }

        private void ContinueSavedJourney()
        {
            if (started || sceneTransitionActive || (titleScreenFx != null && !titleScreenFx.IsReady)) return;
            MeixiLocalProgress.PrepareGameplayLaunch();
            MeixiLocalProgress.EnsureJourneyExists();
            if (MeixiLocalProgress.CurrentChapter <= 1)
            {
                StartAdventure();
                return;
            }
            started = true;
            BeginScreenTransition("读 取 本 地 回 响", () =>
            {
                Time.timeScale = 1f;
                AudioListener.pause = false;
                UnityEngine.SceneManagement.SceneManager.LoadScene("MeixiTheatre");
            }, .48f, .10f, 0f);
        }

        private void OpenNewJourneyConfirmation()
        {
            OpenTitleConfirmation(
                "开 启 新 旅 途",
                "游戏进程将回到第一章开端。\n已获得成就、累计死亡次数、实践记录与设置全部保留。",
                "确 认 新 旅 途",
                StartNewJourneyFromTitle,
                new Color(.48f, .14f, .055f, .99f));
        }

        private void OpenDeleteSaveConfirmation()
        {
            OpenTitleConfirmation(
                "删 除 本 地 存 档",
                "将清除章节进度、旅途统计、全部成就与设置。\n数据会完全初始化；此操作无法撤销。",
                "确 认 删 除",
                ConfirmDeleteLocalSave,
                new Color(.42f, .045f, .055f, .99f));
        }

        private void OpenTitleConfirmation(string title, string body, string acceptLabel,
            Action confirmedAction, Color acceptColor)
        {
            if (titleDeleteConfirmPanel == null || started || sceneTransitionActive) return;
            pendingTitleConfirmation = confirmedAction;
            if (titleConfirmationTitleText != null) titleConfirmationTitleText.text = title;
            if (titleConfirmationBodyText != null) titleConfirmationBodyText.text = body;
            if (titleConfirmationAcceptButton != null)
            {
                Text label = titleConfirmationAcceptButton.GetComponentInChildren<Text>();
                if (label != null) label.text = acceptLabel;
                Image image = titleConfirmationAcceptButton.targetGraphic as Image;
                if (image != null) image.color = acceptColor;
            }
            titleDeleteConfirmPanel.SetActive(true);
            titleDeleteConfirmPanel.transform.SetAsLastSibling();
            if (titlePrimaryButton != null) titlePrimaryButton.interactable = false;
            if (titleSaveMenuGroup != null)
            {
                titleSaveMenuGroup.interactable = false;
                titleSaveMenuGroup.blocksRaycasts = false;
            }
        }

        private void CloseTitleConfirmation()
        {
            if (titleDeleteConfirmPanel != null) titleDeleteConfirmPanel.SetActive(false);
            pendingTitleConfirmation = null;
            bool ready = titleScreenFx == null || titleScreenFx.IsReady;
            if (titlePrimaryButton != null) titlePrimaryButton.interactable = ready;
            RefreshTitleSaveUi();
        }

        private void ConfirmTitleAction()
        {
            Action action = pendingTitleConfirmation;
            CloseTitleConfirmation();
            action?.Invoke();
        }

        private void ConfirmDeleteLocalSave()
        {
            MeixiLocalProgress.DeleteProgressSave();
            RefreshTitleSaveUi();
            ShowToast("本 地 存 档 已 删 除", new Color(1f, .62f, .28f));
        }

        private void StartAdventure()
        {
            if (started || sceneTransitionActive || (titleScreenFx != null && !titleScreenFx.IsReady)) return;
            BeginScreenTransition("返 回 梅 溪", BeginAdventurePresentation, .46f, .08f, .66f);
        }

        private void BeginAdventurePresentation()
        {
            if (started) return;
            // Awake already binds an authored scene. Rebinding here reset both
            // animators on the first playable frame and made the opening hitch.
            if (useSceneAuthoredWorld && (player == null || playerTransform == null || orbitCamera == null))
                BindSceneAuthoredWorld();
            ResetGameplaySession();
            // Scene-authored character references can be restored during the session
            // reset. Re-apply the render-only character/environment upgrade here so
            // the playable frame cannot fall back to the lower-detail bodies.
            MeixiChapterOneEnvironment.Ensure();
            MeixiSecondChapterRoute.Ensure(this);
            started = true;
            Debug.Log("[Meixi] StartAdventure accepted; opening cinematic begins.");
            titlePanel.SetActive(false);
            hudPanel.SetActive(false);
            openingActive = true;
            openingPanel.SetActive(true);
            SetOpeningDiaryPage(0);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private void ResetGameplaySession()
        {
            gameplaySessionRevision++;
            CancelFinalStrikeForSessionReset();
            Time.timeScale = 1f;
            AudioListener.pause = false;
            paused = false;
            complete = false;
            CombatDefeatOverlay3D.Clear();
            gameplayGuideOpen = false;
            gameplayGuideCallback = null;
            explorationGuideShown = false;
            battleGuideShown = false;
            if (gameplayGuidePanel != null) gameplayGuidePanel.SetActive(false);
            phaseThreeGuideOpen = false;
            if (combatPhaseBannerGroup != null)
            {
                combatPhaseBannerGroup.alpha = 0f;
                combatPhaseBannerGroup.blocksRaycasts = false;
            }
            if (combatPhaseBanner != null) combatPhaseBanner.SetActive(false);
            dialogueOpen = false;
            rhythmActive = false;
            openingActive = false;
            finishingStrike = false;
            defeatOpen = false;
            chapterOneCombatActive = false;
            combatBeatPending = false;
            combatBeatSucceeded = false;
            threatDamageResolved = false;
            expectedCombatAction = CombatBeatAction.None;
            currentThreatResolveDsp = 0d;
            combatPhaseRestUntilDsp = 0d;
            expectedCombatStepCount = 0;
            expectedCombatStepIndex = 0;
            bufferedCombatKey = CombatInputKey.None;
            bufferedCombatKeyDsp = 0d;
            lastCombatSequenceInputDsp = 0d;
            currentThreatType = -1;
            activeThreatId = -1;
            dodgedThreatId = -1;
            counteredThreatId = -1;
            resolvedThreatId = -1;
            playerDamageImmuneUntilDsp = 0d;
            chapterOneCombatPhase = 1;
            chapterOneCombatCombo = 0;
            chapterOneCombatBestCombo = 0;
            chapterOneCombatSuccesses = 0;
            chapterOneCombatMisses = 0;
            meleeCounterUntilDsp = 0d;
            meleeCounterThreatId = -1;
            cinematicInputLockedUntil = 0f;
            chapterCardCallback = null;
            dialogueCallback = null;
            pendingNode = null;
            if (chapterOneSoulPickup != null) Destroy(chapterOneSoulPickup.gameObject);
            chapterOneSoulPickup = null;
            chapterOneSoulCollected = false;
            secondChapterLoading = false;
            secondChapterRouteActive = false;
            completedMemories = 0;
            defeatedEnemies = 0;
            health = 5;
            collectedNpcMemories.Clear();
            SyncChapterOneStoryNpcs();

            if (musicSource != null) musicSource.Stop();
            if (rhythmPanel != null) rhythmPanel.SetActive(false);
            if (dialoguePanel != null) dialoguePanel.SetActive(false);
            if (chapterPanel != null) chapterPanel.SetActive(false);
            if (endingPanel != null) endingPanel.SetActive(false);
            if (pausePanel != null) pausePanel.SetActive(false);
            if (defeatPanel != null) defeatPanel.SetActive(false);
            TrackingAttackTelegraph3D.ClearAll();
            MeleeForbiddenRing3D.ClearAll(false);
            EnemyMusicAttackVfx3D.ClearAll();

            if (creekSpirit != null) creekSpirit.gameObject.SetActive(false);
            if (playerTransform != null)
            {
                CharacterController activeController = playerTransform.GetComponent<CharacterController>();
                if (activeController != null) activeController.enabled = false;
                playerTransform.SetPositionAndRotation(respawnPoint, Quaternion.identity);
                if (activeController != null) activeController.enabled = true;
            }
            player?.CompleteFinalStrikeAnimation();
            player?.ResetForNewSession();
            orbitCamera?.ResetGameplayView();
            displayedHealth = health;
            UpdateHealth();
            if (soulCountText != null) soulCountText.text = "收集魂音：0 / 3";
            if (archiveProgressText != null) archiveProgressText.text = "村民线索  0 / 3";
            Debug.Log("[MeixiSession] Deterministic gameplay state initialized.");
        }

        private void CancelFinalStrikeForSessionReset()
        {
            if (finalStrikeRoutine != null)
            {
                StopCoroutine(finalStrikeRoutine);
                finalStrikeRoutine = null;
            }
            Time.timeScale = finalStrikeRestoreTimeScale > 0f ? finalStrikeRestoreTimeScale : 1f;
            if (musicSource != null) musicSource.pitch = finalStrikeRestorePitch > 0f ? finalStrikeRestorePitch : 1f;
            finishingStrike = false;
            player?.CompleteFinalStrikeAnimation();
        }

        private void OnDisable()
        {
            // Editor stop and scene reload can abort a realtime cinematic yield.
            // Never leave the next session globally slowed or its audio pitched down.
            MeixiLocalProgress.ApplyAudioSetting();
            if (finalStrikeRoutine == null) return;
            Time.timeScale = 1f;
            if (musicSource != null) musicSource.pitch = 1f;
            finalStrikeRoutine = null;
            finishingStrike = false;
        }

        private void FinishOpeningSequence(bool immediate = false)
        {
            if (!openingActive) return;
            if (immediate)
            {
                PresentFirstChapterCard();
                return;
            }
            BeginScreenTransition("第 一 章", PresentFirstChapterCard, .42f, .08f, .56f);
        }

        private void PresentFirstChapterCard()
        {
            openingActive = false;
            openingPanel.SetActive(false);
            hudPanel.SetActive(true);
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            orbitCamera?.BeginOpeningShot(2.7f);
            PlayChapterOneExplorationMusic();
            ShowChapterCard(-1, "第 一 章", "没 有 声 音 的 村 庄", "城市的喧闹在山路尽头突然消失", () =>
            {
                ShowExplorationGuide(() => ShowDialogue(new[]
                {
                    "林默｜这里应该就是闽清了。先向村民问问樟洋村怎么走。",
                    "系统｜三下乡实践任务已更新：调查村庄，收集“魂音”。",
                    "林默｜魂音……就是日记里提到的那个词？"
                }, () => SetObjective(0)));
            });
        }

        private void SetOpeningDiaryPage(int index)
        {
            openingDiaryPage = Mathf.Clamp(index, 0, diaryBodies.Length - 1);
            openingPageStartedAt = Time.unscaledTime;
            // The prologue is player-paced: a page remains on screen until the
            // player explicitly clicks or presses a presentation key.
            openingPageUntil = float.PositiveInfinity;
            openingUntil = openingPageUntil;
            if (openingPaperGroup != null) openingPaperGroup.alpha = 0f;
            if (openingDiaryDate != null) openingDiaryDate.text = diaryDates[openingDiaryPage];
            if (openingDiaryTitle != null) openingDiaryTitle.text = diaryTitles[openingDiaryPage];
            if (openingDiaryBody != null) openingDiaryBody.text = diaryBodies[openingDiaryPage];
            if (openingDiaryPageText != null)
                openingDiaryPageText.text = $"— {openingDiaryPage + 1:00} / {diaryBodies.Length:00} —";
        }

        private void UpdateOpeningDiary()
        {
            float elapsed = Time.unscaledTime - openingPageStartedAt;
            if (openingPaperGroup != null)
            {
                float fadeIn = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / .46f));
                openingPaperGroup.alpha = fadeIn;
            }
        }

        private void AdvanceOpeningDiary()
        {
            if (!openingActive) return;
            if (openingDiaryPage + 1 < diaryBodies.Length)
            {
                SetOpeningDiaryPage(openingDiaryPage + 1);
                return;
            }
            FinishOpeningSequence();
        }

        private void ShowChapterCard(int index, string number, string title, string subtitle, Action callback)
        {
            chapterPanel.SetActive(true);
            chapterCardNumber.text = number;
            chapterCardTitle.text = title;
            chapterCardSubtitle.text = subtitle;
            chapterCardUntil = Time.unscaledTime + 2.7f;
            chapterCardCallback = callback;
        }

        private static bool AnyPresentationAdvancePressed(Keyboard keyboard)
        {
            return keyboard.enterKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame ||
                   keyboard.eKey.wasPressedThisFrame || keyboard.wKey.wasPressedThisFrame ||
                   keyboard.aKey.wasPressedThisFrame || keyboard.sKey.wasPressedThisFrame ||
                   keyboard.dKey.wasPressedThisFrame || keyboard.upArrowKey.wasPressedThisFrame ||
                   keyboard.downArrowKey.wasPressedThisFrame || keyboard.leftArrowKey.wasPressedThisFrame ||
                   keyboard.rightArrowKey.wasPressedThisFrame;
        }

        private void FinishChapterCardImmediately()
        {
            chapterCardUntil = 0f;
            chapterCardTransitionPending = false;
            chapterPanel.SetActive(false);
            Action callback = chapterCardCallback;
            chapterCardCallback = null;
            callback?.Invoke();
        }

        private void FinishChapterCardWithTransition()
        {
            if (!chapterPanel.activeSelf || chapterCardTransitionPending || sceneTransitionActive) return;
            chapterCardTransitionPending = true;
            Action callback = chapterCardCallback;
            chapterCardCallback = null;
            BeginScreenTransition("声 纹 同 步", () =>
            {
                chapterPanel.SetActive(false);
                callback?.Invoke();
            }, .30f, .04f, .48f);
        }

        private void LoadSecondChapter()
        {
            if (secondChapterLoading || secondChapterRouteActive || sceneTransitionActive) return;
            BeginScreenTransition("余 音 之 路", UnlockSecondChapterRoute, .38f, .08f, .60f);
        }

        private void UnlockSecondChapterRoute()
        {
            if (secondChapterLoading || secondChapterRouteActive) return;
            MeixiSecondChapterRoute.Ensure(this);
            secondChapterRouteActive = true;
            Time.timeScale = 1f;
            paused = false;
            finishingStrike = false;
            cinematicInputLockedUntil = 0f;
            endingPanel.SetActive(false);
            hudPanel.SetActive(true);
            if (loadingPanel != null) loadingPanel.SetActive(false);
            chapterHudText.text = "第一章 · 余音之路";
            objectiveText.text = "沿村口青石路一直向北，前往亮着灯的古戏台入口";
            player?.ResetForNewSession();
            orbitCamera?.ResetGameplayView();
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            ShowToast("新目标：沿青石路前往古戏台", new Color(.42f, .92f, 1f));
            Debug.Log("[Meixi] Second chapter route unlocked; player control restored.");
        }

        public void TryEnterSecondChapterFromRoad()
        {
            if (secondChapterLoading) return;
            if (!secondChapterRouteActive)
            {
                ShowToast("先找回溪流魂音，再前往古戏台", new Color(1f, .72f, .30f));
                return;
            }
            StartSecondChapterSceneLoad();
        }

        private void StartSecondChapterSceneLoad()
        {
            if (secondChapterLoading || sceneTransitionActive) return;
            secondChapterLoading = true;
            BeginScreenTransition("古 戏 台 · 读 取 中", PresentSecondChapterLoading, .52f, .12f, .56f,
                () => StartCoroutine(LoadSecondChapterRoutine()));
        }

        private void PresentSecondChapterLoading()
        {
            secondChapterRouteActive = false;
            Time.timeScale = 1f;
            paused = false;
            finishingStrike = false;
            cinematicInputLockedUntil = 0f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            hudPanel.SetActive(false);
            endingPanel.SetActive(false);
            if (loadingPanel != null)
            {
                loadingPanel.SetActive(true);
                loadingPanel.transform.SetAsLastSibling();
            }
        }

        private IEnumerator LoadSecondChapterRoutine()
        {
            const string sceneName = "MeixiTheatre";
            yield return new WaitForSecondsRealtime(1.15f);
            if (!Application.CanStreamedLevelBeLoaded(sceneName))
            {
                secondChapterLoading = false;
                Debug.LogError("[Meixi] Second chapter scene is missing from Build Settings: " + sceneName);
                ShowToast("第二章场景未加入构建，请重新执行 Windows 构建", new Color(1f, .32f, .18f));
                yield break;
            }
            Debug.Log("[Meixi] Loading second chapter: " + sceneName);
            AsyncOperation operation = UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(
                sceneName, UnityEngine.SceneManagement.LoadSceneMode.Single);
            if (operation == null)
            {
                secondChapterLoading = false;
                Debug.LogError("[Meixi] Unity failed to create the second chapter load operation.");
                yield break;
            }
            operation.allowSceneActivation = true;
            while (!operation.isDone) yield return null;
        }

        private void SetObjective(int chapter)
        {
            if (chapter >= chapterNames.Length) return;
            chapterHudText.text = $"第一章 · {chapterNames[0].Replace(" ", "")}";
            if (secondChapterRouteActive)
            {
                storyProgressText.text = "主线 04 / 04  ·  余音之路";
                objectiveText.text = "沿北侧青石路穿过村口，前往古戏台";
            }
            else if (chapterOneCombatActive)
            {
                storyProgressText.text = "主线 03 / 04  ·  溪灵对拍";
                objectiveText.text = "看清红色追踪区，按节拍闪避并击破三段声纹";
            }
            else if (completedMemories > 0)
            {
                storyProgressText.text = "主线 04 / 04  ·  溪声归位";
                objectiveText.text = "返回村口，沿新出现的道路前往下一段记忆";
            }
            else if (collectedNpcMemories.Count == 0)
            {
                storyProgressText.text = "主线 01 / 04  ·  询问道路";
                objectiveText.text = "在村口找到老村长，询问樟洋村怎么走";
            }
            else if (collectedNpcMemories.Count < 3)
            {
                storyProgressText.text = "主线 02 / 04  ·  村中异样";
                objectiveText.text = $"继续询问小孩和石婶，收集村民线索（{collectedNpcMemories.Count} / 3）";
            }
            else
            {
                storyProgressText.text = "主线 03 / 04  ·  收集魂音";
                objectiveText.text = "沿青石路前往溪边，按 E 调查发光的魂音";
            }
        }

        private string ToChineseNumber(int value) => value switch { 1 => "一", 2 => "二", 3 => "三", 4 => "四", _ => "五" };

        private void UpdateInteraction(Keyboard keyboard)
        {
            SoulNode3D nearest = null;
            float best = 4.25f;
            foreach (var node in nodes)
            {
                if (node == null || node.Completed) continue;
                float distance = HorizontalDistance(playerTransform.position, node.transform.position);
                if (distance < best)
                {
                    best = distance;
                    nearest = node;
                }
            }
            VillageNpc3D nearestNpc = null;
            float npcDistance = 4.0f;
            foreach (VillageNpc3D npc in villageNpcs)
            {
                if (npc == null) continue;
                float distance = HorizontalDistance(playerTransform.position, npc.transform.position);
                if (distance < npcDistance)
                {
                    npcDistance = distance;
                    nearestNpc = npc;
                }
            }

            // The story-critical soul node wins a close distance tie so a nearby
            // villager cannot consume the same E press and block chapter one.
            if (nearest != null && (nearestNpc == null || best <= npcDistance + 0.65f))
            {
                SetInteractionCard(nearest);
                if (keyboard.eKey.wasPressedThisFrame)
                {
                    Debug.Log($"[MeixiInteraction] Soul node accepted horizontalDistance={best:F2}");
                    nearest.Interact();
                }
            }
            else if (nearestNpc != null)
            {
                SetNpcInteractionCard(nearestNpc);
                if (keyboard.eKey.wasPressedThisFrame)
                {
                    Debug.Log($"[MeixiInteraction] NPC accepted name={nearestNpc.DisplayName} horizontalDistance={npcDistance:F2}");
                    nearestNpc.Interact();
                }
            }
            else
            {
                SetInteractionCard(null);
            }
        }

        private static float HorizontalDistance(Vector3 a, Vector3 b)
        {
            float x = a.x - b.x;
            float z = a.z - b.z;
            return Mathf.Sqrt(x * x + z * z);
        }

        public void BeginNpcDialogue(VillageNpc3D npc, string[] lines)
        {
            if (npc == null || lines == null || lines.Length == 0) return;
            if (!IsAutomatedPresentationRun()) MeixiLocalProgress.RecordNpcDialogue(npc.DisplayName);
            npc.FacePlayerImmediately();
            ShowDialogue(lines, () =>
            {
                bool firstRecord = collectedNpcMemories.Add(npc.DisplayName);
                if (firstRecord)
                {
                    if (archiveProgressText != null) archiveProgressText.text = $"村民线索  {collectedNpcMemories.Count} / 3";
                    ShowToast("◆ 村民线索已记录 · " + npc.DisplayName + " ◆", new Color(1f, .76f, .36f));
                    SetObjective(0);
                }
                else
                {
                    ShowToast("重新聆听 · " + npc.DisplayName, new Color(.48f, .86f, .92f));
                }
            });
        }

        public void BeginMemory(int index, SoulNode3D node)
        {
            if (collectedNpcMemories.Count < 3)
            {
                ShowToast($"先完成村中调查，寻找魂音线索（{collectedNpcMemories.Count} / 3）", new Color(1f, .72f, .30f));
                SetObjective(0);
                return;
            }
            if (index != 0 || completedMemories > 0 || chapterOneCombatActive)
            {
                ShowToast("溪流魂音已经连接", new Color(0.42f, 0.90f, 1f));
                return;
            }
            pendingNode = node != null ? node : nodes.Find(candidate => candidate != null && candidate.Index == index);
            Debug.Log($"[MeixiInteraction] Memory {index} begins node={(pendingNode != null ? pendingNode.name : "fallback")}");
            ShowChapterCard(index, "第 一 章", chapterNames[0], chapterSubtitles[0], () =>
                ShowDialogue(dialogues[0], StartChapterOneCombat));
        }

        private void ShowDialogue(string[] lines, Action callback)
        {
            activeDialogue = lines;
            dialogueLine = 0;
            dialogueCallback = callback;
            dialogueOpen = true;
            dialoguePanel.SetActive(true);
            SetInteractionCard(null);
            SetDialogueLine(lines[0]);
        }

        private void SetDialogueLine(string line)
        {
            int split = line.IndexOf('｜');
            string speaker = split > 0 ? line[..split] : "梅溪回响";
            dialogueSpeaker.text = speaker;
            fullDialogueLine = split > 0 ? line[(split + 1)..] : line;
            if (dialogueCounter != null) dialogueCounter.text = $"{dialogueLine + 1:00} / {Mathf.Max(1, activeDialogue?.Length ?? 1):00}";
            UpdateDialogueSpeakerStyle(speaker);
            // UpdateTypewriter runs independently of keyboard availability, so story
            // text can reveal cinematically without recreating the old blank-dialogue
            // startup bug in controller-only and capture environments.
            typedCharacters = 0f;
            dialogueText.text = string.Empty;
        }

        private void UpdateDialogueSpeakerStyle(string speaker)
        {
            string role = "梅溪魂音档案";
            Color accent = new Color(0.48f, 0.86f, 0.92f);
            bool villageNpc = false;
            if (speaker.Contains("林默"))
            {
                role = "城市调研员 · 电子信息专业";
                accent = new Color(0.38f, 0.82f, 1f);
            }
            else if (speaker.Contains("老村长"))
            {
                role = "村口老人 · 记忆模糊";
                accent = new Color(1f, 0.72f, 0.32f);
                villageNpc = true;
            }
            else if (speaker.Contains("小孩"))
            {
                role = "溪边玩耍的孩子";
                accent = new Color(0.52f, 0.94f, 0.62f);
                villageNpc = true;
            }
            else if (speaker.Contains("石婶"))
            {
                role = "沉默的村民";
                accent = new Color(0.86f, 0.68f, 1f);
                villageNpc = true;
            }
            else if (speaker.Contains("系统"))
            {
                role = "AI 声纹采集系统";
                accent = new Color(0.30f, 0.94f, 1f);
            }
            if (dialogueRole != null) dialogueRole.text = role;
            if (dialoguePortraitImage != null)
            {
                Sprite portraitSprite = speaker.Contains("林默") ? playerDialoguePortraitSprite :
                    speaker.Contains("小孩") ? childDialoguePortraitSprite :
                    speaker.Contains("石婶") ? auntDialoguePortraitSprite :
                    villageNpc ? npcDialoguePortraitSprite : beatSigilSprite;
                dialoguePortraitImage.sprite = portraitSprite;
                dialoguePortraitImage.color = portraitSprite == beatSigilSprite ? accent : Color.white;
                dialoguePortraitImage.gameObject.SetActive(portraitSprite != null);
            }
            if (dialoguePortraitPlate != null) dialoguePortraitPlate.color = Color.Lerp(new Color(0.015f, 0.06f, 0.08f, 0.98f), accent, 0.20f);
            dialogueSpeaker.color = accent;
        }

        private void UpdateTypewriter()
        {
            if (dialogueText.text.Length >= fullDialogueLine.Length) return;
            typedCharacters += Time.unscaledDeltaTime * 32f;
            int count = Mathf.Clamp(Mathf.FloorToInt(typedCharacters), 0, fullDialogueLine.Length);
            dialogueText.text = fullDialogueLine[..count];
        }

        private void AdvanceDialogue()
        {
            if (dialogueText.text.Length < fullDialogueLine.Length)
            {
                dialogueText.text = fullDialogueLine;
                typedCharacters = fullDialogueLine.Length;
                return;
            }
            dialogueLine++;
            if (dialogueLine < activeDialogue.Length)
            {
                SetDialogueLine(activeDialogue[dialogueLine]);
                return;
            }
            dialogueOpen = false;
            dialoguePanel.SetActive(false);
            Action callback = dialogueCallback;
            dialogueCallback = null;
            callback?.Invoke();
        }

        private void StartChapterOneCombat()
        {
            if (chapterOneCombatActive || complete || sceneTransitionActive) return;
            BeginScreenTransition("溪 流 声 纹 · 战 斗 同 步", ActivateChapterOneCombat, .34f, .08f, .54f);
        }

        private void ActivateChapterOneCombat()
        {
            if (chapterOneCombatActive || complete) return;
            ShowBattleGuide(BeginChapterOneCombatCore);
        }

        private void BeginChapterOneCombatCore()
        {
            if (chapterOneCombatActive || complete) return;
            Debug.Log("[MeixiCombat] Chapter one start requested.");
            if (creekSpirit == null)
            {
                ReacquireCreekSpirit();
            }
            if (creekSpirit == null)
            {
                Vector3 fallbackPosition = playerTransform != null
                    ? playerTransform.position + playerTransform.forward * 4.8f
                    : new Vector3(-12.4f, 0.05f, -5.6f);
                fallbackPosition.y = 0.05f;
                CreateCreekSpirit(fallbackPosition);
                if (creekSpirit == null)
                {
                    Debug.LogError("Chapter one combat could not create or bind the creek spirit.");
                    ShowToast("战斗角色加载失败，请重新进入场景", new Color(1f, 0.28f, 0.20f));
                    return;
                }
            }

            chapterOneCombatActive = true;
            rhythmActive = false;
            finishingStrike = false;
            combatBeatPending = false;
            combatBeatSucceeded = false;
            threatDamageResolved = false;
            currentThreatType = -1;
            activeThreatId = 0;
            dodgedThreatId = -1;
            counteredThreatId = -1;
            resolvedThreatId = -1;
            playerDamageImmuneUntilDsp = 0d;
            expectedCombatAction = CombatBeatAction.None;
            currentThreatResolveDsp = 0d;
            combatPhaseRestUntilDsp = 0d;
            expectedCombatStepCount = 0;
            expectedCombatStepIndex = 0;
            bufferedCombatKey = CombatInputKey.None;
            bufferedCombatKeyDsp = 0d;
            lastCombatSequenceInputDsp = 0d;
            ResetDualHandCombatCue();
            chapterOneCombatCombo = 0;
            chapterOneCombatBestCombo = 0;
            chapterOneCombatSuccesses = 0;
            chapterOneCombatMisses = 0;
            chapterOneCombatPhase = 1;
            meleeCounterUntilDsp = 0d;
            meleeCounterThreatId = -1;
            if (pendingNode != null) pendingNode.gameObject.SetActive(false);
            if (chapterPanel != null) chapterPanel.SetActive(false);
            if (dialoguePanel != null) dialoguePanel.SetActive(false);
            SetInteractionCard(null);
            SetObjective(0);
            rhythmPanel.SetActive(true);
            rhythmTitle.text = "BOSS  ·  失 调 溪 灵";
            if (combatPhaseLabel != null) combatPhaseLabel.text = "第一阶段 · 听潮";
            SetCombatBarVisualAmount(combatEnemyHealthFill, 1f);
            SetCombatBarVisualAmount(combatEnemyHealthChipFill, 1f);
            rhythmJudge.text = "红色追踪区收束前按 SPACE 瞬身离开";
            rhythmScore.text = "J 近战突进（需进入范围） · K 远程音符波 · SPACE 瞬身闪避";
            if (combatActionKey != null) combatActionKey.text = "·";
            if (combatActionName != null) combatActionName.text = "聆 听 敌 人";
            chapterOneBeatStart = AudioSettings.dspTime + 0.12d;
            chapterOneBeatPulse = 0f;
            chapterOneCombatBpm = chapterOneBattleTimeline != null ? chapterOneBattleTimeline.Bpm : bpms[0];

            musicSource.Stop();
            musicSource.clip = chapterOneMusic ?? rhythmSong;
            musicSource.pitch = 1f;
            musicSource.time = 0f;
            musicSource.volume = 0.30f;
            musicSource.PlayScheduled(chapterOneBeatStart);
            creekSpirit.BeginBattle(chapterOneBeatStart, chapterOneCombatBpm);
            orbitCamera?.BeginCombatFocus(creekSpirit.transform);
            Debug.Log($"[MeixiCombat] Chapter one active enemy={creekSpirit.name} player={playerTransform.position} enemyPosition={creekSpirit.transform.position}");
            ShowToast("第一段 · 听潮：红区锁定时瞬身，随后自由反击", new Color(1f, 0.72f, 0.28f));
        }

        private void UpdateChapterOneCombatHud()
        {
            if (rhythmFlash == null || creekSpirit == null) return;
            UpdateDualHandCombatHud();
            float musicPulse = chapterOneBattleTimeline != null
                ? chapterOneBattleTimeline.GetPulse(AudioSettings.dspTime, chapterOneBeatStart, 0.20f)
                : 0f;
            double now = AudioSettings.dspTime;
            if (meleeCounterThreatId >= 0 && now > meleeCounterUntilDsp)
            {
                meleeCounterThreatId = -1;
                meleeCounterUntilDsp = 0d;
                if (!combatBeatPending)
                {
                    if (combatActionKey != null) combatActionKey.text = "·";
                    if (combatActionName != null)
                    {
                        combatActionName.text = "聆 听 下 一 拍";
                        combatActionName.color = new Color(0.52f, 0.82f, 0.92f);
                    }
                }
            }
            double displayedBeat = expectedCombatStepCount > 0 && expectedCombatStepIndex < expectedCombatStepCount
                ? expectedCombatStepDsps[expectedCombatStepIndex]
                : expectedCombatBeatDsp;
            float responsePulse = combatBeatPending
                ? Mathf.Clamp01(1f - Mathf.Abs((float)(displayedBeat - now)) / 0.72f)
                : 0f;
            chapterOneBeatPulse = Mathf.Max(musicPulse * 0.34f, responsePulse);
            float scale = 0.88f + chapterOneBeatPulse * 0.22f;
            rhythmFlash.rectTransform.localScale = Vector3.one * scale;
            Color color = rhythmFlash.color;
            color.a = combatBeatPending ? 0.48f + chapterOneBeatPulse * 0.48f : 0.18f + musicPulse * 0.18f;
            Color actionColor = CombatActionColor(expectedCombatAction);
            color = Color.Lerp(new Color(0.18f, 0.72f, 0.90f, color.a), new Color(actionColor.r, actionColor.g, actionColor.b, color.a), chapterOneBeatPulse);
            rhythmFlash.color = color;
            rhythmTitle.text = "失 调 溪 灵";
            if (combatPhaseLabel != null)
                combatPhaseLabel.text = chapterOneCombatPhase switch
                {
                    1 => "第一阶段 · 听潮",
                    2 => "第二阶段 · 逆流",
                    _ => "第三阶段 · 回声暴雨"
                };
            if (combatEnemyHealthFill != null)
            {
                // Keep one continuous boss-health timeline across all three phases.
                // The previous phase-local value jumped back to 100% on every phase
                // transition, making successful rhythm responses appear ineffective.
                float phaseHealth = Mathf.Clamp01(creekSpirit.Health /
                    (float)Mathf.Max(1, creekSpirit.HealthMaximum));
                combatEnemyHealthFill.fillAmount = Mathf.MoveTowards(combatEnemyHealthFill.fillAmount,
                    phaseHealth, Time.unscaledDeltaTime * 2.5f);
                SetCombatBarVisualAmount(combatEnemyHealthFill, combatEnemyHealthFill.fillAmount);
                if (combatEnemyHealthChipFill != null)
                {
                    if (combatEnemyHealthChipFill.fillAmount < combatEnemyHealthFill.fillAmount)
                        combatEnemyHealthChipFill.fillAmount = combatEnemyHealthFill.fillAmount;
                    else
                        combatEnemyHealthChipFill.fillAmount = Mathf.MoveTowards(combatEnemyHealthChipFill.fillAmount,
                            phaseHealth, Time.unscaledDeltaTime * .34f);
                    SetCombatBarVisualAmount(combatEnemyHealthChipFill, combatEnemyHealthChipFill.fillAmount);
                }
                combatEnemyHealthFill.color = chapterOneCombatPhase switch
                {
                    1 => new Color(0.12f, 0.88f, 1f, 0.98f),
                    2 => new Color(1f, 0.62f, 0.14f, 0.98f),
                    _ => new Color(1f, 0.18f, 0.30f, 0.98f)
                };
            }
            for (int i = 0; i < combatPhaseSegments.Length; i++)
            {
                if (combatPhaseSegments[i] == null) continue;
                combatPhaseSegments[i].color = i + 1 < chapterOneCombatPhase
                    ? new Color(0.36f, 0.96f, 0.72f, 0.92f)
                    : i + 1 == chapterOneCombatPhase
                        ? chapterOneCombatPhase == 1
                            ? new Color(0.16f, 0.92f, 1f, 1f)
                            : chapterOneCombatPhase == 2
                                ? new Color(1f, 0.62f, 0.14f, 1f)
                                : new Color(1f, 0.16f, 0.26f, 1f)
                        : new Color(0.18f, 0.28f, 0.32f, 0.82f);
            }
            rhythmScore.text = chapterOneCombatPhase == 1
                ? "短按踩拍  ·  J 近战  ·  K 远程  ·  SPACE 闪避冲刺"
                : chapterOneCombatPhase == 2
                    ? "短按 / 同时按 / 顺序连击  ·  跟随中央主旋律"
                    : "左右声部同时推进  ·  小图标短按  ·  大图标与黄色光圈需要持续按住";
            if (CombatPhaseResting)
            {
                if (combatActionKey != null) combatActionKey.text = "—";
                if (combatActionName != null)
                {
                    combatActionName.text = "节 拍 休 止 · 等 待 新 指 令";
                    combatActionName.color = new Color(.62f, .90f, .94f);
                }
                rhythmJudge.text = "阶段切换期间无伤 · 新按键出现后再回应";
                rhythmJudge.color = new Color(.68f, .90f, .94f);
            }
        }

        private void UpdateDualHandCombatHud()
        {
            bool dual = chapterOneCombatPhase >= 3;
            if (lastDualHandHudPhase != chapterOneCombatPhase)
            {
                lastDualHandHudPhase = chapterOneCombatPhase;
                // The old upper rectangular plates competed with the actual beat cues.
                // Keep the labels floating and let the sigils carry the visual hierarchy.
                if (leftHandCuePlate != null) leftHandCuePlate.SetActive(false);
                if (rightHandCuePlate != null) rightHandCuePlate.SetActive(false);
                if (leftHandRhythmFlash != null) leftHandRhythmFlash.gameObject.SetActive(dual);
                if (leftHandActionKey != null) leftHandActionKey.gameObject.SetActive(dual);
                if (leftHandActionName != null) leftHandActionName.gameObject.SetActive(dual);
                if (rhythmFlash != null)
                    SetRect(rhythmFlash.rectTransform, dual ? new Vector2(.595f, .39f) : new Vector2(.415f, .34f),
                        dual ? new Vector2(.755f, .65f) : new Vector2(.585f, .64f), Vector2.zero, Vector2.zero);
                if (combatActionKey != null)
                    SetRect(combatActionKey.rectTransform, dual ? new Vector2(.625f, .435f) : new Vector2(.44f, .43f),
                        dual ? new Vector2(.725f, .555f) : new Vector2(.56f, .56f), Vector2.zero, Vector2.zero);
                if (combatActionName != null)
                    SetRect(combatActionName.rectTransform, dual ? new Vector2(.585f, .65f) : new Vector2(.38f, .64f),
                        dual ? new Vector2(.765f, .70f) : new Vector2(.62f, .70f), Vector2.zero, Vector2.zero);
            }
            if (!dual) return;

            ApplyLeftHandCueLayout(leftHandHoldRequired);

            double now = AudioSettings.dspTime;
            float cuePulse = leftHandCueActive
                ? Mathf.Clamp01(1f - Mathf.Abs((float)(now - expectedLeftHandBeatDsp)) / .70f)
                : 0f;
            bool holdVisible = leftHandCueActive && leftHandHoldRequired && !leftHandSucceeded;
            float holdPulse = .5f + .5f * Mathf.Sin(Time.unscaledTime * 8.5f);
            if (leftHandHoldHalo != null)
            {
                leftHandHoldHalo.gameObject.SetActive(holdVisible);
                leftHandHoldHalo.rectTransform.localScale = Vector3.one * (1.02f + holdPulse * .10f + cuePulse * .08f);
                leftHandHoldHalo.color = new Color(1f, .67f, .06f, .48f + holdPulse * .38f);
            }
            if (leftHandHoldProgressFill != null)
            {
                leftHandHoldProgressFill.gameObject.SetActive(holdVisible);
                leftHandHoldProgressFill.fillAmount = Mathf.Max(.035f, leftHandHoldProgress);
                leftHandHoldProgressFill.color = Color.Lerp(new Color(1f, .48f, .04f, .58f),
                    new Color(1f, .96f, .42f, .98f), leftHandHoldProgress);
                leftHandHoldProgressFill.rectTransform.localScale = Vector3.one * (1.03f + holdPulse * .055f);
            }
            if (leftHandRhythmFlash != null)
            {
                leftHandRhythmFlash.rectTransform.localScale = Vector3.one *
                    (leftHandHoldRequired ? .98f + cuePulse * .22f : .92f + cuePulse * .20f);
                Color cueColor = leftHandHoldRequired ? new Color(1f, .68f, .08f) : Color.white;
                cueColor.a = leftHandSucceeded ? .34f : .52f + cuePulse * .46f;
                leftHandRhythmFlash.color = cueColor;
            }
            if (leftHandActionKey != null)
            {
                leftHandActionKey.fontSize = leftHandHoldRequired ? 82 : 58;
                leftHandActionKey.color = leftHandHoldRequired
                    ? Color.Lerp(new Color(1f, .72f, .16f), Color.white, holdPulse)
                    : Color.white;
            }
            if (leftHandActionName != null)
            {
                leftHandActionName.fontSize = leftHandHoldRequired ? 25 : 23;
                if (leftHandCueActive && !leftHandSucceeded && !leftHandHolding)
                {
                    leftHandActionName.text = leftHandHoldRequired
                        ? $"长 按 {CombatInputKeyText(expectedLeftHandKey)}  ·  保 持 {expectedLeftHandHoldDuration:0.00}s"
                        : $"短 按 {CombatInputKeyText(expectedLeftHandKey)}  ·  立 即 松 开";
                }
            }
            int illuminated = leftHandHoldRequired
                ? Mathf.RoundToInt(leftHandHoldProgress * leftHandHoldRingSegments.Length)
                : 0;
            for (int i = 0; i < leftHandHoldRingSegments.Length; i++)
            {
                Image segment = leftHandHoldRingSegments[i];
                if (segment == null) continue;
                segment.gameObject.SetActive(leftHandCueActive && leftHandHoldRequired && !leftHandSucceeded);
                segment.color = i < illuminated
                    ? Color.Lerp(new Color(1f, .52f, .04f, .98f), new Color(1f, .98f, .50f, 1f), i / (float)leftHandHoldRingSegments.Length)
                    : new Color(.40f, .27f, .06f, .78f);
                segment.rectTransform.localScale = Vector3.one * (i == illuminated ? 1.28f : 1f);
            }
        }

        private void ApplyLeftHandCueLayout(bool hold)
        {
            // Short presses mirror the right-hand cue exactly. A sustained note
            // expands only the left icon, leaving the yellow timing ring enough
            // room to read clearly at a glance.
            if (leftHandRhythmFlash != null)
                SetRect(leftHandRhythmFlash.rectTransform,
                    hold ? new Vector2(.205f, .295f) : new Vector2(.245f, .39f),
                    hold ? new Vector2(.445f, .705f) : new Vector2(.405f, .65f), Vector2.zero, Vector2.zero);
            if (leftHandActionKey != null)
                SetRect(leftHandActionKey.rectTransform,
                    hold ? new Vector2(.255f, .415f) : new Vector2(.275f, .435f),
                    hold ? new Vector2(.395f, .585f) : new Vector2(.375f, .555f), Vector2.zero, Vector2.zero);
            if (leftHandActionName != null)
                SetRect(leftHandActionName.rectTransform,
                    hold ? new Vector2(.185f, .705f) : new Vector2(.235f, .65f),
                    hold ? new Vector2(.465f, .775f) : new Vector2(.415f, .70f), Vector2.zero, Vector2.zero);
        }

        public bool IsCombatOnBeat(float window = 0.14f)
        {
            return chapterOneCombatActive && !CombatPhaseResting && combatBeatPending &&
                   Math.Abs(AudioSettings.dspTime -
                       (expectedCombatStepCount > 0 && expectedCombatStepIndex < expectedCombatStepCount
                           ? expectedCombatStepDsps[expectedCombatStepIndex] : expectedCombatBeatDsp)) <= window;
        }

        public void PrepareCombatBeat(int attackType, double beatTime)
        {
            PrepareCombatBeat(attackType, beatTime, threatSequence);
        }

        public void PrepareCombatBeat(int attackType, double beatTime, int patternIndex)
        {
            if (!chapterOneCombatActive || finishingStrike || CombatPhaseResting) return;
            meleeCounterThreatId = -1;
            meleeCounterUntilDsp = 0d;
            ResetDualHandCombatCue();
            expectedCombatAction = attackType == 3
                ? CombatBeatAction.Dodge
                : SelectExpectedCombatAction(chapterOneCombatPhase, patternIndex);
            expectedCombatStepCount = BuildCombatPattern(expectedCombatAction, beatTime);
            expectedCombatStepIndex = 0;
            expectedCombatBeatDsp = beatTime;
            currentThreatResolveDsp = expectedCombatStepCount > 1 && !IsChordAction(expectedCombatAction)
                ? beatTime + CurrentCombatSequenceMaxGap * (expectedCombatStepCount - 1) + 0.05d
                : beatTime;
            if (chapterOneCombatPhase >= 3)
            {
                ConfigureLeftHandCombatCue(patternIndex, beatTime);
                double leftResolve = expectedLeftHandBeatDsp + (leftHandHoldRequired ? expectedLeftHandHoldDuration + .08d : .04d);
                currentThreatResolveDsp = Math.Max(currentThreatResolveDsp, leftResolve);
            }
            combatBeatPending = true;
            combatBeatSucceeded = false;
            threatDamageResolved = false;
            currentThreatType = attackType;
            activeThreatId = ++threatSequence;
            autoActionIssuedForBeat = false;
            bufferedCombatKey = CombatInputKey.None;
            bufferedCombatKeyDsp = 0d;
            lastCombatSequenceInputDsp = 0d;
            if (combatActionKey != null) combatActionKey.text = CombatActionKey(expectedCombatAction);
            if (combatActionName != null)
            {
                combatActionName.text = CombatActionName(expectedCombatAction);
                combatActionName.color = CombatActionColor(expectedCombatAction);
            }
            float warningLifetime = Mathf.Max(0.50f, (float)(currentThreatResolveDsp - AudioSettings.dspTime) + 0.30f);
            MeleeForbiddenRing3D.Show(creekSpirit != null ? creekSpirit.transform : null, expectedCombatAction, warningLifetime);
            ReportEnemyTelegraph(attackType);
            Debug.Log($"[MeixiBeat] threat={activeThreatId} phase={chapterOneCombatPhase} action={expectedCombatAction} " +
                      $"first={expectedCombatBeatDsp:F3} resolve={currentThreatResolveDsp:F3} left={expectedLeftHandKey} " +
                      $"hold={leftHandHoldRequired} now={AudioSettings.dspTime:F3}");
        }

        private void ResetDualHandCombatCue()
        {
            leftHandCueActive = false;
            leftHandHoldRequired = false;
            leftHandHolding = false;
            leftHandSucceeded = false;
            rightHandSucceeded = false;
            rightHandDodgeResponse = false;
            expectedLeftHandKey = CombatInputKey.None;
            expectedLeftHandBeatDsp = 0d;
            leftHandHoldStartedDsp = 0d;
            expectedLeftHandHoldDuration = 0f;
            leftHandHoldProgress = 0f;
            dualHandHoldPreviewActive = false;
            if (leftHandActionKey != null) leftHandActionKey.text = "—";
            if (leftHandActionName != null) leftHandActionName.text = "等 待 左 手 声 部";
        }

        private void ConfigureLeftHandCombatCue(int patternIndex, double rightBeatDsp)
        {
            int variation = Mathf.Abs(patternIndex) % 4;
            leftHandCueActive = true;
            expectedLeftHandKey = variation is 0 or 2 ? CombatInputKey.Q : CombatInputKey.E;
            leftHandHoldRequired = variation is 1 or 2;
            expectedLeftHandBeatDsp = rightBeatDsp + (variation switch
            {
                0 => -.14d,
                1 => -.08d,
                2 => .10d,
                _ => .15d
            });
            expectedLeftHandHoldDuration = variation == 1 ? .62f : variation == 2 ? .78f : 0f;
            leftHandSucceeded = false;
            leftHandHolding = false;
            leftHandHoldProgress = 0f;
            if (leftHandActionKey != null) leftHandActionKey.text = CombatInputKeyText(expectedLeftHandKey);
            if (leftHandActionName != null)
            {
                leftHandActionName.text = leftHandHoldRequired
                    ? $"长 按 {CombatInputKeyText(expectedLeftHandKey)}  ·  保 持 {expectedLeftHandHoldDuration:0.00}s"
                    : $"短 按 {CombatInputKeyText(expectedLeftHandKey)}  ·  立 即 松 开";
                leftHandActionName.color = leftHandHoldRequired
                    ? new Color(.52f, .90f, 1f) : new Color(.48f, 1f, .72f);
            }
        }

        private void PrepareCombatBeatLegacy(int attackType, double beatTime)
        {
            if (!chapterOneCombatActive || finishingStrike || CombatPhaseResting) return;
            meleeCounterThreatId = -1;
            meleeCounterUntilDsp = 0d;
            // 高抛坠音与贴地冲击波要求空间闪避；持续追踪的和弦只能用 K 音符波破谱。
            expectedCombatAction = attackType == 2 ? CombatBeatAction.AttackTwo : CombatBeatAction.Dodge;
            expectedCombatBeatDsp = beatTime;
            combatBeatPending = true;
            combatBeatSucceeded = false;
            threatDamageResolved = false;
            currentThreatType = attackType;
            activeThreatId = ++threatSequence;
            autoActionIssuedForBeat = false;
            if (combatActionKey != null) combatActionKey.text = CombatActionKey(expectedCombatAction);
            if (combatActionName != null)
            {
                combatActionName.text = attackType switch
                {
                    0 => "高抛坠音 · 瞬身离开落点",
                    1 => "低音冲击波 · 向侧面瞬身",
                    _ => "追踪和弦 · K 远程破谱"
                };
                combatActionName.color = attackType == 1
                    ? new Color(0.12f, 0.88f, 1f)
                    : attackType == 2 ? new Color(0.88f, 0.30f, 1f) : new Color(1f, 0.24f, 0.22f);
            }
            float warningLifetime = Mathf.Max(0.40f, (float)(beatTime - AudioSettings.dspTime) + 0.28f);
            MeleeForbiddenRing3D.Show(creekSpirit != null ? creekSpirit.transform : null, expectedCombatAction, warningLifetime);
            ReportEnemyTelegraph(attackType);
            Debug.Log($"[MeixiSpatial] Threat id={activeThreatId} type={attackType} impact={expectedCombatBeatDsp:F3} now={AudioSettings.dspTime:F3}");
        }

        public double GetNextAnalyzedCombatBeat(double afterDsp, int phase)
        {
            int spacing = phase <= 1 ? 2 : phase == 2 ? 1 : 1;
            bool strong = phase <= 2;
            ChapterOneBattleTimeline timeline = phase >= 3 && phaseThreeBattleTimeline != null && phaseThreeBattleTimeline.IsValid
                ? phaseThreeBattleTimeline : chapterOneBattleTimeline;
            return timeline != null && timeline.IsValid
                ? timeline.GetNextBeatDsp(afterDsp, chapterOneBeatStart, spacing, strong)
                : afterDsp + (phase <= 1 ? 1.25d : phase == 2 ? 0.82d : 0.64d);
        }

        private CombatBeatAction SelectExpectedCombatAction(int phase, int patternIndex)
        {
            CombatBeatAction[] pattern = phase switch
            {
                1 => new[] { CombatBeatAction.AttackOne, CombatBeatAction.AttackTwo, CombatBeatAction.Dodge,
                    CombatBeatAction.AttackOne, CombatBeatAction.Dodge, CombatBeatAction.AttackTwo },
                2 => new[] { CombatBeatAction.JKWave, CombatBeatAction.TripleJ, CombatBeatAction.JDodgeStrike,
                    CombatBeatAction.JKRiff, CombatBeatAction.KDodgeTripleArc, CombatBeatAction.KJKRiff,
                    CombatBeatAction.AttackTwo, CombatBeatAction.Dodge },
                _ => new[] { CombatBeatAction.TripleJ, CombatBeatAction.JKRiff, CombatBeatAction.KJKRiff,
                    CombatBeatAction.KDodgeTripleArc, CombatBeatAction.JDodgeStrike, CombatBeatAction.JKWave,
                    CombatBeatAction.TripleJ, CombatBeatAction.AttackTwo }
            };
            return pattern[Mathf.Abs(patternIndex) % pattern.Length];
        }

        private int BuildCombatPattern(CombatBeatAction action, double firstBeat)
        {
            CombatInputKey first = CombatInputKey.None;
            switch (action)
            {
                case CombatBeatAction.AttackOne: first = CombatInputKey.J; break;
                case CombatBeatAction.AttackTwo: first = CombatInputKey.K; break;
                case CombatBeatAction.Dodge: first = CombatInputKey.Space; break;
                case CombatBeatAction.JKWave: expectedCombatSteps[0] = CombatInputKey.J; expectedCombatSteps[1] = CombatInputKey.K; break;
                case CombatBeatAction.JDodgeStrike: expectedCombatSteps[0] = CombatInputKey.J; expectedCombatSteps[1] = CombatInputKey.Space; break;
                case CombatBeatAction.KDodgeTripleArc: expectedCombatSteps[0] = CombatInputKey.K; expectedCombatSteps[1] = CombatInputKey.Space; break;
                case CombatBeatAction.TripleJ: expectedCombatSteps[0] = CombatInputKey.J; expectedCombatSteps[1] = CombatInputKey.J; expectedCombatSteps[2] = CombatInputKey.J; break;
                case CombatBeatAction.JKRiff: expectedCombatSteps[0] = CombatInputKey.J; expectedCombatSteps[1] = CombatInputKey.K; expectedCombatSteps[2] = CombatInputKey.J; break;
                case CombatBeatAction.KJKRiff: expectedCombatSteps[0] = CombatInputKey.K; expectedCombatSteps[1] = CombatInputKey.J; expectedCombatSteps[2] = CombatInputKey.K; break;
            }
            int count = action == CombatBeatAction.JKWave || action == CombatBeatAction.JDodgeStrike ||
                        action == CombatBeatAction.KDodgeTripleArc ? 2 :
                        action == CombatBeatAction.TripleJ || action == CombatBeatAction.JKRiff || action == CombatBeatAction.KJKRiff ? 3 : 1;
            if (count == 1) expectedCombatSteps[0] = first;
            expectedCombatStepDsps[0] = firstBeat;
            for (int i = 1; i < count; i++)
                expectedCombatStepDsps[i] = IsChordAction(action)
                    ? firstBeat
                    : firstBeat + i * CombatSequenceStepSpacing;
            return count;
        }

        public bool ProcessChapterOneCombatInput(MeixiThirdPersonPlayer actor, bool jPressed, bool kPressed, bool spacePressed,
            bool jHeld, bool kHeld, bool spaceHeld, bool qPressed, bool ePressed, bool qHeld, bool eHeld,
            bool qReleased, bool eReleased)
        {
            if (!chapterOneCombatActive || finishingStrike || actor == null) return false;
            if (CombatPhaseResting) return true;
            double now = AudioSettings.dspTime;
            // Q/E and J/K/SPACE can arrive on the same hardware frame. Some
            // keyboards briefly report the held state of the sustained key as
            // false while another key is pressed; that transient must not be
            // treated as an intentional release or it turns a valid right-hand
            // response into an avoidable damage event.
            bool rightHandInputPressed = jPressed || kPressed || spacePressed;
            bool leftInputConsumed = chapterOneCombatPhase >= 3 && leftHandCueActive &&
                                     ProcessLeftHandCombatInput(now, qPressed, ePressed, qHeld, eHeld, qReleased, eReleased,
                                         rightHandInputPressed);
            if (!combatBeatPending) return leftInputConsumed;
            if (bufferedCombatKey != CombatInputKey.None && now - bufferedCombatKeyDsp > CurrentCombatChordWindow)
            {
                bufferedCombatKey = CombatInputKey.None;
                rhythmJudge.text = "组合键未同时按住 · 光圈收束前仍可重试";
                rhythmJudge.color = new Color(1f, .72f, .28f);
            }
            if (combatBeatPending && !IsChordAction(expectedCombatAction) && expectedCombatStepIndex > 0 &&
                now - lastCombatSequenceInputDsp > CurrentCombatSequenceMaxGap)
            {
                rhythmJudge.text = "连击间隔过长 · 等待敌方本拍结算";
                rhythmJudge.color = new Color(1f, .62f, .26f);
            }
            CombatInputKey key = jPressed ? CombatInputKey.J : kPressed ? CombatInputKey.K : spacePressed ? CombatInputKey.Space : CombatInputKey.None;
            int pressedCount = (jPressed ? 1 : 0) + (kPressed ? 1 : 0) + (spacePressed ? 1 : 0);
            if (!combatBeatPending) return true;
            if (rightHandSucceeded)
            {
                if (pressedCount > 0)
                {
                    rhythmJudge.text = "右手声部已完成 · 继续维持左手节拍";
                    rhythmJudge.color = new Color(.42f, .88f, 1f);
                }
                return leftInputConsumed || pressedCount > 0;
            }
            if (pressedCount > 0)
            {
                double targetDsp = expectedCombatStepCount > 0 && expectedCombatStepIndex < expectedCombatStepCount
                    ? expectedCombatStepDsps[expectedCombatStepIndex] : expectedCombatBeatDsp;
                double grace = IsChordAction(expectedCombatAction)
                    ? CurrentCombatInputWindow + CurrentCombatChordWindow : CurrentCombatInputWindow;
                if (now < targetDsp - grace)
                {
                    if (IsChordAction(expectedCombatAction) && IsChordMember(expectedCombatAction, key))
                    {
                        bufferedCombatKey = key;
                        bufferedCombatKeyDsp = now;
                    }
                    rhythmJudge.text = "输入稍早 · 保留本拍，请在魂音收束时再按";
                    rhythmJudge.color = new Color(1f, .76f, .30f);
                    return true;
                }
                if (now > targetDsp + grace)
                {
                    rhythmJudge.text = "本拍输入窗口已过 · 等待敌方动作结算";
                    rhythmJudge.color = new Color(.68f, .78f, .84f);
                    return true;
                }
            }
            if (IsChordAction(expectedCombatAction))
            {
                bool chordHeld = ExpectedChordIsHeld(expectedCombatAction, jHeld, kHeld, spaceHeld);
                // Both keys may have been pressed on the previous rendered frame
                // and still be held on the beat frame. A buffered chord edge is
                // enough evidence to resolve it; requiring a fresh wasPressed
                // edge here was the source of intermittent phase-three misses.
                if (chordHeld && (pressedCount > 0 || bufferedCombatKey != CombatInputKey.None || IsChordOnBeat(now)))
                {
                    bool resolved = ResolvePatternInput(actor, expectedCombatAction, now);
                    bufferedCombatKey = CombatInputKey.None;
                    return resolved;
                }
                if (pressedCount == 0) return false;
                if (pressedCount > 1 || !IsChordMember(expectedCombatAction, key))
                {
                    FailCombatBeat("双键必须同时按下");
                    return true;
                }
                if (bufferedCombatKey == CombatInputKey.None)
                {
                    bufferedCombatKey = key;
                    bufferedCombatKeyDsp = now;
                    return true;
                }
                FailCombatBeat("双键必须保持同时按住");
                return true;
            }
            if (pressedCount == 0) return false;
            if (pressedCount > 1) { FailCombatBeat("顺序连击不能同时按键"); return true; }
            if (expectedCombatStepCount > 1) return ResolveSequenceStep(actor, key, now);
            return ResolveSingleKey(actor, key, now);
        }

        private bool ProcessLeftHandCombatInput(double now, bool qPressed, bool ePressed, bool qHeld, bool eHeld,
            bool qReleased, bool eReleased, bool rightHandInputPressed)
        {
            if (!leftHandCueActive || leftHandSucceeded || !combatBeatPending) return false;
            if (dualHandHoldPreviewActive) return false;
            bool correctPressed = expectedLeftHandKey == CombatInputKey.Q ? qPressed : ePressed;
            bool wrongPressed = expectedLeftHandKey == CombatInputKey.Q ? ePressed : qPressed;
            bool correctHeld = expectedLeftHandKey == CombatInputKey.Q ? qHeld : eHeld;
            bool correctReleased = expectedLeftHandKey == CombatInputKey.Q ? qReleased : eReleased;
            int leftPressedCount = (qPressed ? 1 : 0) + (ePressed ? 1 : 0);

            if (leftHandHoldRequired && leftHandHolding)
            {
                // An explicit release is authoritative. When J/K/SPACE is
                // pressed in the same frame, ignore a transient held-state
                // dropout and let the right-hand beat resolve normally.
                if (correctReleased || (!correctHeld && !rightHandInputPressed))
                {
                    FailCombatBeat("持续声部提前松开");
                    return true;
                }
                leftHandHoldProgress = Mathf.Clamp01((float)((now - leftHandHoldStartedDsp) /
                    Math.Max(.05f, expectedLeftHandHoldDuration)));
                if (leftHandHoldProgress >= 1f)
                {
                    CompleteLeftHandCombatInput(now);
                    return true;
                }
                if (leftHandActionName != null)
                    leftHandActionName.text = $"长 按 中  ·  保 持 不 松  {Mathf.RoundToInt(leftHandHoldProgress * 100f)}%";
                return leftPressedCount > 0;
            }

            if (leftPressedCount == 0) return false;
            double timingDelta = now - expectedLeftHandBeatDsp;
            if (timingDelta < -CurrentCombatInputWindow)
            {
                // A sustained note may be pressed slightly before the cue and
                // held through the beat. Start charging at the cue time so a
                // normal frame boundary cannot turn a valid hold into a miss.
                if (leftHandHoldRequired && correctPressed && -timingDelta <= CurrentCombatHoldEarlyBuffer)
                {
                    leftHandHolding = true;
                    leftHandHoldStartedDsp = expectedLeftHandBeatDsp;
                    leftHandHoldProgress = 0f;
                    if (leftHandActionName != null) leftHandActionName.text = "长 按 中  ·  保 持 不 松";
                    rhythmJudge.text = "左手持续声部已预备 · 保持按住";
                    rhythmJudge.color = new Color(.34f, .88f, 1f);
                    return true;
                }
                rhythmJudge.text = "左手输入稍早 · 等待蓝色声纹收束";
                rhythmJudge.color = new Color(1f, .72f, .28f);
                return true;
            }
            if (timingDelta > CurrentCombatInputWindow)
            {
                rhythmJudge.text = "左手声部已错过 · 本拍仍将结算";
                rhythmJudge.color = new Color(1f, .72f, .28f);
                return true;
            }
            if (leftPressedCount > 1 || wrongPressed || !correctPressed)
            {
                FailCombatBeat("左手声部按键错误");
                return true;
            }
            if (!leftHandHoldRequired)
            {
                CompleteLeftHandCombatInput(now);
                return true;
            }

            leftHandHolding = true;
            leftHandHoldStartedDsp = now;
            leftHandHoldProgress = 0f;
            if (leftHandActionName != null) leftHandActionName.text = "长 按 中  ·  保 持 不 松";
            rhythmJudge.text = "左手持续声部已接入 · 不要提前松开";
            rhythmJudge.color = new Color(.34f, .88f, 1f);
            return true;
        }

        private void CompleteLeftHandCombatInput(double now)
        {
            leftHandSucceeded = true;
            leftHandHolding = false;
            leftHandHoldProgress = 1f;
            if (leftHandActionKey != null) leftHandActionKey.text = "✓";
            if (leftHandActionName != null)
            {
                leftHandActionName.text = leftHandHoldRequired ? "长 按 完 成  ✓" : "短 按 命 中  ✓";
                leftHandActionName.color = new Color(.42f, 1f, .78f);
            }
            PlayerCombatFlourish3D.CreateDualHandResonance(PlayerPosition + Vector3.up * .86f,
                expectedLeftHandKey == CombatInputKey.Q, leftHandHoldRequired);
            audioSource.PlayOneShot(highTone, .46f);
            if (rightHandSucceeded) FinalizeCombatBeatSuccess(now, rightHandDodgeResponse);
            else
            {
                rhythmJudge.text = "左手声部完成 · 等待右手主旋律";
                rhythmJudge.color = new Color(.42f, 1f, .78f);
            }
        }

        private static bool IsChordAction(CombatBeatAction action)
        {
            return action == CombatBeatAction.JKWave || action == CombatBeatAction.JDodgeStrike ||
                   action == CombatBeatAction.KDodgeTripleArc;
        }

        private static bool ExpectedChordIsHeld(CombatBeatAction action, bool jHeld, bool kHeld, bool spaceHeld)
        {
            return action switch
            {
                CombatBeatAction.JKWave => jHeld && kHeld && !spaceHeld,
                CombatBeatAction.JDodgeStrike => jHeld && spaceHeld && !kHeld,
                CombatBeatAction.KDodgeTripleArc => kHeld && spaceHeld && !jHeld,
                _ => false
            };
        }

        private static bool IsChordMember(CombatBeatAction action, CombatInputKey key)
        {
            return action switch
            {
                CombatBeatAction.JKWave => key == CombatInputKey.J || key == CombatInputKey.K,
                CombatBeatAction.JDodgeStrike => key == CombatInputKey.J || key == CombatInputKey.Space,
                CombatBeatAction.KDodgeTripleArc => key == CombatInputKey.K || key == CombatInputKey.Space,
                _ => false
            };
        }

        private bool ResolveSingleKey(MeixiThirdPersonPlayer actor, CombatInputKey key, double now)
        {
            if (!IsExpectedStep(key, now)) { FailCombatBeat("节拍动作错误"); return true; }
            CombatBeatAction action = expectedCombatAction;
            bool executed = actor.ExecuteAuthoredRhythmAction(action, key, 0);
            if (!executed) { FailCombatBeat("动作未命中"); return true; }
            CompleteCombatBeatSuccess(now, action == CombatBeatAction.Dodge);
            return true;
        }

        private bool ResolvePatternInput(MeixiThirdPersonPlayer actor, CombatBeatAction action, double now)
        {
            if (expectedCombatAction != action || !IsChordOnBeat(now)) { FailCombatBeat("组合节拍错误"); return true; }
            bool executed = actor.ExecuteAuthoredRhythmAction(action, CombatInputKey.None, 0);
            if (!executed) { FailCombatBeat("组合动作未命中"); return true; }
            CompleteCombatBeatSuccess(now, false);
            return true;
        }

        private bool ResolveSequenceStep(MeixiThirdPersonPlayer actor, CombatInputKey key, double now)
        {
            if (key != expectedCombatSteps[expectedCombatStepIndex])
            { FailCombatBeat("连击顺序错误"); return true; }
            if (expectedCombatStepIndex == 0)
            {
                if (Math.Abs(now - expectedCombatStepDsps[0]) > CurrentCombatInputWindow)
                { FailCombatBeat("连击起手没有踩中鼓点"); return true; }
            }
            else
            {
                double interval = now - lastCombatSequenceInputDsp;
                if (interval < 0.008d || interval > CurrentCombatSequenceMaxGap)
                { FailCombatBeat("连击间隔错误"); return true; }
                if (Math.Abs(now - expectedCombatStepDsps[expectedCombatStepIndex]) > CurrentCombatSequenceStepTolerance)
                { FailCombatBeat("连击未跟随当前鼓点"); return true; }
            }
            if (!actor.ExecuteAuthoredRhythmAction(expectedCombatAction, key, expectedCombatStepIndex))
            { FailCombatBeat("连击有效距离不足"); return true; }
            lastCombatSequenceInputDsp = now;
            expectedCombatStepIndex++;
            if (expectedCombatStepIndex >= expectedCombatStepCount) CompleteCombatBeatSuccess(now, false);
            else
            {
                combatActionKey.text = CombatInputKeyText(expectedCombatSteps[expectedCombatStepIndex]);
                combatActionName.text = $"{CombatActionName(expectedCombatAction)}  {expectedCombatStepIndex + 1}/{expectedCombatStepCount}";
            }
            return true;
        }

        private bool IsExpectedStep(CombatInputKey key, double now)
        {
            return combatBeatPending && expectedCombatStepCount > 0 && expectedCombatSteps[expectedCombatStepIndex] == key &&
                   Math.Abs(now - expectedCombatStepDsps[expectedCombatStepIndex]) <= CurrentCombatInputWindow;
        }

        private bool IsChordOnBeat(double now)
        {
            if (!combatBeatPending) return false;
            double target = expectedCombatStepDsps[0];
            double first = bufferedCombatKey != CombatInputKey.None ? bufferedCombatKeyDsp : now;
            return Math.Abs(first - target) <= CurrentCombatInputWindow + CurrentCombatChordWindow &&
                   Math.Abs(now - target) <= CurrentCombatInputWindow + CurrentCombatChordWindow;
        }

        private void CompleteCombatBeatSuccess(double now, bool dodge)
        {
            rightHandSucceeded = true;
            rightHandDodgeResponse = dodge;
            if (leftHandCueActive && !leftHandSucceeded)
            {
                bufferedCombatKey = CombatInputKey.None;
                lastCombatSequenceInputDsp = 0d;
                if (combatActionKey != null) combatActionKey.text = "✓";
                if (combatActionName != null)
                {
                    combatActionName.text = "右 手 主 旋 律 完 成";
                    combatActionName.color = new Color(1f, .72f, .30f);
                }
                rhythmJudge.text = leftHandHoldRequired
                    ? "右手完成 · 继续维持左手持续声部"
                    : "右手完成 · 等待左手短拍";
                rhythmJudge.color = new Color(.42f, .88f, 1f);
                return;
            }
            FinalizeCombatBeatSuccess(now, dodge);
        }

        private void FinalizeCombatBeatSuccess(double now, bool dodge)
        {
            combatBeatPending = false;
            bufferedCombatKey = CombatInputKey.None;
            lastCombatSequenceInputDsp = 0d;
            combatBeatSucceeded = true;
            threatDamageResolved = true;
            resolvedThreatId = activeThreatId;
            if (dodge) dodgedThreatId = activeThreatId;
            else counteredThreatId = activeThreatId;
            playerDamageImmuneUntilDsp = Math.Max(playerDamageImmuneUntilDsp, currentThreatResolveDsp + 0.48d);
            chapterOneCombatCombo++;
            chapterOneCombatSuccesses++;
            chapterOneCombatBestCombo = Mathf.Max(chapterOneCombatBestCombo, chapterOneCombatCombo);
            MeleeForbiddenRing3D.ClearAll(true);
            TrackingAttackTelegraph3D.ClearAll();
            EnemyMusicAttackVfx3D.ShatterAllAt(CombatTargetPosition + Vector3.up * 0.95f);
            rhythmJudge.text = $"PERFECT  {CombatActionName(expectedCombatAction)}";
            rhythmJudge.color = new Color(0.36f, 1f, 0.84f);
            if (combatActionKey != null) combatActionKey.text = "✓";
            if (leftHandCueActive && leftHandActionKey != null) leftHandActionKey.text = "✓";
            orbitCamera?.TriggerCombatImpact(true);
            audioSource.PlayOneShot(highTone, 0.75f);
            Debug.Log($"[MeixiBeat] success action={expectedCombatAction} threat={activeThreatId} input={now:F3}");
        }

        private static string CombatInputKeyText(CombatInputKey key) => key switch
        {
            CombatInputKey.J => "J",
            CombatInputKey.K => "K",
            CombatInputKey.Space => "SPACE",
            CombatInputKey.Q => "Q",
            CombatInputKey.E => "E",
            _ => "·"
        };

        private void UpdateAutomatedRhythmValidation()
        {
            if (!combatBeatPending || autoActionIssuedForBeat || player == null) return;
            double lead = rhythmFinisherValidation ? 0.48d : meleeAutoValidation ? 0.20d : 0.16d;
            if (AudioSettings.dspTime < expectedCombatBeatDsp - lead) return;
            autoActionIssuedForBeat = true;
            CombatBeatAction action = rhythmFinisherValidation
                ? CombatBeatAction.AttackTwo
                : meleeAutoValidation || wrongMeleeValidation ? CombatBeatAction.AttackOne : expectedCombatAction;
            if ((meleeAutoValidation || wrongMeleeValidation) && creekSpirit != null && playerTransform != null)
            {
                CharacterController controller = playerTransform.GetComponent<CharacterController>();
                if (controller != null) controller.enabled = false;
                Vector3 testPosition = creekSpirit.transform.position + creekSpirit.transform.forward * (wrongMeleeValidation ? 2.45f : 4.15f);
                testPosition.y = 0.30f;
                playerTransform.position = testPosition;
                playerTransform.forward = Vector3.ProjectOnPlane(creekSpirit.transform.position - testPosition, Vector3.up).normalized;
                if (controller != null) controller.enabled = true;
            }
            player.TriggerCombatActionForValidation(action);
        }

        public bool TrySubmitCombatAction(CombatBeatAction action)
        {
            if (!chapterOneCombatActive || finishingStrike) return false;
            if (action == CombatBeatAction.AttackTwo) return TryResolveRangedCounter();
            // J 由三维距离与近战碰撞结算；SPACE 仍由瞬身的实际落点判定。
            if (action != CombatBeatAction.Dodge) return false;
            Vector3 origin = PlayerPosition;
            Vector3 destination = SelectBlinkDestination(origin, Vector3.zero);
            return ResolveBlinkDodge(origin, destination);
        }

        public Vector3 SelectBlinkDestination(Vector3 origin, Vector3 preferredDirection)
        {
            Vector3 enemyDelta = origin - CombatTargetPosition;
            enemyDelta.y = 0f;
            Vector3 away = enemyDelta.sqrMagnitude > 0.01f ? enemyDelta.normalized : -playerTransform.forward;
            Vector3 direction = preferredDirection;
            direction.y = 0f;
            direction = direction.sqrMagnitude > 0.05f ? direction.normalized : away;

            const float blinkDistance = 3.9f;
            float allowedDistance = blinkDistance;
            Vector3 castOrigin = origin + Vector3.up * 0.78f;
            RaycastHit[] hits = Physics.SphereCastAll(castOrigin, 0.32f, direction, blinkDistance, ~0, QueryTriggerInteraction.Ignore);
            foreach (RaycastHit hit in hits)
            {
                Transform hitTransform = hit.collider != null ? hit.collider.transform : null;
                if (hitTransform == null) continue;
                if (playerTransform != null && (hitTransform == playerTransform || hitTransform.IsChildOf(playerTransform))) continue;
                if (creekSpirit != null && (hitTransform == creekSpirit.transform || hitTransform.IsChildOf(creekSpirit.transform))) continue;
                allowedDistance = Mathf.Min(allowedDistance, Mathf.Max(0.35f, hit.distance - 0.48f));
            }
            return origin + direction * allowedDistance;
        }

        public bool ResolveBlinkDodge(Vector3 origin, Vector3 destination)
        {
            if (!chapterOneCombatActive || finishingStrike) return false;
            Vector3 enemyDelta = destination - CombatTargetPosition;
            enemyDelta.y = 0f;
            float travelled = Vector3.ProjectOnPlane(destination - origin, Vector3.up).magnitude;
            bool safe = enemyDelta.magnitude >= 2.35f && travelled >= 1.20f;
            PlayBlinkFx(origin, destination, safe);

            if (combatBeatPending && expectedCombatAction != CombatBeatAction.Dodge)
            {
                rhythmJudge.text = "该拍需要 K 远程破谱 · 追踪和弦仍在锁定";
                rhythmJudge.color = new Color(1f, 0.42f, 0.28f);
                if (combatActionKey != null) combatActionKey.text = "K";
                ShowToast("动 作 不 匹 配 · 追 踪 未 解 除", new Color(1f, 0.42f, 0.28f));
                Debug.Log($"[MeixiSpatial] Dodge ignored for K threat id={activeThreatId}");
                return false;
            }

            TrackingAttackTelegraph3D.LockAllAt(origin);
            EnemyMusicAttackVfx3D.LockAllAt(origin);

            if (!safe)
            {
                if (!threatDamageResolved)
                {
                    DamagePlayer();
                    player?.TriggerHitReaction(CombatTargetPosition);
                    orbitCamera?.TriggerRhythmMiss();
                SpawnMissBeatVfx(destination + Vector3.up * 0.9f);
                    if (!defeatOpen) creekSpirit?.RepositionAfterPlayerMistake(PlayerPosition);
                }
                combatBeatPending = false;
                combatBeatSucceeded = false;
                threatDamageResolved = true;
                resolvedThreatId = activeThreatId;
                MeleeForbiddenRing3D.ClearAll(false);
                chapterOneCombatCombo = 0;
                chapterOneCombatMisses++;
                rhythmJudge.text = travelled < 1.20f ? "瞬身受阻 · 溪灵命中" : "瞬身落点过近 · 受到反噬";
                rhythmJudge.color = new Color(1f, 0.28f, 0.24f);
                if (combatActionKey != null) combatActionKey.text = "×";
                Debug.Log($"[MeixiSpatial] Unsafe blink travelled={travelled:F2} enemyDistance={enemyDelta.magnitude:F2} health={health}");
                return false;
            }

            if (combatBeatPending)
            {
                combatBeatPending = false;
                combatBeatSucceeded = true;
                threatDamageResolved = true;
                dodgedThreatId = activeThreatId;
                playerDamageImmuneUntilDsp = Math.Max(playerDamageImmuneUntilDsp, expectedCombatBeatDsp + 0.55d);
                MeleeForbiddenRing3D.ClearAll(true);
                chapterOneCombatCombo++;
                if (rhythmAutoValidation && !rhythmFinisherValidation && captureMode && !captureDone)
                    captureAt = Time.unscaledTime + 0.08f;
                rhythmJudge.text = "瞬 身 残 响 · 追 踪 丢 失";
                rhythmJudge.color = new Color(0.34f, 1f, 0.82f);
                if (combatActionKey != null) combatActionKey.text = "闪";
                meleeCounterThreatId = activeThreatId;
                meleeCounterUntilDsp = AudioSettings.dspTime + 0.78d;
                if (combatActionKey != null) combatActionKey.text = "J";
                if (combatActionName != null)
                {
                    combatActionName.text = "瞬 身 后 摇 · J 突 进 反 击";
                    combatActionName.color = new Color(1f, 0.80f, 0.28f);
                }
                ReportPerfectDodge(Time.time + 0.78f);
                Debug.Log($"[MeixiSpatial] Dodge success id={activeThreatId} enemyDistance={enemyDelta.magnitude:F2} " +
                          $"immuneUntil={playerDamageImmuneUntilDsp:F3} counterUntil={meleeCounterUntilDsp:F3} health={health}");
            }
            else
            {
                ShowToast("瞬 身 · 未 处 于 攻 击 锁 定", new Color(0.42f, 0.84f, 1f));
            }
            return true;
        }

        public bool TryResolveRangedCounter()
        {
            if (!chapterOneCombatActive || finishingStrike || !combatBeatPending ||
                expectedCombatAction != CombatBeatAction.AttackTwo) return false;
            double delta = Math.Abs(AudioSettings.dspTime - expectedCombatBeatDsp);
            if (delta > CurrentCombatInputWindow)
            {
                rhythmJudge.text = AudioSettings.dspTime < expectedCombatBeatDsp ? "K 抢拍 · 等待和弦聚合" : "K 晚拍 · 追踪仍未解除";
                rhythmJudge.color = new Color(1f, 0.52f, 0.26f);
                return false;
            }

            combatBeatPending = false;
            combatBeatSucceeded = true;
            threatDamageResolved = true;
            counteredThreatId = activeThreatId;
            playerDamageImmuneUntilDsp = Math.Max(playerDamageImmuneUntilDsp, expectedCombatBeatDsp + 0.40d);
            chapterOneCombatCombo++;
            EnemyMusicAttackVfx3D.ShatterAllAt(CombatTargetPosition + Vector3.up * 1.0f);
            TrackingAttackTelegraph3D.ClearAll();
            MeleeForbiddenRing3D.ClearAll(true);
            PlayerCombatFlourish3D.CreateRangedCounter(PlayerPosition + Vector3.up * 0.9f, CombatTargetPosition + Vector3.up * 0.9f);
            rhythmJudge.text = "K 完美破谱 · 追踪和弦崩解";
            rhythmJudge.color = new Color(0.30f, 1f, 0.92f);
            if (combatActionKey != null) combatActionKey.text = "破";
            ShowToast("◆ 远 程 破 谱 · 和 弦 崩 解 ◆", new Color(0.30f, 1f, 0.92f));
            orbitCamera?.TriggerCombatImpact(true);
            audioSource.PlayOneShot(highTone, 0.90f);
            if ((rhythmAutoValidation || rangedCounterValidation) && captureMode && !captureDone)
                captureAt = Time.unscaledTime + 0.10f;
            Debug.Log($"[MeixiSpatial] Ranged counter success id={activeThreatId} delta={delta:F3} health={health}");
            return true;
        }

        public bool TryPunishForbiddenMelee(Vector3 origin)
        {
            if (!chapterOneCombatActive || finishingStrike || !combatBeatPending ||
                expectedCombatAction == CombatBeatAction.AttackOne || !MeleeForbiddenRing3D.Contains(origin)) return false;

            combatBeatPending = false;
            combatBeatSucceeded = false;
            threatDamageResolved = true;
            resolvedThreatId = activeThreatId;
            chapterOneCombatCombo = 0;
            chapterOneCombatMisses++;
            playerDamageImmuneUntilDsp = 0d;
            DamagePlayer();
            player?.TriggerHitReaction(CombatTargetPosition);
            orbitCamera?.TriggerRhythmMiss();
            PlayerCombatFlourish3D.CreateForbiddenCounter(origin + Vector3.up * 0.85f, CombatTargetPosition + Vector3.up * 0.9f);
            EnemyMusicAttackVfx3D.ShatterAllAt(origin + Vector3.up * 0.8f, new Color(1f, 0.08f, 0.16f));
            TrackingAttackTelegraph3D.ClearAll();
            MeleeForbiddenRing3D.ClearAll(false);
            if (!defeatOpen) creekSpirit?.RepositionAfterPlayerMistake(PlayerPosition);
            rhythmJudge.text = expectedCombatAction == CombatBeatAction.AttackTwo
                ? "误按 J · 闯入禁区，被追踪和弦反击"
                : "误按 J · 闯入禁区，被敌方节拍反击";
            rhythmJudge.color = new Color(1f, 0.18f, 0.20f);
            if (combatActionKey != null) combatActionKey.text = "×";
            ShowToast("禁 区 误 攻 · 反 击 扣 血", new Color(1f, 0.16f, 0.20f));
            if (wrongMeleeValidation && captureMode && !captureDone) captureAt = Time.unscaledTime + 0.10f;
            Debug.Log($"[MeixiSpatial] Forbidden melee punished id={activeThreatId} health={health}");
            return true;
        }

        public bool ResolveEnemyAttack(int attackType, Vector3 origin)
        {
            if (!chapterOneCombatActive || finishingStrike || CombatPhaseResting) return false;
            if (activeThreatId <= 0 || resolvedThreatId == activeThreatId) return false;
            if (dodgedThreatId == activeThreatId || counteredThreatId == activeThreatId || combatBeatSucceeded)
            {
                bool rangedCounter = counteredThreatId == activeThreatId;
                resolvedThreatId = activeThreatId;
                combatBeatSucceeded = false;
                threatDamageResolved = true;
                currentThreatType = -1;
                MeleeForbiddenRing3D.ClearAll(true);
                rhythmJudge.text = rangedCounter ? "追 踪 和 弦 已 被 破 谱" : "追 踪 攻 击 落 空";
                rhythmJudge.color = new Color(0.32f, 0.94f, 0.86f);
                if (!rangedCounter && MeleeCounterWindowOpen)
                {
                    rhythmJudge.text = "追踪攻击落空 · 立即按 J 突进反击";
                    if (combatActionKey != null) combatActionKey.text = "J";
                }
                Debug.Log($"[MeixiSpatial] Threat invalidated response={(rangedCounter ? "K" : "SPACE")} id={activeThreatId} health={health}");
                return false;
            }
            if (threatDamageResolved)
            {
                resolvedThreatId = activeThreatId;
                currentThreatType = -1;
                MeleeForbiddenRing3D.ClearAll(false);
                return false;
            }

            resolvedThreatId = activeThreatId;
            combatBeatPending = false;
            chapterOneCombatCombo = 0;
            chapterOneCombatMisses++;
            threatDamageResolved = true;
            MeleeForbiddenRing3D.ClearAll(false);
            DamagePlayer();
            player?.TriggerHitReaction(origin);
            orbitCamera?.TriggerCombatImpact(false);
            SpawnMissBeatVfx(PlayerPosition + Vector3.up * 0.9f);
            if (!defeatOpen) creekSpirit?.RepositionAfterPlayerMistake(PlayerPosition);
            rhythmJudge.text = attackType switch
            {
                0 => "高抛坠音命中 · 回响力受损",
                1 => "低音冲击波命中 · 回响力受损",
                3 => "音符冲刺贯穿 · 回响力受损",
                _ => "追踪和弦合击 · 回响力受损"
            };
            rhythmJudge.color = new Color(1f, 0.28f, 0.24f);
            if (combatActionKey != null) combatActionKey.text = "×";
            Debug.Log($"[MeixiSpatial] Guaranteed enemy hit type={attackType} health={health}");
            return true;
        }

        public bool TryGetAuthoredRhythmMove(CombatBeatAction action, CombatInputKey stepKey, int stepIndex,
            Vector3 origin, out Vector3 destination)
        {
            destination = origin;
            if (!chapterOneCombatActive || creekSpirit == null) return false;
            Vector3 enemy = CombatTargetPosition;
            Vector3 fromEnemy = Vector3.ProjectOnPlane(origin - enemy, Vector3.up);
            if (fromEnemy.sqrMagnitude < 0.01f) fromEnemy = -playerTransform.forward;
            fromEnemy.Normalize();
            if (action == CombatBeatAction.Dodge)
            {
                destination = SelectBlinkDestination(origin, fromEnemy);
                return Vector3.ProjectOnPlane(destination - origin, Vector3.up).magnitude >= 1.20f &&
                       Vector3.ProjectOnPlane(destination - enemy, Vector3.up).magnitude >= 2.35f;
            }
            if (action == CombatBeatAction.JDodgeStrike)
            {
                Vector3 side = Vector3.Cross(Vector3.up, fromEnemy).normalized;
                destination = enemy + (fromEnemy * 1.58f + side * 0.72f).normalized * 1.82f;
                destination.y = origin.y;
                return true;
            }
            if (action == CombatBeatAction.TripleJ && stepKey == CombatInputKey.J)
            {
                float crossDistance = 1.62f + Mathf.Clamp(stepIndex, 0, 2) * .10f;
                destination = enemy - fromEnemy * crossDistance;
                destination.y = origin.y;
                return Vector3.ProjectOnPlane(enemy - origin, Vector3.up).magnitude <= 5.80f;
            }
            bool meleeStep = action == CombatBeatAction.AttackOne ||
                             ((action == CombatBeatAction.TripleJ || action == CombatBeatAction.JKRiff || action == CombatBeatAction.KJKRiff) && stepKey == CombatInputKey.J);
            if (!meleeStep) return true;
            float distance = Vector3.ProjectOnPlane(enemy - origin, Vector3.up).magnitude;
            if (distance > 4.80f) return false;
            destination = enemy + fromEnemy * 1.72f;
            destination.y = origin.y;
            return true;
        }

        public bool ResolveAuthoredRhythmImpact(CombatBeatAction action, CombatInputKey stepKey, int stepIndex,
            Vector3 origin, Vector3 destination, Vector3 forward)
        {
            if (!chapterOneCombatActive || creekSpirit == null || finishingStrike || CombatPhaseResting) return false;
            if (action == CombatBeatAction.Dodge)
            {
                bool safe = Vector3.ProjectOnPlane(destination - CombatTargetPosition, Vector3.up).magnitude >= 2.35f &&
                            Vector3.ProjectOnPlane(destination - origin, Vector3.up).magnitude >= 1.20f;
                PlayBlinkFx(origin, destination, safe);
                return safe;
            }

            bool meleeImpact = action == CombatBeatAction.AttackOne || action == CombatBeatAction.JDodgeStrike ||
                               ((action == CombatBeatAction.TripleJ || action == CombatBeatAction.JKRiff || action == CombatBeatAction.KJKRiff) && stepKey == CombatInputKey.J);
            if (meleeImpact)
            {
                Vector3 hitDelta = CombatTargetPosition - playerTransform.position;
                hitDelta.y = 0f;
                if (hitDelta.magnitude > 2.45f || (hitDelta.sqrMagnitude > 0.01f && Vector3.Dot(forward.normalized, hitDelta.normalized) < 0.10f))
                    return false;
            }

            bool hit = creekSpirit.TryReceiveRhythmActionHit(action, stepKey, stepIndex);
            if (!hit) return false;
            Vector3 impact = creekSpirit.transform.position + Vector3.up * 0.92f;
            if (action == CombatBeatAction.TripleJ)
                PlayerCombatFlourish3D.CreatePhaseThroughSlash(origin, destination, impact, Mathf.Clamp(stepIndex, 0, 2));
            else if (action == CombatBeatAction.JKWave)
                PlayerCombatFlourish3D.CreateComboWave(origin + Vector3.up * 0.85f, impact, forward);
            else if (action == CombatBeatAction.JDodgeStrike)
            {
                PlayerCombatFlourish3D.CreateBlink(origin, destination, true);
                PlayerCombatFlourish3D.CreateBlinkStrike(destination + Vector3.up * 0.78f, impact, forward);
                PlayerCombatFlourish3D.CreateComboWave(destination + Vector3.up * .64f, impact, forward);
            }
            else if (action == CombatBeatAction.KDodgeTripleArc)
                PlayerCombatFlourish3D.CreateTripleArc(origin + Vector3.up * 1.02f, impact, forward);
            else if (stepKey == CombatInputKey.K || action == CombatBeatAction.AttackTwo)
            {
                PlayerCombatFlourish3D.CreateRangedCast(origin + Vector3.up * 1.02f, forward, true);
                PlayerCombatFlourish3D.CreateRangedCounter(origin + Vector3.up * 0.95f, impact);
            }
            else
                PlayerCombatFlourish3D.CreateMelee(origin, impact, forward, true, Mathf.Clamp(stepIndex, 0, 2));

            if (stepIndex >= 2 && (action == CombatBeatAction.JKRiff || action == CombatBeatAction.KJKRiff))
                PlayerCombatFlourish3D.CreateSequenceFinale(origin, impact, forward,
                    action == CombatBeatAction.KJKRiff);

            if (action != CombatBeatAction.TripleJ)
                EnemyMusicImpact3D.Create(impact, CombatActionColor(action),
                    action == CombatBeatAction.JDodgeStrike ? 5.4f : action == CombatBeatAction.JKWave ? 4.2f : 3.2f,
                    action == CombatBeatAction.JDodgeStrike ? 0.82f : 0.62f, 18 + stepIndex * 5);
            if (action != CombatBeatAction.TripleJ || stepIndex >= 2)
                SpawnGroundSigil(new Vector3(impact.x, 0.06f, impact.z),
                    action == CombatBeatAction.TripleJ ? 2.0f : 2.4f + stepIndex * 0.20f,
                    action == CombatBeatAction.TripleJ ? .38f : 0.55f, CombatActionColor(action));
            SpawnBeatShardBurst(impact,
                action == CombatBeatAction.TripleJ ? 3 + stepIndex * 2 : 22 + stepIndex * 5,
                action == CombatBeatAction.TripleJ ? 3.1f : 4.2f, CombatActionColor(action));
            orbitCamera?.TriggerMeleeImpact(Mathf.Clamp(stepIndex, 0, 2), true);
            audioSource.PlayOneShot(stepKey == CombatInputKey.K || action == CombatBeatAction.AttackTwo ? highTone : lowTone, 0.82f);
            return true;
        }

        public bool TryStartMeleeLunge(Vector3 origin, out Vector3 direction)
        {
            Vector3 delta = CombatTargetPosition - origin;
            delta.y = 0f;
            direction = delta.sqrMagnitude > 0.01f ? delta.normalized : playerTransform.forward;
            float acquisitionRange = MeleeCounterWindowOpen ? 5.60f : 4.80f;
            bool inAcquisitionRange = chapterOneCombatActive && !CombatPhaseResting && creekSpirit != null && delta.magnitude <= acquisitionRange;
            Debug.Log($"[MeixiSpatial] Melee acquire distance={delta.magnitude:F2} armed={inAcquisitionRange}");
            if (!inAcquisitionRange)
            {
                rhythmJudge.text = "J 近战距离不足 · 仅向目标突进";
                rhythmJudge.color = new Color(0.54f, 0.78f, 0.90f);
                ShowToast("距 离 过 远 · 突 进 无 攻 击", new Color(0.48f, 0.78f, 0.92f));
            }
            return inAcquisitionRange;
        }

        public bool ResolvePlayerMelee(Vector3 origin, Vector3 forward, int comboStep)
        {
            if (!chapterOneCombatActive || creekSpirit == null || finishingStrike) return false;
            bool counterWindow = MeleeCounterWindowOpen;
            bool onBeat = counterWindow || IsChapterMusicOnBeat();
            bool hit = creekSpirit.TryReceiveMeleeHit(origin, forward, onBeat, comboStep, counterWindow);
            if (!hit)
            {
                rhythmJudge.text = "近 战 落 空 · 需 靠 近 溪 灵";
                rhythmJudge.color = new Color(0.58f, 0.76f, 0.86f);
                ShowToast("突 进 落 空", new Color(0.52f, 0.76f, 0.90f));
                return false;
            }
            if (counterWindow)
            {
                meleeCounterThreatId = -1;
                meleeCounterUntilDsp = 0d;
                chapterOneCombatCombo++;
                if (combatActionKey != null) combatActionKey.text = "反";
                if (combatActionName != null)
                {
                    combatActionName.text = "J 完 美 近 战 反 击";
                    combatActionName.color = new Color(1f, 0.80f, 0.28f);
                }
            }
            // 致命一击已经切入专属终结演出，不再叠加普通 J 命中特效。
            if (finishingStrike) return true;
            Vector3 meleeImpact = creekSpirit.transform.position + Vector3.up * 0.92f;
            PlayerCombatFlourish3D.CreateMelee(origin, meleeImpact, forward, onBeat, comboStep);
            EnemyMusicImpact3D.Create(meleeImpact, onBeat ? Color.white : new Color(0.18f, 0.94f, 1f),
                onBeat ? 3.0f + comboStep * 0.28f : 2.2f, onBeat ? 0.58f : 0.44f, onBeat ? 12 + comboStep * 2 : 8);
            PlayCombatResponseFx(CombatBeatAction.AttackOne, onBeat, false);
            orbitCamera?.TriggerMeleeImpact(comboStep, onBeat);
            audioSource.PlayOneShot(lowTone, 0.82f);
            if (meleeAutoValidation && captureMode && !captureDone)
                captureAt = Time.unscaledTime + 0.07f;
            Debug.Log($"[MeixiSpatial] Melee hit combo={comboStep + 1} onBeat={onBeat} counter={counterWindow}");
            return true;
        }

        public void FirePlayerNoteWave(Vector3 origin, Vector3 direction)
        {
            if (!chapterOneCombatActive || creekSpirit == null || finishingStrike || CombatPhaseResting) return;
            bool countered = TryResolveRangedCounter();
            bool onBeat = countered || IsChapterMusicOnBeat();
            Vector3 targetDirection = CombatTargetPosition + Vector3.up * 0.85f - origin;
            if (targetDirection.sqrMagnitude > 0.01f) direction = targetDirection.normalized;
            PlayerNoteWaveProjectile3D.Create(this, creekSpirit, origin, direction, cyan, gold, onBeat);
            PlayerCombatFlourish3D.CreateRangedCast(origin, direction, onBeat);
            SpawnGeneratedSlash(origin + direction * 0.55f, 1.35f, 24f, 0.30f, new Color(0.30f, 0.92f, 1f, 0.78f));
            audioSource.PlayOneShot(highTone, 0.72f);
            rhythmJudge.text = onBeat ? "K 共鸣音符波 · 节拍强化" : "K 远程音符波";
            rhythmJudge.color = onBeat ? new Color(1f, 0.80f, 0.28f) : new Color(0.28f, 0.92f, 1f);
        }

        public void ResolvePlayerRangedHit(CreekRhythmEnemy3D target, bool onBeat, Vector3 impactPosition)
        {
            if (!chapterOneCombatActive || target == null || finishingStrike || CombatPhaseResting) return;
            if (!target.TryReceiveRangedHit(onBeat)) return;
            // 致命 K 命中会在 TryReceiveRangedHit 内启动终结演出，跳过普通远攻爆发以免遮挡特写。
            if (finishingStrike) return;
            EnemyMusicImpact3D.Create(impactPosition, onBeat ? new Color(0.92f, 1f, 1f) : new Color(0.18f, 0.90f, 1f),
                onBeat ? 4.0f : 3.0f, onBeat ? 0.72f : 0.56f, onBeat ? 22 : 14);
            PlayerCombatFlourish3D.CreateRangedImpact(impactPosition, onBeat);
            SpawnGroundSigil(new Vector3(impactPosition.x, 0.06f, impactPosition.z), 2.2f, 0.48f,
                onBeat ? Color.white : new Color(0.28f, 0.90f, 1f, 0.84f));
            SpawnBeatShardBurst(impactPosition, onBeat ? 26 : 16, onBeat ? 4.3f : 3.2f,
                onBeat ? Color.white : new Color(0.28f, 0.90f, 1f));
            orbitCamera?.TriggerCombatImpact(onBeat);
            Debug.Log($"[MeixiSpatial] Ranged note hit onBeat={onBeat}");
        }

        public bool IsChapterMusicOnBeat(float windowSeconds = 0.12f)
        {
            if (!chapterOneCombatActive) return false;
            ChapterOneBattleTimeline timeline = chapterOneCombatPhase >= 3 && phaseThreeBattleTimeline != null && phaseThreeBattleTimeline.IsValid
                ? phaseThreeBattleTimeline : chapterOneBattleTimeline;
            if (timeline != null && timeline.IsValid)
                return timeline.DistanceToNearestBeat(AudioSettings.dspTime, chapterOneBeatStart) <= windowSeconds;
            double beatLength = 60d / Math.Max(1f, chapterOneCombatBpm);
            double elapsed = Math.Max(0d, AudioSettings.dspTime - chapterOneBeatStart);
            double phase = elapsed % beatLength;
            return phase <= windowSeconds || phase >= beatLength - windowSeconds;
        }

        private static void SetCombatBarVisualAmount(Image image, float amount)
        {
            if (image == null) return;
            amount = Mathf.Clamp01(amount);
            image.fillAmount = amount;
            RectTransform rect = image.rectTransform;
            const float minX = .015f;
            const float maxX = .985f;
            rect.anchorMin = new Vector2(minX, .15f);
            rect.anchorMax = new Vector2(Mathf.Lerp(minX, maxX, amount), .85f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        public void ReportCombatPhase(int phase)
        {
            chapterOneCombatPhase = Mathf.Clamp(phase, 1, 3);
            string phaseName = chapterOneCombatPhase switch { 1 => "听潮", 2 => "逆流", _ => "回声暴雨" };
            if (creekSpirit != null)
            {
                float overallHealth = Mathf.Clamp01(creekSpirit.Health /
                    (float)Mathf.Max(1, creekSpirit.HealthMaximum));
                SetCombatBarVisualAmount(combatEnemyHealthFill, overallHealth);
                SetCombatBarVisualAmount(combatEnemyHealthChipFill, overallHealth);
                Debug.Log($"[MeixiBossHud] phase={chapterOneCombatPhase} overall={overallHealth:F3} " +
                          $"health={creekSpirit.Health}/{creekSpirit.HealthMaximum}.");
            }
            if (chapterOneCombatPhase < 3)
                ShowToast($"第 {chapterOneCombatPhase} 段 · {phaseName}", new Color(1f, 0.72f, 0.28f));
            rhythmJudge.text = chapterOneCombatPhase == 3
                ? "红区锁定加速 · 观察后瞬身"
                : "声纹护甲破裂 · 攻击节奏加快";
            BurstSoulParticles(CombatTargetPosition + Vector3.up * 1.1f);
            EnemyMusicImpact3D.Create(CombatTargetPosition + Vector3.up * 0.45f,
                chapterOneCombatPhase == 3 ? new Color(1f, 0.10f, 0.22f) : new Color(1f, 0.58f, 0.16f),
                chapterOneCombatPhase == 3 ? 6.2f : 5.0f, 0.92f, 32);
        }

        public void BeginCombatPhaseTransition(int phase, double restUntilDsp)
        {
            if (!chapterOneCombatActive || finishingStrike) return;
            chapterOneCombatPhase = Mathf.Clamp(phase, 1, 3);
            combatPhaseRestUntilDsp = Math.Max(AudioSettings.dspTime + CombatPhaseRestSeconds, restUntilDsp);
            if (chapterOneCombatPhase == 3 && chapterOnePhaseThreeMusic != null && musicSource != null)
            {
                // Phase three gets its own tense BGM. Start it on a scheduled DSP
                // boundary after the phase banner so the first cue is aligned to
                // the analyzed phase-three beat map instead of inheriting the old
                // song's clock.
                double phaseMusicStart = combatPhaseRestUntilDsp + .12d;
                musicSource.Stop();
                musicSource.clip = chapterOnePhaseThreeMusic;
                musicSource.loop = true;
                musicSource.pitch = 1f;
                musicSource.volume = .34f;
                musicSource.time = 0f;
                musicSource.PlayScheduled(phaseMusicStart);
                chapterOneBeatStart = phaseMusicStart;
                chapterOneCombatBpm = phaseThreeBattleTimeline != null && phaseThreeBattleTimeline.IsValid
                    ? phaseThreeBattleTimeline.Bpm : chapterOneCombatBpm;
                Debug.Log($"[MeixiAudio] Phase-three BGM scheduled at DSP {phaseMusicStart:F3}: {chapterOnePhaseThreeMusic.name}");
            }
            combatBeatPending = false;
            combatBeatSucceeded = false;
            threatDamageResolved = true;
            resolvedThreatId = activeThreatId;
            activeThreatId = 0;
            currentThreatType = -1;
            currentThreatResolveDsp = 0d;
            expectedCombatAction = CombatBeatAction.None;
            expectedCombatStepCount = 0;
            expectedCombatStepIndex = 0;
            bufferedCombatKey = CombatInputKey.None;
            bufferedCombatKeyDsp = 0d;
            lastCombatSequenceInputDsp = 0d;
            ResetDualHandCombatCue();
            meleeCounterThreatId = -1;
            meleeCounterUntilDsp = 0d;
            playerDamageImmuneUntilDsp = Math.Max(playerDamageImmuneUntilDsp, combatPhaseRestUntilDsp + .24d);
            TrackingAttackTelegraph3D.ClearAll();
            EnemyMusicAttackVfx3D.ClearAll();
            MeleeForbiddenRing3D.ClearAll(true);
            player?.ResetInputTransientState();

            ReportCombatPhase(chapterOneCombatPhase);
            string phaseName = chapterOneCombatPhase == 2 ? "逆流" : "回声暴雨";
            if (combatPhaseBannerRoutine != null) StopCoroutine(combatPhaseBannerRoutine);
            combatPhaseBannerRoutine = null;
            if (chapterOneCombatPhase == 3) ShowPhaseThreeGuide(phaseName);
            else combatPhaseBannerRoutine = StartCoroutine(ShowCombatPhaseBanner(chapterOneCombatPhase, phaseName));
            if (combatActionKey != null) combatActionKey.text = "—";
            if (combatActionName != null)
            {
                combatActionName.text = "节 拍 休 止 · 等 待 新 指 令";
                combatActionName.color = new Color(.62f, .90f, .94f);
            }
            Debug.Log($"[MeixiPhaseRest] phase={chapterOneCombatPhase} start={AudioSettings.dspTime:F3} " +
                      $"until={combatPhaseRestUntilDsp:F3}; old threat cancelled.");
        }

        private void ShowPhaseThreeGuide(string phaseName)
        {
            if (combatPhaseBanner == null || combatPhaseBannerGroup == null || combatPhaseBannerText == null) return;
            phaseThreeGuideOpen = true;
            phaseThreeGuidePreviousTimeScale = Time.timeScale > 0f ? Time.timeScale : 1f;
            phaseThreeGuidePreviousAudioPause = AudioListener.pause;
            Time.timeScale = 0f;
            AudioListener.pause = true;
            player?.ResetInputTransientState();

            combatPhaseBanner.SetActive(true);
            combatPhaseBanner.transform.SetAsLastSibling();
            combatPhaseBanner.transform.localScale = Vector3.one;
            combatPhaseBannerGroup.alpha = 1f;
            combatPhaseBannerGroup.blocksRaycasts = true;
            combatPhaseBannerText.color = new Color(1f, .36f, .28f);
            combatPhaseBannerText.text =
                $"第 三 阶 段 · {phaseName}\n" +
                "<size=20>左手 Q / E 维持声部　·　右手 J / K / SPACE 回应</size>\n" +
                "<size=16>ENTER / SPACE / E 或点击确认后继续</size>";
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            Debug.Log("[MeixiPhaseGuide] Phase three guide opened; waiting for manual confirmation.");
        }

        private void DismissPhaseThreeGuide()
        {
            if (!phaseThreeGuideOpen) return;
            phaseThreeGuideOpen = false;
            Time.timeScale = phaseThreeGuidePreviousTimeScale > 0f ? phaseThreeGuidePreviousTimeScale : 1f;
            AudioListener.pause = phaseThreeGuidePreviousAudioPause;
            if (combatPhaseBannerGroup != null)
            {
                combatPhaseBannerGroup.alpha = 0f;
                combatPhaseBannerGroup.blocksRaycasts = false;
            }
            if (combatPhaseBanner != null) combatPhaseBanner.SetActive(false);

            double safeResumeDsp = AudioSettings.dspTime + .95d;
            combatPhaseRestUntilDsp = safeResumeDsp;
            playerDamageImmuneUntilDsp = Math.Max(playerDamageImmuneUntilDsp, safeResumeDsp + .20d);
            creekSpirit?.ResumeAfterPhaseGuide(safeResumeDsp);
            lastDualHandHudPhase = -1;
            player?.ResetInputTransientState();
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            Debug.Log($"[MeixiPhaseGuide] Phase three guide confirmed; combat resumes at {safeResumeDsp:F3}.");
        }

        private IEnumerator ShowCombatPhaseBanner(int phase, string phaseName)
        {
            if (combatPhaseBanner == null || combatPhaseBannerGroup == null || combatPhaseBannerText == null) yield break;
            combatPhaseBanner.SetActive(true);
            combatPhaseBanner.transform.SetAsLastSibling();
            combatPhaseBannerText.text =
                $"第 二 阶 段 · {phaseName}\n<size=18>节 拍 休 止 · 新 指 令 即 将 出 现</size>";
            combatPhaseBannerText.color = new Color(1f, 0.68f, 0.20f);
            float startedAt = Time.unscaledTime;
            const float duration = 1.65f;
            while (Time.unscaledTime - startedAt < duration)
            {
                float age = Time.unscaledTime - startedAt;
                float enter = Mathf.Clamp01(age / 0.16f);
                float exit = 1f - Mathf.Clamp01((age - duration + 0.32f) / 0.32f);
                combatPhaseBannerGroup.alpha = Mathf.Min(enter, exit);
                combatPhaseBanner.transform.localScale = Vector3.one * Mathf.Lerp(1.12f, 1f, Mathf.SmoothStep(0f, 1f, enter));
                yield return null;
            }
            combatPhaseBannerGroup.alpha = 0f;
            combatPhaseBanner.SetActive(false);
            combatPhaseBannerRoutine = null;
        }

        private void PlayBlinkFx(Vector3 origin, Vector3 destination, bool safe)
        {
            PlayerCombatFlourish3D.CreateBlink(origin, destination, safe);
            Vector3 midpoint = Vector3.Lerp(origin, destination, 0.5f) + Vector3.up * 0.9f;
            Color tint = safe ? new Color(0.30f, 1f, 0.86f, 0.92f) : new Color(1f, 0.20f, 0.22f, 0.88f);
            SpawnGeneratedSlash(midpoint, 2.75f, safe ? -26f : 32f, 0.34f, tint);
            SpawnGroundSigil(origin + Vector3.up * 0.05f, 1.9f, 0.36f, tint);
            SpawnGroundSigil(destination + Vector3.up * 0.05f, 2.4f, 0.46f, tint);
            SpawnBeatShardBurst(destination + Vector3.up * 0.7f, safe ? 28 : 16, safe ? 4.8f : 2.8f, tint);
        }

        public void ReportEnemyTelegraph(int attackType)
        {
            if (!chapterOneCombatActive) return;
            if (attackType == 3)
            {
                rhythmJudge.color = new Color(1f, .46f, .14f);
                rhythmJudge.text = "橙红五线谱锁定长距离冲刺 · SPACE 侧向瞬身";
                return;
            }
            if (expectedCombatAction != CombatBeatAction.None)
            {
                rhythmJudge.color = CombatActionColor(expectedCombatAction);
                rhythmJudge.text = $"敌人锁定鼓点 · {CombatActionKey(expectedCombatAction)} · {CombatActionName(expectedCombatAction)}";
                return;
            }
            rhythmJudge.color = new Color(1f, 0.24f, 0.20f);
            rhythmJudge.text = attackType switch
            {
                0 => "朱红高抛音符追踪落点 · SPACE 瞬身脱离",
                1 => "溪蓝低音冲击波对准你 · SPACE 向侧面瞬身",
                3 => "橙红五线谱正在锁定冲刺路径 · SPACE 侧向瞬身",
                _ => "五声音符群正在合围 · K 在节拍点远程破谱"
            };
        }

        private void FailCombatBeat(string reason)
        {
            if (!combatBeatPending || finishingStrike || CombatPhaseResting) return;
            combatBeatPending = false;
            combatBeatSucceeded = false;
            bufferedCombatKey = CombatInputKey.None;
            expectedCombatStepIndex = 0;
            lastCombatSequenceInputDsp = 0d;
            leftHandHolding = false;
            threatDamageResolved = true;
            resolvedThreatId = activeThreatId;
            chapterOneCombatCombo = 0;
            chapterOneCombatMisses++;
            rhythmJudge.text = reason + " · 回响力受损";
            rhythmJudge.color = new Color(1f, 0.30f, 0.28f);
            if (combatActionKey != null) combatActionKey.text = "×";
            TrackingAttackTelegraph3D.ClearAll();
            EnemyMusicAttackVfx3D.ShatterAllAt(CombatTargetPosition + Vector3.up * 0.9f,
                new Color(0.82f, 0.16f, 1f));
            MeleeForbiddenRing3D.ClearAll(false);
            DamagePlayer();
            player?.TriggerHitReaction(creekSpirit != null ? creekSpirit.transform.position : PlayerPosition + Vector3.forward);
            orbitCamera?.TriggerRhythmMiss();
            SpawnMissBeatVfx(PlayerPosition + Vector3.up * 0.9f);
            if (!defeatOpen) creekSpirit?.RepositionAfterPlayerMistake(PlayerPosition);
            Debug.Log($"[MeixiRhythm] Miss {reason} action={expectedCombatAction} health={health} now={AudioSettings.dspTime:F3}");
        }

        private static string CombatActionKey(CombatBeatAction action) => action switch
        {
            CombatBeatAction.AttackOne => "J",
            CombatBeatAction.AttackTwo => "K",
            CombatBeatAction.Dodge => "SPACE",
            CombatBeatAction.JKWave => "J + K",
            CombatBeatAction.JDodgeStrike => "J + SPACE",
            CombatBeatAction.KDodgeTripleArc => "K + SPACE",
            CombatBeatAction.TripleJ => "J · J · J",
            CombatBeatAction.JKRiff => "J · K · J",
            CombatBeatAction.KJKRiff => "K · J · K",
            _ => "·"
        };

        private static string CombatActionName(CombatBeatAction action) => action switch
        {
            CombatBeatAction.AttackOne => "攻 击 一 · 破 招",
            CombatBeatAction.AttackTwo => "攻 击 二 · 断 流",
            CombatBeatAction.Dodge => "踏 音 闪 避",
            CombatBeatAction.JKWave => "双键 · 大型声浪",
            CombatBeatAction.JDodgeStrike => "瞬身 · 强力近战",
            CombatBeatAction.KDodgeTripleArc => "三重 · 抛物线音符",
            CombatBeatAction.TripleJ => "三连击 · 鼓点追击",
            CombatBeatAction.JKRiff => "多连击 · J K J",
            CombatBeatAction.KJKRiff => "多连击 · K J K",
            _ => "聆 听 敌 人"
        };

        private static Color CombatActionColor(CombatBeatAction action) => action switch
        {
            CombatBeatAction.AttackOne => new Color(0.26f, 0.90f, 1f),
            CombatBeatAction.AttackTwo => new Color(1f, 0.48f, 0.22f),
            CombatBeatAction.Dodge => new Color(0.42f, 1f, 0.70f),
            CombatBeatAction.JKWave => new Color(1f, 0.64f, 0.16f),
            CombatBeatAction.JDodgeStrike => new Color(0.58f, 0.92f, 1f),
            CombatBeatAction.KDodgeTripleArc => new Color(0.82f, 0.38f, 1f),
            CombatBeatAction.TripleJ => new Color(1f, 0.30f, 0.22f),
            CombatBeatAction.JKRiff => new Color(1f, 0.72f, 0.20f),
            CombatBeatAction.KJKRiff => new Color(0.76f, 0.42f, 1f),
            _ => new Color(0.52f, 0.82f, 0.92f)
        };

        public void ReportEnemyDodge()
        {
            rhythmJudge.text = "溪灵错拍侧闪 · 等待反击窗口";
            rhythmJudge.color = new Color(0.32f, 0.86f, 1f);
            ShowToast("攻 击 抢 拍 · 溪 灵 侧 闪", new Color(0.38f, 0.84f, 1f));
        }

        public void ReportPerfectDodge(float counterDeadline)
        {
            rhythmJudge.text = "完 美 瞬 身 · J 反 击 窗 口";
            rhythmJudge.color = new Color(1f, 0.82f, 0.28f);
            ShowToast("◆ 完 美 瞬 身 · 立 即 按 J 反 击 ◆", new Color(1f, 0.78f, 0.24f));
            orbitCamera?.TriggerCombatImpact(true);
            audioSource.PlayOneShot(highTone, 0.85f);
        }

        public bool TryDamagePlayerFromEnemy(Vector3 origin, Vector3 forward, float range, float minimumDot)
        {
            if (!chapterOneCombatActive || playerTransform == null) return false;
            // 第一章节拍战由指定动作状态机统一判伤，避免物理碰撞与漏拍同时重复扣血。
            if (combatBeatPending || combatBeatSucceeded || finishingStrike ||
                dodgedThreatId == activeThreatId || counteredThreatId == activeThreatId || resolvedThreatId == activeThreatId ||
                AudioSettings.dspTime < playerDamageImmuneUntilDsp) return false;
            Vector3 delta = playerTransform.position + Vector3.up - origin;
            if (delta.magnitude > range) return false;
            if (delta.sqrMagnitude > 0.001f && Vector3.Dot(forward.normalized, delta.normalized) < minimumDot) return false;
            if (PlayerIsDodging) return false;

            DamagePlayer();
            player?.TriggerHitReaction(origin);
            orbitCamera?.TriggerCombatImpact(false);
            rhythmJudge.text = "失 拍 · 溪灵命中";
            rhythmJudge.color = new Color(1f, 0.36f, 0.30f);
            return true;
        }

        public void ReportPlayerCombatHit(int damage, bool counter, int remaining, int maximum)
        {
            rhythmJudge.text = counter ? "节 拍 强 化 · 声纹破裂" : "声 纹 命 中";
            rhythmJudge.color = counter ? new Color(1f, 0.80f, 0.24f) : new Color(0.28f, 0.94f, 1f);
            ShowToast(counter ? "◆ 节 拍 强 化 ◆" : "声 纹 命 中", rhythmJudge.color);
            orbitCamera?.TriggerCombatImpact(counter);
        }

        public void ChapterOneEnemyDefeated(Vector3 position)
        {
            if (!chapterOneCombatActive) return;
            chapterOneCombatActive = false;
            finishingStrike = false;
            combatBeatPending = false;
            Time.timeScale = 1f;
            musicSource.Stop();
            rhythmPanel.SetActive(false);
            orbitCamera?.EndCombatFocus();
            audioSource.PlayOneShot(successTone);
            PlayChapterOneExplorationMusic();
            SpawnChapterOneSoulEchoPickup(position);
            if (soulCountText != null) soulCountText.text = "收集魂音：0 / 3";
            if (storyProgressText != null) storyProgressText.text = "主线 03 / 04  ·  拾取魂音";
            if (objectiveText != null) objectiveText.text = "前往溪灵爆炸的位置，靠近并拾取留下的魂音";
            ShowToast("溪 灵 已 净 化 · 前 往 拾 取 魂 音", new Color(.42f, 1f, .78f));
        }

        private void SpawnChapterOneSoulEchoPickup(Vector3 position)
        {
            if (chapterOneSoulPickup != null) Destroy(chapterOneSoulPickup.gameObject);
            GameObject root = new("第一章_掉落魂音");
            root.transform.position = position + Vector3.up * .88f;

            GameObject core = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            core.name = "魂音核心";
            core.transform.SetParent(root.transform, false);
            core.transform.localScale = Vector3.one * .34f;
            Destroy(core.GetComponent<Collider>());
            Renderer coreRenderer = core.GetComponent<Renderer>();
            coreRenderer.sharedMaterial = new Material(gold);
            if (coreRenderer.sharedMaterial.HasProperty("_EmissionColor"))
            {
                coreRenderer.sharedMaterial.SetColor("_EmissionColor", new Color(1f, .42f, .05f) * 2.8f);
                coreRenderer.sharedMaterial.EnableKeyword("_EMISSION");
            }

            GameObject ringObject = new("魂音环");
            ringObject.transform.SetParent(root.transform, false);
            LineRenderer ring = ringObject.AddComponent<LineRenderer>();
            ring.useWorldSpace = false;
            ring.loop = true;
            ring.positionCount = 48;
            ring.widthMultiplier = .035f;
            ring.sharedMaterial = new Material(cyan);
            ring.startColor = new Color(.28f, 1f, .92f, .95f);
            ring.endColor = new Color(1f, .65f, .18f, .95f);
            for (int i = 0; i < ring.positionCount; i++)
            {
                float angle = i / (float)ring.positionCount * Mathf.PI * 2f;
                ring.SetPosition(i, new Vector3(Mathf.Cos(angle) * .46f, Mathf.Sin(angle) * .46f, 0f));
            }

            Light glow = new GameObject("魂音拾取光").AddComponent<Light>();
            glow.transform.SetParent(root.transform, false);
            glow.type = LightType.Point;
            glow.color = new Color(1f, .58f, .18f);
            glow.range = 4.2f;
            glow.intensity = 2.4f;
            glow.shadows = LightShadows.None;

            chapterOneSoulPickup = root.AddComponent<ChapterOneSoulEchoPickup>();
            chapterOneSoulPickup.Configure(this, root.transform.position);
        }

        public void CollectChapterOneSoulEcho(ChapterOneSoulEchoPickup pickup)
        {
            if (chapterOneSoulCollected || pickup == null || pickup != chapterOneSoulPickup) return;
            chapterOneSoulCollected = true;
            Vector3 pickupPosition = pickup.transform.position;
            chapterOneSoulPickup = null;
            Destroy(pickup.gameObject);
            pendingNode = null;
            completedMemories = Mathf.Max(1, completedMemories);
            if (soulOrbImages.Count > 0) soulOrbImages[0].color = new Color(1f, .58f, .16f, 1f);
            health = Mathf.Min(5, health + 2);
            UpdateHealth();
            if (soulCountText != null) soulCountText.text = "收集魂音：1 / 3";
            if (storyProgressText != null) storyProgressText.text = "主线 04 / 04  ·  魂音归位";
            if (objectiveText != null) objectiveText.text = "魂音已收集，回响力恢复 2 点";
            respawnPoint = playerTransform.position + Vector3.up * .3f;
            BurstSoulParticles(pickupPosition);
            audioSource.PlayOneShot(successTone, 1f);
            player?.TriggerVictory();
            ShowToast("收 集 魂 音：1 / 3  ·  回 响 力 +2", new Color(1f, .76f, .24f));
            complete = true;
            ShowDialogue(new[]
            {
                "系统｜收集魂音：1 / 3。回响力恢复 2 点。",
                "老村长｜多年没听见的泉声，终于又回来了。",
                "林默｜声音不是一串数据。它记得这里的人怎样生活。"
            }, ShowEnding);
        }

        private void StartRhythm(int chapter)
        {
            activeRhythmChapter = chapter;
            rhythmActive = true;
            rhythmPanel.SetActive(true);
            rhythmTitle.text = chapter == 3
                ? "失 声 陶 灵  ·  攻 防 共 鸣"
                : chapterNames[chapter] + "  ·  动 作 合 奏";
            rhythmJudge.text = "观 察 · 破 招 · 合 拍";
            rhythmHits = 0;
            rhythmMisses = 0;
            rhythmCombo = 0;
            rhythmBestCombo = 0;
            heavyCharging = false;
            rhythmNotes.Clear();
            foreach (var visual in rhythmVisuals) Destroy(visual);
            rhythmVisuals.Clear();
            float beat = 60f / bpms[chapter];
            float time = 1.75f;
            foreach (string token in patterns[chapter].Split('-'))
            {
                if (!TryParseRhythmAction(token, out RhythmActionType action)) continue;
                var note = new RhythmNote { Action = action, Time = time };
                note.Visual = CreateRhythmNote(action);
                rhythmNotes.Add(note);
                time += beat * (action == RhythmActionType.HeavyStrike ? 1.38f : 1f);
            }
            rhythmDuration = time + 1.25f;
            rhythmStart = AudioSettings.dspTime + 0.08d;
            musicSource.Stop();
            musicSource.clip = rhythmSong;
            musicSource.pitch = bpms[chapter] / 105f;
            musicSource.time = 0f;
            musicSource.PlayScheduled(rhythmStart);
            orbitCamera?.BeginRhythmFocus(chapter);
            UpdateRhythmScore();
        }

        private static bool TryParseRhythmAction(string token, out RhythmActionType action)
        {
            action = token switch
            {
                "L" => RhythmActionType.LightStrike,
                "H" => RhythmActionType.HeavyStrike,
                "D" => RhythmActionType.Dodge,
                "A" => RhythmActionType.LeftStrike,
                "R" => RhythmActionType.RightStrike,
                "B" => RhythmActionType.EchoBurst,
                _ => RhythmActionType.LightStrike
            };
            return token is "L" or "H" or "D" or "A" or "R" or "B";
        }

        private RectTransform CreateRhythmNote(RhythmActionType action)
        {
            var obj = CreatePanel("动作指令_" + ActionName(action), rhythmTrack, ActionColor(action), Vector2.zero, Vector2.zero);
            var rect = obj.GetComponent<RectTransform>();
            rect.sizeDelta = action == RhythmActionType.EchoBurst ? new Vector2(150f, 64f) : new Vector2(124f, 56f);
            PolishPanel(obj, new Color(1f, 0.88f, 0.46f, 0.72f));
            var text = CreateText("动作名称", obj.transform, ActionPrompt(action), 20, TextAnchor.MiddleCenter, Color.white);
            SetRect(text.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            DecorateText(text, Color.black, new Vector2(2f, -2f), false);
            obj.AddComponent<FancyTextPulse>().Amount = action == RhythmActionType.EchoBurst ? 0.10f : 0.055f;
            rhythmVisuals.Add(obj);
            return rect;
        }

        private void UpdateRhythm(Keyboard keyboard)
        {
            float elapsed = (float)(AudioSettings.dspTime - rhythmStart);
            const float approach = 2.15f;
            const float window = 0.29f;
            Color flash = rhythmFlash.color;
            flash.a = Mathf.MoveTowards(flash.a, 0f, Time.unscaledDeltaTime * 2.8f);
            rhythmFlash.color = flash;

            foreach (var note in rhythmNotes)
            {
                if (note.Resolved) continue;
                float until = note.Time - elapsed;
                bool visible = until <= approach + 0.12f;
                if (note.Visual.gameObject.activeSelf != visible) note.Visual.gameObject.SetActive(visible);
                float progress = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(1f - until / approach));
                Vector2 position = Vector2.Lerp(ActionStart(note.Action), new Vector2(0.5f, 0.5f), progress);
                note.Visual.anchorMin = position;
                note.Visual.anchorMax = position;
                note.Visual.anchoredPosition = Vector2.zero;
                note.Visual.localScale = Vector3.one * Mathf.Lerp(1.10f, 0.76f, progress);
                if (elapsed - note.Time > window)
                {
                    note.Resolved = true;
                    rhythmMisses++;
                    rhythmCombo = 0;
                    note.Visual.gameObject.SetActive(false);
                    ShowRhythmJudge(note.Action == RhythmActionType.Dodge ? "闪 避 失 败" : "漏 招 · 被 破", new Color(1f, 0.30f, 0.38f));
                    orbitCamera?.TriggerRhythmMiss();
                    UpdateRhythmScore();
                }
            }

            RhythmActionType? inputAction = DetectRhythmAction(keyboard);
            if (inputAction.HasValue) JudgeRhythm(inputAction.Value, elapsed, window);
            if (elapsed >= rhythmDuration) FinishRhythm();
        }

        private RhythmActionType? DetectRhythmAction(Keyboard keyboard)
        {
            // J/K are combat verbs, not exploration shortcuts. Keep the legacy
            // rhythm-input path behind the same authoritative chapter-one gate as
            // MeixiThirdPersonPlayer so no pre-battle press can charge, animate or
            // leak into a later encounter when a rhythm panel is still initialising.
            if (keyboard == null || !ChapterOneCombatActive)
            {
                heavyCharging = false;
                return null;
            }
            Mouse mouse = Mouse.current;
            bool lightDown = keyboard.jKey.wasPressedThisFrame || (mouse != null && mouse.leftButton.wasPressedThisFrame);
            bool lightHeld = keyboard.jKey.isPressed || (mouse != null && mouse.leftButton.isPressed);
            bool heavyDown = keyboard.kKey.wasPressedThisFrame || (mouse != null && mouse.rightButton.wasPressedThisFrame);
            bool heavyHeld = keyboard.kKey.isPressed || (mouse != null && mouse.rightButton.isPressed);
            bool heavyUp = keyboard.kKey.wasReleasedThisFrame || (mouse != null && mouse.rightButton.wasReleasedThisFrame);

            if ((lightDown && heavyHeld) || (heavyDown && lightHeld))
            {
                heavyCharging = false;
                return RhythmActionType.EchoBurst;
            }

            if (keyboard.spaceKey.wasPressedThisFrame) return RhythmActionType.Dodge;
            if (lightDown && (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed)) return RhythmActionType.LeftStrike;
            if (lightDown && (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed)) return RhythmActionType.RightStrike;
            if (lightDown) return RhythmActionType.LightStrike;

            if (heavyDown)
            {
                heavyCharging = true;
                heavyChargeStarted = Time.unscaledTime;
                ShowRhythmJudge("蓄 力 · 等 待 拍 点", new Color(1f, 0.65f, 0.22f));
            }
            if (heavyCharging && heavyUp)
            {
                heavyCharging = false;
                if (Time.unscaledTime - heavyChargeStarted >= 0.22f) return RhythmActionType.HeavyStrike;
                ShowRhythmJudge("蓄 力 不 足", new Color(1f, 0.38f, 0.30f));
            }
            return null;
        }

        private void JudgeRhythm(RhythmActionType action, float elapsed, float window)
        {
            RhythmNote bestMatch = null;
            RhythmNote nearest = null;
            float matchDelta = float.MaxValue;
            float nearestDelta = float.MaxValue;
            foreach (var note in rhythmNotes)
            {
                if (note.Resolved) continue;
                float delta = Mathf.Abs(note.Time - elapsed);
                if (delta < nearestDelta) { nearestDelta = delta; nearest = note; }
                if (note.Action == action && delta < matchDelta) { matchDelta = delta; bestMatch = note; }
            }

            if (bestMatch != null && matchDelta <= window)
            {
                bestMatch.Resolved = true;
                rhythmHits++;
                rhythmCombo++;
                rhythmBestCombo = Mathf.Max(rhythmBestCombo, rhythmCombo);
                bestMatch.Visual.gameObject.SetActive(false);
                bool perfect = matchDelta < 0.085f;
                ShowRhythmJudge((perfect ? "完 美 · " : "合 拍 · ") + ActionName(action), perfect ? new Color(1f, 0.78f, 0.25f) : new Color(0.30f, 1f, 0.70f));
                PlayRhythmActionFx(action, perfect);
                Color color = rhythmFlash.color;
                color.a = perfect ? 0.38f : 0.20f;
                rhythmFlash.color = color;
            }
            else
            {
                rhythmMisses++;
                rhythmCombo = 0;
                if (nearest != null && nearestDelta <= window * 1.25f)
                {
                    nearest.Resolved = true;
                    nearest.Visual.gameObject.SetActive(false);
                    ShowRhythmJudge("动 作 错 误 · 需 要 " + ActionName(nearest.Action), new Color(1f, 0.34f, 0.44f));
                }
                else
                {
                    ShowRhythmJudge("动 作 抢 拍", new Color(1f, 0.34f, 0.44f));
                }
                orbitCamera?.TriggerRhythmMiss();
            }
            UpdateRhythmScore();
        }

        private void PlayRhythmActionFx(RhythmActionType action, bool perfect)
        {
            bool bright = action is RhythmActionType.Dodge or RhythmActionType.RightStrike or RhythmActionType.EchoBurst;
            audioSource.PlayOneShot(bright ? highTone : lowTone, action == RhythmActionType.HeavyStrike ? 1f : 0.75f);
            if (playerTransform == null) return;

            var pulse = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            pulse.name = "动作共鸣_" + ActionName(action);
            pulse.transform.position = playerTransform.position + Vector3.up * 0.9f + playerTransform.forward * 0.7f;
            float scale = action == RhythmActionType.EchoBurst ? 0.75f : action == RhythmActionType.HeavyStrike ? 0.58f : 0.38f;
            pulse.transform.localScale = Vector3.one * (perfect ? scale * 1.25f : scale);
            pulse.GetComponent<Renderer>().material = new Material(bright ? cyan : gold);
            Destroy(pulse.GetComponent<Collider>());
            var fx = pulse.AddComponent<PulseFx3D>();
            fx.Lifetime = action == RhythmActionType.HeavyStrike ? 0.65f : 0.42f;
            fx.Growth = action == RhythmActionType.EchoBurst ? 7f : 4.5f;
            if (action == RhythmActionType.Dodge) SpawnDashTrail(playerTransform.position, -playerTransform.forward);
            if (action == RhythmActionType.EchoBurst) BurstSoulParticles(playerTransform.position + Vector3.up * 1.0f);
            if (action is RhythmActionType.LeftStrike or RhythmActionType.RightStrike) SpawnRhythmSlash(action == RhythmActionType.LeftStrike ? -1f : 1f);
            if (action == RhythmActionType.HeavyStrike) SpawnRhythmShockwave();
            orbitCamera?.TriggerRhythmCut((int)action, perfect);
        }

        private void SpawnRhythmSlash(float direction)
        {
            if (playerTransform == null) return;
            SpawnGeneratedSlash(playerTransform.position + Vector3.up * 1.05f + playerTransform.forward * 1.15f,
                2.0f, direction * 34f, 0.36f, new Color(0.44f, 0.94f, 1f, 0.92f));
        }

        private void SpawnRhythmShockwave()
        {
            if (playerTransform == null) return;
            SpawnGroundSigil(playerTransform.position + playerTransform.forward * 1.0f + Vector3.up * 0.045f,
                2.8f, 0.58f, new Color(0.58f, 0.94f, 1f, 0.90f));
        }

        private void PlayCombatResponseFx(CombatBeatAction action, bool perfect, bool final)
        {
            if (playerTransform == null) return;
            Vector3 target = creekSpirit != null
                ? Vector3.Lerp(playerTransform.position, creekSpirit.transform.position, 0.72f) + Vector3.up * 1.05f
                : playerTransform.position + playerTransform.forward * 1.6f + Vector3.up;
            Color tint = perfect ? Color.white : CombatActionColor(action);

            if (action == CombatBeatAction.AttackOne)
            {
                SpawnGeneratedSlash(target, final ? 5.6f : 2.45f, -28f, final ? 0.82f : 0.40f, tint);
            }
            else if (action == CombatBeatAction.AttackTwo)
            {
                SpawnGeneratedSlash(target, final ? 6.2f : 3.05f, 34f, final ? 0.90f : 0.48f, tint);
                SpawnGeneratedSlash(target + Vector3.up * 0.08f, final ? 4.8f : 2.35f, -42f, final ? 0.74f : 0.40f,
                    new Color(1f, 0.50f, 0.20f, 0.82f));
                SpawnGroundSigil(creekSpirit != null ? creekSpirit.transform.position + Vector3.up * 0.05f : target,
                    final ? 5.4f : 3.1f, final ? 1.0f : 0.58f, tint);
            }
            else if (action == CombatBeatAction.Dodge)
            {
                SpawnGroundSigil(playerTransform.position + Vector3.up * 0.05f, 2.6f, 0.48f, new Color(0.36f, 1f, 0.76f, 0.88f));
                SpawnDashTrail(playerTransform.position, -playerTransform.forward);
            }

            SpawnBeatShardBurst(target, perfect ? 34 : 18, perfect ? 4.8f : 3.2f, tint);
        }

        private void SpawnGeneratedSlash(Vector3 position, float size, float roll, float lifetime, Color tint)
        {
            if (finalSlashTexture == null) return;
            var plane = GameObject.CreatePrimitive(PrimitiveType.Quad);
            plane.name = "梅溪生成声纹斩击";
            plane.transform.position = position;
            Destroy(plane.GetComponent<Collider>());
            Material material = CreateTransparentVfxMaterial(finalSlashTexture, tint);
            plane.GetComponent<Renderer>().material = material;
            plane.AddComponent<GeneratedVfxPlane3D>().Configure(size, roll, lifetime, true, material);
        }

        private void SpawnGroundSigil(Vector3 position, float size, float lifetime, Color tint)
        {
            Texture2D texture = beatSigilSprite != null ? beatSigilSprite.texture : null;
            if (texture == null) return;
            var plane = GameObject.CreatePrimitive(PrimitiveType.Quad);
            plane.name = "梅溪魂音地面印记";
            plane.transform.position = position;
            plane.transform.rotation = Quaternion.Euler(-90f, 0f, 0f);
            Destroy(plane.GetComponent<Collider>());
            Material material = CreateTransparentVfxMaterial(texture, tint);
            plane.GetComponent<Renderer>().material = material;
            plane.AddComponent<GeneratedVfxPlane3D>().Configure(size, 0f, lifetime, false, material);
        }

        private Material CreateTransparentVfxMaterial(Texture2D texture, Color tint)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default");
            var material = new Material(shader) { name = "梅溪生成透明特效", color = tint };
            material.mainTexture = texture;
            if (material.HasProperty("_BaseMap")) material.SetTexture("_BaseMap", texture);
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", tint);
            if (material.HasProperty("_Surface")) material.SetFloat("_Surface", 1f);
            if (material.HasProperty("_SrcBlend")) material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            if (material.HasProperty("_DstBlend")) material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            if (material.HasProperty("_ZWrite")) material.SetFloat("_ZWrite", 0f);
            material.renderQueue = 3060;
            return material;
        }

        private void SpawnBeatShardBurst(Vector3 position, int count, float speed, Color tint)
        {
            var root = new GameObject("魂音碎片爆发");
            root.transform.position = position;
            var ps = root.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.loop = false;
            main.duration = 0.55f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.32f, 0.72f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(speed * 0.45f, speed);
            main.startSize = new ParticleSystem.MinMaxCurve(0.035f, 0.12f);
            main.startColor = new ParticleSystem.MinMaxGradient(tint, new Color(1f, 0.45f, 0.16f, tint.a));
            var emission = ps.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)count) });
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 42f;
            shape.radius = 0.16f;
            var renderer = ps.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Mesh;
            renderer.mesh = EnemyMusicVfxFactory3D.GetShardMesh();
            renderer.material = new Material(cyan);
            ps.Play();
            Destroy(root, 1.2f);
        }

        private static void ConfigureShardParticleRenderer(ParticleSystem particles, Material material)
        {
            if (particles == null) return;
            ParticleSystemRenderer renderer = particles.GetComponent<ParticleSystemRenderer>();
            if (renderer == null) return;
            renderer.renderMode = ParticleSystemRenderMode.Mesh;
            renderer.mesh = EnemyMusicVfxFactory3D.GetShardMesh();
            renderer.material = material;
        }


        private void SpawnMissBeatVfx(Vector3 position)
        {
            SpawnGroundSigil(new Vector3(position.x, 0.06f, position.z), 2.2f, 0.40f, new Color(1f, 0.18f, 0.20f, 0.72f));
            SpawnBeatShardBurst(position, 14, 2.4f, new Color(1f, 0.18f, 0.20f, 0.78f));
        }

        public void BeginFinalStrike(CombatBeatAction action, Vector3 targetPosition)
        {
            if (finishingStrike || !chapterOneCombatActive) return;
            finalStrikeRoutine = StartCoroutine(FinalStrikeSequence(action, targetPosition, gameplaySessionRevision));
        }

        private IEnumerator FinalStrikeSequence(CombatBeatAction action, Vector3 targetPosition, int sessionRevision)
        {
            finishingStrike = true;
            cinematicInputLockedUntil = Time.unscaledTime + 3.20f;
            combatBeatPending = false;
            meleeCounterThreatId = -1;
            meleeCounterUntilDsp = 0d;
            MeleeForbiddenRing3D.ClearAll(false);
            TrackingAttackTelegraph3D.ClearAll();
            EnemyMusicAttackVfx3D.ClearAll();
            if (combatActionKey != null) combatActionKey.text = "终";
            if (combatActionName != null)
            {
                combatActionName.text = "梅 溪 终 响";
                combatActionName.color = new Color(1f, 0.82f, 0.34f);
            }
            rhythmJudge.text = "终结拍 · 魂音汇流";
            rhythmJudge.color = new Color(1f, 0.82f, 0.34f);
            player?.TriggerFinalStrike(action);
            orbitCamera?.BeginFinisherFocus(creekSpirit != null ? creekSpirit.transform : null);
            FinisherCinematicOverlay3D.Begin(2.15f);
            PlayerCombatFlourish3D.CreateFinisherCharge(PlayerPosition, targetPosition, action);

            float previousTimeScale = Time.timeScale;
            float previousPitch = musicSource != null ? musicSource.pitch : 1f;
            finalStrikeRestoreTimeScale = previousTimeScale;
            finalStrikeRestorePitch = previousPitch;
            Time.timeScale = 0.38f;
            if (musicSource != null) musicSource.pitch = previousPitch * 0.68f;

            // Move by actual distance instead of teleporting or rotating through a
            // preset cut. The root faces one attack axis for the entire approach.
            float approachDeadline = Time.unscaledTime + .92f;
            while (Time.unscaledTime < approachDeadline && creekSpirit != null)
            {
                if (sessionRevision != gameplaySessionRevision) yield break;
                targetPosition = creekSpirit.transform.position;
                float remaining = player != null
                    ? player.AdvanceFinalStrikeTowards(targetPosition, 1.58f, 6.2f)
                    : Vector3.Distance(PlayerPosition, targetPosition);
                if (remaining <= 1.62f) break;
                yield return null;
            }
            player?.LockFinalStrikeFacing(targetPosition);
            yield return new WaitForSecondsRealtime(.06f);
            if (sessionRevision != gameplaySessionRevision) yield break;

            player?.TriggerFinalStrikeImpact(action);
            player?.LockFinalStrikeFacing(targetPosition);
            orbitCamera?.TriggerFinisherCut(0);
            Color impactColor = action == CombatBeatAction.AttackTwo
                ? new Color(0.16f, 1f, 0.92f)
                : new Color(1f, 0.66f, 0.16f);
            FinisherCinematicOverlay3D.Impact(impactColor);
            PlayerCombatFlourish3D.CreateFinisherImpact(PlayerPosition, targetPosition, action);
            SpawnGroundSigil(new Vector3(targetPosition.x, 0.06f, targetPosition.z), 5.8f, 1.10f, Color.white);
            SpawnBeatShardBurst(targetPosition + Vector3.up, 28, 5.6f, Color.white);
            audioSource.PlayOneShot(successTone, 1f);
            Time.timeScale = 0.10f;
            if (rhythmFinisherValidation && captureMode && !captureDone)
                captureAt = Time.unscaledTime + 0.10f;
            Debug.Log($"[MeixiRhythm] Final strike collision reached action={action}");
            yield return new WaitForSecondsRealtime(0.13f);
            if (sessionRevision != gameplaySessionRevision) yield break;

            Vector3 explosionPosition = creekSpirit != null
                ? creekSpirit.ExplodeForFinalStrike()
                : targetPosition;
            SpawnFinalEnemyExplosion(explosionPosition, impactColor);
            Time.timeScale = 0.24f;
            orbitCamera?.TriggerFinisherCut(1);
            SpawnGeneratedSlash(explosionPosition + Vector3.up * 1.05f, 7.2f, -36f, 0.92f,
                new Color(1f, 0.58f, 0.22f, 0.90f));
            yield return new WaitForSecondsRealtime(0.36f);
            if (sessionRevision != gameplaySessionRevision) yield break;

            Time.timeScale = previousTimeScale;
            if (musicSource != null) musicSource.pitch = previousPitch;
            player?.CompleteFinalStrikeAnimation();
            ChapterOneEnemyDefeated(explosionPosition);
            finalStrikeRoutine = null;
        }

        private void SpawnFinalEnemyExplosion(Vector3 position, Color accent)
        {
            Vector3 centre = position + Vector3.up * 1.05f;
            EnemyMusicImpact3D.Create(centre, Color.white, 7.2f, 1.05f, 64);
            SpawnGroundSigil(new Vector3(position.x, .06f, position.z), 6.8f, 1.20f, accent);
            SpawnBeatShardBurst(centre, 64, 7.4f, Color.white);
            BurstSoulParticles(centre);
        }

        private static string ActionName(RhythmActionType action) => action switch
        {
            RhythmActionType.LightStrike => "轻 击",
            RhythmActionType.HeavyStrike => "重 击",
            RhythmActionType.Dodge => "闪 避",
            RhythmActionType.LeftStrike => "左 突 击",
            RhythmActionType.RightStrike => "右 突 击",
            RhythmActionType.EchoBurst => "合 奏 爆 发",
            _ => "动 作"
        };

        private static string ActionPrompt(RhythmActionType action) => action switch
        {
            RhythmActionType.LightStrike => "轻击  J / 左键",
            RhythmActionType.HeavyStrike => "蓄力重击  K / 右键",
            RhythmActionType.Dodge => "闪避  SPACE",
            RhythmActionType.LeftStrike => "左突  A + J",
            RhythmActionType.RightStrike => "右突  D + J",
            RhythmActionType.EchoBurst => "合奏  J + K",
            _ => "动作"
        };

        private static Color ActionColor(RhythmActionType action) => action switch
        {
            RhythmActionType.LightStrike => new Color(0.08f, 0.57f, 0.82f, 0.98f),
            RhythmActionType.HeavyStrike => new Color(0.88f, 0.25f, 0.07f, 0.98f),
            RhythmActionType.Dodge => new Color(0.12f, 0.68f, 0.48f, 0.98f),
            RhythmActionType.LeftStrike => new Color(0.39f, 0.28f, 0.76f, 0.98f),
            RhythmActionType.RightStrike => new Color(0.72f, 0.24f, 0.60f, 0.98f),
            RhythmActionType.EchoBurst => new Color(0.94f, 0.57f, 0.08f, 1f),
            _ => Color.white
        };

        private static Vector2 ActionStart(RhythmActionType action) => action switch
        {
            RhythmActionType.LightStrike => new Vector2(0.06f, 0.18f),
            RhythmActionType.HeavyStrike => new Vector2(0.94f, 0.18f),
            RhythmActionType.Dodge => new Vector2(0.50f, 0.94f),
            RhythmActionType.LeftStrike => new Vector2(0.06f, 0.82f),
            RhythmActionType.RightStrike => new Vector2(0.94f, 0.82f),
            RhythmActionType.EchoBurst => new Vector2(0.50f, 0.06f),
            _ => new Vector2(0.5f, 0.5f)
        };

        private void ShowRhythmJudge(string text, Color color)
        {
            rhythmJudge.text = text;
            rhythmJudge.color = color;
        }

        private void UpdateRhythmScore()
        {
            rhythmScore.text = $"命中  {rhythmHits}     漏招  {rhythmMisses}     连击  {rhythmCombo}  /  最佳 {rhythmBestCombo}";
        }

        private void FinishRhythm()
        {
            rhythmActive = false;
            rhythmPanel.SetActive(false);
            musicSource.Stop();
            orbitCamera?.EndRhythmFocus();
            float accuracy = (float)rhythmHits / Mathf.Max(1, rhythmNotes.Count);
            if (accuracy < 0.55f)
            {
                ShowDialogue(new[] { "系统｜魂音仍不稳定。放慢呼吸，再听一次村庄的节拍。" }, () => StartRhythm(completedMemories));
                return;
            }
            audioSource.PlayOneShot(successTone);
            pendingNode?.Complete();
            pendingNode = null;
            if (completedMemories < soulOrbImages.Count) soulOrbImages[completedMemories].color = new Color(1f, 0.58f, 0.16f, 1f);
            completedMemories++;
            soulCountText.text = $"收集魂音：{completedMemories} / 3";
            respawnPoint = playerTransform.position + Vector3.up * 0.3f;
            if (completedMemories >= 1)
            {
                complete = true;
                ShowDialogue(new[]
                {
                    "系统｜溪流魂音同步完成。第一章声纹结构恢复。",
                    "林默｜声音不是数据。它保存着人与土地之间的连接。"
                }, ShowEnding);
            }
            else
            {
                ShowToast($"魂 音 归 位    {completedMemories} / 3", new Color(0.38f, 1f, 0.70f));
                SetObjective(completedMemories);
            }
        }

        private void ShowEnding()
        {
            if (sceneTransitionActive) return;
            BeginScreenTransition("溪 声 归 来", PresentEnding, .42f, .14f, .72f);
        }

        private void PresentEnding()
        {
            if (!IsAutomatedPresentationRun()) MeixiLocalProgress.MarkChapterOneCompleted();
            complete = true;
            chapterOneCombatActive = false;
            rhythmPanel.SetActive(false);
            hudPanel.SetActive(false);
            endingPanel.SetActive(true);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        public void PerformSoundPulse(Vector3 origin, Vector3 forward, bool high)
        {
            audioSource.PlayOneShot(high ? highTone : lowTone);
            if (chapterOneCombatActive)
            {
                // 合拍命中由 TrySubmitCombatAction 生成完整特效；抢拍/错拍仍保留较弱的动作拖尾。
                if (!combatBeatSucceeded)
                    SpawnGeneratedSlash(origin + forward * 0.8f, high ? 1.8f : 1.25f, high ? 22f : -28f, 0.28f, new Color(0.30f, 0.74f, 0.92f, 0.58f));
                return;
            }

            bool onBeat = IsOnBeat();
            SpawnGeneratedSlash(origin + forward * 0.8f, onBeat ? 1.65f : 1.15f, high ? 26f : -24f, 0.32f, onBeat ? Color.white : new Color(0.28f, 0.82f, 1f, 0.68f));
            foreach (var enemy in FindObjectsByType<NoiseBeast3D>())
            {
                Vector3 delta = enemy.transform.position - origin;
                float distance = delta.magnitude;
                if (distance < (onBeat ? 5.0f : 3.2f) && Vector3.Dot(forward.normalized, delta.normalized) > 0.25f) enemy.Hit(onBeat);
            }
            ShowToast(onBeat ? "◆  共 鸣 攻 击  ◆" : "声 纹 脉 冲", onBeat ? new Color(1f, 0.76f, 0.24f) : new Color(0.40f, 0.90f, 1f));
        }

        private bool IsOnBeat()
        {
            const float beat = 0.6f;
            float phase = Time.time % beat;
            return phase < 0.14f || phase > beat - 0.14f;
        }

        public void SpawnDashTrail(Vector3 position, Vector3 direction)
        {
            for (int i = 0; i < 5; i++)
            {
                var mote = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                mote.name = "踏音流光";
                mote.transform.position = position - direction * (i * 0.38f) + Vector3.up * (0.2f + i * 0.18f);
                mote.transform.localScale = Vector3.one * (0.16f - i * 0.018f);
                mote.GetComponent<Renderer>().material = new Material(cyan);
                Destroy(mote.GetComponent<Collider>());
                var fx = mote.AddComponent<PulseFx3D>();
                fx.Lifetime = 0.35f;
                fx.Growth = 1.2f;
            }
        }

        public void BurstSoulParticles(Vector3 position)
        {
            var root = new GameObject("魂音绽放");
            root.transform.position = position;
            var ps = root.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.loop = false;
            main.duration = 1f;
            main.startLifetime = 1.6f;
            main.startSpeed = new ParticleSystem.MinMaxCurve(2f, 5f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.22f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.42f, 0.08f), new Color(0.10f, 0.88f, 1f));
            var emission = ps.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)80) });
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.45f;
            ConfigureShardParticleRenderer(ps, gold);
            ps.Play();
            Destroy(root, 3f);
        }

        public void EnemyDefeated(Vector3 position)
        {
            defeatedEnemies++;
            BurstSoulParticles(position);
            if (completedMemories >= 3) SetObjective(3);
            ShowToast($"噪 音 净 化    {defeatedEnemies} / 3", new Color(0.42f, 1f, 0.72f));
        }

        public void DamagePlayer()
        {
            if (defeatOpen) return;
            if (CombatPhaseResting)
            {
                Debug.Log($"[MeixiDamage] Ignored during phase rest until={combatPhaseRestUntilDsp:F3}.");
                return;
            }
            if (chapterOneCombatActive && AudioSettings.dspTime < playerDamageImmuneUntilDsp)
            {
                Debug.Log($"[MeixiDamage] Ignored delayed damage after dodge id={dodgedThreatId} " +
                          $"now={AudioSettings.dspTime:F3} immuneUntil={playerDamageImmuneUntilDsp:F3} health={health}");
                return;
            }
            health--;
            Debug.Log($"[MeixiDamage] Applied health={health} threat={activeThreatId} type={currentThreatType}");
            UpdateHealth();
            CombatDamageVignette3D.Flash();
            ShowToast("回 响 力 受 到 噪 音 侵 蚀", new Color(1f, 0.27f, 0.38f));
            if (health <= 0)
            {
                health = 0;
                UpdateHealth();
                ShowDefeatPanel();
            }
        }

        private void UpdateHealth()
        {
            int clampedHealth = Mathf.Clamp(health, 0, 5);
            int delta = clampedHealth - displayedHealth;
            displayedHealth = clampedHealth;
            if (healthText != null) healthText.text = $"回响力  {clampedHealth} / 5";
            Color baseBarColor = clampedHealth <= 1
                ? new Color(1f, .16f, .20f, .98f)
                : clampedHealth <= 3 ? new Color(1f, .58f, .16f, .98f) : new Color(.18f, .92f, 1f, .98f);
            for (int i = 0; i < healthBarSegments.Length; i++)
            {
                Image segment = healthBarSegments[i];
                if (segment == null) continue;
                segment.gameObject.SetActive(i < clampedHealth);
                segment.color = baseBarColor;
            }
            if (delta != 0 && healthFxGroup != null)
            {
                if (healthFxRoutine != null) StopCoroutine(healthFxRoutine);
                Debug.Log($"[MeixiHealthHud] delta={delta} health={clampedHealth}; pulse started.");
                healthFxRoutine = StartCoroutine(PlayHealthHudFx(delta));
            }
        }

        private IEnumerator PlayHealthHudFx(int delta)
        {
            bool restored = delta > 0;
            Color pulseColor = restored ? new Color(.18f, 1f, .58f, .42f) : new Color(1f, .10f, .16f, .48f);
            Color baseBarColor = displayedHealth <= 1
                ? new Color(1f, .16f, .20f, .98f)
                : displayedHealth <= 3 ? new Color(1f, .58f, .16f, .98f) : new Color(.18f, .92f, 1f, .98f);
            Color baseLabelColor = new(1f, .58f, .34f);
            Color flashBarColor = restored ? new Color(.20f, 1f, .52f, 1f) : new Color(1f, .08f, .12f, 1f);
            if (healthBarGlow != null) healthBarGlow.color = pulseColor;
            if (healthDeltaText != null)
            {
                healthDeltaText.text = restored ? $"+{delta} 回响" : $"{delta} 回响";
                healthDeltaText.color = restored ? new Color(.38f, 1f, .68f) : new Color(1f, .30f, .30f);
                healthDeltaText.gameObject.SetActive(true);
                healthDeltaText.transform.SetAsLastSibling();
            }
            if (healthFxGroup != null) healthFxGroup.alpha = 1f;

            float start = Time.unscaledTime;
            const float duration = 1.05f;
            while (Time.unscaledTime - start < duration)
            {
                float age = Time.unscaledTime - start;
                float enter = Mathf.Clamp01(age / .10f);
                float exit = 1f - Mathf.Clamp01((age - .62f) / .43f);
                float pulse = 1f + Mathf.Sin(age * 30f) * .024f * exit;
                float flash = (.40f + Mathf.Sin(age * 31f) * .30f) * exit;
                if (healthFxGroup != null) healthFxGroup.alpha = Mathf.Min(enter, exit);
                if (healthCardRect != null) healthCardRect.localScale = Vector3.one * pulse;
                for (int i = 0; i < healthBarSegments.Length; i++)
                    if (healthBarSegments[i] != null && healthBarSegments[i].gameObject.activeSelf)
                        healthBarSegments[i].color = Color.Lerp(baseBarColor, flashBarColor, flash);
                if (healthText != null) healthText.color = Color.Lerp(baseLabelColor, flashBarColor, flash * .72f);
                if (healthDeltaText != null)
                {
                    Color deltaColor = restored ? new Color(.38f, 1f, .68f) : new Color(1f, .30f, .30f);
                    deltaColor.a = Mathf.Min(enter, exit);
                    healthDeltaText.color = deltaColor;
                    healthDeltaText.rectTransform.localScale = Vector3.one * (1f + enter * exit * .10f);
                }
                yield return null;
            }
            if (healthFxGroup != null) healthFxGroup.alpha = 0f;
            if (healthCardRect != null) healthCardRect.localScale = Vector3.one;
            for (int i = 0; i < healthBarSegments.Length; i++)
                if (healthBarSegments[i] != null && healthBarSegments[i].gameObject.activeSelf)
                    healthBarSegments[i].color = baseBarColor;
            if (healthText != null) healthText.color = baseLabelColor;
            if (healthDeltaText != null)
            {
                healthDeltaText.rectTransform.localScale = Vector3.one;
                healthDeltaText.gameObject.SetActive(false);
            }
            healthFxRoutine = null;
        }

        public void RespawnPlayer()
        {
            var controller = playerTransform.GetComponent<CharacterController>();
            controller.enabled = false;
            playerTransform.position = respawnPoint;
            controller.enabled = true;
            ShowToast("回 到 最 近 的 魂 音 记 忆", new Color(0.42f, 0.84f, 1f));
        }

        public void PlayJumpSound()
        {
            audioSource.PlayOneShot(highTone, 0.22f);
        }

        public void ShowToast(string message, Color color)
        {
            if (toastText == null || toastPanel == null) return;
            toastText.text = message;
            toastText.color = Color.Lerp(Color.white, color, 0.38f);
            if (toastBannerFx != null) toastBannerFx.Play(color);
            else toastPanel.SetActive(true);
            toastPanel.transform.SetAsLastSibling();
            toastUntil = Time.unscaledTime + 1.85f;
        }
    }

    public sealed class ChapterOneSoulEchoPickup : MonoBehaviour
    {
        private MeixiGame3D game;
        private Vector3 basePosition;
        private bool collected;

        public void Configure(MeixiGame3D owner, Vector3 position)
        {
            game = owner;
            basePosition = position;
        }

        private void Update()
        {
            if (game == null || collected) return;
            float wave = Mathf.Sin(Time.unscaledTime * 2.4f) * .12f;
            transform.position = basePosition + Vector3.up * wave;
            transform.Rotate(0f, 58f * Time.unscaledDeltaTime, 18f * Time.unscaledDeltaTime, Space.Self);
            Vector3 delta = game.PlayerPosition - transform.position;
            delta.y *= .45f;
            if (delta.sqrMagnitude > 1.55f * 1.55f) return;
            collected = true;
            game.CollectChapterOneSoulEcho(this);
        }
    }
}
