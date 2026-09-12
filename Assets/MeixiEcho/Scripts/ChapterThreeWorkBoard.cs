using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace MeixiEcho
{
    // Pointer work surface shared by luggage and room-service activities.
    // Coordinates are canvas-local so resolution and UI scaling do not affect hits.
    public sealed class ChapterThreeWorkBoard : MonoBehaviour
    {
        private sealed class Item
        {
            public RectTransform Rect;
            public Image Image;
            public Vector2 Home;
            public float Dirt = 1f;
            public bool Done;
            public int Destination;
        }
        private readonly List<Item> items = new();
        private readonly List<RectTransform> destinations = new();
        private Font font;
        private Text hint;
        private RectTransform surface;
        private Action<string> step;
        private Action finished;
        private Action<string> mistake;
        private int inspectionStep;
        private bool cleaning;
        private int variant, phase;
        private Item dragged;
        private Vector2 lastPointer;
        private bool hasLastPointer;

        public void Begin(Font textFont, bool clean, int dailyVariant, Action<string> onStep, Action onFinished, Action<string> onMistake = null)
        {
            font = textFont; cleaning = clean; variant = dailyVariant; step = onStep; finished = onFinished;
            phase = 0; inspectionStep = 0; mistake = onMistake; surface = (RectTransform)transform;
            gameObject.SetActive(true);
            BuildPhase();
        }

        private void Clear()
        {
            foreach (Transform child in transform) { child.gameObject.SetActive(false); Destroy(child.gameObject); }
            items.Clear(); destinations.Clear(); dragged = null; hasLastPointer = false;
        }

        private void BuildPhase()
        {
            Clear();
            hint = TextAt("操作说明", cleaning ? (phase == 0 ? "按住左键，往返擦净六处顽固污渍" : phase == 1 ? "拖动物品到对应收纳区" : InspectionHint()) : "核对四件行李的房号，拖入对应寄存格", new Vector2(.5f,.91f), new Vector2(.98f,.17f), 18);
            int count = cleaning && phase == 0 ? 6 : 4;
            string[] labels = cleaning ? new[] { "毛巾", "洗漱用品", "记录册", "茶具" } : new[] { "101", "102", "202", "203" };
            if ((!cleaning || phase == 1))
                for (int i = 0; i < 4; i++)
                {
                    var slot = Rect("收纳格", new Vector2(.14f + .24f * i,.25f), new Vector2(.21f,.32f), new Color(.07f,.24f,.23f));
                    TextAt("收纳标签", labels[(i + variant) % 4], new Vector2(.14f+.24f*i,.25f), new Vector2(.20f,.25f), 18);
                    destinations.Add(slot);
                }
            for (int i = 0; i < count; i++)
            {
                Vector2 pos = cleaning && phase == 0 ? new Vector2(.18f + (i % 3)*.32f,.25f+(i/3)*.39f) :
                    new Vector2(.14f+.24f*i, phase == 2 ? .48f : .67f);
                if (cleaning && phase == 0) pos.x += (variant-2)*.018f;
                string label = cleaning && phase == 0 ? "污渍" : cleaning && phase == 2 ? InspectionLabels[i] : labels[i];
                RectTransform rect = Rect(label, pos, new Vector2(.19f,.23f), cleaning && phase == 0 ? new Color(.49f,.32f,.16f) : new Color(.22f,.49f,.46f));
                Text text = TextAt("物品名称", label, Vector2.one*.5f, Vector2.one, 17, rect);
                text.color = new Color(1f,.90f,.65f);
                rect.GetComponent<Image>().sprite = MeixiUiSprites.Round;
                if (cleaning && phase == 0)
                {
                    rect.GetComponent<Image>().sprite = MeixiUiSprites.Stain;
                    text.text = string.Empty;
                }
                else if (!cleaning)
                {
                    // Case handle, two straps and wheels form a readable luggage silhouette.
                    Detail(rect,new Vector2(.5f,1.07f),new Vector2(.30f,.16f),new Color(.46f,.55f,.55f));
                    for(int side=0;side<2;side++)
                    {
                        Detail(rect,new Vector2(.18f+side*.64f,.5f),new Vector2(.045f,.91f),new Color(.10f,.22f,.23f));
                        Detail(rect,new Vector2(.20f+side*.60f,-.035f),new Vector2(.12f,.11f),new Color(.10f,.14f,.15f));
                    }
                }
                items.Add(new Item { Rect = rect, Image = rect.GetComponent<Image>(), Home = pos, Destination = (i - variant % 4 + 4) % 4 });
            }
        }

        private void Update()
        {
            if (Time.timeScale == 0f || Mouse.current == null || surface == null) return;
            Mouse m = Mouse.current;
            ProcessPointer(m.position.ReadValue(),m.leftButton.isPressed,m.leftButton.wasPressedThisFrame,m.leftButton.wasReleasedThisFrame);
        }

        // One hit-test path for live input and deterministic replay tests.
        public void ProcessPointer(Vector2 screen,bool held,bool pressed,bool released)
        {
            if (surface == null || !gameObject.activeInHierarchy || Time.timeScale == 0f) return;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(surface, screen, null, out Vector2 p);
            Vector2 normalized = new(p.x/surface.rect.width + .5f, p.y/surface.rect.height + .5f);
            float movement = hasLastPointer ? Vector2.Distance(p, lastPointer) : 0f;
            lastPointer = p; hasLastPointer = true;
            foreach (var item in items)
            {
                if (item.Done || !RectTransformUtility.RectangleContainsScreenPoint(item.Rect,screen,null)) continue;
                if (cleaning && phase == 0 && held && movement > .15f)
                {
                    item.Dirt = Mathf.Max(0f,item.Dirt - Mathf.Min(movement,28f)/400f);
                    item.Image.color = new Color(.49f,.32f,.16f,Mathf.Max(.12f,item.Dirt));
                    if (item.Dirt <= 0f) Complete(item,"污渍已擦净");
                }
                else if (cleaning && phase == 2 && pressed)
                {
                    if (items.IndexOf(item) == (variant + inspectionStep) % 4)
                    { inspectionStep++; Complete(item,"检查合格"); hint.text = InspectionHint(); }
                    else { hint.text = "复检顺序不符 · " + InspectionHint(); mistake?.Invoke("请按照检查单顺序复核"); }
                }
                else if ((!cleaning || phase == 1) && pressed && dragged == null) dragged = item;
            }
            if (dragged != null)
            {
                dragged.Rect.anchorMin = dragged.Rect.anchorMax = normalized;
                dragged.Rect.SetAsLastSibling();
                if (released)
                {
                    if (RectTransformUtility.RectangleContainsScreenPoint(destinations[dragged.Destination],screen,null)) Complete(dragged,"物品归位正确");
                    else
                    {
                        dragged.Rect.anchorMin = dragged.Rect.anchorMax = dragged.Home;
                        hint.text = "标签不匹配，请放入对应收纳格";
                        mistake?.Invoke("归位错误 · 请核对标签");
                    }
                    dragged = null;
                }
            }
            if (items.Count > 0 && items.TrueForAll(x=>x.Done))
            {
                if (cleaning && phase < 2) { phase++; BuildPhase(); }
                else { gameObject.SetActive(false); finished?.Invoke(); }
            }
        }

        private void Complete(Item item,string message)
        {
            if (item.Done) return;
            item.Done = true; item.Rect.gameObject.SetActive(false); step?.Invoke(message);
        }

        private static readonly string[] InspectionLabels = { "床品", "台面", "卫浴", "门窗" };
        private string InspectionHint() => inspectionStep >= 4 ? "四项复检通过" :
            "检查单：" + string.Join(" → ", new[] { InspectionLabels[variant%4], InspectionLabels[(variant+1)%4], InspectionLabels[(variant+2)%4], InspectionLabels[(variant+3)%4] }) +
            " · 当前 " + InspectionLabels[(variant+inspectionStep)%4];

        private void Detail(RectTransform parent,Vector2 at,Vector2 size,Color color)
        {
            var go=new GameObject("行李细节",typeof(RectTransform),typeof(Image));
            var r=(RectTransform)go.transform;r.SetParent(parent,false);
            r.anchorMin=at-size*.5f;r.anchorMax=at+size*.5f;r.offsetMin=r.offsetMax=Vector2.zero;
            var image=go.GetComponent<Image>();image.sprite=MeixiUiSprites.Round;image.color=color;image.raycastTarget=false;
        }

        private RectTransform Rect(string name,Vector2 at,Vector2 size,Color color)
        {
            var go = new GameObject(name,typeof(RectTransform),typeof(Image));
            var rt = (RectTransform)go.transform; rt.SetParent(transform,false);
            rt.anchorMin = rt.anchorMax = at;
            rt.sizeDelta = new Vector2(surface.rect.width*size.x,surface.rect.height*size.y);
            var img = go.GetComponent<Image>(); img.color=color; img.raycastTarget=false;
            return rt;
        }

        private Text TextAt(string name,string value,Vector2 at,Vector2 size,int pixels,Transform parent=null)
        {
            var go = new GameObject(name,typeof(RectTransform),typeof(Text));
            var rt = (RectTransform)go.transform; rt.SetParent(parent ?? transform,false);
            rt.anchorMin=at-size*.5f; rt.anchorMax=at+size*.5f; rt.offsetMin=rt.offsetMax=Vector2.zero;
            var text=go.GetComponent<Text>(); text.font=font; text.text=value; text.fontSize=pixels;
            text.alignment=TextAnchor.MiddleCenter; text.color=new Color(.8f,.93f,.88f); text.raycastTarget=false;
            return text;
        }
    }
}
