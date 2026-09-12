using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace MeixiEcho
{
    /// <summary>
    /// Complete chapter-three loop: first-person investigation, a thirteen-minute day clock,
    /// sanity, repeatable practice tasks, persistent evidence, branching epilogue and
    /// the final-book gate. Geometry remains authored by ChapterThreeHomestayRuntime.
    /// </summary>
    public sealed partial class ChapterThreeGameDirector : MonoBehaviour
    {
        private static readonly int SkyTintId = Shader.PropertyToID("_SkyTint");
        private static readonly int GroundColorId = Shader.PropertyToID("_GroundColor");
        private static readonly int ExposureId = Shader.PropertyToID("_Exposure");
        private const int AllTruthMask = (1 << ChapterThreeRules.TruthClueCount) - 1;
        private const int AllHousingMask = (1 << ChapterThreeRules.HousingClueCount) - 1;
        // A comfortable first-person reach for furniture and wall fixtures. The
        // line-of-sight surface check below still prevents using props through a
        // wall, while avoiding the old pixel-perfect aim requirement.
        private const float InteractionDistance = 4.05f;
        private const float MealDuration = 8.33f;

        private static readonly string[] TruthTitles =
        {
            "2025 · 音愈乡野实践册", "工作站未寄出的评语", "AI音乐获奖剪报", "古建修复资金会议纪要",
            "被涂改的传统乐谱", "林氏祠堂火场残页", "石婶的诊疗回执", "旧录音机里的证词"
        };

        private static readonly string[] TruthBodies =
        {
            "龚炜杰带领黄授函、陈欣彤、庄瑜璇、罗浩鑫、罗成炜、翁若萱、连响洲来到闽清。记录很薄，最后一页却写着：项目没有结束，只是被下一支队伍接走。",
            "站长裴锦泽认为第一年的成果粗糙，却看见了声音档案的潜力。他在页边写下：真正的成果要回到村民身上，而不是只停在奖状里。",
            "2026年的AI音乐迅速走红。剪报里，荣誉占满整版；关于闽清后来发生了什么，只剩一行模糊的小字。",
            "守旧派最终争取到资金，用于修复古建筑与打造传统乐器。另一栏被红笔反复圈住：AI改革小组拒绝接受表决。",
            "曲谱被两种笔迹撕扯。一边写着‘旧声也该被听见’，另一边写着‘只有更新才有未来’。争执早已越过音乐本身。",
            "卷纸边缘有灼烧和雨水痕迹：林氏祠堂及相邻古厝在改造前夜起火，现场出现人员伤亡。纵火者借改革之名，把分歧变成了灾难。",
            "石婶是火灾中唯一幸存的安保人员。烟尘永久损伤了她的发声能力；她没有失去记忆，只是再也无法亲口讲出那一夜。",
            "噪声后传来一句残缺的证词：获奖后的队长只关心下一份荣誉，村里的裂痕却无人收拾。八页证据终于指向同一场被掩埋的火。"
        };

        private static readonly string[] HousingTitles =
        {
            "住房卡 01", "住房卡 02", "住房卡 03", "住房卡 04", "住房卡 05"
        };

        private static readonly string[] HousingBodies =
        {
            "李俊泽 + 刘海云 · 201房\n李俊泽负责田野录音，习惯把备用电池整齐放在床头柜右侧；刘海云记录村民口述，雨天会把窗帘留一道缝。",
            "赵津浩 + 张旭 · 202房\n赵津浩带着旧相机，胶卷盒上写着‘返乡’；张旭擅长修琴，衣柜内侧留有一小片松香粉末。",
            "何欣雯 + 李思彤 · 203房\n何欣雯的笔记按颜色归档，最常用青绿色；李思彤每天清晨练习单音，床头柜里藏着一枚磨损的拨片。",
            "李芷晴 + 曾仪敏 · 204房\n李芷晴收集民宿印章，曾仪敏负责经费登记。两人的入住卡背面都写着同一句：先听完，再判断。",
            "唐铭池 · 205房（单人）\n个人资料只有一张模糊证件照和一支未拆封录音笔。十人名单只有九人的床位；龚炜杰的名字存在于成果署名，却从未出现在入住记录中。"
        };

        private static readonly string[] TypingPassages =
        {
            "清晨我们沿梅溪采集流水与鸟鸣，逐条标注时间和地点。回到民宿后核对住房记录，并将原始录音另存备份。",
            "实践材料应说明采集对象与声音来源，不能用获奖介绍替代田野记录。整理前征求讲述者意见，保留原话与停顿。",
            "今天先听完村民的讲述，再校对录音中的人名与地名。无法确认的内容暂记为待核实，不把推测写成事实。",
            "旧曲的价值不只在旋律，也在演奏者留下的记忆。我们记录乐器的制作方法，再请村民确认整理后的文字。",
            "数字化档案应当能够回到真实的村庄。整理完成后保存原始文件与修改记录，并向提供材料的人交还一份副本。",
            "傍晚在民宿复核当天的实践任务，将入住卡与访谈笔记逐项比对。发现时间不一致时回查原件，不擅自补写结论。"
        };

        private static readonly string[] CleaningPatterns =
        {
            "ADDAADADDA", "AADDADDAAD", "DADDAADADA", "ADAADDADAD", "DDAADADAAD"
        };

        private static readonly string[] CleaningVariantNames =
        {
            "床品除尘", "窗台擦拭", "浴室清洁", "地毯整理", "客用品归位"
        };

        private static readonly string[] CarryVariantNames =
        {
            "易碎器材", "录音设备", "村民行李", "展演道具", "档案器材箱"
        };

        private static readonly string[] StrokeVariantNames =
        {
            "民宿屋檐", "梅溪水纹", "山叶脉络", "纸灯笼", "旧曲音符"
        };

        private static readonly Vector2[][] StrokeNodeLayouts =
        {
            new[] { new Vector2(.12f,.18f), new Vector2(.12f,.62f), new Vector2(.50f,.88f), new Vector2(.88f,.62f), new Vector2(.88f,.18f), new Vector2(.62f,.18f), new Vector2(.62f,.48f), new Vector2(.38f,.48f), new Vector2(.38f,.18f) },
            new[] { new Vector2(.08f,.40f), new Vector2(.22f,.68f), new Vector2(.38f,.28f), new Vector2(.54f,.70f), new Vector2(.70f,.30f), new Vector2(.86f,.62f), new Vector2(.94f,.42f) },
            new[] { new Vector2(.14f,.16f), new Vector2(.28f,.38f), new Vector2(.42f,.62f), new Vector2(.55f,.86f), new Vector2(.62f,.56f), new Vector2(.76f,.72f), new Vector2(.68f,.38f), new Vector2(.86f,.24f) },
            new[] { new Vector2(.30f,.18f), new Vector2(.24f,.36f), new Vector2(.24f,.72f), new Vector2(.38f,.88f), new Vector2(.62f,.88f), new Vector2(.76f,.72f), new Vector2(.76f,.36f), new Vector2(.70f,.18f) },
            new[] { new Vector2(.24f,.22f), new Vector2(.38f,.14f), new Vector2(.48f,.30f), new Vector2(.48f,.78f), new Vector2(.78f,.88f), new Vector2(.78f,.46f), new Vector2(.66f,.34f), new Vector2(.56f,.42f) }
        };

        private static readonly int[][] StrokeOrders =
        {
            new[] { 0, 1, 2, 3, 4, 5, 6, 7, 8 },
            new[] { 0, 1, 2, 3, 4, 5, 6 },
            new[] { 0, 1, 2, 3, 4, 5, 6, 7 },
            new[] { 0, 1, 2, 3, 4, 5, 6, 7 },
            new[] { 0, 1, 2, 3, 4, 5, 6, 7 }
        };

        private readonly List<ChapterThreeInteractable> interactables = new();
        private readonly List<ChapterThreeInteractable> dailyHousingProps = new();
        private readonly List<Transform> finalBookPagePivots = new();
        private readonly List<Light> indoorLights = new();
        private readonly List<Light> exteriorLights = new();
        private readonly List<RectTransform> miniStrokeNodes = new();
        private readonly List<Image> miniStrokeNodeImages = new();
        private readonly List<RectTransform> miniStrokeLines = new();
        private readonly StringBuilder hudBuilder = new(256);
        private readonly RaycastHit[] interactionHits = new RaycastHit[16];

        private ChapterThreeFirstPersonWalkthrough walker;
        private Camera viewCamera;
        private Light skyLight;
        private ChapterThreeInteractable focused;
        private ChapterThreeInteractable fundingProp;
        private ChapterThreeInteractable departureProp;
        private ChapterThreeInteractable finalBookProp;
        private Transform finalBook;
        private Vector3 finalBookBasePosition;
        private AudioSource endingMusic;
        private AudioSource chapterDayMusic;
        private AudioSource chapterNightMusic;
        private Transform finalBookCoverPivot;

        private Font uiFont;
        private Canvas canvas;
        private Text dayClockText;
        private Text sanityText;
        private Image sanityFill;
        private Text objectiveText;
        private Text taskText;
        private Text evidenceText;
        private Text promptText;
        private GameObject promptPanel;
        private Text toastText;
        private GameObject toastPanel;
        private Image vignette;
        private Image fadeImage;
        private GameObject endingCinematicPanel;
        private CanvasGroup endingCinematicGroup;
        private Image endingCinematicGlow;
        private Text endingKicker;
        private Text endingTitle;
        private Text endingBody;
        private Text endingHint;
        private int endingBookPageIndex;
        private float endingBookInputReadyAt;
        private string[] endingBookPageTitles = Array.Empty<string>();
        private string[] endingBookPageBodies = Array.Empty<string>();
        private GameObject modalPanel;
        private Text modalTitle;
        private Text modalBody;
        private RawImage modalMediaImage;
        private AspectRatioFitter modalMediaAspect;
        private Button modalLeftButton;
        private Button modalRightButton;
        private Text modalLeftLabel;
        private Text modalRightLabel;
        private GameObject miniGamePanel;
        private Text miniGameTitle;
        private Text miniGameInstruction;
        private Text miniGameProgressText;
        private Text miniGameStageText;
        private Text miniGameFeedbackText;
        private Image miniGameFill;
        private GameObject miniGameCarryTarget;
        private RectTransform miniGameCarryCursor;
        private GameObject miniCarryMouseControls;
        private GameObject miniCleanMouseControls;
        private GameObject miniTypeMouseControls;
        private GameObject miniStrokeMouseControls;
        private InputField miniTypingInput;
        private ChapterThreeWorkBoard workBoard;
        private RectTransform miniStrokeLiveLine;
        private char pendingMiniMouseInput;
        private MeixiSharedChapterPauseMenu pauseMenu;
        private Material housingCardMaterial;
        private readonly int[] housingSpawnDays = new int[ChapterThreeRules.HousingClueCount];
        private readonly int[] housingSpawnRooms = new int[ChapterThreeRules.HousingClueCount];

        private int currentDay;
        private float gameMinutes;
        private float sanity;
        private float scoreTotal;
        private int taskMask;
        private int truthMask;
        private int housingMask;
        private bool voteResolved;
        private bool stayed;
        private bool fundingFound;
        private bool lightsOn = true;
        private bool inputLocked;
        private bool cinematicActive;
        private bool endingSequence;
        private bool endingBookSpawned;
        private bool finalComplete;
        private bool endingCinematicReady;
        private bool endingFilmActive;
        private bool modalIsChoice;
        private bool sleepSelectorActive;
        private int selectedSleepHours = 9;
        private bool miniGameActive;
        private float toastUntil;
        private int lastSavedHour = -1;
        private int lastHudDay = -1;
        private int lastHudMinute = -1;
        private int lastHudSanity = -1;
        private int lastHudDifficulty = -1;
        private int lastHudTaskMask = int.MinValue;
        private int lastHudTruthMask = int.MinValue;
        private int lastHudHousingMask = int.MinValue;
        private bool lastHudStayed;
        private bool lastHudFundingFound;
        private float nextLightingRefreshAt;
        private Action modalLeftAction;
        private Action modalRightAction;
        private string endingSummary = string.Empty;

        private ChapterThreeTaskKind miniTaskKind;
        private int miniTaskBit;
        private int miniDifficulty;
        private float miniElapsed;
        private float miniProgress;
        private float miniTarget;
        private float miniLastInputAt;
        private int miniSuccesses;
        private int miniMistakes;
        private int miniCombo;
        private int miniVariant;
        private int miniTypingLastLength;
        private string miniTypingTarget = string.Empty;
        private Vector2[] miniStrokeLayout = Array.Empty<Vector2>();
        private int miniStrokeCursor;
        private bool miniStrokeDragging;
        private bool cleanExpectLeft = true;
        private float carryTargetMin = .42f;
        private float carryTargetMax = .60f;

        public int CurrentDay => currentDay;
        public float CurrentSanity => sanity;
        public bool InputLocked => inputLocked;
        public bool FinalBookAvailable => endingBookSpawned && finalBookProp != null && !finalBookProp.Consumed;

        private IEnumerator Start()
        {
            Application.targetFrameRate = 90;
            // Keep interaction animations and VideoPlayer preparation progressing
            // when the standalone window briefly loses focus.
            Application.runInBackground = true;
            if (MeixiLocalProgress.CurrentChapter < 3) MeixiLocalProgress.SetGameplayChapter(3);
            walker = FindAnyObjectByType<ChapterThreeFirstPersonWalkthrough>();
            viewCamera = walker != null ? walker.GetComponentInChildren<Camera>() : Camera.main;
            CacheLights();
            BuildUi();
            SetupChapterMusic();
            LoadProgress();
            BuildGameplayProps();
            RestoreCollectedProps();
            RefreshWorldState(true);
            yield return null;

            if (MeixiLocalProgress.ChapterThreeCompleted)
            {
                finalComplete = true;
                yield return RevealFromBlack(.55f);
                endingSummary = MeixiLocalProgress.ChapterThreeEnding;
                ShowChoice("回响已经归档", "终末之书已收回所有声音。可以重看结局影像并翻阅终末之书，或返回游戏首页。",
                    "重看结局", PlayEndingFilm, "返回首页", ReturnToStartupPage);
            }
            else if (MeixiLocalProgress.ChapterThreeEndingReady || MeixiLocalProgress.TerminalBookPending)
            {
                endingSequence = true;
                endingSummary = MeixiLocalProgress.ChapterThreeEnding;
                yield return RevealFromBlack(.55f);
                ShowInformation("终章 · 余音归宿", "此前的结局已经写入档案。终末之书正在民宿深处等待最后一次翻阅。", SpawnFinalBook);
            }
            else
            {
                yield return OpeningSequence();
            }

            if (HasArgument("-meixiChapterThreeSmoke")) StartCoroutine(RunSmokeValidation());
        }

        private void Update()
        {
            UpdateChapterMusic();
            UpdateFinalBookVisual();
            if (endingFilmActive) return;
            if (toastText != null && toastText.gameObject.activeSelf && Time.unscaledTime >= toastUntil)
            {
                toastText.gameObject.SetActive(false);
                if (toastPanel != null) toastPanel.SetActive(false);
            }
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame && pauseMenu != null)
            {
                if (pauseMenu.IsPaused)
                {
                    pauseMenu.HandleEscape();
                    return;
                }
                bool modalOpen = modalPanel != null && modalPanel.activeSelf;
                if (!modalOpen && !miniGameActive && !cinematicActive && !endingCinematicReady)
                {
                    pauseMenu.SetPaused(true);
                    return;
                }
            }
            if (pauseMenu != null && pauseMenu.IsPaused) return;
            if (viewCamera == null) return;
            if (miniGameActive)
            {
                // A modal work session owns its input and completion day. Resume
                // the world clock after closing it so midnight or exhaustion
                // cannot replace a live task with a sleep cinematic.
                UpdateMiniGame(keyboard);
                RefreshWorldState(false);
                return;
            }

            if (modalPanel != null && modalPanel.activeSelf)
            {
                if (keyboard != null)
                {
                    if (sleepSelectorActive)
                    {
                        if (keyboard.aKey.wasPressedThisFrame || keyboard.leftArrowKey.wasPressedThisFrame ||
                            keyboard.digit1Key.wasPressedThisFrame || keyboard.numpad1Key.wasPressedThisFrame) AdjustSleepHours(-1);
                        else if (keyboard.dKey.wasPressedThisFrame || keyboard.rightArrowKey.wasPressedThisFrame ||
                                 keyboard.digit2Key.wasPressedThisFrame || keyboard.numpad2Key.wasPressedThisFrame) AdjustSleepHours(1);
                        else if (keyboard.eKey.wasPressedThisFrame || keyboard.enterKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame) ConfirmSleepSelection();
                        else if (keyboard.escapeKey.wasPressedThisFrame) CancelSleepSelection();
                    }
                    else if (modalIsChoice && (keyboard.digit1Key.wasPressedThisFrame || keyboard.numpad1Key.wasPressedThisFrame)) InvokeModalLeft();
                    else if (modalIsChoice && (keyboard.digit2Key.wasPressedThisFrame || keyboard.numpad2Key.wasPressedThisFrame)) InvokeModalRight();
                    else if (!modalIsChoice && (keyboard.eKey.wasPressedThisFrame || keyboard.enterKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame)) InvokeModalLeft();
                }
                RefreshWorldState(false);
                return;
            }

            if (endingCinematicReady)
            {
                Mouse mouse = Mouse.current;
                if (mouse != null && mouse.leftButton.wasPressedThisFrame)
                {
                    AdvanceEndingBookPage(1);
                    return;
                }
                if (keyboard != null)
                {
                    if (keyboard.escapeKey.wasPressedThisFrame)
                    {
                        FinishEndingCinematic();
                        return;
                    }
                    if (keyboard.leftArrowKey.wasPressedThisFrame || keyboard.upArrowKey.wasPressedThisFrame ||
                        keyboard.aKey.wasPressedThisFrame)
                    {
                        AdvanceEndingBookPage(-1);
                        return;
                    }
                    if (keyboard.rightArrowKey.wasPressedThisFrame || keyboard.downArrowKey.wasPressedThisFrame ||
                        keyboard.dKey.wasPressedThisFrame || keyboard.eKey.wasPressedThisFrame ||
                        keyboard.enterKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame)
                    {
                        AdvanceEndingBookPage(1);
                        return;
                    }
                }
                return;
            }

            // Completed saves remain explorable, but their archival notice still needs
            // to be dismissible. Keep the modal path above this guard.
            if (finalComplete) return;

            if (!cinematicActive && (!endingSequence || endingBookSpawned))
            {
                // Once the ending judgement has finished, time and daily events stay
                // frozen, but the player must still be able to aim at and collect the
                // final book. The old all-or-nothing endingSequence gate made the book
                // visible yet impossible to interact with.
                if (!endingSequence) AdvanceTime(Time.unscaledDeltaTime);
                UpdateInteraction(keyboard);
                if (!endingSequence)
                {
                    MaybeOpenVote();
                    MaybeOfferDeparture();
                }
            }
            RefreshWorldState(false);
        }

        public void HandleInteraction(ChapterThreeInteractable item)
        {
            if (item == null || inputLocked || cinematicActive) return;
            switch (item.Kind)
            {
                case ChapterThreeInteractionKind.TruthClue:
                    StartCoroutine(InspectClue(item, true));
                    break;
                case ChapterThreeInteractionKind.HousingClue:
                    StartCoroutine(InspectClue(item, false));
                    break;
                case ChapterThreeInteractionKind.FundingClue:
                    StartCoroutine(InspectFunding(item));
                    break;
                case ChapterThreeInteractionKind.NoticeMedia:
                    ShowMediaNotice(item);
                    break;
                case ChapterThreeInteractionKind.TeaService:
                    if (MeixiLocalProgress.UnlockAchievement("kize_gift"))
                        ShowInformation("锦泽的馈赠", "茶水仍是温的。你取用了一杯，也替住客把这份照料继续留在了民宿里。", null);
                    else
                        ShowToast("茶水自取 · 这份馈赠已经记录");
                    break;
                case ChapterThreeInteractionKind.TaskStation:
                    TryStartTask(item.TaskKind);
                    break;
                case ChapterThreeInteractionKind.Meal:
                    StartCoroutine(EatMeal());
                    break;
                case ChapterThreeInteractionKind.Bed:
                    OpenSleepSelector();
                    break;
                case ChapterThreeInteractionKind.LightSwitch:
                    lightsOn = !lightsOn;
                    ShowToast(lightsOn ? "民宿灯光已打开" : "民宿灯光已关闭");
                    break;
                case ChapterThreeInteractionKind.Television:
                case ChapterThreeInteractionKind.BedsideLamp:
                case ChapterThreeInteractionKind.AirConditioner:
                case ChapterThreeInteractionKind.Wardrobe:
                case ChapterThreeInteractionKind.GuestDoor:
                    ShowToast(item.ToggleFixture());
                    break;
                case ChapterThreeInteractionKind.Clock:
                    ShowInformation("墙上的机械钟", $"第 {currentDay} 天  ·  {FormatTime(gameMinutes)}\n现实13分钟对应游戏24小时。05:00后天色渐亮，18:30后夜色逐渐压低。", null);
                    break;
                case ChapterThreeInteractionKind.Departure:
                    if (stayed && currentDay >= 9 && ChapterThreeRules.IsDayComplete(taskMask, 9)) BeginDepartureEnding();
                    else ShowToast(currentDay < 9 ? "交流实践尚未结束" : "完成今天的任务后再离开");
                    break;
                case ChapterThreeInteractionKind.FinalBook:
                    if (!endingCinematicReady) StartCoroutine(CollectFinalBook());
                    break;
            }
        }

        private void LoadProgress()
        {
            currentDay = MeixiLocalProgress.ChapterThreeDay;
            gameMinutes = MeixiLocalProgress.ChapterThreeMinutes;
            sanity = MeixiLocalProgress.ChapterThreeSanity;
            scoreTotal = MeixiLocalProgress.ChapterThreeScoreTotal;
            taskMask = MeixiLocalProgress.ChapterThreeTaskMask;
            truthMask = MeixiLocalProgress.ChapterThreeTruthMask;
            housingMask = MeixiLocalProgress.ChapterThreeHousingMask;
            voteResolved = MeixiLocalProgress.ChapterThreeVoteResolved;
            stayed = MeixiLocalProgress.ChapterThreeStayed;
            fundingFound = MeixiLocalProgress.ChapterThreeFundingFound;
        }

        private void SaveProgress()
        {
            if (endingSequence || finalComplete) return;
            MeixiLocalProgress.SaveChapterThreeCheckpoint(currentDay, gameMinutes, sanity, scoreTotal,
                taskMask, truthMask, housingMask, voteResolved, stayed, fundingFound);
        }

        private void CacheLights()
        {
            Light[] lights = GetComponentsInChildren<Light>(true);
            for (int i = 0; i < lights.Length; i++)
            {
                if (lights[i].type == LightType.Directional) skyLight = lights[i];
                else
                {
                    lights[i].shadows = LightShadows.None;
                    // Older baked scenes contain the same fixtures with the
                    // pre-tuning intensity/range. Normalize them at runtime so a
                    // scene opened before the next bake receives the brighter
                    // whole-room ceiling lighting as well.
                    if (lights[i].name == "Chapter3_InteriorLight" && lights[i].range >= 5f)
                    {
                        lights[i].intensity = Mathf.Max(lights[i].intensity, 2.20f);
                        lights[i].range = Mathf.Max(lights[i].range, 8.0f);
                    }
                    if (lights[i].name.Contains("Exterior")) exteriorLights.Add(lights[i]);
                    else indoorLights.Add(lights[i]);
                }
            }
        }

        private void SetupChapterMusic()
        {
            AudioListener.pause = false;
            GameObject musicRoot = new("ChapterThree_DayNightMusic");
            musicRoot.transform.SetParent(transform, false);
            chapterDayMusic = musicRoot.AddComponent<AudioSource>();
            chapterNightMusic = musicRoot.AddComponent<AudioSource>();
            ConfigureChapterMusicSource(chapterDayMusic, Resources.Load<AudioClip>("Audio/ChapterThreeDay"));
            ConfigureChapterMusicSource(chapterNightMusic, Resources.Load<AudioClip>("Audio/ChapterThreeNight"));
            if (chapterDayMusic.clip == null || chapterNightMusic.clip == null)
                Debug.LogError("[MeixiChapterThree] Missing day/night BGM under Resources/Audio.");
        }

        private static void ConfigureChapterMusicSource(AudioSource source, AudioClip clip)
        {
            source.clip = clip;
            source.loop = true;
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            source.volume = 0f;
            source.priority = 96;
            source.ignoreListenerPause = false;
            if (clip != null) source.Play();
        }

        private void UpdateChapterMusic()
        {
            if (chapterDayMusic == null || chapterNightMusic == null) return;
            if (AudioListener.pause) return;
            if (chapterDayMusic.clip != null && !chapterDayMusic.isPlaying) chapterDayMusic.Play();
            if (chapterNightMusic.clip != null && !chapterNightMusic.isPlaying) chapterNightMusic.Play();
            float hour = Mathf.Repeat(gameMinutes / 60f, 24f);
            float evening = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(17.75f, 19.25f, hour));
            float beforeDawn = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(5.10f, 6.65f, hour));
            float nightWeight = Mathf.Clamp01(Mathf.Max(evening, beforeDawn));
            float master = endingSequence || finalComplete ? 0f : 1f;
            float dayTarget = (1f - nightWeight) * .28f * master;
            float nightTarget = nightWeight * .30f * master;
            float fadeStep = Time.unscaledDeltaTime * .12f;
            chapterDayMusic.volume = Mathf.MoveTowards(chapterDayMusic.volume, dayTarget, fadeStep);
            chapterNightMusic.volume = Mathf.MoveTowards(chapterNightMusic.volume, nightTarget, fadeStep);
        }

        private IEnumerator OpeningSequence()
        {
            cinematicActive = true;
            SetControlLocked(true, false);
            fadeImage.gameObject.SetActive(true);
            fadeImage.color = Color.black;
            yield return new WaitForSecondsRealtime(.35f);
            ShowToast("2030 · 梅溪实践民宿");
            yield return RevealFromBlack(1.45f);
            cinematicActive = false;
            ShowInformation("第三章 · 七日回声",
                "你随实践队入住梅溪民宿。白天完成实践任务，夜里留意理智与灯光；散落在客房里的档案，也许比奖状更接近事实。\n\nWASD移动 · 鼠标观察 · E互动 · Shift快走", null);
        }

        private IEnumerator RevealFromBlack(float duration)
        {
            fadeImage.gameObject.SetActive(true);
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                fadeImage.color = new Color(0f, 0f, 0f, 1f - Mathf.SmoothStep(0f, 1f, elapsed / duration));
                yield return null;
            }
            fadeImage.color = Color.clear;
            fadeImage.gameObject.SetActive(false);
        }

        private void AdvanceTime(float realDelta)
        {
            if (realDelta <= 0f || cinematicActive || endingSequence) return;
            gameMinutes += realDelta * ChapterThreeRules.GameMinutesPerRealSecond;
            while (gameMinutes >= 1440f)
            {
                gameMinutes -= 1440f;
                currentDay++;
                if (currentDay > (stayed ? 9 : 7))
                {
                    currentDay = stayed ? 9 : 7;
                    gameMinutes = 1320f;
                    BeginDepartureEnding();
                    return;
                }
                RefreshDailyHousingClue();
                lastSavedHour = -1;
                ShowToast($"第 {currentDay} 天开始了");
                SaveProgress();
            }

            float hour = gameMinutes / 60f;
            float drain = ChapterThreeRules.SanityDrainPerSecond(hour, lightsOn);
            sanity = Mathf.Max(0f, sanity - drain * realDelta);
            if (sanity <= 0f && !cinematicActive) StartCoroutine(SleepHours(10f, true));

            int saveHour = currentDay * 24 + Mathf.FloorToInt(hour);
            if (saveHour != lastSavedHour)
            {
                lastSavedHour = saveHour;
                SaveProgress();
            }
        }

        private void RefreshWorldState(bool force)
        {
            if (dayClockText == null) return;
            float hour = gameMinutes / 60f;
            float sunrise = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(5f, 7f, hour));
            float sunset = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(18.5f, 20f, hour));
            float daylight = Mathf.Min(sunrise, sunset);
            bool lightingTick = force || Time.unscaledTime >= nextLightingRefreshAt;
            if (lightingTick)
            {
                nextLightingRefreshAt = Time.unscaledTime + .10f;
                if (skyLight != null)
                {
                    skyLight.intensity = Mathf.Lerp(.34f, 1.42f, daylight);
                    skyLight.color = Color.Lerp(new Color(.20f, .34f, .48f), new Color(1f, .86f, .68f), daylight);
                    skyLight.transform.rotation = Quaternion.Euler(Mathf.Lerp(18f, 58f, daylight), -32f + hour * 2f, 0f);
                }
                RenderSettings.ambientSkyColor = Color.Lerp(new Color(.16f, .20f, .26f), new Color(.56f, .61f, .67f), daylight);
                RenderSettings.ambientEquatorColor = Color.Lerp(new Color(.11f, .12f, .14f), new Color(.39f, .37f, .33f), daylight);
                RenderSettings.ambientGroundColor = Color.Lerp(new Color(.065f, .072f, .082f), new Color(.26f, .25f, .22f), daylight);
                RenderSettings.ambientIntensity = Mathf.Lerp(.80f, 1.24f, daylight);
                RenderSettings.fogColor = Color.Lerp(new Color(.045f, .060f, .078f), new Color(.31f, .37f, .40f), daylight);
                Material sky = RenderSettings.skybox;
                if (sky != null)
                {
                    if (sky.HasProperty(SkyTintId)) sky.SetColor(SkyTintId, Color.Lerp(new Color(.035f, .055f, .10f), new Color(.31f, .43f, .60f), daylight));
                    if (sky.HasProperty(GroundColorId)) sky.SetColor(GroundColorId, Color.Lerp(new Color(.008f, .012f, .022f), new Color(.13f, .10f, .08f), daylight));
                    if (sky.HasProperty(ExposureId)) sky.SetFloat(ExposureId, Mathf.Lerp(.54f, 1.16f, daylight));
                }

                for (int i = 0; i < indoorLights.Count; i++)
                {
                    // Ceiling fixtures should light the whole bedroom, including
                    // the far desk and wardrobe side of the room.
                    bool nearby = true;
                    ChapterThreeInteractable fixture = indoorLights[i].GetComponentInParent<ChapterThreeInteractable>();
                    bool fixtureEnabled = fixture == null || fixture.Kind != ChapterThreeInteractionKind.BedsideLamp || fixture.ToggleState;
                    indoorLights[i].enabled = lightsOn && nearby && fixtureEnabled;
                }
                bool exteriorNight = hour >= 18.15f || hour < 6.35f;
                for (int i = 0; i < exteriorLights.Count; i++) exteriorLights[i].enabled = exteriorNight;
            }

            int difficulty = ChapterThreeRules.Difficulty(sanity);
            int minute = Mathf.FloorToInt(gameMinutes);
            int roundedSanity = Mathf.RoundToInt(sanity);
            bool hudDirty = force || currentDay != lastHudDay || minute != lastHudMinute || roundedSanity != lastHudSanity ||
                            difficulty != lastHudDifficulty || taskMask != lastHudTaskMask || truthMask != lastHudTruthMask ||
                            housingMask != lastHudHousingMask || stayed != lastHudStayed || fundingFound != lastHudFundingFound;
            if (!hudDirty) return;

            lastHudDay = currentDay;
            lastHudMinute = minute;
            lastHudSanity = roundedSanity;
            lastHudDifficulty = difficulty;
            lastHudTaskMask = taskMask;
            lastHudTruthMask = truthMask;
            lastHudHousingMask = housingMask;
            lastHudStayed = stayed;
            lastHudFundingFound = fundingFound;
            dayClockText.text = $"第 {currentDay} 天   {FormatTime(gameMinutes)}";
            sanityText.text = difficulty == 0 ? $"理智 {sanity:0}" : difficulty == 1 ? $"理智 {sanity:0} · 注意" : $"理智 {sanity:0} · 危险";
            sanityFill.fillAmount = sanity / 100f;
            sanityFill.color = difficulty == 0 ? new Color(.22f, .86f, .72f) : difficulty == 1 ? new Color(1f, .68f, .22f) : new Color(1f, .20f, .16f);
            vignette.color = new Color(.10f, 0f, .02f, Mathf.Lerp(.04f, .38f, 1f - sanity / 100f) + (1f - daylight) * .06f);
            RefreshTaskHud();
            evidenceText.text = $"真相 {CountBits(truthMask)}/{ChapterThreeRules.TruthClueCount}   ·   住房 {CountBits(housingMask)}/{ChapterThreeRules.HousingClueCount}" +
                                (fundingFound ? "   ·   保障期限已确认" : string.Empty);

            if (fundingProp != null) fundingProp.gameObject.SetActive(stayed && currentDay >= 8 && !fundingFound);
            if (departureProp != null) departureProp.gameObject.SetActive(stayed && currentDay >= 9);
        }

        private void RefreshTaskHud()
        {
            ChapterThreeTaskKind[] tasks = ChapterThreeRules.TasksForDay(currentDay);
            hudBuilder.Clear();
            for (int slot = 0; slot < tasks.Length; slot++)
            {
                bool done = ChapterThreeRules.IsTaskCompleted(taskMask, currentDay, slot);
                if (slot > 0) hudBuilder.Append("   ");
                hudBuilder.Append(done ? "✓ " : "○ ").Append(ChapterThreeRules.TaskName(tasks[slot]));
            }
            taskText.text = hudBuilder.ToString();
            float average = ChapterThreeRules.AverageScore(scoreTotal, Mathf.Min(currentDay, 7));
            string taskObjective = ChapterThreeRules.IsDayComplete(taskMask, currentDay)
                ? (stayed && currentDay >= 9 ? "今日任务完成 · 从前门结束实践" : "今日任务完成 · 可继续调查或回房睡眠")
                : $"今日实践尚未完成   ·   当前过程均分 {average:0.0}";
            objectiveText.text = taskObjective + BuildEvidenceHint();
        }

        private string BuildEvidenceHint()
        {
            ChapterThreeInteractable nearest = null;
            float nearestDistance = float.MaxValue;
            Vector3 observer = viewCamera != null ? viewCamera.transform.position : transform.position;
            for (int i = 0; i < interactables.Count; i++)
            {
                ChapterThreeInteractable item = interactables[i];
                if (item == null || item.Consumed ||
                    (item.Kind != ChapterThreeInteractionKind.TruthClue && item.Kind != ChapterThreeInteractionKind.HousingClue)) continue;
                float distance = (item.transform.position - observer).sqrMagnitude;
                if (distance >= nearestDistance) continue;
                nearest = item;
                nearestDistance = distance;
            }
            if (nearest == null) return "\n调查提示 · 客房与院外线索已全部找到";

            Vector3 position = nearest.transform.position;
            if (position.z < -15.7f) return "\n调查提示 · 院墙外的进村路旁有线索微光";
            string floor = position.y >= 4f ? "二楼" : "一楼";
            string wing = position.x < -3f ? "西侧" : position.x > 3f ? "东侧" : "中间";
            return $"\n调查提示 · 最近的未读线索在{floor}{wing}客房";
        }

        private void UpdateInteraction(Keyboard keyboard)
        {
            ChapterThreeInteractable nextFocused = null;
            int hitCount = Physics.SphereCastNonAlloc(viewCamera.transform.position, .20f, viewCamera.transform.forward,
                interactionHits, InteractionDistance + .35f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Collide);
            float nearestSurface = float.MaxValue;
            float nearestInteractable = float.MaxValue;
            for (int i = 0; i < hitCount; i++)
            {
                RaycastHit hit = interactionHits[i];
                nearestSurface = Mathf.Min(nearestSurface, hit.distance);
                ChapterThreeInteractable item = hit.collider.GetComponentInParent<ChapterThreeInteractable>();
                if (item == null || !item.CanUse(this) || hit.distance >= nearestInteractable) continue;
                nextFocused = item;
                nearestInteractable = hit.distance;
            }
            // Surface-mounted props may sit just behind the desk or wardrobe proxy;
            // farther objects remain blocked so interaction cannot pass through walls.
            if (nearestInteractable > nearestSurface + .85f) nextFocused = null;

            if (focused != nextFocused)
            {
                if (focused != null) focused.SetFocused(false);
                focused = nextFocused;
                if (focused != null) focused.SetFocused(true);
            }

            if (focused == null || !focused.CanUse(this))
            {
                if (promptPanel != null) promptPanel.SetActive(false);
                promptText.gameObject.SetActive(false);
                return;
            }

            if (promptPanel != null) promptPanel.SetActive(true);
            promptText.gameObject.SetActive(true);
            promptText.text = "[ E ]  " + focused.DisplayName;
            if (keyboard != null && keyboard.eKey.wasPressedThisFrame) focused.Interact(this);
        }

        private void MaybeOpenVote()
        {
            if (voteResolved || currentDay < 7 || gameMinutes < 480f || cinematicActive || miniGameActive) return;
            voteResolved = true;
            SaveProgress();
            ShowChoice("第七天 · 留下还是离开",
                "两天后还有一支大学实践队来交流。队长询问你是否愿意留下。\n\n留下将继续到第九天；离开则现在乘高铁返程，并进入结项评定。",
                "留下交流  [1]", () =>
                {
                    stayed = true;
                    SaveProgress();
                    ShowInformation("你选择留下", "实践延长到第九天。民宿里也许会出现此前看不到的文件。", null);
                },
                "按期离开  [2]", BeginDepartureEnding);
        }

        private void MaybeOfferDeparture()
        {
            if (!stayed || currentDay < 9 || endingSequence || departureProp == null) return;
            if (ChapterThreeRules.IsDayComplete(taskMask, 9)) objectiveText.text = "九日实践完成 · 前往民宿大门启程";
        }

        private IEnumerator EatMeal()
        {
            if (cinematicActive || inputLocked) yield break;
            cinematicActive = true;
            SetControlLocked(true, false);
            float elapsed = 0f;
            SetCarryTaskVisual(false);
            miniGamePanel.SetActive(true);
            miniGameTitle.text = "民宿晚餐";
            miniGameInstruction.text = "一碗热汤让噪声暂时退远……";
            while (elapsed < MealDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                miniGameFill.fillAmount = Mathf.Clamp01(elapsed / MealDuration);
                miniGameProgressText.text = $"用餐 {Mathf.Min(elapsed, MealDuration):0.0} / {MealDuration:0.00} 秒";
                yield return null;
            }
            AddGameMinutes(MealDuration * ChapterThreeRules.GameMinutesPerRealSecond);
            sanity = Mathf.Min(100f, sanity + 18f);
            miniGamePanel.SetActive(false);
            cinematicActive = false;
            SetControlLocked(false, false);
            SaveProgress();
            ShowToast("用餐完成 · 理智恢复 18");
        }

        private void OpenSleepSelector()
        {
            if (cinematicActive || endingSequence || modalPanel == null) return;
            selectedSleepHours = 9;
            sleepSelectorActive = true;
            modalIsChoice = true;
            modalLeftAction = null;
            modalRightAction = null;
            modalTitle.text = "自定义睡眠时长";
            modalLeftButton.gameObject.SetActive(true);
            modalRightButton.gameObject.SetActive(true);
            modalPanel.SetActive(true);
            fadeImage.transform.SetAsLastSibling();
            modalPanel.transform.SetAsLastSibling();
            SetControlLocked(true, true);
            RefreshSleepSelector();
        }

        private void AdjustSleepHours(int amount)
        {
            if (!sleepSelectorActive) return;
            selectedSleepHours = Mathf.Clamp(selectedSleepHours + amount, 1, 12);
            RefreshSleepSelector();
        }

        private void RefreshSleepSelector()
        {
            int wakeDay = currentDay;
            float wakeMinutes = gameMinutes + selectedSleepHours * 60f;
            int maxDay = stayed ? 9 : 7;
            while (wakeMinutes >= 1440f && wakeDay < maxDay)
            {
                wakeMinutes -= 1440f;
                wakeDay++;
            }
            if (wakeMinutes >= 1440f) wakeMinutes = 1439f;
            float recovery = ChapterThreeRules.SleepRecovery(selectedSleepHours);
            float projected = ChapterThreeRules.SanityAfterSleep(sanity, selectedSleepHours);
            modalBody.text = $"选择 1—12 小时，9小时即可恢复满额理智；超过9小时只继续推进时间。\n\n" +
                             $"本次睡眠：{selectedSleepHours} 小时\n醒来：第 {wakeDay} 天 {FormatTime(wakeMinutes)}\n" +
                             $"理智：{sanity:0} + {recovery:0.#} → {projected:0.#}\n\n" +
                             "A / ← / 1 减少 · D / → / 2 增加\nE / Enter 确认 · Esc 取消";
            modalLeftLabel.text = "− 1小时  [1]";
            modalRightLabel.text = "+ 1小时  [2]";
        }

        private void ConfirmSleepSelection()
        {
            if (!sleepSelectorActive) return;
            int hours = selectedSleepHours;
            sleepSelectorActive = false;
            CloseModalWithoutAction();
            StartCoroutine(SleepHours(hours, false));
        }

        private void CancelSleepSelection()
        {
            if (!sleepSelectorActive) return;
            sleepSelectorActive = false;
            CloseModalWithoutAction();
            ShowToast("已取消睡眠");
        }

        private IEnumerator SleepHours(float hours, bool forced)
        {
            if (cinematicActive || endingSequence) yield break;
            cinematicActive = true;
            SetControlLocked(true, false);
            fadeImage.gameObject.SetActive(true);
            float elapsed = 0f;
            while (elapsed < 1.1f)
            {
                elapsed += Time.unscaledDeltaTime;
                fadeImage.color = new Color(0f, .01f, .02f, Mathf.SmoothStep(0f, 1f, elapsed / 1.1f));
                yield return null;
            }
            if (forced) ShowToast("理智归零 · 身体自行回到了房间");
            AddGameMinutes(hours * 60f);
            sanity = forced ? 95f : ChapterThreeRules.SanityAfterSleep(sanity, hours);
            SaveProgress();
            RefreshWorldState(true);
            yield return new WaitForSecondsRealtime(.65f);
            elapsed = 0f;
            while (elapsed < 1.1f)
            {
                elapsed += Time.unscaledDeltaTime;
                fadeImage.color = new Color(0f, .01f, .02f, 1f - Mathf.SmoothStep(0f, 1f, elapsed / 1.1f));
                yield return null;
            }
            fadeImage.gameObject.SetActive(false);
            cinematicActive = false;
            SetControlLocked(false, false);
            MaybeOpenVote();
            ShowToast(forced ? "强制睡眠10小时 · 理智恢复至95" : $"睡眠 {hours:0} 小时 · 理智恢复至 {sanity:0.#}");
        }

        private void AddGameMinutes(float amount)
        {
            int dayBefore = currentDay;
            gameMinutes += Mathf.Max(0f, amount);
            int maximumDay = stayed ? 9 : 7;
            while (gameMinutes >= 1440f)
            {
                if (currentDay >= maximumDay)
                {
                    gameMinutes = 1439f;
                    break;
                }
                gameMinutes -= 1440f;
                currentDay++;
            }
            if (currentDay != dayBefore) RefreshDailyHousingClue();
        }

        private void TryStartTask(ChapterThreeTaskKind kind)
        {
            ChapterThreeTaskKind[] tasks = ChapterThreeRules.TasksForDay(currentDay);
            for (int slot = 0; slot < tasks.Length; slot++)
            {
                if (tasks[slot] != kind || ChapterThreeRules.IsTaskCompleted(taskMask, currentDay, slot)) continue;
                BeginMiniGame(kind, ChapterThreeRules.TaskBitIndex(currentDay, slot));
                return;
            }
            ShowToast("今天没有待完成的“" + ChapterThreeRules.TaskName(kind) + "”");
        }

        private IEnumerator InspectClue(ChapterThreeInteractable item, bool truth)
        {
            SetControlLocked(true, false);
            Collider collider = item.GetComponent<Collider>();
            if (collider != null) collider.enabled = false;
            Vector3 originalScale = item.transform.localScale;
            Quaternion originalRotation = item.transform.rotation;
            float elapsed = 0f;
            while (elapsed < .48f)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / .48f);
                item.transform.localScale = originalScale * Mathf.Lerp(1f, 1.18f, Mathf.Sin(t * Mathf.PI));
                item.transform.rotation = originalRotation * Quaternion.Euler(0f, t * 22f, 0f);
                yield return null;
            }
            item.transform.localScale = originalScale;
            if (truth) truthMask |= 1 << item.Id;
            else housingMask |= 1 << item.Id;
            item.SetConsumed(true);
            CheckEvidenceAchievements();
            SaveProgress();
            ShowInformation(item.DisplayName, item.Story, null);
        }

        private IEnumerator InspectFunding(ChapterThreeInteractable item)
        {
            yield return InspectClueAnimation(item);
            fundingFound = true;
            item.SetConsumed(true);
            MeixiLocalProgress.UnlockAchievement("choice");
            SaveProgress();
            ShowInformation("实践保障说明 · 第4条",
                "工作站承担住宿与餐食的保障期限：7天。\n\n留下的两天不在项目预算内。原记录把住宿160元/天、餐食60元/天写成总计480元；按逐项相加实际应为440元，这个矛盾也被保留在证据里。", null);
        }

        private IEnumerator InspectClueAnimation(ChapterThreeInteractable item)
        {
            SetControlLocked(true, false);
            Collider collider = item.GetComponent<Collider>();
            if (collider != null) collider.enabled = false;
            float elapsed = 0f;
            while (elapsed < .42f)
            {
                elapsed += Time.unscaledDeltaTime;
                item.transform.Rotate(Vector3.up, Time.unscaledDeltaTime * 48f, Space.World);
                yield return null;
            }
        }

        private void CheckEvidenceAchievements()
        {
            if ((housingMask & AllHousingMask) == AllHousingMask) MeixiLocalProgress.UnlockAchievement("missing_person");
            if ((truthMask & AllTruthMask) == AllTruthMask &&
                MeixiLocalProgress.IsAchievementUnlocked("aunt_shi_first") &&
                MeixiLocalProgress.IsAchievementUnlocked("heritage"))
                MeixiLocalProgress.UnlockAchievement("chapter_three_truth");
        }

        private void BeginDepartureEnding()
        {
            if (endingSequence) return;
            endingSequence = true;
            CloseModalWithoutAction();
            StartCoroutine(TrainSequence());
        }

        private IEnumerator TrainSequence()
        {
            cinematicActive = true;
            SetControlLocked(true, false);
            fadeImage.gameObject.SetActive(true);
            float elapsed = 0f;
            while (elapsed < 1.15f)
            {
                elapsed += Time.unscaledDeltaTime;
                fadeImage.color = new Color(.01f, .025f, .035f, Mathf.SmoothStep(0f, 1f, elapsed / 1.15f));
                yield return null;
            }
            fadeImage.color = new Color(.01f, .025f, .035f, 1f);
            modalPanel.SetActive(true);
            modalIsChoice = false;
            if (modalMediaImage != null) modalMediaImage.gameObject.SetActive(false);
            if (modalBody != null) modalBody.gameObject.SetActive(true);
            modalTitle.text = "高铁穿过清晨的山雾";
            modalBody.text = stayed
                ? "第九天结束。窗外的溪流向后退去，民宿里没有说完的话被留在了档案中。"
                : "第七天结束。车窗映出一周的疲惫，也映出那些尚未被拼完整的名字。";
            modalLeftButton.gameObject.SetActive(false);
            modalRightButton.gameObject.SetActive(false);
            // The fade layer is authored after the normal modal and used to cover
            // it completely. Explicit sibling order keeps the narrative card above
            // the dark transition instead of presenting an unexplained black screen.
            fadeImage.transform.SetAsLastSibling();
            modalPanel.transform.SetAsLastSibling();
            yield return new WaitForSecondsRealtime(2.6f);
            elapsed = 0f;
            while (elapsed < .90f)
            {
                elapsed += Time.unscaledDeltaTime;
                float alpha = 1f - Mathf.SmoothStep(0f, 1f, elapsed / .90f);
                fadeImage.color = new Color(.01f, .025f, .035f, alpha);
                yield return null;
            }
            fadeImage.color = Color.clear;
            fadeImage.gameObject.SetActive(false);
            float average = ChapterThreeRules.AverageScore(scoreTotal, 7);
            ChapterThreeEndingTier tier = ChapterThreeRules.EvaluateTier(average);
            string tierName;
            if (tier == ChapterThreeEndingTier.Ordinary) tierName = "平平无奇";
            else if (tier == ChapterThreeEndingTier.Model)
            {
                tierName = "实践标兵";
                MeixiLocalProgress.UnlockAchievement("chapter_three_soul");
                MeixiLocalProgress.UnlockAchievement("chapter_three_model");
            }
            else
            {
                tierName = "优秀结项";
                MeixiLocalProgress.UnlockAchievement("chapter_three_soul");
            }
            endingSummary = $"{tierName} · 平均分 {average:0.0}";
            cinematicActive = false;
            ShowInformation("第三章实践评定", $"七日基础实践均分：{average:0.0}\n结项等级：{tierName}\n\n" +
                (tier == ChapterThreeEndingTier.Ordinary ? "第三枚音魂没有回应。支线道德判定被跳过。" : "第三枚音魂已经与你共鸣。"),
                tier == ChapterThreeEndingTier.Ordinary ? () => FinalizeEnding("没有惊天动地的成果，但你至少看见了真实的乡野生活。") : BeginMoralBranch);
        }

        private void BeginMoralBranch()
        {
            if (!MeixiLocalProgress.IsAchievementUnlocked("choice"))
            {
                FinalizeEnding("实践按既定期限结束，未发生额外费用争议。");
                return;
            }
            ShowChoice("额外两天的费用",
                "队长低声提醒：保障只覆盖七天。按住宿160元/天、餐食60元/天计算，两天实际为440元；旧表却写着480元。目前工作站尚未索要费用。你选择——",
                "主动联系并付费  [1]", () =>
                {
                    MeixiLocalProgress.UnlockAchievement("reason");
                    BeginReportBranch();
                },
                "保持沉默  [2]", () =>
                {
                    MeixiLocalProgress.UnlockAchievement("silence");
                    FinalizeEnding("你决定等待对方询问，额外两天的账目沉入了沉默。");
                });
        }

        private void BeginReportBranch()
        {
            bool missing = MeixiLocalProgress.IsAchievementUnlocked("missing_person");
            bool truth = MeixiLocalProgress.IsAchievementUnlocked("chapter_three_truth");
            if (!missing)
            {
                FinalizeEnding("你主动结清了费用，但住房名册仍缺少足够的证据。");
                return;
            }

            if (truth)
            {
                ShowChoice("完整证据 · 是否举报",
                    "住房记录证明龚炜杰只是挂名；跨越三章的证据还揭示了队长的功利、资金争夺与林氏祠堂纵火。事情过去多年，手续繁琐，但沉默同样是一种选择。",
                    "提交完整证据  [1]", () =>
                    {
                        MeixiLocalProgress.UnlockAchievement("true_ending");
                        FinalizeEnding("真正的大结局：完整证据被提交，失声者的记忆终于进入公共档案。");
                    },
                    "不再追究  [2]", () =>
                    {
                        MeixiLocalProgress.UnlockAchievement("cover_up");
                        FinalizeEnding("你保留了完整证据，却选择不再追究。真相被收藏，也被包庇。");
                    });
                return;
            }

            ShowChoice("不完整证据 · 是否举报",
                "住房记录显示龚炜杰在2026年仅挂名，涉嫌人员造假与学术不端；但火灾与队长动机的证据仍不完整。",
                "仍然举报  [1]", () =>
                {
                    MeixiLocalProgress.UnlockAchievement("false_truth");
                    FinalizeEnding("你提交了人员造假的证据，却只能触及一层不完整的真相。");
                },
                "不举报  [2]", () =>
                {
                    MeixiLocalProgress.UnlockAchievement("silence");
                    FinalizeEnding("你没有提交不完整的材料，名单重新归于沉默。");
                });
        }

        private void FinalizeEnding(string suffix)
        {
            endingSummary += "\n" + suffix;
            float average = ChapterThreeRules.AverageScore(scoreTotal, 7);
            bool soul = ChapterThreeRules.EvaluateTier(average) != ChapterThreeEndingTier.Ordinary;
            MeixiLocalProgress.MarkChapterThreeEndingReady(average, soul, endingSummary);
            ShowInformation("余音落定", endingSummary + "\n\n最后一阵声纹从民宿深处亮起。所有判断完成后，终末之书才真正出现。", SpawnFinalBook);
        }

        private void SpawnFinalBook()
        {
            CloseModalWithoutAction();
            if (endingBookSpawned || finalComplete) return;
            endingBookSpawned = true;
            Material cover = CreateMaterial("Chapter3_FinalBookCover", new Color(.018f, .10f, .12f), .55f);
            Material paper = CreateMaterial("Chapter3_FinalBookPaper", new Color(.88f, .76f, .54f), .10f);
            Material gold = CreateEmissive("Chapter3_FinalBookGold", new Color(1f, .54f, .12f), 4f);
            GameObject bookRoot = new("第三章终点_终末之书");
            bookRoot.transform.SetParent(transform, false);
            finalBookBasePosition = new Vector3(8.42f, 5.18f, 8.12f);
            bookRoot.transform.localPosition = finalBookBasePosition;
            finalBook = bookRoot.transform;
            CreateBookPart("下封面", bookRoot.transform, new Vector3(0f, -.10f, 0f), new Vector3(1.22f, .09f, .88f), cover);
            CreateBookPart("书页纸芯", bookRoot.transform, new Vector3(.01f, -.01f, 0f), new Vector3(1.10f, .13f, .79f), paper);
            CreateBookPart("金色书脊", bookRoot.transform, new Vector3(-.59f, .01f, 0f), new Vector3(.07f, .24f, .90f), gold);
            GameObject coverPivot = new("终末之书上封面铰链");
            coverPivot.transform.SetParent(bookRoot.transform, false);
            coverPivot.transform.localPosition = new Vector3(-.59f, .10f, 0f);
            finalBookCoverPivot = coverPivot.transform;
            CreateBookPart("上封面", coverPivot.transform, new Vector3(.61f, 0f, 0f), new Vector3(1.22f, .07f, .88f), cover);
            finalBookPagePivots.Clear();
            for (int i = 0; i < 6; i++)
            {
                GameObject pagePivot = new("终末之书翻页铰链_" + i);
                pagePivot.transform.SetParent(bookRoot.transform, false);
                pagePivot.transform.localPosition = new Vector3(-.55f, .025f + i * .012f, 0f);
                CreateBookPart("终末之书可翻页_" + i, pagePivot.transform,
                    new Vector3(.555f, 0f, 0f), new Vector3(1.10f, .012f, .78f), paper);
                finalBookPagePivots.Add(pagePivot.transform);
            }
            Light glow = bookRoot.AddComponent<Light>();
            glow.type = LightType.Point; glow.range = 6f; glow.intensity = 2.4f; glow.color = new Color(.26f, .92f, .82f); glow.shadows = LightShadows.None;
            BoxCollider collider = bookRoot.AddComponent<BoxCollider>();
            collider.size = new Vector3(2.65f, 1.55f, 2.20f);
            collider.center = new Vector3(0f, .25f, 0f);
            collider.isTrigger = true;
            finalBookProp = bookRoot.AddComponent<ChapterThreeInteractable>();
            finalBookProp.Configure(ChapterThreeInteractionKind.FinalBook, 0, "拾取终末之书", "第三章结束后才出现的最后一页");
            interactables.Add(finalBookProp);
            objectiveText.text = "前往二楼 203 档案房，拾取终末之书";
            endingMusic = bookRoot.AddComponent<AudioSource>();
            endingMusic.clip = Resources.Load<AudioClip>("Audio/EndingTheme");
            endingMusic.loop = true; endingMusic.volume = .28f; endingMusic.spatialBlend = 0f;
            if (endingMusic.clip != null) endingMusic.Play();
            SetControlLocked(false, false);
            ShowToast("终末之书已在二楼 203 档案房显现");

            GameObject CreateBookPart(string name, Transform parent, Vector3 localPosition, Vector3 localScale, Material material)
            {
                GameObject part = GameObject.CreatePrimitive(PrimitiveType.Cube);
                part.name = name; part.transform.SetParent(parent, false); part.transform.localPosition = localPosition; part.transform.localScale = localScale;
                part.GetComponent<Renderer>().sharedMaterial = material;
                Destroy(part.GetComponent<Collider>());
                return part;
            }
        }

        private void UpdateFinalBookVisual()
        {
            if (finalBook == null || finalComplete) return;
            float wave = Mathf.Sin(Time.unscaledTime * 1.5f);
            finalBook.localPosition = finalBookBasePosition + Vector3.up * (wave * .10f);
            finalBook.Rotate(Vector3.up, Time.unscaledDeltaTime * 11f, Space.World);
            Light light = finalBook.GetComponent<Light>();
            if (light != null) light.intensity = 2.2f + (wave + 1f) * .32f;
        }

        private IEnumerator CollectFinalBook()
        {
            if (finalComplete || endingCinematicReady || finalBookProp == null) yield break;
            cinematicActive = true;
            SetControlLocked(true, false);
            Collider finalCollider = finalBookProp.GetComponent<Collider>();
            if (finalCollider != null) finalCollider.enabled = false;
            if (endingMusic != null) endingMusic.volume = .18f;
            if (endingCinematicPanel == null || endingCinematicGroup == null)
            {
                finalComplete = true;
                cinematicActive = false;
                yield break;
            }

            endingCinematicReady = false;
            endingCinematicPanel.SetActive(true);
            endingCinematicPanel.transform.SetAsLastSibling();
            endingCinematicGroup.alpha = 0f;
            Image endingBackground = endingCinematicPanel.GetComponent<Image>();
            if (endingBackground != null)
            {
                endingBackground.sprite = null;
                endingBackground.color = new Color(.004f, .018f, .022f, .92f);
            }
            endingCinematicGlow.gameObject.SetActive(true);
            endingCinematicGlow.color = new Color(.08f, .72f, .62f, .08f);
            endingKicker.text = "";
            endingTitle.text = "终末之书";
            endingBody.text = "";
            endingHint.text = "";
            SetTextAlpha(endingKicker, 0f); SetTextAlpha(endingTitle, 0f); SetTextAlpha(endingBody, 0f); SetTextAlpha(endingHint, 0f);
            fadeImage.gameObject.SetActive(false);

            Vector3 bookStart = finalBook != null ? finalBook.position : transform.position;
            Quaternion bookRotation = finalBook != null ? finalBook.rotation : Quaternion.identity;
            Vector3 bookScale = finalBook != null ? finalBook.localScale : Vector3.one;
            float elapsed = 0f;
            const float riseDuration = 2.25f;
            while (elapsed < riseDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / riseDuration);
                float eased = Mathf.SmoothStep(0f, 1f, t);
                endingCinematicGroup.alpha = Mathf.Lerp(0f, .78f, Mathf.SmoothStep(0f, 1f, t * 1.15f));
                endingCinematicGlow.color = new Color(.08f, .72f, .62f, Mathf.Lerp(.08f, .28f, eased));
                if (finalBook != null)
                {
                    finalBook.position = bookStart + Vector3.up * (.85f * eased);
                    finalBook.rotation = bookRotation * Quaternion.Euler(0f, 250f * eased, 16f * Mathf.Sin(t * Mathf.PI));
                    finalBook.localScale = Vector3.Lerp(bookScale, bookScale * 1.16f, eased);
                }
                // Preserve the established page-turn language: the cover opens
                // first, then individual sheets follow with a readable stagger.
                float coverTurn = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(.16f, .58f, t));
                if (finalBookCoverPivot != null)
                    finalBookCoverPivot.localRotation = Quaternion.Euler(0f, 0f, -148f * coverTurn);
                for (int i = 0; i < finalBookPagePivots.Count; i++)
                {
                    Transform page = finalBookPagePivots[i];
                    if (page == null) continue;
                    float pageTurn = Mathf.SmoothStep(0f, 1f,
                        Mathf.InverseLerp(.31f + i * .055f, .66f + i * .045f, t));
                    page.localRotation = Quaternion.Euler(0f, 0f, -136f * pageTurn);
                }
                yield return null;
            }

            if (finalBook != null)
            {
                finalBookProp.SetConsumed(true);
                finalBook.gameObject.SetActive(false);
                finalBook = null;
            }
            // Commit completion only after the physical pickup animation finishes.
            // If the application closes before this point, the book safely respawns.
            MeixiLocalProgress.RecordFinalBookCollected();
            PlayEndingFilm();
            Debug.Log("[MeixiChapterThree] Final book collected; film first, then manual book pages.");
        }

        private void LoadEndingBookPages()
        {
            TextAsset endingAsset = Resources.Load<TextAsset>("EndingMessage");
            string source = endingAsset != null ? endingAsset.text.Trim() : string.Empty;
            if (string.IsNullOrEmpty(source))
            {
                endingBookPageTitles = new[] { "余音未散" };
                endingBookPageBodies = new[] { "终末之书的页页回响仍在继续。" };
                return;
            }

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
            endingBookPageTitles = titles.ToArray();
            endingBookPageBodies = bodies.ToArray();
        }

        private void PresentEndingBookPages()
        {
            cinematicActive = true;
            SetControlLocked(true, true);
            endingCinematicPanel.SetActive(true);
            endingCinematicPanel.transform.SetAsLastSibling();
            LoadEndingBookPages();
            endingBookPageIndex = 0;
            endingBookInputReadyAt = Time.unscaledTime + .75f;
            Image background = endingCinematicPanel.GetComponent<Image>();
            Texture2D texture = Resources.Load<Texture2D>("Art/EndingOpenBook");
            if (background != null && texture != null)
            {
                background.sprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height),
                    new Vector2(.5f, .5f), 100f);
                background.preserveAspect = false;
                background.color = Color.white;
            }
            endingCinematicGlow.gameObject.SetActive(false);
            endingCinematicGroup.alpha = 1f;
            endingCinematicGroup.blocksRaycasts = true;
            endingCinematicReady = true;
            PresentEndingBookPage();
            MeixiCursorState.SetPointer();
        }

        private void PresentEndingBookPage()
        {
            if (endingBookPageBodies.Length == 0) return;
            endingBookPageIndex = Mathf.Clamp(endingBookPageIndex, 0, endingBookPageBodies.Length - 1);
            endingKicker.text = $"{endingBookPageIndex + 1:00}  /  {endingBookPageBodies.Length:00}";
            endingTitle.text = endingBookPageTitles[endingBookPageIndex];
            endingBody.text = endingBookPageBodies[endingBookPageIndex];
            endingBody.fontSize = endingBookPageIndex == endingBookPageBodies.Length - 1 ? 20 :
                endingBookPageIndex == 3 ? 23 : 27;
            endingHint.text = endingBookPageIndex >= endingBookPageBodies.Length - 1
                ? "A / ← 上一页       左键 / SPACE 完成阅读并返回首页"
                : "A / ← 上一页       左键 / SPACE 下一页";
            SetTextAlpha(endingKicker, 1f);
            SetTextAlpha(endingTitle, 1f);
            SetTextAlpha(endingBody, 1f);
            SetTextAlpha(endingHint, 1f);
        }

        private void AdvanceEndingBookPage(int direction)
        {
            if (!endingCinematicReady || Time.unscaledTime < endingBookInputReadyAt ||
                endingBookPageBodies.Length == 0) return;
            int next = endingBookPageIndex + Math.Sign(direction);
            if (next < 0)
            {
                endingBookPageIndex = 0;
                PresentEndingBookPage();
                return;
            }
            if (next >= endingBookPageBodies.Length)
            {
                FinishEndingCinematic();
                return;
            }
            endingBookPageIndex = next;
            PresentEndingBookPage();
        }

        private void FinishEndingCinematic()
        {
            if (!endingCinematicReady || Time.unscaledTime < endingBookInputReadyAt) return;
            // ESC may finish the last page, but cannot skip unread pages.
            if (endingBookPageIndex < endingBookPageBodies.Length - 1) return;
            endingCinematicReady = false;
            if (endingCinematicGroup != null) endingCinematicGroup.blocksRaycasts = false;
            if (endingCinematicPanel != null) endingCinematicPanel.SetActive(false);
            ReturnToStartupPage();
        }

        private void ReturnToStartupPage()
        {
            Time.timeScale = 1f;
            AudioListener.pause = false;
            MeixiCursorState.SetPointer();
            // Loading the first scene presents its existing startup/title UI;
            // do not reset the player's completed save or return to the homestay.
            UnityEngine.SceneManagement.SceneManager.LoadSceneAsync("MeixiEditable");
        }

        private void PlayEndingFilm()
        {
            if (endingFilmActive) return;
            CloseModalWithoutAction();
            endingFilmActive = true; cinematicActive = true;
            endingCinematicReady = false;
            if (endingCinematicPanel != null) endingCinematicPanel.SetActive(false);
            if (fadeImage != null) fadeImage.gameObject.SetActive(false);
            SetControlLocked(true, true);
            if (endingMusic != null) endingMusic.Pause();
            var film = gameObject.AddComponent<ChapterThreeEndingFilm>();
            StartCoroutine(film.Play(canvas, endingSummary, () =>
            {
                endingFilmActive = false; finalComplete = true;
                PresentEndingBookPages();
                Destroy(film, 1f);
            }));
        }

        private void BuildGameplayProps()
        {
            ChapterThreeInteractable[] authored = GetComponentsInChildren<ChapterThreeInteractable>(true);
            for (int i = 0; i < authored.Length; i++)
                if (!interactables.Contains(authored[i])) interactables.Add(authored[i]);
            Material paper = CreateMaterial("Chapter3_EvidencePaper", new Color(.86f, .75f, .54f), .08f);
            housingCardMaterial = CreateMaterial("Chapter3_HousingCard", new Color(.40f, .66f, .70f), .22f);
            Material card = housingCardMaterial;
            Material task = CreateEmissive("Chapter3_TaskMarker", new Color(.08f, .34f, .30f), 1.45f);
            Material warning = CreateEmissive("Chapter3_Deadline", new Color(1f, .42f, .12f), 2.2f);
            Vector3[] safeTruthPositions =
            {
                // Preserve one clue outside the gate and one on the reception
                // archive counter. Every other clue is assigned to a specific
                // wardrobe below; there are no bed or bedside fallback points.
                new(1.75f, .52f, -16.85f), new(-5.05f, 1.51f, -3.82f)
            };
            int[] wardrobeRooms = { 101, 102, 103, 201, 202, 203 };
            for (int i = 0; i < TruthTitles.Length; i++)
            {
                bool useWardrobe = i >= 1 && i <= 6;
                Transform wardrobeParent = useWardrobe ? FindWardrobeParentByRoomNumber(wardrobeRooms[i - 1]) : null;
                Vector3 cluePosition;
                if (wardrobeParent != null)
                    cluePosition = new Vector3(0f, 1.12f, -.24f);
                else
                    cluePosition = i == 0 ? safeTruthPositions[0] : safeTruthPositions[1] + new Vector3((i % 3) * .18f, .04f, 0f);
                CreateProp("真相线索_" + i, cluePosition, new Vector3(.62f, .07f, .46f), paper,
                    ChapterThreeInteractionKind.TruthClue, i, TruthTitles[i], TruthBodies[i], 1,
                    ChapterThreeTaskKind.Carry, wardrobeParent);
            }

            BuildHousingSchedule();
            RefreshDailyHousingClue();

            CreateProp("搬运行李任务点", new Vector3(-3.70f, .72f, -7.05f), new Vector3(1.2f, 1.35f, .9f), task,
                ChapterThreeInteractionKind.TaskStation, 0, "搬运行李", "", 1, ChapterThreeTaskKind.Carry);
            CreateProp("清扫任务点", new Vector3(.05f, .44f, 6.20f), new Vector3(.8f, .55f, .8f), task,
                ChapterThreeInteractionKind.TaskStation, 1, "整理客房", "", 1, ChapterThreeTaskKind.Clean);
            // Put the typing station on the accessible reception counter face;
            // the old rear-room position was routinely occluded by the bed and
            // wall colliders, making the task impossible to aim at.
            CreateProp("录入任务点", new Vector3(-4.55f, 1.48f, -4.28f), new Vector3(.82f, .22f, .58f), task,
                ChapterThreeInteractionKind.TaskStation, 2, "录入实践材料", "", 1, ChapterThreeTaskKind.Type);
            CreateProp("一笔画任务点", new Vector3(9.62f, 1.00f, 4.28f), new Vector3(.10f, 1.05f, 1.05f), task,
                ChapterThreeInteractionKind.TaskStation, 3, "一笔画声纹", "", 1, ChapterThreeTaskKind.OneStroke);

            CreateProp("民宿餐食", new Vector3(4.96f, 1.02f, -4.82f), new Vector3(.46f, .10f, .38f), paper,
                ChapterThreeInteractionKind.Meal, 0, "用餐（8.33秒）", "");
            CreateProp("民宿灯控", new Vector3(2.86f, 1.38f, 5.02f), new Vector3(.10f, .26f, .18f), warning,
                ChapterThreeInteractionKind.LightSwitch, 0, "切换民宿灯光", "");
            CreateProp("墙上机械钟", new Vector3(-9.62f, 2.48f, 4.26f), new Vector3(.10f, .62f, .62f), card,
                ChapterThreeInteractionKind.Clock, 0, "查看时间规则", "");
            fundingProp = CreateProp("第八天保障期限", new Vector3(8.42f, 4.98f, 8.12f), new Vector3(.66f, .06f, .46f), warning,
                ChapterThreeInteractionKind.FundingClue, 0, "实践保障说明", "", 8);
            departureProp = CreateProp("第九天离开民宿", new Vector3(0f, 1.25f, -15.05f), new Vector3(1.4f, 2.5f, .18f), task,
                ChapterThreeInteractionKind.Departure, 0, "结束九日实践", "", 9);
        }

        private void BuildHousingSchedule()
        {
            List<int> days = new(9);
            for (int day = 1; day <= 9; day++) days.Add(day);
            System.Random random = new(MeixiLocalProgress.ChapterThreeHousingScheduleSeed);
            for (int i = days.Count - 1; i > 0; i--)
            {
                int swap = random.Next(i + 1);
                (days[i], days[swap]) = (days[swap], days[i]);
            }
            for (int i = 0; i < ChapterThreeRules.HousingClueCount; i++)
            {
                housingSpawnDays[i] = days[i];
                housingSpawnRooms[i] = random.Next(0, 6);
            }
            Debug.Log($"[MeixiChapterThree] Housing schedule: " +
                      string.Join(", ", System.Array.ConvertAll(housingSpawnDays, day => day.ToString())));
        }

        private void RefreshDailyHousingClue()
        {
            for (int i = dailyHousingProps.Count - 1; i >= 0; i--)
            {
                ChapterThreeInteractable old = dailyHousingProps[i];
                interactables.Remove(old);
                if (old != null) Destroy(old.gameObject);
            }
            dailyHousingProps.Clear();
            if (housingCardMaterial == null || endingSequence || finalComplete) return;

            for (int clue = 0; clue < ChapterThreeRules.HousingClueCount; clue++)
            {
                if (housingSpawnDays[clue] != currentDay || (housingMask & (1 << clue)) != 0) continue;
                int room = Mathf.Clamp(housingSpawnRooms[clue], 0, 5);
                int roomNumber = (room >= 3 ? 201 : 101) + room % 3;
                Transform wardrobeParent = FindWardrobeParentByRoomNumber(roomNumber);
                // Housing cards and truth evidence now share the same explicit
                // bed-free rule. The card sits above the drawers, offset from the
                // truth sheet, and is only visible after the wardrobe is opened.
                // Never fall back to a bedside or bed-foot coordinate.
                Vector3 spawn = wardrobeParent != null
                    ? new Vector3(.24f, .72f, -.30f)
                    : new Vector3(-4.72f + clue * .16f, 1.52f, -3.82f);
                ChapterThreeInteractable prop = CreateProp("住房线索_" + clue, spawn,
                    new Vector3(.52f, .08f, .36f), housingCardMaterial,
                    ChapterThreeInteractionKind.HousingClue, clue, HousingTitles[clue], HousingBodies[clue],
                    1, ChapterThreeTaskKind.Carry, wardrobeParent);
                dailyHousingProps.Add(prop);
            }
        }

        private ChapterThreeInteractable CreateProp(string name, Vector3 position, Vector3 scale, Material material,
            ChapterThreeInteractionKind kind, int id, string title, string body, int minimumDay = 1,
            ChapterThreeTaskKind taskKind = ChapterThreeTaskKind.Carry, Transform parent = null)
        {
            GameObject prop = GameObject.CreatePrimitive(PrimitiveType.Cube);
            prop.name = name;
            prop.transform.SetParent(parent != null ? parent : transform, false);
            prop.transform.localPosition = position;
            prop.transform.localScale = scale;
            Renderer renderer = prop.GetComponent<Renderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            DecorateGameplayProp(prop, kind, id, taskKind);
            ChapterThreeInteractable interactable = prop.AddComponent<ChapterThreeInteractable>();
            interactable.Configure(kind, id, title, body, minimumDay, taskKind);
            BoxCollider interactionCollider = prop.GetComponent<BoxCollider>();
            if (interactionCollider != null)
            {
                // Visual props remain small and precise, while the invisible
                // interaction volume stays comfortable for first-person aiming.
                float reach = kind == ChapterThreeInteractionKind.TaskStation ? 1.45f : .90f;
                interactionCollider.size = new Vector3(
                    Mathf.Max(1.25f, reach / Mathf.Max(.01f, scale.x)),
                    Mathf.Max(1.25f, reach / Mathf.Max(.01f, scale.y)),
                    Mathf.Max(1.25f, reach / Mathf.Max(.01f, scale.z)));
                // Task/clue markers should never become invisible furniture
                // blockers. Their trigger volume is used only by the interaction
                // sphere cast and is ignored by the character controller.
                interactionCollider.isTrigger = true;
            }
            interactables.Add(interactable);
            return interactable;
        }

        private static void DecorateGameplayProp(GameObject prop, ChapterThreeInteractionKind kind, int id,
            ChapterThreeTaskKind taskKind)
        {
            if (prop == null) return;
            Texture2D atlas = null;
            int tile = 0;
            bool taskMarker = kind == ChapterThreeInteractionKind.TaskStation || kind == ChapterThreeInteractionKind.Departure;
            bool evidence = kind == ChapterThreeInteractionKind.TruthClue || kind == ChapterThreeInteractionKind.HousingClue ||
                kind == ChapterThreeInteractionKind.FundingClue;
            if (taskMarker)
            {
                atlas = Resources.Load<Texture2D>("ChapterThree/TaskMarkerAtlas");
                tile = kind == ChapterThreeInteractionKind.Departure ? 0 : Mathf.Abs((int)taskKind) % 4;
            }
            else if (evidence)
            {
                atlas = Resources.Load<Texture2D>("ChapterThree/EvidenceStickerAtlas");
                tile = Mathf.Abs(id) % 4;
            }
            if (atlas == null) return;

            Material sticker = CreateAtlasStickerMaterial($"Chapter3_{kind}_Sticker_{tile}", atlas, tile);
            Texture2D surfaceAtlas = Resources.Load<Texture2D>("ChapterThree/PropMaterialAtlas");
            Material leather = CreateAtlasSurfaceMaterial("Chapter3_PropLeather", surfaceAtlas, 0, .28f);
            Material wood = CreateAtlasSurfaceMaterial("Chapter3_PropIndigoWood", surfaceAtlas, 1, .22f);
            Material linen = CreateAtlasSurfaceMaterial("Chapter3_PropLinen", surfaceAtlas, 2, .08f);
            Material metal = CreateAtlasSurfaceMaterial("Chapter3_PropMetal", surfaceAtlas, 3, .62f);
            Renderer rootRenderer = prop.GetComponent<Renderer>();
            if (rootRenderer != null) rootRenderer.enabled = false;

            if (taskMarker)
            {
                if (kind == ChapterThreeInteractionKind.Departure)
                    BuildDepartureMarker(prop.transform, wood, metal, sticker);
                else if (taskKind == ChapterThreeTaskKind.Carry)
                    BuildLuggageTaskProp(prop.transform, leather, metal, sticker);
                else if (taskKind == ChapterThreeTaskKind.Clean)
                    BuildCleaningTaskProp(prop.transform, wood, linen, metal, sticker);
                else if (taskKind == ChapterThreeTaskKind.Type)
                    BuildTypingTaskProp(prop.transform, wood, metal, sticker);
                else
                    BuildSoundTaskProp(prop.transform, wood, metal, sticker);
            }
            else
            {
                BuildEvidenceProp(prop.transform, kind, linen, wood, metal, sticker);
            }
        }

        private static void BuildLuggageTaskProp(Transform root, Material leather, Material metal, Material sticker)
        {
            GameObject caseBody = AddPropPrimitive(PrimitiveType.Cube, root, "仿真皮箱箱体", new Vector3(0f, -.06f, 0f),
                new Vector3(.72f, .56f, .62f), Quaternion.identity, leather);
            AddPropPrimitive(PrimitiveType.Cube, root, "皮箱左束带", new Vector3(-.22f, -.06f, -.015f), new Vector3(.075f, .58f, .65f), Quaternion.identity, metal);
            AddPropPrimitive(PrimitiveType.Cube, root, "皮箱右束带", new Vector3(.22f, -.06f, -.015f), new Vector3(.075f, .58f, .65f), Quaternion.identity, metal);
            AddPropPrimitive(PrimitiveType.Cube, root, "行李车底盘", new Vector3(0f, -.39f, .03f), new Vector3(.88f, .09f, .72f), Quaternion.identity, metal);
            for (int side = -1; side <= 1; side += 2)
            {
                AddPropPrimitive(PrimitiveType.Cylinder, root, "行李车轮", new Vector3(side * .34f, -.48f, -.21f),
                    new Vector3(.14f, .09f, .14f), Quaternion.Euler(0f, 0f, 90f), metal);
                AddPropPrimitive(PrimitiveType.Cube, root, "拉杆立柱", new Vector3(side * .20f, .45f, .20f),
                    new Vector3(.045f, .72f, .045f), Quaternion.identity, metal);
            }
            AddPropPrimitive(PrimitiveType.Cube, root, "拉杆握把", new Vector3(0f, .78f, .20f), new Vector3(.45f, .055f, .065f), Quaternion.identity, metal);
            AddStickerFace(caseBody.transform, "任务贴纸_行李标签", new Vector3(0f, 0f, -.515f), Quaternion.identity, new Vector3(.78f, .76f, 1f), sticker);
            AddStickerFace(caseBody.transform, "任务贴纸_行李背标", new Vector3(0f, 0f, .515f), Quaternion.Euler(0f, 180f, 0f), new Vector3(.78f, .76f, 1f), sticker);
        }

        private static void BuildCleaningTaskProp(Transform root, Material wood, Material linen, Material metal, Material sticker)
        {
            GameObject crate = AddPropPrimitive(PrimitiveType.Cube, root, "清洁工具篮", new Vector3(0f, -.16f, 0f),
                new Vector3(.80f, .48f, .68f), Quaternion.identity, wood);
            AddPropPrimitive(PrimitiveType.Cube, root, "折叠毛巾", new Vector3(.14f, .16f, .05f), new Vector3(.40f, .13f, .38f), Quaternion.Euler(0f, -8f, 0f), linen);
            AddPropPrimitive(PrimitiveType.Cylinder, root, "清洁喷瓶", new Vector3(-.19f, .16f, .05f), new Vector3(.16f, .30f, .16f), Quaternion.identity, metal);
            AddPropPrimitive(PrimitiveType.Cylinder, root, "扫帚木杆", new Vector3(.31f, .63f, .13f), new Vector3(.055f, 1.16f, .055f), Quaternion.Euler(0f, 0f, -13f), wood);
            AddPropPrimitive(PrimitiveType.Cube, root, "扫帚刷头", new Vector3(.43f, .08f, .13f), new Vector3(.34f, .22f, .22f), Quaternion.Euler(0f, 0f, -13f), linen);
            for (int side = -1; side <= 1; side += 2)
                AddPropPrimitive(PrimitiveType.Cylinder, root, "清洁车轮", new Vector3(side * .31f, -.44f, -.20f),
                    new Vector3(.13f, .08f, .13f), Quaternion.Euler(0f, 0f, 90f), metal);
            AddStickerFace(crate.transform, "任务贴纸_清扫正面", new Vector3(0f, 0f, -.515f), Quaternion.identity, new Vector3(.82f, .74f, 1f), sticker);
        }

        private static void BuildTypingTaskProp(Transform root, Material wood, Material metal, Material sticker)
        {
            AddPropPrimitive(PrimitiveType.Cube, root, "录入设备底座", new Vector3(0f, -.22f, 0f),
                new Vector3(.90f, .16f, .82f), Quaternion.identity, wood);
            GameObject screen = AddPropPrimitive(PrimitiveType.Cube, root, "笔记本显示屏", new Vector3(0f, .20f, .26f),
                new Vector3(.86f, .62f, .075f), Quaternion.Euler(-9f, 0f, 0f), metal);
            AddStickerFace(screen.transform, "任务贴纸_录入屏幕", new Vector3(0f, 0f, -.515f), Quaternion.identity, new Vector3(.84f, .80f, 1f), sticker);
            GameObject keyboard = AddPropPrimitive(PrimitiveType.Cube, root, "笔记本键盘", new Vector3(0f, -.10f, -.13f),
                new Vector3(.82f, .055f, .60f), Quaternion.Euler(5f, 0f, 0f), metal);
            for (int row = 0; row < 3; row++)
            for (int col = 0; col < 7; col++)
                AddPropPrimitive(PrimitiveType.Cube, keyboard.transform, "录入键帽", new Vector3(-.34f + col * .112f, .56f, -.26f + row * .21f),
                    new Vector3(.085f, .08f, .12f), Quaternion.identity, wood);
        }

        private static void BuildSoundTaskProp(Transform root, Material wood, Material metal, Material sticker)
        {
            GameObject panel = AddPropPrimitive(PrimitiveType.Cube, root, "声纹仪器面板", Vector3.zero,
                new Vector3(.52f, .82f, .82f), Quaternion.identity, metal);
            AddStickerFace(panel.transform, "任务贴纸_声纹左面", new Vector3(-.515f, 0f, 0f), Quaternion.Euler(0f, 90f, 0f), new Vector3(.86f, .86f, 1f), sticker);
            AddStickerFace(panel.transform, "任务贴纸_声纹右面", new Vector3(.515f, 0f, 0f), Quaternion.Euler(0f, -90f, 0f), new Vector3(.86f, .86f, 1f), sticker);
            for (int side = -1; side <= 1; side += 2)
            {
                AddPropPrimitive(PrimitiveType.Cube, root, "声纹木框竖边", new Vector3(0f, 0f, side * .46f), new Vector3(.70f, .96f, .065f), Quaternion.identity, wood);
                AddPropPrimitive(PrimitiveType.Cube, root, "声纹木框横边", new Vector3(0f, side * .46f, 0f), new Vector3(.70f, .065f, .96f), Quaternion.identity, wood);
            }
            AddPropPrimitive(PrimitiveType.Sphere, root, "声纹状态灯", new Vector3(-.36f, -.34f, -.39f), new Vector3(.18f, .055f, .055f), Quaternion.identity, metal);
        }

        private static void BuildDepartureMarker(Transform root, Material wood, Material metal, Material sticker)
        {
            for (int side = -1; side <= 1; side += 2)
                AddPropPrimitive(PrimitiveType.Cube, root, "离店指示木柱", new Vector3(side * .34f, -.02f, 0f),
                    new Vector3(.11f, .92f, .55f), Quaternion.identity, wood);
            GameObject plaque = AddPropPrimitive(PrimitiveType.Cube, root, "离店档案牌", new Vector3(0f, .18f, 0f),
                new Vector3(.72f, .38f, .68f), Quaternion.identity, metal);
            AddStickerFace(plaque.transform, "任务贴纸_离店正面", new Vector3(0f, 0f, -.515f), Quaternion.identity, new Vector3(.82f, .82f, 1f), sticker);
            AddStickerFace(plaque.transform, "任务贴纸_离店背面", new Vector3(0f, 0f, .515f), Quaternion.Euler(0f, 180f, 0f), new Vector3(.82f, .82f, 1f), sticker);
            AddPropPrimitive(PrimitiveType.Cube, root, "离店牌横梁", new Vector3(0f, .48f, 0f), new Vector3(.90f, .10f, .58f), Quaternion.identity, wood);
        }

        private static void BuildEvidenceProp(Transform root, ChapterThreeInteractionKind kind, Material linen,
            Material wood, Material metal, Material sticker)
        {
            Material baseMaterial = kind == ChapterThreeInteractionKind.HousingClue ? wood : linen;
            GameObject backing = AddPropPrimitive(PrimitiveType.Cube, root, kind == ChapterThreeInteractionKind.HousingClue ? "住房登记夹板" : "线索纸张底页",
                new Vector3(.03f, -.04f, .02f), new Vector3(.98f, .56f, .96f), Quaternion.Euler(0f, -2.5f, 0f), baseMaterial);
            GameObject paper = AddPropPrimitive(PrimitiveType.Cube, root, "线索纸张正文", new Vector3(-.02f, .17f, -.02f),
                new Vector3(.92f, .26f, .90f), Quaternion.Euler(0f, 2f, 0f), linen);
            AddStickerFace(paper.transform, "线索纸面", new Vector3(0f, .515f, 0f), Quaternion.Euler(90f, 0f, 0f), new Vector3(.94f, .92f, 1f), sticker);
            AddPropPrimitive(PrimitiveType.Cube, root, "线索金属夹", new Vector3(0f, .36f, .38f), new Vector3(.34f, .20f, .11f), Quaternion.identity, metal);
            if (kind == ChapterThreeInteractionKind.TruthClue)
                AddPropPrimitive(PrimitiveType.Sphere, root, "线索图钉", new Vector3(.32f, .37f, -.31f), new Vector3(.09f, .16f, .09f), Quaternion.identity, metal);
        }

        private static GameObject AddPropPrimitive(PrimitiveType primitive, Transform parent, string name,
            Vector3 localPosition, Vector3 localScale, Quaternion localRotation, Material material)
        {
            GameObject part = GameObject.CreatePrimitive(primitive);
            part.name = name;
            part.transform.SetParent(parent, false);
            part.transform.localPosition = localPosition;
            part.transform.localRotation = localRotation;
            part.transform.localScale = primitive == PrimitiveType.Cylinder
                ? new Vector3(localScale.x, localScale.y * .5f, localScale.z)
                : localScale;
            Renderer renderer = part.GetComponent<Renderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            UnityEngine.Object.Destroy(part.GetComponent<Collider>());
            return part;
        }

        private static Material CreateAtlasSurfaceMaterial(string name, Texture2D texture, int tile, float smoothness)
        {
            if (texture == null) return CreateMaterial(name, Color.white, smoothness);
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            Material material = new(shader) { name = name, color = Color.white };
            Vector2 scale = new(.5f, .5f);
            Vector2 offset = new((tile & 1) == 0 ? 0f : .5f, tile < 2 ? .5f : 0f);
            if (material.HasProperty("_BaseMap"))
            {
                material.SetTexture("_BaseMap", texture);
                material.SetTextureScale("_BaseMap", scale);
                material.SetTextureOffset("_BaseMap", offset);
            }
            if (material.HasProperty("_MainTex"))
            {
                material.SetTexture("_MainTex", texture);
                material.SetTextureScale("_MainTex", scale);
                material.SetTextureOffset("_MainTex", offset);
            }
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", smoothness);
            return material;
        }

        private static Material CreateAtlasStickerMaterial(string name, Texture2D texture, int tile)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Texture") ?? Shader.Find("Standard");
            Material material = new(shader) { name = name, color = Color.white };
            Vector2 scale = new(.5f, .5f);
            Vector2 offset = new((tile & 1) == 0 ? 0f : .5f, tile < 2 ? .5f : 0f);
            if (material.HasProperty("_BaseMap"))
            {
                material.SetTexture("_BaseMap", texture);
                material.SetTextureScale("_BaseMap", scale);
                material.SetTextureOffset("_BaseMap", offset);
            }
            if (material.HasProperty("_MainTex"))
            {
                material.SetTexture("_MainTex", texture);
                material.SetTextureScale("_MainTex", scale);
                material.SetTextureOffset("_MainTex", offset);
            }
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", Color.white);
            return material;
        }

        private static void AddStickerFace(Transform parent, string name, Vector3 localPosition,
            Quaternion localRotation, Vector3 localScale, Material material)
        {
            GameObject sticker = GameObject.CreatePrimitive(PrimitiveType.Quad);
            sticker.name = name;
            sticker.transform.SetParent(parent, false);
            sticker.transform.localPosition = localPosition;
            sticker.transform.localRotation = localRotation;
            sticker.transform.localScale = localScale;
            Renderer renderer = sticker.GetComponent<Renderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.sortingOrder = 8;
            Destroy(sticker.GetComponent<Collider>());
        }

        private Transform FindWardrobeParentByRoomNumber(int roomNumber)
        {
            ChapterThreeInteractable[] fixtures = GetComponentsInChildren<ChapterThreeInteractable>(true);
            for (int i = 0; i < fixtures.Length; i++)
            {
                ChapterThreeInteractable fixture = fixtures[i];
                if (fixture != null && fixture.Kind == ChapterThreeInteractionKind.Wardrobe && fixture.Id == roomNumber)
                    return fixture.transform;
            }
            return null;
        }

        private void AttachToExisting(string name, ChapterThreeInteractionKind kind, string title, string body)
        {
            GameObject target = GameObject.Find(name);
            if (target == null) return;
            ChapterThreeInteractable item = target.GetComponent<ChapterThreeInteractable>() ?? target.AddComponent<ChapterThreeInteractable>();
            item.Configure(kind, 0, title, body);
            interactables.Add(item);
        }

        private void RestoreCollectedProps()
        {
            for (int i = 0; i < interactables.Count; i++)
            {
                ChapterThreeInteractable item = interactables[i];
                if (item.Kind == ChapterThreeInteractionKind.TruthClue && (truthMask & (1 << item.Id)) != 0) item.SetConsumed(true);
                else if (item.Kind == ChapterThreeInteractionKind.HousingClue && (housingMask & (1 << item.Id)) != 0) item.SetConsumed(true);
                else if (item.Kind == ChapterThreeInteractionKind.FundingClue && fundingFound) item.SetConsumed(true);
            }
            CheckEvidenceAchievements();
        }

        private void BuildUi()
        {
            uiFont = Font.CreateDynamicFontFromOSFont(new[] { "Microsoft YaHei", "SimHei", "Arial" }, 32);
            GameObject canvasObject = new("ChapterThree_UI");
            canvasObject.transform.SetParent(transform, false);
            canvas = canvasObject.AddComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 40;
            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(1920f, 1080f); scaler.matchWidthOrHeight = .5f;
            canvasObject.AddComponent<GraphicRaycaster>();
            if (EventSystem.current == null)
            {
                GameObject eventSystem = new("ChapterThree_EventSystem"); eventSystem.transform.SetParent(transform, false);
                eventSystem.AddComponent<EventSystem>(); eventSystem.AddComponent<InputSystemUIInputModule>();
            }

            GameObject hud = Panel("第三章HUD", canvas.transform, new Color(.008f, .025f, .030f, .78f), new Vector2(.018f, .755f), new Vector2(.46f, .978f));
            Outline hudOutline = hud.AddComponent<Outline>(); hudOutline.effectColor = new Color(.18f, .72f, .63f, .38f); hudOutline.effectDistance = new Vector2(1.2f, -1.2f);
            Shadow hudShadow = hud.AddComponent<Shadow>(); hudShadow.effectColor = new Color(0f, 0f, 0f, .62f); hudShadow.effectDistance = new Vector2(0f, -7f);
            hud.AddComponent<MeixiUiPanelReveal>().Configure(.20f, .97f);
            Panel("HUD顶端金线", hud.transform, new Color(1f, .58f, .20f, .82f), new Vector2(.025f, .965f), new Vector2(.975f, .978f));
            Panel("HUD左侧青线", hud.transform, new Color(.18f, .78f, .68f, .75f), new Vector2(.020f, .08f), new Vector2(.026f, .91f));
            Label("HUD眉题", hud.transform, "MEIXI HOMESTAY  /  FIELD ARCHIVE", 12, TextAnchor.MiddleLeft,
                new Color(.43f, .74f, .68f), new Vector2(.045f, .88f), new Vector2(.55f, .965f));
            dayClockText = Label("日期与时间", hud.transform, "", 26, TextAnchor.MiddleLeft, new Color(1f, .79f, .38f), new Vector2(.045f, .69f), new Vector2(.56f, .89f));
            sanityText = Label("理智文字", hud.transform, "", 21, TextAnchor.MiddleRight, Color.white, new Vector2(.59f, .74f), new Vector2(.95f, .93f));
            GameObject sanityBack = Panel("理智槽背景", hud.transform, new Color(.02f, .05f, .06f, .95f), new Vector2(.59f, .66f), new Vector2(.95f, .72f));
            GameObject sanityBar = Panel("理智槽", sanityBack.transform, new Color(.22f, .86f, .72f), Vector2.zero, Vector2.one);
            sanityFill = sanityBar.GetComponent<Image>(); sanityFill.type = Image.Type.Filled; sanityFill.fillMethod = Image.FillMethod.Horizontal;
            objectiveText = Label("第三章目标", hud.transform, "", 18, TextAnchor.UpperLeft, new Color(.82f, .92f, .91f), new Vector2(.045f, .39f), new Vector2(.95f, .64f));
            taskText = Label("今日任务", hud.transform, "", 17, TextAnchor.UpperLeft, new Color(.60f, .88f, .80f), new Vector2(.045f, .18f), new Vector2(.95f, .39f));
            evidenceText = Label("线索进度", hud.transform, "", 16, TextAnchor.MiddleLeft, new Color(.90f, .73f, .45f), new Vector2(.045f, .02f), new Vector2(.95f, .18f));

            promptPanel = Panel("互动提示胶囊", canvas.transform, new Color(.012f, .075f, .075f, .93f), new Vector2(.22f, .065f), new Vector2(.78f, .15f));
            Outline promptOutline = promptPanel.AddComponent<Outline>(); promptOutline.effectColor = new Color(.12f, .82f, .68f, .55f); promptOutline.effectDistance = new Vector2(1.5f, 1.5f);
            promptPanel.AddComponent<MeixiUiPanelReveal>().Configure(.16f, .96f);
            Panel("互动提示金线", promptPanel.transform, new Color(1f, .58f, .20f, .82f), new Vector2(.02f, .17f), new Vector2(.025f, .83f));
            promptText = Label("互动提示", promptPanel.transform, "", 23, TextAnchor.MiddleCenter, new Color(.92f, 1f, .96f), new Vector2(.035f, .05f), new Vector2(.965f, .95f));
            promptText.gameObject.AddComponent<Outline>().effectColor = new Color(0f, 0f, 0f, .9f);
            Label("准星", canvas.transform, "+", 23, TextAnchor.MiddleCenter, new Color(.90f, 1f, .96f, .86f), new Vector2(.485f, .485f), new Vector2(.515f, .535f));
            toastPanel = Panel("第三章Toast底", canvas.transform, new Color(.025f, .055f, .055f, .92f), new Vector2(.22f, .835f), new Vector2(.78f, .92f));
            Outline toastOutline = toastPanel.AddComponent<Outline>(); toastOutline.effectColor = new Color(1f, .58f, .20f, .42f); toastOutline.effectDistance = new Vector2(1f, 1f);
            toastPanel.AddComponent<MeixiUiPanelReveal>().Configure(.18f, .94f);
            Panel("Toast青线", toastPanel.transform, new Color(.20f, .78f, .68f, .66f), new Vector2(.06f, .91f), new Vector2(.94f, .94f));
            toastText = Label("第三章Toast", toastPanel.transform, "", 22, TextAnchor.MiddleCenter, new Color(1f, .82f, .42f), new Vector2(.035f, .05f), new Vector2(.965f, .95f));
            toastPanel.SetActive(false);
            toastText.gameObject.SetActive(false);

            vignette = Panel("理智暗角", canvas.transform, Color.clear, Vector2.zero, Vector2.one).GetComponent<Image>();
            vignette.raycastTarget = false;

            modalPanel = Panel("第三章剧情模态层", canvas.transform, new Color(.002f, .008f, .012f, .94f), Vector2.zero, Vector2.one);
            GameObject modalCard = Panel("剧情卡", modalPanel.transform, new Color(.025f, .052f, .058f, .98f), new Vector2(.20f, .18f), new Vector2(.80f, .82f));
            Outline modalOutline = modalCard.AddComponent<Outline>(); modalOutline.effectColor = new Color(.22f, .72f, .65f, .44f); modalOutline.effectDistance = new Vector2(2f, -2f);
            Shadow modalShadow = modalCard.AddComponent<Shadow>(); modalShadow.effectColor = new Color(0f, 0f, 0f, .86f); modalShadow.effectDistance = new Vector2(0f, -14f);
            modalCard.AddComponent<MeixiUiPanelReveal>().Configure(.23f, .91f);
            Panel("剧情卡左金线", modalCard.transform, new Color(1f, .58f, .20f, .82f), new Vector2(.055f, .14f), new Vector2(.060f, .87f));
            Panel("剧情卡顶青线", modalCard.transform, new Color(.20f, .72f, .66f, .58f), new Vector2(.055f, .935f), new Vector2(.78f, .939f));
            Label("剧情眉题", modalCard.transform, "MEIXI LOCAL ARCHIVE  /  实践记录", 14, TextAnchor.MiddleLeft,
                new Color(.42f, .77f, .70f), new Vector2(.09f, .88f), new Vector2(.88f, .95f));
            modalTitle = Label("剧情标题", modalCard.transform, "", 36, TextAnchor.MiddleCenter, new Color(1f, .76f, .32f), new Vector2(.07f, .73f), new Vector2(.93f, .88f));
            modalBody = Label("剧情正文", modalCard.transform, "", 22, TextAnchor.UpperLeft, new Color(.84f, .91f, .89f), new Vector2(.09f, .29f), new Vector2(.91f, .75f));
            modalBody.lineSpacing = 1.22f;
            GameObject mediaObject = new("广告栏全貌");
            mediaObject.transform.SetParent(modalCard.transform, false);
            modalMediaImage = mediaObject.AddComponent<RawImage>();
            modalMediaImage.color = Color.white;
            modalMediaImage.raycastTarget = false;
            modalMediaAspect = mediaObject.AddComponent<AspectRatioFitter>();
            modalMediaAspect.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            modalMediaAspect.aspectRatio = 1.6f;
            RectTransform mediaRect = mediaObject.GetComponent<RectTransform>();
            mediaRect.anchorMin = new Vector2(.075f, .245f);
            mediaRect.anchorMax = new Vector2(.925f, .755f);
            mediaRect.offsetMin = Vector2.zero;
            mediaRect.offsetMax = Vector2.zero;
            mediaObject.SetActive(false);
            modalLeftButton = UiButton("左选择", modalCard.transform, "继续", new Vector2(.09f, .08f), new Vector2(.47f, .23f), out modalLeftLabel);
            modalRightButton = UiButton("右选择", modalCard.transform, "", new Vector2(.53f, .08f), new Vector2(.91f, .23f), out modalRightLabel);
            modalLeftButton.onClick.AddListener(InvokeModalLeft); modalRightButton.onClick.AddListener(InvokeModalRight);
            modalPanel.SetActive(false);

            miniGamePanel = Panel("第三章任务层", canvas.transform, new Color(.001f, .006f, .009f, .84f), Vector2.zero, Vector2.one);
            GameObject taskCard = Panel("任务档案卡", miniGamePanel.transform, new Color(.014f, .040f, .043f, .985f), new Vector2(.18f, .10f), new Vector2(.82f, .90f));
            Outline taskOutline = taskCard.AddComponent<Outline>(); taskOutline.effectColor = new Color(.20f, .78f, .67f, .55f); taskOutline.effectDistance = new Vector2(2f, -2f);
            Shadow taskShadow = taskCard.AddComponent<Shadow>(); taskShadow.effectColor = new Color(0f, 0f, 0f, .88f); taskShadow.effectDistance = new Vector2(0f, -16f);
            taskCard.AddComponent<MeixiUiPanelReveal>().Configure(.22f, .91f);
            Panel("任务卡顶金线", taskCard.transform, new Color(1f, .58f, .20f, .92f), new Vector2(.055f, .947f), new Vector2(.70f, .955f));
            Panel("任务卡右青线", taskCard.transform, new Color(.18f, .78f, .68f, .75f), new Vector2(.945f, .10f), new Vector2(.950f, .88f));
            Label("任务眉题", taskCard.transform, "FIELDWORK PROCEDURE  /  实践作业", 13, TextAnchor.MiddleLeft,
                new Color(.43f, .76f, .70f), new Vector2(.07f, .88f), new Vector2(.72f, .95f));
            miniGameTitle = Label("任务标题", taskCard.transform, "", 36, TextAnchor.MiddleCenter, new Color(1f, .75f, .30f), new Vector2(.07f, .78f), new Vector2(.93f, .90f));
            miniGameInstruction = Label("任务说明", taskCard.transform, "", 19, TextAnchor.MiddleCenter, new Color(.82f, .92f, .90f), new Vector2(.08f, .66f), new Vector2(.92f, .79f));
            miniGameStageText = Label("任务阶段", taskCard.transform, "阶段 1 / 4", 16, TextAnchor.MiddleLeft,
                new Color(.48f, .82f, .75f), new Vector2(.10f, .59f), new Vector2(.90f, .65f));
            GameObject progressBack = Panel("任务进度背景", taskCard.transform, new Color(.006f, .020f, .023f, .98f), new Vector2(.10f, .51f), new Vector2(.90f, .57f));
            Outline progressOutline = progressBack.AddComponent<Outline>(); progressOutline.effectColor = new Color(.24f, .62f, .58f, .55f); progressOutline.effectDistance = new Vector2(1f, -1f);
            miniGameFill = Panel("任务进度", progressBack.transform, new Color(.18f, .82f, .68f), Vector2.zero, Vector2.one).GetComponent<Image>();
            miniGameFill.type = Image.Type.Filled; miniGameFill.fillMethod = Image.FillMethod.Horizontal;
            miniGameFill.sprite = MeixiUiSprites.White;
            miniGameCarryTarget = Panel("行李中央亮区", progressBack.transform, new Color(.20f, .86f, .50f, .44f), new Vector2(.42f, .12f), new Vector2(.60f, .88f));
            miniGameCarryTarget.GetComponent<Image>().raycastTarget = false;
            GameObject cursor = Panel("行李移动准星", progressBack.transform, new Color(1f, .94f, .62f, 1f), new Vector2(.495f, .05f), new Vector2(.505f, .95f));
            cursor.GetComponent<Image>().raycastTarget = false;
            miniGameCarryCursor = cursor.GetComponent<RectTransform>();
            for (int i = 1; i <= 3; i++)
            {
                float x = i * .25f;
                Image divider = Panel("任务阶段刻度_" + i, progressBack.transform, new Color(1f, .78f, .34f, .46f),
                    new Vector2(x - .0015f, .08f), new Vector2(x + .0015f, .92f)).GetComponent<Image>();
                divider.raycastTarget = false;
            }
            SetCarryTaskVisual(false);
            miniGameFeedbackText = Label("任务即时反馈", taskCard.transform, "准备就绪", 17, TextAnchor.MiddleCenter,
                new Color(.52f, .80f, .75f), new Vector2(.10f, .44f), new Vector2(.90f, .50f));

            miniCarryMouseControls = Panel("行李鼠标控制", taskCard.transform, Color.clear, new Vector2(.10f, .19f), new Vector2(.90f, .41f));
            miniCarryMouseControls.GetComponent<Image>().raycastTarget = false;
            Button carryClick = UiButton("鼠标固定行李", miniCarryMouseControls.transform, "点击固定行李  ·  CLICK",
                new Vector2(.26f, .08f), new Vector2(.74f, .92f), out Text carryClickLabel);
            carryClick.onClick.AddListener(() => QueueMiniMouseInput(' '));
            carryClickLabel.fontSize = 17;

            miniCleanMouseControls = Panel("清扫鼠标控制", taskCard.transform, Color.clear, new Vector2(.10f, .19f), new Vector2(.90f, .41f));
            miniCleanMouseControls.GetComponent<Image>().raycastTarget = false;
            Button cleanLeft = UiButton("向左清扫", miniCleanMouseControls.transform, "←  左侧清扫  [A]",
                new Vector2(.10f, .08f), new Vector2(.47f, .92f), out _);
            Button cleanRight = UiButton("向右清扫", miniCleanMouseControls.transform, "右侧清扫  [D]  →",
                new Vector2(.53f, .08f), new Vector2(.90f, .92f), out _);
            cleanLeft.onClick.AddListener(() => QueueMiniMouseInput('A'));
            cleanRight.onClick.AddListener(() => QueueMiniMouseInput('D'));

            miniTypeMouseControls = Panel("录入键盘区域", taskCard.transform, Color.clear, new Vector2(.10f, .19f), new Vector2(.90f, .41f));
            miniTypeMouseControls.GetComponent<Image>().raycastTarget = false;
            GameObject typingBox = Panel("实践文本输入框", miniTypeMouseControls.transform, new Color(.006f, .022f, .024f, .98f), new Vector2(.02f, .18f), new Vector2(.98f, .82f));
            Outline typingOutline = typingBox.AddComponent<Outline>(); typingOutline.effectColor = new Color(.22f, .75f, .66f, .70f); typingOutline.effectDistance = new Vector2(2f, -2f);
            miniTypingInput = typingBox.AddComponent<InputField>();
            miniTypingInput.targetGraphic = typingBox.GetComponent<Image>();
            miniTypingInput.lineType = InputField.LineType.SingleLine;
            miniTypingInput.contentType = InputField.ContentType.Standard;
            miniTypingInput.caretColor = new Color(1f, .72f, .28f);
            miniTypingInput.selectionColor = new Color(.18f, .70f, .62f, .45f);
            Text typingText = Label("录入正文", typingBox.transform, string.Empty, 22, TextAnchor.MiddleLeft,
                new Color(.86f, .96f, .92f), new Vector2(.035f, .08f), new Vector2(.965f, .92f));
            Text typingPlaceholder = Label("录入占位提示", typingBox.transform, "请直接使用键盘输入上方完整文本……", 18, TextAnchor.MiddleLeft,
                new Color(.42f, .58f, .56f), new Vector2(.035f, .08f), new Vector2(.965f, .92f));
            miniTypingInput.textComponent = typingText;
            miniTypingInput.placeholder = typingPlaceholder;
            miniTypingInput.onValueChanged.AddListener(OnTypingValueChanged);

            miniStrokeMouseControls = Panel("一笔画连线画布", taskCard.transform, new Color(.006f, .022f, .024f, .96f), new Vector2(.14f, .15f), new Vector2(.86f, .43f));
            miniStrokeMouseControls.GetComponent<Image>().raycastTarget = false;
            Outline strokeOutline = miniStrokeMouseControls.AddComponent<Outline>(); strokeOutline.effectColor = new Color(.22f, .72f, .65f, .50f); strokeOutline.effectDistance = new Vector2(1f, -1f);
            SetMiniGameMouseControls(ChapterThreeTaskKind.Carry);

            miniGameProgressText = Label("任务数据", taskCard.transform, "", 16, TextAnchor.MiddleCenter, new Color(.72f, .86f, .84f), new Vector2(.08f, .055f), new Vector2(.92f, .145f));
            Label("任务按键提示", taskCard.transform, "键盘或鼠标操作以当前任务提示为准", 13, TextAnchor.MiddleLeft,
                new Color(.42f, .62f, .62f), new Vector2(.07f, .008f), new Vector2(.55f, .052f));
            Label("任务退出提示", taskCard.transform, "ESC  暂停并稍后重试", 14, TextAnchor.MiddleRight, new Color(.58f, .70f, .70f), new Vector2(.50f, .012f), new Vector2(.93f, .07f));
            miniGamePanel.SetActive(false);

            fadeImage = Panel("第三章转场", canvas.transform, Color.black, Vector2.zero, Vector2.one).GetComponent<Image>();
            fadeImage.transform.SetAsLastSibling();
            BuildEndingCinematicUi();
            pauseMenu = GetComponent<MeixiSharedChapterPauseMenu>() ?? gameObject.AddComponent<MeixiSharedChapterPauseMenu>();
            pauseMenu.Initialize(canvas, "CHAPTER 03  /  民宿档案",
                () => $"乡 村 数 字 文 化 实 践  ·  第 {MeixiLocalProgress.PracticeNumber:00} 次\n第 {currentDay} 天  ·  真相 {CountBits(truthMask)}/{ChapterThreeRules.TruthClueCount}  ·  住房 {CountBits(housingMask)}/{ChapterThreeRules.HousingClueCount}",
                OnPauseChanged, onReturn: () => { SaveProgress(); UnityEngine.SceneManagement.SceneManager.LoadScene("MeixiEditable"); });
        }

        private void BuildEndingCinematicUi()
        {
            endingCinematicPanel = Panel("第三章终末之书翻页层", canvas.transform, new Color(.004f, .018f, .022f, .92f), Vector2.zero, Vector2.one);
            endingCinematicPanel.SetActive(false);
            endingCinematicPanel.GetComponent<Image>().raycastTarget = false;
            endingCinematicGroup = endingCinematicPanel.AddComponent<CanvasGroup>();
            endingCinematicGroup.alpha = 0f;
            endingCinematicGroup.blocksRaycasts = false;

            endingCinematicGlow = Panel("终章青绿色晕光", endingCinematicPanel.transform, new Color(.08f, .72f, .62f, .10f), new Vector2(.10f, .12f), new Vector2(.90f, .88f)).GetComponent<Image>();
            endingCinematicGlow.raycastTarget = false;
            // These anchors/colors intentionally match the established terminal
            // book in MeixiGame3D, including its open-book art and right-page copy.
            endingTitle = Label("终末之书页标题", endingCinematicPanel.transform, "余音未散", 42,
                TextAnchor.MiddleLeft, new Color(.30f, .18f, .08f), new Vector2(.555f, .69f), new Vector2(.855f, .79f));
            endingBody = Label("终末之书页正文", endingCinematicPanel.transform, "", 27,
                TextAnchor.UpperLeft, new Color(.10f, .078f, .052f), new Vector2(.555f, .285f), new Vector2(.855f, .675f));
            endingBody.lineSpacing = 1.32f;
            endingKicker = Label("终末之书页码", endingCinematicPanel.transform, "01  /  05", 15,
                TextAnchor.MiddleRight, new Color(.25f, .38f, .34f), new Vector2(.735f, .79f), new Vector2(.855f, .835f));
            endingHint = Label("终末之书翻页提示", endingCinematicPanel.transform, "", 16,
                TextAnchor.MiddleRight, new Color(.14f, .32f, .29f), new Vector2(.53f, .205f), new Vector2(.855f, .255f));
            endingHint.text = "";
            SetTextAlpha(endingKicker, 0f); SetTextAlpha(endingTitle, 0f); SetTextAlpha(endingBody, 0f); SetTextAlpha(endingHint, 0f);
        }

        private void ShowMediaNotice(ChapterThreeInteractable item)
        {
            if (item == null || modalPanel == null) return;
            string resource = item.Id == 0 ? "ChapterThree/NoticeNewspaper" : "ChapterThree/NoticePostcard";
            Texture2D texture = Resources.Load<Texture2D>(resource);
            if (texture == null)
            {
                ShowInformation(item.DisplayName, "广告栏图片资源暂时不可用。", null);
                return;
            }
            modalIsChoice = false;
            modalLeftAction = CloseModalWithoutAction;
            modalRightAction = null;
            modalTitle.text = item.DisplayName + " · 全貌";
            modalBody.text = item.Story;
            modalBody.gameObject.SetActive(false);
            modalMediaImage.material = null;
            modalMediaImage.color = Color.white;
            modalMediaImage.uvRect = new Rect(0f, 0f, 1f, 1f);
            modalMediaImage.texture = texture;
            if (modalMediaAspect != null)
                modalMediaAspect.aspectRatio = texture.width / (float)Mathf.Max(1, texture.height);
            modalMediaImage.gameObject.SetActive(true);
            modalLeftLabel.text = "返 回  [E]";
            modalLeftButton.gameObject.SetActive(true);
            modalRightButton.gameObject.SetActive(false);
            modalPanel.SetActive(true);
            fadeImage.transform.SetAsLastSibling();
            modalPanel.transform.SetAsLastSibling();
            SetControlLocked(true, true);
        }

        private void ShowInformation(string title, string body, Action onContinue)
        {
            modalIsChoice = false;
            modalLeftAction = () =>
            {
                CloseModalWithoutAction();
                onContinue?.Invoke();
            };
            modalRightAction = null;
            modalTitle.text = title;
            modalBody.text = body;
            if (modalMediaImage != null) modalMediaImage.gameObject.SetActive(false);
            modalBody.gameObject.SetActive(true);
            modalLeftLabel.text = "继 续  [E]";
            modalLeftButton.gameObject.SetActive(true);
            modalRightButton.gameObject.SetActive(false);
            modalPanel.SetActive(true);
            fadeImage.transform.SetAsLastSibling();
            modalPanel.transform.SetAsLastSibling();
            SetControlLocked(true, true);
        }

        private void ShowChoice(string title, string body, string left, Action leftAction, string right, Action rightAction)
        {
            modalIsChoice = true;
            modalLeftAction = leftAction;
            modalRightAction = rightAction;
            modalTitle.text = title;
            modalBody.text = body;
            if (modalMediaImage != null) modalMediaImage.gameObject.SetActive(false);
            modalBody.gameObject.SetActive(true);
            modalLeftLabel.text = left;
            modalRightLabel.text = right;
            modalLeftButton.gameObject.SetActive(true);
            modalRightButton.gameObject.SetActive(true);
            modalPanel.SetActive(true);
            fadeImage.transform.SetAsLastSibling();
            modalPanel.transform.SetAsLastSibling();
            SetControlLocked(true, true);
        }

        private void InvokeModalLeft()
        {
            if (sleepSelectorActive)
            {
                AdjustSleepHours(-1);
                return;
            }
            Action action = modalLeftAction;
            CloseModalWithoutAction();
            action?.Invoke();
        }

        private void InvokeModalRight()
        {
            if (sleepSelectorActive)
            {
                AdjustSleepHours(1);
                return;
            }
            Action action = modalRightAction;
            CloseModalWithoutAction();
            action?.Invoke();
        }

        private void CloseModalWithoutAction()
        {
            modalLeftAction = null;
            modalRightAction = null;
            modalIsChoice = false;
            if (modalMediaImage != null) modalMediaImage.gameObject.SetActive(false);
            if (modalBody != null) modalBody.gameObject.SetActive(true);
            if (modalPanel != null) modalPanel.SetActive(false);
            if (!cinematicActive && !miniGameActive) SetControlLocked(false, false);
        }

        private void SetControlLocked(bool locked, bool pointer)
        {
            inputLocked = locked;
            if (walker != null) walker.enabled = !locked;
            // Use the shared cursor coordinator. Direct Cursor assignments are
            // overwritten in LateUpdate, which made the luggage timing cursor
            // appear without a usable hardware pointer.
            if (pointer) MeixiCursorState.SetPointer();
            else MeixiCursorState.SetGameplay();
            if (promptText != null && locked) promptText.gameObject.SetActive(false);
            if (promptPanel != null && locked) promptPanel.SetActive(false);
            if (locked && focused != null)
            {
                focused.SetFocused(false);
                focused = null;
            }
        }

        private void OnPauseChanged(bool paused)
        {
            if (walker != null) walker.enabled = !paused && !inputLocked;
            if (paused)
            {
                if (promptText != null) promptText.gameObject.SetActive(false);
                if (promptPanel != null) promptPanel.SetActive(false);
                if (focused != null)
                {
                    focused.SetFocused(false);
                    focused = null;
                }
            }
            else if (!inputLocked)
            {
                MeixiCursorState.SetGameplay();
            }
        }

        private void ShowToast(string value)
        {
            toastText.text = value;
            if (toastPanel != null) toastPanel.SetActive(true);
            toastText.gameObject.SetActive(true);
            if (toastPanel != null) toastPanel.transform.SetAsLastSibling();
            else toastText.transform.SetAsLastSibling();
            toastUntil = Time.unscaledTime + 3.2f;
        }

        private static void SetTextAlpha(Text text, float alpha)
        {
            if (text == null) return;
            Color color = text.color; color.a = Mathf.Clamp01(alpha); text.color = color;
        }

        private IEnumerator RunSmokeValidation()
        {
            yield return new WaitForSecondsRealtime(.5f);
            if (ChapterThreeRules.Difficulty(60f) != 0 || ChapterThreeRules.Difficulty(59.99f) != 1 ||
                ChapterThreeRules.Difficulty(35f) != 1 || ChapterThreeRules.Difficulty(34.99f) != 2)
                throw new InvalidOperationException("Chapter-three sanity thresholds are invalid.");
            if (ChapterThreeRules.TotalAssignedTasks(7) != 18 || ChapterThreeRules.TotalAssignedTasks(9) != 23)
                throw new InvalidOperationException("Chapter-three task schedule is invalid.");
            if (interactables.Count < 22 || finalBook != null)
                throw new InvalidOperationException($"Chapter-three interaction gate invalid: interactables={interactables.Count}, book={(finalBook != null)}");
            int shadowedPointLights = 0;
            for (int i = 0; i < indoorLights.Count; i++) if (indoorLights[i].shadows != LightShadows.None) shadowedPointLights++;
            if (shadowedPointLights > 0) throw new InvalidOperationException("Chapter-three additional lights must not cast realtime shadows.");
            Debug.Log($"[MeixiChapterThree] SMOKE PASS interactions={interactables.Count} tasks7=18 tasks9=23 lights={indoorLights.Count}.");
        }

        private static bool HasArgument(string value)
        {
            return Array.Exists(Environment.GetCommandLineArgs(), argument => string.Equals(argument, value, StringComparison.OrdinalIgnoreCase));
        }

        private static bool AnyLetterPressed(Keyboard keyboard)
        {
            return keyboard.mKey.wasPressedThisFrame || keyboard.eKey.wasPressedThisFrame || keyboard.iKey.wasPressedThisFrame ||
                   keyboard.xKey.wasPressedThisFrame || keyboard.cKey.wasPressedThisFrame || keyboard.hKey.wasPressedThisFrame || keyboard.oKey.wasPressedThisFrame;
        }

        private static bool WasLetterPressed(Keyboard keyboard, char value) => value switch
        {
            'M' => keyboard.mKey.wasPressedThisFrame,
            'E' => keyboard.eKey.wasPressedThisFrame,
            'I' => keyboard.iKey.wasPressedThisFrame,
            'X' => keyboard.xKey.wasPressedThisFrame,
            'C' => keyboard.cKey.wasPressedThisFrame,
            'H' => keyboard.hKey.wasPressedThisFrame,
            'O' => keyboard.oKey.wasPressedThisFrame,
            _ => false
        };

        private static int CountBits(int value)
        {
            int count = 0;
            uint bits = unchecked((uint)value);
            while (bits != 0) { bits &= bits - 1; count++; }
            return count;
        }

        private static string FormatTime(float minutes)
        {
            int total = Mathf.FloorToInt(Mathf.Repeat(minutes, 1440f));
            return $"{total / 60:00}:{total % 60:00}";
        }

        private static Material CreateMaterial(string name, Color color, float smoothness)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            Material material = new(shader) { name = name, color = color };
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", smoothness);
            return material;
        }

        private static Material CreateEmissive(string name, Color color, float intensity)
        {
            Material material = CreateMaterial(name, color, .42f);
            if (material.HasProperty("_EmissionColor")) { material.EnableKeyword("_EMISSION"); material.SetColor("_EmissionColor", color * intensity); }
            return material;
        }

        private GameObject Panel(string name, Transform parent, Color color, Vector2 min, Vector2 max)
        {
            GameObject panel = new(name, typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(parent, false);
            RectTransform rect = panel.GetComponent<RectTransform>(); rect.anchorMin = min; rect.anchorMax = max; rect.offsetMin = Vector2.zero; rect.offsetMax = Vector2.zero;
            panel.GetComponent<Image>().color = color;
            return panel;
        }

        private Text Label(string name, Transform parent, string value, int size, TextAnchor anchor, Color color, Vector2 min, Vector2 max)
        {
            GameObject label = new(name, typeof(RectTransform), typeof(Text));
            label.transform.SetParent(parent, false);
            RectTransform rect = label.GetComponent<RectTransform>(); rect.anchorMin = min; rect.anchorMax = max; rect.offsetMin = Vector2.zero; rect.offsetMax = Vector2.zero;
            Text text = label.GetComponent<Text>(); text.font = uiFont; text.text = value; text.fontSize = size; text.alignment = anchor; text.color = color; text.raycastTarget = false; text.supportRichText = true;
            return text;
        }

        private Button UiButton(string name, Transform parent, string value, Vector2 min, Vector2 max, out Text label)
        {
            GameObject buttonObject = Panel(name, parent, new Color(.08f, .34f, .34f, .98f), min, max);
            Button button = buttonObject.AddComponent<Button>(); button.targetGraphic = buttonObject.GetComponent<Image>();
            ColorBlock colors = button.colors; colors.highlightedColor = new Color(.16f, .56f, .50f, 1f); colors.pressedColor = new Color(.05f, .22f, .24f, 1f); button.colors = colors;
            label = Label(name + "文字", buttonObject.transform, value, 19, TextAnchor.MiddleCenter, Color.white, new Vector2(.03f, .05f), new Vector2(.97f, .95f));
            return button;
        }
    }
}
