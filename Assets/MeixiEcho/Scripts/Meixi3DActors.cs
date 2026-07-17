using UnityEngine;
using UnityEngine.UI;

namespace MeixiEcho
{
    public class SoulNode3DBase : MonoBehaviour
    {
        [SerializeField] private MeixiGame3D game;
        [SerializeField] private Renderer core;
        [SerializeField] private Transform haloA;
        [SerializeField] private Transform haloB;
        [SerializeField] private Vector3 basePosition;

        public int Index { get; private set; }
        public bool Completed { get; private set; }

        public void Configure(MeixiGame3D owner, int index, Renderer renderer, Transform firstHalo, Transform secondHalo)
        {
            game = owner;
            Index = index;
            core = renderer;
            haloA = firstHalo;
            haloB = secondHalo;
            basePosition = transform.position;
        }

        public void Rebind(MeixiGame3D owner)
        {
            game = owner;
            basePosition = transform.position;
        }

        protected virtual void Update()
        {
            transform.position = basePosition + Vector3.up * (Mathf.Sin(Time.time * 1.8f + Index) * 0.2f);
            if (haloA != null) haloA.Rotate(0f, 55f * Time.deltaTime, 24f * Time.deltaTime, Space.Self);
            if (haloB != null) haloB.Rotate(20f * Time.deltaTime, -38f * Time.deltaTime, 0f, Space.Self);
        }

        public void Interact()
        {
            if (Completed || game == null) return;
            game.BeginMemory(Index, (SoulNode3D)this);
        }

        public void Complete()
        {
            Completed = true;
            if (core != null) core.material.color = new Color(0.28f, 1f, 0.70f);
            transform.localScale *= 0.70f;
            game.BurstSoulParticles(transform.position);
        }
    }

    public class NoiseBeast3DBase : MonoBehaviour
    {
        private MeixiGame3D game;
        private Renderer body;
        private Vector3 origin;
        private float angle;
        private float hitCooldown;
        private int health = 4;

        public void Configure(MeixiGame3D owner, Renderer renderer, float phase)
        {
            game = owner;
            body = renderer;
            origin = transform.position;
            angle = phase;
        }

        protected virtual void Update()
        {
            if (game == null || !game.GameplayRunning || health <= 0) return;
            hitCooldown -= Time.deltaTime;
            angle += Time.deltaTime * 0.72f;
            Vector3 wander = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * 2.3f;
            transform.position = origin + wander + Vector3.up * (0.85f + Mathf.Sin(Time.time * 4f + angle) * 0.18f);
            transform.Rotate(33f * Time.deltaTime, 75f * Time.deltaTime, 47f * Time.deltaTime);

            if (hitCooldown <= 0f && Vector3.Distance(transform.position, game.PlayerPosition + Vector3.up) < 1.65f)
            {
                hitCooldown = 1.1f;
                game.DamagePlayer();
            }
        }

        public void Hit(bool onBeat)
        {
            health -= onBeat ? 2 : 1;
            if (body != null) body.material.color = onBeat ? Color.white : new Color(0.72f, 0.32f, 0.90f);
            if (health <= 0)
            {
                game.EnemyDefeated(transform.position);
                Destroy(gameObject);
            }
        }
    }

    public class CreekRhythmEnemy3DBase : MonoBehaviour
    {
        private const string AnimatorControllerResource = "Animations/LinMoCombat";
        private const string CharacterModelResource = "Models/Characters/LinMo";
        private const int MaxHealth = 36;
        private const double AttackResolutionDelay = 0.10d;
        private static readonly int[] AttackPattern = { 2, 0, 1, 2, 0, 2, 1, 0, 1, 2, 2, 0 };
        private readonly int speedHash = Animator.StringToHash("Speed");
        private readonly int attackHash = Animator.StringToHash("Light");
        private readonly int castHash = Animator.StringToHash("Heavy");
        private readonly int dodgeHash = Animator.StringToHash("Dodge");
        private readonly int hitHash = Animator.StringToHash("Hit");
        private readonly int dieHash = Animator.StringToHash("Victory");

        [SerializeField] private MeixiGame3D game;
        [SerializeField] private Transform player;
        [SerializeField] private CharacterController controller;
        [SerializeField] private Animator animator;
        private ProceduralHumanoidFallback proceduralPose;
        private bool animatorReady;
        private RuntimeAnimatorController boundAnimatorController;
        private Avatar boundAvatar;
        private float nextAnimatorRefresh;
        [SerializeField] private Renderer[] renderers;
        [SerializeField] private Material cyan;
        [SerializeField] private Material gold;
        private Vector3 arenaCenter;
        private double nextAttackBeat;
        private float telegraphLead;
        private bool active;
        private bool telegraphed;
        private bool resolved;
        private bool dying;
        private bool deathReported;
        private int patternIndex;
        private int health = MaxHealth;
        private float staggerUntil;
        private float counterUntil;
        private float evadeCooldown;
        private float deathAt;
        private float battleBpm = 92f;
        private int battlePhase = 1;
        private float verticalVelocity;
        private Vector3 movementVelocity;
        private Vector3 movementSmoothVelocity;
        private Vector3 pendingAttackPoint;
        private bool attackAimLocked;

