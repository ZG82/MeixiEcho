using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

namespace MeixiEcho
{
    public enum CombatBeatAction
    {
        None,
        AttackOne,
        AttackTwo,
        Dodge
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
                "林默｜采样器捕捉到重复声纹，可这条溪明明已经沉默。",
                "老村长｜孩子们曾经每天听着泉水醒来。你若听得见，就替梅溪把它找回来。",
                "林默｜我只能分析频率，不会唱歌。它为什么会回应我？",
                "老村长｜先别急着分析。水记得脚步，石桥也记得谁曾从这里回家。",
                "系统｜失调溪灵正在吞噬水声。红色区域会追踪你的落点；瞬身脱离后，用近战突进或远程音符波击破三段声纹。"
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
        private AudioClip chapterOneMusic;
        private Canvas canvas;
        private GameObject titlePanel;
        private GameObject hudPanel;
        private GameObject dialoguePanel;
        private GameObject rhythmPanel;
        private GameObject chapterPanel;
        private GameObject endingPanel;
        private GameObject pausePanel;
        private GameObject interactionCard;
        private GameObject openingPanel;
        private GameObject loadingPanel;
        private Text chapterHudText;
        private Text objectiveText;
        private Text healthText;
        private Text soulCountText;
        private Text interactionText;
        private Text interactionTitle;
        private Text interactionSubtitle;
        private Text navigationText;
        private Text dialogueSpeaker;
        private Text dialogueText;
        private Text dialogueRole;
        private Text dialogueCounter;
        private Text dialoguePortraitGlyph;
        private Image dialoguePortraitPlate;
        private Text toastText;
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
        private float captureAt;
        private string captureFileName = "MeixiCapture.png";
        private bool rhythmAutoValidation;
        private bool rhythmFinisherValidation;
        private bool meleeAutoValidation;
        private bool rangedCounterValidation;
        private bool wrongMeleeValidation;
        private bool autoActionIssuedForBeat;
        private bool openingActive;
        private bool secondChapterLoading;
        private bool secondChapterRouteActive;
        private float nextRouteIntegrityCheckAt;
        private float openingUntil;
        private bool chapterOneCombatActive;
        private float chapterOneCombatBpm = 92f;
        private double chapterOneBeatStart;
        private float chapterOneBeatPulse;
        private const float CombatInputWindow = 0.19f;
        private CombatBeatAction expectedCombatAction;
        private double expectedCombatBeatDsp;
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
        private bool finishingStrike;
        private float cinematicInputLockedUntil;
        private int chapterOneCombatCombo;
        private int chapterOneCombatMisses;

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
        public bool CanPlayerMove => started && !openingActive && !paused && !finishingStrike && !CinematicInputLocked &&
                                     (!rhythmActive || chapterOneCombatActive) && (!complete || secondChapterRouteActive);
        public bool GameplayRunning => started && !openingActive && !dialogueOpen && !complete && !paused && !CinematicInputLocked;
        public bool ChapterOneCombatActive => chapterOneCombatActive && !complete && !paused && !CinematicInputLocked;
        public bool PlayerIsDodging => player != null && player.IsInvulnerable;
        public bool SecondChapterRouteActive => secondChapterRouteActive;
        public Vector3 CombatTargetPosition => creekSpirit != null ? creekSpirit.transform.position : PlayerPosition + Vector3.forward * 3f;
        public Vector3 PlayerPosition => playerTransform != null ? playerTransform.position : Vector3.zero;
        public bool EnemyThreatPending => chapterOneCombatActive && combatBeatPending;

        private void Awake()
        {
            Application.targetFrameRate = 120;
            Application.runInBackground = true;
            uiFont = Font.CreateDynamicFontFromOSFont(new[] { "Microsoft YaHei", "SimHei", "Arial" }, 34);
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
            TryStartCaptureMode();
            TryStartSecondChapterSmokeTest();
        }

        private void TryStartSecondChapterSmokeTest()
        {
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

        private IEnumerator ValidateFreeMovement()
        {
            yield return null;
            StartAdventure();
            FinishOpeningSequence();
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
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.W));
            yield return new WaitForSecondsRealtime(.65f);
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            yield return null;
            float explorationDistance = HorizontalDistance(explorationStart, playerTransform.position);
            Debug.Log($"[MeixiMovementSmoke] exploration distance={explorationDistance:F3} canMove={CanPlayerMove}");

