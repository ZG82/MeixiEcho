using System.Collections.Generic;
using UnityEngine;

namespace MeixiEcho
{
    /// <summary>敌方蓄力期间围绕溪灵的红色禁近战音环。玩家在环内误按 J 会触发反击。</summary>
    public sealed class MeleeForbiddenRing3D : MonoBehaviour
    {
        private static readonly List<MeleeForbiddenRing3D> Active = new();
        private const float Radius = 3.15f;
        private Transform target;
        private LineRenderer[] rings;
        private Transform[] notes;
        private Vector3[] noteBaseScales;
        private float lifetime;
        private float age;
        private CombatBeatAction requiredAction;

        public static void Show(Transform enemy, CombatBeatAction action, float duration)
        {
            ClearAll(false);
            if (enemy == null || action == CombatBeatAction.AttackOne) return;
            var root = new GameObject("敌方近战禁区·立体红色音环");
            var effect = root.AddComponent<MeleeForbiddenRing3D>();
            effect.Build(enemy, action, duration);
        }

        public static bool Contains(Vector3 worldPosition)
        {
            for (int i = Active.Count - 1; i >= 0; i--)
            {
                MeleeForbiddenRing3D ring = Active[i];
                if (ring == null || ring.target == null)
                {
                    Active.RemoveAt(i);
                    continue;
                }
                Vector3 delta = worldPosition - ring.target.position;
                delta.y = 0f;
                if (delta.sqrMagnitude <= Radius * Radius) return true;
            }
            return false;
        }

        public static void ClearAll(bool countered)
        {
            for (int i = Active.Count - 1; i >= 0; i--)
            {
                MeleeForbiddenRing3D ring = Active[i];
                if (ring == null) continue;
                if (countered && ring.target != null)
                    EnemyMusicImpact3D.Create(ring.target.position + Vector3.up * 0.12f,
                        new Color(0.18f, 1f, 0.88f), 2.25f, 0.40f, 8);
                Destroy(ring.gameObject);
            }
            Active.Clear();
        }

        private void Build(Transform enemy, CombatBeatAction action, float duration)
        {
            target = enemy;
            requiredAction = action;
            lifetime = Mathf.Max(0.35f, duration);
            Active.Add(this);
            transform.position = GroundPosition(enemy.position);

            Color red = new(1f, 0.035f, 0.085f, 0.98f);
            Color accent = action == CombatBeatAction.AttackTwo
                ? new Color(0.92f, 0.12f, 1f, 0.94f)
                : new Color(1f, 0.48f, 0.06f, 0.94f);
            rings = new LineRenderer[4];
            for (int band = 0; band < rings.Length; band++)
            {
                Material material = EnemyMusicVfxFactory3D.CreateMaterial("禁近战音环材质_" + band,
                    band == 1 ? accent : Color.Lerp(red, Color.white, band * 0.10f), true);
                LineRenderer line = EnemyMusicVfxFactory3D.CreateCurve(transform, "十二瓣危险声纹_" + band,
                    material, 96, 0.080f + band * 0.016f, true);
                line.transform.localPosition = Vector3.up * (0.045f + band * 0.055f);
                line.startColor = band == 1 ? accent : red;
                line.endColor = Color.Lerp(red, Color.white, 0.35f);
                float bandRadius = Radius - band * 0.16f;
                for (int i = 0; i < line.positionCount; i++)
                {
                    float a = i / (float)line.positionCount * Mathf.PI * 2f;
                    float lobe = 1f + Mathf.Sin(a * (band % 2 == 0 ? 12f : 5f) + band) * (0.055f + band * 0.012f);
                    float y = Mathf.Sin(a * 8f + band) * 0.035f;
                    line.SetPosition(i, new Vector3(Mathf.Cos(a) * bandRadius * lobe, y, Mathf.Sin(a) * bandRadius * lobe));
                }
                rings[band] = line;
            }

            // 五线谱式径向警示弦，让禁区看起来像敌方正在演奏，而不是一个平面圆圈。
            for (int spoke = 0; spoke < 8; spoke++)
            {
                float a = spoke / 8f * Mathf.PI * 2f;
                Material spokeMaterial = EnemyMusicVfxFactory3D.CreateMaterial("禁区谱线_" + spoke,
                    new Color(red.r, red.g, red.b, 0.55f), true);
                LineRenderer line = EnemyMusicVfxFactory3D.CreateCurve(transform, "禁区放射谱线_" + spoke,
                    spokeMaterial, 18, 0.025f);
                for (int p = 0; p < line.positionCount; p++)
                {
                    float u = p / (line.positionCount - 1f);
                    float r = Mathf.Lerp(0.92f, Radius - 0.22f, u);
                    float sideways = Mathf.Sin(u * Mathf.PI * 5f) * 0.08f;
                    line.SetPosition(p, new Vector3(Mathf.Cos(a) * r + Mathf.Cos(a + Mathf.PI * 0.5f) * sideways,
                        0.07f + Mathf.Sin(u * Mathf.PI) * 0.10f,
                        Mathf.Sin(a) * r + Mathf.Sin(a + Mathf.PI * 0.5f) * sideways));
                }
            }

            notes = new Transform[7];
            noteBaseScales = new Vector3[notes.Length];
            for (int i = 0; i < notes.Length; i++)
            {
                float a = i / (float)notes.Length * Mathf.PI * 2f;
                Transform note = EnemyMusicVfxFactory3D.CreateNote(transform, "禁区反击音符_" + i,
                    i % 2 == 0 ? red : accent, Color.white, i == 3 ? 0.34f : 0.25f, i == 3);
                note.localPosition = new Vector3(Mathf.Cos(a) * (Radius - 0.42f), 0.28f + (i % 3) * 0.08f,
                    Mathf.Sin(a) * (Radius - 0.42f));
                note.localRotation = Quaternion.Euler(-18f, -a * Mathf.Rad2Deg + 90f, i * 13f);
                notes[i] = note;
                noteBaseScales[i] = note.localScale;
            }
        }

