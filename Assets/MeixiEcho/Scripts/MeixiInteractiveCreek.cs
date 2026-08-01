using UnityEngine;

namespace MeixiEcho
{
    /// <summary>
    /// Lightweight shallow-water interaction for the chapter-one creek. The water
    /// remains a GPU-rendered surface, while this component supplies persistent
    /// ripples, rock flow obstacles, splash droplets and a small physical current.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MeixiInteractiveCreek : MonoBehaviour
    {
        private const int MaxRipples = 12;
        private const int MaxObstacles = 12;
        private const float CreekStartX = -24f;
        private const float CreekEndX = 24f;
        private const float RippleLifetime = 2.8f;

        private static readonly int RippleCountId = Shader.PropertyToID("_MeixiWaterRippleCount");
        private static readonly int RipplesId = Shader.PropertyToID("_MeixiWaterRipples");
        private static readonly int ObstacleCountId = Shader.PropertyToID("_MeixiWaterObstacleCount");
        private static readonly int ObstaclesId = Shader.PropertyToID("_MeixiWaterObstacles");
        private static readonly int InteractorId = Shader.PropertyToID("_MeixiWaterInteractor");

        private static MeixiInteractiveCreek active;

        private readonly Vector4[] ripples = new Vector4[MaxRipples];
        private readonly Vector4[] obstacles = new Vector4[MaxObstacles];
        private Transform player;
        private ParticleSystem splashParticles;
        private Vector3 lastPlayerPosition;
        private float surfaceY = -.305f;
        private float nextMovingRipple;
        private float nextPlayerSearch;
        private int rippleCursor;
        private int rippleCount;
        private int obstacleCount;
        private bool playerWasInWater;

        public void Configure(float waterSurfaceY, Vector4[] flowObstacles)
        {
            surfaceY = waterSurfaceY;
            obstacleCount = Mathf.Min(flowObstacles?.Length ?? 0, MaxObstacles);
            for (int i = 0; i < obstacleCount; i++) obstacles[i] = flowObstacles[i];
            UploadObstacles();
        }

        public static bool TryGetWaterState(Vector3 worldPosition, out float depth, out Vector3 current)
        {
            if (active != null && active.Contains(worldPosition, out depth))
            {
                float derivative = CreekCenterZ(worldPosition.x + .12f) - CreekCenterZ(worldPosition.x - .12f);
                current = new Vector3(1f, 0f, derivative / .24f).normalized;
                return true;
            }

            depth = 0f;
            current = Vector3.zero;
            return false;
        }

        public static void EmitValidationInteraction(Vector3 worldPosition)
        {
            if (active == null) return;
            active.AddRipple(worldPosition, 1f, true);
            active.UploadRipples();
        }

        private void OnEnable()
        {
            active = this;
            CreateSplashParticles();
            UploadObstacles();
        }

        private void OnDisable()
        {
            if (active == this) active = null;
            Shader.SetGlobalInt(RippleCountId, 0);
            Shader.SetGlobalInt(ObstacleCountId, 0);
            Shader.SetGlobalVector(InteractorId, Vector4.zero);
        }

        private void LateUpdate()
        {
            if (player == null && Time.unscaledTime >= nextPlayerSearch)
            {
                nextPlayerSearch = Time.unscaledTime + .5f;
                MeixiThirdPersonPlayer controller = FindAnyObjectByType<MeixiThirdPersonPlayer>();
                player = controller != null ? controller.transform : null;
                if (player != null) lastPlayerPosition = player.position;
            }

            if (player == null)
            {
                Shader.SetGlobalVector(InteractorId, Vector4.zero);
                return;
            }

            Vector3 position = player.position;
            float planarSpeed = Vector2.Distance(new Vector2(position.x, position.z),
                new Vector2(lastPlayerPosition.x, lastPlayerPosition.z)) / Mathf.Max(Time.deltaTime, .001f);
            bool inWater = Contains(position, out float depth);
            float immersion = inWater ? Mathf.InverseLerp(.02f, .38f, depth) : 0f;
            Shader.SetGlobalVector(InteractorId, new Vector4(position.x, position.z, immersion, planarSpeed));

            if (inWater && !playerWasInWater)
                AddRipple(position, .92f, true);
            else if (!inWater && playerWasInWater)
                AddRipple(lastPlayerPosition, .58f, true);
            else if (inWater && planarSpeed > .22f && Time.time >= nextMovingRipple)
            {
                float strength = Mathf.Lerp(.28f, .78f, Mathf.InverseLerp(.2f, 5.5f, planarSpeed));
                AddRipple(position, strength, planarSpeed > 1.8f);
                nextMovingRipple = Time.time + Mathf.Lerp(.28f, .11f, Mathf.InverseLerp(.2f, 5.5f, planarSpeed));
            }

            playerWasInWater = inWater;
            lastPlayerPosition = position;
            UploadRipples();
        }

        private bool Contains(Vector3 worldPosition, out float depth)
        {
            if (worldPosition.x < CreekStartX || worldPosition.x > CreekEndX)
            {
                depth = 0f;
                return false;
            }

            float lateralDistance = Mathf.Abs(worldPosition.z - CreekCenterZ(worldPosition.x));
            bool insideRibbon = lateralDistance < CreekHalfWidth(worldPosition.x) * .94f;
            depth = Mathf.Max(0f, surfaceY - worldPosition.y);
            return insideRibbon && worldPosition.y < surfaceY + .16f && worldPosition.y > surfaceY - 1.15f;
        }

        private void AddRipple(Vector3 position, float strength, bool splash)
        {
            float centerZ = CreekCenterZ(position.x);
            float halfWidth = CreekHalfWidth(position.x) * .88f;
            position.z = Mathf.Clamp(position.z, centerZ - halfWidth, centerZ + halfWidth);
            position.y = surfaceY + .015f;
            ripples[rippleCursor] = new Vector4(position.x, position.z, Time.time, Mathf.Clamp01(strength));
            rippleCursor = (rippleCursor + 1) % MaxRipples;
            rippleCount = Mathf.Min(rippleCount + 1, MaxRipples);

            if (splash && splashParticles != null)
            {
                int count = Mathf.RoundToInt(Mathf.Lerp(3f, 9f, strength));
                for (int i = 0; i < count; i++)
                {
                    float angle = (i / (float)Mathf.Max(1, count) * 360f + Time.frameCount * 23f) * Mathf.Deg2Rad;
                    Vector3 radial = new(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
                    ParticleSystem.EmitParams emit = new()
                    {
                        position = position + radial * .08f,
                        velocity = radial * Mathf.Lerp(.24f, .72f, strength) + Vector3.up * Mathf.Lerp(.45f, 1.35f, strength),
                        startLifetime = Mathf.Lerp(.34f, .68f, strength),
                        startSize = Mathf.Lerp(.025f, .075f, strength),
                        startColor = new Color(.68f, .88f, .88f, .72f)
                    };
                    splashParticles.Emit(emit, 1);
                }
            }
        }

        private void UploadRipples()
        {
            // Expired entries remain in the fixed array; the shader fades them by age.
            // Periodically collapse the count when every visible ring has dissipated.
            bool anyAlive = false;
            for (int i = 0; i < rippleCount; i++)
                anyAlive |= Time.time - ripples[i].z < RippleLifetime;
            if (!anyAlive)
            {
                rippleCount = 0;
                rippleCursor = 0;
            }
            Shader.SetGlobalInt(RippleCountId, rippleCount);
            Shader.SetGlobalVectorArray(RipplesId, ripples);
        }

        private void UploadObstacles()
        {
            Shader.SetGlobalInt(ObstacleCountId, obstacleCount);
            Shader.SetGlobalVectorArray(ObstaclesId, obstacles);
        }

        private void CreateSplashParticles()
        {
            if (splashParticles != null) return;
            GameObject particlesObject = new("浅水交互飞溅");
            particlesObject.transform.SetParent(transform, false);
            splashParticles = particlesObject.AddComponent<ParticleSystem>();

            ParticleSystem.MainModule main = splashParticles.main;
            main.loop = false;
            main.playOnAwake = false;
            main.maxParticles = 96;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.gravityModifier = .78f;
            main.startLifetime = .5f;
            main.startSpeed = 0f;
            main.startSize = .055f;
            main.startColor = new Color(.68f, .88f, .88f, .72f);

            ParticleSystem.EmissionModule emission = splashParticles.emission;
            emission.enabled = false;
            ParticleSystem.ShapeModule shape = splashParticles.shape;
            shape.enabled = false;
            ParticleSystem.ColorOverLifetimeModule colorOverLifetime = splashParticles.colorOverLifetime;
            colorOverLifetime.enabled = true;
            Gradient fade = new();
            fade.SetKeys(
                new[] { new GradientColorKey(new Color(.78f, .94f, .93f), 0f), new GradientColorKey(new Color(.30f, .58f, .60f), 1f) },
                new[] { new GradientAlphaKey(.78f, 0f), new GradientAlphaKey(0f, 1f) });
            colorOverLifetime.color = fade;

            ParticleSystemRenderer renderer = particlesObject.GetComponent<ParticleSystemRenderer>();
            Shader shader = Shader.Find("MeixiEcho/Water Droplet");
            if (shader != null)
            {
                Material material = new(shader) { name = "浅水飞溅粒子材质" };
                if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", Color.white);
                renderer.sharedMaterial = material;
            }
        }

        private static float CreekCenterZ(float x) =>
            -11f + Mathf.Sin((x + 15f) * .21f) * 2f + Mathf.Sin((x - 4f) * .075f) * .34f;

        private static float CreekHalfWidth(float x)
        {
            float sample = Mathf.InverseLerp(CreekStartX, CreekEndX, x) * 44f;
            return 2.25f + Mathf.Sin(sample * 1.73f) * .18f + Mathf.Sin(sample * .53f) * .12f;
        }
    }
}