        public int Health => health;
        public int HealthMaximum => MaxHealth;
        public bool CounterWindowOpen => Time.time <= counterUntil;

        public void Configure(MeixiGame3D owner, Transform target, CharacterController characterController, Animator modelAnimator, Renderer[] modelRenderers, Material cyanMaterial, Material goldMaterial)
        {
            game = owner;
            player = target;
            controller = characterController;
            animator = modelAnimator;
            proceduralPose = animator != null ? animator.GetComponent<ProceduralHumanoidFallback>() : null;
            renderers = modelRenderers;
            cyan = cyanMaterial;
            gold = goldMaterial;
            arenaCenter = transform.position;
            RefreshAnimatorBinding(true);
        }

        public void Rebind(MeixiGame3D owner, Transform target)
        {
            game = owner;
            player = target;
            controller = controller != null ? controller : GetComponent<CharacterController>();
            animator = animator != null ? animator : GetComponentInChildren<Animator>();
            proceduralPose = animator != null ? animator.GetComponent<ProceduralHumanoidFallback>() : null;
            renderers = renderers != null && renderers.Length > 0 ? renderers : GetComponentsInChildren<Renderer>(true);
            arenaCenter = transform.position;
            RefreshAnimatorBinding(true);
        }

        public void BeginBattle(double beatStart, float bpm)
        {
            gameObject.SetActive(true);
            // Authored enemies begin inactive. Refresh all references after activation
            // and explicitly restore renderers so a saved editor visibility state can
            // never produce an invisible combat encounter.
            controller = controller != null ? controller : GetComponent<CharacterController>();
            animator = animator != null ? animator : GetComponentInChildren<Animator>(true);
            renderers = GetComponentsInChildren<Renderer>(true);
            foreach (Renderer modelRenderer in renderers)
            {
                if (modelRenderer != null) modelRenderer.enabled = true;
            }
            if (!animatorReady) RefreshAnimatorBinding(true);
            if (player != null)
            {
                Vector3 separation = player.position - transform.position;
                separation.y = 0f;
                if (separation.magnitude > 7.2f)
                {
                    Vector3 playerForward = Vector3.ProjectOnPlane(player.forward, Vector3.up);
                    if (playerForward.sqrMagnitude < 0.01f) playerForward = Vector3.forward;
                    Vector3 corrected = player.position + playerForward.normalized * 4.8f;
                    corrected.y = transform.position.y;
                    bool controllerWasEnabled = controller != null && controller.enabled;
                    if (controller != null) controller.enabled = false;
                    transform.position = corrected;
                    if (controller != null) controller.enabled = controllerWasEnabled;
                    Debug.Log($"[MeixiRhythm] Creek spirit moved into visible duel range at {corrected}");
                }
            }
            arenaCenter = transform.position;
            active = true;
            dying = false;
            deathReported = false;
            health = MaxHealth;
            battleBpm = Mathf.Max(50f, bpm);
            battlePhase = 1;
            patternIndex = 0;
            counterUntil = -1f;
            evadeCooldown = 0f;
            double beatLength = 60d / battleBpm;
            telegraphLead = (float)(beatLength * 0.88d);
            nextAttackBeat = beatStart + beatLength * 4d;
            telegraphed = false;
            resolved = false;
            SetAnimatorFloat(speedHash, 0f);
            if (!animatorReady) proceduralPose?.SetLocomotion(0f, 0f);
            movementVelocity = Vector3.zero;
            movementSmoothVelocity = Vector3.zero;
            FacePlayer(1f);
            game.ReportCombatPhase(1);
            Debug.Log($"[MeixiRhythm] Battle start dsp={AudioSettings.dspTime:F3}, firstBeat={nextAttackBeat:F3}");
        }

        public void PrepareFinisherValidation(double beatStart)
        {
            health = 2;
            patternIndex = 2; // AttackPattern[2] = K 重击，可直接触发终结一击。
            nextAttackBeat = beatStart + 1.05d;
            telegraphed = false;
            resolved = false;
        }

        public void PrepareAttackValidation(double beatStart)
        {
            patternIndex = 1; // AttackPattern[1] = J 攻击一。
            nextAttackBeat = beatStart + 1.45d;
            telegraphed = false;
            resolved = false;
        }

        public void PrepareSpecificAttackValidation(int attackType, double beatStart)
        {
            int found = System.Array.IndexOf(AttackPattern, Mathf.Clamp(attackType, 0, 2));
            patternIndex = found >= 0 ? found : 0;
            nextAttackBeat = beatStart + 1.45d;
            telegraphed = false;
            resolved = false;
        }