        private void Update()
        {
            if (target == null)
            {
                Destroy(gameObject);
                return;
            }
            age += Time.deltaTime;
            transform.position = GroundPosition(target.position);
            float t = Mathf.Clamp01(age / lifetime);
            float tempo = Mathf.Lerp(8f, 26f, t);
            float pulse = 1f + Mathf.Sin(Time.unscaledTime * tempo) * Mathf.Lerp(0.025f, 0.085f, t);
            transform.localScale = Vector3.one * pulse;
            transform.Rotate(0f, (requiredAction == CombatBeatAction.AttackTwo ? -18f : 13f) * Time.deltaTime, 0f, Space.Self);
            if (rings != null)
            {
                for (int i = 0; i < rings.Length; i++)
                    if (rings[i] != null) rings[i].widthMultiplier = (0.080f + i * 0.016f) * (1f + t * 0.75f);
            }
            if (notes != null)
            {
                for (int i = 0; i < notes.Length; i++)
                {
                    if (notes[i] == null) continue;
                    notes[i].Rotate(45f, (i % 2 == 0 ? 125f : -110f) * Time.deltaTime, 70f * Time.deltaTime, Space.Self);
                    notes[i].localScale = noteBaseScales[i] * (1f + Mathf.Sin(Time.unscaledTime * 12f + i) * 0.12f);
                }
            }
            if (age >= lifetime) Destroy(gameObject);
        }

        private void OnDestroy() => Active.Remove(this);

        private static Vector3 GroundPosition(Vector3 position) => new(position.x, 0.055f, position.z);
    }

    /// <summary>玩家 J/K/SPACE 的程序化三维声纹、音符与丝带特效。</summary>
    public sealed class PlayerCombatFlourish3D : MonoBehaviour
    {
        private readonly List<LineRenderer> lines = new();
        private readonly List<Color> lineColors = new();
        private readonly List<Transform> notes = new();
        private readonly List<Vector3> noteScales = new();
        private float lifetime;
        private float age;
        private float spin;

