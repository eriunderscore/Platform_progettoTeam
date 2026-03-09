using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController3D : MonoBehaviour
{
    // ── Movement ───────────────────────────────────────────────
    [Header("Movement")]
    public float moveSpeed = 8f;
    public float acceleration = 14f;    // how fast you reach move speed on ground
    public float deceleration = 10f;    // how fast you slow down on ground
    public float airAcceleration = 5f;     // much weaker in air — momentum preserved
    public float airDeceleration = 2f;     // very weak air drag — momentum bleeds slowly
    public float rotationSpeed = 15f;

    // ── Jump ──────────────────────────────────────────────────
    [Header("Jump")]
    public float jumpHeight = 3.5f;
    public float gravity = -30f;
    public float fallMultiplier = 2.5f;
    public float jumpCutMultiplier = 0.4f;
    public float coyoteTime = 0.12f;
    public float jumpBufferTime = 0.12f;
    public float maxFallSpeed = 40f;

    // ── Momentum Jump Bonus ────────────────────────────────────
    [Header("Momentum Jump Bonus")]
    [Tooltip("How much horizontal speed adds to jump height. 0 = no bonus, 1 = full transfer")]
    public float horizontalToJumpBonus = 0.08f;  // small multiplier on h-speed → extra y velocity
    [Tooltip("Max extra upward velocity that horizontal speed can add")]
    public float maxJumpBonus = 4f;

    // ── Wall Jump ─────────────────────────────────────────────
    [Header("Wall Jump")]
    public float wallSlideSpeed = 1.5f;
    public float wallJumpUpForce = 12f;
    public float wallJumpOutForce = 10f;
    public float wallJumpLockTime = 0.2f;
    public float wallCheckDistance = 0.6f;

    // ── Dash ──────────────────────────────────────────────────
    [Header("Dash")]
    public float dashSpeed = 22f;
    public float dashDuration = 0.18f;
    public float dashCooldown = 0.25f;
    public int maxDashes = 1;
    [Tooltip("How quickly dash momentum bleeds off after the dash ends")]
    public float dashMomentumDecay = 6f;

    // ── Ground Check ──────────────────────────────────────────
    [Header("Ground Check")]
    public LayerMask groundLayer;
    public Transform groundCheck;
    public float groundCheckRadius = 0.25f;

    // ── Wall Ground Check ─────────────────────────────────────
    [Header("Wall Ground Check")]
    public Transform wallGroundCheck;
    public float wallGroundCheckRadius = 0.25f;
    public LayerMask wallLayer;

    // ── References ────────────────────────────────────────────
    [Header("References")]
    public Transform cameraTransform;
    public SquishEffect squishEffect;
    public WallClimb wallClimb;

    // ── Private State ─────────────────────────────────────────
    private CharacterController cc;

    // Full 3D velocity — momentum lives here
    private Vector3 velocity;

    private bool isGrounded;
    public bool IsGrounded => isGrounded;

    private bool isTouchingWall;
    private Vector3 wallNormal;
    private bool isWallSliding;

    // Wall jump alternation — tracks which wall normal we last jumped from
    // Player can wall jump the same wall again ONLY after jumping a different wall
    private Vector3 lastWallJumpNormal = Vector3.zero;
    private bool hasWallJump = true;

    private float coyoteTimer;
    private float jumpBufferTimer;
    private bool isJumping;
    private bool jumpHeld;

    private float wallJumpTimer;

    private int dashesLeft;
    private bool isDashing;
    private float dashTimer;
    private float dashCooldownTimer;
    private Vector3 dashDirection;

    public bool IsDashing => isDashing;
    public Vector3 DashDirection => dashDirection;
    public float InputH { get; private set; }
    public float InputV { get; private set; }

    private Vector3 respawnPosition;

    // ──────────────────────────────────────────────────────────

    void Awake()
    {
        cc = GetComponent<CharacterController>();
        dashesLeft = maxDashes;
        respawnPosition = transform.position;
    }

    void Update()
    {
        if (wallClimb != null && wallClimb.IsClimbing)
        {
            wallClimb.ClimbUpdate();
            return;
        }

        GatherInput();
        CheckGrounded();
        CheckWall();
        CheckWallGround();
        UpdateTimers();
        HandleWallSlide();
        HandleJump();
        HandleMovement();
        ApplyGravity();
        ApplyMove();
        RotateToFacing();
    }

    // ──────────────────────────────────────────────────────────
    //  INPUT
    // ──────────────────────────────────────────────────────────
    void GatherInput()
    {
        InputH = Input.GetAxisRaw("Horizontal");
        InputV = Input.GetAxisRaw("Vertical");
        jumpHeld = Input.GetButton("Jump");

        if (Input.GetButtonDown("Jump"))
            jumpBufferTimer = jumpBufferTime;

        // Jump cut
        if (Input.GetButtonUp("Jump") && velocity.y > 0 && isJumping)
            velocity.y *= jumpCutMultiplier;

        if (Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.X))
            TryDash();
    }

    // ──────────────────────────────────────────────────────────
    //  TIMERS
    // ──────────────────────────────────────────────────────────
    void UpdateTimers()
    {
        jumpBufferTimer -= Time.deltaTime;
        dashCooldownTimer -= Time.deltaTime;
        if (wallJumpTimer > 0f) wallJumpTimer -= Time.deltaTime;

        if (isGrounded)
        {
            coyoteTimer = coyoteTime;
            dashesLeft = maxDashes;
            isJumping = false;
            // Reset wall jump when grounded
            hasWallJump = true;
            lastWallJumpNormal = Vector3.zero;
        }
        else
        {
            coyoteTimer -= Time.deltaTime;
        }

        if (isDashing)
        {
            dashTimer -= Time.deltaTime;
            if (dashTimer <= 0f) StopDash();
        }
    }

    // ──────────────────────────────────────────────────────────
    //  COLLISION CHECKS
    // ──────────────────────────────────────────────────────────
    void CheckGrounded()
    {
        isGrounded = Physics.CheckSphere(groundCheck.position, groundCheckRadius, groundLayer);
    }

    void CheckWall()
    {
        Vector3[] dirs = { transform.forward, -transform.forward, transform.right, -transform.right };
        isTouchingWall = false;

        foreach (var dir in dirs)
        {
            if (Physics.Raycast(transform.position, dir, out RaycastHit hit, wallCheckDistance, groundLayer))
            {
                if (Vector3.Dot(hit.normal, Vector3.up) < 0.3f)
                {
                    isTouchingWall = true;
                    wallNormal = hit.normal;
                    break;
                }
            }
        }
    }

    void CheckWallGround()
    {
        if (wallGroundCheck == null) return;
        if (Physics.CheckSphere(wallGroundCheck.position, wallGroundCheckRadius, wallLayer))
        {
            hasWallJump = true;
            lastWallJumpNormal = Vector3.zero;
            coyoteTimer = coyoteTime;
        }
    }

    // ──────────────────────────────────────────────────────────
    //  WALL SLIDE
    // ──────────────────────────────────────────────────────────
    void HandleWallSlide()
    {
        bool pushingIntoWall = isTouchingWall && (InputH != 0 || InputV != 0);
        isWallSliding = pushingIntoWall && !isGrounded && velocity.y < 0;

        if (isWallSliding)
            velocity.y = Mathf.Max(velocity.y, -wallSlideSpeed);
    }

    // ──────────────────────────────────────────────────────────
    //  JUMP
    // ──────────────────────────────────────────────────────────
    void HandleJump()
    {
        if (jumpBufferTimer <= 0f) return;

        if (isWallSliding && CanWallJump()) { WallJump(); return; }
        if (coyoteTimer > 0f) { NormalJump(); return; }
    }

    // Wall jump alternation check:
    // Can jump same wall again ONLY if we've jumped a different wall since last time
    bool CanWallJump()
    {
        if (!hasWallJump) return false;

        // If we haven't wall jumped yet this airtime, always allowed
        if (lastWallJumpNormal == Vector3.zero) return true;

        // Allow if this is a DIFFERENT wall (normals differ)
        // Dot < 0.9 means the angle between normals is more than ~25° = different wall
        return Vector3.Dot(wallNormal, lastWallJumpNormal) < 0.9f;
    }

    void NormalJump()
    {
        // Base jump velocity
        float baseJumpVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);

        // Horizontal momentum bonus — faster you're moving, slightly higher you jump
        float horizontalSpeed = new Vector3(velocity.x, 0f, velocity.z).magnitude;
        float bonus = Mathf.Min(horizontalSpeed * horizontalToJumpBonus, maxJumpBonus);

        velocity.y = baseJumpVelocity + bonus;
        jumpBufferTimer = 0f;
        coyoteTimer = 0f;
        isJumping = true;

        if (squishEffect != null) squishEffect.TriggerJumpSquish();
    }

    void WallJump()
    {
        // Record which wall we jumped from
        lastWallJumpNormal = wallNormal;

        // After jumping one wall, lock wall jump until we hit a DIFFERENT wall
        // (hasWallJump stays true — CanWallJump handles the logic)

        Vector3 jumpDir = (wallNormal + Vector3.up).normalized;

        // Preserve and redirect existing horizontal momentum
        // instead of snapping to a fixed force
        float currentSpeed = new Vector3(velocity.x, 0f, velocity.z).magnitude;
        float outSpeed = Mathf.Max(wallJumpOutForce, currentSpeed * 0.6f);

        velocity = jumpDir * outSpeed;
        velocity.y = wallJumpUpForce;

        jumpBufferTimer = 0f;
        isJumping = true;
        wallJumpTimer = wallJumpLockTime;
    }

    // ──────────────────────────────────────────────────────────
    //  MOVEMENT  (momentum-based)
    // ──────────────────────────────────────────────────────────
    void HandleMovement()
    {
        if (isDashing || wallJumpTimer > 0f) return;

        Vector3 camForward = cameraTransform.forward; camForward.y = 0f; camForward.Normalize();
        Vector3 camRight = cameraTransform.right; camRight.y = 0f; camRight.Normalize();

        Vector3 inputDir = camForward * InputV + camRight * InputH;
        if (inputDir.sqrMagnitude > 1f) inputDir.Normalize();

        float inputMag = inputDir.magnitude;

        if (isGrounded)
        {
            if (inputMag > 0.01f)
            {
                // Accelerate toward input direction on the ground
                Vector3 targetVelocity = inputDir * moveSpeed;
                velocity.x = Mathf.MoveTowards(velocity.x, targetVelocity.x, acceleration * Time.deltaTime);
                velocity.z = Mathf.MoveTowards(velocity.z, targetVelocity.z, acceleration * Time.deltaTime);
            }
            else
            {
                // No input — decelerate to stop on ground
                velocity.x = Mathf.MoveTowards(velocity.x, 0f, deceleration * Time.deltaTime);
                velocity.z = Mathf.MoveTowards(velocity.z, 0f, deceleration * Time.deltaTime);
            }
        }
        else
        {
            // In air — much weaker influence, momentum is mostly preserved
            if (inputMag > 0.01f)
            {
                // Nudge in input direction but don't override existing momentum strongly
                velocity.x += inputDir.x * airAcceleration * Time.deltaTime;
                velocity.z += inputDir.z * airAcceleration * Time.deltaTime;

                // Soft cap at move speed — can exceed it from dash/launch but input
                // alone won't push beyond normal move speed
                Vector3 hVel = new Vector3(velocity.x, 0f, velocity.z);
                float hSpeed = hVel.magnitude;
                if (hSpeed > moveSpeed)
                {
                    // Only cap the input-added portion, not dash momentum
                    // Decay excess speed slowly rather than hard clamping
                    float excess = hSpeed - moveSpeed;
                    hVel = hVel.normalized * (moveSpeed + excess * (1f - airDeceleration * Time.deltaTime));
                    velocity.x = hVel.x;
                    velocity.z = hVel.z;
                }
            }
            else
            {
                // No input in air — very light drag, momentum bleeds slowly
                velocity.x = Mathf.MoveTowards(velocity.x, 0f, airDeceleration * Time.deltaTime);
                velocity.z = Mathf.MoveTowards(velocity.z, 0f, airDeceleration * Time.deltaTime);
            }
        }
    }

    // ──────────────────────────────────────────────────────────
    //  GRAVITY
    // ──────────────────────────────────────────────────────────
    void ApplyGravity()
    {
        if (isDashing) return;

        if (isGrounded && velocity.y < 0f) { velocity.y = -2f; return; }

        float gMult = velocity.y < 0f ? fallMultiplier : 1f;
        velocity.y += gravity * gMult * Time.deltaTime;
        velocity.y = Mathf.Max(velocity.y, -maxFallSpeed);
    }

    // ──────────────────────────────────────────────────────────
    //  DASH
    // ──────────────────────────────────────────────────────────
    void TryDash()
    {
        if (dashesLeft <= 0 || dashCooldownTimer > 0f || isDashing) return;

        Vector3 camForward = cameraTransform.forward; camForward.y = 0f; camForward.Normalize();
        Vector3 camRight = cameraTransform.right; camRight.y = 0f; camRight.Normalize();

        float upComponent = jumpHeld ? 0.7f : 0f;

        dashDirection = camForward * InputV + camRight * InputH;
        if (dashDirection.sqrMagnitude < 0.01f) dashDirection = transform.forward;
        dashDirection.y = upComponent;
        dashDirection = dashDirection.normalized;

        dashesLeft--;
        isDashing = true;
        dashTimer = dashDuration;
        dashCooldownTimer = dashCooldown;
        velocity = dashDirection * dashSpeed;

        if (squishEffect != null) squishEffect.TriggerDashSquish();
    }

    void StopDash()
    {
        isDashing = false;
        // Don't zero out velocity — let momentum carry forward
        // Just cap vertical component so it doesn't feel floaty after upward dash
        if (dashDirection.y > 0f)
            velocity.y = Mathf.Min(velocity.y, dashSpeed * 0.4f);
    }

    // ──────────────────────────────────────────────────────────
    //  APPLY MOVE
    // ──────────────────────────────────────────────────────────
    void ApplyMove() => cc.Move(velocity * Time.deltaTime);

    public void MoveByVector(Vector3 motion) => cc.Move(motion * Time.deltaTime);
    public void SetVelocity(Vector3 v) => velocity = v;
    public void RefillDash() => dashesLeft = maxDashes;

    // ──────────────────────────────────────────────────────────
    //  ROTATE
    // ──────────────────────────────────────────────────────────
    void RotateToFacing()
    {
        Vector3 flat = new Vector3(velocity.x, 0f, velocity.z);
        if (flat.sqrMagnitude > 0.1f)
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                Quaternion.LookRotation(flat),
                rotationSpeed * Time.deltaTime);
    }

    // ──────────────────────────────────────────────────────────
    //  RESPAWN
    // ──────────────────────────────────────────────────────────
    public void SetRespawnPoint(Vector3 pos) => respawnPosition = pos;

    public void Die()
    {
        if (RespawnManager3D.Instance != null)
            RespawnManager3D.Instance.KillPlayer(gameObject, respawnPosition);
    }

    // ──────────────────────────────────────────────────────────
    //  GIZMOS
    // ──────────────────────────────────────────────────────────
    void OnDrawGizmosSelected()
    {
        if (groundCheck)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }
        if (wallGroundCheck)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(wallGroundCheck.position, wallGroundCheckRadius);
        }
        Gizmos.color = Color.cyan;
        Gizmos.DrawRay(transform.position, transform.forward * wallCheckDistance);
        Gizmos.DrawRay(transform.position, -transform.forward * wallCheckDistance);
        Gizmos.DrawRay(transform.position, transform.right * wallCheckDistance);
        Gizmos.DrawRay(transform.position, -transform.right * wallCheckDistance);
    }
}