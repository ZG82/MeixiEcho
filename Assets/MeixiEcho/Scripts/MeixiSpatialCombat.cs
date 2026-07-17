using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace MeixiEcho
{
    /// <summary>贴地追踪玩家的红色攻击区：横扫/音符使用圆形，突刺使用矩形走廊。</summary>
    public sealed class TrackingAttackTelegraph3D : MonoBehaviour
    {
        private static readonly List<TrackingAttackTelegraph3D> ActiveTelegraphs = new();
        private Transform target;
        private Transform source;
        private Transform fill;
        private LineRenderer border;
        private Material fillMaterial;
        private float duration;
        private float age;
        private int attackType;
        private Vector3 trackedPosition;
        private Vector3 baseFillScale;
        private bool locked;
        private Transform[] ornaments;
        private Vector3[] ornamentScales;
        private Color primaryColor;
        private Color secondaryColor;

        public static void Create(Transform player, Transform enemy, int type, float lifetime)
        {
            if (player == null) return;
            string attackName = type == 0 ? "高抛坠音十二瓣锁定" : type == 1 ? "低音冲击波声纹走廊" : "追踪和弦五芒锁定";
            var root = new GameObject(attackName);
            var effect = root.AddComponent<TrackingAttackTelegraph3D>();
            effect.Build(player, enemy, type, lifetime);
        }

        public static void LockAllAt(Vector3 worldPosition)
        {
            for (int i = ActiveTelegraphs.Count - 1; i >= 0; i--)
            {
                TrackingAttackTelegraph3D telegraph = ActiveTelegraphs[i];
                if (telegraph == null)
                {
                    ActiveTelegraphs.RemoveAt(i);
                    continue;
                }
                worldPosition.y = 0.055f;
                telegraph.trackedPosition = worldPosition;
                telegraph.locked = true;
            }
        }

        public static void ClearAll()
        {
            for (int i = ActiveTelegraphs.Count - 1; i >= 0; i--)
            {
                TrackingAttackTelegraph3D telegraph = ActiveTelegraphs[i];
                if (telegraph != null) Destroy(telegraph.gameObject);
            }
            ActiveTelegraphs.Clear();
        }

        private void Build(Transform player, Transform enemy, int type, float lifetime)
        {
            target = player;
            source = enemy;
            attackType = type;
            duration = Mathf.Max(0.32f, lifetime);
            trackedPosition = player.position;
            ActiveTelegraphs.Add(this);

            primaryColor = attackType switch
            {
                0 => new Color(1f, 0.05f, 0.14f, 0.98f),
                1 => new Color(0.05f, 0.82f, 1f, 0.98f),
                _ => new Color(0.82f, 0.10f, 1f, 0.98f)
            };
            secondaryColor = attackType switch
            {
                0 => new Color(1f, 0.52f, 0.08f, 0.92f),
                1 => new Color(0.42f, 0.30f, 1f, 0.92f),
                _ => new Color(1f, 0.38f, 0.72f, 0.92f)
            };

            Texture2D texture = Resources.Load<Texture2D>("Textures/Generated/MeixiBeatSigil");
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default");
            fillMaterial = CreateTransparentMaterial(shader, texture,
                new Color(primaryColor.r, primaryColor.g, primaryColor.b, 0.30f));

            var fillObject = GameObject.CreatePrimitive(PrimitiveType.Quad);
            fillObject.name = "追踪危险区填充";
            fillObject.transform.SetParent(transform, false);
            fillObject.transform.localPosition = Vector3.up * 0.018f;
            fillObject.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            Destroy(fillObject.GetComponent<Collider>());
            fillObject.GetComponent<Renderer>().material = fillMaterial;
            fill = fillObject.transform;

            border = gameObject.AddComponent<LineRenderer>();
            border.useWorldSpace = false;
            border.loop = true;
            border.widthMultiplier = 0.085f;
            border.material = CreateTransparentMaterial(shader, null, primaryColor);
            border.startColor = primaryColor;
            border.endColor = secondaryColor;
            border.numCornerVertices = 4;
            border.numCapVertices = 4;

            if (attackType == 1)
            {
                fill.localScale = new Vector3(2.15f, 6.8f, 1f);
                border.positionCount = 52;
                int half = border.positionCount / 2;
                for (int i = 0; i < half; i++)
                {
                    float u = i / (half - 1f);
                    float z = Mathf.Lerp(-3.4f, 3.4f, u);
                    float wave = Mathf.Sin(u * Mathf.PI * 6f) * 0.14f;
                    border.SetPosition(i, new Vector3(-1.06f + wave, 0.03f + Mathf.Abs(wave) * 0.12f, z));
                    border.SetPosition(half + i, new Vector3(1.06f - wave, 0.03f + Mathf.Abs(wave) * 0.12f, -z));
                }
            }
            else
            {
                float radius = attackType == 2 ? 1.65f : 2.35f;
                fill.localScale = Vector3.one * radius * 2f;
                border.positionCount = attackType == 2 ? 60 : 72;
                for (int i = 0; i < border.positionCount; i++)
                {
                    float angle = i / (float)border.positionCount * Mathf.PI * 2f;
                    float lobes = attackType == 2 ? 5f : 12f;
                    float shapedRadius = radius * (1f + Mathf.Sin(angle * lobes) * (attackType == 2 ? 0.12f : 0.075f));
                    float verticalWave = Mathf.Sin(angle * (lobes + 3f)) * 0.035f;
                    border.SetPosition(i, new Vector3(Mathf.Cos(angle) * shapedRadius, 0.03f + verticalWave,
                        Mathf.Sin(angle) * shapedRadius));
                }
            }
            BuildOrnaments();
            baseFillScale = fill.localScale;
            UpdateTransform(true);
        }

        private void OnDestroy()
        {
            ActiveTelegraphs.Remove(this);
        }

        private void Update()
        {
            age += Time.deltaTime;
            float t = Mathf.Clamp01(age / duration);
            if (!locked && t >= 0.82f) locked = true;
            UpdateTransform(false);

            float pulse = 1f + Mathf.Sin(Time.unscaledTime * Mathf.Lerp(13f, 34f, t)) * Mathf.Lerp(0.035f, 0.10f, t);
            if (fill != null) fill.localScale = baseFillScale * pulse;
            transform.localScale = Vector3.one * Mathf.Lerp(1.08f, 0.94f, t);
            border.widthMultiplier = Mathf.Lerp(0.07f, 0.18f, t);
            Color borderColor = Color.Lerp(new Color(primaryColor.r, primaryColor.g, primaryColor.b, 0.66f),
                new Color(primaryColor.r, primaryColor.g, primaryColor.b, 1f), t);
            border.startColor = borderColor;
            border.endColor = new Color(secondaryColor.r, secondaryColor.g, secondaryColor.b, borderColor.a);
            if (fillMaterial != null)
            {
                Color tint = new Color(primaryColor.r, primaryColor.g, primaryColor.b, Mathf.Lerp(0.22f, 0.58f, t));
                fillMaterial.color = tint;
                if (fillMaterial.HasProperty("_BaseColor")) fillMaterial.SetColor("_BaseColor", tint);
            }
            if (ornaments != null)
            {
                for (int i = 0; i < ornaments.Length; i++)
                {
                    if (ornaments[i] == null) continue;
                    ornaments[i].Rotate(0f, (i % 2 == 0 ? 120f : -95f) * Time.deltaTime, 0f, Space.Self);
                    float ornamentPulse = 1f + Mathf.Sin(Time.unscaledTime * 12f + i) * 0.10f;
                    ornaments[i].localScale = ornamentScales[i] * ornamentPulse;
                }
            }
            if (age >= duration) Destroy(gameObject);
        }

        private void BuildOrnaments()
        {
            int count = attackType == 1 ? 4 : attackType == 2 ? 5 : 6;
            ornaments = new Transform[count];
            ornamentScales = new Vector3[count];
            for (int i = 0; i < count; i++)
            {
                Transform note = EnemyMusicVfxFactory3D.CreateNote(transform, "危险区立体音符_" + i,
                    i % 2 == 0 ? primaryColor : secondaryColor, Color.white, attackType == 1 ? 0.30f : 0.26f,
                    attackType == 2 && i == 2);
                if (attackType == 1)
                {
                    float z = Mathf.Lerp(-2.70f, 2.70f, i / Mathf.Max(1f, count - 1f));
                    note.localPosition = new Vector3(i % 2 == 0 ? -0.78f : 0.78f, 0.16f, z);
                }
                else
                {
                    float angle = i / (float)count * Mathf.PI * 2f;
                    float radius = attackType == 2 ? 1.18f : 1.78f;
                    note.localPosition = new Vector3(Mathf.Cos(angle) * radius, 0.16f, Mathf.Sin(angle) * radius);
                }
                note.localRotation = Quaternion.Euler(-12f, i * (360f / count), 0f);
                ornaments[i] = note;
                ornamentScales[i] = note.localScale;
            }
        }

        private void UpdateTransform(bool immediate)
        {
            if (!locked && target != null)
            {
                Vector3 next = target.position;
                next.y = 0.055f;
                trackedPosition = immediate ? next : Vector3.Lerp(trackedPosition, next, 1f - Mathf.Exp(-24f * Time.deltaTime));
            }
            transform.position = trackedPosition;
            if (attackType == 1 && source != null)
            {
                Vector3 direction = trackedPosition - source.position;
                direction.y = 0f;
                if (direction.sqrMagnitude > 0.01f) transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
            }
        }

        private static Material CreateTransparentMaterial(Shader shader, Texture texture, Color color)
        {
            var material = new Material(shader) { name = "红色追踪透明材质", color = color };
            if (texture != null)
            {
                material.mainTexture = texture;
                if (material.HasProperty("_BaseMap")) material.SetTexture("_BaseMap", texture);
            }
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Surface")) material.SetFloat("_Surface", 1f);
            if (material.HasProperty("_SrcBlend")) material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            if (material.HasProperty("_DstBlend")) material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            if (material.HasProperty("_ZWrite")) material.SetFloat("_ZWrite", 0f);
            material.renderQueue = 3070;
            return material;
        }
    }

    /// <summary>K 键发射的三维音符波；飞行、轻微追踪并在接触溪灵时结算。</summary>
    public sealed class PlayerNoteWaveProjectile3D : MonoBehaviour
    {
        private MeixiGame3D game;
        private CreekRhythmEnemy3D target;
        private Vector3 direction;
        private bool onBeat;
        private float age;
        private Transform[] orbiters;
        private Transform[] ribbonRoots;
        private Transform coreNote;

        public static void Create(MeixiGame3D owner, CreekRhythmEnemy3D enemy, Vector3 position, Vector3 travelDirection,
            Material cyan, Material gold, bool rhythmBoosted)
        {
            var root = new GameObject("玩家远程音符波");
            root.transform.position = position;
            root.transform.rotation = Quaternion.LookRotation(travelDirection.normalized, Vector3.up);

            var waveform = root.AddComponent<LineRenderer>();
            waveform.useWorldSpace = false;
            waveform.loop = false;
            waveform.positionCount = 30;
            waveform.widthMultiplier = rhythmBoosted ? 0.11f : 0.075f;
            waveform.material = new Material(cyan);
            waveform.startColor = rhythmBoosted ? Color.white : new Color(0.22f, 0.92f, 1f, 0.96f);
            waveform.endColor = new Color(1f, 0.52f, 0.16f, 0.70f);
            for (int i = 0; i < waveform.positionCount; i++)
            {
                float z = Mathf.Lerp(-0.75f, 0.75f, i / (waveform.positionCount - 1f));
                float envelope = 1f - Mathf.Abs(z) / 0.75f;
                waveform.SetPosition(i, new Vector3(Mathf.Sin(i * 1.55f) * 0.34f * envelope,
                    Mathf.Cos(i * 1.15f) * 0.18f * envelope, z));
            }

            var trail = root.AddComponent<TrailRenderer>();
            trail.time = 0.34f;
            trail.minVertexDistance = 0.04f;
            trail.startWidth = rhythmBoosted ? 0.42f : 0.30f;
            trail.endWidth = 0.015f;
            trail.material = new Material(cyan);
            trail.startColor = rhythmBoosted ? Color.white : new Color(0.18f, 0.92f, 1f, 0.76f);
            trail.endColor = new Color(0.10f, 0.42f, 1f, 0f);

            var goldTrailObject = new GameObject("音符波金色副轨");
            goldTrailObject.transform.SetParent(root.transform, false);
            var goldTrail = goldTrailObject.AddComponent<TrailRenderer>();
            goldTrail.time = 0.24f;
            goldTrail.minVertexDistance = 0.035f;
            goldTrail.startWidth = rhythmBoosted ? 0.16f : 0.10f;
            goldTrail.endWidth = 0.005f;
            goldTrail.material = new Material(gold);
            goldTrail.startColor = new Color(1f, 0.68f, 0.18f, rhythmBoosted ? 0.92f : 0.68f);
            goldTrail.endColor = new Color(1f, 0.32f, 0.08f, 0f);

            Transform coreNote = EnemyMusicVfxFactory3D.CreateNote(root.transform, "音符波核心双音",
                rhythmBoosted ? Color.white : new Color(0.16f, 0.94f, 1f), new Color(1f, 0.62f, 0.16f),
                rhythmBoosted ? 0.48f : 0.38f, true);
            coreNote.localRotation = Quaternion.Euler(0f, 0f, -18f);

            Transform[] ribbons = new Transform[3];
            for (int ribbonIndex = 0; ribbonIndex < ribbons.Length; ribbonIndex++)
            {
                Color ribbonColor = ribbonIndex == 1 ? new Color(1f, 0.62f, 0.16f) : new Color(0.14f, 0.94f, 1f);
                Material ribbonMaterial = EnemyMusicVfxFactory3D.CreateMaterial("玩家音符波螺旋谱带_" + ribbonIndex,
                    new Color(ribbonColor.r, ribbonColor.g, ribbonColor.b, 0.88f), true);
                LineRenderer ribbon = EnemyMusicVfxFactory3D.CreateCurve(root.transform, "玩家音符波螺旋谱带_" + ribbonIndex,
                    ribbonMaterial, 34, rhythmBoosted ? 0.075f : 0.052f);
                ribbon.startColor = ribbonIndex == 0 && rhythmBoosted ? Color.white : ribbonColor;
                ribbon.endColor = new Color(ribbonColor.r, ribbonColor.g, ribbonColor.b, 0.08f);
                for (int i = 0; i < ribbon.positionCount; i++)
                {
                    float u = i / (ribbon.positionCount - 1f);
                    float z = Mathf.Lerp(-0.80f, 0.92f, u);
                    float envelope = Mathf.Sin(u * Mathf.PI);
                    float angle = u * Mathf.PI * 4f + ribbonIndex * Mathf.PI * 0.66f;
                    ribbon.SetPosition(i, new Vector3(Mathf.Cos(angle) * envelope * (0.42f + ribbonIndex * 0.04f),
                        Mathf.Sin(angle) * envelope * (0.34f + ribbonIndex * 0.03f), z));
                }
                ribbons[ribbonIndex] = ribbon.transform;
            }

            Transform[] motes = new Transform[rhythmBoosted ? 7 : 5];
            for (int i = 0; i < motes.Length; i++)
            {
                Transform mote = EnemyMusicVfxFactory3D.CreateNote(root.transform, "音符波泛音_" + i,
                    i % 3 == 1 ? new Color(1f, 0.62f, 0.16f) : new Color(0.12f, 0.94f, 1f), Color.white,
                    0.13f + (i % 2) * 0.025f, false);
                motes[i] = mote;
            }

            var projectile = root.AddComponent<PlayerNoteWaveProjectile3D>();
            projectile.game = owner;
            projectile.target = enemy;
            projectile.direction = travelDirection.normalized;
            projectile.onBeat = rhythmBoosted;
            projectile.orbiters = motes;
            projectile.ribbonRoots = ribbons;
            projectile.coreNote = coreNote;
        }

        private void Update()
        {
            age += Time.deltaTime;
            if (target == null || game == null)
            {
                Destroy(gameObject);
                return;
            }

            Vector3 targetPoint = target.transform.position + Vector3.up * 0.85f;
            Vector3 desired = targetPoint - transform.position;
            if (desired.sqrMagnitude > 0.01f)
                direction = Vector3.Slerp(direction, desired.normalized, 1f - Mathf.Exp(-4.8f * Time.deltaTime));
            transform.position += direction * (onBeat ? 12.6f : 10.8f) * Time.deltaTime;
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction, Vector3.up),
                1f - Mathf.Exp(-12f * Time.deltaTime));
            if (coreNote != null)
            {
                coreNote.Rotate(95f * Time.deltaTime, 180f * Time.deltaTime, -150f * Time.deltaTime, Space.Self);
                float corePulse = 1f + Mathf.Sin(Time.unscaledTime * 22f) * 0.08f;
                coreNote.localScale = Vector3.one * (onBeat ? 0.48f : 0.38f) * corePulse;
            }
            if (ribbonRoots != null)
            {
                for (int i = 0; i < ribbonRoots.Length; i++)
                    if (ribbonRoots[i] != null) ribbonRoots[i].Rotate(0f, 0f, (i % 2 == 0 ? 220f : -190f) * Time.deltaTime, Space.Self);
            }

            if (orbiters != null)
            {
                for (int i = 0; i < orbiters.Length; i++)
                {
                    if (orbiters[i] == null) continue;
                    float angle = Time.unscaledTime * (7f + i * 1.7f) + i * Mathf.PI * 2f / orbiters.Length;
                    orbiters[i].localPosition = new Vector3(Mathf.Cos(angle) * (0.42f + (i % 2) * 0.09f),
                        Mathf.Sin(angle) * (0.34f + (i % 3) * 0.04f), (i - (orbiters.Length - 1) * 0.5f) * 0.13f);
                    orbiters[i].Rotate(80f * Time.deltaTime, 150f * Time.deltaTime, 45f * Time.deltaTime, Space.Self);
                }
            }

            if (Vector3.Distance(transform.position, targetPoint) <= 1.08f)
            {
                game.ResolvePlayerRangedHit(target, onBeat, targetPoint);
                Destroy(gameObject);
                return;
            }
            if (age >= 3.2f) Destroy(gameObject);
        }
    }
}
