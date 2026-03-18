// ============================================================
//  PlayerController3D.cs
//  Script UNIFICATO che fonde PlayerController3D + WallClimb.
//  Richiede sulla stessa GameObject:
//    - CharacterController
//    - PlayerStateMachine
//    - PlayerInputHandler
//
//  SETUP NELL'EDITOR:
//  1. Aggiungi questo script al tuo Player GameObject
//  2. Aggiungi anche PlayerStateMachine e PlayerInputHandler
//  3. Crea un GameObject figlio vuoto chiamato "GroundCheck"
//     e posizionalo ai piedi del personaggio (Y = -0.9 circa)
//     → assegnalo al campo "Ground Check"
//  4. Assegna il Transform della Camera al campo "Camera Transform"
//  5. Crea un Layer chiamato "Ground" e assegnalo ai tuoi terreni
//     → selezionalo nel campo "Ground Layer"
//  6. Crea un Layer chiamato "Wall" per le pareti arrampicabili
//     → selezionalo nel campo "Wall Layer"
// ============================================================

using UnityEngine;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(PlayerStateMachine))]
[RequireComponent(typeof(PlayerInputHandler))]
public class PlayerController3D : MonoBehaviour
{
    // ══════════════════════════════════════════════════════════
    //  INSPECTOR PARAMETERS
    // ══════════════════════════════════════════════════════════

    [Header("── Movement ──────────────────────────────────")]
    [Tooltip("Velocità massima di corsa a terra")]
    public float moveSpeed = 8f;

    [Tooltip("Accelerazione a terra quando si dà input")]
    public float acceleration = 60f;

    [Tooltip("Decelerazione a terra quando non si dà input")]
    public float deceleration = 60f;

    [Tooltip("Accelerazione in aria quando si dà input")]
    public float airAcceleration = 40f;

    [Tooltip("Decelerazione in aria senza input")]
    public float airDeceleration = 8f;

    [Tooltip("Velocità di rotazione del personaggio verso la direzione di movimento")]
    public float rotationSpeed = 15f;

    [Header("── Jump ─────────────────────────────────────")]
    [Tooltip("Altezza massima del salto in unità Unity")]
    public float jumpHeight = 3.5f;

    [Tooltip("Gravità base applicata al personaggio")]
    public float gravity = -30f;

    [Tooltip("Moltiplicatore gravità durante la caduta (rende la caduta più pesante)")]
    public float fallMultiplier = 2.5f;

    [Tooltip("Quanto viene ridotta la velocità Y se si rilascia Jump prima del picco")]
    public float jumpCutMultiplier = 0.4f;

    [Tooltip("Secondi di grazia dopo aver lasciato un bordo in cui si può ancora saltare")]
    public float coyoteTime = 0.12f;

    [Tooltip("Secondi di buffer: se premi Jump poco prima di atterrare, viene eseguito")]
    public float jumpBufferTime = 0.12f;

    [Tooltip("Velocità massima di caduta (cap verso il basso)")]
    public float maxFallSpeed = 40f;

    [Header("── Jump Momentum Bonus ───────────────────────")]
    [Tooltip("Quanto la velocità orizzontale bonus il salto")]
    public float horizontalToJumpBonus = 0.08f;

    [Tooltip("Bonus massimo aggiunto alla velocità Y del salto")]
    public float maxJumpBonus = 4f;

    [Header("── Dash ─────────────────────────────────────")]
    [Tooltip("Velocità del dash")]
    public float dashSpeed = 22f;

    [Tooltip("Durata del dash in secondi")]
    public float dashDuration = 0.18f;

    [Tooltip("Cooldown tra un dash e l'altro (modifica qui nell'editor)")]
    public float dashCooldown = 0.25f;

    [Tooltip("Quanti dash si possono fare prima di dover toccare terra")]
    public int maxDashes = 1;

    [Header("── Wall Jump ────────────────────────────────")]
    [Tooltip("Velocità di scivolamento lungo la parete")]
    public float wallSlideSpeed = 1.5f;

    [Tooltip("Forza verticale del wall jump")]
    public float wallJumpUpForce = 12f;

    [Tooltip("Forza orizzontale (allontanamento dalla parete) del wall jump")]
    public float wallJumpOutForce = 10f;

