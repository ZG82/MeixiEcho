using UnityEngine;

namespace MeixiEcho
{
    /// <summary>Shared, build-safe UI masks; no dependency on Editor-only skin resources.</summary>
    public static class MeixiUiSprites
    {
        private static Sprite white, round, stain;
        public static Sprite White => white != null ? white : white=Sprite.Create(Texture2D.whiteTexture,new Rect(0,0,2,2),Vector2.one*.5f);
        public static Sprite Round => round != null ? round : round=Make(false);
        public static Sprite Stain => stain != null ? stain : stain=Make(true);
        private static Sprite Make(bool organic)
        {
            const int n=96;
            var texture=new Texture2D(n,n,TextureFormat.RGBA32,false){name=organic?"SoftStainMask":"RoundedUiMask",wrapMode=TextureWrapMode.Clamp};
            var colors=new Color[n*n];
            for(int y=0;y<n;y++)for(int x=0;x<n;x++)
            {
                float u=(x+.5f)/n*2-1,v=(y+.5f)/n*2-1;
                float angle=Mathf.Atan2(v,u);
                float radius=organic ? .75f+.10f*Mathf.Sin(angle*5)+.06f*Mathf.Cos(angle*9) : .95f;
                float distance=organic ? Mathf.Sqrt(u*u+v*v) : Mathf.Pow(Mathf.Pow(Mathf.Abs(u),6)+Mathf.Pow(Mathf.Abs(v),6),1f/6);
                float a=Mathf.Clamp01((radius-distance)/(organic?.20f:.035f));
                if(organic)a*=.65f+.35f*Mathf.PerlinNoise(x*.19f,y*.19f);
                colors[y*n+x]=new Color(1,1,1,a);
            }
            texture.SetPixels(colors);texture.Apply(false,true);
            return Sprite.Create(texture,new Rect(0,0,n,n),Vector2.one*.5f,100);
        }
    }
}