        public static void CreateBlink(Vector3 origin, Vector3 destination, bool safe)
        {
            Color primary = safe ? new Color(0.14f, 1f, 0.84f) : new Color(1f, 0.08f, 0.16f);
            var effect = CreateRoot("SPACE·瞬身三重残响", 0.72f, 110f);
            Vector3 travel = destination - origin;
            Vector3 flat = Vector3.ProjectOnPlane(travel, Vector3.up);
            Vector3 right = flat.sqrMagnitude > 0.01f ? Vector3.Cross(Vector3.up, flat.normalized) : Vector3.right;
            for (int ribbon = 0; ribbon < 3; ribbon++)
            {
                LineRenderer line = effect.AddWorldLine("瞬身弦光_" + ribbon, ribbon == 1 ? Color.white : primary,
                    36, 0.12f - ribbon * 0.022f);
                for (int i = 0; i < line.positionCount; i++)
                {
                    float t = i / (line.positionCount - 1f);
                    float envelope = Mathf.Sin(t * Mathf.PI);
                    float side = Mathf.Sin(t * Mathf.PI * 3f + ribbon * Mathf.PI * 0.66f) * envelope * (0.24f + ribbon * 0.07f);
                    line.SetPosition(i, Vector3.Lerp(origin, destination, t) + Vector3.up * (0.28f + envelope * (0.72f + ribbon * 0.11f)) + right * side);
                }
            }
            for (int i = 0; i < 5; i++)
            {
                float t = (i + 0.5f) / 5.5f;
                effect.AddNote(Vector3.Lerp(origin, destination, t) + Vector3.up * (0.42f + Mathf.Sin(t * Mathf.PI) * 0.65f),
                    i % 2 == 0 ? primary : Color.white, 0.16f + i * 0.012f, false);
            }
            EnemyMusicImpact3D.Create(origin + Vector3.up * 0.08f, primary, 1.55f, 0.34f, 6);
            EnemyMusicImpact3D.Create(destination + Vector3.up * 0.08f, primary, safe ? 2.15f : 1.55f, 0.42f, safe ? 10 : 6);
        }

        public static void CreateMelee(Vector3 origin, Vector3 impact, Vector3 forward, bool onBeat)
        {
            Color primary = onBeat ? new Color(0.88f, 1f, 1f) : new Color(0.10f, 0.90f, 1f);
            var effect = CreateRoot("J·近战突进四重弦刃", onBeat ? 0.78f : 0.62f, 145f);
            Vector3 flatForward = Vector3.ProjectOnPlane(forward, Vector3.up).normalized;
            if (flatForward.sqrMagnitude < 0.01f) flatForward = Vector3.forward;
            Vector3 right = Vector3.Cross(Vector3.up, flatForward);
            for (int slash = 0; slash < 4; slash++)
            {
                Color color = slash == 2 ? new Color(1f, 0.68f, 0.18f) : primary;
                LineRenderer line = effect.AddWorldLine("近战弦刃_" + slash, color, 30, 0.14f - slash * 0.015f);
                float radius = 1.20f + slash * 0.23f;
                for (int i = 0; i < line.positionCount; i++)
                {
                    float t = i / (line.positionCount - 1f);
                    float angle = Mathf.Lerp(-1.20f, 1.12f, t) + slash * 0.14f;
                    Vector3 arc = right * Mathf.Sin(angle) * radius + Vector3.up * (Mathf.Cos(angle) * radius * 0.72f);
                    line.SetPosition(i, impact + flatForward * (0.18f + slash * 0.05f) + arc);
                }
            }
            LineRenderer lunge = effect.AddWorldLine("突进残响谱线", primary, 26, 0.085f);
            for (int i = 0; i < lunge.positionCount; i++)
            {
                float t = i / (lunge.positionCount - 1f);
                lunge.SetPosition(i, Vector3.Lerp(origin, impact, t) + Vector3.up * Mathf.Sin(t * Mathf.PI) * 0.45f +
                    right * Mathf.Sin(t * Mathf.PI * 5f) * 0.09f);
            }
            for (int i = 0; i < (onBeat ? 7 : 4); i++)
            {
                float angle = i / (float)(onBeat ? 7 : 4) * Mathf.PI * 2f;
                effect.AddNote(impact + right * Mathf.Cos(angle) * 1.25f + Vector3.up * (0.25f + Mathf.Sin(angle) * 0.90f),
                    i % 3 == 0 ? new Color(1f, 0.68f, 0.18f) : primary, 0.18f, i == 3);
            }
        }

