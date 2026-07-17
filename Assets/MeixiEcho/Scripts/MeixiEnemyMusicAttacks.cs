using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace MeixiEcho
{
    /// <summary>第一章溪灵的三种锁定型音乐攻击入口。</summary>
    public static class EnemyMusicAttackVfx3D
    {
        public static void Launch(MeixiGame3D game, CreekRhythmEnemy3D owner, Transform player,
            int attackType, Vector3 origin, double impactDsp)
        {
            if (game == null || owner == null || player == null) return;

            switch (attackType)
            {
                case 0:
                    EnemyArcNoteProjectile3D.Create(player, origin + Vector3.up * 1.25f, impactDsp,
                        new Color(1f, 0.13f, 0.24f), new Color(1f, 0.52f, 0.10f));
                    break;
                case 1:
                    EnemyGroundSoundWave3D.Create(player, origin + Vector3.up * 0.12f, impactDsp,
                        new Color(0.08f, 0.88f, 1f), new Color(0.34f, 0.42f, 1f));
                    break;
                default:
                    EnemyHomingChord3D.Create(player, origin + Vector3.up * 1.05f, impactDsp);
                    break;
            }
        }

        public static void LockAllAt(Vector3 preDodgePosition)
        {
            EnemyTrackedAttackVfx3D.LockEveryActiveAttack(preDodgePosition);
        }

        public static void ShatterAllAt(Vector3 position)
        {
            ShatterAllAt(position, new Color(0.18f, 0.96f, 1f));
        }

        public static void ShatterAllAt(Vector3 position, Color color)
        {
            EnemyTrackedAttackVfx3D.ShatterEveryActiveAttack(position, color);
        }

        public static void ClearAll()
        {
            EnemyTrackedAttackVfx3D.ClearEveryActiveAttack();
        }
    }

    public abstract class EnemyTrackedAttackVfx3D : MonoBehaviour
    {
        private static readonly List<EnemyTrackedAttackVfx3D> ActiveAttacks = new();

        protected virtual void OnEnable()
        {
            if (!ActiveAttacks.Contains(this)) ActiveAttacks.Add(this);
        }

        protected virtual void OnDestroy()
        {
            ActiveAttacks.Remove(this);
        }

        public static void LockEveryActiveAttack(Vector3 worldPosition)
        {
            for (int i = ActiveAttacks.Count - 1; i >= 0; i--)
            {
                EnemyTrackedAttackVfx3D attack = ActiveAttacks[i];
                if (attack == null)
                {
                    ActiveAttacks.RemoveAt(i);
                    continue;
                }
                attack.LockImpactPoint(worldPosition);
            }
        }

        public static void ShatterEveryActiveAttack(Vector3 worldPosition, Color color)
        {
            bool shattered = false;
            for (int i = ActiveAttacks.Count - 1; i >= 0; i--)
            {
                EnemyTrackedAttackVfx3D attack = ActiveAttacks[i];
                if (attack == null)
                {
                    ActiveAttacks.RemoveAt(i);
                    continue;
                }
                shattered = true;
                attack.Shatter(worldPosition, color);
            }
            if (shattered) EnemyMusicImpact3D.Create(worldPosition, color, 3.4f, 0.62f, 18);
        }

        public static void ClearEveryActiveAttack()
        {
            for (int i = ActiveAttacks.Count - 1; i >= 0; i--)
            {
                EnemyTrackedAttackVfx3D attack = ActiveAttacks[i];
                ActiveAttacks.RemoveAt(i);
                if (attack != null) Object.Destroy(attack.gameObject);
            }
        }

        protected virtual void Shatter(Vector3 worldPosition, Color color)
        {
            transform.position = worldPosition;
            gameObject.SetActive(false);
            Destroy(gameObject);
        }

        public abstract void LockImpactPoint(Vector3 worldPosition);
    }

    internal static class EnemyMusicVfxFactory3D
    {
        private static Mesh shardMesh;

        public static Mesh GetShardMesh()
        {
            if (shardMesh != null) return shardMesh;
            shardMesh = new Mesh { name = "魂音菱形碎晶网格" };
            shardMesh.vertices = new[]
            {
                new Vector3(0f, 0.72f, 0f), new Vector3(0f, -0.72f, 0f),
                new Vector3(-0.34f, 0f, 0f), new Vector3(0.34f, 0f, 0f),
                new Vector3(0f, 0f, -0.24f), new Vector3(0f, 0f, 0.24f)
            };
            shardMesh.triangles = new[]
            {
                0, 4, 3, 0, 3, 5, 0, 5, 2, 0, 2, 4,
                1, 3, 4, 1, 5, 3, 1, 2, 5, 1, 4, 2
            };
            shardMesh.RecalculateNormals();
            shardMesh.RecalculateBounds();
            return shardMesh;
        }

        public static Material CreateMaterial(string name, Color color, bool transparent = false)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var material = new Material(shader) { name = name, color = color };
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", 0.72f);
            if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", 0.16f);
            if (material.HasProperty("_EmissionColor"))
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", new Color(color.r, color.g, color.b, 1f) * 2.35f);
            }
            if (transparent)
            {
                if (material.HasProperty("_Surface")) material.SetFloat("_Surface", 1f);
                if (material.HasProperty("_SrcBlend")) material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
                if (material.HasProperty("_DstBlend")) material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
                if (material.HasProperty("_ZWrite")) material.SetFloat("_ZWrite", 0f);
                material.renderQueue = 3050;
            }
            return material;
        }

        public static Transform CreateNote(Transform parent, string name, Color mainColor, Color accentColor,
            float scale, bool doubleNote = false)
        {
            var root = new GameObject(name).transform;
            root.SetParent(parent, false);
            root.localScale = Vector3.one * scale;
            Material main = CreateMaterial(name + "_主体", mainColor);
            Material accent = CreateMaterial(name + "_高光", accentColor);

            CreatePart(PrimitiveType.Sphere, "立体音符头", root, new Vector3(-0.17f, -0.24f, 0f),
                new Vector3(0.52f, 0.34f, 0.25f), Quaternion.Euler(0f, 0f, -18f), main);
            CreatePart(PrimitiveType.Sphere, "音符头高光", root, new Vector3(-0.24f, -0.18f, -0.12f),
                new Vector3(0.18f, 0.10f, 0.07f), Quaternion.Euler(0f, 0f, -18f), accent);
            CreatePart(PrimitiveType.Cylinder, "音符柄", root, new Vector3(0.08f, 0.34f, 0f),
                new Vector3(0.065f, 0.48f, 0.065f), Quaternion.identity, main);

            Vector3[] flagPoints =
            {
                new(0.08f, 0.79f, 0f), new(0.30f, 0.74f, 0f), new(0.48f, 0.60f, 0f),
                new(0.52f, 0.40f, 0f), new(0.40f, 0.27f, 0f)
            };
            for (int i = 0; i < flagPoints.Length; i++)
            {
                float size = Mathf.Lerp(0.15f, 0.08f, i / (flagPoints.Length - 1f));
                CreatePart(PrimitiveType.Sphere, "弯曲音符旗_" + i, root, flagPoints[i],
                    new Vector3(size * 1.35f, size, size * 0.55f), Quaternion.identity, i < 2 ? accent : main);
            }

            if (doubleNote)
            {
                CreatePart(PrimitiveType.Sphere, "和声音符头", root, new Vector3(0.40f, -0.15f, 0.02f),
                    new Vector3(0.45f, 0.30f, 0.23f), Quaternion.Euler(0f, 0f, -15f), accent);
                CreatePart(PrimitiveType.Cylinder, "和声音符柄", root, new Vector3(0.62f, 0.34f, 0.02f),
                    new Vector3(0.055f, 0.42f, 0.055f), Quaternion.identity, accent);
                CreatePart(PrimitiveType.Cube, "和弦连接梁", root, new Vector3(0.35f, 0.75f, 0.01f),
                    new Vector3(0.62f, 0.10f, 0.11f), Quaternion.Euler(0f, 0f, -8f), main);
            }
            return root;
        }

        public static LineRenderer CreateCurve(Transform parent, string name, Material material, int points,
            float width, bool loop = false)
        {
            var objectRoot = new GameObject(name);
            objectRoot.transform.SetParent(parent, false);
            var line = objectRoot.AddComponent<LineRenderer>();
            line.useWorldSpace = false;
            line.loop = loop;
            line.positionCount = points;
            line.widthMultiplier = width;
            line.numCornerVertices = 3;
            line.numCapVertices = 4;
            line.material = material;
            return line;
        }

        public static TrailRenderer AddTrail(GameObject owner, Material material, Color start, Color end,
            float width, float lifetime)
        {
            var trail = owner.AddComponent<TrailRenderer>();
            trail.time = lifetime;
            trail.minVertexDistance = 0.025f;
            trail.startWidth = width;
            trail.endWidth = 0.01f;
            trail.material = material;
            trail.startColor = start;
            trail.endColor = end;
            trail.widthCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);
            return trail;
        }

        public static void CreateCastBurst(Vector3 position, Color color)
        {
            EnemyMusicImpact3D.Create(position, color, 0.72f, 0.30f, 3);
        }

        private static GameObject CreatePart(PrimitiveType primitive, string name, Transform parent,
            Vector3 localPosition, Vector3 localScale, Quaternion localRotation, Material material)
        {
            GameObject part = GameObject.CreatePrimitive(primitive);
            part.name = name;
            part.transform.SetParent(parent, false);
            part.transform.localPosition = localPosition;
            part.transform.localScale = localScale;
            part.transform.localRotation = localRotation;
            part.GetComponent<Renderer>().material = material;
            Collider collider = part.GetComponent<Collider>();
            if (collider != null) Object.Destroy(collider);
            return part;
        }
    }

    public sealed class EnemyArcNoteProjectile3D : EnemyTrackedAttackVfx3D
    {
        private Transform target;
        private Transform note;
        private LineRenderer trajectory;
        private Vector3 start;
        private Vector3 lockedTarget;
        private Vector3 previousPosition;
        private double launchDsp;
        private double impactDsp;
        private bool targetLocked;
        private bool completed;
        private Color mainColor;

        public static void Create(Transform player, Vector3 origin, double hitDsp, Color main, Color accent)
        {
            var root = new GameObject("溪灵攻击·高抛八分音符");
            root.transform.position = origin;
            var effect = root.AddComponent<EnemyArcNoteProjectile3D>();
            effect.target = player;
            effect.start = origin;
            effect.lockedTarget = player.position + Vector3.up * 0.12f;
            effect.previousPosition = origin;
            effect.launchDsp = AudioSettings.dspTime;
            effect.impactDsp = System.Math.Max(hitDsp, effect.launchDsp + 0.16d);
            effect.mainColor = main;

            effect.note = EnemyMusicVfxFactory3D.CreateNote(root.transform, "朱红坠音", main, accent, 0.86f);
            Material trailMaterial = EnemyMusicVfxFactory3D.CreateMaterial("高抛音符流光", new Color(main.r, main.g, main.b, 0.82f), true);
            EnemyMusicVfxFactory3D.AddTrail(root, trailMaterial, Color.white,
                new Color(main.r, main.g, main.b, 0f), 0.38f, 0.34f);
            effect.trajectory = EnemyMusicVfxFactory3D.CreateCurve(root.transform, "弧形五线谱轨迹", trailMaterial, 34, 0.045f);
            effect.trajectory.useWorldSpace = true;
            effect.trajectory.startColor = new Color(accent.r, accent.g, accent.b, 0.68f);
            effect.trajectory.endColor = new Color(main.r, main.g, main.b, 0.08f);
            EnemyMusicVfxFactory3D.CreateCastBurst(origin - Vector3.up * 1.10f, accent);
            effect.UpdateTrajectory(0f);
        }

        private void Update()
        {
            if (target == null || completed)
            {
                Destroy(gameObject);
                return;
            }

            float t = Progress();
            if (!targetLocked)
            {
                lockedTarget = target.position + Vector3.up * 0.12f;
                if (t >= 0.80f) targetLocked = true;
            }

            Vector3 next = Evaluate(t);
            Vector3 velocity = next - previousPosition;
            transform.position = next;
            if (velocity.sqrMagnitude > 0.0001f)
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(velocity.normalized, Vector3.up),
                    1f - Mathf.Exp(-18f * Time.deltaTime));
            previousPosition = next;
            if (note != null)
            {
                note.Rotate(80f * Time.deltaTime, 145f * Time.deltaTime, -110f * Time.deltaTime, Space.Self);
                note.localScale = Vector3.one * (0.86f * (1f + Mathf.Sin(Time.unscaledTime * 15f) * 0.06f));
            }
            UpdateTrajectory(t);

            if (t >= 1f)
            {
                completed = true;
                EnemyMusicImpact3D.Create(lockedTarget, mainColor, 2.65f, 0.62f, 10);
                Destroy(gameObject);
            }
        }

        public override void LockImpactPoint(Vector3 worldPosition)
        {
            lockedTarget = worldPosition + Vector3.up * 0.12f;
            targetLocked = true;
            UpdateTrajectory(Progress());
        }

        private float Progress() => Mathf.Clamp01((float)((AudioSettings.dspTime - launchDsp) /
            System.Math.Max(0.05d, impactDsp - launchDsp)));

        private Vector3 Evaluate(float t)
        {
            Vector3 flat = Vector3.Lerp(start, lockedTarget, t);
            float distance = Vector3.ProjectOnPlane(lockedTarget - start, Vector3.up).magnitude;
            float height = 2.25f + distance * 0.16f;
            Vector3 flatDirection = Vector3.ProjectOnPlane(lockedTarget - start, Vector3.up).normalized;
            Vector3 side = Vector3.Cross(Vector3.up, flatDirection);
            return flat + Vector3.up * (Mathf.Sin(t * Mathf.PI) * height) +
                   side * (Mathf.Sin(t * Mathf.PI * 2f) * 0.24f * (1f - t));
        }

        private void UpdateTrajectory(float progress)
        {
            if (trajectory == null) return;
            for (int i = 0; i < trajectory.positionCount; i++)
            {
                float t = i / (trajectory.positionCount - 1f);
                Vector3 point = Evaluate(t);
                point += Vector3.up * Mathf.Sin(t * Mathf.PI * 8f) * 0.035f;
                trajectory.SetPosition(i, point);
            }
            float alpha = Mathf.Lerp(0.70f, 0.12f, progress);
            Color color = trajectory.startColor;
            color.a = alpha;
            trajectory.startColor = color;
        }
    }

    public sealed class EnemyGroundSoundWave3D : EnemyTrackedAttackVfx3D
    {
        private Transform target;
        private Transform emblem;
        private LineRenderer[] crests;
        private Vector3 start;
        private Vector3 lockedTarget;
        private double launchDsp;
        private double impactDsp;
        private bool targetLocked;
        private Color mainColor;

        public static void Create(Transform player, Vector3 origin, double hitDsp, Color main, Color accent)
        {
            var root = new GameObject("溪灵攻击·低音三重冲击波");
            root.transform.position = origin;
            var effect = root.AddComponent<EnemyGroundSoundWave3D>();
            effect.target = player;
            effect.start = origin;
            effect.lockedTarget = player.position + Vector3.up * 0.10f;
            effect.launchDsp = AudioSettings.dspTime;
            effect.impactDsp = System.Math.Max(hitDsp, effect.launchDsp + 0.16d);
            effect.mainColor = main;
            Material mainMaterial = EnemyMusicVfxFactory3D.CreateMaterial("溪蓝冲击波", new Color(main.r, main.g, main.b, 0.90f), true);
            Material accentMaterial = EnemyMusicVfxFactory3D.CreateMaterial("靛蓝冲击波", new Color(accent.r, accent.g, accent.b, 0.82f), true);

            effect.crests = new LineRenderer[3];
            for (int crestIndex = 0; crestIndex < effect.crests.Length; crestIndex++)
            {
                LineRenderer crest = EnemyMusicVfxFactory3D.CreateCurve(root.transform, "立体声浪弧面_" + crestIndex,
                    crestIndex == 1 ? accentMaterial : mainMaterial, 31, 0.12f - crestIndex * 0.018f);
                crest.transform.localPosition = new Vector3(0f, 0f, (crestIndex - 1) * 0.38f);
                crest.startColor = crestIndex == 1 ? accent : Color.white;
                crest.endColor = new Color(main.r, main.g, main.b, 0.16f);
                for (int i = 0; i < crest.positionCount; i++)
                {
                    float u = i / (crest.positionCount - 1f);
                    float x = Mathf.Lerp(-1.25f, 1.25f, u);
                    float envelope = Mathf.Sin(u * Mathf.PI);
                    float y = envelope * (0.74f + crestIndex * 0.18f);
                    float z = Mathf.Sin(u * Mathf.PI * 4f) * 0.065f;
                    crest.SetPosition(i, new Vector3(x, y, z));
                }
                effect.crests[crestIndex] = crest;
            }

            effect.emblem = EnemyMusicVfxFactory3D.CreateNote(root.transform, "冲击波低音和弦", main, Color.white, 0.50f, true);
            effect.emblem.localPosition = new Vector3(0f, 0.42f, 0.05f);
            EnemyMusicVfxFactory3D.AddTrail(root, mainMaterial, new Color(main.r, main.g, main.b, 0.78f),
                new Color(accent.r, accent.g, accent.b, 0f), 1.35f, 0.28f);
            EnemyMusicVfxFactory3D.CreateCastBurst(origin, main);
        }

        private void Update()
        {
            if (target == null)
            {
                Destroy(gameObject);
                return;
            }
            float t = Mathf.Clamp01((float)((AudioSettings.dspTime - launchDsp) /
                System.Math.Max(0.05d, impactDsp - launchDsp)));
            if (!targetLocked)
            {
                lockedTarget = target.position + Vector3.up * 0.10f;
                if (t >= 0.80f) targetLocked = true;
            }

            Vector3 flatDirection = Vector3.ProjectOnPlane(lockedTarget - transform.position, Vector3.up);
            transform.position = Vector3.Lerp(start, lockedTarget, t) + Vector3.up * Mathf.Sin(t * Mathf.PI) * 0.16f;
            if (flatDirection.sqrMagnitude > 0.001f)
                transform.rotation = Quaternion.LookRotation(flatDirection.normalized, Vector3.up);
            float pulse = 1f + Mathf.Sin(Time.unscaledTime * 24f) * 0.08f;
            transform.localScale = new Vector3(Mathf.Lerp(0.72f, 1.32f, t) * pulse,
                Mathf.Lerp(0.72f, 1.18f, t), Mathf.Lerp(0.72f, 1.08f, t));
            if (emblem != null) emblem.Rotate(0f, 0f, 150f * Time.deltaTime, Space.Self);

            if (t >= 1f)
            {
                EnemyMusicImpact3D.Create(lockedTarget, mainColor, 3.8f, 0.72f, 14);
                Destroy(gameObject);
            }
        }

        public override void LockImpactPoint(Vector3 worldPosition)
        {
            lockedTarget = worldPosition + Vector3.up * 0.10f;
            targetLocked = true;
        }
    }

    public sealed class EnemyHomingChord3D : EnemyTrackedAttackVfx3D
    {
        private Transform target;
        private Transform[] notes;
        private Vector3[] starts;
        private Vector3 lockedTarget;
        private double launchDsp;
        private double impactDsp;
        private bool targetLocked;

        public static void Create(Transform player, Vector3 origin, double hitDsp)
        {
            var root = new GameObject("溪灵攻击·追踪五声音群");
            root.transform.position = Vector3.zero;
            var effect = root.AddComponent<EnemyHomingChord3D>();
            effect.target = player;
            effect.lockedTarget = player.position + Vector3.up * 0.82f;
            effect.launchDsp = AudioSettings.dspTime;
            effect.impactDsp = System.Math.Max(hitDsp, effect.launchDsp + 0.16d);
            effect.notes = new Transform[5];
            effect.starts = new Vector3[5];
            Color[] colors =
            {
                new(0.78f, 0.16f, 1f), new(1f, 0.28f, 0.66f), new(1f, 0.68f, 0.16f),
                new(0.20f, 0.88f, 1f), new(0.46f, 0.34f, 1f)
            };
            Vector3 right = player != null ? Vector3.Cross(Vector3.up, (player.position - origin).normalized) : Vector3.right;
            for (int i = 0; i < effect.notes.Length; i++)
            {
                float offset = i - (effect.notes.Length - 1) * 0.5f;
                effect.starts[i] = origin + right * offset * 0.34f + Vector3.up * (Mathf.Abs(offset) * 0.18f);
                Transform note = EnemyMusicVfxFactory3D.CreateNote(root.transform, "追踪音符_" + (i + 1),
                    colors[i], Color.white, i == 2 ? 0.52f : 0.38f, i == 2);
                note.position = effect.starts[i];
                Material trail = EnemyMusicVfxFactory3D.CreateMaterial("追踪音符流光_" + i,
                    new Color(colors[i].r, colors[i].g, colors[i].b, 0.78f), true);
                EnemyMusicVfxFactory3D.AddTrail(note.gameObject, trail, Color.white,
                    new Color(colors[i].r, colors[i].g, colors[i].b, 0f), i == 2 ? 0.24f : 0.16f, 0.38f);
                effect.notes[i] = note;
            }
            EnemyMusicVfxFactory3D.CreateCastBurst(origin - Vector3.up * 0.88f, new Color(0.78f, 0.18f, 1f));
        }

        private void Update()
        {
            if (target == null)
            {
                Destroy(gameObject);
                return;
            }
            float t = Mathf.Clamp01((float)((AudioSettings.dspTime - launchDsp) /
                System.Math.Max(0.05d, impactDsp - launchDsp)));
            if (!targetLocked)
            {
                lockedTarget = target.position + Vector3.up * 0.82f;
                if (t >= 0.80f) targetLocked = true;
            }

            Vector3 travel = Vector3.ProjectOnPlane(lockedTarget - starts[2], Vector3.up).normalized;
            Vector3 right = Vector3.Cross(Vector3.up, travel);
            for (int i = 0; i < notes.Length; i++)
            {
                if (notes[i] == null) continue;
                float phase = i * Mathf.PI * 2f / notes.Length;
                float spread = (1f - t) * (0.56f + Mathf.Abs(i - 2) * 0.11f);
                Vector3 spiral = right * Mathf.Cos(phase + t * Mathf.PI * 3.5f) * spread +
                                 Vector3.up * Mathf.Sin(phase + t * Mathf.PI * 3.5f) * spread * 0.72f;
                float arch = Mathf.Sin(t * Mathf.PI) * (0.85f + i * 0.12f);
                Vector3 position = Vector3.Lerp(starts[i], lockedTarget, t) + spiral + Vector3.up * arch;
                Vector3 velocity = position - notes[i].position;
                notes[i].position = position;
                if (velocity.sqrMagnitude > 0.0001f)
                    notes[i].rotation = Quaternion.LookRotation(velocity.normalized, Vector3.up) * Quaternion.Euler(0f, 0f, 18f);
                notes[i].Rotate(0f, 0f, (110f + i * 18f) * Time.deltaTime, Space.Self);
            }

            if (t >= 1f)
            {
                EnemyMusicImpact3D.Create(lockedTarget - Vector3.up * 0.72f, new Color(0.86f, 0.18f, 1f), 2.9f, 0.68f, 12);
                Destroy(gameObject);
            }
        }

        public override void LockImpactPoint(Vector3 worldPosition)
        {
            lockedTarget = worldPosition + Vector3.up * 0.82f;
            targetLocked = true;
        }
    }

    public sealed class EnemyMusicImpact3D : MonoBehaviour
    {
        private LineRenderer[] rings;
        private Color[] colors;
        private Transform[] shards;
        private Vector3[] shardDirections;
        private float radius;
        private float duration;
        private float age;

        public static void Create(Vector3 position, Color color, float targetRadius, float lifetime, int shardCount)
        {
            var root = new GameObject("音符命中·立体声纹冲击");
            root.transform.position = new Vector3(position.x, Mathf.Max(0.07f, position.y), position.z);
            var effect = root.AddComponent<EnemyMusicImpact3D>();
            effect.Build(color, targetRadius, lifetime, shardCount);
        }

        private void Build(Color color, float targetRadius, float lifetime, int shardCount)
        {
            radius = targetRadius;
            duration = Mathf.Max(0.16f, lifetime);
            rings = new LineRenderer[3];
            colors = new Color[rings.Length];
            for (int ringIndex = 0; ringIndex < rings.Length; ringIndex++)
            {
                Color ringColor = ringIndex == 1 ? Color.Lerp(color, Color.white, 0.58f) :
                    Color.Lerp(color, new Color(0.15f, 0.86f, 1f), ringIndex * 0.28f);
                colors[ringIndex] = ringColor;
                Material material = EnemyMusicVfxFactory3D.CreateMaterial("声纹冲击环_" + ringIndex,
                    new Color(ringColor.r, ringColor.g, ringColor.b, 0.86f), true);
                LineRenderer ring = EnemyMusicVfxFactory3D.CreateCurve(transform, "十二瓣声纹环_" + ringIndex,
                    material, 72, 0.095f - ringIndex * 0.014f, true);
                ring.transform.localPosition = Vector3.up * (0.035f + ringIndex * 0.08f);
                ring.startColor = ringColor;
                ring.endColor = Color.Lerp(ringColor, Color.white, 0.32f);
                for (int i = 0; i < ring.positionCount; i++)
                {
                    float angle = i / (float)ring.positionCount * Mathf.PI * 2f;
                    float lobes = 1f + Mathf.Sin(angle * (ringIndex == 1 ? 8f : 12f)) * (0.10f - ringIndex * 0.018f);
                    ring.SetPosition(i, new Vector3(Mathf.Cos(angle) * lobes, Mathf.Sin(angle * 4f) * 0.045f,
                        Mathf.Sin(angle) * lobes));
                }
                rings[ringIndex] = ring;
            }

            int count = Mathf.Clamp(shardCount, 0, 18);
            shards = new Transform[count];
            shardDirections = new Vector3[count];
            for (int i = 0; i < count; i++)
            {
                float angle = i / Mathf.Max(1f, count) * Mathf.PI * 2f;
                Color shardColor = Color.Lerp(color, Color.white, (i % 3) * 0.22f);
                Transform note = EnemyMusicVfxFactory3D.CreateNote(transform, "冲击音符碎片_" + i,
                    shardColor, Color.white, 0.13f + (i % 3) * 0.025f);
                note.localPosition = Vector3.up * 0.18f;
                shards[i] = note;
                shardDirections[i] = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
            }

            var particlesObject = new GameObject("声纹粒子喷泉");
            particlesObject.transform.SetParent(transform, false);
            var particles = particlesObject.AddComponent<ParticleSystem>();
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = particles.main;
            main.duration = duration;
            main.loop = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.22f, duration * 0.92f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(radius * 1.6f, radius * 3.1f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.035f, 0.14f);
            main.startColor = new ParticleSystem.MinMaxGradient(color, Color.white);
            main.maxParticles = 52;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            var emission = particles.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)Mathf.Clamp(count * 3, 12, 48)) });
            var shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Hemisphere;
            shape.radius = 0.28f;
            var renderer = particles.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Mesh;
            renderer.mesh = EnemyMusicVfxFactory3D.GetShardMesh();
            renderer.material = EnemyMusicVfxFactory3D.CreateMaterial("声纹粒子", color);
            particles.Play();
        }

        private void Update()
        {
            age += Time.deltaTime;
            float t = Mathf.Clamp01(age / duration);
            float eased = 1f - Mathf.Pow(1f - t, 3f);
            for (int i = 0; i < rings.Length; i++)
            {
                if (rings[i] == null) continue;
                float ringScale = Mathf.Lerp(0.16f + i * 0.09f, radius * (1f - i * 0.12f), eased);
                rings[i].transform.localScale = Vector3.one * ringScale;
                Color faded = colors[i];
                faded.a = 1f - Mathf.SmoothStep(0.42f, 1f, t);
                rings[i].startColor = faded;
                rings[i].endColor = Color.Lerp(faded, Color.white, 0.28f);
                rings[i].widthMultiplier = Mathf.Lerp(0.14f, 0.025f, t);
                rings[i].transform.Rotate(0f, (i % 2 == 0 ? 95f : -125f) * Time.deltaTime, 0f, Space.Self);
            }
            for (int i = 0; i < shards.Length; i++)
            {
                if (shards[i] == null) continue;
                float distance = radius * eased * (0.50f + (i % 4) * 0.08f);
                float height = Mathf.Sin(t * Mathf.PI) * (0.55f + (i % 3) * 0.18f);
                shards[i].localPosition = shardDirections[i] * distance + Vector3.up * height;
                shards[i].Rotate(100f * Time.deltaTime, 170f * Time.deltaTime, 130f * Time.deltaTime, Space.Self);
                shards[i].localScale = Vector3.one * Mathf.Lerp(1f, 0.22f, t);
            }
            if (age >= duration) Destroy(gameObject);
        }
    }
}