        protected virtual void Update()
        {
            if (Time.unscaledTime >= nextAnimatorRefresh)
            {
                nextAnimatorRefresh = Time.unscaledTime + (animatorReady ? 0.50f : 0.12f);
                RefreshAnimatorBinding(false);
            }
            ApplyGravity();
            if (dying)
            {
                if (!deathReported && Time.time >= deathAt)
                {
                    deathReported = true;
                    game.ChapterOneEnemyDefeated(transform.position);
                }
                return;
            }
            if (!active || game == null || player == null || !game.ChapterOneCombatActive) return;

            evadeCooldown -= Time.deltaTime;
            FacePlayer(1f - Mathf.Exp(-10f * Time.deltaTime));
            double now = AudioSettings.dspTime;
            UpdateAttackAim(now);

            if (Time.time < staggerUntil)
            {
                SetAnimatorFloat(speedHash, 0f);
                return;
            }

            if (!telegraphed && now >= nextAttackBeat - telegraphLead)
            {
                telegraphed = true;
                BeginTelegraph();
            }
            // Resolve just after the musical beat so a Space press sampled later in the same rendered
            // frame can still invalidate the attack. This removes Update-order dependent hits.
            if (!resolved && now >= nextAttackBeat + AttackResolutionDelay)
            {
                resolved = true;
                ExecuteAttack();
            }
            if (now >= nextAttackBeat + 0.48d)
            {
                patternIndex++;
                double intervalBeats = battlePhase == 1 ? 2d : battlePhase == 2 ? 1.72d : 1.42d;
                double musicalNextBeat = nextAttackBeat + 60d / battleBpm * intervalBeats;
                // Never catch up an overdue attack in one frame. On a slow/loading frame the next
                // threat is moved forward so the player always receives the complete telegraph.
                double earliestFairBeat = now + telegraphLead + 0.08d;
                nextAttackBeat = System.Math.Max(musicalNextBeat, earliestFairBeat);
                telegraphed = false;
                resolved = false;
                attackAimLocked = false;
                Debug.Log($"[MeixiRhythm] Next fair threat beat={nextAttackBeat:F3} now={now:F3}");
            }

            if (!telegraphed)
            {
                Vector3 flat = player.position - transform.position;
                flat.y = 0f;
                float distance = flat.magnitude;
                Vector3 desired = Vector3.zero;
                if (distance > 4.1f) desired = flat.normalized;
                else if (distance < 2.45f) desired = -flat.normalized;
                else desired = transform.right * Mathf.Sin(Time.time * 1.7f + patternIndex) * 0.38f;
                float moveSpeed = battlePhase == 1 ? 1.75f : battlePhase == 2 ? 2.05f : 2.35f;
                Vector3 targetVelocity = desired * moveSpeed;
                movementVelocity = Vector3.SmoothDamp(movementVelocity, targetVelocity, ref movementSmoothVelocity, 0.12f, Mathf.Infinity, Time.deltaTime);
                MoveInsideArena(movementVelocity * Time.deltaTime);
                float locomotion = Mathf.Clamp01(movementVelocity.magnitude / moveSpeed);
                SetAnimatorFloat(speedHash, locomotion * 0.55f);
                if (!animatorReady) proceduralPose?.SetLocomotion(locomotion, Vector3.Dot(movementVelocity.normalized, transform.right));
            }
            else
            {
                movementVelocity = Vector3.SmoothDamp(movementVelocity, Vector3.zero, ref movementSmoothVelocity, 0.08f, Mathf.Infinity, Time.deltaTime);
                MoveInsideArena(movementVelocity * Time.deltaTime);
                SetAnimatorFloat(speedHash, 0f);
                if (!animatorReady) proceduralPose?.SetLocomotion(0f, 0f);
            }
        }

        private void BeginTelegraph()
        {
            int attackType = AttackPattern[patternIndex % AttackPattern.Length];
            pendingAttackPoint = player.position;
            attackAimLocked = false;
            Color color = attackType switch
            {
                0 => new Color(1f, 0.12f, 0.24f, 0.96f),
                1 => new Color(0.08f, 0.88f, 1f, 0.96f),
                _ => new Color(0.82f, 0.18f, 1f, 0.96f)
            };
            float castRadius = attackType == 0 ? 1.32f : attackType == 1 ? 1.50f : 1.20f;
            float visibleLead = telegraphLead + (float)AttackResolutionDelay;
            RhythmTelegraph3D.Create(transform, castRadius, visibleLead, color, attackType == 1);
            TrackingAttackTelegraph3D.Create(player, transform, attackType, visibleLead);
            EnemyMusicAttackVfx3D.Launch(game, (CreekRhythmEnemy3D)this, player, attackType,
                transform.position + transform.forward * 0.55f, nextAttackBeat + AttackResolutionDelay);
            game.PrepareCombatBeat(attackType, nextAttackBeat);
        }