        public static void CreateRangedCast(Vector3 origin, Vector3 direction, bool boosted)
        {
            Color primary = boosted ? Color.white : new Color(0.12f, 0.92f, 1f);
            var effect = CreateRoot("K·远程音符波起奏", 0.55f, -170f);
            Vector3 forward = direction.sqrMagnitude > 0.01f ? direction.normalized : Vector3.forward;
            Vector3 right = Vector3.Cross(Vector3.up, forward);
            for (int band = 0; band < 3; band++)
            {
                LineRenderer line = effect.AddWorldLine("远攻螺旋谱带_" + band,
                    band == 1 ? new Color(1f, 0.62f, 0.18f) : primary, 32, 0.10f - band * 0.016f);
                for (int i = 0; i < line.positionCount; i++)
                {
                    float t = i / (line.positionCount - 1f);
                    float angle = t * Mathf.PI * 4f + band * Mathf.PI * 0.66f;
                    float radius = (1f - t) * (0.56f + band * 0.08f);
                    line.SetPosition(i, origin + forward * t * 2.1f + right * Mathf.Cos(angle) * radius + Vector3.up * Mathf.Sin(angle) * radius);
                }
            }
            effect.AddNote(origin + forward * 0.78f, primary, boosted ? 0.42f : 0.32f, true);
        }

        public static void CreateRangedCounter(Vector3 origin, Vector3 target)
        {
            var effect = CreateRoot("K·破谱聚焦弦光", 0.68f, 190f);
            Vector3 travel = target - origin;
            Vector3 right = Vector3.Cross(Vector3.up, travel.normalized);
            for (int band = 0; band < 5; band++)
            {
                Color color = band == 2 ? Color.white : band % 2 == 0 ? new Color(0.14f, 1f, 0.90f) : new Color(1f, 0.62f, 0.16f);
                LineRenderer line = effect.AddWorldLine("破谱聚焦线_" + band, color, 38, band == 2 ? 0.13f : 0.065f);
                for (int i = 0; i < line.positionCount; i++)
                {
                    float t = i / (line.positionCount - 1f);
                    float envelope = Mathf.Sin(t * Mathf.PI);
                    float phase = t * Mathf.PI * 6f + band * Mathf.PI * 0.4f;
                    line.SetPosition(i, Vector3.Lerp(origin, target, t) + right * Mathf.Cos(phase) * envelope * 0.24f +
                        Vector3.up * Mathf.Sin(phase) * envelope * 0.18f);
                }
            }
            EnemyMusicImpact3D.Create(target, new Color(0.18f, 1f, 0.90f), 3.6f, 0.62f, 20);
        }

        public static void CreateForbiddenCounter(Vector3 playerPosition, Vector3 enemyPosition)
        {
            var effect = CreateRoot("禁区误攻·猩红反击", 0.78f, 220f);
            Vector3 travel = playerPosition - enemyPosition;
            Vector3 right = Vector3.Cross(Vector3.up, travel.normalized);
            for (int band = 0; band < 4; band++)
            {
                Color color = band == 1 ? Color.white : new Color(1f, 0.02f + band * 0.04f, 0.10f);
                LineRenderer line = effect.AddWorldLine("反击锯齿和弦_" + band, color, 30, 0.14f - band * 0.018f);
                for (int i = 0; i < line.positionCount; i++)
                {
                    float t = i / (line.positionCount - 1f);
                    float spike = (i % 2 == 0 ? 1f : -1f) * Mathf.Sin(t * Mathf.PI) * (0.18f + band * 0.05f);
                    line.SetPosition(i, Vector3.Lerp(enemyPosition, playerPosition, t) + right * spike + Vector3.up * Mathf.Sin(t * Mathf.PI) * 0.45f);
                }
            }
            EnemyMusicImpact3D.Create(playerPosition, new Color(1f, 0.03f, 0.12f), 2.8f, 0.62f, 18);
        }

        public static void CreateRangedImpact(Vector3 position, bool boosted)
        {
            var effect = CreateRoot("K·音符波命中展开", boosted ? 0.82f : 0.62f, 180f);
            int count = boosted ? 10 : 6;
            for (int i = 0; i < count; i++)
            {
                float a = i / (float)count * Mathf.PI * 2f;
                Color color = i % 3 == 0 ? new Color(1f, 0.64f, 0.16f) : new Color(0.12f, 0.96f, 1f);
                effect.AddNote(position + new Vector3(Mathf.Cos(a) * 1.2f, Mathf.Sin(a * 2f) * 0.72f, Mathf.Sin(a) * 1.2f),
                    color, boosted ? 0.23f : 0.18f, i == count / 2);
            }
        }