            StartChapterOneCombat();
            yield return new WaitForSecondsRealtime(.15f);
            Vector3 combatStart = playerTransform.position;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.D));
            yield return new WaitForSecondsRealtime(.65f);
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            yield return null;
            float combatDistance = HorizontalDistance(combatStart, playerTransform.position);
            Debug.Log($"[MeixiMovementSmoke] combat distance={combatDistance:F3} canMove={CanPlayerMove}");
            yield return new WaitForSecondsRealtime(.25f);
            Application.Quit();
        }

        private IEnumerator ValidateEndingPointerClick()
        {
            yield return null;
            started = true;
            ShowEnding();
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
            StartAdventure();
            FinishOpeningSequence();
            if (chapterPanel.activeSelf) FinishChapterCardImmediately();
            while (dialogueOpen) AdvanceDialogue();
            yield return null;
            CharacterController activeController = playerTransform != null
                ? playerTransform.GetComponent<CharacterController>()
                : null;
            Debug.Log($"[MeixiInputSmoke] keyboard={Keyboard.current != null} canMove={CanPlayerMove} controller={activeController != null && activeController.enabled}");
            yield return new WaitForSecondsRealtime(.25f);
            LoadSecondChapter();
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

            if (toastText != null && Time.unscaledTime > toastUntil) toastText.gameObject.SetActive(false);

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

            if (chapterPanel.activeSelf && Time.unscaledTime >= chapterCardUntil)
            {
                chapterPanel.SetActive(false);
                Action callback = chapterCardCallback;
                chapterCardCallback = null;
                callback?.Invoke();
            }

            if (openingActive && Time.unscaledTime >= openingUntil) FinishOpeningSequence();
            if (chapterOneCombatActive) UpdateChapterOneCombatHud();
            if ((rhythmAutoValidation || rhythmFinisherValidation || meleeAutoValidation || rangedCounterValidation || wrongMeleeValidation) && chapterOneCombatActive)
                UpdateAutomatedRhythmValidation();

            if (started && hudPanel.activeSelf) UpdateNavigation();

            // Typewriter playback is visual state and must not depend on an attached
            // keyboard (batch captures, controllers-only setups and remote play can all
            // have Keyboard.current == null).
            if (dialogueOpen) UpdateTypewriter();

            // Starting the game must not depend on EventSystem/UI module setup.
            // A direct Input System path also handles a title button whose visual
            // children intercept pointer raycasts in the authored scene.
            bool startRequested = !started &&
                                  ((keyboard != null && (keyboard.enterKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame)) ||
                                   (mouse != null && mouse.leftButton.wasPressedThisFrame));
            if (startRequested)
            {
                StartAdventure();
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

            if (keyboard == null) return;

            // Every blocking presentation can be advanced directly from the
            // keyboard. This prevents the opening/card sequence from looking like
            // a frozen controller when the player immediately presses movement.
            if (openingActive && AnyPresentationAdvancePressed(keyboard))
            {
                FinishOpeningSequence();
                return;
            }
            if (chapterPanel.activeSelf && AnyPresentationAdvancePressed(keyboard))
            {
                FinishChapterCardImmediately();
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

            if (started && keyboard.escapeKey.wasPressedThisFrame)
            {
                TogglePause();
                return;
            }

            if (paused) return;

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

            ApplySurfaceTexture(grass, "Textures/Realistic/HumidGrassGround", new Vector2(7.5f, 7.5f), new Color(0.72f, 0.84f, 0.70f));
            ApplySurfaceTexture(earth, "Textures/Realistic/HumidGrassGround", new Vector2(5.5f, 5.5f), new Color(0.58f, 0.43f, 0.30f));
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
            // 仅加载用户自行取得授权并放入 Resources 的音源；没有文件时使用项目原创配乐。
            chapterOneMusic = Resources.Load<AudioClip>("Audio/PingFanZhiLu") ?? rhythmSong;
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
            sun.intensity = 1.38f;
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
            roofLeft.transform.localRotation = Quaternion.Euler(0f, 0f, -16f);
            var roofRight = CreateChildCube("青瓦坡二", root.transform, new Vector3(size.x * 0.24f, size.y * 0.60f, 0f), new Vector3(size.x * 0.58f, 0.28f, size.z * 1.25f), roof, false);
            roofRight.transform.localRotation = Quaternion.Euler(0f, 0f, 16f);
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
            fire.range = 12f;
            fire.intensity = 5f;
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
                Destroy(stem.GetComponent<Collider>());
                var crown = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                crown.name = "竹叶云";
                crown.transform.position = p + Vector3.up * (4.7f + i * 0.15f);
                crown.transform.localScale = new Vector3(1.1f, 0.55f, 1.1f);
                crown.GetComponent<Renderer>().material = leaves;
                Destroy(crown.GetComponent<Collider>());
            }
        }

        private void CreateOliveTree(Vector3 position)
        {
            var trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            trunk.name = "闽清橄榄树";
            trunk.transform.position = position + Vector3.up * 1.5f;
            trunk.transform.localScale = new Vector3(0.32f, 1.5f, 0.32f);
            trunk.GetComponent<Renderer>().material = wood;
            for (int i = 0; i < 5; i++)
            {
                var crown = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                crown.name = "橄榄树冠";
                float angle = i * 137.5f * Mathf.Deg2Rad;
                crown.transform.position = position + new Vector3(Mathf.Cos(angle) * 0.92f, 3.55f + (i % 3) * 0.34f, Mathf.Sin(angle) * 0.72f);
                crown.transform.localScale = new Vector3(1.65f + (i % 2) * 0.32f, 1.08f + (i % 3) * 0.12f, 1.48f);
                crown.GetComponent<Renderer>().material = leaves;
                Destroy(crown.GetComponent<Collider>());

                Vector3 start = position + Vector3.up * 2.35f;
                Vector3 end = crown.transform.position - Vector3.up * 0.28f;
                Vector3 direction = end - start;
                var branch = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                branch.name = "橄榄树自然分枝";
                branch.transform.position = (start + end) * 0.5f;
                branch.transform.rotation = Quaternion.FromToRotation(Vector3.up, direction.normalized);
                branch.transform.localScale = new Vector3(0.095f, direction.magnitude * 0.5f, 0.095f);
                branch.GetComponent<Renderer>().material = wood;
                Destroy(branch.GetComponent<Collider>());
            }
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
                point.range = 8f;
                point.intensity = 2.2f;
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
                Bounds initialBounds = CalculateBounds(characterModel);
                float characterScale = initialBounds.size.y > 0.01f ? 1.92f / initialBounds.size.y : 1f;
                characterModel.transform.localScale = Vector3.one * characterScale;
                Bounds scaledBounds = CalculateBounds(characterModel);
                Vector3 modelCorrection = new(
                    root.transform.position.x - scaledBounds.center.x,
                    root.transform.position.y - scaledBounds.min.y,
                    root.transform.position.z - scaledBounds.center.z);
                characterModel.transform.position += modelCorrection;
                ApplyCharacterMaterial(characterModel, characterAsset.name == "DefaultMale");

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
            CreateVillageNpc("老村长", "守泉人 · 林水生", new Vector3(2.7f, 0f, 5.8f), 198f, 1.76f,
                new Color(0.72f, 0.77f, 0.70f), VillageNpcStyle.Elder,
                new[]
                {
                    "老村长｜你就是林家的孩子吧？你爷爷年轻时，总在桥边领唱。",
                    "林默｜您认识我爷爷？",
                    "老村长｜认识他的声音。人走远了，声音会替他认路。",
                    "老村长｜去溪边听听。先别急着分析，让水声把话说完。"
                }, "老村长｜溪水恢复以前，我每天都来这里等一会儿。声音，也是在等人。", "村");

            CreateVillageNpc("阿禾", "村中少年 · 采样爱好者", new Vector3(-4.0f, 0f, 2.6f), 34f, 1.68f,
                new Color(0.70f, 0.83f, 0.66f), VillageNpcStyle.Youth,
                new[]
                {
                    "阿禾｜城里的采样器，能听懂竹叶落在哪块石头上吗？",
                    "林默｜它能记录频率，但“听懂”……也许还不能。",
                    "阿禾｜那就别只看屏幕。梅溪的节拍在脚底下。",
                    "林默｜你不觉得村里的音乐太旧吗？",
                    "阿禾｜旧的是唱法，不是这里的声音。"
                }, "阿禾｜等溪水醒了，我给你听我做的橄榄树节拍。", "禾");

            CreateVillageNpc("桂婶", "橄榄园守护人", new Vector3(-6.6f, 0f, 8.3f), 145f, 1.64f,
                new Color(0.72f, 0.70f, 0.86f), VillageNpcStyle.Aunt,
                new[]
                {
                    "桂婶｜这片橄榄树比我年纪还大。风一来，每棵树唱得都不一样。",
                    "林默｜这些声音一直有人记录吗？",
                    "桂婶｜从前记在歌里。歌没人唱，才需要你们的机器来记。",
                    "桂婶｜机器记得清楚，人可要记得为什么唱。"
                }, "桂婶｜顺着青石路走，闻到水汽时，就离溪边不远了。", "桂");
        }

        private void CreateVillageNpc(string displayName, string role, Vector3 position, float yaw, float height,
            Color tint, VillageNpcStyle style, string[] firstDialogue, string repeatDialogue, string glyph)
        {
            var root = new GameObject(displayName + "_可交互NPC");
            root.transform.position = position;
            root.transform.rotation = Quaternion.Euler(0f, yaw, 0f);

            GameObject characterAsset = LoadPreferredCharacterModel();
            if (characterAsset != null)
            {
                GameObject model = Instantiate(characterAsset, root.transform);
                model.name = displayName + "_写实人物模型";
                model.transform.localPosition = Vector3.zero;
                model.transform.localRotation = Quaternion.identity;
                model.transform.localScale = Vector3.one;
                Bounds initialBounds = CalculateBounds(model);
                float scale = initialBounds.size.y > 0.01f ? height / initialBounds.size.y : 1f;
                model.transform.localScale = Vector3.one * scale;
                Bounds scaledBounds = CalculateBounds(model);
                model.transform.position += new Vector3(position.x - scaledBounds.center.x, position.y - scaledBounds.min.y, position.z - scaledBounds.center.z);
                ApplyNpcCharacterMaterial(model, tint, displayName, characterAsset.name == "DefaultMale");

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
                if (characterAsset.name == "DefaultMale") animator.enabled = false;
            }

            AddNpcAccessories(root.transform, style, height);
            CreateWorldLabel(root.transform, displayName + "  ·  " + role, new Vector3(0f, height + 0.42f, 0f));
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
            // Unity requires all three orbital curves to use the same curve mode.
            // Leaving X/Z at their default constant mode while Y uses two constants
            // floods the player log every frame and causes visible combat stutter.
            velocity.orbitalX = new ParticleSystem.MinMaxCurve(0f, 0f);
            velocity.orbitalY = new ParticleSystem.MinMaxCurve(1.1f, 2.0f);
            velocity.orbitalZ = new ParticleSystem.MinMaxCurve(0f, 0f);
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
            // The current combat library was authored against LinMo's Humanoid bind pose.
            // Keep that fully textured/animated body active; the higher-detail Standard Assets
            // reference remains available for a later dedicated retargeting pass.
            return Resources.Load<GameObject>("Models/Characters/LinMo") ??
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
            bloom.intensity.Override(0.62f);
            bloom.threshold.Override(0.84f);
            bloom.scatter.Override(0.72f);
            Vignette vignette = profile.Add<Vignette>(true);
            vignette.intensity.Override(0.22f);
            vignette.smoothness.Override(0.58f);
            ColorAdjustments color = profile.Add<ColorAdjustments>(true);
            color.postExposure.Override(0.08f);
            color.contrast.Override(11f);
            color.saturation.Override(7f);
            Tonemapping tonemapping = profile.Add<Tonemapping>(true);
            tonemapping.mode.Override(TonemappingMode.ACES);
        }

        private void BuildUI()
        {
            if (uiFont == null) uiFont = Font.CreateDynamicFontFromOSFont(new[] { "Microsoft YaHei", "SimHei", "Arial" }, 34);
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

            toastText = CreateText("华丽爆字", canvas.transform, "", 42, TextAnchor.MiddleCenter, Color.white);
            SetRect(toastText.rectTransform, new Vector2(0.22f, 0.72f), new Vector2(0.78f, 0.83f), Vector2.zero, Vector2.zero);
            DecorateText(toastText, new Color(0f, 0f, 0f, 0.9f), new Vector2(3f, -3f), true);
            toastText.gameObject.AddComponent<FancyTextPulse>().Amount = 0.08f;
            toastText.gameObject.SetActive(false);
        }

        private void BuildTitleUI()
        {
            titlePanel = CreatePanel("主标题界面", canvas.transform, Color.white, Vector2.zero, Vector2.one);
            ApplyTexture(titlePanel, "Art/TitleBackgroundV2");
            // The whole title screen can start the game. This keeps decorative
            // images from making the visible start button appear unresponsive.
            var titleStartButton = titlePanel.AddComponent<Button>();
            titleStartButton.transition = Selectable.Transition.None;
            titleStartButton.targetGraphic = titlePanel.GetComponent<Image>();
            titleStartButton.onClick.AddListener(StartAdventure);
            CreatePanel("主视觉暗角", titlePanel.transform, new Color(0f, 0.015f, 0.035f, 0.24f), Vector2.zero, Vector2.one);
            CreatePanel("左侧墨色渐隐", titlePanel.transform, new Color(0.005f, 0.018f, 0.027f, 0.18f), new Vector2(0f, 0f), new Vector2(0.51f, 1f));
            var storyCard = CreatePanel("标题叙事玻璃卡", titlePanel.transform, new Color(0.008f, 0.035f, 0.046f, 0.68f), new Vector2(0.038f, 0.13f), new Vector2(0.49f, 0.885f));
            storyCard.GetComponent<Image>().raycastTarget = false;
            PolishPanel(storyCard, new Color(0.46f, 0.82f, 0.82f, 0.34f));
            CreatePanel("标题卡金线", storyCard.transform, new Color(1f, 0.58f, 0.18f, 0.92f), new Vector2(0f, 0.05f), new Vector2(0.006f, 0.95f)).GetComponent<Image>().raycastTarget = false;
            var episode = CreateText("体验章节", titlePanel.transform, "CHAPTER 01  /  STREAM OF MEMORY", 15, TextAnchor.MiddleLeft, new Color(1f, 0.68f, 0.28f));
            SetRect(episode.rectTransform, new Vector2(0.065f, 0.845f), new Vector2(0.43f, 0.88f), Vector2.zero, Vector2.zero);
            var eyebrow = CreateText("标题眉题", titlePanel.transform, "MEIXI  ·  FUJIAN  ·  ECHOES OF THE LAND", 18, TextAnchor.MiddleLeft, new Color(0.56f, 0.86f, 0.90f));
            SetRect(eyebrow.rectTransform, new Vector2(0.065f, 0.79f), new Vector2(0.45f, 0.835f), Vector2.zero, Vector2.zero);
            var title = CreateText("梅溪回响标题", titlePanel.transform, "梅 溪 回 响", 92, TextAnchor.MiddleLeft, new Color(1f, 0.78f, 0.34f));
            SetRect(title.rectTransform, new Vector2(0.06f, 0.64f), new Vector2(0.47f, 0.80f), Vector2.zero, Vector2.zero);
            DecorateText(title, new Color(0.08f, 0.02f, 0f, 0.95f), new Vector2(5f, -5f), true);
            title.gameObject.AddComponent<FancyTextPulse>().Amount = 0.025f;
            var line = CreatePanel("金色题线", titlePanel.transform, new Color(1f, 0.58f, 0.18f, 0.88f), new Vector2(0.065f, 0.625f), new Vector2(0.34f, 0.629f));
            line.GetComponent<Image>().raycastTarget = false;
            var subtitle = CreateText("类型", titlePanel.transform, "第三人称剧情动作音游  ·  第一章体验版", 24, TextAnchor.MiddleLeft, new Color(0.90f, 0.95f, 1f));
            SetRect(subtitle.rectTransform, new Vector2(0.065f, 0.55f), new Vector2(0.47f, 0.615f), Vector2.zero, Vector2.zero);
            DecorateText(subtitle, Color.black, new Vector2(2f, -2f), false);
            var story = CreateText("标题故事钩子", titlePanel.transform, "一座逐渐失去声音的村庄。\n一个只相信数据的年轻人。\n沿着溪流，听见土地留下的记忆。", 20, TextAnchor.UpperLeft, new Color(0.82f, 0.90f, 0.90f));
            SetRect(story.rectTransform, new Vector2(0.065f, 0.405f), new Vector2(0.45f, 0.535f), Vector2.zero, Vector2.zero);
            var start = CreateButton("启程按钮", titlePanel.transform, "踏 入 梅 溪", new Color(0.65f, 0.20f, 0.08f, 0.96f));
            SetRect(start.GetComponent<RectTransform>(), new Vector2(0.065f, 0.265f), new Vector2(0.315f, 0.375f), Vector2.zero, Vector2.zero);
            start.onClick.AddListener(StartAdventure);
            start.gameObject.AddComponent<FancyTextPulse>().Amount = 0.035f;
            PolishPanel(start.gameObject, new Color(1f, 0.62f, 0.22f, 0.72f));
            var controls = CreateText("主界面操作", titlePanel.transform, "WASD  行走探索   ·   鼠标  环绕镜头   ·   E  与村民交谈\nJ  近战突进   ·   K  远程音符   ·   SPACE  瞬身闪避", 17, TextAnchor.MiddleLeft, new Color(0.72f, 0.86f, 0.90f));
            SetRect(controls.rectTransform, new Vector2(0.065f, 0.155f), new Vector2(0.46f, 0.245f), Vector2.zero, Vector2.zero);
        }

        private void BuildOpeningUI()
        {
            openingPanel = CreatePanel("第一章电影开场", canvas.transform, Color.clear, Vector2.zero, Vector2.one);
            CreatePanel("电影黑边上", openingPanel.transform, new Color(0f, 0f, 0f, 0.96f), new Vector2(0f, 0.885f), Vector2.one);
            CreatePanel("电影黑边下", openingPanel.transform, new Color(0f, 0f, 0f, 0.96f), Vector2.zero, new Vector2(1f, 0.115f));
            var place = CreateText("开场地点", openingPanel.transform, "福 建 · 闽 清\n梅 溪 村   05:42", 28, TextAnchor.LowerLeft, new Color(0.92f, 0.96f, 0.94f));
            SetRect(place.rectTransform, new Vector2(0.055f, 0.14f), new Vector2(0.40f, 0.29f), Vector2.zero, Vector2.zero);
            DecorateText(place, Color.black, new Vector2(2f, -2f), false);
            var caption = CreateText("开场旁白", openingPanel.transform, "城市的噪声，在山路尽头突然消失。", 24, TextAnchor.LowerRight, new Color(1f, 0.77f, 0.38f));
            SetRect(caption.rectTransform, new Vector2(0.47f, 0.14f), new Vector2(0.945f, 0.23f), Vector2.zero, Vector2.zero);
            openingPanel.SetActive(false);
        }

        private void BuildHudUI()
        {
            hudPanel = CreatePanel("游戏HUD", canvas.transform, Color.clear, Vector2.zero, Vector2.one);
            var quest = CreatePanel("水纹任务签", hudPanel.transform, Color.white, new Vector2(0.018f, 0.80f), new Vector2(0.39f, 0.965f));
            ApplyTexture(quest, "Art/QuestFrameV2");
            chapterHudText = CreateText("当前章节", quest.transform, "第一章 · 没有声音的村庄", 27, TextAnchor.MiddleLeft, new Color(1f, 0.78f, 0.35f));
            SetRect(chapterHudText.rectTransform, new Vector2(0.15f, 0.49f), new Vector2(0.91f, 0.87f), Vector2.zero, Vector2.zero);
            DecorateText(chapterHudText, Color.black, new Vector2(2f, -2f), false);
            objectiveText = CreateText("当前目标", quest.transform, "调查村庄中央的异常声纹", 21, TextAnchor.MiddleLeft, Color.white);
            SetRect(objectiveText.rectTransform, new Vector2(0.15f, 0.16f), new Vector2(0.91f, 0.53f), Vector2.zero, Vector2.zero);

            healthText = CreateText("回响力", hudPanel.transform, "回响力  ◆ ◆ ◆ ◆ ◆", 23, TextAnchor.UpperRight, new Color(1f, 0.48f, 0.32f));
            SetRect(healthText.rectTransform, new Vector2(0.72f, 0.915f), new Vector2(0.965f, 0.967f), Vector2.zero, Vector2.zero);
            DecorateText(healthText, Color.black, new Vector2(2f, -2f), false);
            soulCountText = CreateText("魂音数量", hudPanel.transform, "溪流魂音  0 / 1", 19, TextAnchor.UpperRight, new Color(0.42f, 0.90f, 1f));
            SetRect(soulCountText.rectTransform, new Vector2(0.72f, 0.865f), new Vector2(0.965f, 0.915f), Vector2.zero, Vector2.zero);

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
            var card = CreatePanel("暂停主卡", pausePanel.transform, new Color(0.018f, 0.055f, 0.080f, 0.98f), new Vector2(0.34f, 0.20f), new Vector2(0.66f, 0.80f));
            PolishPanel(card, new Color(1f, 0.55f, 0.17f, 0.65f));
            var eyebrow = CreateText("暂停眉题", card.transform, "MEIXI RESONANCE", 16, TextAnchor.MiddleCenter, new Color(0.32f, 0.82f, 0.92f));
            SetRect(eyebrow.rectTransform, new Vector2(0.12f, 0.84f), new Vector2(0.88f, 0.92f), Vector2.zero, Vector2.zero);
            var title = CreateText("暂停标题", card.transform, "魂 音 暂 歇", 48, TextAnchor.MiddleCenter, new Color(1f, 0.78f, 0.34f));
            SetRect(title.rectTransform, new Vector2(0.08f, 0.68f), new Vector2(0.92f, 0.84f), Vector2.zero, Vector2.zero);
            DecorateText(title, Color.black, new Vector2(3f, -3f), true);
            var controls = CreateText("暂停操作", card.transform, "WASD  行走     鼠标  环绕镜头\nSPACE  跳跃     SHIFT / Q  踏音闪避\n左 / 右键（J / K）声纹打击     E  聆听魂音", 19, TextAnchor.MiddleCenter, new Color(0.74f, 0.87f, 0.93f));
            SetRect(controls.rectTransform, new Vector2(0.09f, 0.45f), new Vector2(0.91f, 0.66f), Vector2.zero, Vector2.zero);
            var resume = CreateButton("继续游戏", card.transform, "继 续 聆 听", new Color(0.62f, 0.18f, 0.07f, 0.98f));
            SetRect(resume.GetComponent<RectTransform>(), new Vector2(0.18f, 0.29f), new Vector2(0.82f, 0.41f), Vector2.zero, Vector2.zero);
            resume.onClick.AddListener(TogglePause);
            var restart = CreateButton("重新开始", card.transform, "重 新 启 程", new Color(0.06f, 0.24f, 0.31f, 0.98f));
            SetRect(restart.GetComponent<RectTransform>(), new Vector2(0.18f, 0.14f), new Vector2(0.82f, 0.26f), Vector2.zero, Vector2.zero);
            restart.onClick.AddListener(() =>
            {
                Time.timeScale = 1f;
                UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
            });
            var hint = CreateText("暂停提示", card.transform, "ESC  返回游戏", 15, TextAnchor.MiddleCenter, new Color(0.48f, 0.72f, 0.79f));
            SetRect(hint.rectTransform, new Vector2(0.15f, 0.03f), new Vector2(0.85f, 0.11f), Vector2.zero, Vector2.zero);
            pausePanel.SetActive(false);
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
            dialoguePanel = CreatePanel("电影式水纹对话", canvas.transform, Color.white, new Vector2(0.045f, 0.025f), new Vector2(0.955f, 0.315f));
            ApplyTexture(dialoguePanel, "Art/QuestFrameV2");
            var archive = CreateText("声纹档案标记", dialoguePanel.transform, "MEIXI VOICE ARCHIVE  ·  乡村声音档案", 13, TextAnchor.MiddleLeft, new Color(0.42f, 0.76f, 0.80f));
            SetRect(archive.rectTransform, new Vector2(0.17f, 0.84f), new Vector2(0.67f, 0.96f), Vector2.zero, Vector2.zero);
            var portrait = CreatePanel("魂音说话者徽章", dialoguePanel.transform, new Color(0.06f, 0.20f, 0.25f, 0.98f), new Vector2(0.026f, 0.12f), new Vector2(0.145f, 0.88f));
            PolishPanel(portrait, new Color(0.24f, 0.82f, 0.92f, 0.48f));
            dialoguePortraitPlate = portrait.GetComponent<Image>();
            // Unity 6 no longer ships UI/Skin/Knob.psd as a built-in resource.
            // Use the built-in white texture instead so the HUD does not emit an
            // error every time the editable scene starts.
            Sprite roundSprite = Sprite.Create(Texture2D.whiteTexture,
                new Rect(0f, 0f, Texture2D.whiteTexture.width, Texture2D.whiteTexture.height),
                new Vector2(0.5f, 0.5f), 100f);
            if (roundSprite != null)
            {
                dialoguePortraitPlate.sprite = roundSprite;
                dialoguePortraitPlate.preserveAspect = true;
            }
            Image portraitRing = CreatePanel("肖像内圈", portrait.transform, new Color(1f, 0.62f, 0.18f, 0.12f), new Vector2(0.08f, 0.08f), new Vector2(0.92f, 0.92f)).GetComponent<Image>();
            portraitRing.raycastTarget = false;
            if (roundSprite != null)
            {
                portraitRing.sprite = roundSprite;
                portraitRing.preserveAspect = true;
            }
            dialoguePortraitGlyph = CreateText("魂音徽记", portrait.transform, "林", 48, TextAnchor.MiddleCenter, new Color(1f, 0.78f, 0.34f));
            SetRect(dialoguePortraitGlyph.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            DecorateText(dialoguePortraitGlyph, new Color(0f, 0.03f, 0.05f, 0.95f), new Vector2(3f, -3f), true);
            dialogueSpeaker = CreateText("说话者", dialoguePanel.transform, "林默", 28, TextAnchor.MiddleLeft, new Color(1f, 0.75f, 0.30f));
            SetRect(dialogueSpeaker.rectTransform, new Vector2(0.17f, 0.64f), new Vector2(0.42f, 0.86f), Vector2.zero, Vector2.zero);
            dialogueRole = CreateText("说话者身份", dialoguePanel.transform, "城市调研员", 15, TextAnchor.MiddleLeft, new Color(0.48f, 0.82f, 0.88f));
            SetRect(dialogueRole.rectTransform, new Vector2(0.34f, 0.66f), new Vector2(0.64f, 0.83f), Vector2.zero, Vector2.zero);
            dialogueCounter = CreateText("对话进度", dialoguePanel.transform, "01 / 01", 14, TextAnchor.MiddleRight, new Color(0.52f, 0.76f, 0.80f));
            SetRect(dialogueCounter.rectTransform, new Vector2(0.78f, 0.69f), new Vector2(0.94f, 0.84f), Vector2.zero, Vector2.zero);
            dialogueText = CreateText("对白", dialoguePanel.transform, "", 27, TextAnchor.UpperLeft, new Color(0.96f, 0.98f, 0.97f));
            SetRect(dialogueText.rectTransform, new Vector2(0.17f, 0.15f), new Vector2(0.94f, 0.66f), Vector2.zero, Vector2.zero);
            var hint = CreateText("继续提示", dialoguePanel.transform, "E / SPACE  继续  ◆", 17, TextAnchor.LowerRight, new Color(0.55f, 0.90f, 1f));
            SetRect(hint.rectTransform, new Vector2(0.70f, 0.01f), new Vector2(0.94f, 0.17f), Vector2.zero, Vector2.zero);
            hint.gameObject.AddComponent<FancyTextPulse>().Fade = true;
            dialoguePanel.SetActive(false);
        }

        private void BuildRhythmUI()
        {
            rhythmPanel = CreatePanel("三维节拍战斗HUD", canvas.transform, Color.clear, Vector2.zero, Vector2.one);
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
            var ribbon = CreatePanel("战斗水纹签", rhythmPanel.transform, Color.white, new Vector2(0.30f, 0.025f), new Vector2(0.70f, 0.17f));
            ApplyTexture(ribbon, "Art/QuestFrameV2");
            rhythmTitle = CreateText("节奏标题", ribbon.transform, "失 调 溪 灵   回响 12 / 12", 24, TextAnchor.MiddleCenter, new Color(1f, 0.78f, 0.30f));
            SetRect(rhythmTitle.rectTransform, new Vector2(0.12f, 0.55f), new Vector2(0.90f, 0.88f), Vector2.zero, Vector2.zero);
            rhythmJudge = CreateText("动作判定", ribbon.transform, "听节拍 · 看指令 · 在魂音归一时回应", 18, TextAnchor.MiddleCenter, new Color(0.45f, 0.95f, 1f));
            SetRect(rhythmJudge.rectTransform, new Vector2(0.12f, 0.27f), new Vector2(0.90f, 0.57f), Vector2.zero, Vector2.zero);
            rhythmScore = CreateText("动作提示", ribbon.transform, "SPACE 瞬身    ·    J 近战突进    ·    K 远程音符波", 14, TextAnchor.MiddleCenter, new Color(0.78f, 0.91f, 1f));
            SetRect(rhythmScore.rectTransform, new Vector2(0.10f, 0.06f), new Vector2(0.92f, 0.30f), Vector2.zero, Vector2.zero);
            rhythmTrack = CreatePanel("兼容节拍轨", rhythmPanel.transform, Color.clear, new Vector2(0.49f, 0.49f), new Vector2(0.51f, 0.51f)).GetComponent<RectTransform>();
            rhythmPanel.SetActive(false);
        }

        private void BuildChapterUI()
        {
            chapterPanel = CreatePanel("章节转场", canvas.transform, Color.white, Vector2.zero, Vector2.one);
            ApplyTexture(chapterPanel, "Art/ChapterBackdrop");
            chapterCardNumber = CreateText("章节编号", chapterPanel.transform, "第 一 章", 25, TextAnchor.MiddleCenter, new Color(0.45f, 0.85f, 0.94f));
            SetRect(chapterCardNumber.rectTransform, new Vector2(0.32f, 0.63f), new Vector2(0.68f, 0.71f), Vector2.zero, Vector2.zero);
            chapterCardTitle = CreateText("章节大标题", chapterPanel.transform, chapterNames[0], 66, TextAnchor.MiddleCenter, new Color(1f, 0.76f, 0.30f));
            SetRect(chapterCardTitle.rectTransform, new Vector2(0.18f, 0.45f), new Vector2(0.82f, 0.63f), Vector2.zero, Vector2.zero);
            DecorateText(chapterCardTitle, Color.black, new Vector2(4f, -4f), true);
            chapterCardTitle.gameObject.AddComponent<FancyTextPulse>().Amount = 0.025f;
            chapterCardSubtitle = CreateText("章节副题", chapterPanel.transform, chapterSubtitles[0], 25, TextAnchor.MiddleCenter, new Color(0.90f, 0.93f, 0.94f));
            SetRect(chapterCardSubtitle.rectTransform, new Vector2(0.22f, 0.34f), new Vector2(0.78f, 0.43f), Vector2.zero, Vector2.zero);
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
            titlePanel.SetActive(true);
            hudPanel.SetActive(false);
            dialoguePanel.SetActive(false);
            rhythmPanel.SetActive(false);
            chapterPanel.SetActive(false);
            endingPanel.SetActive(false);
            if (loadingPanel != null) loadingPanel.SetActive(false);
            if (openingPanel != null) openingPanel.SetActive(false);
            if (pausePanel != null) pausePanel.SetActive(false);
            if (interactionCard != null) interactionCard.SetActive(false);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private void TryStartCaptureMode()
        {
            string[] arguments = Environment.GetCommandLineArgs();
            bool titleRequested = Array.Exists(arguments, value => string.Equals(value, "-meixiTitleCapture", StringComparison.OrdinalIgnoreCase));
            bool openingRequested = Array.Exists(arguments, value => string.Equals(value, "-meixiOpeningCapture", StringComparison.OrdinalIgnoreCase));
            bool villageRequested = Array.Exists(arguments, value => string.Equals(value, "-meixiVillageCapture", StringComparison.OrdinalIgnoreCase));
            bool dialogueRequested = Array.Exists(arguments, value => string.Equals(value, "-meixiDialogueCapture", StringComparison.OrdinalIgnoreCase));
            bool animationRequested = Array.Exists(arguments, value => string.Equals(value, "-meixiAnimationCapture", StringComparison.OrdinalIgnoreCase));
            bool rhythmAutoRequested = Array.Exists(arguments, value => string.Equals(value, "-meixiRhythmAuto", StringComparison.OrdinalIgnoreCase));
            bool finisherRequested = Array.Exists(arguments, value => string.Equals(value, "-meixiFinisherCapture", StringComparison.OrdinalIgnoreCase));
            bool meleeRequested = Array.Exists(arguments, value => string.Equals(value, "-meixiMeleeCapture", StringComparison.OrdinalIgnoreCase));
            bool rangedCounterRequested = Array.Exists(arguments, value => string.Equals(value, "-meixiRangedCounterCapture", StringComparison.OrdinalIgnoreCase));
            bool wrongMeleeRequested = Array.Exists(arguments, value => string.Equals(value, "-meixiWrongMeleeCapture", StringComparison.OrdinalIgnoreCase));
            bool flowRequested = Array.Exists(arguments, value => string.Equals(value, "-meixiFlowCapture", StringComparison.OrdinalIgnoreCase));
            bool requested = Application.isBatchMode || titleRequested || openingRequested || villageRequested || dialogueRequested || animationRequested || rhythmAutoRequested || finisherRequested || meleeRequested ||
                             rangedCounterRequested || wrongMeleeRequested || flowRequested ||
                             Array.Exists(arguments, value => string.Equals(value, "-meixiCapture", StringComparison.OrdinalIgnoreCase));
            if (!requested) return;
            captureMode = true;
            if (titleRequested)
            {
                captureFileName = "MeixiTitleCapture.png";
                captureAt = Time.unscaledTime + 1.2f;
                ShowTitle();
                return;
            }
            if (openingRequested)
            {
                captureFileName = "MeixiOpeningCapture.png";
                captureAt = Time.unscaledTime + 2.6f;
                StartAdventure();
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
            if (villageRequested || dialogueRequested)
            {
                captureFileName = dialogueRequested ? "MeixiDialogueCapture.png" : "MeixiVillageCapture.png";
                captureAt = Time.unscaledTime + (dialogueRequested ? 1.8f : 2.2f);
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
                if (dialogueRequested)
                    ShowDialogue(new[]
                    {
                        "老村长｜人走远了，声音会替他认路。",
                        "林默｜所以你们说的魂音，不是传说，而是所有人留下的记忆？"
                    }, null);
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                return;
            }
            if (animationRequested)
            {
                captureFileName = "MeixiAnimationCapture.png";
                captureAt = Time.unscaledTime + 1.86f;
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
                player?.BeginAnimationPreview();
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
                rhythmAutoRequested ? "MeixiRhythmSuccessCapture.png" : "MeixiCapture.png";
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
            StartChapterOneCombat();
            if (finisherRequested) creekSpirit?.PrepareFinisherValidation(chapterOneBeatStart);
            else if (rangedCounterRequested || wrongMeleeRequested) creekSpirit?.PrepareSpecificAttackValidation(2, chapterOneBeatStart);
            else if (rhythmAutoRequested || meleeRequested) creekSpirit?.PrepareAttackValidation(chapterOneBeatStart);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private IEnumerator ValidateInteractionToCombatFlow()
        {
            yield return null;

            VillageNpc3D npc = villageNpcs.Find(candidate => candidate != null);
            int safety = 0;
            if (npc != null)
            {
                npc.Interact();
                yield return null;
                Debug.Log($"[MeixiValidation] NPC interaction dialogueOpen={dialogueOpen} npc={npc.DisplayName}");
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
            yield return new WaitForSecondsRealtime(2.85f);
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
            if (!started || complete) return;
            paused = !paused;
            Time.timeScale = paused ? 0f : 1f;
            pausePanel.SetActive(paused);
            Cursor.lockState = paused ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = paused;
            if (paused && interactionCard != null) interactionCard.SetActive(false);
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

        private void StartAdventure()
        {
            if (started) return;
            // Awake already binds an authored scene. Rebinding here reset both
            // animators on the first playable frame and made the opening hitch.
            if (useSceneAuthoredWorld && (player == null || playerTransform == null || orbitCamera == null))
                BindSceneAuthoredWorld();
            ResetGameplaySession();
            MeixiSecondChapterRoute.Ensure(this);
            started = true;
            Debug.Log("[Meixi] StartAdventure accepted; opening cinematic begins.");
            titlePanel.SetActive(false);
            hudPanel.SetActive(false);
            openingActive = true;
            openingUntil = Time.unscaledTime + 5.2f;
            openingPanel.SetActive(true);
            if (orbitCamera != null)
            {
                orbitCamera.BeginOpeningShot(5.2f);
            }
            else
            {
                Debug.LogError("Unable to start the opening shot: no gameplay camera is available.");
            }
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void ResetGameplaySession()
        {
            Time.timeScale = 1f;
            paused = false;
            complete = false;
            dialogueOpen = false;
            rhythmActive = false;
            openingActive = false;
            finishingStrike = false;
            chapterOneCombatActive = false;
            combatBeatPending = false;
            combatBeatSucceeded = false;
            threatDamageResolved = false;
            expectedCombatAction = CombatBeatAction.None;
            currentThreatType = -1;
            activeThreatId = -1;
            dodgedThreatId = -1;
            counteredThreatId = -1;
            resolvedThreatId = -1;
            playerDamageImmuneUntilDsp = 0d;
            chapterOneCombatPhase = 1;
            chapterOneCombatCombo = 0;
            chapterOneCombatMisses = 0;
            cinematicInputLockedUntil = 0f;
            chapterCardCallback = null;
            dialogueCallback = null;
            pendingNode = null;
            secondChapterLoading = false;
            secondChapterRouteActive = false;
            completedMemories = 0;
            defeatedEnemies = 0;
            health = 5;

            if (musicSource != null) musicSource.Stop();
            if (rhythmPanel != null) rhythmPanel.SetActive(false);
            if (dialoguePanel != null) dialoguePanel.SetActive(false);
            if (chapterPanel != null) chapterPanel.SetActive(false);
            if (endingPanel != null) endingPanel.SetActive(false);
            if (pausePanel != null) pausePanel.SetActive(false);
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
            UpdateHealth();
            if (soulCountText != null) soulCountText.text = "溪流魂音  0 / 1";
            Debug.Log("[MeixiSession] Deterministic gameplay state initialized.");
        }

        private void FinishOpeningSequence()
        {
            if (!openingActive) return;
            openingActive = false;
            openingPanel.SetActive(false);
            hudPanel.SetActive(true);
            ShowChapterCard(-1, "第 一 章", "没 有 声 音 的 村 庄", "城市的喧闹在山路尽头突然消失", () =>
            {
                ShowDialogue(new[]
                {
                    "林默｜三下乡数字文化调研，第一项任务：记录梅溪村的环境声音。",
                    "系统｜检测到未知声纹结构。空气中存在大量破碎音符。",
                    "林默｜这里安静得不正常。连鸟叫和水声都像被什么截断了。",
                    "老村长｜你手里的机器听见了碎片，可碎片原本属于谁，它不会告诉你。",
                    "林默｜声音……怎么可能有记忆？",
                    "老村长｜先去和村里的人说说话。然后沿青石路走到溪边，别只盯着屏幕。"
                }, () => SetObjective(0));
            });
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
            chapterPanel.SetActive(false);
            Action callback = chapterCardCallback;
            chapterCardCallback = null;
            callback?.Invoke();
        }

        private void LoadSecondChapter()
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
            if (secondChapterLoading) return;
            secondChapterLoading = true;
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
            StartCoroutine(LoadSecondChapterRoutine());
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
            objectiveText.text = chapterOneCombatActive
                ? "脱离红色追踪区，击破溪灵的三段声纹护甲"
                : "沿青石路前往溪畔，按 E 聆听第一段魂音";
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
            npc.FacePlayerImmediately();
            ShowDialogue(lines, () => ShowToast("已记录 " + npc.DisplayName + " 的村落记忆", new Color(0.44f, 0.92f, 0.84f)));
        }

        public void BeginMemory(int index, SoulNode3D node)
        {
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
            // Keep story text immediately legible across keyboard, controller and capture
            // environments. The old typewriter path could leave the body line blank when
            // no keyboard device was registered during startup.
            typedCharacters = fullDialogueLine.Length;
            dialogueText.text = fullDialogueLine;
        }

        private void UpdateDialogueSpeakerStyle(string speaker)
        {
            string role = "梅溪魂音档案";
            string glyph = "音";
            Color accent = new Color(0.48f, 0.86f, 0.92f);
            if (speaker.Contains("林默"))
            {
                role = "城市调研员 · 电子信息专业";
                glyph = "林";
                accent = new Color(0.38f, 0.82f, 1f);
            }
            else if (speaker.Contains("老村长"))
            {
                role = "梅溪村守泉人";
                glyph = "村";
                accent = new Color(1f, 0.72f, 0.32f);
            }
            else if (speaker.Contains("阿禾"))
            {
                role = "村中少年 · 采样爱好者";
                glyph = "禾";
                accent = new Color(0.52f, 0.94f, 0.62f);
            }
            else if (speaker.Contains("桂婶"))
            {
                role = "橄榄园守护人";
                glyph = "桂";
                accent = new Color(0.86f, 0.68f, 1f);
            }
            else if (speaker.Contains("系统"))
            {
                role = "AI 声纹采集系统";
                glyph = "声";
                accent = new Color(0.30f, 0.94f, 1f);
            }
            if (dialogueRole != null) dialogueRole.text = role;
            if (dialoguePortraitGlyph != null)
            {
                dialoguePortraitGlyph.text = glyph;
                dialoguePortraitGlyph.color = accent;
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
            chapterOneCombatCombo = 0;
            chapterOneCombatMisses = 0;
            chapterOneCombatPhase = 1;
            if (pendingNode != null) pendingNode.gameObject.SetActive(false);
            if (chapterPanel != null) chapterPanel.SetActive(false);
            if (dialoguePanel != null) dialoguePanel.SetActive(false);
            SetInteractionCard(null);
            SetObjective(0);
            rhythmPanel.SetActive(true);
            rhythmTitle.text = "失 调 溪 灵   三段声纹 36 / 36";
            rhythmJudge.text = "红色追踪区收束前按 SPACE 瞬身离开";
            rhythmScore.text = "J 近战突进（需进入范围） · K 远程音符波 · SPACE 瞬身闪避";
            if (combatActionKey != null) combatActionKey.text = "·";
            if (combatActionName != null) combatActionName.text = "聆 听 敌 人";
            chapterOneBeatStart = AudioSettings.dspTime + 0.12d;
            chapterOneBeatPulse = 0f;
            chapterOneCombatBpm = chapterOneMusic != null && chapterOneMusic != rhythmSong ? 76f : bpms[0];

            musicSource.Stop();
            musicSource.clip = chapterOneMusic ?? rhythmSong;
            musicSource.pitch = musicSource.clip == rhythmSong ? chapterOneCombatBpm / 105f : 1f;
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
            float beat = 60f / chapterOneCombatBpm;
            double elapsed = Math.Max(0d, AudioSettings.dspTime - chapterOneBeatStart);
            float phase = (float)(elapsed % beat) / beat;
            float distanceToBeat = Mathf.Min(phase, 1f - phase);
            float musicPulse = Mathf.Clamp01(1f - distanceToBeat / 0.24f);
            double now = AudioSettings.dspTime;
            float responsePulse = combatBeatPending
                ? Mathf.Clamp01(1f - Mathf.Abs((float)(expectedCombatBeatDsp - now)) / 0.72f)
                : 0f;
            chapterOneBeatPulse = Mathf.Max(musicPulse * 0.34f, responsePulse);
            float scale = 0.88f + chapterOneBeatPulse * 0.22f;
            rhythmFlash.rectTransform.localScale = Vector3.one * scale;
            Color color = rhythmFlash.color;
            color.a = combatBeatPending ? 0.48f + chapterOneBeatPulse * 0.48f : 0.18f + musicPulse * 0.18f;
            Color actionColor = CombatActionColor(expectedCombatAction);
            color = Color.Lerp(new Color(0.18f, 0.72f, 0.90f, color.a), new Color(actionColor.r, actionColor.g, actionColor.b, color.a), chapterOneBeatPulse);
            rhythmFlash.color = color;
            rhythmTitle.text = $"失 调 溪 灵   第 {chapterOneCombatPhase} 段声纹   {creekSpirit.Health} / {creekSpirit.HealthMaximum}";
            rhythmScore.text = $"SPACE 瞬身  ·  J 近战突进  ·  K 远程音符波     闪避 {chapterOneCombatCombo}   受击 {chapterOneCombatMisses}";
        }

        public bool IsCombatOnBeat(float window = 0.14f)
        {
            return chapterOneCombatActive && combatBeatPending &&
                   Math.Abs(AudioSettings.dspTime - expectedCombatBeatDsp) <= window;
        }

        public void PrepareCombatBeat(int attackType, double beatTime)
        {
            if (!chapterOneCombatActive || finishingStrike) return;
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
                ReportPerfectDodge(Time.time + 0.34f);
                Debug.Log($"[MeixiSpatial] Dodge success id={activeThreatId} enemyDistance={enemyDelta.magnitude:F2} " +
                          $"immuneUntil={playerDamageImmuneUntilDsp:F3} health={health}");
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
            if (delta > CombatInputWindow)
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
            if (!chapterOneCombatActive || finishingStrike) return false;
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
            rhythmJudge.text = attackType switch
            {
                0 => "高抛坠音命中 · 回响力受损",
                1 => "低音冲击波命中 · 回响力受损",
                _ => "追踪和弦合击 · 回响力受损"
            };
            rhythmJudge.color = new Color(1f, 0.28f, 0.24f);
            if (combatActionKey != null) combatActionKey.text = "×";
            Debug.Log($"[MeixiSpatial] Guaranteed enemy hit type={attackType} health={health}");
            return true;
        }

        public bool TryStartMeleeLunge(Vector3 origin, out Vector3 direction)
        {
            Vector3 delta = CombatTargetPosition - origin;
            delta.y = 0f;
            direction = delta.sqrMagnitude > 0.01f ? delta.normalized : playerTransform.forward;
            bool inAcquisitionRange = chapterOneCombatActive && creekSpirit != null && delta.magnitude <= 4.80f;
            Debug.Log($"[MeixiSpatial] Melee acquire distance={delta.magnitude:F2} armed={inAcquisitionRange}");
            if (!inAcquisitionRange)
            {
                rhythmJudge.text = "J 近战距离不足 · 仅向目标突进";
                rhythmJudge.color = new Color(0.54f, 0.78f, 0.90f);
                ShowToast("距 离 过 远 · 突 进 无 攻 击", new Color(0.48f, 0.78f, 0.92f));
            }
            return inAcquisitionRange;
        }

        public bool ResolvePlayerMelee(Vector3 origin, Vector3 forward)
        {
            if (!chapterOneCombatActive || creekSpirit == null || finishingStrike) return false;
            bool onBeat = IsChapterMusicOnBeat();
            bool hit = creekSpirit.TryReceiveMeleeHit(origin, forward, onBeat);
            if (!hit)
            {
                rhythmJudge.text = "近 战 落 空 · 需 靠 近 溪 灵";
                rhythmJudge.color = new Color(0.58f, 0.76f, 0.86f);
                ShowToast("突 进 落 空", new Color(0.52f, 0.76f, 0.90f));
                return false;
            }
            // 致命一击已经切入专属终结演出，不再叠加普通 J 命中特效。
            if (finishingStrike) return true;
            Vector3 meleeImpact = creekSpirit.transform.position + Vector3.up * 0.92f;
            PlayerCombatFlourish3D.CreateMelee(origin, meleeImpact, forward, onBeat);
            EnemyMusicImpact3D.Create(meleeImpact, onBeat ? Color.white : new Color(0.18f, 0.94f, 1f),
                onBeat ? 3.0f : 2.2f, onBeat ? 0.58f : 0.44f, onBeat ? 16 : 10);
            PlayCombatResponseFx(CombatBeatAction.AttackOne, onBeat, false);
            orbitCamera?.TriggerRhythmCut(1, onBeat);
            audioSource.PlayOneShot(lowTone, 0.82f);
            if (meleeAutoValidation && captureMode && !captureDone)
                captureAt = Time.unscaledTime + 0.07f;
            Debug.Log($"[MeixiSpatial] Melee hit onBeat={onBeat}");
            return true;
        }

        public void FirePlayerNoteWave(Vector3 origin, Vector3 direction)
        {
            if (!chapterOneCombatActive || creekSpirit == null || finishingStrike) return;
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
            if (!chapterOneCombatActive || target == null || finishingStrike) return;
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
            double beatLength = 60d / Math.Max(1f, chapterOneCombatBpm);
            double elapsed = Math.Max(0d, AudioSettings.dspTime - chapterOneBeatStart);
            double phase = elapsed % beatLength;
            return phase <= windowSeconds || phase >= beatLength - windowSeconds;
        }

        public void ReportCombatPhase(int phase)
        {
            chapterOneCombatPhase = Mathf.Clamp(phase, 1, 3);
            string phaseName = chapterOneCombatPhase switch { 1 => "听潮", 2 => "逆流", _ => "回声暴雨" };
            ShowToast($"第 {chapterOneCombatPhase} 段 · {phaseName}", chapterOneCombatPhase == 3
                ? new Color(1f, 0.34f, 0.24f) : new Color(1f, 0.72f, 0.28f));
            rhythmJudge.text = chapterOneCombatPhase == 3
                ? "红区锁定加速 · 观察后瞬身"
                : "声纹护甲破裂 · 攻击节奏加快";
            BurstSoulParticles(CombatTargetPosition + Vector3.up * 1.1f);
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
            rhythmJudge.color = new Color(1f, 0.24f, 0.20f);
            rhythmJudge.text = attackType switch
            {
                0 => "朱红高抛音符追踪落点 · SPACE 瞬身脱离",
                1 => "溪蓝低音冲击波对准你 · SPACE 向侧面瞬身",
                _ => "五声音符群正在合围 · K 在节拍点远程破谱"
            };
        }

        private void FailCombatBeat(string reason)
        {
            if (!combatBeatPending || finishingStrike) return;
            combatBeatPending = false;
            combatBeatSucceeded = false;
            chapterOneCombatCombo = 0;
            chapterOneCombatMisses++;
            rhythmJudge.text = reason + " · 回响力受损";
            rhythmJudge.color = new Color(1f, 0.30f, 0.28f);
            if (combatActionKey != null) combatActionKey.text = "×";
            DamagePlayer();
            player?.TriggerHitReaction(creekSpirit != null ? creekSpirit.transform.position : PlayerPosition + Vector3.forward);
            orbitCamera?.TriggerRhythmMiss();
            SpawnMissBeatVfx(PlayerPosition + Vector3.up * 0.9f);
            Debug.Log($"[MeixiRhythm] Miss {reason} action={expectedCombatAction} health={health} now={AudioSettings.dspTime:F3}");
        }

        private static string CombatActionKey(CombatBeatAction action) => action switch
        {
            CombatBeatAction.AttackOne => "J",
            CombatBeatAction.AttackTwo => "K",
            CombatBeatAction.Dodge => "SPACE",
            _ => "·"
        };

        private static string CombatActionName(CombatBeatAction action) => action switch
        {
            CombatBeatAction.AttackOne => "攻 击 一 · 破 招",
            CombatBeatAction.AttackTwo => "攻 击 二 · 断 流",
            CombatBeatAction.Dodge => "踏 音 闪 避",
            _ => "聆 听 敌 人"
        };

        private static Color CombatActionColor(CombatBeatAction action) => action switch
        {
            CombatBeatAction.AttackOne => new Color(0.26f, 0.90f, 1f),
            CombatBeatAction.AttackTwo => new Color(1f, 0.48f, 0.22f),
            CombatBeatAction.Dodge => new Color(0.42f, 1f, 0.70f),
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
            rhythmJudge.text = "完 美 瞬 身 · 追 踪 丢 失";
            rhythmJudge.color = new Color(1f, 0.82f, 0.28f);
            ShowToast("◆ 完 美 瞬 身 · 安 全 落 点 ◆", new Color(1f, 0.78f, 0.24f));
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
            rhythmJudge.text = counter ? $"节 拍 强 化 · -{damage}" : $"声 纹 命 中 · -{damage}";
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
            BurstSoulParticles(position + Vector3.up);
            audioSource.PlayOneShot(successTone);

            if (pendingNode != null)
            {
                pendingNode.gameObject.SetActive(true);
                pendingNode.Complete();
                pendingNode = null;
            }
            completedMemories = 1;
            soulCountText.text = "溪流魂音  1 / 1";
            player?.TriggerVictory();
            complete = true;
            ShowDialogue(new[]
            {
                "系统｜溪流声纹恢复。水滴、竹叶与石桥回声重新同步。",
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
            StartCoroutine(FinalStrikeSequence(action, targetPosition));
        }

        private IEnumerator FinalStrikeSequence(CombatBeatAction action, Vector3 targetPosition)
        {
            finishingStrike = true;
            cinematicInputLockedUntil = Time.unscaledTime + 2.60f;
            combatBeatPending = false;
            MeleeForbiddenRing3D.ClearAll(false);
            TrackingAttackTelegraph3D.ClearAll();
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
            FinisherCinematicOverlay3D.Begin(2.12f);
            PlayerCombatFlourish3D.CreateFinisherCharge(PlayerPosition, targetPosition, action);

            float previousTimeScale = Time.timeScale;
            float previousPitch = musicSource != null ? musicSource.pitch : 1f;
            Time.timeScale = 0.20f;
            if (musicSource != null) musicSource.pitch = previousPitch * 0.58f;
            yield return new WaitForSecondsRealtime(0.28f);

            player?.TriggerFinalStrikeImpact(action);
            orbitCamera?.TriggerFinisherCut(0);
            Color impactColor = action == CombatBeatAction.AttackTwo
                ? new Color(0.16f, 1f, 0.92f)
                : new Color(1f, 0.66f, 0.16f);
            FinisherCinematicOverlay3D.Impact(impactColor);
            PlayerCombatFlourish3D.CreateFinisherImpact(PlayerPosition, targetPosition, action);
            SpawnGroundSigil(new Vector3(targetPosition.x, 0.06f, targetPosition.z), 5.8f, 1.10f, Color.white);
            SpawnBeatShardBurst(targetPosition + Vector3.up, 16, 4.6f, Color.white);
            audioSource.PlayOneShot(successTone, 1f);
            Time.timeScale = 0.035f;
            if (rhythmFinisherValidation && captureMode && !captureDone)
                captureAt = Time.unscaledTime + 0.10f;
            Debug.Log($"[MeixiRhythm] Final strike close-up impact action={action}");
            yield return new WaitForSecondsRealtime(0.12f);

            Time.timeScale = 0.28f;
            orbitCamera?.TriggerFinisherCut(1);
            SpawnGeneratedSlash(targetPosition + Vector3.up * 1.05f, 7.2f, -36f, 0.92f, new Color(1f, 0.58f, 0.22f, 0.90f));
            yield return new WaitForSecondsRealtime(0.46f);

            cinematicInputLockedUntil = Time.unscaledTime + 1.15f;
            Time.timeScale = 0f;
            if (musicSource != null) musicSource.pitch = previousPitch * 0.18f;
            yield return new WaitForSecondsRealtime(1.15f);

            Time.timeScale = previousTimeScale;
            if (musicSource != null) musicSource.pitch = previousPitch;
            player?.CompleteFinalStrikeAnimation();
            creekSpirit?.CompleteFinalStrike();
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
            soulCountText.text = $"溪流魂音  {completedMemories} / 1";
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
                ShowToast($"魂 音 归 位    {completedMemories} / 1", new Color(0.38f, 1f, 0.70f));
                SetObjective(completedMemories);
            }
        }

        private void ShowEnding()
        {
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

        public void SpawnFootstep(Vector3 position)
        {
            var mote = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            mote.name = "脚步音尘";
            mote.transform.position = position + Vector3.up * 0.08f;
            mote.transform.localScale = Vector3.one * 0.08f;
            mote.GetComponent<Renderer>().material = new Material(cyan);
            Destroy(mote.GetComponent<Collider>());
            var fx = mote.AddComponent<PulseFx3D>();
            fx.Lifetime = 0.28f;
            fx.Growth = 3.5f;
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
            if (chapterOneCombatActive && AudioSettings.dspTime < playerDamageImmuneUntilDsp)
            {
                Debug.Log($"[MeixiDamage] Ignored delayed damage after dodge id={dodgedThreatId} " +
                          $"now={AudioSettings.dspTime:F3} immuneUntil={playerDamageImmuneUntilDsp:F3} health={health}");
                return;
            }
            health--;
            Debug.Log($"[MeixiDamage] Applied health={health} threat={activeThreatId} type={currentThreatType}");
            UpdateHealth();
            ShowToast("回 响 力 受 到 噪 音 侵 蚀", new Color(1f, 0.27f, 0.38f));
            if (health <= 0)
            {
                health = 5;
                RespawnPlayer();
                UpdateHealth();
            }
        }

        private void UpdateHealth()
        {
            healthText.text = "回响力  " + new string('◆', health) + new string('◇', 5 - health);
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
            if (toastText == null) return;
            toastText.text = message;
            toastText.color = color;
            toastText.gameObject.SetActive(true);
            toastUntil = Time.unscaledTime + 1.45f;
        }
    }
}