    [Tooltip("Tempo in cui il movimento orizzontale è bloccato dopo un wall jump")]
    public float wallJumpLockTime = 0.2f;

    [Tooltip("Distanza di rilevamento della parete per lo sliding")]
    public float wallCheckDistance = 0.6f;

    [Header("── Wall Climb ───────────────────────────────")]
    [Tooltip("Velocità di movimento sulla parete (su/giù/destra/sinistra)")]
    public float climbSpeed = 5f;

    [Tooltip("Distanza di snap alla parete")]
    public float snapDistance = 0.55f;

    [Tooltip("Raggio di ricerca parete per l'attach (automatico col dash)")]
    public float climbSearchRadius = 1.1f;

    [Tooltip("Forza verticale del vault (supera il bordo della parete salendo)")]
    public float vaultUpForce = 10f;

    [Tooltip("Forza in avanti del vault")]
    public float vaultForwardForce = 5f;

    [Tooltip("Forza verticale del leap off (salto dalla parete con Jump)")]
    public float leapUpForce = 13f;

    [Tooltip("Forza orizzontale del leap off")]
    public float leapOutForce = 10f;

    [Header("── Stamina (Climb) ──────────────────────────")]
    [Tooltip("Stamina massima per arrampicarsi")]
    public float maxStamina = 1f;

    [Tooltip("Stamina consumata al secondo mentre si arrampica")]
    public float staminaDrainRate = 0.15f;

    [Tooltip("Stamina recuperata al secondo quando si è a terra")]
    public float staminaRechargeRate = 0.8f;

    [Header("── Ground Check ─────────────────────────────")]
    [Tooltip("Transform del punto di controllo a terra (mettilo ai piedi del personaggio)")]
    public Transform groundCheck;

    [Tooltip("Raggio della sfera di controllo a terra")]
    public float groundCheckRadius = 0.25f;

    [Tooltip("Layer dei terreni calpestabili")]
    public LayerMask groundLayer;

    [Header("── Wall Layer ──────────────────────────────")]
    [Tooltip("Layer delle pareti arrampicabili")]
    public LayerMask wallLayer;

    [Header("── Camera ───────────────────────────────────")]
    [Tooltip("Transform della camera (serve per il movimento relativo alla camera)")]
    public Transform cameraTransform;

    [Header("── Stamina UI ──────────────────────────────────")]
    [Tooltip("Riferimento allo StaminaUI (cerchio stamina world-space)")]
    public StaminaUI staminaUI;

    [Header("── Death / Respawn ──────────────────────────")]
    [Tooltip("Se il personaggio scende sotto questa coordinata Y, muore")]
    public float deathYThreshold = -10f;

    // ══════════════════════════════════════════════════════════
    //  PRIVATE STATE
    // ══════════════════════════════════════════════════════════

    private CharacterController cc;
    private PlayerStateMachine stateMachine;
    private PlayerInputHandler input;

    private Vector3 velocity;

    // Ground
    private bool isGrounded;

    // Coyote / Jump buffer
    private float coyoteTimer;
    private float jumpBufferTimer;
    private bool isJumping;

    // Wall slide
    private bool isTouchingWall;
    private Vector3 wallNormal;
    private bool isWallSliding;
    private Vector3 lastWallJumpNormal;
    private bool hasWallJump = true;
    private float wallJumpTimer;

    // Dash
    private int dashesLeft;
    private bool isDashing;
    private float dashTimer;
    private float dashCooldownTimer;
    private Vector3 dashDirection;

    // Climb
    private bool isClimbing;
    private Collider attachedWall;
    private Vector3 climbWallNormal;
    private Vector3 climbWallRight;
    private float currentStamina;
    private float climbGraceTimer;
    private const float CLIMB_GRACE = 0.15f;  // secondi di grazia dopo il dash

    // Respawn
    private Vector3 respawnPosition;

    // Freeze (es. durante dialogo NPC)
    private bool isFrozen = false;

    // Cooldown dopo LeapOff: evita re-attach immediato alla parete
    private int leapCooldownFrames = 0;