        public static void CreateFinisherCharge(Vector3 playerPosition, Vector3 targetPosition, CombatBeatAction action)
        {
            Color primary = action == CombatBeatAction.AttackTwo
                ? new Color(0.16f, 1f, 0.94f)
                : new Color(1f, 0.68f, 0.18f);
            var effect = CreateRoot("终结演出·魂音汇流", 0.32f, 210f);
            Vector3 axis = targetPosition - playerPosition;
            Vector3 forward = axis.sqrMagnitude > 0.01f ? axis.normalized : Vector3.forward;
            Vector3 right = Vector3.Cross(Vector3.up, forward);
            for (int band = 0; band < 3; band++)
            {
                Color color = band == 2 ? Color.white : band % 2 == 0 ? primary : new Color(0.18f, 0.88f, 1f);
                LineRenderer line = effect.AddWorldLine("终响汇流谱带_" + band, color, 44, band == 2 ? 0.10f : 0.055f);
                for (int i = 0; i < line.positionCount; i++)
                {
                    float t = i / (line.positionCount - 1f);
                    float reverse = 1f - t;
                    float angle = reverse * Mathf.PI * 6f + band * Mathf.PI * 0.4f;
                    float radius = reverse * (0.50f + band * 0.045f);
                    Vector3 center = Vector3.Lerp(playerPosition + Vector3.up * 0.96f,
                        playerPosition + forward * 1.25f + Vector3.up * 1.05f, t);
                    line.SetPosition(i, center + right * Mathf.Cos(angle) * radius + Vector3.up * Mathf.Sin(angle) * radius * 0.62f);
                }
            }
            for (int i = 0; i < 6; i++)
            {
                float a = i / 6f * Mathf.PI * 2f;
                effect.AddNote(playerPosition + Vector3.up * (0.72f + Mathf.Sin(a * 2f) * 0.34f) +
                    right * Mathf.Cos(a) * 0.68f + forward * Mathf.Sin(a) * 0.42f,
                    i % 3 == 0 ? Color.white : primary, 0.12f + (i % 2) * 0.018f, i == 3);
            }
        }

        public static void CreateFinisherImpact(Vector3 playerPosition, Vector3 targetPosition, CombatBeatAction action)
        {
            Color primary = action == CombatBeatAction.AttackTwo
                ? new Color(0.12f, 1f, 0.92f)
                : new Color(1f, 0.62f, 0.12f);
            var effect = CreateRoot("终结演出·梅溪终响爆发", 1.05f, 260f);
            Vector3 axis = targetPosition - playerPosition;
            Vector3 forward = axis.sqrMagnitude > 0.01f ? axis.normalized : Vector3.forward;
            Vector3 right = Vector3.Cross(Vector3.up, forward);
            for (int slash = 0; slash < 3; slash++)
            {
                Color color = slash == 1 ? Color.white : slash % 2 == 0 ? primary : new Color(0.18f, 0.90f, 1f);
                LineRenderer line = effect.AddWorldLine("终响立体弦刃_" + slash, color, 36, slash == 1 ? 0.10f : 0.065f);
                float radius = 0.72f + slash * 0.14f;
                for (int i = 0; i < line.positionCount; i++)
                {
                    float t = i / (line.positionCount - 1f);
                    float angle = Mathf.Lerp(-1.30f, 1.28f, t) + slash * 0.19f;
                    line.SetPosition(i, targetPosition + Vector3.up * 0.92f + forward * (slash - 1) * 0.07f +
                        right * Mathf.Sin(angle) * radius + Vector3.up * Mathf.Cos(angle) * radius * 0.72f);
                }
            }
            for (int i = 0; i < 4; i++)
            {
                float a = i / 4f * Mathf.PI * 2f;
                effect.AddNote(targetPosition + Vector3.up * (0.82f + Mathf.Sin(a * 3f) * 0.64f) +
                    right * Mathf.Cos(a) * 1.05f + forward * Mathf.Sin(a) * 0.82f,
                    i % 3 == 0 ? Color.white : primary, 0.10f + (i % 2) * 0.012f, i == 2);
            }
            EnemyMusicImpact3D.Create(targetPosition + Vector3.up * 0.16f, primary, 2.45f, 0.68f, 4);
        }

        private static PlayerCombatFlourish3D CreateRoot(string name, float duration, float rotationSpeed)
        {
            var root = new GameObject(name);
            var effect = root.AddComponent<PlayerCombatFlourish3D>();
            effect.lifetime = duration;
            effect.spin = rotationSpeed;
            return effect;
        }

