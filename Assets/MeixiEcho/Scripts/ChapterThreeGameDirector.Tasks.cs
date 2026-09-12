using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace MeixiEcho
{
    public sealed partial class ChapterThreeGameDirector
    {
        private int miniStrokeRound, miniStrokeBanked;
        private void BeginMiniGame(ChapterThreeTaskKind kind, int bitIndex)
        {
            // A world-interaction toast sits in the same upper-center region as
            // the task archive. Clear it before opening a task so neither layer
            // can obscure the task title or instructions.
            if (toastPanel != null) toastPanel.SetActive(false);
            if (toastText != null) toastText.gameObject.SetActive(false);
            miniGameActive = true;
            miniTaskKind = kind;
            miniTaskBit = bitIndex;
            miniDifficulty = ChapterThreeRules.Difficulty(sanity);
            miniElapsed = 0f;
            miniProgress = 0f;
            miniLastInputAt = -1f;
            miniSuccesses = 0;
            miniMistakes = 0;
            miniCombo = 0;
            miniVariant = Mathf.Abs(currentDay - 1 + (int)kind * 2 + MeixiLocalProgress.PracticeNumber) % 5;
            miniTypingLastLength = 0;
            miniTypingTarget = kind == ChapterThreeTaskKind.Type ? GetTypingPassage() : string.Empty;
            miniStrokeCursor = 0;
            miniStrokeDragging = false;
            miniStrokeRound = 0;
            miniStrokeBanked = 0;
            cleanExpectLeft = true;
            miniTarget = kind switch
            {
                ChapterThreeTaskKind.Carry => 9,
                ChapterThreeTaskKind.Clean => 14,
                ChapterThreeTaskKind.Type => miniTypingTarget.Length,
                _ => StrokeOrders[miniVariant].Length + StrokeOrders[(miniVariant + 1) % StrokeOrders.Length].Length
            };
            miniGamePanel.SetActive(true);
            miniGameFill.fillAmount = 0f;
            SetCarryTaskVisual(kind == ChapterThreeTaskKind.Carry);
            if (miniGameStageText != null) miniGameStageText.text = "阶段 1 / 4  ·  熟悉流程";
            if (miniGameFeedbackText != null)
            {
                miniGameFeedbackText.text = "准备就绪 · 注意节奏与准确度";
                miniGameFeedbackText.color = new Color(.52f, .80f, .75f);
            }
            miniGameTitle.text = ChapterThreeRules.TaskName(kind) + " · " + GetMiniGameVariantName(kind) +
                                 (miniDifficulty == 0 ? string.Empty : miniDifficulty == 1 ? " · 噪声干扰" : " · 理智危机");
            miniGameInstruction.text = kind switch
            {
                ChapterThreeTaskKind.Carry => "四件物品具有不同重量 · 准星进入每轮发光安全区时点击或按 Space 固定",
                ChapterThreeTaskKind.Clean => "根据当天客房状况左右拖动清扫 · 每日分区顺序都会改变",
                ChapterThreeTaskKind.Type => "只使用键盘完整录入实践记录（支持中文输入法）\n“" + miniTypingTarget + "”",
                _ => "完成两幅连续声纹 · 按住左键从 1 依次连线，完成一幅后松开，再开始下一幅"
            };
            SetMiniGameMouseControls(kind);
            SetControlLocked(true, kind != ChapterThreeTaskKind.Type);
            if (workBoard != null) workBoard.gameObject.SetActive(false);
            if (kind == ChapterThreeTaskKind.Clean || kind == ChapterThreeTaskKind.Carry)
            {
                miniCleanMouseControls.SetActive(false);
                miniCarryMouseControls.SetActive(false);
                SetCarryTaskVisual(false);
                miniGameInstruction.text = kind == ChapterThreeTaskKind.Clean ? "擦净六处污渍 → 归位四件物品 → 按检查单顺序完成四项复检" : "先按房号寄存四件行李，再完成五轮固定与配重";
                if (workBoard == null)
                {
                    var board = Panel("实践操作台", miniCleanMouseControls.transform.parent, new Color(.006f,.022f,.024f,.96f),new Vector2(.10f,.15f),new Vector2(.90f,.43f));
                    workBoard = board.AddComponent<ChapterThreeWorkBoard>();
                }
                Canvas.ForceUpdateCanvases();
                workBoard.Begin(uiFont,kind == ChapterThreeTaskKind.Clean,miniVariant,
                    message => { miniProgress++; miniSuccesses++; miniCombo++; miniGameFeedbackText.text=message; },
                    () => { if(miniTaskKind==ChapterThreeTaskKind.Carry) { miniCarryMouseControls.SetActive(true); SetCarryTaskVisual(true); } },
                    message => { miniMistakes++; miniCombo=0; miniGameFeedbackText.text=message; });
            }
            if (kind == ChapterThreeTaskKind.Type)
            {
                miniTypingInput.SetTextWithoutNotify(string.Empty);
                miniTypingInput.characterLimit = miniTypingTarget.Length;
                miniTypingInput.textComponent.color = new Color(.86f, .96f, .92f);
                StartCoroutine(FocusTypingInputNextFrame());
            }
            else if (kind == ChapterThreeTaskKind.OneStroke)
            {
                ConfigureOneStrokePattern();
            }
        }

        private void UpdateMiniGame(Keyboard keyboard)
        {
            miniElapsed += Time.unscaledDeltaTime;
            bool input = false;
            bool correct = false;
            float ratioBeforeInput = Mathf.Clamp01(miniProgress / Mathf.Max(1f, miniTarget));
            int stage = Mathf.Clamp(Mathf.FloorToInt(ratioBeforeInput * 4f) + 1, 1, 4);
            if (miniTaskKind == ChapterThreeTaskKind.Carry) stage = Mathf.Clamp(miniSuccesses - 3,1,5);
            float carryCenter = GetCarryTargetCenter(stage);
            float carryHalfWidth = Mathf.Max(.05f, .105f - miniDifficulty * .013f - (stage - 1) * .008f - miniVariant * .002f);
            carryTargetMin = carryCenter - carryHalfWidth;
            carryTargetMax = carryCenter + carryHalfWidth;
            float carryMarker = GetCarryMarker(stage);
            if (miniTaskKind == ChapterThreeTaskKind.Carry)
            {
                UpdateCarryTaskTarget(carryTargetMin, carryTargetMax);
                UpdateCarryTaskCursor(carryMarker);
            }
            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
            {
                AbortMiniGame();
                return;
            }

            char mouseInput = pendingMiniMouseInput;
            pendingMiniMouseInput = '\0';
            float dragX = Mouse.current != null && Mouse.current.leftButton.isPressed
                ? Mouse.current.delta.ReadValue().x : 0f;
            if (Time.unscaledTime - miniLastInputAt >= .12f)
            {
                switch (miniTaskKind)
                {
                    case ChapterThreeTaskKind.Carry:
                        if (workBoard != null && workBoard.gameObject.activeSelf) break;
                        if ((keyboard != null && keyboard.spaceKey.wasPressedThisFrame) || mouseInput == ' ')
                        {
                            input = true;
                            correct = carryMarker >= carryTargetMin && carryMarker <= carryTargetMax;
                        }
                        break;
                    case ChapterThreeTaskKind.Clean:
                        // The work board reports physical wipe/drop/inspection
                        // actions. Keyboard spam cannot complete housekeeping.
                        break;
                    case ChapterThreeTaskKind.Type:
                        // The focused InputField handles physical keyboard and IME
                        // text. No clickable character keyboard is provided.
                        break;
                    case ChapterThreeTaskKind.OneStroke:
                        UpdateOneStrokeMouse();
                        break;
                }
            }

            if (input)
            {
                miniLastInputAt = Time.unscaledTime;
                if (correct)
                {
                    miniSuccesses++;
                    miniCombo++;
                    miniProgress += 1f;
                    if (miniGameFeedbackText != null)
                    {
                        miniGameFeedbackText.text = miniCombo >= 4 ? $"精准操作  ·  连击 × {miniCombo}" : "操作正确  ·  流程推进";
                        miniGameFeedbackText.color = new Color(.33f, .94f, .72f);
                    }
                }
                else
                {
                    miniMistakes++;
                    miniCombo = 0;
                    // Errors affect the score, not already completed deliveries.
                    if (miniTaskKind == ChapterThreeTaskKind.OneStroke)
                        miniProgress = Mathf.Max(0f, miniProgress - 1f);
                    if (miniGameFeedbackText != null)
                    {
                        miniGameFeedbackText.text = "操作偏差  ·  已重新校准当前步骤";
                        miniGameFeedbackText.color = new Color(1f, .55f, .28f);
                    }
                }
            }

            float ratio = Mathf.Clamp01(miniProgress / miniTarget);
            stage = Mathf.Clamp(Mathf.FloorToInt(ratio * 4f) + 1, 1, 4);
            miniGameFill.fillAmount = ratio;
            if (miniGameStageText != null)
            {
                string stageLabel = stage == 1 ? "熟悉流程" : stage == 2 ? "节奏变化" : stage == 3 ? "复核校准" : "最终确认";
                miniGameStageText.text = $"阶段 {stage} / 4  ·  {stageLabel}";
            }
            miniGameProgressText.text = $"完成度 {ratio * 100f:0}%   ·   用时 {miniElapsed:0.0}s   ·   失误 {miniMistakes}   ·   连击 {miniCombo}\n{GetMiniGameNextPrompt(stage)}";
            if (ratio >= 1f) FinishMiniGame(ratio);
            else if (miniElapsed >= GetMiniGameTimeLimit()) AbortMiniGame("本轮超时 · 可以立即重新尝试");
        }

        private void FinishMiniGame(float completion)
        {
            int attempts = Mathf.Max(1, miniSuccesses + miniMistakes);
            float accuracy = miniSuccesses / (float)attempts;
            float speed = Mathf.Clamp01(1f - miniElapsed / GetMiniGameTimeLimit());
            float score = Mathf.Clamp((completion * .48f + accuracy * .32f + speed * .20f) * 100f, 0f, 100f);
            taskMask |= 1 << miniTaskBit;
            // The document defines the award from the seven-day practice average.
            // Days eight and nine are optional exchange work and therefore do not
            // retroactively change that already-completed assessment.
            if (ChapterThreeRules.CountsTowardAssessment(currentDay)) scoreTotal += score;
            sanity = Mathf.Max(0f, sanity - Mathf.Lerp(1.2f, 3.8f, miniDifficulty / 2f));
            miniGameActive = false;
            miniGamePanel.SetActive(false);
            pendingMiniMouseInput = '\0';
            SetCarryTaskVisual(false);
            if (miniTypingInput != null) miniTypingInput.DeactivateInputField();
            SetControlLocked(false, false);
            SaveProgress();
            ShowToast(ChapterThreeRules.CountsTowardAssessment(currentDay)
                ? $"任务完成 · 评分 {score:0.0}"
                : $"额外交流任务完成 · 过程评分 {score:0.0}（不计七日评定）");
        }

        private void AbortMiniGame(string reason = "任务已暂停，可以稍后重试")
        {
            miniGameActive = false;
            miniGamePanel.SetActive(false);
            pendingMiniMouseInput = '\0';
            SetCarryTaskVisual(false);
            SetControlLocked(false, false);
            if (miniTypingInput != null) miniTypingInput.DeactivateInputField();
            ShowToast(reason);
        }

        private void SetCarryTaskVisual(bool visible)
        {
            if (miniGameCarryTarget != null) miniGameCarryTarget.SetActive(visible);
            if (miniGameCarryCursor != null) miniGameCarryCursor.gameObject.SetActive(visible);
        }

        private void SetMiniGameMouseControls(ChapterThreeTaskKind kind)
        {
            if (miniCarryMouseControls != null) miniCarryMouseControls.SetActive(kind == ChapterThreeTaskKind.Carry);
            if (miniCleanMouseControls != null) miniCleanMouseControls.SetActive(kind == ChapterThreeTaskKind.Clean);
            if (miniTypeMouseControls != null) miniTypeMouseControls.SetActive(kind == ChapterThreeTaskKind.Type);
            if (miniStrokeMouseControls != null) miniStrokeMouseControls.SetActive(kind == ChapterThreeTaskKind.OneStroke);
        }

        private void QueueMiniMouseInput(char value)
        {
            if (!miniGameActive) return;
            pendingMiniMouseInput = value;
        }

        private void UpdateCarryTaskCursor(float normalizedPosition)
        {
            if (miniGameCarryCursor == null) return;
            float marker = Mathf.Clamp01(normalizedPosition);
            miniGameCarryCursor.anchorMin = new Vector2(marker, .05f);
            miniGameCarryCursor.anchorMax = new Vector2(marker, .95f);
            miniGameCarryCursor.offsetMin = new Vector2(-5f, 0f);
            miniGameCarryCursor.offsetMax = new Vector2(5f, 0f);
        }

        private void UpdateCarryTaskTarget(float minimum, float maximum)
        {
            if (miniGameCarryTarget == null) return;
            RectTransform rect = miniGameCarryTarget.GetComponent<RectTransform>();
            if (rect == null) return;
            rect.anchorMin = new Vector2(Mathf.Clamp01(minimum), .12f);
            rect.anchorMax = new Vector2(Mathf.Clamp01(maximum), .88f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private string GetTypingPassage() => TypingPassages[Mathf.Abs(currentDay - 1 + MeixiLocalProgress.PracticeNumber) % TypingPassages.Length];

        private string GetCleaningPattern() => CleaningPatterns[miniVariant % CleaningPatterns.Length];

        private string GetMiniGameVariantName(ChapterThreeTaskKind kind) => kind switch
        {
            ChapterThreeTaskKind.Carry => CarryVariantNames[miniVariant % CarryVariantNames.Length],
            ChapterThreeTaskKind.Clean => CleaningVariantNames[miniVariant % CleaningVariantNames.Length],
            ChapterThreeTaskKind.Type => "田野记录 " + (miniVariant + 1).ToString("00"),
            _ => StrokeVariantNames[miniVariant % StrokeVariantNames.Length]
        };

        private float GetCarryTargetCenter(int stage)
        {
            if (stage == 5) return miniVariant % 2 == 0 ? .35f : .65f;
            return miniVariant switch
            {
                0 => stage switch { 1 => .48f, 2 => .30f, 3 => .69f, _ => .52f },
                1 => stage switch { 1 => .36f, 2 => .64f, 3 => .42f, _ => .72f },
                2 => stage switch { 1 => .62f, 2 => .38f, 3 => .76f, _ => .28f },
                3 => stage switch { 1 => .50f, 2 => .72f, 3 => .32f, _ => .58f },
                _ => stage switch { 1 => .28f, 2 => .52f, 3 => .74f, _ => .44f }
            };
        }

        private float GetCarryMarker(int stage)
        {
            float speed = 2.05f + stage * .23f + miniDifficulty * .42f + miniVariant * .09f;
            float phase = miniElapsed * speed + miniVariant * .71f;
            float marker = miniVariant switch
            {
                1 => Mathf.PingPong(phase * .42f, 1f),
                2 => .5f + Mathf.Sin(phase) * .38f + Mathf.Sin(phase * 2.37f) * .12f,
                3 => .5f + Mathf.Sin(phase + Mathf.Sin(phase * .43f)) * .50f,
                4 => Mathf.PingPong(phase * .34f + Mathf.Sin(phase) * .12f, 1f),
                _ => .5f + Mathf.Sin(phase) * .5f
            };
            return Mathf.Clamp01(marker);
        }

        private float GetMiniGameTimeLimit() => miniTaskKind switch
        {
            ChapterThreeTaskKind.Type => 150f,
            ChapterThreeTaskKind.OneStroke => 60f,
            ChapterThreeTaskKind.Clean => 90f,
            _ => 75f
        };

        private IEnumerator FocusTypingInputNextFrame()
        {
            yield return null;
            if (!miniGameActive || miniTaskKind != ChapterThreeTaskKind.Type || miniTypingInput == null) yield break;
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(miniTypingInput.gameObject);
            miniTypingInput.Select();
            miniTypingInput.ActivateInputField();
        }

        private void OnTypingValueChanged(string value)
        {
            if (!miniGameActive || miniTaskKind != ChapterThreeTaskKind.Type || string.IsNullOrEmpty(miniTypingTarget)) return;
            int prefix = 0;
            int count = Mathf.Min(value.Length, miniTypingTarget.Length);
            while (prefix < count && value[prefix] == miniTypingTarget[prefix]) prefix++;
            bool newMistake = value.Length > miniTypingLastLength && prefix < value.Length;
            if (newMistake)
            {
                miniMistakes++;
                miniCombo = 0;
                miniGameFeedbackText.text = "录入不一致 · 请用退格删除错误字符";
                miniGameFeedbackText.color = new Color(1f, .55f, .28f);
            }
            else if (prefix > miniSuccesses)
            {
                miniCombo += prefix - miniSuccesses;
                miniGameFeedbackText.text = prefix == miniTypingTarget.Length ? "实践记录核对完成" : $"已准确录入 {prefix} 个字符";
                miniGameFeedbackText.color = new Color(.33f, .94f, .72f);
            }
            miniTypingLastLength = value.Length;
            miniSuccesses = prefix;
            miniProgress = prefix;
            if (miniTypingInput != null)
                miniTypingInput.textComponent.color = prefix == value.Length ? new Color(.86f, .96f, .92f) : new Color(1f, .52f, .30f);
        }

        private void ConfigureOneStrokePattern()
        {
            for (int i = miniStrokeMouseControls.transform.childCount - 1; i >= 0; i--)
            {
                GameObject child = miniStrokeMouseControls.transform.GetChild(i).gameObject;
                child.SetActive(false);
                Destroy(child);
            }
            miniStrokeNodes.Clear();
            miniStrokeNodeImages.Clear();
            miniStrokeLines.Clear();
            miniStrokeLayout = StrokeNodeLayouts[miniVariant % StrokeNodeLayouts.Length];
            int[] order = StrokeOrders[miniVariant % StrokeOrders.Length];

            for (int i = 0; i < order.Length - 1; i++)
            {
                GameObject lineObject = Panel("已连接线_" + i, miniStrokeMouseControls.transform,
                    new Color(.20f, .90f, .72f, .92f), new Vector2(.5f, .5f), new Vector2(.5f, .5f));
                RectTransform line = lineObject.GetComponent<RectTransform>();
                line.pivot = new Vector2(0f, .5f);
                lineObject.GetComponent<Image>().raycastTarget = false;
                lineObject.SetActive(false);
                miniStrokeLines.Add(line);
            }
            GameObject liveLineObject = Panel("当前连线", miniStrokeMouseControls.transform,
                new Color(1f, .72f, .28f, .78f), new Vector2(.5f, .5f), new Vector2(.5f, .5f));
            miniStrokeLiveLine = liveLineObject.GetComponent<RectTransform>();
            miniStrokeLiveLine.pivot = new Vector2(0f, .5f);
            liveLineObject.GetComponent<Image>().raycastTarget = false;
            liveLineObject.SetActive(false);

            Sprite knob = MeixiUiSprites.Round;
            float size = 40f - miniDifficulty * 3f;
            for (int i = 0; i < miniStrokeLayout.Length; i++)
            {
                Vector2 anchor = miniStrokeLayout[i];
                GameObject node = Panel("连线节点_" + i, miniStrokeMouseControls.transform,
                    new Color(.12f, .32f, .31f, 1f), anchor, anchor);
                RectTransform rect = node.GetComponent<RectTransform>();
                rect.sizeDelta = new Vector2(size, size);
                Image image = node.GetComponent<Image>();
                image.raycastTarget = false;
                if (knob != null) image.sprite = knob;
                Outline outline = node.AddComponent<Outline>();
                outline.effectColor = new Color(1f, .72f, .28f, .92f);
                outline.effectDistance = new Vector2(2f, -2f);
                int firstVisit = Array.IndexOf(order, i) + 1;
                Text number = Label("节点序号", node.transform, firstVisit.ToString(), 17, TextAnchor.MiddleCenter,
                    new Color(1f, .88f, .52f), Vector2.zero, Vector2.one);
                number.fontStyle = FontStyle.Bold;
                miniStrokeNodes.Add(rect);
                miniStrokeNodeImages.Add(image);
            }
            Canvas.ForceUpdateCanvases();
            ResetOneStrokeTrace(true);
        }

        private void UpdateOneStrokeMouse()
        {
            Mouse mouse = Mouse.current;
            if (mouse == null || miniStrokeNodes.Count == 0) return;
            ProcessOneStrokePointer(mouse.position.ReadValue(), mouse.leftButton.wasPressedThisFrame,
                mouse.leftButton.isPressed, mouse.leftButton.wasReleasedThisFrame);
        }

        private void ProcessOneStrokePointer(Vector2 screen, bool pressed, bool held, bool released)
        {
            int[] order = StrokeOrders[miniVariant % StrokeOrders.Length];
            int hovered = FindStrokeNode(screen);

            if (pressed)
            {
                if (hovered == order[0])
                {
                    ResetOneStrokeTrace(true);
                    miniStrokeDragging = true;
                    miniStrokeCursor = 1;
                    miniProgress = miniStrokeBanked + 1f;
                    miniSuccesses = miniStrokeBanked + 1;
                    miniCombo = 1;
                    SetStrokeNodeCompleted(order[0]);
                }
                else if (hovered >= 0)
                {
                    miniMistakes++;
                    miniGameFeedbackText.text = "请从编号 1 的节点开始连线";
                    miniGameFeedbackText.color = new Color(1f, .55f, .28f);
                }
            }

            if (miniStrokeDragging && held)
            {
                int previousNode = order[Mathf.Max(0, miniStrokeCursor - 1)];
                UpdateStrokeLine(miniStrokeLiveLine, StrokePointLocal(previousNode), MousePointLocal(screen));
                miniStrokeLiveLine.gameObject.SetActive(true);
                if (miniStrokeCursor < order.Length && hovered >= 0)
                {
                    int expected = order[miniStrokeCursor];
                    if (hovered == expected)
                    {
                        RectTransform fixedLine = miniStrokeLines[miniStrokeCursor - 1];
                        UpdateStrokeLine(fixedLine, StrokePointLocal(previousNode), StrokePointLocal(expected));
                        fixedLine.gameObject.SetActive(true);
                        SetStrokeNodeCompleted(expected);
                        miniStrokeCursor++;
                        miniProgress = miniStrokeBanked + miniStrokeCursor;
                        miniSuccesses = miniStrokeBanked + miniStrokeCursor;
                        miniCombo = miniStrokeCursor;
                        if (miniStrokeCursor >= order.Length)
                        {
                            miniStrokeDragging = false;
                            miniStrokeLiveLine.gameObject.SetActive(false);
                            miniGameFeedbackText.text = "图案一笔完成 · 声纹已经归档";
                            miniGameFeedbackText.color = new Color(.33f, .94f, .72f);
                            if (miniStrokeRound == 0)
                            {
                                miniStrokeBanked = order.Length;
                                miniStrokeRound = 1;
                                miniVariant = (miniVariant + 1) % StrokeOrders.Length;
                                ConfigureOneStrokePattern();
                                miniGameFeedbackText.text = "第一幅已归档 · 松开鼠标，再从新图的 1 开始";
                            }
                        }
                    }
                    else if (hovered != previousNode)
                    {
                        miniMistakes++;
                        miniGameFeedbackText.text = "碰到了错误节点 · 请重新从 1 开始";
                        miniGameFeedbackText.color = new Color(1f, .55f, .28f);
                        ResetOneStrokeTrace(true);
                    }
                }
            }

            if (released && miniStrokeDragging)
            {
                miniMistakes++;
                miniGameFeedbackText.text = "连线中断 · 按住鼠标不要松开";
                miniGameFeedbackText.color = new Color(1f, .55f, .28f);
                ResetOneStrokeTrace(true);
            }
        }

        private int FindStrokeNode(Vector2 screenPosition)
        {
            for (int i = 0; i < miniStrokeNodes.Count; i++)
                if (RectTransformUtility.RectangleContainsScreenPoint(miniStrokeNodes[i], screenPosition, null)) return i;
            return -1;
        }

        private Vector2 StrokePointLocal(int nodeIndex)
        {
            RectTransform canvasRect = miniStrokeMouseControls.GetComponent<RectTransform>();
            Vector2 normalized = miniStrokeLayout[nodeIndex];
            return new Vector2((normalized.x - canvasRect.pivot.x) * canvasRect.rect.width,
                (normalized.y - canvasRect.pivot.y) * canvasRect.rect.height);
        }

        private Vector2 MousePointLocal(Vector2 screenPosition)
        {
            RectTransform canvasRect = miniStrokeMouseControls.GetComponent<RectTransform>();
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPosition, null, out Vector2 local);
            return local;
        }

        private static void UpdateStrokeLine(RectTransform line, Vector2 from, Vector2 to)
        {
            Vector2 delta = to - from;
            line.anchoredPosition = from;
            line.sizeDelta = new Vector2(delta.magnitude, 7f);
            line.localEulerAngles = new Vector3(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
        }

        private void SetStrokeNodeCompleted(int nodeIndex)
        {
            if (nodeIndex < 0 || nodeIndex >= miniStrokeNodeImages.Count) return;
            miniStrokeNodeImages[nodeIndex].color = new Color(.16f, .78f, .62f, 1f);
        }

        private void ResetOneStrokeTrace(bool resetProgress)
        {
            miniStrokeDragging = false;
            miniStrokeCursor = 0;
            for (int i = 0; i < miniStrokeLines.Count; i++) miniStrokeLines[i].gameObject.SetActive(false);
            if (miniStrokeLiveLine != null) miniStrokeLiveLine.gameObject.SetActive(false);
            for (int i = 0; i < miniStrokeNodeImages.Count; i++) miniStrokeNodeImages[i].color = new Color(.12f, .32f, .31f, 1f);
            if (!resetProgress) return;
            miniProgress = miniStrokeBanked;
            miniSuccesses = miniStrokeBanked;
            miniCombo = 0;
        }

        private string GetMiniGameNextPrompt(int stage)
        {
            if (miniTaskKind == ChapterThreeTaskKind.Carry)
                return workBoard != null && workBoard.gameObject.activeSelf ? "先核对房号完成分拣" :
                    $"{CarryVariantNames[miniVariant]} · 第 {Mathf.Clamp(miniSuccesses-3,1,5)} / 5 轮校准 · 点击或 SPACE 固定";
            if (miniTaskKind == ChapterThreeTaskKind.Clean)
            {
                string pattern = GetCleaningPattern();
                return "按住左键擦拭 · 拖放归位 · 点击复检";
            }
            if (miniTaskKind == ChapterThreeTaskKind.Type)
                return $"已核对 {Mathf.FloorToInt(miniProgress)} / {miniTypingTarget.Length} 字 · 错误字符需退格修正";
            int[] order = StrokeOrders[miniVariant % StrokeOrders.Length];
            return $"声纹 {miniStrokeRound+1} / 2 · " + (miniStrokeCursor <= 0 ? "按住鼠标，从节点 1 开始" :
                miniStrokeCursor >= order.Length ? "图案完成" : $"保持按住 · 连接节点 {miniStrokeCursor + 1} / {order.Length}");
        }

        private static bool WasDirectionPressed(Keyboard keyboard, char value) => value switch
        {
            'W' => keyboard.wKey.wasPressedThisFrame,
            'A' => keyboard.aKey.wasPressedThisFrame,
            'S' => keyboard.sKey.wasPressedThisFrame,
            'D' => keyboard.dKey.wasPressedThisFrame,
            _ => false
        };

    }
}