    // Grace timer per pendii — evita Fall state brevissimi su slope
    private float slopeGroundedTimer = 0f;
    private const float SLOPE_GRACE = 0.12f;

    // ══════════════════════════════════════════════════════════
    //  UNITY LIFECYCLE
    // ══════════════════════════════════════════════════════════

    void Awake()
    {
        cc = GetComponent<CharacterController>();
        stateMachine = GetComponent<PlayerStateMachine>();
        input = GetComponent<PlayerInputHandler>();

        dashesLeft = maxDashes;
        currentStamina = maxStamina;
        respawnPosition = transform.position;
    }

    void Update()
    {
        // ── Frozen: blocca tutto il movimento (es. dialogo NPC) ──
        if (isFrozen)
        {
            // Applica solo gravità minima per tenerlo a terra
            if (isGrounded) velocity.y = -2f;
            else velocity.y += gravity * Time.deltaTime;
            cc.Move(velocity * Time.deltaTime);
            return;
        }

        // ── Se stiamo arrampicando, gestiamo solo il climb ────
        if (isClimbing)
        {
            HandleClimbState();
            return;
        }

        // ── Update normale ────────────────────────────────────
        CheckGrounded();
        CheckWall();
        UpdateTimers();
        TryAttachToWall();   // tenta attach automatico col dash
        HandleWallSlide();
        HandleJump();
        HandleDash();
        HandleMovement();
        ApplyGravity();
        ApplyMove();
        RotateToFacing();
        UpdateStateMachine();
        RechargeStamina();

        // ── Morte per caduta ──────────────────────────────────
        if (transform.position.y < deathYThreshold)
        {
            if (LivesManager.Instance != null)
                LivesManager.Instance.LoseLife(); // gestisce vite + respawn + death screen
            else
                Die(); // fallback se LivesManager non è nella scena
        }
    }

