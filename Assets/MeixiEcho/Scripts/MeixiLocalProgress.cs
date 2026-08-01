using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace MeixiEcho
{
    [Serializable]
    public sealed class MeixiOfflineSaveData
    {
        public int schemaVersion = 4;
        public bool hasEnteredGame;
        public int currentChapter = 1;
        public int journeyCount;
        public int currentJourneyDeaths;
        public int totalDeaths;
        public int currentJourneyNpcDialogues;
        public bool firstConversationRecorded;
        public bool chapterOneCompleted;
        public bool chapterTwoCompleted;
        public bool terminalBookPending;
        public bool finalBookCollected;
        public string checkpoint = "None";
        public string savedAtUtc = string.Empty;
        public List<string> unlockedAchievements = new();
        public List<MeixiAchievementUnlockRecord> achievementRecords = new();
    }

    [Serializable]
    public sealed class MeixiAchievementUnlockRecord
    {
        public string id = string.Empty;
        public string unlockedAtUtc = string.Empty;
    }

    [Serializable]
    public sealed class MeixiOfflineSettingsData
    {
        public int schemaVersion = 1;
        public float masterVolume = .85f;
    }

    public enum MeixiAchievementRank
    {
        C,
        B,
        A,
        S,
        SSS
    }

    public readonly struct MeixiAchievementDefinition
    {
        public readonly string Id;
        public readonly string Title;
        public readonly string Description;
        public readonly MeixiAchievementRank Rank;
        public readonly string IconResource;

        public MeixiAchievementDefinition(string id, string title, string description,
            MeixiAchievementRank rank, string iconResource = "")
        {
            Id = id;
            Title = title;
            Description = description;
            Rank = rank;
            IconResource = iconResource ?? string.Empty;
        }
    }

    /// <summary>
    /// Single-player offline progress. No account, device id or network data is used.
    /// Files live under Application.persistentDataPath/SaveData so an installed build
    /// can update or move without writing into Program Files or losing user progress.
    /// </summary>
    public static class MeixiLocalProgress
    {
        private const int CurrentSchema = 4;
        private const int MaxSafeCounter = 1000000;
        private const string ProgressFileName = "meixi_progress_v1.json";
        private const string SettingsFileName = "meixi_settings_v1.json";
        private static readonly object Gate = new();
        private static bool initialized;
        private static MeixiOfflineSaveData progress;
        private static MeixiOfflineSettingsData settings;

        private static readonly MeixiAchievementDefinition[] Definitions =
        {
            new("heritage", "\u4f20\u627f", "\u5728\u540e\u53f0\u53d1\u73b0\u58c1\u753b\u4e2d\u9690\u85cf\u7684\u4f20\u627f\u5fbd\u8bb0", MeixiAchievementRank.A,
                "Art/Achievements/Heritage"),
            new("bearbug_bite", "小熊虫--你可以去撕咬一切了！", "在大门密码锁输入 071029 并确认", MeixiAchievementRank.S,
                "Art/Achievements/AchievementBearBug"),
            new("graduation_fourth_death", "同学，毕业了，我们不会有下次旅程了", "在第四次实践中因回响力耗尽而倒下", MeixiAchievementRank.B),
            new("one_life_clear", "一命速通", "未曾死亡便取得终末之书", MeixiAchievementRank.SSS),
            new("journey_complete", "旅途结束了", "取得终末之书", MeixiAchievementRank.A),
            new("death_15", "死士", "累计死亡达到 15 次", MeixiAchievementRank.A),
            new("first_arrival", "初来乍到", "第一次进入《梅溪回响》", MeixiAchievementRank.C),
            new("chatterbox", "话痨", "单次旅途中与 NPC 对话达到 30 次", MeixiAchievementRank.B),
            new("death_30", "永不言弃", "累计死亡达到 30 次", MeixiAchievementRank.S),
            new("aunt_shi_first", "石婶结局", "首次安装后的首个对话对象是石婶", MeixiAchievementRank.S)
        };

        public static IReadOnlyList<MeixiAchievementDefinition> AchievementDefinitions => Definitions;
        public static string RankLabel(MeixiAchievementRank rank) => rank switch
        {
            MeixiAchievementRank.SSS => "SSS",
            MeixiAchievementRank.S => "S",
            MeixiAchievementRank.A => "A",
            MeixiAchievementRank.B => "B",
            _ => "C"
        };

        public static Color RankColor(MeixiAchievementRank rank) => rank switch
        {
            MeixiAchievementRank.SSS => new Color(1f, .34f, .12f),
            MeixiAchievementRank.S => new Color(1f, .74f, .18f),
            MeixiAchievementRank.A => new Color(.70f, .42f, 1f),
            MeixiAchievementRank.B => new Color(.28f, .78f, 1f),
            _ => new Color(.58f, .72f, .78f)
        };

        public static string RankHexColor(MeixiAchievementRank rank) => rank switch
        {
            MeixiAchievementRank.SSS => "FF571F",
            MeixiAchievementRank.S => "FFBD2E",
            MeixiAchievementRank.A => "B36BFF",
            MeixiAchievementRank.B => "47C7FF",
            _ => "94AFC0"
        };

        private static bool IsAutomatedRun => Application.isBatchMode || Array.Exists(
            Environment.GetCommandLineArgs(), value => value.StartsWith("-meixi", StringComparison.OrdinalIgnoreCase));
        public static string SaveDirectoryPath => IsAutomatedRun
            ? Path.Combine(Application.temporaryCachePath, "MeixiEchoAutomationSave")
            : Path.Combine(Application.persistentDataPath, "SaveData");
        public static string ProgressFilePath => Path.Combine(SaveDirectoryPath, ProgressFileName);
        private static string SettingsFilePath => Path.Combine(SaveDirectoryPath, SettingsFileName);
        public static bool HasGameplayProgress { get { Initialize(); return progress.currentChapter >= 1; } }
        public static bool HasContinueSave => HasGameplayProgress;
        public static int CurrentChapter { get { Initialize(); return Mathf.Clamp(progress.currentChapter, 1, 2); } }
        public static bool IsCheckpoint(string value)
        {
            Initialize();
            lock (Gate) return string.Equals(progress.checkpoint, value, StringComparison.Ordinal);
        }
        public static bool TerminalBookPending { get { Initialize(); return progress.terminalBookPending; } }
        public static bool FinalBookCollected { get { Initialize(); return progress.finalBookCollected; } }
        public static int JourneyCount { get { Initialize(); return progress.journeyCount; } }
        public static int PracticeNumber { get { Initialize(); return Math.Max(1, progress.totalDeaths + 1); } }
        public static int TotalDeaths { get { Initialize(); return progress.totalDeaths; } }
        public static int CurrentJourneyDeaths { get { Initialize(); return progress.currentJourneyDeaths; } }
        public static int CurrentJourneyNpcDialogues { get { Initialize(); return progress.currentJourneyNpcDialogues; } }
        public static float MasterVolume { get { Initialize(); return settings.masterVolume; } }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            Initialize();
            MeixiAchievementToast.EnsureHost();
        }

        public static void Initialize()
        {
            lock (Gate)
            {
                if (initialized) return;
                Directory.CreateDirectory(SaveDirectoryPath);
                progress = LoadProgress();
                settings = LoadSettings();
                int loadedSchema = progress != null ? progress.schemaVersion : 0;
                NormalizeProgress();
                settings.masterVolume = Mathf.Clamp01(settings.masterVolume);
                initialized = true;
                if (loadedSchema != CurrentSchema)
                {
                    // Persist the rank metadata and practice-count migration
                    // immediately, so an old local save is upgraded safely.
                    SaveProgressLocked();
                }
            }
        }

        /// <summary>
        /// Marks the first actual gameplay entry. Merely reaching the title screen
        /// must not recreate an achievement immediately after a full save deletion.
        /// </summary>
        public static void PrepareGameplayLaunch()
        {
            Initialize();
            lock (Gate)
            {
                if (progress.hasEnteredGame) return;
                progress.hasEnteredGame = true;
                UnlockWithoutSaving("first_arrival", true);
                SaveProgressLocked();
            }
        }

        public static void ApplyAudioSetting()
        {
            AudioListener.volume = MasterVolume;
        }

        public static void SetMasterVolume(float value)
        {
            Initialize();
            lock (Gate)
            {
                settings.masterVolume = Mathf.Clamp01(value);
                AudioListener.volume = settings.masterVolume;
                SaveSettingsLocked();
            }
        }

        public static void BeginNewJourney()
        {
            Initialize();
            lock (Gate)
            {
                // A manual restart is still the same practical attempt. The
                // next practice is created only after an actual death.
                progress.journeyCount = Math.Min(MaxSafeCounter + 1, Math.Max(1, progress.totalDeaths + 1));
                progress.currentJourneyDeaths = 0;
                progress.currentJourneyNpcDialogues = 0;
                SaveProgressLocked();
            }
        }

        /// <summary>
        /// Starts Chapter One again while retaining the player's permanent archive:
        /// total deaths, practice count, settings and earned achievements.
        /// </summary>
        public static void ResetGameplayToChapterOne()
        {
            Initialize();
            lock (Gate)
            {
                progress.currentChapter = 1;
                progress.chapterOneCompleted = false;
                progress.chapterTwoCompleted = false;
                progress.terminalBookPending = false;
                progress.finalBookCollected = false;
                progress.checkpoint = "ChapterOne";
                progress.currentJourneyDeaths = 0;
                progress.currentJourneyNpcDialogues = 0;
                progress.journeyCount = Math.Min(MaxSafeCounter + 1, Math.Max(1, progress.totalDeaths + 1));
                SaveProgressLocked();
            }
        }

        /// <summary>
        /// Changes only the resumable game state. Permanent statistics, settings
        /// and achievement ownership are deliberately left untouched.
        /// </summary>
        public static void SetGameplayChapter(int chapter)
        {
            Initialize();
            lock (Gate)
            {
                progress.currentChapter = Mathf.Clamp(chapter, 1, 2);
                if (progress.currentChapter >= 2)
                {
                    progress.chapterOneCompleted = true;
                    progress.terminalBookPending = false;
                    progress.checkpoint = "ChapterTwo";
                }
                else
                {
                    progress.chapterOneCompleted = false;
                    progress.chapterTwoCompleted = false;
                    progress.terminalBookPending = false;
                    progress.finalBookCollected = false;
                    progress.checkpoint = "ChapterOne";
                }
                SaveProgressLocked();
            }
        }

        public static void EnsureJourneyExists()
        {
            Initialize();
            lock (Gate)
            {
                int practiceNumber = Math.Max(1, progress.totalDeaths + 1);
                if (progress.journeyCount == practiceNumber) return;
                progress.journeyCount = practiceNumber;
                SaveProgressLocked();
            }
        }

        public static void SetCheckpoint(string value)
        {
            Initialize();
            lock (Gate)
            {
                if (string.IsNullOrWhiteSpace(value)) return;
                if (string.Equals(progress.checkpoint, value, StringComparison.Ordinal)) return;
                progress.checkpoint = value;
                SaveProgressLocked();
            }
        }

        public static void MarkChapterOneCompleted()
        {
            Initialize();
            lock (Gate)
            {
                progress.currentChapter = 2;
                progress.chapterOneCompleted = true;
                progress.terminalBookPending = false;
                progress.checkpoint = "ChapterTwo";
                SaveProgressLocked();
            }
        }

        public static void MarkChapterTwoCompleted()
        {
            Initialize();
            lock (Gate)
            {
                progress.currentChapter = 1;
                progress.chapterOneCompleted = true;
                progress.chapterTwoCompleted = true;
                progress.terminalBookPending = true;
                progress.finalBookCollected = false;
                progress.checkpoint = "TerminalBook";
                SaveProgressLocked();
            }
        }

        public static void RecordDeath()
        {
            Initialize();
            lock (Gate)
            {
                if (progress.journeyCount <= 0) progress.journeyCount = 1;
                progress.currentJourneyDeaths = Math.Min(MaxSafeCounter, progress.currentJourneyDeaths + 1);
                progress.totalDeaths = Math.Min(MaxSafeCounter, progress.totalDeaths + 1);
                progress.journeyCount = Math.Min(MaxSafeCounter + 1, Math.Max(1, progress.totalDeaths + 1));
                progress.currentJourneyNpcDialogues = 0;
                if (progress.totalDeaths == 4) UnlockWithoutSaving("graduation_fourth_death", true);
                if (progress.totalDeaths >= 15) UnlockWithoutSaving("death_15", true);
                if (progress.totalDeaths >= 30) UnlockWithoutSaving("death_30", true);
                SaveProgressLocked();
            }
        }

        public static void RecordNpcDialogue(string npcName)
        {
            Initialize();
            lock (Gate)
            {
                if (progress.journeyCount <= 0) progress.journeyCount = 1;
                progress.currentJourneyNpcDialogues = Math.Min(MaxSafeCounter, progress.currentJourneyNpcDialogues + 1);
                if (!progress.firstConversationRecorded)
                {
                    progress.firstConversationRecorded = true;
                    if (!string.IsNullOrWhiteSpace(npcName) &&
                        npcName.Contains("石婶", StringComparison.Ordinal))
                        UnlockWithoutSaving("aunt_shi_first", true);
                }
                if (progress.currentJourneyNpcDialogues >= 30)
                    UnlockWithoutSaving("chatterbox", true);
                SaveProgressLocked();
            }
        }

        public static void RecordFinalBookCollected()
        {
            Initialize();
            lock (Gate)
            {
                progress.currentChapter = 2;
                progress.chapterOneCompleted = true;
                progress.chapterTwoCompleted = true;
                progress.terminalBookPending = false;
                progress.finalBookCollected = true;
                progress.checkpoint = "FinalBookCollected";
                UnlockWithoutSaving("journey_complete", true);
                if (progress.currentJourneyDeaths == 0) UnlockWithoutSaving("one_life_clear", true);
                SaveProgressLocked();
            }
        }

        public static bool IsAchievementUnlocked(string id)
        {
            Initialize();
            lock (Gate) return progress.unlockedAchievements.Contains(id);
        }

        public static bool UnlockAchievement(string id, bool showToast = true)
        {
            Initialize();
            lock (Gate)
            {
                bool unlocked = UnlockWithoutSaving(id, showToast);
                if (unlocked) SaveProgressLocked();
                return unlocked;
            }
        }

        public static int UnlockedAchievementCount
        {
            get
            {
                Initialize();
                lock (Gate) return progress.unlockedAchievements.Count;
            }
        }

        public static string GetContinueSummary()
        {
            Initialize();
            if (progress.terminalBookPending) return "终章 · 终末之书已在梅溪村显现";
            if (progress.currentChapter <= 1) return "第一章 · 溪流里的节拍";
            return progress.finalBookCollected
                ? "终末之书已取得  ·  可重返古戏台"
                : "第二章 · 古戏台的回响";
        }

        public static void DeleteProgressSave()
        {
            Initialize();
            lock (Gate)
            {
                DeleteIfExists(ProgressFilePath);
                DeleteIfExists(ProgressFilePath + ".bak");
                DeleteIfExists(ProgressFilePath + ".tmp");
                DeleteIfExists(SettingsFilePath);
                DeleteIfExists(SettingsFilePath + ".bak");
                DeleteIfExists(SettingsFilePath + ".tmp");
                progress = new MeixiOfflineSaveData
                {
                    schemaVersion = CurrentSchema,
                    hasEnteredGame = false,
                    currentChapter = 1,
                    checkpoint = "ChapterOne",
                    unlockedAchievements = new List<string>(),
                    achievementRecords = new List<MeixiAchievementUnlockRecord>()
                };
                settings = new MeixiOfflineSettingsData();
                AudioListener.volume = settings.masterVolume;
                MeixiAchievementToast.ClearPending();
                SaveProgressLocked();
                SaveSettingsLocked();
            }
        }

        private static bool UnlockWithoutSaving(string id, bool showToast)
        {
            if (string.IsNullOrWhiteSpace(id)) return false;
            progress.unlockedAchievements ??= new List<string>();
            if (progress.unlockedAchievements.Contains(id)) return false;
            progress.unlockedAchievements.Add(id);
            progress.achievementRecords ??= new List<MeixiAchievementUnlockRecord>();
            progress.achievementRecords.Add(new MeixiAchievementUnlockRecord
            {
                id = id,
                unlockedAtUtc = DateTime.UtcNow.ToString("O")
            });
            if (!showToast) return true;
            for (int i = 0; i < Definitions.Length; i++)
            {
                if (!string.Equals(Definitions[i].Id, id, StringComparison.Ordinal)) continue;
                MeixiAchievementToast.Enqueue(Definitions[i]);
                break;
            }
            return true;
        }

        private static MeixiOfflineSaveData LoadProgress()
        {
            if (!File.Exists(ProgressFilePath)) return new MeixiOfflineSaveData();
            try
            {
                string json = File.ReadAllText(ProgressFilePath, Encoding.UTF8);
                return JsonUtility.FromJson<MeixiOfflineSaveData>(json) ?? new MeixiOfflineSaveData();
            }
            catch (Exception exception)
            {
                string backupPath = ProgressFilePath + ".bak";
                try
                {
                    if (File.Exists(backupPath))
                    {
                        string backupJson = File.ReadAllText(backupPath, Encoding.UTF8);
                        MeixiOfflineSaveData recovered = JsonUtility.FromJson<MeixiOfflineSaveData>(backupJson);
                        if (recovered != null)
                        {
                            Debug.LogWarning("[MeixiSave] Primary progress was invalid; recovered the last atomic backup.");
                            return recovered;
                        }
                    }
                }
                catch (Exception backupException)
                {
                    Debug.LogWarning("[MeixiSave] Backup recovery failed: " + backupException.Message);
                }
                PreserveCorruptFile(ProgressFilePath);
                Debug.LogWarning("[MeixiSave] Progress save was invalid and has been preserved separately: " + exception.Message);
                return new MeixiOfflineSaveData();
            }
        }

        private static MeixiOfflineSettingsData LoadSettings()
        {
            string path = Path.Combine(SaveDirectoryPath, SettingsFileName);
            if (!File.Exists(path)) return new MeixiOfflineSettingsData();
            try
            {
                string json = File.ReadAllText(path, Encoding.UTF8);
                return JsonUtility.FromJson<MeixiOfflineSettingsData>(json) ?? new MeixiOfflineSettingsData();
            }
            catch (Exception exception)
            {
                PreserveCorruptFile(path);
                Debug.LogWarning("[MeixiSave] Settings file was invalid and has been reset: " + exception.Message);
                return new MeixiOfflineSettingsData();
            }
        }

        private static void NormalizeProgress()
        {
            progress ??= new MeixiOfflineSaveData();
            int sourceSchema = progress.schemaVersion;
            bool legacyChapterTwo = progress.chapterOneCompleted || progress.finalBookCollected ||
                                    string.Equals(progress.checkpoint, "ChapterTwo", StringComparison.Ordinal);
            if (sourceSchema < 4)
            {
                progress.chapterTwoCompleted = progress.finalBookCollected;
                progress.terminalBookPending = false;
            }
            if (sourceSchema < 3 || progress.currentChapter < 1)
                progress.currentChapter = legacyChapterTwo ? 2 : 1;
            progress.currentChapter = Mathf.Clamp(progress.currentChapter, 1, 2);
            if (progress.terminalBookPending)
            {
                progress.currentChapter = 1;
                progress.chapterOneCompleted = true;
                progress.chapterTwoCompleted = true;
                progress.finalBookCollected = false;
                progress.checkpoint = "TerminalBook";
            }
            else if (progress.currentChapter >= 2)
            {
                progress.chapterOneCompleted = true;
                progress.checkpoint = "ChapterTwo";
            }
            else
            {
                progress.chapterOneCompleted = false;
                if (string.IsNullOrWhiteSpace(progress.checkpoint) || progress.checkpoint == "None")
                    progress.checkpoint = "ChapterOne";
            }
            progress.schemaVersion = CurrentSchema;
            progress.currentJourneyDeaths = Math.Min(MaxSafeCounter, Math.Max(0, progress.currentJourneyDeaths));
            progress.totalDeaths = Math.Min(MaxSafeCounter, Math.Max(0, progress.totalDeaths));
            progress.currentJourneyDeaths = Math.Min(progress.currentJourneyDeaths, progress.totalDeaths);
            progress.journeyCount = Math.Min(MaxSafeCounter + 1, Math.Max(1, progress.totalDeaths + 1));
            progress.currentJourneyNpcDialogues = Math.Min(MaxSafeCounter, Math.Max(0, progress.currentJourneyNpcDialogues));
            progress.checkpoint ??= "ChapterOne";
            progress.unlockedAchievements ??= new List<string>();
            progress.achievementRecords ??= new List<MeixiAchievementUnlockRecord>();
            var unique = new HashSet<string>(progress.unlockedAchievements, StringComparer.Ordinal);
            progress.unlockedAchievements = new List<string>(unique);
            var recordedIds = new HashSet<string>(StringComparer.Ordinal);
            progress.achievementRecords.RemoveAll(record => record == null || string.IsNullOrWhiteSpace(record.id) ||
                                                                  !recordedIds.Add(record.id));
            foreach (string id in progress.unlockedAchievements)
            {
                if (progress.achievementRecords.Exists(record => record != null &&
                        string.Equals(record.id, id, StringComparison.Ordinal))) continue;
                progress.achievementRecords.Add(new MeixiAchievementUnlockRecord
                {
                    id = id,
                    unlockedAtUtc = progress.savedAtUtc ?? string.Empty
                });
            }
            settings ??= new MeixiOfflineSettingsData();
        }

        private static void SaveProgressLocked()
        {
            progress.schemaVersion = CurrentSchema;
            progress.savedAtUtc = DateTime.UtcNow.ToString("O");
            WriteAtomic(ProgressFilePath, JsonUtility.ToJson(progress, true));
        }

        private static void SaveSettingsLocked()
        {
            settings.schemaVersion = CurrentSchema;
            WriteAtomic(Path.Combine(SaveDirectoryPath, SettingsFileName), JsonUtility.ToJson(settings, true));
        }

        private static void WriteAtomic(string path, string json)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path) ?? SaveDirectoryPath);
            string temporary = path + ".tmp";
            string backup = path + ".bak";
            File.WriteAllText(temporary, json, new UTF8Encoding(false));
            if (!File.Exists(path))
            {
                File.Move(temporary, path);
                return;
            }
            try
            {
                File.Replace(temporary, path, backup, true);
            }
            catch
            {
                File.Copy(path, backup, true);
                File.Delete(path);
                File.Move(temporary, path);
            }
        }

        private static void PreserveCorruptFile(string path)
        {
            try
            {
                string preserved = path + ".corrupt-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");
                File.Move(path, preserved);
            }
            catch { }
        }

        private static void DeleteIfExists(string path)
        {
            try { if (File.Exists(path)) File.Delete(path); }
            catch (Exception exception) { Debug.LogWarning("[MeixiSave] Could not delete " + path + ": " + exception.Message); }
        }
    }

    public sealed class MeixiAchievementToast : MonoBehaviour
    {
        private static readonly Queue<MeixiAchievementDefinition> Pending = new();
        private static readonly HashSet<string> PendingIds = new(StringComparer.Ordinal);
        private static MeixiAchievementToast active;
        private MeixiAchievementDefinition current;
        private Texture2D currentIcon;
        private bool showing;
        private float shownAt;
        private Font font;
        private GUIStyle eyebrowStyle;
        private GUIStyle titleStyle;
        private GUIStyle descriptionStyle;
        private GUIStyle rankStyle;
        private AudioSource audioSource;
        private AudioClip achievementClip;

        public static void EnsureHost()
        {
            if (active != null) return;
            GameObject host = new("本地成就提示·常驻");
            DontDestroyOnLoad(host);
            active = host.AddComponent<MeixiAchievementToast>();
        }

        public static void Enqueue(MeixiAchievementDefinition definition)
        {
            if (string.IsNullOrWhiteSpace(definition.Id)) return;
            lock (Pending)
            {
                if (!PendingIds.Add(definition.Id)) return;
                Pending.Enqueue(definition);
            }
            EnsureHost();
        }

        public static void ClearPending()
        {
            lock (Pending)
            {
                Pending.Clear();
                PendingIds.Clear();
            }
            if (active != null)
            {
                active.showing = false;
                active.current = default;
                active.currentIcon = null;
            }
        }

        private void Awake()
        {
            if (active != null && active != this) { Destroy(gameObject); return; }
            active = this;
            DontDestroyOnLoad(gameObject);
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f;
            achievementClip = Resources.Load<AudioClip>("Audio/\u6210\u5c31\u63d0\u793a");
            font = Font.CreateDynamicFontFromOSFont(
                new[] { "Microsoft YaHei", "SimHei", "STKaiti", "Arial" }, 30);
        }

        private GUIStyle NewStyle(int size, Color color, FontStyle style)
        {
            return new GUIStyle(GUI.skin.label)
            {
                font = font,
                fontSize = size,
                fontStyle = style,
                normal = { textColor = color },
                alignment = TextAnchor.MiddleLeft,
                wordWrap = true
            };
        }

        private void Update()
        {
            if (!showing)
            {
                lock (Pending)
                {
                    if (Pending.Count > 0)
                    {
                        current = Pending.Dequeue();
                        currentIcon = string.IsNullOrEmpty(current.IconResource)
                            ? null : Resources.Load<Texture2D>(current.IconResource);
                        if (currentIcon == null && string.Equals(current.Id, "heritage", StringComparison.Ordinal))
                            currentIcon = LoadRuntimeHeritageIcon();
                        if (audioSource != null && achievementClip != null)
                            audioSource.PlayOneShot(achievementClip, .82f);
                        showing = true;
                        shownAt = Time.unscaledTime;
                    }
                }
            }
            if (showing && Time.unscaledTime - shownAt >= 3.6f)
            {
                lock (Pending) PendingIds.Remove(current.Id);
                showing = false;
                current = default;
                currentIcon = null;
            }
        }

        private static Texture2D LoadRuntimeHeritageIcon()
        {
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
                    Texture2D texture = new(2, 2, TextureFormat.RGBA32, true)
                    {
                        name = "HeritageAchievementIcon_Runtime",
                        filterMode = FilterMode.Bilinear,
                        wrapMode = TextureWrapMode.Clamp
                    };
                    if (texture.LoadImage(File.ReadAllBytes(path), false)) return texture;
                    UnityEngine.Object.Destroy(texture);
                }
                catch (IOException)
                {
                    // Optional icon fallback; the achievement remains valid.
                }
            }
            return null;
        }

        private void OnGUI()
        {
            if (!showing) return;
            // GUI.skin is only legal while Unity is inside an OnGUI event.
            // Building these styles in Awake produced a startup ArgumentException.
            eyebrowStyle ??= NewStyle(14, new Color(.58f, .94f, 1f), FontStyle.Bold);
            titleStyle ??= NewStyle(24, new Color(1f, .78f, .30f), FontStyle.Bold);
            descriptionStyle ??= NewStyle(15, new Color(.88f, .93f, .94f), FontStyle.Normal);
            rankStyle ??= NewStyle(27, Color.white, FontStyle.Bold);
            rankStyle.alignment = TextAnchor.MiddleCenter;
            Color rankColor = MeixiLocalProgress.RankColor(current.Rank);
            titleStyle.normal.textColor = rankColor;
            float age = Time.unscaledTime - shownAt;
            float enter = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(age / .34f));
            float exit = 1f - Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((age - 3.0f) / .55f));
            float visibility = Mathf.Min(enter, exit);
            float width = Mathf.Clamp(Screen.width * .38f, 440f, 620f);
            float height = 122f;
            float x = Mathf.Lerp(Screen.width + 18f, Screen.width - width - 34f, visibility);
            float y = 34f;
            Color previous = GUI.color;
            GUI.depth = -1100;
            GUI.color = new Color(0f, 0f, 0f, .42f * visibility);
            GUI.DrawTexture(new Rect(x + 11f, y + 12f, width, height), Texture2D.whiteTexture);
            GUI.color = new Color(.009f, .038f, .052f, .985f * visibility);
            GUI.DrawTexture(new Rect(x, y, width, height), Texture2D.whiteTexture);
            GUI.color = new Color(rankColor.r, rankColor.g, rankColor.b, .16f * visibility);
            GUI.DrawTexture(new Rect(x + 8f, y + 8f, width - 16f, height - 16f), Texture2D.whiteTexture);
            GUI.color = new Color(rankColor.r, rankColor.g, rankColor.b, .95f * visibility);
            GUI.DrawTexture(new Rect(x, y, 8f, height), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(x + 8f, y, width - 8f, 3f), Texture2D.whiteTexture);
            Rect iconRect = new Rect(x + 18f, y + 21f, 76f, 76f);
            GUI.DrawTexture(iconRect, Texture2D.whiteTexture);
            GUI.color = new Color(1f, 1f, 1f, visibility);
            if (currentIcon != null)
            {
                GUI.DrawTexture(iconRect, currentIcon, ScaleMode.ScaleToFit, true);
            }
            else
            {
                GUI.Label(iconRect, "?", rankStyle);
            }
            GUI.Label(new Rect(x + 74f, y + 9f, width - 88f, 18f), "成 就 徽 章 解 锁", eyebrowStyle);
            GUI.Label(new Rect(x + 112f, y + 34f, width - 130f, 38f), current.Title, titleStyle);
            GUI.Label(new Rect(x + 112f, y + 72f, width - 130f, 38f), current.Description, descriptionStyle);
            GUI.color = previous;
        }
    }
}
