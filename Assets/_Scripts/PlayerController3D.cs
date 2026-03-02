using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController3D : MonoBehaviour
{
    // ── Movement ───────────────────────────────────────────────
    [Header("Movement")]
    public float moveSpeed = 8f;
    public float iceFactor = 1f;
    public float acceleration = 14f;
    public float deceleration = 10f;
    public float airAcceleration = 5f;
    public float airDeceleration = 2f;
    public float rotationSpeed = 15f;

    [Header("Jump")]
    public float jumpHeight = 3.5f;
    public float gravity = -30f;
    public float fallMultiplier = 2.5f;
    public float jumpCutMultiplier = 0.4f;
    public float coyoteTime = 0.12f;
    public float jumpBufferTime = 0.12f;
    public float maxFallSpeed = 40f;

    [Header("Momentum Jump Bonus")]
    public float horizontalToJumpBonus = 0.08f;
    public float maxJumpBonus = 4f;

    [Header("Wall Jump")]
    public float wallSlideSpeed = 1.5f;
    public float wallJumpUpForce = 12f;
    public float wallJumpOutForce = 10f;
    public float wallJumpLockTime = 0.2f;
    public float wallCheckDistance = 0.6f;

    [Header("Dash")]
    public float dashSpeed = 22f;
    public float dashDuration = 0.18f;
    public float dashCooldown = 0.25f;
    public int maxDashes = 1;

    [Header("Ground Check")]
    public LayerMask groundLayer;
    public Transform groundCheck;
    public float groundCheckRadius = 0.25f;

    [Header("Wall Ground Check")]
    public Transform wallGroundCheck;
    public float wallGroundCheckRadius = 0.25f;
    public LayerMask wallLayer;

    [Header("References")]
    public Transform cameraTransform;
    public SquishEffect squishEffect;
    public WallClimb wallClimb;

    [Header("Morte e Caduta")]
    public float limiteCaduta = -10f;
    public int maxLives = 3; // Vite massime impostabili dall'ispettore

    private CharacterController cc;
    private Vector3 velocity;
    private bool isGrounded;
    public bool IsGrounded => isGrounded;
    private bool isTouchingWall;
    private Vector3 wallNormal;
    private bool isWallSliding;
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

    // VARIABILE VITE AGGIUNTA
    private int currentLives;

    void Awake()
    {
        cc = GetComponent<CharacterController>();
        dashesLeft = maxDashes;
        respawnPosition = transform.position;
        currentLives = maxLives; // Inizializza le vite
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

        if (transform.position.y < limiteCaduta)
        {
            Die();
        }
    }

    void GatherInput()
    {
        InputH = Input.GetAxisRaw("Horizontal");
        InputV = Input.GetAxisRaw("Vertical");
        jumpHeld = Input.GetButton("Jump");
        if (Input.GetButtonDown("Jump")) jumpBufferTimer = jumpBufferTime;
        if (Input.GetButtonUp("Jump") && velocity.y > 0 && isJumping) velocity.y *= jumpCutMultiplier;
        if (Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.X)) TryDash();
    }

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
            hasWallJump = true;
            lastWallJumpNormal = Vector3.zero;
        }
        else coyoteTimer -= Time.deltaTime;

        if (isDashing)
        {
            dashTimer -= Time.deltaTime;
            if (dashTimer <= 0f) StopDash();
        }
    }

    void CheckGrounded() => isGrounded = Physics.CheckSphere(groundCheck.position, groundCheckRadius, groundLayer);

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

    void HandleWallSlide()
    {
        bool pushingIntoWall = isTouchingWall && (InputH != 0 || InputV != 0);
        isWallSliding = pushingIntoWall && !isGrounded && velocity.y < 0;
        if (isWallSliding) velocity.y = Mathf.Max(velocity.y, -wallSlideSpeed);
    }

    void HandleJump()
    {
        if (jumpBufferTimer <= 0f) return;
        if (isWallSliding && CanWallJump()) { WallJump(); return; }
        if (coyoteTimer > 0f) { NormalJump(); return; }
    }

    bool CanWallJump()
    {
        if (!hasWallJump) return false;
        if (lastWallJumpNormal == Vector3.zero) return true;
        return Vector3.Dot(wallNormal, lastWallJumpNormal) < 0.9f;
    }

    void NormalJump()
    {
        float baseJumpVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
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
        lastWallJumpNormal = wallNormal;
        Vector3 jumpDir = (wallNormal + Vector3.up).normalized;
        float currentSpeed = new Vector3(velocity.x, 0f, velocity.z).magnitude;
        float outSpeed = Mathf.Max(wallJumpOutForce, currentSpeed * 0.6f);
        velocity = jumpDir * outSpeed;
        velocity.y = wallJumpUpForce;
        jumpBufferTimer = 0f;
        isJumping = true;
        wallJumpTimer = wallJumpLockTime;
    }

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
            Vector3 targetVelocity = inputDir * moveSpeed;
            if (inputMag > 0.01f)
            {
                velocity.x = Mathf.MoveTowards(velocity.x, targetVelocity.x, acceleration * iceFactor * Time.deltaTime);
                velocity.z = Mathf.MoveTowards(velocity.z, targetVelocity.z, acceleration * iceFactor * Time.deltaTime);
            }
            else
            {
                velocity.x = Mathf.MoveTowards(velocity.x, 0f, deceleration * iceFactor * Time.deltaTime);
                velocity.z = Mathf.MoveTowards(velocity.z, 0f, deceleration * iceFactor * Time.deltaTime);
            }
        }
        else
        {
            if (inputMag > 0.01f)
            {
                velocity.x += inputDir.x * airAcceleration * Time.deltaTime;
                velocity.z += inputDir.z * airAcceleration * Time.deltaTime;
            }
            velocity.x = Mathf.MoveTowards(velocity.x, 0f, airDeceleration * Time.deltaTime);
            velocity.z = Mathf.MoveTowards(velocity.z, 0f, airDeceleration * Time.deltaTime);
        }
    }

    void ApplyGravity()
    {
        if (isDashing) return;
        if (isGrounded && velocity.y < 0f) { velocity.y = -2f; return; }
        float gMult = velocity.y < 0f ? fallMultiplier : 1f;
        velocity.y += gravity * gMult * Time.deltaTime;
        velocity.y = Mathf.Max(velocity.y, -maxFallSpeed);
    }

    void TryDash()
    {
        if (dashesLeft <= 0 || dashCooldownTimer > 0f || isDashing) return;
        Vector3 camForward = cameraTransform.forward; camForward.y = 0f; camForward.Normalize();
        Vector3 camRight = cameraTransform.right; camRight.y = 0f; camRight.Normalize();
        dashDirection = (camForward * InputV + camRight * InputH).normalized;
        if (dashDirection.sqrMagnitude < 0.01f) dashDirection = transform.forward;
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
        velocity.y = Mathf.Min(velocity.y, dashSpeed * 0.4f);
    }

    void ApplyMove() => cc.Move(velocity * Time.deltaTime);

    public void MoveByVector(Vector3 motion) => cc.Move(motion * Time.deltaTime);
    public void RefillDash() => dashesLeft = maxDashes;
    public void SetVelocity(Vector3 v) => velocity = v;

    public void SetRespawnPoint(Vector3 pos) => respawnPosition = pos;

    void RotateToFacing()
    {
        Vector3 flat = new Vector3(velocity.x, 0f, velocity.z);
        if (flat.sqrMagnitude > 0.1f)
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(flat), rotationSpeed * Time.deltaTime);
    }

    // MODIFICATA: ORA GESTISCE VITE E UI
    public void Die()
    {
        velocity = Vector3.zero;

        // Gestione Vite
        currentLives--;

        // Aggiorna UI dei Cuori
        if (LivesUI.Instance != null)
        {
            LivesUI.Instance.OnPlayerDied(currentLives);
        }

        if (currentLives <= 0)
        {
            // Reset base in caso di Game Over (puoi aggiungere altro qui)
            currentLives = maxLives;
            if (LivesUI.Instance != null) LivesUI.Instance.OnGameReset(currentLives);
        }

        // Teletrasporto al respawn
        cc.enabled = false;
        transform.position = respawnPosition;
        cc.enabled = true;

        Debug.Log("Il Rapanello è morto! Vite rimaste: " + currentLives);
    }

    void OnDrawGizmosSelected()
    {
        if (groundCheck) Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
    }
}