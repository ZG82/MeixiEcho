using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.Video;

namespace MeixiEcho
{
    public sealed class ChapterThreeEndingFilm : MonoBehaviour
    {
        private readonly VideoPlayer[] players=new VideoPlayer[2];
        private readonly AudioSource[] sources=new AudioSource[2];
        private readonly RenderTexture[] textures=new RenderTexture[2];
        private readonly RawImage[] pictures=new RawImage[2];
        private readonly bool[] failed=new bool[2];
        private GameObject panel;
        private CanvasGroup group;
        private Text caption,hint;
        private bool skip,paused;
        private float skipHold;
        private int playingSlot=-1;
        public bool IsRunning {get;private set;}
        public int ShotsPlayed {get;private set;}
        public static int ResolveBranch(string summary)
        {
            if(!string.IsNullOrEmpty(summary) && summary.Contains("真正的大结局"))return 7;
            if(!string.IsNullOrEmpty(summary) && summary.Contains("你提交了人员造假"))return 5;
            return 6;
        }
        public static int[] ShotOrder(string summary)=>new[]{1,2,3,ResolveBranch(summary),4,0};

        public IEnumerator Play(Canvas canvas,string summary,Action finished)
        {
            if(IsRunning)yield break;
            IsRunning=true;skip=false;paused=false;skipHold=0;ShotsPlayed=0;Build(canvas);MeixiCursorState.SetPointer();
            int[] order=ShotOrder(summary);
            Configure(0,order[0]);
            for(int i=0;i<order.Length && !skip;i++)
            {
                int active=i%2,old=1-active;var p=players[active];
                caption.text=$"终 章 · 余 音 归 宿    {i+1:00} / {order.Length:00}";
                float waited=0;
                while(!p.isPrepared && !failed[active] && waited<12 && !skip)
                {if(!paused)waited+=Time.unscaledDeltaTime;yield return null;}
                if(skip)break;
                if(failed[active] || !p.isPrepared)
                {
                    Debug.LogWarning($"[EndingFilm] Cannot prepare shot {order[i]:00}.");
                    if(i+1<order.Length)Configure(old,order[i+1]);
                    continue;
                }
                playingSlot=active;p.Play();waited=0;
                while(p.frame<1 && waited<6 && !failed[active] && !skip)
                {if(!paused)waited+=Time.unscaledDeltaTime;yield return null;}
                if(skip)break;
                if(failed[active] || p.frame<1)
                {if(i+1<order.Length)Configure(old,order[i+1]);continue;}
                pictures[active].transform.SetAsLastSibling();
                float fade=0;
                while(fade<.4f && !skip)
                {
                    if(paused){yield return null;continue;}
                    fade+=Time.unscaledDeltaTime;float t=Mathf.Clamp01(fade/.4f);
                    group.alpha=Mathf.Max(group.alpha,t);pictures[active].color=new Color(1,1,1,t);
                    sources[active].volume=.8f*t;sources[old].volume=.8f*(1-t);yield return null;
                }
                pictures[active].color=Color.white;players[old].Stop();pictures[old].color=Color.clear;ShotsPlayed++;
                if(i+1<order.Length)Configure(old,order[i+1]);
                double last=p.time;float stalled=0;
                while(!skip && !failed[active])
                {
                    if(paused){yield return null;continue;}
                    if(p.length>0 && p.time>=p.length-.09)break;
                    if(p.frame>0 && !p.isPlaying)break;
                    stalled=Math.Abs(p.time-last)<.001?stalled+Time.unscaledDeltaTime:0;last=p.time;
                    if(stalled>8){Debug.LogWarning("[EndingFilm] Decode stalled, continuing safely.");break;}
                    yield return null;
                }
                p.Pause();Debug.Log($"[EndingFilm] Shot {order[i]:00} finished.");
            }
            IsRunning=false;playingSlot=-1;
            foreach(var p in players)if(p!=null)p.Pause();
            finished?.Invoke();
            float end=0,startAlpha=group.alpha;
            while(end<.55f){end+=Time.unscaledDeltaTime;group.alpha=startAlpha*(1-Mathf.Clamp01(end/.55f));yield return null;}
            Cleanup();Debug.Log($"[EndingFilm] COMPLETE shots={ShotsPlayed} skipped={skip}");
        }