        private LineRenderer AddWorldLine(string name, Color color, int pointCount, float width)
        {
            Material material = EnemyMusicVfxFactory3D.CreateMaterial(name + "_材质", new Color(color.r, color.g, color.b, 0.94f), true);
            LineRenderer line = EnemyMusicVfxFactory3D.CreateCurve(transform, name, material, pointCount, width);
            line.useWorldSpace = true;
            line.startColor = color;
            line.endColor = new Color(color.r, color.g, color.b, 0.18f);
            lines.Add(line);
            lineColors.Add(color);
            return line;
        }

        private void AddNote(Vector3 position, Color color, float scale, bool doubleNote)
        {
            Transform note = EnemyMusicVfxFactory3D.CreateNote(transform, "玩家立体声纹音符", color, Color.white, scale, doubleNote);
            note.position = position;
            note.rotation = Quaternion.Euler(Random.Range(-30f, 30f), Random.Range(0f, 360f), Random.Range(-35f, 35f));
            notes.Add(note);
            noteScales.Add(note.localScale);
        }

        private void Update()
        {
            age += Time.deltaTime;
            float t = Mathf.Clamp01(age / Mathf.Max(0.05f, lifetime));
            float alpha = 1f - Mathf.SmoothStep(0.48f, 1f, t);
            for (int i = 0; i < lines.Count; i++)
            {
                if (lines[i] == null) continue;
                Color color = lineColors[i];
                lines[i].startColor = new Color(color.r, color.g, color.b, alpha);
                lines[i].endColor = new Color(color.r, color.g, color.b, alpha * 0.12f);
                lines[i].widthMultiplier *= 1f + Time.deltaTime * 0.55f;
            }
            for (int i = 0; i < notes.Count; i++)
            {
                if (notes[i] == null) continue;
                notes[i].Rotate(spin * Time.deltaTime, (spin + i * 17f) * Time.deltaTime, -spin * 0.55f * Time.deltaTime, Space.Self);
                notes[i].localScale = noteScales[i] * Mathf.Lerp(1f, 0.15f, t);
            }
            if (age >= lifetime) Destroy(gameObject);
        }
    }

    /// <summary>终结一击的电影黑边与命中闪白；使用不受 Time.timeScale 影响的时间。</summary>
    public sealed class FinisherCinematicOverlay3D : MonoBehaviour
    {
        private static FinisherCinematicOverlay3D active;
        private float startedAt;
        private float duration;
        private float impactAt = -10f;
        private Color impactColor = Color.white;

        public static void Begin(float lifetime)
        {
            if (active != null) Destroy(active.gameObject);
            var root = new GameObject("终结演出·电影黑边与闪光");
            active = root.AddComponent<FinisherCinematicOverlay3D>();
            active.startedAt = Time.unscaledTime;
            active.duration = Mathf.Max(0.6f, lifetime);
        }

        public static void Impact(Color color)
        {
            if (active == null) Begin(1.1f);
            active.impactAt = Time.unscaledTime;
            active.impactColor = color;
        }

        private void OnGUI()
        {
            float age = Time.unscaledTime - startedAt;
            float enter = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(age / 0.16f));
            float exit = 1f - Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((age - duration + 0.24f) / 0.24f));
            float bars = Mathf.Min(enter, exit);
            float barHeight = Screen.height * 0.105f * bars;
            Color previous = GUI.color;
            GUI.depth = -1000;
            GUI.color = new Color(0.005f, 0.008f, 0.015f, 0.98f * bars);
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, barHeight), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(0f, Screen.height - barHeight, Screen.width, barHeight), Texture2D.whiteTexture);

            float flashAge = Time.unscaledTime - impactAt;
            if (flashAge >= 0f && flashAge <= 0.22f)
            {
                float flash = (1f - flashAge / 0.22f);
                flash *= flash;
                GUI.color = new Color(impactColor.r, impactColor.g, impactColor.b, flash * 0.72f);
                GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
            }
            GUI.color = previous;
        }

        private void Update()
        {
            if (Time.unscaledTime - startedAt >= duration)
            {
                if (active == this) active = null;
                Destroy(gameObject);
            }
        }

        private void OnDestroy()
        {
            if (active == this) active = null;
        }
    }
}