    // ══════════════════════════════════════════════════════════
    //  GROUNDED / WALL CHECK
    // ══════════════════════════════════════════════════════════

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
                // Solo pareti verticali (non pavimenti/soffitti)
                if (Vector3.Dot(hit.normal, Vector3.up) < 0.3f)
                {
                    isTouchingWall = true;
                    wallNormal = hit.normal;
                    break;
                }
            }
        }
    }

    // ══════════════════════════════════════════════════════════
    //  TIMERS
    // ══════════════════════════════════════════════════════════

    void UpdateTimers()
    {
        jumpBufferTimer -= Time.deltaTime;
        dashCooldownTimer -= Time.deltaTime;
        climbGraceTimer -= Time.deltaTime;

        if (wallJumpTimer > 0f) wallJumpTimer -= Time.deltaTime;

        if (isGrounded)
        {
            coyoteTimer = coyoteTime;
            dashesLeft = maxDashes;
            isJumping = false;
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

        if (leapCooldownFrames > 0) leapCooldownFrames--;

        // Jump buffer: premi Jump → registra l'intenzione
        if (input.JumpPressed)
            jumpBufferTimer = jumpBufferTime;

        // Jump cut: rilasci Jump prima del picco → taglia la salita
        if (input.JumpReleased && velocity.y > 0f && isJumping)
            velocity.y *= jumpCutMultiplier;
    }

    // ══════════════════════════════════════════════════════════
    //  WALL SLIDE
    // ══════════════════════════════════════════════════════════

    void HandleWallSlide()
    {
        bool pushingIntoWall = isTouchingWall && input.MoveInput.sqrMagnitude > 0.01f;
        isWallSliding = pushingIntoWall && !isGrounded && velocity.y < 0f;

        if (isWallSliding)
            velocity.y = Mathf.Max(velocity.y, -wallSlideSpeed);
    }

    // ══════════════════════════════════════════════════════════
    //  JUMP
    // ══════════════════════════════════════════════════════════

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
        float baseJump = Mathf.Sqrt(jumpHeight * -2f * gravity);
        float hSpeed = new Vector3(velocity.x, 0f, velocity.z).magnitude;
        float bonus = Mathf.Min(hSpeed * horizontalToJumpBonus, maxJumpBonus);

        velocity.y = baseJump + bonus;
        jumpBufferTimer = 0f;
        coyoteTimer = 0f;
        isJumping = true;
    }

    void WallJump()
    {
        lastWallJumpNormal = wallNormal;

        Vector3 jumpDir = (wallNormal + Vector3.up).normalized;
        float outSpd = Mathf.Max(wallJumpOutForce,
                              new Vector3(velocity.x, 0f, velocity.z).magnitude * 0.6f);

        velocity = jumpDir * outSpd;
        velocity.y = wallJumpUpForce;
        jumpBufferTimer = 0f;
        isJumping = true;
        wallJumpTimer = wallJumpLockTime;
    }

    // ══════════════════════════════════════════════════════════
    //  DASH
    // ══════════════════════════════════════════════════════════

    void HandleDash()
    {
        if (!input.DashPressed) return;
        if (dashesLeft <= 0 || dashCooldownTimer > 0f || isDashing) return;

        // Calcola direzione del dash in base all'input relativo alla camera
        Vector3 camFwd = cameraTransform.forward; camFwd.y = 0f; camFwd.Normalize();
        Vector3 camRight = cameraTransform.right; camRight.y = 0f; camRight.Normalize();

        dashDirection = camFwd * input.MoveInput.y + camRight * input.MoveInput.x;

        // Se nessun input → dash nella direzione in cui guarda il personaggio
        if (dashDirection.sqrMagnitude < 0.01f)
            dashDirection = transform.forward;
        else
            dashDirection = dashDirection.normalized;

        dashesLeft--;
        isDashing = true;
        dashTimer = dashDuration;
        dashCooldownTimer = dashCooldown;
        velocity = dashDirection * dashSpeed;

        // Attiva la grazia per l'attach al muro
        climbGraceTimer = CLIMB_GRACE;
    }

    void StopDash()
    {
        isDashing = false;
        velocity.y = Mathf.Min(velocity.y, dashSpeed * 0.4f);
    }

    // ══════════════════════════════════════════════════════════
    //  MOVEMENT
    // ══════════════════════════════════════════════════════════

    void HandleMovement()
    {
        if (isDashing || wallJumpTimer > 0f) return;

        Vector3 camFwd = cameraTransform.forward; camFwd.y = 0f; camFwd.Normalize();
        Vector3 camRight = cameraTransform.right; camRight.y = 0f; camRight.Normalize();

        Vector3 inputDir = camFwd * input.MoveInput.y + camRight * input.MoveInput.x;
        if (inputDir.sqrMagnitude > 1f) inputDir.Normalize();

        float inputMag = inputDir.magnitude;

        if (isGrounded)
        {
            // A terra: accelera/decelerazione immediata
            Vector3 target = inputDir * moveSpeed;
            float accel = inputMag > 0.01f ? acceleration : deceleration;

            velocity.x = Mathf.MoveTowards(velocity.x, target.x, accel * Time.deltaTime);
            velocity.z = Mathf.MoveTowards(velocity.z, target.z, accel * Time.deltaTime);
        }
        else
        {
            // In aria: più controllo ma cap sulla velocità orizzontale
            Vector3 horizontal = new Vector3(velocity.x, 0f, velocity.z);

            if (inputMag > 0.01f)
                horizontal = Vector3.MoveTowards(horizontal, inputDir * moveSpeed, airAcceleration * Time.deltaTime);
            else
                horizontal = Vector3.MoveTowards(horizontal, Vector3.zero, airDeceleration * Time.deltaTime);

            velocity.x = horizontal.x;
            velocity.z = horizontal.z;
        }
    }

    // ══════════════════════════════════════════════════════════
    //  GRAVITY
    // ══════════════════════════════════════════════════════════

    void ApplyGravity()
    {
        if (isDashing) return;

        if (isGrounded && velocity.y < 0f) { velocity.y = -2f; return; }

        float gMult = velocity.y < 0f ? fallMultiplier : 1f;
        velocity.y += gravity * gMult * Time.deltaTime;
        velocity.y = Mathf.Max(velocity.y, -maxFallSpeed);
    }

    // ══════════════════════════════════════════════════════════
    //  APPLY MOVE + ROTATION
    // ══════════════════════════════════════════════════════════

    void ApplyMove() => cc.Move(velocity * Time.deltaTime);

    void RotateToFacing()
    {
        Vector3 flat = new Vector3(velocity.x, 0f, velocity.z);
        if (flat.sqrMagnitude > 0.1f)
        {
            Quaternion target = Quaternion.LookRotation(flat);
            transform.rotation = Quaternion.Slerp(transform.rotation, target, rotationSpeed * Time.deltaTime);
        }
    }

    // ══════════════════════════════════════════════════════════
    //  STATE MACHINE UPDATE
    // ══════════════════════════════════════════════════════════

    void UpdateStateMachine()
    {
        if (isDashing)
        {
            stateMachine.ChangeState(PlayerState.Dash);
            return;
        }

        // Aggiorna il grace timer per i pendii
        if (isGrounded)
            slopeGroundedTimer = SLOPE_GRACE;
        else if (slopeGroundedTimer > 0f)
            slopeGroundedTimer -= Time.deltaTime;

        if (!isGrounded)
        {
            // Entra in Fall/Jump solo se siamo davvero in aria da un po'
            // Questo evita che su pendii scoscesi entri brevemente in Fall
            bool trulyAirborne = slopeGroundedTimer <= 0f;

            if (velocity.y > 0f && isJumping)
                stateMachine.ChangeState(PlayerState.Jump);
            else if (trulyAirborne)
                stateMachine.ChangeState(PlayerState.Fall);
            // Se slopeGroundedTimer > 0 → rimane nello stato corrente (Idle/Run)
            return;
        }

        // A terra
        bool hasMovementInput = input.MoveInput.sqrMagnitude > 0.01f;
        bool isMoving = new Vector3(velocity.x, 0f, velocity.z).sqrMagnitude > 0.1f;

        if (hasMovementInput || isMoving)
            stateMachine.ChangeState(PlayerState.Run);
        else
            stateMachine.ChangeState(PlayerState.Idle);
    }

    // ══════════════════════════════════════════════════════════
    //  WALL CLIMB
    // ══════════════════════════════════════════════════════════

    /// <summary>
    /// Tenta di agganciarsi automaticamente a una parete dopo un dash.
    /// </summary>
    void TryAttachToWall()
    {
        // Solo durante la finestra di grazia dopo il dash
        if (climbGraceTimer <= 0f) return;
        // Blocca re-attach subito dopo un LeapOff
        if (leapCooldownFrames > 0) return;
        if (isGrounded) return;
        if (currentStamina <= 0f) return;
        if (isClimbing) return;

        Collider[] cols = Physics.OverlapSphere(transform.position, climbSearchRadius, wallLayer);
        if (cols.Length == 0) return;

        // Trova il collider più vicino
        Collider best = null;
        float bestDist = float.MaxValue;
        foreach (var c in cols)
        {
            float d = Vector3.Distance(transform.position, c.ClosestPoint(transform.position));
            if (d < bestDist) { bestDist = d; best = c; }
        }
        if (best == null) return;

        Vector3 toSurf = best.ClosestPoint(transform.position) - transform.position;
        toSurf.y = 0f;
        if (toSurf.sqrMagnitude < 0.001f) return;
        // Non è un soffitto/pavimento
        if (Mathf.Abs(Vector3.Dot(-toSurf.normalized, Vector3.up)) > 0.5f) return;

        AttachToWall(best, -toSurf.normalized);
    }

    void AttachToWall(Collider wall, Vector3 normal)
    {
        attachedWall = wall;
        climbWallNormal = normal;
        climbWallRight = Vector3.Cross(normal, Vector3.up).normalized;
        isClimbing = true;
        // NON ricaricare il dash qui — viene ricaricato solo atterrando a terra

        SnapToWall();
        transform.rotation = Quaternion.LookRotation(-climbWallNormal, Vector3.up);
        stateMachine.ChangeState(PlayerState.Climbing);
        staminaUI?.Show();
    }

    /// <summary>
    /// Gestisce tutto il movimento mentre si è in Climbing state.
    /// Chiamato ogni frame al posto dell'update normale.
    /// </summary>
    void HandleClimbState()
    {
        // ── USCITA: premi Jump → Leap Off ─────────────────────
        // NOTA: questo controlla JumpPressed qui perché siamo in Climbing
        // ed il normale HandleJump() non viene chiamato.
        if (input.JumpPressed)
        {
            LeapOff();
            return;
        }

        // ── Consuma stamina ───────────────────────────────────
        DrainStamina();
        staminaUI?.SetStamina(currentStamina / maxStamina);
        if (!isClimbing) return; // stamina esaurita → ExitClimb() già chiamato

        // ── Parete ancora presente? ───────────────────────────
        if (attachedWall == null) { ExitClimb(); return; }

        // ── A terra? → esci ───────────────────────────────────
        if (Physics.CheckSphere(
                transform.position + Vector3.down * (cc.height * 0.5f + 0.1f),
                0.2f, groundLayer))
        {
            ExitClimb();
            return;
        }

        // ── Controlla bordi / angoli ──────────────────────────
        CheckClimbEdgesAndWrap();
        if (!isClimbing) return;

        // ── Snap + ruota verso la parete ─────────────────────
        SnapToWall();
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            Quaternion.LookRotation(-climbWallNormal, Vector3.up),
            20f * Time.deltaTime);

        // ── Movimento sulla parete ────────────────────────────
        float h = input.MoveInput.x;
        float v = input.MoveInput.y;

        // Verticale
        if (Mathf.Abs(v) > 0.1f)
        {
            if (v > 0f)
            {
                // Controlla se siamo al bordo superiore → Vault
                float headY = transform.position.y + cc.height * 0.5f;
                float wallTop = attachedWall.bounds.max.y;
                if (headY + climbSpeed * Time.deltaTime >= wallTop)
                {
                    Vault();
                    return;
                }
            }

            cc.enabled = false;
            transform.position += Vector3.up * v * climbSpeed * Time.deltaTime;
            cc.enabled = true;
        }

        // Orizzontale — muovi sempre, poi CheckClimbEdgesAndWrap gestisce la transizione
        if (Mathf.Abs(h) > 0.1f)
        {
            Vector3 newPos = transform.position + climbWallRight * h * climbSpeed * Time.deltaTime;
            cc.enabled = false;
            transform.position = newPos;
            cc.enabled = true;

            // Dopo aver mosso, controlla se siamo usciti dalla faccia corrente
            // e transiziona alla faccia adiacente se necessario
            Vector3 toWall = attachedWall.ClosestPoint(transform.position) - transform.position;
            toWall.y = 0f;
            if (toWall.magnitude > snapDistance + 0.2f)
            {
                Vector3 sideDir = climbWallRight * Mathf.Sign(h);
                if (!TryTransitionAroundCorner(sideDir))
                    if (!TryTransitionToNearbyWall())
                        ExitClimb();
            }
        }

        // Applica anche il move del CC per evitare che venga "espulso"
        cc.Move(Vector3.zero);
    }

    // ── Vault: supera il bordo della parete ──────────────────
    void Vault()
    {
        Bounds b = attachedWall.bounds;
        cc.enabled = false;
        transform.position = new Vector3(
            transform.position.x,
            b.max.y + cc.height * 0.5f + 0.05f,
            transform.position.z);
        cc.enabled = true;

        attachedWall = null;
        isClimbing = false;
        velocity = -climbWallNormal * vaultForwardForce + Vector3.up * vaultUpForce;
        stateMachine.ChangeState(PlayerState.Jump);
    }

    // ── Leap off: salta via dalla parete ─────────────────────
    void LeapOff()
    {
        Vector3 leap = climbWallNormal * leapOutForce + Vector3.up * leapUpForce;
        isClimbing = false;
        attachedWall = null;
        velocity = leap;
        isJumping = true;
        jumpBufferTimer = 0f;

        // Blocca re-attach per 10 frame e azzera grace timer
        climbGraceTimer = 0f;
        leapCooldownFrames = 10;

        // Applica subito il movimento questo frame
        cc.Move(velocity * Time.deltaTime);
        staminaUI?.Hide();
        stateMachine.ChangeState(PlayerState.Jump);
    }

    // ── Exit climb senza salto (stamina esaurita / a terra) ──
    void ExitClimb()
    {
        isClimbing = false;
        attachedWall = null;
        velocity = Vector3.zero;
        staminaUI?.Hide();
    }

    // ── Snap alla parete ──────────────────────────────────────
    void SnapToWall()
    {
        if (attachedWall == null) return;

        Vector3 toSurf = attachedWall.ClosestPoint(transform.position) - transform.position;
        toSurf.y = 0f;
        if (toSurf.sqrMagnitude < 0.001f) return;

        float diff = toSurf.magnitude - snapDistance;
        if (Mathf.Abs(diff) > 0.01f)
        {
            cc.enabled = false;
            transform.position += toSurf.normalized * diff;
            cc.enabled = true;
        }
    }

    // ── Controlla bordi e angoli durante il climb ─────────────
    void CheckClimbEdgesAndWrap()
    {
        if (attachedWall == null) return;

        Vector3 toWall = attachedWall.ClosestPoint(transform.position) - transform.position;
        toWall.y = 0f;

        // Se troppo lontano dalla parete corrente → cerca un'altra
        if (toWall.magnitude > snapDistance + 0.6f)
        {
            if (!TryTransitionToNearbyWall()) ExitClimb();
            return;
        }

        float h = input.MoveInput.x;
        if (Mathf.Abs(h) < 0.1f) return;

        Vector3 sideDir = climbWallRight * Mathf.Sign(h);
        Vector3 sideProbe = transform.position + sideDir * (snapDistance + 0.4f);
        Vector3 cpSide = attachedWall.ClosestPoint(sideProbe);
        Vector3 sideDiff = cpSide - sideProbe; sideDiff.y = 0f;

        // Soglia più bassa per triggerare la transizione laterale
        if (sideDiff.magnitude > snapDistance + 0.15f)
            TryTransitionAroundCorner(sideDir);
    }

    bool TryTransitionAroundCorner(Vector3 cornerDir)
    {
        // Cerca su entrambi i layer (wallLayer E groundLayer) per trovare la faccia adiacente
        LayerMask combinedMask = wallLayer | groundLayer;
        Collider[] nearby = Physics.OverlapSphere(
            transform.position + cornerDir * 0.9f, 1.2f, combinedMask);

        Collider best = null;
        float bestScore = float.MaxValue;

        foreach (var col in nearby)
        {
            if (col == attachedWall) continue;
            Vector3 toSurf = col.ClosestPoint(transform.position) - transform.position;
            toSurf.y = 0f;
            if (toSurf.sqrMagnitude < 0.001f) continue;
            Vector3 norm = -toSurf.normalized;
            if (Mathf.Abs(Vector3.Dot(norm, Vector3.up)) > 0.5f) continue;
            if (Vector3.Dot(norm, climbWallNormal) > 0.95f) continue;

            float score = toSurf.magnitude;
            if (score < bestScore) { bestScore = score; best = col; }
        }

        if (best == null) return false;

        Vector3 t = best.ClosestPoint(transform.position) - transform.position;
        t.y = 0f;
        attachedWall = best;
        climbWallNormal = -t.normalized;
        climbWallRight = Vector3.Cross(climbWallNormal, Vector3.up).normalized;
        SnapToWall();
        transform.rotation = Quaternion.LookRotation(-climbWallNormal, Vector3.up);
        return true;
    }

    bool TryTransitionToNearbyWall()
    {
        LayerMask combinedMask = wallLayer | groundLayer;
        Collider[] nearby = Physics.OverlapSphere(transform.position, 1.2f, combinedMask);

        foreach (var col in nearby)
        {
            if (col == attachedWall) continue;
            Vector3 toSurf = col.ClosestPoint(transform.position) - transform.position;
            toSurf.y = 0f;
            if (toSurf.sqrMagnitude < 0.001f) continue;
            Vector3 norm = -toSurf.normalized;
            if (Mathf.Abs(Vector3.Dot(norm, Vector3.up)) > 0.5f) continue;

            attachedWall = col;
            climbWallNormal = norm;
            climbWallRight = Vector3.Cross(norm, Vector3.up).normalized;
            SnapToWall();
            transform.rotation = Quaternion.LookRotation(-climbWallNormal, Vector3.up);
            return true;
        }
        return false;
    }

    // ══════════════════════════════════════════════════════════
    //  STAMINA
    // ══════════════════════════════════════════════════════════

    void DrainStamina()
    {
        currentStamina = Mathf.MoveTowards(currentStamina, 0f, staminaDrainRate * Time.deltaTime);
        if (currentStamina <= 0f) ExitClimb();
    }

    void RechargeStamina()
    {
        if (!isGrounded) return;
        currentStamina = Mathf.MoveTowards(currentStamina, maxStamina, staminaRechargeRate * Time.deltaTime);
        // Aggiorna UI anche durante la ricarica (se visibile)
        staminaUI?.SetStamina(currentStamina / maxStamina);
    }

    /// <summary>Ricarica la stamina al massimo (chiamata da powerup/gem).</summary>
    public void RefillStamina() => currentStamina = maxStamina;

    /// <summary>Ricarica i dash (chiamata da powerup/gem).</summary>
    public void RefillDash() => dashesLeft = maxDashes;

    // ══════════════════════════════════════════════════════════
    //  DEATH / RESPAWN
    // ══════════════════════════════════════════════════════════

    public void SetRespawnPoint(Vector3 pos) => respawnPosition = pos;

    public void Die()
    {
        velocity = Vector3.zero;
        isClimbing = false;
        attachedWall = null;

        FallingPlatform.ResetAll();
        GetComponent<PlatformDetector>()?.ResetPlatformEffects();

        cc.enabled = false;
        transform.position = respawnPosition;
        cc.enabled = true;

        stateMachine.ChangeState(PlayerState.Idle);
        Debug.Log("[PlayerController3D] Morte! Respawn a " + respawnPosition);
    }

    // ══════════════════════════════════════════════════════════
    //  PUBLIC UTILITIES (usate da altri script se necessario)
    // ══════════════════════════════════════════════════════════

    public bool IsGrounded => isGrounded;
    public bool IsDashing => isDashing;
    public bool IsFrozen => isFrozen;

    /// <summary>Blocca movimento player (dialogo NPC).</summary>
    public void Freeze()
    {
        isFrozen = true;
        velocity.x = 0f;
        velocity.z = 0f;
        if (isClimbing) ExitClimb();
        stateMachine.ChangeState(PlayerState.Idle);
    }

    /// <summary>Riabilita il movimento player (fine dialogo).</summary>
    public void Unfreeze() => isFrozen = false;
    public bool IsClimbing => isClimbing;
    public float Stamina => currentStamina / maxStamina; // 0..1
    public void SetVelocity(Vector3 v) => velocity = v;
    public Vector3 GetVelocity() => velocity;

    // Chiamato automaticamente da CharacterController ad ogni collisione
    void OnControllerColliderHit(ControllerColliderHit hit)
    {
        // ── MushroomBounce ────────────────────────────────────
        MushroomBounce mushroom = hit.collider.GetComponent<MushroomBounce>();
        if (mushroom != null)
            mushroom.TryBounce(this, hit.normal);

        // ── PlatformDetector (FallingPlatform, IcePlatform) ───
        PlatformDetector pd = GetComponent<PlatformDetector>();
        if (pd != null)
            pd.OnHit(hit, !wasGroundedLastFrame);


    }

    private bool wasGroundedLastFrame = false;

    void LateUpdate()
    {
        wasGroundedLastFrame = isGrounded;
    }

    // ══════════════════════════════════════════════════════════
    //  GIZMOS (visualizzazione nell'editor)
    // ══════════════════════════════════════════════════════════

    void OnDrawGizmosSelected()
    {
        // Ground check sphere
        if (groundCheck != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }

        // Wall check rays
        Gizmos.color = Color.blue;
        Vector3[] dirs = { transform.forward, -transform.forward, transform.right, -transform.right };
        foreach (var d in dirs)
            Gizmos.DrawRay(transform.position, d * wallCheckDistance);

        // Climb search radius
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, climbSearchRadius);

        // Parete attaccata
        if (isClimbing && attachedWall != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawRay(transform.position, -climbWallNormal * snapDistance);
            Gizmos.color = Color.cyan;
            Gizmos.DrawRay(transform.position, climbWallRight * (snapDistance + 0.4f));
            Gizmos.DrawRay(transform.position, -climbWallRight * (snapDistance + 0.4f));
        }
    }
}