        private void Update()
        {
            if(!IsRunning || Keyboard.current==null)return;
            var k=Keyboard.current;
            if(k.spaceKey.wasPressedThisFrame && playingSlot>=0)
                SetPlaybackPaused(!paused);
            skipHold=k.escapeKey.isPressed?skipHold+Time.unscaledDeltaTime:0;
            if(skipHold>=1.2f)RequestSkip();
            hint.text=skipHold>.1f?$"按住 ESC 跳过  {Mathf.Clamp01(skipHold/1.2f)*100:0}%":
                paused?"SPACE 继续播放    ·    按住 ESC 跳过":"SPACE 暂停影像    ·    按住 ESC 跳过";
        }
        public void SetPlaybackPaused(bool value)
        {
            if(!IsRunning || playingSlot<0)return;
            paused=value;if(paused)players[playingSlot].Pause();else players[playingSlot].Play();
        }
        public void RequestSkip(){if(IsRunning)skip=true;}
        private void Configure(int index,int shot)
        {
            var p=players[index];p.Stop();failed[index]=false;
            string path=Path.Combine(Application.streamingAssetsPath,"ChapterThreeEnding",$"Shot{shot:00}.mp4");
            if(!File.Exists(path)){failed[index]=true;return;}
            p.url=new Uri(path).AbsoluteUri;p.Prepare();
        }
        private void Build(Canvas canvas)
        {
            panel=Rect("第三章结局影像",canvas.transform,Vector2.zero,Vector2.one);
            panel.AddComponent<Image>().color=new Color(.005f,.013f,.016f,1);
            var layer=panel.AddComponent<Canvas>();layer.overrideSorting=true;layer.sortingOrder=1200;panel.AddComponent<GraphicRaycaster>();
            group=panel.AddComponent<CanvasGroup>();group.alpha=0;
            var area=Rect("影像区域",panel.transform,new Vector2(.025f,.115f),new Vector2(.975f,.94f));
            var frame=Rect("原始比例画幅",area.transform,Vector2.zero,Vector2.one);
            var aspect=frame.AddComponent<AspectRatioFitter>();aspect.aspectMode=AspectRatioFitter.AspectMode.FitInParent;aspect.aspectRatio=16f/9f;
            var stack=Rect("视频叠化层",frame.transform,Vector2.zero,Vector2.one);
            for(int i=0;i<2;i++)
            {
                int index=i;
                pictures[i]=Rect("结局视频_"+i,stack.transform,Vector2.zero,Vector2.one).AddComponent<RawImage>();pictures[i].color=Color.clear;pictures[i].raycastTarget=false;
                textures[i]=new RenderTexture(1280,720,0,RenderTextureFormat.ARGB32);textures[i].Create();pictures[i].texture=textures[i];
                var host=new GameObject("结局播放器_"+i);host.transform.SetParent(panel.transform,false);
                var p=players[i]=host.AddComponent<VideoPlayer>();var a=sources[i]=host.AddComponent<AudioSource>();a.playOnAwake=false;a.spatialBlend=0;a.volume=0;
                p.playOnAwake=false;p.source=VideoSource.Url;p.renderMode=VideoRenderMode.RenderTexture;p.targetTexture=textures[i];
                p.audioOutputMode=VideoAudioOutputMode.AudioSource;p.controlledAudioTrackCount=1;p.EnableAudioTrack(0,true);p.SetTargetAudioSource(0,a);
                p.isLooping=false;p.waitForFirstFrame=true;p.skipOnDrop=true;
                p.errorReceived+=(source,message)=>{failed[index]=true;Debug.LogWarning("[EndingFilm] "+message);};
            }
            // Matte coverage is deterministic; artwork feathers its ragged inner edge.
            var ink=Rect("水墨边角",frame.transform,Vector2.zero,Vector2.one);ink.AddComponent<ChapterThreeInkCorners>().raycastTarget=false;
            var art=Resources.Load<Texture2D>("ChapterThree/EndingInkFrame");
            if(art!=null){var image=Rect("水墨笔触",ink.transform,Vector2.zero,Vector2.one).AddComponent<RawImage>();image.texture=art;image.raycastTarget=false;}
            var font=Font.CreateDynamicFontFromOSFont(new[]{"Microsoft YaHei","SimHei"},28);
            caption=Label("结局镜头",panel.transform,new Vector2(.10f,.065f),new Vector2(.90f,.105f),font,22);
            hint=Label("影像操作",panel.transform,new Vector2(.10f,.015f),new Vector2(.90f,.055f),font,16);hint.text="正在展开书页中的影像……";
        }
        private static GameObject Rect(string name,Transform parent,Vector2 min,Vector2 max)
        {
            var go=new GameObject(name,typeof(RectTransform));var r=(RectTransform)go.transform;r.SetParent(parent,false);
            r.anchorMin=min;r.anchorMax=max;r.offsetMin=r.offsetMax=Vector2.zero;return go;
        }
        private static Text Label(string name,Transform parent,Vector2 min,Vector2 max,Font font,int size)
        {
            var text=Rect(name,parent,min,max).AddComponent<Text>();text.font=font;text.fontSize=size;text.alignment=TextAnchor.MiddleCenter;
            text.color=new Color(.86f,.82f,.66f);text.raycastTarget=false;return text;
        }
        private void Cleanup()
        {
            foreach(var p in players)if(p!=null)p.Stop();
            for(int i=0;i<textures.Length;i++)if(textures[i]!=null){textures[i].Release();Destroy(textures[i]);textures[i]=null;}
            if(panel!=null){Destroy(panel);panel=null;}
        }
        private void OnDestroy(){Cleanup();}
    }
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class ChapterThreeInkCorners : MaskableGraphic
    {
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();var r=rectTransform.rect;
            Vector2[] edge={new(0,0),new(.205f,0),new(.200f,.025f),new(.212f,.035f),new(.194f,.052f),new(.203f,.065f),new(.181f,.080f),new(.173f,.123f),new(.142f,.143f),new(.118f,.135f),new(.10f,.157f),new(.065f,.147f),new(0,.173f)};
            for(int side=0;side<2;side++)
            {
                int offset=vh.currentVertCount;
                foreach(var p in edge){Vector2 v=side==0?new Vector2(p.x,1-p.y):new Vector2(1-p.x,p.y);vh.AddVert(new Vector3(r.x+v.x*r.width,r.y+v.y*r.height),new Color(.008f,.018f,.021f,1),Vector2.zero);}
                for(int j=1;j<edge.Length-1;j++)vh.AddTriangle(offset,offset+j,offset+j+1);
            }
        }
    }
}
