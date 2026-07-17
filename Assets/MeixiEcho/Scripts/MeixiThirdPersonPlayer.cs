using UnityEngine;
using UnityEngine.InputSystem;

namespace MeixiEcho
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class MeixiThirdPersonPlayer : MonoBehaviour
    {
        private const string AnimatorControllerResource = "Animations/LinMoCombat";
        private const string CharacterModelResource = "Models/Characters/LinMo";
        private CharacterController controller;
        private MeixiGame3D game;
        private Transform visual;
        private ThirdPersonOrbitCamera orbitCamera;
        private Animator animator;
        private ProceduralHumanoidFallback fallbackPose;
        private float verticalVelocity;
        private float dashTimer;
        private float dashCooldown;
        private float attackCooldown;
        private float actionLockTimer;
        private bool finisherAnimationActive;
        private bool finisherProceduralActive;
        private bool finisherAnimatorWasEnabled;
        private AnimatorUpdateMode finisherPreviousUpdateMode;
        private float meleeLungeTimer;
        private float meleeImpactTimer;
        private bool meleeAttackArmed;
        private bool meleeImpactResolved;
        private bool meleeLungeLogged;
        private float footstepTimer;
        private bool animationPreview;
        private float animationPreviewStarted;
        private bool previewLightTriggered;
        private bool previewHeavyTriggered;
        private Vector3 lastMove = Vector3.forward;
        private Vector3 dashDirection = Vector3.forward;
        private Vector3 meleeDirection = Vector3.forward;
        private Vector3 horizontalVelocity;
        private Vector3 horizontalSmoothVelocity;
        private bool wasDashing;
        private bool animatorReady;
        private RuntimeAnimatorController boundAnimatorController;
        private Avatar boundAvatar;
        private float nextAnimatorRefresh;
        private Vector3 visualBasePosition;
        private Quaternion visualBaseRotation;
        private bool initialGroundSnapPending;
        private float initialGroundSnapDeadline;
        private float nextInitialGroundProbe;
        private readonly RaycastHit[] groundHits = new RaycastHit[12];
        private readonly int speedHash = Animator.StringToHash("Speed");
        private readonly int groundedHash = Animator.StringToHash("Grounded");
        private readonly int lightHash = Animator.StringToHash("Light");
        private readonly int heavyHash = Animator.StringToHash("Heavy");
        private readonly int dodgeHash = Animator.StringToHash("Dodge");
        private readonly int hitHash = Animator.StringToHash("Hit");
        private readonly int victoryHash = Animator.StringToHash("Victory");

        public Vector3 Forward => lastMove.sqrMagnitude > 0.01f ? lastMove.normalized : transform.forward;
        public bool IsInvulnerable => dashTimer > 0.015f;

        public void ResetForNewSession()
        {
            CompleteFinalStrikeAnimation();
            verticalVelocity = -1.8f;
            dashTimer = 0f;
            dashCooldown = 0f;
            attackCooldown = 0f;
            actionLockTimer = 0f;
            meleeLungeTimer = 0f;
            meleeAttackArmed = false;
            meleeImpactResolved = false;
            horizontalVelocity = Vector3.zero;
            horizontalSmoothVelocity = Vector3.zero;
            lastMove = transform.forward.sqrMagnitude > 0.01f ? transform.forward : Vector3.forward;
            dashDirection = lastMove;
            meleeDirection = lastMove;
            wasDashing = false;
            animationPreview = false;
            initialGroundSnapPending = true;
            initialGroundSnapDeadline = Time.unscaledTime + 1.5f;
            nextInitialGroundProbe = Time.unscaledTime;
            if (fallbackPose != null) fallbackPose.SetLocomotion(0f, 0f);
            SetAnimatorFloat(speedHash, 0f);
            SetAnimatorBool(groundedHash, IsGroundedStable());
        }

        public void ResetInputTransientState()
        {
            dashTimer = 0f;
            actionLockTimer = 0f;
            meleeLungeTimer = 0f;
            meleeAttackArmed = false;
            meleeImpactResolved = true;
            horizontalVelocity = Vector3.zero;
            horizontalSmoothVelocity = Vector3.zero;
            wasDashing = false;
        }

        public void Configure(MeixiGame3D owner, ThirdPersonOrbitCamera cameraRig, Transform model)
        {
            game = owner;
            orbitCamera = cameraRig;
            visual = model;
            visualBasePosition = visual != null ? visual.localPosition : Vector3.zero;
            visualBaseRotation = visual != null ? visual.localRotation : Quaternion.identity;
            controller = GetComponent<CharacterController>();
            animator = visual != null ? visual.GetComponentInChildren<Animator>() : null;
            fallbackPose = visual != null ? visual.GetComponent<ProceduralHumanoidFallback>() : null;
            RefreshAnimatorBinding(true);
            initialGroundSnapPending = !SnapToGroundImmediately(1.6f);
            initialGroundSnapDeadline = Time.unscaledTime + 1.5f;
            nextInitialGroundProbe = Time.unscaledTime;
        }

        private void Update()
        {
            if (Time.unscaledTime >= nextAnimatorRefresh)
            {
                nextAnimatorRefresh = Time.unscaledTime + (animatorReady ? 0.50f : 0.12f);
                RefreshAnimatorBinding(false);
            }
            if (game == null || controller == null) return;

            // Authored scenery and physics colliders may finish binding one or two
            // frames after the player. Retry briefly so the first launch is grounded
            // just as reliably as later scene reloads.
            if (initialGroundSnapPending && Time.unscaledTime >= nextInitialGroundProbe)
            {
                nextInitialGroundProbe = Time.unscaledTime + 0.06f;
                initialGroundSnapPending = !SnapToGroundImmediately(1.6f) && Time.unscaledTime < initialGroundSnapDeadline;
            }

            dashTimer -= Time.deltaTime;
            dashCooldown -= Time.deltaTime;
            attackCooldown -= Time.deltaTime;
            actionLockTimer -= Time.deltaTime;
            meleeLungeTimer -= Time.deltaTime;
            if (meleeAttackArmed && !meleeImpactResolved)
            {
                meleeImpactTimer -= Time.deltaTime;
                if (meleeImpactTimer <= 0f)
                {
                    meleeImpactResolved = true;
                    Vector3 toTarget = game.CombatTargetPosition - transform.position;
                    toTarget.y = 0f;
                    if (controller.enabled && toTarget.sqrMagnitude > 0.01f)
                    {
                        // Only a small collision-aware correction is allowed here.
                        // The old multi-metre correction could place the hero behind
                        // the enemy before the hit was resolved on a slow frame.
                        float remainingAdvance = Mathf.Clamp(toTarget.magnitude - 2.20f, 0f, 0.45f);
                        controller.Move(toTarget.normalized * remainingAdvance);
                        FaceDirection(toTarget, true);
                    }
                    meleeLungeTimer = 0f;
                    horizontalVelocity = Vector3.zero;
                    horizontalSmoothVelocity = Vector3.zero;
                    Debug.Log($"[MeixiSpatial] Melee impact player={transform.position} target={game.CombatTargetPosition}");
                    Vector3 strikeOrigin = transform.position + transform.forward * 0.72f + Vector3.up * 0.92f;
                    game.ResolvePlayerMelee(strikeOrigin, transform.forward);
                    meleeAttackArmed = false;
                }
            }

            if (animationPreview)
            {
                UpdateAnimationPreview();
                return;
            }

            if (!game.CanPlayerMove)
            {
                horizontalVelocity = Vector3.SmoothDamp(horizontalVelocity, Vector3.zero, ref horizontalSmoothVelocity, 0.08f, Mathf.Infinity, Time.deltaTime);
                if (IsGroundedStable()) verticalVelocity = -1.6f;
                else verticalVelocity = Mathf.Max(verticalVelocity + Physics.gravity.y * 2.25f * Time.deltaTime, -24f);
                controller.Move(Vector3.up * verticalVelocity * Time.deltaTime);
                StickToGround();
                SetAnimatorFloat(speedHash, 0f);
                if (!animatorReady) fallbackPose?.SetLocomotion(0f, 0f);
                UpdateVisualMotion(0f, 0f);
                return;
            }

            var keyboard = Keyboard.current;
            if (keyboard == null)
            {
                horizontalVelocity = Vector3.SmoothDamp(horizontalVelocity, Vector3.zero, ref horizontalSmoothVelocity, 0.08f, Mathf.Infinity, Time.deltaTime);
                if (IsGroundedStable()) verticalVelocity = -1.6f;
                else verticalVelocity = Mathf.Max(verticalVelocity + Physics.gravity.y * 2.25f * Time.deltaTime, -24f);
                controller.Move(Vector3.up * verticalVelocity * Time.deltaTime);
                StickToGround();
                return;
            }

            float x = 0f;
            float z = 0f;
            if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) x -= 1f;
            if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) x += 1f;
            if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) z -= 1f;
            if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) z += 1f;

            Vector3 input = Vector3.ClampMagnitude(new Vector3(x, 0f, z), 1f);
            Vector3 forward = orbitCamera != null ? orbitCamera.FlatForward : Vector3.forward;
            Vector3 right = orbitCamera != null ? orbitCamera.FlatRight : Vector3.right;
            Vector3 requestedMove = Vector3.ClampMagnitude(forward * input.z + right * input.x, 1f);
            Vector3 move = actionLockTimer > 0f ? Vector3.zero : requestedMove;

            if (requestedMove.sqrMagnitude > 0.02f && actionLockTimer <= 0f)
            {
                lastMove = requestedMove;
                FaceDirection(requestedMove, false);
                footstepTimer -= Time.deltaTime;
                if (IsGroundedStable() && footstepTimer <= 0f)
                {
                    footstepTimer = dashTimer > 0f ? 0.14f : 0.34f;
                    game.SpawnFootstep(transform.position);
                }
            }

            if (IsGroundedStable())
            {
                verticalVelocity = -1.6f;
                if (keyboard.spaceKey.wasPressedThisFrame && !game.ChapterOneCombatActive)
                {
                    verticalVelocity = 8.4f;
                    game.PlayJumpSound();
                }
            }
            else
            {
                verticalVelocity += Physics.gravity.y * 2.25f * Time.deltaTime;
            }

            bool dodgePressed = game.ChapterOneCombatActive
                ? keyboard.spaceKey.wasPressedThisFrame
                : keyboard.leftShiftKey.wasPressedThisFrame || keyboard.rightShiftKey.wasPressedThisFrame || keyboard.qKey.wasPressedThisFrame;
            if (dodgePressed && dashCooldown <= 0f)
            {
                if (game.ChapterOneCombatActive)
                {
                    Vector3 origin = transform.position;
                    Vector3 preferred = move.sqrMagnitude > 0.05f ? move.normalized : Vector3.zero;
                    Vector3 destination = game.SelectBlinkDestination(origin, preferred);
                    Vector3 displacement = destination - origin;
                    controller.Move(displacement);
                    dashDirection = Vector3.zero;
                    lastMove = displacement.sqrMagnitude > 0.01f ? displacement.normalized : Forward;
                    dashTimer = 0.20f;
                    dashCooldown = 0.58f;
                    actionLockTimer = 0.24f;
                    horizontalVelocity = Vector3.zero;
                    horizontalSmoothVelocity = Vector3.zero;
                    SetAnimatorTrigger(dodgeHash);
                    if (!animatorReady) fallbackPose?.TriggerDodge();
                    game.ResolveBlinkDodge(origin, transform.position);
                }
                else
                {
                    dashDirection = move.sqrMagnitude > 0.05f ? move.normalized : Forward;
                    lastMove = dashDirection;
                    dashTimer = 0.34f;
                    dashCooldown = 0.62f;
                    actionLockTimer = 0.28f;
                    SetAnimatorTrigger(dodgeHash);
                    if (!animatorReady) fallbackPose?.TriggerDodge();
                    game.ShowToast("踏 音 · 闪 避", new Color(0.36f, 0.90f, 1f));
                    game.SpawnDashTrail(transform.position, dashDirection);
                }
            }

            bool dashing = dashTimer > 0f;
            Vector3 targetHorizontal = move * 5.2f;
            if (dashing)
            {
                horizontalVelocity = dashDirection * 12.8f;
                horizontalSmoothVelocity = Vector3.zero;
            }
            else if (meleeLungeTimer > 0f)
            {
                Vector3 targetDelta = game.CombatTargetPosition - transform.position;
                targetDelta.y = 0f;
                if (targetDelta.sqrMagnitude > 0.01f)
                {
                    meleeDirection = targetDelta.normalized;
                    FaceDirection(meleeDirection, true);
                }
                float stoppingDistance = meleeAttackArmed ? 1.78f : 2.55f;
                float remaining = Mathf.Max(0f, targetDelta.magnitude - stoppingDistance);
                float desiredSpeed = meleeAttackArmed ? 10.8f : 8.0f;
                horizontalVelocity = meleeDirection * Mathf.Min(desiredSpeed,
                    remaining / Mathf.Max(Time.deltaTime, 0.001f));
                horizontalSmoothVelocity = Vector3.zero;
                if (!meleeLungeLogged)
                {
                    meleeLungeLogged = true;
                    Debug.Log($"[MeixiSpatial] Lunge start position={transform.position} direction={meleeDirection} controller={controller.enabled}");
                }
            }
            else
            {
                if (wasDashing) horizontalVelocity = targetHorizontal;
                bool reversing = horizontalVelocity.sqrMagnitude > 0.05f && targetHorizontal.sqrMagnitude > 0.05f &&
                                 Vector3.Dot(horizontalVelocity, targetHorizontal) < 0f;
                float smoothTime = reversing ? 0.038f : targetHorizontal.sqrMagnitude > 0.01f ? 0.060f : 0.085f;
                horizontalVelocity = Vector3.SmoothDamp(horizontalVelocity, targetHorizontal, ref horizontalSmoothVelocity, smoothTime, Mathf.Infinity, Time.deltaTime);
            }
            wasDashing = dashing;
            float movementAmount = Mathf.Clamp01(horizontalVelocity.magnitude / 5.2f);
            UpdateVisualMotion(movementAmount, x);
            SetAnimatorFloat(speedHash, dashing ? 1f : movementAmount * 0.62f);
            SetAnimatorBool(groundedHash, IsGroundedStable());
            if (!animatorReady) fallbackPose?.SetLocomotion(movementAmount, x);
            controller.Move((horizontalVelocity + Vector3.up * verticalVelocity) * Time.deltaTime);
            // CharacterController.Move already applies the downward grounding
            // velocity. A second ground move every frame caused visible micro-stops.
            if (verticalVelocity <= 0f && !controller.isGrounded) StickToGround();

            Mouse mouse = Mouse.current;
            bool lightStrike = keyboard.jKey.wasPressedThisFrame || (mouse != null && mouse.leftButton.wasPressedThisFrame);
            bool heavyStrike = keyboard.kKey.wasPressedThisFrame || (mouse != null && mouse.rightButton.wasPressedThisFrame);
            if (lightStrike && attackCooldown <= 0f)
            {
                if (game.ChapterOneCombatActive)
                {
                    if (game.TryPunishForbiddenMelee(transform.position))
                    {
                        meleeAttackArmed = false;
                        meleeImpactResolved = true;
                        meleeLungeTimer = 0f;
                        attackCooldown = 0.58f;
                        actionLockTimer = 0.38f;
                        horizontalVelocity = Vector3.zero;
                        horizontalSmoothVelocity = Vector3.zero;
                        return;
                    }
                    meleeAttackArmed = game.TryStartMeleeLunge(transform.position, out meleeDirection);
                    meleeLungeLogged = false;
                    meleeImpactResolved = !meleeAttackArmed;
                    meleeImpactTimer = 0.20f;
                    meleeLungeTimer = meleeAttackArmed ? 0.25f : 0.18f;
                    if (meleeDirection.sqrMagnitude > 0.01f)
                    {
                        FaceDirection(meleeDirection, true);
                        lastMove = transform.forward;
                    }
                    attackCooldown = 0.44f;
                    actionLockTimer = 0.30f;
                    if (meleeAttackArmed)
                    {
                        SetAnimatorTrigger(lightHash);
                        if (!animatorReady) fallbackPose?.TriggerLight();
                    }
                    return;
                }
                attackCooldown = 0.28f;
                actionLockTimer = 0.34f;
                SetAnimatorTrigger(lightHash);
                if (!animatorReady) fallbackPose?.TriggerLight();
                game.PerformSoundPulse(transform.position + Vector3.up * 0.9f, Forward, false);
            }
            else if (heavyStrike && attackCooldown <= 0f)
            {
                attackCooldown = 0.62f;
                actionLockTimer = 0.46f;
                if (game.ChapterOneCombatActive)
                {
                    Vector3 targetDirection = game.CombatTargetPosition - transform.position;
                    targetDirection.y = 0f;
                    if (targetDirection.sqrMagnitude > 0.01f)
                    {
                        FaceDirection(targetDirection, true);
                        lastMove = transform.forward;
                    }
                    SetAnimatorTrigger(heavyHash);
                    if (!animatorReady) fallbackPose?.TriggerHeavy();
                    game.FirePlayerNoteWave(transform.position + Vector3.up * 1.05f + transform.forward * 0.72f, transform.forward);
                    return;
                }
                SetAnimatorTrigger(heavyHash);
                if (!animatorReady) fallbackPose?.TriggerHeavy();
                game.PerformSoundPulse(transform.position + Vector3.up * 0.9f, Forward, true);
            }

            if (transform.position.y < -6f) game.RespawnPlayer();
        }

        public void BeginAnimationPreview()
        {
            animationPreview = true;
            animationPreviewStarted = Time.time;
            previewLightTriggered = false;
            previewHeavyTriggered = false;
            verticalVelocity = -1.6f;
            transform.forward = new Vector3(0.72f, 0f, 0.69f).normalized;
        }

        private void UpdateAnimationPreview()
        {
            float elapsed = Time.time - animationPreviewStarted;
            bool grounded = IsGroundedStable();
            verticalVelocity = grounded ? -1.6f : Mathf.Max(verticalVelocity + Physics.gravity.y * 2.25f * Time.deltaTime, -24f);

            if (elapsed < 0.88f)
            {
                // 自动走两步，确保迈腿、屈膝、摆臂和重心起伏能被独立验收。
                Vector3 previewMove = transform.forward * 1.45f;
                lastMove = transform.forward;
                controller.Move((previewMove + Vector3.up * verticalVelocity) * Time.deltaTime);
                StickToGround();
                SetAnimatorFloat(speedHash, 0.58f);
                SetAnimatorBool(groundedHash, grounded);
                if (!animatorReady) fallbackPose?.SetLocomotion(0.92f, 0f);
                UpdateVisualMotion(0.92f, 0f);
                return;
            }

            controller.Move(Vector3.up * verticalVelocity * Time.deltaTime);
            StickToGround();
            SetAnimatorFloat(speedHash, 0f);
            SetAnimatorBool(groundedHash, grounded);
            if (!animatorReady) fallbackPose?.SetLocomotion(0f, 0f);
            UpdateVisualMotion(0f, 0f);

            if (!previewLightTriggered && elapsed >= 0.98f)
            {
                previewLightTriggered = true;
                SetAnimatorTrigger(lightHash);
                if (!animatorReady) fallbackPose?.TriggerLight();
            }

            if (!previewHeavyTriggered && elapsed >= 1.48f)
            {
                previewHeavyTriggered = true;
                SetAnimatorTrigger(heavyHash);
                if (!animatorReady) fallbackPose?.TriggerHeavy();
            }
        }

        private bool IsGroundedStable()
        {
            if (controller == null || !controller.enabled) return false;
            if (controller.isGrounded) return true;
            return verticalVelocity <= 0.10f && TryGetGround(out _, out float gap) && gap >= -0.08f && gap <= 0.17f;
        }

        private void StickToGround()
        {
            if (controller == null || !controller.enabled || verticalVelocity > 0.10f) return;
            if (!TryGetGround(out _, out float gap)) return;
            if (gap > 0.012f && gap <= 0.38f)
            {
                controller.Move(Vector3.down * Mathf.Min(gap - 0.008f, 0.18f));
                verticalVelocity = -1.6f;
            }
        }

        private bool SnapToGroundImmediately(float maxDistance)
        {
            if (controller == null || !controller.enabled) return false;
            if (!TryGetGround(out RaycastHit hit, out float gap, maxDistance)) return false;
            if (gap < -0.12f || gap > maxDistance) return false;

            bool wasEnabled = controller.enabled;
            controller.enabled = false;
            Vector3 groundedPosition = transform.position;
            float bottomOffset = controller.center.y - controller.height * 0.5f;
            groundedPosition.y = hit.point.y - bottomOffset + 0.012f;
            transform.position = groundedPosition;
            controller.enabled = wasEnabled;
            verticalVelocity = -1.6f;
            Debug.Log($"[MeixiGround] Initial ground snap gap={gap:F3} position={transform.position}");
            return true;
        }

        private bool TryGetGround(out RaycastHit bestHit, out float gap, float maxDistance = 0.55f)
        {
            bestHit = default;
            gap = float.PositiveInfinity;
            if (controller == null) return false;

            Vector3 origin = transform.position + Vector3.up * 0.62f;
            int count = Physics.RaycastNonAlloc(origin, Vector3.down, groundHits, maxDistance + 0.75f, ~0, QueryTriggerInteraction.Ignore);
            bool found = false;
            float highestGround = float.NegativeInfinity;
            for (int i = 0; i < count; i++)
            {
                Collider hitCollider = groundHits[i].collider;
                if (hitCollider == null) continue;
                Transform hitTransform = hitCollider.transform;
                if (hitTransform == transform || hitTransform.IsChildOf(transform)) continue;
                if (Vector3.Dot(groundHits[i].normal, Vector3.up) < 0.46f) continue;
                if (groundHits[i].point.y <= highestGround) continue;
                highestGround = groundHits[i].point.y;
                bestHit = groundHits[i];
                found = true;
            }
            if (!found) return false;

            float controllerBottom = transform.position.y + controller.center.y - controller.height * 0.5f;
            gap = controllerBottom - bestHit.point.y;
            return true;
        }

        private void UpdateVisualMotion(float movement, float sideInput)
        {
            if (visual == null) return;
            if (animator != null && animator.enabled && animator.runtimeAnimatorController != null)
            {
                visual.localPosition = Vector3.Lerp(visual.localPosition, visualBasePosition, 1f - Mathf.Exp(-18f * Time.unscaledDeltaTime));
                visual.localRotation = Quaternion.Slerp(visual.localRotation, visualBaseRotation, 1f - Mathf.Exp(-18f * Time.unscaledDeltaTime));
                return;
            }
            float amount = Mathf.Clamp01(movement);
            float bob = Mathf.Sin(Time.time * 10f) * 0.035f * amount;
            Vector3 targetPosition = visualBasePosition + Vector3.up * bob;
            visual.localPosition = Vector3.Lerp(visual.localPosition, targetPosition, 1f - Mathf.Exp(-16f * Time.unscaledDeltaTime));
            Quaternion targetRotation = visualBaseRotation * Quaternion.Euler(0f, 0f, -sideInput * 2.4f * amount);
            visual.localRotation = Quaternion.Slerp(visual.localRotation, targetRotation, 1f - Mathf.Exp(-12f * Time.unscaledDeltaTime));
        }

        private void FaceDirection(Vector3 direction, bool immediate)
        {
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.0004f) return;
            Quaternion desired = Quaternion.LookRotation(direction.normalized, Vector3.up);
            transform.rotation = immediate
                ? desired
                : Quaternion.RotateTowards(transform.rotation, desired, 720f * Mathf.Max(Time.deltaTime, 0.001f));
        }

        public void TriggerHitReaction(Vector3 source)
        {
            SetAnimatorTrigger(hitHash);
            if (!animatorReady) fallbackPose?.TriggerHit();
            Vector3 away = transform.position - source;
            away.y = 0f;
            if (controller != null && controller.enabled && away.sqrMagnitude > 0.01f) controller.Move(away.normalized * 0.22f);
        }

        public void TriggerVictory()
        {
            SetAnimatorTrigger(victoryHash);
        }

        public void TriggerFinalStrike(CombatBeatAction action)
        {
            actionLockTimer = 2.10f;
            horizontalVelocity = Vector3.zero;
            horizontalSmoothVelocity = Vector3.zero;
            Vector3 targetDirection = game.CombatTargetPosition - transform.position;
            targetDirection.y = 0f;
            if (targetDirection.sqrMagnitude > 0.01f)
            {
                FaceDirection(targetDirection, true);
                lastMove = transform.forward;
                // Pull the hero into a readable cinematic distance before the final pose.
                // CharacterController.Move keeps the lunge collision-aware and prevents
                // the close-up camera from framing only the enemy/effect ribbon.
                float targetDistance = targetDirection.magnitude;
                if (controller != null && controller.enabled && targetDistance > 2.55f)
                {
                    float advance = Mathf.Min(targetDistance - 2.35f, 1.85f);
                    controller.Move(targetDirection.normalized * advance);
                }
            }
            bool heavy = action == CombatBeatAction.AttackTwo;
            if (fallbackPose != null)
            {
                finisherProceduralActive = true;
                finisherAnimatorWasEnabled = animator != null && animator.enabled;
                if (animator != null) animator.enabled = false;
                animatorReady = false;
                fallbackPose.enabled = true;
                fallbackPose.TriggerFinisher(heavy);
            }
            else if (animator != null && animator.enabled)
            {
                finisherPreviousUpdateMode = animator.updateMode;
                animator.updateMode = AnimatorUpdateMode.UnscaledTime;
                finisherAnimationActive = true;
            }
            if (!finisherProceduralActive) SetAnimatorTrigger(heavy ? heavyHash : lightHash);
        }

        public void TriggerFinalStrikeImpact(CombatBeatAction action)
        {
            bool heavy = action == CombatBeatAction.AttackTwo;
            if (finisherProceduralActive) fallbackPose?.TriggerFinisher(heavy);
            else SetAnimatorTrigger(heavy ? heavyHash : lightHash);
        }

        public void CompleteFinalStrikeAnimation()
        {
            if (finisherProceduralActive)
            {
                finisherProceduralActive = false;
                if (fallbackPose != null) fallbackPose.enabled = false;
                if (animator != null) animator.enabled = finisherAnimatorWasEnabled;
                RefreshAnimatorBinding(true);
                return;
            }
            if (!finisherAnimationActive || animator == null) return;
            animator.updateMode = finisherPreviousUpdateMode;
            finisherAnimationActive = false;
        }

        public void TriggerCombatActionForValidation(CombatBeatAction action)
        {
            Vector3 targetDirection = game.CombatTargetPosition - transform.position;
            targetDirection.y = 0f;
            if (targetDirection.sqrMagnitude > 0.01f)
            {
                FaceDirection(targetDirection, true);
                lastMove = transform.forward;
            }

            if (action == CombatBeatAction.Dodge)
            {
                Vector3 origin = transform.position;
                Vector3 destination = game.SelectBlinkDestination(origin, -transform.forward);
                controller.Move(destination - origin);
                actionLockTimer = 0.24f;
                dashDirection = Vector3.zero;
                dashTimer = 0.20f;
                SetAnimatorTrigger(dodgeHash);
                if (!animatorReady) fallbackPose?.TriggerDodge();
                game.ResolveBlinkDodge(origin, transform.position);
                return;
            }

            if (action == CombatBeatAction.AttackTwo)
            {
                actionLockTimer = 0.46f;
                SetAnimatorTrigger(heavyHash);
                if (!animatorReady) fallbackPose?.TriggerHeavy();
                game.FirePlayerNoteWave(transform.position + Vector3.up * 1.05f + transform.forward * 0.72f, transform.forward);
                return;
            }

            if (game.TryPunishForbiddenMelee(transform.position))
            {
                meleeAttackArmed = false;
                meleeImpactResolved = true;
                meleeLungeTimer = 0f;
                actionLockTimer = 0.38f;
                return;
            }
            meleeAttackArmed = game.TryStartMeleeLunge(transform.position, out meleeDirection);
            meleeLungeLogged = false;
            meleeImpactResolved = !meleeAttackArmed;
            meleeImpactTimer = 0.20f;
            meleeLungeTimer = meleeAttackArmed ? 0.25f : 0.18f;
            if (meleeAttackArmed)
            {
                SetAnimatorTrigger(lightHash);
                if (!animatorReady) fallbackPose?.TriggerLight();
            }
        }

        private void SetAnimatorTrigger(int hash)
        {
            if (animator != null && animator.enabled && animator.runtimeAnimatorController != null) animator.SetTrigger(hash);
        }

        private void SetAnimatorFloat(int hash, float value)
        {
            if (animator != null && animator.enabled && animator.runtimeAnimatorController != null) animator.SetFloat(hash, value, 0.10f, Time.deltaTime);
        }

        private void SetAnimatorBool(int hash, bool value)
        {
            if (animator != null && animator.enabled && animator.runtimeAnimatorController != null) animator.SetBool(hash, value);
        }

        private void RefreshAnimatorBinding(bool force)
        {
            if (finisherProceduralActive) return;
            if (visual == null) return;

            Animator candidate = visual.GetComponentInChildren<Animator>(true);
            if (candidate != null) animator = candidate;
            if (fallbackPose == null) fallbackPose = visual.GetComponent<ProceduralHumanoidFallback>();

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
                // Standard Assets and KayKit use different Humanoid bind-pose conventions.
                // Keep the more detailed Standard Assets body on the procedural motion layer
                // so the first frame, walking and combat never fall back to a T-pose.
                bool useProceduralBody = animator.avatar != null &&
                                         animator.avatar.name.IndexOf("DefaultMale", System.StringComparison.OrdinalIgnoreCase) >= 0;
                if (useProceduralBody) animator.enabled = false;
                else if (animator.runtimeAnimatorController != null && !animator.enabled) animator.enabled = true;
            }

            bool readyNow = animator != null && animator.enabled && animator.runtimeAnimatorController != null &&
                            animator.avatar != null && animator.avatar.isValid && animator.layerCount > 0;
            RuntimeAnimatorController currentController = readyNow ? animator.runtimeAnimatorController : null;
            Avatar currentAvatar = readyNow ? animator.avatar : null;
            bool bindingChanged = readyNow &&
                                  (currentController != boundAnimatorController || currentAvatar != boundAvatar);
            bool wasReady = animatorReady;

            // Animator and the procedural fallback both write humanoid bones, so exactly one may run.
            // Disable the fallback before Rebind so any temporary pose is cleared by the controller.
            if (fallbackPose != null) fallbackPose.enabled = !readyNow;
            animatorReady = readyNow;

            if (readyNow && (force || !wasReady || bindingChanged))
            {
                animator.Rebind();
                animator.Update(0f);
                animator.SetFloat(speedHash, 0f);
                animator.SetBool(groundedHash, controller == null || controller.isGrounded);
                Debug.Log($"[MeixiAnimation] Player animator bound on frame {Time.frameCount}; " +
                          $"controller={currentController.name}, avatar={currentAvatar.name}");
            }
            else if (!readyNow && wasReady)
            {
                Debug.LogWarning("[MeixiAnimation] Player animator asset changed during startup; using the procedural pose until automatic rebind succeeds.");
            }

            boundAnimatorController = currentController;
            boundAvatar = currentAvatar;
        }
    }

    // Implementation base only. The concrete MonoBehaviour lives in the matching
    // ProceduralHumanoidFallback.cs asset so Unity no longer resolves these saved
    // components as MeixiThirdPersonPlayer.
    public class ProceduralHumanoidFallbackBase : MonoBehaviour
    {
        private Transform hips;
        private Transform spine;
        private Transform leftArm;
        private Transform leftForearm;
        private Transform rightArm;
        private Transform rightForearm;
        private Transform leftHand;
        private Transform rightHand;
        private Transform leftLeg;
        private Transform rightLeg;
        private Transform leftLowerLeg;
        private Transform rightLowerLeg;
        private Transform leftFoot;
        private Transform rightFoot;
        private Quaternion hipsBase;
        private Quaternion spineBase;
        private Quaternion leftArmBase;
        private Quaternion leftForearmBase;
        private Quaternion rightArmBase;
        private Quaternion rightForearmBase;
        private Quaternion leftLegBase;
        private Quaternion rightLegBase;
        private Quaternion leftLowerLegBase;
        private Quaternion rightLowerLegBase;
        private Quaternion leftFootBase;
        private Quaternion rightFootBase;
        private Vector3 hipsBasePosition;
        private float locomotion;
        private float side;
        private float actionUntil;
        private float actionStarted;
        private float actionDuration;
        private int action;

        protected virtual void Awake()
        {
            hips = FindBone("Hips");
            spine = FindBone("Spine2", "Spine1", "Spine");
            leftArm = FindBone("LeftArm", "LeftUpperArm");
            leftForearm = FindBone("LeftForeArm", "LeftLowerArm");
            rightArm = FindBone("RightArm", "RightUpperArm");
            rightForearm = FindBone("RightForeArm", "RightLowerArm");
            leftHand = FindBone("LeftHand");
            rightHand = FindBone("RightHand");
            leftLeg = FindBone("LeftUpLeg", "LeftUpperLeg");
            rightLeg = FindBone("RightUpLeg", "RightUpperLeg");
            leftLowerLeg = FindBone("LeftLeg", "LeftLowerLeg");
            rightLowerLeg = FindBone("RightLeg", "RightLowerLeg");
            leftFoot = FindBone("LeftFoot");
            rightFoot = FindBone("RightFoot");

            // 部分写实 FBX 的骨骼名带命名空间，不能只依赖字符串后缀。
            // Humanoid Avatar 是最稳定的首次启动兜底，避免 Animator 第一帧停在 T Pose。
            Animator humanoid = GetComponent<Animator>();
            if (humanoid != null && humanoid.avatar != null && humanoid.avatar.isHuman)
            {
                hips ??= humanoid.GetBoneTransform(HumanBodyBones.Hips);
                spine ??= humanoid.GetBoneTransform(HumanBodyBones.UpperChest) ?? humanoid.GetBoneTransform(HumanBodyBones.Chest) ?? humanoid.GetBoneTransform(HumanBodyBones.Spine);
                leftArm ??= humanoid.GetBoneTransform(HumanBodyBones.LeftUpperArm);
                leftForearm ??= humanoid.GetBoneTransform(HumanBodyBones.LeftLowerArm);
                rightArm ??= humanoid.GetBoneTransform(HumanBodyBones.RightUpperArm);
                rightForearm ??= humanoid.GetBoneTransform(HumanBodyBones.RightLowerArm);
                leftHand ??= humanoid.GetBoneTransform(HumanBodyBones.LeftHand);
                rightHand ??= humanoid.GetBoneTransform(HumanBodyBones.RightHand);
                leftLeg ??= humanoid.GetBoneTransform(HumanBodyBones.LeftUpperLeg);
                rightLeg ??= humanoid.GetBoneTransform(HumanBodyBones.RightUpperLeg);
                leftLowerLeg ??= humanoid.GetBoneTransform(HumanBodyBones.LeftLowerLeg);
                rightLowerLeg ??= humanoid.GetBoneTransform(HumanBodyBones.RightLowerLeg);
                leftFoot ??= humanoid.GetBoneTransform(HumanBodyBones.LeftFoot);
                rightFoot ??= humanoid.GetBoneTransform(HumanBodyBones.RightFoot);
            }

            PoseArm(leftArm, leftForearm, (Vector3.down + transform.forward * 0.10f).normalized);
            PoseArm(rightArm, rightForearm, (Vector3.down + transform.forward * 0.10f).normalized);
            hipsBase = Local(hips);
            spineBase = Local(spine);
            leftArmBase = Local(leftArm);
            leftForearmBase = Local(leftForearm);
            rightArmBase = Local(rightArm);
            rightForearmBase = Local(rightForearm);
            leftLegBase = Local(leftLeg);
            rightLegBase = Local(rightLeg);
            leftLowerLegBase = Local(leftLowerLeg);
            rightLowerLegBase = Local(rightLowerLeg);
            leftFootBase = Local(leftFoot);
            rightFootBase = Local(rightFoot);
            hipsBasePosition = hips != null ? hips.localPosition : Vector3.zero;
        }

        public void SetLocomotion(float amount, float lateral)
        {
            locomotion = Mathf.Clamp01(amount);
            side = lateral;
        }

        public void TriggerLight() => BeginAction(1, 0.58f);
        public void TriggerHeavy() => BeginAction(2, 0.72f);
        public void TriggerDodge() => BeginAction(3, 0.34f);
        public void TriggerHit() => BeginAction(4, 0.38f);
        public void TriggerFinisher(bool heavy) => BeginAction(heavy ? 6 : 5, 1.65f);

        private void BeginAction(int nextAction, float duration)
        {
            action = nextAction;
            actionStarted = Time.time;
            actionDuration = duration;
            actionUntil = actionStarted + duration;
        }

        protected virtual void LateUpdate()
        {
            float phase = Time.time * Mathf.Lerp(2.2f, 10.5f, locomotion);
            float stride = Mathf.Sin(phase);
            float swing = stride * 30f * locomotion;
            float breath = Mathf.Sin(Time.time * 1.9f) * 1.8f;
            float leftKnee = Mathf.Max(0f, stride) * 34f * locomotion;
            float rightKnee = Mathf.Max(0f, -stride) * 34f * locomotion;
            Quaternion hipsTarget = hipsBase * Quaternion.Euler(0f, side * locomotion * 2f, -side * locomotion * 4f);
            Quaternion spineTarget = spineBase * Quaternion.Euler(breath - locomotion * 5f, 0f, side * locomotion * 3f);
            Quaternion leftArmTarget = leftArmBase * Quaternion.Euler(swing, 0f, 0f);
            Quaternion rightArmTarget = rightArmBase * Quaternion.Euler(-swing, 0f, 0f);
            Quaternion leftForearmTarget = leftForearmBase * Quaternion.Euler(-Mathf.Abs(swing) * 0.48f, 0f, 0f);
            Quaternion rightForearmTarget = rightForearmBase * Quaternion.Euler(-Mathf.Abs(swing) * 0.48f, 0f, 0f);
            Quaternion leftLegTarget = leftLegBase * Quaternion.Euler(-swing * 0.88f, 0f, 0f);
            Quaternion rightLegTarget = rightLegBase * Quaternion.Euler(swing * 0.88f, 0f, 0f);
            Quaternion leftLowerLegTarget = leftLowerLegBase * Quaternion.Euler(leftKnee, 0f, 0f);
            Quaternion rightLowerLegTarget = rightLowerLegBase * Quaternion.Euler(rightKnee, 0f, 0f);
            Quaternion leftFootTarget = leftFootBase * Quaternion.Euler(-leftKnee * 0.34f, 0f, 0f);
            Quaternion rightFootTarget = rightFootBase * Quaternion.Euler(-rightKnee * 0.34f, 0f, 0f);

            float actionCurve = 0f;
            if (Time.time <= actionUntil)
            {
                float normalized = Mathf.Clamp01((Time.time - actionStarted) / Mathf.Max(0.01f, actionDuration));
                float curve = action >= 5
                    ? Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(normalized / 0.30f))
                    : Mathf.Sin(Mathf.Clamp01(normalized) * Mathf.PI);
                actionCurve = curve;
                if (action == 1)
                {
                    rightArmTarget *= Quaternion.Euler(-92f * curve, 18f * curve, -26f * curve);
                    rightForearmTarget *= Quaternion.Euler(-58f * curve, 0f, 0f);
                    spineTarget *= Quaternion.Euler(0f, 28f * curve, 0f);
                }
                else if (action == 2)
                {
                    rightArmTarget *= Quaternion.Euler(-54f * curve, 16f, -28f * curve);
                    leftArmTarget *= Quaternion.Euler(-42f * curve, 0f, 26f * curve);
                    spineTarget *= Quaternion.Euler(14f * curve, -32f * curve, 0f);
                    rightLegTarget *= Quaternion.Euler(-76f * curve, 8f * curve, 0f);
                    rightLowerLegTarget *= Quaternion.Euler(58f * curve, 0f, 0f);
                    rightFootTarget *= Quaternion.Euler(-26f * curve, 0f, 0f);
                }
                else if (action == 3)
                {
                    hipsTarget *= Quaternion.Euler(22f * curve, 0f, 14f * curve);
                    spineTarget *= Quaternion.Euler(38f * curve, 0f, -22f * curve);
                    leftLegTarget *= Quaternion.Euler(-48f * curve, 0f, 0f);
                    rightLegTarget *= Quaternion.Euler(34f * curve, 0f, 0f);
                    leftLowerLegTarget *= Quaternion.Euler(52f * curve, 0f, 0f);
                    rightLowerLegTarget *= Quaternion.Euler(28f * curve, 0f, 0f);
                }
                else if (action == 4)
                {
                    spineTarget *= Quaternion.Euler(-26f * curve, 0f, 22f * curve);
                    leftArmTarget *= Quaternion.Euler(40f * curve, 0f, 35f * curve);
                    rightArmTarget *= Quaternion.Euler(32f * curve, 0f, -35f * curve);
                }
                else if (action == 5)
                {
                    hipsTarget *= Quaternion.Euler(0f, -16f * curve, -8f * curve);
                    spineTarget *= Quaternion.Euler(10f * curve, 32f * curve, -9f * curve);
                    rightArmTarget *= Quaternion.Euler(-104f * curve, 22f * curve, -20f * curve);
                    rightForearmTarget *= Quaternion.Euler(-74f * curve, 0f, 0f);
                    leftLegTarget *= Quaternion.Euler(-48f * curve, 0f, 0f);
                    rightLowerLegTarget *= Quaternion.Euler(36f * curve, 0f, 0f);
                }
                else if (action == 6)
                {
                    hipsTarget *= Quaternion.Euler(12f * curve, 18f * curve, 10f * curve);
                    spineTarget *= Quaternion.Euler(18f * curve, -38f * curve, 12f * curve);
                    rightArmTarget *= Quaternion.Euler(-78f * curve, 12f * curve, -34f * curve);
                    leftArmTarget *= Quaternion.Euler(-66f * curve, -8f * curve, 30f * curve);
                    rightLegTarget *= Quaternion.Euler(-70f * curve, 9f * curve, 0f);
                    rightLowerLegTarget *= Quaternion.Euler(56f * curve, 0f, 0f);
                }
            }

            Apply(hips, hipsTarget);
            Apply(spine, spineTarget);
            Apply(leftArm, leftArmTarget);
            Apply(leftForearm, leftForearmTarget);
            Apply(rightArm, rightArmTarget);
            Apply(rightForearm, rightForearmTarget);
            Apply(leftLeg, leftLegTarget);
            Apply(rightLeg, rightLegTarget);
            Apply(leftLowerLeg, leftLowerLegTarget);
            Apply(rightLowerLeg, rightLowerLegTarget);
            Apply(leftFoot, leftFootTarget);
            Apply(rightFoot, rightFootTarget);

            // FBX 骨骼的局部轴并不统一。世界方向约束保证无论重定向结果如何，
            // 肩-肘-手与髋-膝-脚都会产生清晰的三维动作，而不是停留在固定姿势。
            float walkWeight = locomotion * 0.72f;
            if (walkWeight > 0.01f && actionCurve <= 0.01f)
            {
                Vector3 down = -transform.up;
                Vector3 forward = transform.forward;
                AimBone(leftArm, leftForearm, down + forward * (stride * 0.62f), walkWeight);
                AimBone(rightArm, rightForearm, down - forward * (stride * 0.62f), walkWeight);
                AimBone(leftForearm, leftHand, down + forward * 0.16f, walkWeight * 0.65f);
                AimBone(rightForearm, rightHand, down + forward * 0.16f, walkWeight * 0.65f);
                AimBone(leftLeg, leftLowerLeg, down + forward * (stride * 0.78f), walkWeight);
                AimBone(rightLeg, rightLowerLeg, down - forward * (stride * 0.78f), walkWeight);
                AimBone(leftLowerLeg, leftFoot, down - forward * (Mathf.Max(0f, stride) * 0.38f), walkWeight * 0.78f);
                AimBone(rightLowerLeg, rightFoot, down - forward * (Mathf.Max(0f, -stride) * 0.38f), walkWeight * 0.78f);
            }

            if (actionCurve > 0.01f)
            {
                Vector3 forward = transform.forward;
                Vector3 up = transform.up;
                Vector3 right = transform.right;
                if (action == 1)
                {
                    AimBone(rightArm, rightForearm, forward + up * 0.18f + right * 0.08f, actionCurve);
                    AimBone(rightForearm, rightHand, forward - up * 0.05f, actionCurve);
                    AimBone(leftArm, leftForearm, forward * 0.35f - up * 0.35f - right * 0.30f, actionCurve * 0.72f);
                }
                else if (action == 2)
                {
                    // 前踢：大腿抬起、膝部送前、脚尖略向下，同时双臂形成护架。
                    AimBone(rightLeg, rightLowerLeg, forward * 0.82f + up * 0.56f + right * 0.08f, actionCurve);
                    AimBone(rightLowerLeg, rightFoot, forward * 0.94f - up * 0.28f, actionCurve);
                    AimBone(rightArm, rightForearm, forward * 0.46f + up * 0.42f + right * 0.22f, actionCurve * 0.90f);
                    AimBone(leftArm, leftForearm, forward * 0.48f + up * 0.38f - right * 0.22f, actionCurve * 0.90f);
                    AimBone(rightForearm, rightHand, forward + up * 0.06f, actionCurve * 0.82f);
                    AimBone(leftForearm, leftHand, forward + up * 0.06f, actionCurve * 0.82f);
                }
                else if (action == 5)
                {
                    AimBone(rightArm, rightForearm, forward + up * 0.12f + right * 0.10f, actionCurve);
                    AimBone(rightForearm, rightHand, forward - up * 0.08f, actionCurve);
                    AimBone(leftArm, leftForearm, forward * 0.45f + up * 0.38f - right * 0.28f, actionCurve * 0.88f);
                    AimBone(leftLeg, leftLowerLeg, forward * 0.56f - up * 0.82f, actionCurve * 0.85f);
                }
                else if (action == 6)
                {
                    AimBone(rightArm, rightForearm, forward * 0.72f + up * 0.56f + right * 0.18f, actionCurve);
                    AimBone(leftArm, leftForearm, forward * 0.64f + up * 0.50f - right * 0.18f, actionCurve);
                    AimBone(rightForearm, rightHand, forward + up * 0.04f, actionCurve);
                    AimBone(leftForearm, leftHand, forward + up * 0.04f, actionCurve);
                    AimBone(rightLeg, rightLowerLeg, forward * 0.74f + up * 0.40f, actionCurve * 0.92f);
                    AimBone(rightLowerLeg, rightFoot, forward * 0.86f - up * 0.30f, actionCurve * 0.90f);
                }
            }
            else if (locomotion <= 0.03f)
            {
                // Animator import can briefly report ready while still yielding its bind pose.
                // Reassert a relaxed, anatomically neutral idle in world space every frame.
                Vector3 relaxed = -transform.up + transform.forward * 0.08f;
                AimBone(leftArm, leftForearm, relaxed - transform.right * 0.05f, 0.94f);
                AimBone(rightArm, rightForearm, relaxed + transform.right * 0.05f, 0.94f);
                AimBone(leftForearm, leftHand, relaxed + transform.forward * 0.12f, 0.88f);
                AimBone(rightForearm, rightHand, relaxed + transform.forward * 0.12f, 0.88f);
            }
            if (hips != null)
            {
                float bob = Mathf.Abs(stride) * 0.018f * locomotion;
                hips.localPosition = Vector3.Lerp(hips.localPosition, hipsBasePosition + Vector3.up * bob, 1f - Mathf.Exp(-18f * Time.deltaTime));
            }
        }

        private Transform FindBone(params string[] endings)
        {
            foreach (Transform candidate in GetComponentsInChildren<Transform>(true))
            {
                foreach (string ending in endings)
                {
                    if (candidate.name.EndsWith(ending, System.StringComparison.OrdinalIgnoreCase)) return candidate;
                }
            }
            return null;
        }

        private static void PoseArm(Transform bone, Transform child, Vector3 direction)
        {
            if (bone == null || child == null) return;
            Vector3 current = child.position - bone.position;
            if (current.sqrMagnitude > 0.0001f) bone.rotation = Quaternion.FromToRotation(current.normalized, direction) * bone.rotation;
        }

        private static void AimBone(Transform bone, Transform child, Vector3 direction, float weight)
        {
            if (bone == null || child == null || direction.sqrMagnitude < 0.001f) return;
            Vector3 current = child.position - bone.position;
            if (current.sqrMagnitude < 0.0001f) return;
            Quaternion targetRotation = Quaternion.FromToRotation(current.normalized, direction.normalized) * bone.rotation;
            bone.rotation = Quaternion.Slerp(bone.rotation, targetRotation, Mathf.Clamp01(weight));
        }

        private static Quaternion Local(Transform bone) => bone != null ? bone.localRotation : Quaternion.identity;

        private static void Apply(Transform bone, Quaternion target)
        {
            if (bone != null) bone.localRotation = Quaternion.Slerp(bone.localRotation, target, 1f - Mathf.Exp(-18f * Time.deltaTime));
        }
    }

    // This partial declaration contains the implementation. The matching
    // ThirdPersonOrbitCamera.cs asset contains the other declaration so Unity can
    // serialize the component correctly in authored scenes.
    public sealed partial class ThirdPersonOrbitCamera : MonoBehaviour
    {
        private Transform target;
        private Camera cameraComponent;
        private float yaw = 28f;
        private float pitch = 22f;
        private float distance = 7.6f;
        private Vector3 velocity;
        private bool rhythmFocus;
        private float rhythmYaw;
        private float rhythmPitch;
        private float rhythmDistance;
        private float storedYaw;
        private float storedPitch;
        private float storedDistance;
        private float focusPunch;
        private float cameraShake;
        private int rhythmChapter;
        private Transform combatTarget;
        private bool combatFocus;
        private bool finisherFocus;
        private int finisherStage;
        private float finisherStageStarted;
        private float finisherSide = 1f;
        private bool animationPreviewFocus;
        private bool openingShot;
        private float openingStarted;
        private float openingDuration;
        private Vector3 openingCameraStart;
        private Vector3 openingCameraEnd;
        private Vector3 openingLookStart;
        private Vector3 openingLookEnd;
        private float smoothedCollisionDistance = -1f;
        private Vector2 smoothedLookDelta;
        private Vector3 smoothedTargetPosition;
        private bool hasSmoothedTargetPosition;
        private float lookInputBlockedUntil;
        private readonly RaycastHit[] obstructionHits = new RaycastHit[24];

        public Vector3 FlatForward
        {
            get
            {
                // Use the camera's actual rendered heading. During combat focus the
                // camera can orbit independently from the stored gameplay yaw; using
                // the stale yaw made WASD and character facing disagree.
                Vector3 value = cameraComponent != null ? cameraComponent.transform.forward :
                    Quaternion.Euler(0f, yaw, 0f) * Vector3.forward;
                value.y = 0f;
                return value.sqrMagnitude > 0.001f ? value.normalized : Vector3.forward;
            }
        }

        public Vector3 FlatRight => Quaternion.Euler(0f, 90f, 0f) * FlatForward;

        public void Configure(Transform followTarget)
        {
            target = followTarget;
            cameraComponent = GetComponent<Camera>();
            if (target != null)
            {
                smoothedTargetPosition = target.position;
                hasSmoothedTargetPosition = true;
            }
            if (target == null)
            {
                Debug.LogError("第三人称相机没有绑定玩家目标。");
            }
        }

        public void ResetGameplayView()
        {
            openingShot = false;
            rhythmFocus = false;
            combatFocus = false;
            finisherFocus = false;
            animationPreviewFocus = false;
            combatTarget = null;
            yaw = 28f;
            pitch = 22f;
            distance = 7.6f;
            storedYaw = yaw;
            storedPitch = pitch;
            storedDistance = distance;
            focusPunch = 0f;
            cameraShake = 0f;
            velocity = Vector3.zero;
            smoothedLookDelta = Vector2.zero;
            smoothedCollisionDistance = -1f;
            lookInputBlockedUntil = Time.unscaledTime + 0.35f;
            if (target != null)
            {
                smoothedTargetPosition = target.position;
                hasSmoothedTargetPosition = true;
            }
        }

        public void BeginOpeningShot(float durationSeconds)
        {
            if (target == null)
            {
                MeixiThirdPersonPlayer scenePlayer = FindAnyObjectByType<MeixiThirdPersonPlayer>();
                if (scenePlayer != null) Configure(scenePlayer.transform);
            }
            if (target == null) return;

            rhythmFocus = false;
            combatFocus = false;
            finisherFocus = false;
            animationPreviewFocus = false;
            combatTarget = null;
            velocity = Vector3.zero;
            cameraShake = 0f;
            focusPunch = 0f;
            smoothedCollisionDistance = -1f;
            openingShot = true;
            openingStarted = Time.unscaledTime;
            openingDuration = Mathf.Max(0.1f, durationSeconds);
            if (cameraComponent != null) cameraComponent.fieldOfView = 48f;

            Vector3 targetPosition = target.position;
            openingCameraStart = targetPosition + new Vector3(11.8f, 7.8f, 13.2f);
            openingLookStart = targetPosition + new Vector3(-4.8f, 1.05f, -4.0f);
            openingLookEnd = targetPosition + Vector3.up * 1.75f;
            Quaternion gameplayRotation = Quaternion.Euler(22f, 28f, 0f);
            openingCameraEnd = openingLookEnd - gameplayRotation * Vector3.forward * 7.6f;
            transform.position = ResolveCameraCollision(openingLookStart, openingCameraStart, true);
            Vector3 lookDirection = openingLookStart - transform.position;
            if (lookDirection.sqrMagnitude > 0.001f)
                transform.rotation = Quaternion.LookRotation(lookDirection, Vector3.up);
        }

        public void BeginCombatFocus(Transform enemy)
        {
            openingShot = false;
            animationPreviewFocus = false;
            if (!rhythmFocus)
            {
                storedYaw = yaw;
                storedPitch = pitch;
                storedDistance = distance;
            }
            combatTarget = enemy;
            combatFocus = enemy != null;
            finisherFocus = false;
            rhythmFocus = combatFocus;
            if (enemy != null)
            {
                Vector3 line = enemy.position - target.position;
                rhythmYaw = Mathf.Atan2(line.x, line.z) * Mathf.Rad2Deg + 30f;
                rhythmPitch = 16f;
                rhythmDistance = Mathf.Clamp(4.8f + line.magnitude * 0.78f, 6.2f, 15.5f);
            }
            focusPunch = 3.5f;
            cameraShake = 0f;
            velocity = Vector3.zero;
            smoothedCollisionDistance = -1f;
        }

        public void TriggerCombatImpact(bool perfect)
        {
            focusPunch = perfect ? 7.5f : 3.5f;
            cameraShake = perfect ? 0.13f : 0.07f;
        }

        public void EndCombatFocus()
        {
            combatFocus = false;
            finisherFocus = false;
            combatTarget = null;
            EndRhythmFocus();
        }

        public void BeginFinisherFocus(Transform enemy)
        {
            openingShot = false;
            animationPreviewFocus = false;
            if (target == null)
            {
                MeixiThirdPersonPlayer scenePlayer = FindAnyObjectByType<MeixiThirdPersonPlayer>();
                if (scenePlayer != null) Configure(scenePlayer.transform);
            }
            if (target == null) return;
            if (!rhythmFocus)
            {
                storedYaw = yaw;
                storedPitch = pitch;
                storedDistance = distance;
            }
            combatTarget = enemy;
            combatFocus = false;
            finisherFocus = enemy != null;
            rhythmFocus = true;
            finisherStage = 0;
            finisherStageStarted = Time.unscaledTime;
            if (enemy != null)
            {
                Vector3 line = enemy.position - target.position;
                float heading = Mathf.Atan2(line.x, line.z) * Mathf.Rad2Deg;
                Vector3 flatLine = Vector3.ProjectOnPlane(line, Vector3.up).normalized;
                Vector3 right = Vector3.Cross(Vector3.up, flatLine);
                Vector3 midpoint = Vector3.Lerp(target.position, enemy.position, 0.5f);
                finisherSide = Vector3.Dot(transform.position - midpoint, right) >= 0f ? 1f : -1f;
                rhythmYaw = heading + 78f;
                rhythmPitch = 11f;
                rhythmDistance = 4.35f;
            }
            focusPunch = 10f;
            cameraShake = 0.12f;
            smoothedCollisionDistance = -1f;
        }

        public void TriggerFinisherCut(int stage)
        {
            if (!finisherFocus || combatTarget == null) return;
            finisherStage = Mathf.Clamp(stage + 1, 1, 2);
            finisherStageStarted = Time.unscaledTime;
            Vector3 line = combatTarget.position - target.position;
            float heading = Mathf.Atan2(line.x, line.z) * Mathf.Rad2Deg;
            rhythmYaw = heading + (stage == 0 ? 104f : -58f);
            rhythmPitch = stage == 0 ? 9f : 18f;
            rhythmDistance = stage == 0 ? 3.65f : 5.15f;
            focusPunch = stage == 0 ? 12f : 8f;
            cameraShake = stage == 0 ? 0.18f : 0.12f;
        }

        public void BeginRhythmFocus(int chapter)
        {
            if (!rhythmFocus)
            {
                storedYaw = yaw;
                storedPitch = pitch;
                storedDistance = distance;
            }
            rhythmFocus = true;
            rhythmChapter = chapter;
            rhythmYaw = yaw + (chapter % 2 == 0 ? 32f : -38f);
            rhythmPitch = chapter == 3 ? 13f : 17f;
            rhythmDistance = chapter == 3 ? 4.4f : 5.0f;
            focusPunch = 4f;
        }

        public void BeginAnimationPreviewFocus()
        {
            openingShot = false;
            rhythmFocus = false;
            combatFocus = false;
            combatTarget = null;
            animationPreviewFocus = true;
            velocity = Vector3.zero;
        }

        public void TriggerRhythmCut(int actionIndex, bool perfect)
        {
            float[] cuts = { -24f, 38f, 112f, -72f, 72f, 164f };
            float[] distances = { 4.6f, 3.7f, 6.1f, 4.3f, 4.3f, 5.5f };
            int index = Mathf.Abs(actionIndex) % cuts.Length;
            rhythmYaw = storedYaw + cuts[index] + rhythmChapter * 7f;
            rhythmPitch = actionIndex == 2 ? 25f : actionIndex == 1 ? 10f : 16f;
            rhythmDistance = distances[index];
            focusPunch = perfect ? 8f : 4.5f;
            cameraShake = actionIndex is 1 or 5 ? (perfect ? 0.42f : 0.26f) : 0.12f;
        }

        public void TriggerRhythmMiss()
        {
            cameraShake = 0.16f;
            focusPunch = -3f;
        }

        public void EndRhythmFocus()
        {
            rhythmFocus = false;
            finisherFocus = false;
            yaw = storedYaw;
            pitch = storedPitch;
            distance = storedDistance;
            focusPunch = 0f;
            cameraShake = 0f;
            velocity = Vector3.zero;
            smoothedCollisionDistance = -1f;
        }

        private void LateUpdate()
        {
            // Authored scenes can be reloaded or have their hierarchy edited. Do not
            // leave the camera permanently frozen when a serialized reference is
            // missing; reacquire the actual gameplay player automatically.
            if (target == null)
            {
                MeixiThirdPersonPlayer scenePlayer = FindAnyObjectByType<MeixiThirdPersonPlayer>();
                if (scenePlayer != null) Configure(scenePlayer.transform);
                if (target == null) return;
            }

            if (openingShot)
            {
                float t = Mathf.Clamp01((Time.unscaledTime - openingStarted) / openingDuration);
                float eased = t * t * (3f - 2f * t);
                Vector3 desiredOpening = Vector3.Lerp(openingCameraStart, openingCameraEnd, eased);
                Vector3 openingPivot = Vector3.Lerp(openingLookStart, openingLookEnd, eased);
                transform.position = ResolveCameraCollision(openingPivot, desiredOpening);
                Vector3 openingLook = openingPivot - transform.position;
                if (openingLook.sqrMagnitude > 0.001f)
                    transform.rotation = Quaternion.LookRotation(openingLook, Vector3.up);
                if (cameraComponent != null) cameraComponent.fieldOfView = Mathf.Lerp(47f, 60f, eased);
                if (t >= 1f)
                {
                    openingShot = false;
                    yaw = 28f;
                    pitch = 22f;
                    distance = 7.6f;
                    velocity = Vector3.zero;
                    smoothedCollisionDistance = -1f;
                    smoothedLookDelta = Vector2.zero;
                    smoothedTargetPosition = target.position;
                    hasSmoothedTargetPosition = true;
                    lookInputBlockedUntil = Time.unscaledTime + 0.18f;
                }
                return;
            }

            float followBlend = 1f - Mathf.Exp(-15f * Mathf.Max(0.001f, Time.unscaledDeltaTime));
            if (!hasSmoothedTargetPosition)
            {
                smoothedTargetPosition = target.position;
                hasSmoothedTargetPosition = true;
            }
            else
            {
                smoothedTargetPosition = Vector3.Lerp(smoothedTargetPosition, target.position, followBlend);
            }

            if (animationPreviewFocus)
            {
                // 从角色正前侧拍摄全身，专门检查拳、腿与重心动作；该镜头不参与正式游戏。
                Vector3 previewPivot = target.position + Vector3.up * 1.02f;
                Vector3 previewDesired = previewPivot + target.right * 3.65f + target.forward * 1.35f + Vector3.up * 0.92f;
                float blend = 1f - Mathf.Exp(-9f * Time.unscaledDeltaTime);
                transform.position = Vector3.Lerp(transform.position, previewDesired, blend);
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(previewPivot - transform.position, Vector3.up), blend);
                if (cameraComponent != null) cameraComponent.fieldOfView = Mathf.Lerp(cameraComponent.fieldOfView, 44f, blend);
                return;
            }

            if (finisherFocus && combatTarget != null)
            {
                UpdateFinisherCamera();
                return;
            }

            var mouse = Mouse.current;
            if (!rhythmFocus && mouse != null)
            {
#if UNITY_EDITOR
                // Clicking the Game view after inspecting an object often releases
                // the cursor in the Editor. Capture it again instead of silently
                // disabling all camera input.
                if (Cursor.lockState != CursorLockMode.Locked && mouse.leftButton.wasPressedThisFrame)
                {
                    Cursor.lockState = CursorLockMode.Locked;
                    Cursor.visible = false;
                }
#endif
                bool acceptsLookInput = Cursor.lockState == CursorLockMode.Locked || mouse.rightButton.isPressed;
                if (acceptsLookInput && Time.unscaledTime >= lookInputBlockedUntil)
                {
                    Vector2 rawDelta = Vector2.ClampMagnitude(mouse.delta.ReadValue(), 32f);
                    if (rawDelta.sqrMagnitude < 0.035f) rawDelta = Vector2.zero;
                    float lookBlend = 1f - Mathf.Exp(-21f * Mathf.Max(0.001f, Time.unscaledDeltaTime));
                    smoothedLookDelta = Vector2.Lerp(smoothedLookDelta, rawDelta, lookBlend);
                    yaw += smoothedLookDelta.x * 0.082f;
                    pitch = Mathf.Clamp(pitch - smoothedLookDelta.y * 0.072f, 10f, 48f);
                    distance = Mathf.Clamp(distance - mouse.scroll.ReadValue().y * 0.004f, 4.8f, 10.5f);
                }
                else
                {
                    smoothedLookDelta = Vector2.Lerp(smoothedLookDelta, Vector2.zero, 1f - Mathf.Exp(-24f * Time.unscaledDeltaTime));
                }
            }
            else if (rhythmFocus)
            {
                float responsiveness = 1f - Mathf.Exp(-4.8f * Time.unscaledDeltaTime);
                if (combatFocus && combatTarget != null)
                {
                    Vector3 line = combatTarget.position - target.position;
                    float sideHeading = Mathf.Atan2(line.x, line.z) * Mathf.Rad2Deg + 30f;
                    rhythmYaw = sideHeading;
                    // Keep both combatants visible even when the player retreats or a
                    // blocked blink leaves them farther apart than the ideal duel range.
                    rhythmDistance = Mathf.Clamp(4.8f + line.magnitude * 0.78f, 6.2f, 15.5f);
                }
                yaw = Mathf.LerpAngle(yaw, rhythmYaw, responsiveness);
                pitch = Mathf.Lerp(pitch, rhythmPitch, responsiveness);
                distance = Mathf.Lerp(distance, rhythmDistance, responsiveness);
            }

            focusPunch = Mathf.MoveTowards(focusPunch, 0f, Time.unscaledDeltaTime * 16f);
            cameraShake = Mathf.MoveTowards(cameraShake, 0f, Time.unscaledDeltaTime * 3.8f);
            if (cameraComponent != null)
            {
                float targetFov = rhythmFocus ? (combatFocus ? 54f : 52f) - focusPunch : 60f;
                cameraComponent.fieldOfView = Mathf.Lerp(cameraComponent.fieldOfView, targetFov, 1f - Mathf.Exp(-9f * Time.unscaledDeltaTime));
            }

            Vector3 pivot = (combatFocus || finisherFocus) && combatTarget != null
                ? Vector3.Lerp(smoothedTargetPosition, combatTarget.position, finisherFocus ? 0.56f : 0.48f) + Vector3.up * 1.18f
                : smoothedTargetPosition + Vector3.up * (rhythmFocus ? 1.20f : 1.75f);
            Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
            Vector3 desired = pivot - rotation * Vector3.forward * distance;
            if (cameraShake > 0f)
            {
                desired += transform.right * Mathf.Sin(Time.unscaledTime * 57f) * cameraShake;
                desired += transform.up * Mathf.Cos(Time.unscaledTime * 43f) * cameraShake * 0.55f;
            }
            desired = ResolveCameraCollision(pivot, desired);
            transform.position = Vector3.SmoothDamp(transform.position, desired, ref velocity, 0.095f);
            Vector3 lookDirection = pivot - transform.position;
            if (lookDirection.sqrMagnitude > 0.001f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(lookDirection, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 1f - Mathf.Exp(-18f * Time.unscaledDeltaTime));
            }
        }

        private void OnApplicationFocus(bool focused)
        {
            if (!focused) return;
            smoothedLookDelta = Vector2.zero;
            velocity = Vector3.zero;
            lookInputBlockedUntil = Time.unscaledTime + 0.30f;
        }

        public void ClearInputAfterFocus()
        {
            smoothedLookDelta = Vector2.zero;
            velocity = Vector3.zero;
            lookInputBlockedUntil = Time.unscaledTime + 0.18f;
        }

        private Vector3 ResolveCameraCollision(Vector3 pivot, Vector3 desired, bool snap = false)
        {
            Vector3 cast = desired - pivot;
            float distanceToCamera = cast.magnitude;
            if (distanceToCamera < 0.01f) return desired;
            int count = Physics.SphereCastNonAlloc(pivot, 0.27f, cast / distanceToCamera, obstructionHits, distanceToCamera, ~0, QueryTriggerInteraction.Ignore);
            float nearest = distanceToCamera;
            for (int i = 0; i < count; i++)
            {
                Collider collider = obstructionHits[i].collider;
                if (collider == null) continue;
                Transform hitTransform = collider.transform;
                if (hitTransform == target || hitTransform.IsChildOf(target)) continue;
                if (combatTarget != null && (hitTransform == combatTarget || hitTransform.IsChildOf(combatTarget))) continue;
                if (hitTransform.GetComponentInParent<VillageNpc3D>() != null) continue;
                if (hitTransform.GetComponentInParent<SoulNode3D>() != null) continue;
                if (hitTransform.GetComponentInParent<NoiseBeast3D>() != null) continue;
                if (hitTransform.GetComponentInParent<ParticleSystem>() != null) continue;
                Vector3 boundsSize = collider.bounds.size;
                if (boundsSize.y < 0.65f && Mathf.Max(boundsSize.x, boundsSize.z) < 0.90f) continue;
                if (collider.bounds.max.y < pivot.y - 0.16f) continue;
                nearest = Mathf.Min(nearest, obstructionHits[i].distance);
            }
            float safeDistance = nearest < distanceToCamera ? Mathf.Max(0.55f, nearest - 0.18f) : distanceToCamera;
            if (snap || smoothedCollisionDistance < 0f || safeDistance < smoothedCollisionDistance)
            {
                smoothedCollisionDistance = safeDistance;
            }
            else
            {
                float release = 1f - Mathf.Exp(-5f * Time.unscaledDeltaTime);
                smoothedCollisionDistance = Mathf.Lerp(smoothedCollisionDistance, safeDistance, release);
            }
            smoothedCollisionDistance = Mathf.Clamp(smoothedCollisionDistance, 0.55f, distanceToCamera);
            return pivot + cast.normalized * smoothedCollisionDistance;
        }

        private void UpdateFinisherCamera()
        {
            Vector3 playerPosition = target.position;
            Vector3 enemyPosition = combatTarget.position;
            Vector3 line = Vector3.ProjectOnPlane(enemyPosition - playerPosition, Vector3.up);
            if (line.sqrMagnitude < 0.01f) line = target.forward;
            line.Normalize();
            Vector3 side = Vector3.Cross(Vector3.up, line) * finisherSide;
            Vector3 midpoint = Vector3.Lerp(playerPosition, enemyPosition, 0.52f);
            float stageAge = Time.unscaledTime - finisherStageStarted;
            Vector3 pivot;
            Vector3 desired;
            float fov;
            float roll;
            float responsiveness;

            if (finisherStage == 0)
            {
                // 蓄力特写：镜头从主角肩侧缓慢推近，同时保留敌人在景深方向中。
                float dolly = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(stageAge / 0.34f));
                pivot = playerPosition + Vector3.up * 1.22f + line * Mathf.Lerp(0.34f, 0.66f, dolly);
                desired = playerPosition - line * Mathf.Lerp(1.35f, 0.92f, dolly) + side * Mathf.Lerp(2.45f, 1.82f, dolly) + Vector3.up * 1.54f;
                fov = Mathf.Lerp(39f, 31f, dolly);
                roll = -finisherSide * 2.8f;
                responsiveness = 12f;
            }
            else if (finisherStage == 1)
            {
                // 命中特写：切到攻击轴另一侧的低机位，同时保留主角完整终结姿态。
                pivot = Vector3.Lerp(playerPosition, enemyPosition, 0.45f) + Vector3.up * 1.02f;
                desired = midpoint + side * 6.15f - line * 0.58f + Vector3.up * 1.96f;
                fov = 49f;
                roll = finisherSide * 2.6f;
                responsiveness = 22f;
            }
            else
            {
                // 收势英雄镜头：低角度展示主角、溪灵和最后扩散的魂音。
                pivot = midpoint + Vector3.up * 1.04f;
                desired = playerPosition - line * 3.55f - side * 1.65f + Vector3.up * 0.82f;
                fov = 42f;
                roll = -finisherSide * 1.6f;
                responsiveness = 17f;
            }

            if (cameraShake > 0f)
            {
                desired += side * Mathf.Sin(Time.unscaledTime * 61f) * cameraShake;
                desired += Vector3.up * Mathf.Cos(Time.unscaledTime * 47f) * cameraShake * 0.48f;
            }
            focusPunch = Mathf.MoveTowards(focusPunch, 0f, Time.unscaledDeltaTime * 20f);
            cameraShake = Mathf.MoveTowards(cameraShake, 0f, Time.unscaledDeltaTime * 2.2f);
            desired = ResolveCameraCollision(pivot, desired);
            float blend = 1f - Mathf.Exp(-responsiveness * Time.unscaledDeltaTime);
            transform.position = Vector3.Lerp(transform.position, desired, blend);
            Quaternion look = Quaternion.LookRotation(pivot - transform.position, Vector3.up) * Quaternion.Euler(0f, 0f, roll);
            transform.rotation = Quaternion.Slerp(transform.rotation, look, blend);
            if (cameraComponent != null)
                cameraComponent.fieldOfView = Mathf.Lerp(cameraComponent.fieldOfView, Mathf.Max(22f, fov - focusPunch), blend);
        }
    }
}