        private void ExecuteAttack()
        {
            if (!attackAimLocked) pendingAttackPoint = player.position;
            Vector3 toPlayer = pendingAttackPoint - transform.position;
            toPlayer.y = 0f;
            Vector3 direction = toPlayer.sqrMagnitude > 0.01f ? toPlayer.normalized : transform.forward;
            transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
            int attackType = AttackPattern[patternIndex % AttackPattern.Length];
            if (attackType == 2)
            {
                SetAnimatorTrigger(castHash);
                if (!animatorReady) proceduralPose?.TriggerHeavy();
            }
            else
            {
                SetAnimatorTrigger(attackHash);
                if (!animatorReady) proceduralPose?.TriggerLight();
            }

            // DSP beat and damage resolve exactly when the modeled note reaches the locked aim point.
            // All three attack directions come from the player's tracked world position.
            game.ResolveEnemyAttack(attackType, transform.position + Vector3.up * 0.8f);
        }

        private void UpdateAttackAim(double now)
        {
            if (!telegraphed || resolved || player == null || attackAimLocked) return;
            pendingAttackPoint = player.position;
            float progress = 1f - Mathf.Clamp01((float)((nextAttackBeat - now) / Mathf.Max(0.05f, telegraphLead)));
            if (progress >= 0.80f) attackAimLocked = true;

            Vector3 direction = pendingAttackPoint - transform.position;
            direction.y = 0f;
            if (direction.sqrMagnitude > 0.01f)
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction, Vector3.up),
                    1f - Mathf.Exp(-18f * Time.deltaTime));
        }

        public void ResolveRhythmResponse(CombatBeatAction action, bool perfect)
        {
            if (!active || dying || Time.time < staggerUntil) return;
            counterUntil = -1f;

            if (action == CombatBeatAction.Dodge)
            {
                staggerUntil = Time.time + 0.22f;
                game.ReportPerfectDodge(Time.time + 0.30f);
                return;
            }
            ApplySpatialDamage(action == CombatBeatAction.AttackTwo ? 3 : 5, action, perfect);
        }

        public bool TryReceiveMeleeHit(Vector3 origin, Vector3 forward, bool onBeat)
        {
            if (!active || dying || Time.time < staggerUntil) return false;
            Vector3 delta = transform.position + Vector3.up * 0.85f - origin;
            Vector3 flatDelta = Vector3.ProjectOnPlane(delta, Vector3.up);
            // origin is now the hero's forward strike point, not the capsule centre.
            // A clear forward cone keeps the visual slash and damage result aligned.
            if (flatDelta.magnitude > 2.25f) return false;
            if (flatDelta.sqrMagnitude > 0.01f && Vector3.Dot(forward.normalized, flatDelta.normalized) < 0.12f) return false;
            return ApplySpatialDamage(onBeat ? 5 : 4, CombatBeatAction.AttackOne, onBeat);
        }

        public bool TryReceiveRangedHit(bool onBeat)
        {
            if (!active || dying || Time.time < staggerUntil) return false;
            return ApplySpatialDamage(onBeat ? 3 : 2, CombatBeatAction.AttackTwo, onBeat);
        }

        private bool ApplySpatialDamage(int damage, CombatBeatAction action, bool onBeat)
        {
            if (!active || dying || Time.time < staggerUntil) return false;
            health = Mathf.Max(0, health - Mathf.Max(1, damage));
            staggerUntil = Time.time + (action == CombatBeatAction.AttackOne ? 0.30f : 0.24f);
            counterUntil = -1f;
            SetAnimatorTrigger(hitHash);
            if (!animatorReady) proceduralPose?.TriggerHit();
            FlashHit(onBeat);
            game.ReportPlayerCombatHit(damage, onBeat, health, MaxHealth);

            int nextPhase = health <= 12 ? 3 : health <= 24 ? 2 : 1;
            if (nextPhase != battlePhase && health > 0)
            {
                battlePhase = nextPhase;
                telegraphLead = Mathf.Max(0.44f, telegraphLead - 0.08f);
                game.ReportCombatPhase(battlePhase);
            }

            if (health <= 0)
            {
                active = false;
                staggerUntil = float.PositiveInfinity;
                game.BeginFinalStrike(action, transform.position);
            }
            return true;
        }

        public void CompleteFinalStrike()
        {
            if (dying) return;
            Die();
        }

        public bool ReceivePlayerAttack(Vector3 origin, Vector3 forward, bool heavy, bool onBeat)
        {
            if (!active || dying || Time.time < staggerUntil) return false;
            Vector3 delta = transform.position + Vector3.up - origin;
            float distance = delta.magnitude;
            float range = heavy ? 4.1f : 3.35f;
            if (distance > range || Vector3.Dot(forward.normalized, delta.normalized) < 0.16f) return false;

            bool counter = CounterWindowOpen;
            if (!counter && !onBeat && evadeCooldown <= 0f)
            {
                evadeCooldown = 1.4f;
                SetAnimatorTrigger(dodgeHash);
                if (!animatorReady) proceduralPose?.TriggerDodge();
                float side = Vector3.Dot(transform.right, origin - transform.position) >= 0f ? -1f : 1f;
                MoveInsideArena(transform.right * side * 1.35f);
                RhythmTelegraph3D.Create(transform, 1.05f, 0.28f, new Color(0.12f, 0.82f, 1f, 0.85f), false);
                game.ReportEnemyDodge();
                return false;
            }

            int damage = counter ? (heavy ? 5 : 3) : onBeat ? (heavy ? 3 : 2) : 1;
            health = Mathf.Max(0, health - damage);
            counterUntil = -1f;
            staggerUntil = Time.time + (heavy ? 0.48f : 0.30f);
            SetAnimatorTrigger(hitHash);
            if (!animatorReady) proceduralPose?.TriggerHit();
            FlashHit(counter);
            game.ReportPlayerCombatHit(damage, counter, health, MaxHealth);

            if (health <= 0) Die();
            return true;
        }

        public void ResolveProjectileNearPlayer()
        {
            // 仅保留投射物视觉；第一章节拍判定由 MeixiGame3D 统一结算。
        }

        private void RegisterSuccessfulDodge()
        {
            counterUntil = Time.time + 0.72f;
            staggerUntil = Time.time + 0.34f;
            game.ReportPerfectDodge(counterUntil);
        }

        private void Die()
        {
            if (dying) return;
            dying = true;
            active = false;
            SetAnimatorFloat(speedHash, 0f);
            SetAnimatorTrigger(dieHash);
            if (!animatorReady) proceduralPose?.TriggerHit();
            if (controller != null) controller.enabled = false;
            RhythmTelegraph3D.Create(transform, 2.4f, 1.0f, new Color(0.20f, 0.92f, 1f, 0.95f), false);
            deathAt = Time.time + 1.05f;
        }

        private void FacePlayer(float amount)
        {
            Vector3 direction = player.position - transform.position;
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.01f) return;
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction, Vector3.up), amount);
        }

        private void MoveInsideArena(Vector3 displacement)
        {
            if (controller == null || !controller.enabled) return;
            Vector3 destination = transform.position + displacement;
            Vector3 offset = destination - arenaCenter;
            offset.y = 0f;
            if (offset.magnitude > 5.2f) displacement += (arenaCenter - destination).normalized * displacement.magnitude;
            controller.Move(displacement);
        }

        private void ApplyGravity()
        {
            if (controller == null || !controller.enabled) return;
            if (controller.isGrounded && verticalVelocity < 0f) verticalVelocity = -1.6f;
            else verticalVelocity = Mathf.Max(verticalVelocity + Physics.gravity.y * 2.0f * Time.deltaTime, -24f);
            controller.Move(Vector3.up * verticalVelocity * Time.deltaTime);
        }

        private void SpawnAttackArc(float radius, float degrees, Material source)
        {
            var root = new GameObject("溪灵三维攻击轨迹");
            root.transform.position = transform.position + Vector3.up * 0.14f;
            root.transform.rotation = transform.rotation;
            var line = root.AddComponent<LineRenderer>();
            int points = 32;
            line.positionCount = points;
            line.loop = false;
            line.useWorldSpace = false;
            line.widthMultiplier = 0.12f;
            line.material = new Material(source);
            line.startColor = new Color(1f, 0.82f, 0.35f, 0.92f);
            line.endColor = new Color(0.20f, 0.88f, 1f, 0.16f);
            for (int i = 0; i < points; i++)
            {
                float angle = Mathf.Lerp(-degrees * 0.5f, degrees * 0.5f, i / (points - 1f)) * Mathf.Deg2Rad;
                line.SetPosition(i, new Vector3(Mathf.Sin(angle) * radius, Mathf.Sin(i * 0.55f) * 0.08f, Mathf.Cos(angle) * radius));
            }
            root.AddComponent<LineFx3D>().Configure(0.34f, 0.75f);
        }

        private void FlashHit(bool counter)
        {
            if (renderers == null) return;
            Color flash = counter ? new Color(1f, 0.78f, 0.28f) : new Color(0.25f, 0.92f, 1f);
            foreach (Renderer renderer in renderers)
            {
                if (renderer == null) continue;
                foreach (Material material in renderer.materials)
                {
                    if (material.HasProperty("_EmissionColor"))
                    {
                        material.EnableKeyword("_EMISSION");
                        material.SetColor("_EmissionColor", flash * 1.7f);
                    }
                }
            }
        }

        private void SetAnimatorTrigger(int hash)
        {
            if (animator != null && animator.runtimeAnimatorController != null) animator.SetTrigger(hash);
        }

        private void SetAnimatorFloat(int hash, float value)
        {
            if (animator != null && animator.runtimeAnimatorController != null) animator.SetFloat(hash, value, 0.10f, Time.deltaTime);
        }

        private void RefreshAnimatorBinding(bool force)
        {
            if (animator != null)
            {
                RuntimeAnimatorController controllerAsset = animator.runtimeAnimatorController;
                if (controllerAsset == null)
                {
                    controllerAsset = Resources.Load<RuntimeAnimatorController>(AnimatorControllerResource);
                    if (controllerAsset != null) animator.runtimeAnimatorController = controllerAsset;
                }

                if (animator.avatar == null || !animator.avatar.isValid)
                {
                    GameObject modelAsset = Resources.Load<GameObject>(CharacterModelResource);
                    Animator sourceAnimator = modelAsset != null ? modelAsset.GetComponent<Animator>() : null;
                    if (sourceAnimator != null && sourceAnimator.avatar != null && sourceAnimator.avatar.isValid)
                        animator.avatar = sourceAnimator.avatar;
                }

                animator.applyRootMotion = false;
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                if (animator.runtimeAnimatorController != null && !animator.enabled) animator.enabled = true;
            }

            bool readyNow = animator != null && animator.enabled && animator.runtimeAnimatorController != null &&
                            animator.avatar != null && animator.avatar.isValid && animator.layerCount > 0;
            RuntimeAnimatorController currentController = readyNow ? animator.runtimeAnimatorController : null;
            Avatar currentAvatar = readyNow ? animator.avatar : null;
            bool bindingChanged = readyNow &&
                                  (currentController != boundAnimatorController || currentAvatar != boundAvatar);
            bool wasReady = animatorReady;

            if (proceduralPose != null) proceduralPose.enabled = !readyNow;
            animatorReady = readyNow;
            if (readyNow && (force || !wasReady || bindingChanged))
            {
                animator.Rebind();
                animator.Update(0f);
                animator.SetFloat(speedHash, 0f);
                Debug.Log($"[MeixiAnimation] Creek spirit animator bound on frame {Time.frameCount}.");
            }

            boundAnimatorController = currentController;
            boundAvatar = currentAvatar;
        }
    }

    public class RhythmTelegraph3DBase : MonoBehaviour
    {
        private LineRenderer line;
        private Transform glyph;
        private Material glyphMaterial;
        private float duration;
        private float age;
        private float startRadius;
        private bool directional;

        public static void Create(Transform follow, float radius, float lifetime, Color color, bool isDirectional)
        {
            var root = new GameObject(isDirectional ? "踏音突袭预警" : "节拍收缩环");
            root.transform.SetParent(follow, false);
            root.transform.localPosition = new Vector3(0f, 0.045f, 0f);
            var effect = root.AddComponent<RhythmTelegraph3D>();
            effect.Build(radius, lifetime, color, isDirectional);
        }

        private void Build(float radius, float lifetime, Color color, bool isDirectional)
        {
            duration = lifetime;
            startRadius = radius;
            directional = isDirectional;

            Texture2D sigilTexture = Resources.Load<Texture2D>("Textures/Generated/MeixiBeatSigil");
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default");
            var glyphObject = GameObject.CreatePrimitive(PrimitiveType.Quad);
            glyphObject.name = "梅溪魂音预警印记";
            glyphObject.transform.SetParent(transform, false);
            glyphObject.transform.localPosition = Vector3.zero;
            glyphObject.transform.localRotation = Quaternion.Euler(-90f, isDirectional ? 18f : 0f, 0f);
            Destroy(glyphObject.GetComponent<Collider>());
            glyph = glyphObject.transform;
            glyphMaterial = new Material(shader) { name = "魂音预警透明材质" };
            glyphMaterial.mainTexture = sigilTexture;
            if (glyphMaterial.HasProperty("_BaseMap")) glyphMaterial.SetTexture("_BaseMap", sigilTexture);
            if (glyphMaterial.HasProperty("_Surface")) glyphMaterial.SetFloat("_Surface", 1f);
            if (glyphMaterial.HasProperty("_SrcBlend")) glyphMaterial.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            if (glyphMaterial.HasProperty("_DstBlend")) glyphMaterial.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            if (glyphMaterial.HasProperty("_ZWrite")) glyphMaterial.SetFloat("_ZWrite", 0f);
            glyphMaterial.renderQueue = 3050;
            glyphMaterial.color = new Color(1f, 1f, 1f, 0.82f);
            glyphObject.GetComponent<Renderer>().material = glyphMaterial;

            if (directional)
            {
                line = gameObject.AddComponent<LineRenderer>();
                line.useWorldSpace = false;
                line.loop = false;
                line.widthMultiplier = 0.06f;
                line.positionCount = 7;
                line.material = new Material(shader);
                line.startColor = color;
                line.endColor = new Color(1f, 1f, 1f, 0.15f);
            }
            UpdateGeometry(1f);
        }

        protected virtual void Update()
        {
            age += Time.deltaTime;
            float t = Mathf.Clamp01(age / Mathf.Max(0.01f, duration));
            float scale = Mathf.Lerp(1.12f, 0.30f, t * t);
            UpdateGeometry(scale);
            if (glyph != null)
            {
                float size = startRadius * 2.15f * scale;
                glyph.localScale = Vector3.one * size;
                glyph.localRotation *= Quaternion.Euler(0f, 0f, (directional ? 22f : -14f) * Time.deltaTime);
            }
            if (glyphMaterial != null)
            {
                float alpha = Mathf.Lerp(0.44f, 1f, Mathf.SmoothStep(0f, 1f, t));
                Color tint = new Color(1f, 1f, 1f, alpha);
                glyphMaterial.color = tint;
                if (glyphMaterial.HasProperty("_BaseColor")) glyphMaterial.SetColor("_BaseColor", tint);
            }
            if (line != null)
            {
                Color lineColor = line.startColor;
                lineColor.a = Mathf.Lerp(0.58f, 0.96f, t);
                line.startColor = lineColor;
                line.widthMultiplier = Mathf.Lerp(0.05f, 0.13f, t);
            }
            if (age >= duration) Destroy(gameObject);
        }

        private void UpdateGeometry(float scale)
        {
            if (directional)
            {
                float length = startRadius * scale;
                if (line == null) return;
                line.SetPosition(0, new Vector3(-0.46f, 0.02f, 0.30f));
                line.SetPosition(1, new Vector3(-0.18f, 0.02f, length * 0.34f));
                line.SetPosition(2, new Vector3(-0.38f, 0.02f, length * 0.61f));
                line.SetPosition(3, new Vector3(0f, 0.02f, length + 0.48f));
                line.SetPosition(4, new Vector3(0.38f, 0.02f, length * 0.61f));
                line.SetPosition(5, new Vector3(0.18f, 0.02f, length * 0.34f));
                line.SetPosition(6, new Vector3(0.46f, 0.02f, 0.30f));
                return;
            }
        }
    }

    public class CreekWaveProjectile3DBase : MonoBehaviour
    {
        private MeixiGame3D game;
        private CreekRhythmEnemy3D owner;
        private Vector3 direction;
        private float age;
        private bool resolved;

        public static void Create(MeixiGame3D game, CreekRhythmEnemy3D owner, Vector3 position, Vector3 direction, Material source)
        {
            var root = new GameObject("溪流拍点波");
            root.transform.position = position;
            root.transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
            var line = root.AddComponent<LineRenderer>();
            line.useWorldSpace = false;
            line.loop = true;
            line.positionCount = 33;
            line.widthMultiplier = 0.09f;
            line.material = new Material(source);
            line.startColor = new Color(0.25f, 0.95f, 1f, 0.94f);
            line.endColor = new Color(1f, 0.72f, 0.28f, 0.70f);
            for (int i = 0; i < line.positionCount; i++)
            {
                float angle = i / (line.positionCount - 1f) * Mathf.PI * 2f;
                line.SetPosition(i, new Vector3(Mathf.Cos(angle) * 0.78f, Mathf.Sin(angle) * 0.35f, 0f));
            }
            root.AddComponent<CreekWaveProjectile3D>().Configure(game, owner, direction);
        }

        private void Configure(MeixiGame3D ownerGame, CreekRhythmEnemy3D enemy, Vector3 travelDirection)
        {
            game = ownerGame;
            owner = enemy;
            direction = travelDirection.normalized;
        }

        protected virtual void Update()
        {
            age += Time.deltaTime;
            transform.position += direction * (6.8f * Time.deltaTime);
            transform.Rotate(0f, 0f, 240f * Time.deltaTime, Space.Self);
            if (!resolved && Vector3.Distance(transform.position, game.PlayerPosition + Vector3.up) < 1.15f)
            {
                resolved = true;
                owner.ResolveProjectileNearPlayer();
                Destroy(gameObject);
            }
            if (age >= 2.3f) Destroy(gameObject);
        }
    }

    public class WristSamplerFx3DBase : MonoBehaviour
    {
        private Transform hand;
        private Transform core;
        private LineRenderer outerRing;

        public void Configure(Transform followHand, Material cyan, Material gold)
        {
            hand = followHand;
            transform.localScale = Vector3.one;

            outerRing = gameObject.AddComponent<LineRenderer>();
            outerRing.useWorldSpace = false;
            outerRing.loop = true;
            outerRing.positionCount = 32;
            outerRing.widthMultiplier = 0.012f;
            outerRing.material = new Material(cyan);
            outerRing.startColor = new Color(0.28f, 0.94f, 1f, 0.95f);
            outerRing.endColor = new Color(1f, 0.70f, 0.24f, 0.90f);
            for (int i = 0; i < outerRing.positionCount; i++)
            {
                float angle = i / (float)outerRing.positionCount * Mathf.PI * 2f;
                outerRing.SetPosition(i, new Vector3(Mathf.Cos(angle) * 0.075f, Mathf.Sin(angle) * 0.075f, 0f));
            }

            var coreObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            coreObject.name = "采集器声纹微光";
            coreObject.transform.SetParent(transform, false);
            coreObject.transform.localPosition = Vector3.forward * 0.018f;
            coreObject.transform.localScale = Vector3.one * 0.038f;
            coreObject.GetComponent<Renderer>().material = new Material(gold);
            Destroy(coreObject.GetComponent<Collider>());
            core = coreObject.transform;
        }

        protected virtual void LateUpdate()
        {
            if (hand == null)
            {
                Destroy(gameObject);
                return;
            }
            transform.position = hand.position + hand.right * 0.025f;
            transform.rotation = hand.rotation * Quaternion.Euler(0f, 90f, 0f);
            transform.localScale = Vector3.one;
            if (core != null)
            {
                float pulse = 0.034f + (Mathf.Sin(Time.unscaledTime * 7f) + 1f) * 0.004f;
                core.localScale = Vector3.one * pulse;
            }
        }
    }

    public class GeneratedVfxPlane3DBase : MonoBehaviour
    {
        private float size;
        private float roll;
        private float lifetime;
        private float age;
        private bool billboard;
        private Material material;
        private Color baseColor;
        private Quaternion baseRotation;

        public void Configure(float targetSize, float zRoll, float duration, bool faceCamera, Material sourceMaterial)
        {
            size = targetSize;
            roll = zRoll;
            lifetime = Mathf.Max(0.05f, duration);
            billboard = faceCamera;
            material = sourceMaterial;
            baseColor = sourceMaterial != null ? sourceMaterial.color : Color.white;
            baseRotation = transform.rotation;
            transform.localScale = Vector3.one * (size * 0.38f);
        }

        protected virtual void Update()
        {
            age += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(age / lifetime);
            float eased = 1f - Mathf.Pow(1f - t, 3f);
            transform.localScale = Vector3.one * Mathf.Lerp(size * 0.38f, size * 1.08f, eased);

            if (billboard && Camera.main != null)
            {
                Vector3 toCamera = Camera.main.transform.position - transform.position;
                if (toCamera.sqrMagnitude > 0.001f)
                    transform.rotation = Quaternion.LookRotation(toCamera.normalized, Camera.main.transform.up) * Quaternion.Euler(0f, 0f, roll);
            }
            else
            {
                transform.rotation = baseRotation * Quaternion.Euler(0f, 0f, roll + t * 26f);
            }

            if (material != null)
            {
                float alpha = baseColor.a * (1f - Mathf.SmoothStep(0.58f, 1f, t));
                Color color = new Color(baseColor.r, baseColor.g, baseColor.b, alpha);
                material.color = color;
                if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            }
            if (age >= lifetime) Destroy(gameObject);
        }
    }

    public class LineFx3DBase : MonoBehaviour
    {
        private float duration;
        private float growth;
        private float age;
        private LineRenderer line;

        public void Configure(float lifetime, float growthAmount)
        {
            duration = lifetime;
            growth = growthAmount;
            line = GetComponent<LineRenderer>();
        }

        protected virtual void Update()
        {
            age += Time.deltaTime;
            float t = Mathf.Clamp01(age / Mathf.Max(0.01f, duration));
            transform.localScale = Vector3.one * (1f + growth * t);
            if (line != null)
            {
                Color start = line.startColor;
                Color end = line.endColor;
                start.a *= 1f - t;
                end.a *= 1f - t;
                line.startColor = start;
                line.endColor = end;
            }
            if (age >= duration) Destroy(gameObject);
        }
    }

    public class FancyTextPulseBase : MonoBehaviour
    {
        public float Speed = 2f;
        public float Amount = 0.035f;
        public bool Fade;
        private Vector3 baseScale;
        private Graphic graphic;

        protected virtual void Awake()
        {
            baseScale = transform.localScale;
            graphic = GetComponent<Graphic>();
        }

        protected virtual void Update()
        {
            float wave = (Mathf.Sin(Time.unscaledTime * Speed) + 1f) * 0.5f;
            transform.localScale = baseScale * (1f + wave * Amount);
            if (Fade && graphic != null)
            {
                Color color = graphic.color;
                color.a = 0.65f + wave * 0.35f;
                graphic.color = color;
            }
        }
    }

    public class PulseFx3DBase : MonoBehaviour
    {
        public float Lifetime = 0.55f;
        public float Growth = 7f;
        private float age;
        private Vector3 startScale;
        private Renderer effectRenderer;

        protected virtual void Awake()
        {
            startScale = transform.localScale;
            effectRenderer = GetComponent<Renderer>();
        }

        protected virtual void Update()
        {
            age += Time.deltaTime;
            float t = Mathf.Clamp01(age / Lifetime);
            transform.localScale = startScale * (1f + t * Growth);
            if (effectRenderer != null)
            {
                Color color = effectRenderer.material.color;
                color.a = 1f - t;
                effectRenderer.material.color = color;
            }
            if (age >= Lifetime) Destroy(gameObject);
        }
    }

    public class BillboardLabelBase : MonoBehaviour
    {
        protected virtual void LateUpdate()
        {
            if (Camera.main == null) return;
            transform.rotation = Quaternion.LookRotation(transform.position - Camera.main.transform.position, Vector3.up);
        }
    }
}
