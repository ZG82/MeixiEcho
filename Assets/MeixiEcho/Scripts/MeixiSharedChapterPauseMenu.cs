using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MeixiEcho
{
    /// <summary>
    /// Chapter-one visual language reused by later playable chapters. Input is
    /// deliberately owned by the chapter director so Escape cannot race another
    /// modal (inventory, keypad, sleep selector or ending presentation).
    /// </summary>
    public sealed class MeixiSharedChapterPauseMenu : MonoBehaviour
    {
        private GameObject panel;
        private GameObject settingsPanel;
        private Text recordText;
        private Text settingsVolumeText;
        private Text settingsAchievementText;
        private Action<bool> pauseChanged;
        private Action restartAction;
        private Action returnAction;
        private Func<string> recordProvider;
        private float previousTimeScale = 1f;
        private bool settingsOpen;
        private Font uiFont;
        private Action archiveAction;

        public bool IsPaused { get; private set; }
        public bool IsSettingsOpen => settingsOpen;
        public GameObject PanelRoot => panel;

        public void Initialize(Canvas canvas, string chapterLabel, Func<string> practiceRecord,
            Action<bool> onPauseChanged, Action onRestart = null, Action onReturn = null, string controls = null,
            Action onArchive = null)
        {
            if (canvas == null) return;
            if (panel != null)
            {
                // Chapter two hands off from the theatre controller to investigation.
                // Both receive state changes but share one visible menu.
                pauseChanged -= onPauseChanged;
                pauseChanged += onPauseChanged;
                recordProvider = practiceRecord;
                return;
            }
            pauseChanged = onPauseChanged;
            restartAction = onRestart;
            returnAction = onReturn;
            recordProvider = practiceRecord;
            archiveAction = onArchive;

            uiFont = Font.CreateDynamicFontFromOSFont(
                new[] { "Microsoft YaHei", "SimHei", "Arial" }, 30);
            panel = CreatePanel("共享章节暂停界面", canvas.transform,
                new Color(.003f, .012f, .020f, .82f), Vector2.zero, Vector2.one);
            Canvas overlayCanvas = panel.AddComponent<Canvas>();
            overlayCanvas.overrideSorting = true;
            overlayCanvas.sortingOrder = 980;
            panel.AddComponent<GraphicRaycaster>();

            GameObject card = CreatePanel("暂停主卡", panel.transform,
                new Color(.018f, .055f, .080f, .98f), new Vector2(.34f, .16f), new Vector2(.66f, .84f));
            PolishCard(card, new Color(1f, .55f, .17f, .65f));
            card.AddComponent<MeixiUiPanelReveal>().Configure(.24f, .90f);

            // Keep the visible hierarchy pixel-compatible with the first/two
            // chapter pause card. Chapter-specific progress belongs in the
            // record line, not in a different eyebrow or frame treatment.
            CreateText("暂停眉题", card.transform, "MEIXI RESONANCE", 16,
                TextAnchor.MiddleCenter, new Color(.32f, .82f, .92f), new Vector2(.12f, .84f), new Vector2(.88f, .92f), uiFont);
            Text title = CreateText("暂停标题", card.transform, "魂 音 暂 歇", 48,
                TextAnchor.MiddleCenter, new Color(1f, .78f, .34f), new Vector2(.08f, .68f), new Vector2(.92f, .84f), uiFont);
            title.fontStyle = FontStyle.Bold;
            AddTextShadow(title);
            recordText = CreateText("暂停实践记录", card.transform, string.Empty, 15,
                TextAnchor.MiddleCenter, new Color(.48f, .90f, .88f), new Vector2(.10f, .615f), new Vector2(.90f, .685f), uiFont);
            recordText.lineSpacing = 1.12f;
            CreateText("暂停控制提示", card.transform,
                controls ?? "WASD  行走     鼠标  观察\nE  调查与互动     ESC  返回游戏\n靠近物件，聆听并记录梅溪的回响", 19,
                TextAnchor.MiddleCenter, new Color(.74f, .87f, .93f), new Vector2(.09f, .445f), new Vector2(.91f, .595f), uiFont);

            Button resume = CreateButton("继续游戏", card.transform, "继 续 聆 听",
                new Color(.62f, .18f, .07f, .98f), new Vector2(.18f, .33f), new Vector2(.82f, .44f), uiFont);
            resume.onClick.AddListener(() => SetPaused(false));
            Button settings = CreateButton("设置与成就", card.transform, "设 置 · 成 就",
                new Color(.13f, .16f, .34f, .98f), new Vector2(.18f, .20f), new Vector2(.49f, .31f), uiFont);
            settings.onClick.AddListener(OpenSettings);
            Button back = CreateButton("返回梅溪村", card.transform, "返 回 梅 溪 村",
                new Color(.06f, .24f, .31f, .98f), new Vector2(.51f, .20f), new Vector2(.82f, .31f), uiFont);
            back.onClick.AddListener(ReturnToVillage);
            Button quit = CreateButton("退出游戏", card.transform, "退 出 游 戏",
                new Color(.30f, .07f, .08f, .98f), new Vector2(.18f, .07f), new Vector2(.82f, .18f), uiFont);
            quit.onClick.AddListener(Application.Quit);
            CreateText("暂停页脚", card.transform, "ESC  返回游戏", 14,
                TextAnchor.MiddleCenter, new Color(.48f, .72f, .79f), new Vector2(.15f, .005f), new Vector2(.85f, .06f), uiFont);

            BuildSettingsPage();
            panel.SetActive(false);
        }

        public void Toggle() => SetPaused(!IsPaused);

        public void HandleEscape()
        {
            if (!IsPaused)
            {
                SetPaused(true);
                return;
            }
            if (settingsOpen)
            {
                CloseSettings();
                return;
            }
            SetPaused(false);
        }

        public void SetPaused(bool value)
        {
            if (panel == null || IsPaused == value) return;
            if (value) previousTimeScale = Mathf.Approximately(Time.timeScale, 0f) ? 1f : Time.timeScale;
            IsPaused = value;
            if (!value) CloseSettings();
            if (recordText != null) recordText.text = recordProvider?.Invoke() ?? string.Empty;
            panel.SetActive(value);
            if (value)
            {
                RefreshSettings();
                panel.transform.SetAsLastSibling();
            }
            Time.timeScale = value ? 0f : Mathf.Max(.01f, previousTimeScale);
            AudioListener.pause = value;
            if (value) MeixiCursorState.SetPointer();
            else MeixiCursorState.SetGameplay();
            // Discard the click/delta used to dismiss a menu before gameplay
            // consumers run later in this frame.
            MeixiCursorState.BlockGameplayInput();
            pauseChanged?.Invoke(value);
        }

        private void BuildSettingsPage()
        {
            settingsPanel = CreatePanel("共享设置与成就页", panel.transform,
                new Color(.002f, .009f, .014f, .96f), Vector2.zero, Vector2.one);
            GameObject card = CreatePanel("设置成就主卡", settingsPanel.transform,
                new Color(.008f, .035f, .046f, .998f), new Vector2(.27f, .14f), new Vector2(.73f, .86f));
            PolishCard(card, new Color(.30f, .84f, .90f, .72f));
            card.AddComponent<MeixiUiPanelReveal>().Configure(.20f, .94f);
            CreateText("设置成就眉题", card.transform, "OFFLINE ARCHIVE  /  LOCAL USER DATA", 14,
                TextAnchor.MiddleCenter, new Color(.42f, .82f, .88f), new Vector2(.10f, .89f), new Vector2(.90f, .95f), uiFont);
            CreateText("设置成就标题", card.transform, "设 置 · 成 就 档 案", 38,
                TextAnchor.MiddleCenter, new Color(1f, .76f, .30f), new Vector2(.08f, .79f), new Vector2(.92f, .89f), uiFont);

            GameObject volumeCard = CreatePanel("本地音量设置", card.transform,
                new Color(.015f, .076f, .091f, .94f), new Vector2(.08f, .64f), new Vector2(.92f, .77f));
            AddOutline(volumeCard, new Color(.26f, .68f, .74f, .46f));
            settingsVolumeText = CreateText("主音量文字", volumeCard.transform, string.Empty, 18,
                TextAnchor.MiddleCenter, new Color(.84f, .94f, .94f), new Vector2(.22f, .08f), new Vector2(.78f, .92f), uiFont);
            Button down = CreateButton("降低音量", volumeCard.transform, "－", new Color(.07f, .21f, .28f, .98f),
                new Vector2(.04f, .14f), new Vector2(.18f, .86f), uiFont);
            Button up = CreateButton("提高音量", volumeCard.transform, "＋", new Color(.10f, .28f, .27f, .98f),
                new Vector2(.82f, .14f), new Vector2(.96f, .86f), uiFont);
            down.onClick.AddListener(() => AdjustMasterVolume(-.10f));
            up.onClick.AddListener(() => AdjustMasterVolume(.10f));

            GameObject achievementCard = CreatePanel("成就档案摘要", card.transform,
                new Color(.008f, .028f, .038f, .94f), new Vector2(.08f, .37f), new Vector2(.92f, .60f));
            AddOutline(achievementCard, new Color(.25f, .58f, .64f, .40f));
            settingsAchievementText = CreateText("成就完成度", achievementCard.transform, string.Empty, 18,
                TextAnchor.MiddleCenter, new Color(.55f, .91f, .95f), new Vector2(.06f, .18f), new Vector2(.94f, .82f), uiFont);
            CreateText("设置说明", card.transform,
                archiveAction == null ? "三章共用同一份本地档案\n音量设置即时保存" : string.Empty, 14,
                TextAnchor.MiddleCenter, new Color(.58f, .76f, .78f), new Vector2(.10f, .26f), new Vector2(.90f, .36f), uiFont);

            Button restart = CreateButton("设置页重新开始", card.transform, "重 新 启 程",
                new Color(.06f, .24f, .31f, .98f), new Vector2(.10f, .08f), new Vector2(.47f, .22f), uiFont);
            restart.onClick.AddListener(Restart);
            Button close = CreateButton("返回暂停页", card.transform, "返 回 暂 停 页",
                new Color(.055f, .30f, .34f, .99f), new Vector2(.53f, .08f), new Vector2(.90f, .22f), uiFont);
            close.onClick.AddListener(CloseSettings);
            if (archiveAction != null)
            {
                Button archive = CreateButton("完整档案", card.transform, "详细成就与终端", new Color(.10f,.24f,.28f),
                    new Vector2(.18f,.24f), new Vector2(.82f,.34f), uiFont);
                archive.onClick.AddListener(() => { CloseSettings(); archiveAction(); });
            }
            settingsPanel.SetActive(false);
        }

        private void OpenSettings()
        {
            if (!IsPaused || settingsPanel == null) return;
            settingsOpen = true;
            RefreshSettings();
            settingsPanel.SetActive(true);
            settingsPanel.transform.SetAsLastSibling();
            MeixiCursorState.SetPointer();
        }

        private void CloseSettings()
        {
            settingsOpen = false;
            if (settingsPanel != null) settingsPanel.SetActive(false);
        }

        private void AdjustMasterVolume(float delta)
        {
            MeixiLocalProgress.SetMasterVolume(MeixiLocalProgress.MasterVolume + delta);
            RefreshSettings();
        }

        private void RefreshSettings()
        {
            if (settingsVolumeText != null)
                settingsVolumeText.text = $"主 音 量    {Mathf.RoundToInt(MeixiLocalProgress.MasterVolume * 100f)}%";
            if (settingsAchievementText != null)
                settingsAchievementText.text =
                    $"成 就 档 案    {MeixiLocalProgress.UnlockedAchievementCount:00} / {MeixiLocalProgress.AchievementDefinitions.Count:00}\n\n" +
                    $"实 践 次 数    {MeixiLocalProgress.PracticeNumber:00}    ·    累 计 倒 下    {MeixiLocalProgress.TotalDeaths:00}";
        }

        private void Restart()
        {
            SetPaused(false);
            if (restartAction != null) restartAction();
            else SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        private void ReturnToVillage()
        {
            SetPaused(false);
            if (returnAction != null) returnAction();
            else SceneManager.LoadScene("MeixiEditable");
        }

        private static GameObject CreatePanel(string name, Transform parent, Color color, Vector2 min, Vector2 max)
        {
            GameObject root = new(name);
            root.transform.SetParent(parent, false);
            Image image = root.AddComponent<Image>();
            image.color = color;
            SetRect(image.rectTransform, min, max);
            return root;
        }

        private static Text CreateText(string name, Transform parent, string value, int size, TextAnchor alignment,
            Color color, Vector2 min, Vector2 max, Font font)
        {
            GameObject root = new(name);
            root.transform.SetParent(parent, false);
            Text text = root.AddComponent<Text>();
            text.text = value;
            text.font = font;
            text.fontSize = size;
            text.alignment = alignment;
            text.color = color;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.raycastTarget = false;
            SetRect(text.rectTransform, min, max);
            return text;
        }

        private static Button CreateButton(string name, Transform parent, string label, Color color,
            Vector2 min, Vector2 max, Font font)
        {
            GameObject root = CreatePanel(name, parent, color, min, max);
            Image buttonImage = root.GetComponent<Image>();
            ApplyUiTexture(buttonImage, "Art/QuestFrameV2");
            buttonImage.color = Color.white;
            Button button = root.AddComponent<Button>();
            button.targetGraphic = buttonImage;
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(.72f, .93f, 1f, 1f);
            colors.pressedColor = new Color(1f, .67f, .28f, 1f);
            colors.selectedColor = colors.highlightedColor;
            button.colors = colors;
            Text text = CreateText("标签", root.transform, label, 28, TextAnchor.MiddleCenter,
                Color.white, Vector2.zero, Vector2.one, font);
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = 14;
            text.resizeTextMaxSize = 28;
            Shadow shadow = text.gameObject.AddComponent<Shadow>();
            shadow.effectColor = Color.black;
            shadow.effectDistance = new Vector2(2f, -2f);
            return button;
        }

        private static void PolishCard(GameObject card, Color outlineColor)
        {
            Shadow shadow = card.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, .72f);
            shadow.effectDistance = new Vector2(7f, -7f);
            AddOutline(card, outlineColor, 1.2f);
        }

        private static void ApplyUiTexture(Image image, string resourcePath)
        {
            if (image == null || string.IsNullOrWhiteSpace(resourcePath)) return;
            Texture2D texture = Resources.Load<Texture2D>(resourcePath);
            if (texture == null) return;
            image.sprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(.5f, .5f), 100f);
            image.preserveAspect = false;
        }

        private static void AddOutline(GameObject target, Color color, float distance = 1f)
        {
            Outline outline = target.AddComponent<Outline>();
            outline.effectColor = color;
            outline.effectDistance = new Vector2(distance, -distance);
        }

        private static void AddTextShadow(Text text)
        {
            Shadow shadow = text.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, .90f);
            shadow.effectDistance = new Vector2(3f, -3f);
        }

        private static void SetRect(RectTransform rect, Vector2 min, Vector2 max)
        {
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private void OnDestroy()
        {
            if (!IsPaused) return;
            Time.timeScale = Mathf.Max(.01f, previousTimeScale);
            AudioListener.pause = false;
        }
    }
}
