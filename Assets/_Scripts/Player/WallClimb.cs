using UnityEngine;

[RequireComponent(typeof(PlayerController3D))]
[RequireComponent(typeof(CharacterController))]
public class WallClimb : MonoBehaviour
{
    [Header("Climb")]
    public float climbSpeed = 5f;
    public float snapDistance = 0.55f;

    [Header("Stamina")]
    public float maxStamina = 1f;
    public float staminaDrainRate = 0.15f;  // per second while climbing
    public float staminaRechargeRate = 0.8f;   // per second while grounded

    [Header("Vault")]
    public float vaultUpForce = 10f;
    public float vaultForwardForce = 5f;

    [Header("Leap Off")]
    public float leapUpForce = 13f;
    public float leapOutForce = 10f;

    [Header("Layers")]
    public LayerMask wallLayer;
    public LayerMask groundLayer;

    [Header("Camera")]
    public OrbitCamera orbitCamera;
    public float cameraWrapSpeed = 5f;

    [Header("Stamina UI")]
    public StaminaUI staminaUI;   // drag StaminaUI object here

    // ── Public ────────────────────────────────────────────────
    public bool IsClimbing { get; private set; }
    public float CurrentStamina { get; private set; }

    // ── Private ───────────────────────────────────────────────
    private PlayerController3D player;
    private CharacterController cc;

    private Collider attachedWall;
    private Vector3 wallNormal;
    private Vector3 wallRight;

    private float graceTimer;
    private const float GRACE = 0.15f;

    private float targetYaw;
    private bool smoothingCamera;

    // ──────────────────────────────────────────────────────────

    void Awake()
    {
        player = GetComponent<PlayerController3D>();
        cc = GetComponent<CharacterController>();
        CurrentStamina = maxStamina;
    }

    void Update()
    {
        if (!IsClimbing)
        {
            if (player.IsDashing) graceTimer = GRACE;
            else if (graceTimer > 0f) graceTimer -= Time.deltaTime;

            TryAttach();
            RechargeStamina();
        }
    }

    // ──────────────────────────────────────────────────────────
    //  STAMINA
    // ──────────────────────────────────────────────────────────
    void DrainStamina()
    {
        CurrentStamina = Mathf.MoveTowards(CurrentStamina, 0f, staminaDrainRate * Time.deltaTime);
        if (staminaUI != null) staminaUI.SetStamina(CurrentStamina / maxStamina);

        // Out of stamina — fall off
        if (CurrentStamina <= 0f)
            ExitClimb();
    }

    void RechargeStamina()
    {
        if (!player.IsGrounded) return;
        CurrentStamina = Mathf.MoveTowards(CurrentStamina, maxStamina, staminaRechargeRate * Time.deltaTime);
        if (staminaUI != null) staminaUI.SetStamina(CurrentStamina / maxStamina);
    }

    // ──────────────────────────────────────────────────────────
    //  ATTACH
    // ──────────────────────────────────────────────────────────
    void TryAttach()
    {
        if (graceTimer <= 0f && !player.IsDashing) return;
        if (player.IsGrounded) return;
        if (CurrentStamina <= 0f) return;   // no stamina = can't attach

        Collider[] cols = Physics.OverlapSphere(transform.position, 1.1f, wallLayer);
        if (cols.Length == 0) return;

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
        if (Mathf.Abs(Vector3.Dot(-toSurf.normalized, Vector3.up)) > 0.5f) return;

        AttachToWall(best, -toSurf.normalized);
    }

    void AttachToWall(Collider wall, Vector3 normal)
    {
        attachedWall = wall;
        SetAxes(normal);
        SnapPlayerToWall();
        transform.rotation = Quaternion.LookRotation(-wallNormal, Vector3.up);
        IsClimbing = true;
        player.RefillDash();
        SetCameraTarget();

        if (staminaUI != null) staminaUI.Show();
    }

    void SetAxes(Vector3 normal)
    {
        wallNormal = normal;
        wallRight = Vector3.Cross(wallNormal, Vector3.up).normalized;
    }

    // ──────────────────────────────────────────────────────────
    //  SNAP
    // ──────────────────────────────────────────────────────────
    void SnapPlayerToWall()
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

    // ──────────────────────────────────────────────────────────
    //  CLIMB UPDATE
    // ──────────────────────────────────────────────────────────
    public void ClimbUpdate()
    {
        // 1. Jump exit
        if (Input.GetButtonDown("Jump"))
        {
            LeapOff();
            return;
        }

        // 2. Drain stamina every frame while climbing
        DrainStamina();
        if (!IsClimbing) return;   // stamina ran out mid-frame

        // 3. Camera smooth
        if (smoothingCamera && orbitCamera != null)
        {
            orbitCamera.Yaw = Mathf.LerpAngle(orbitCamera.Yaw, targetYaw,
                                               cameraWrapSpeed * Time.deltaTime);
            if (Mathf.Abs(Mathf.DeltaAngle(orbitCamera.Yaw, targetYaw)) < 1f)
                smoothingCamera = false;
        }

        // 4. Wall still exists?
        if (attachedWall == null) { ExitClimb(); return; }

        // 5. Ground check
        if (Physics.CheckSphere(
            transform.position + Vector3.down * (cc.height * 0.5f + 0.1f),
            0.2f, groundLayer))
        {
            ExitClimb();
            return;
        }

        // 6. Edge / corner check
        CheckEdgesAndWrap();
        if (!IsClimbing) return;

        // 7. Snap + rotate
        SnapPlayerToWall();
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            Quaternion.LookRotation(-wallNormal, Vector3.up),
            20f * Time.deltaTime);

        // 8. Input
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        // Vertical
        if (Mathf.Abs(v) > 0.1f)
        {
            if (v > 0f)
            {
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

        // Horizontal
        if (Mathf.Abs(h) > 0.1f)
        {
            Vector3 newPos = transform.position + wallRight * h * climbSpeed * Time.deltaTime;
            Vector3 cp = attachedWall.ClosestPoint(newPos);
            Vector3 diff = cp - newPos; diff.y = 0f;
            if (diff.magnitude <= snapDistance + 0.5f)
            {
                cc.enabled = false;
                transform.position = newPos;
                cc.enabled = true;
            }
        }
    }

    // ──────────────────────────────────────────────────────────
    //  EDGE / CORNER CHECK
    // ──────────────────────────────────────────────────────────
    void CheckEdgesAndWrap()
    {
        if (attachedWall == null) return;

        Vector3 toWall = attachedWall.ClosestPoint(transform.position) - transform.position;
        toWall.y = 0f;
        if (toWall.magnitude > snapDistance + 0.6f)
        {
            if (!TryTransitionToNearbyWall()) ExitClimb();
            return;
        }

        float h = Input.GetAxisRaw("Horizontal");
        if (Mathf.Abs(h) > 0.1f)
        {
            Vector3 sideDir = wallRight * Mathf.Sign(h);
            Vector3 sideProbe = transform.position + sideDir * (snapDistance + 0.4f);
            Vector3 cpSide = attachedWall.ClosestPoint(sideProbe);
            Vector3 sideDiff = cpSide - sideProbe; sideDiff.y = 0f;
            if (sideDiff.magnitude > snapDistance + 0.35f)
                TryTransitionAroundCorner(sideDir);
        }
    }

    bool TryTransitionAroundCorner(Vector3 cornerDir)
    {
        Collider[] nearby = Physics.OverlapSphere(
            transform.position + cornerDir * 0.9f, 1.2f, wallLayer);

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
            if (Vector3.Dot(norm, wallNormal) > 0.95f) continue;
            float score = toSurf.magnitude;
            if (score < bestScore) { bestScore = score; best = col; }
        }

        if (best == null) return false;

        Vector3 t = best.ClosestPoint(transform.position) - transform.position;
        t.y = 0f;
        attachedWall = best;
        SetAxes(-t.normalized);
        SnapPlayerToWall();
        transform.rotation = Quaternion.LookRotation(-wallNormal, Vector3.up);
        SetCameraTarget();
        return true;
    }

    bool TryTransitionToNearbyWall()
    {
        Collider[] nearby = Physics.OverlapSphere(transform.position, 1.2f, wallLayer);
        foreach (var col in nearby)
        {
            if (col == attachedWall) continue;
            Vector3 toSurf = col.ClosestPoint(transform.position) - transform.position;
            toSurf.y = 0f;
            if (toSurf.sqrMagnitude < 0.001f) continue;
            Vector3 norm = -toSurf.normalized;
            if (Mathf.Abs(Vector3.Dot(norm, Vector3.up)) > 0.5f) continue;
            attachedWall = col;
            SetAxes(norm);
            SnapPlayerToWall();
            transform.rotation = Quaternion.LookRotation(-wallNormal, Vector3.up);
            SetCameraTarget();
            return true;
        }
        return false;
    }

    // ──────────────────────────────────────────────────────────
    //  VAULT
    // ──────────────────────────────────────────────────────────
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
        IsClimbing = false;
        player.SetVelocity(-wallNormal * vaultForwardForce + Vector3.up * vaultUpForce);
        if (staminaUI != null) staminaUI.Hide();
    }

    // ──────────────────────────────────────────────────────────
    //  EXIT / LEAP
    // ──────────────────────────────────────────────────────────
    void ExitClimb()
    {
        IsClimbing = false;
        attachedWall = null;
        player.SetVelocity(Vector3.zero);
        if (staminaUI != null) staminaUI.Hide();
    }

    void LeapOff()
    {
        Vector3 leap = wallNormal * leapOutForce + Vector3.up * leapUpForce;
        IsClimbing = false;
        attachedWall = null;
        player.SetVelocity(leap);
        if (staminaUI != null) staminaUI.Hide();
    }

    // ──────────────────────────────────────────────────────────
    //  CAMERA
    // ──────────────────────────────────────────────────────────
    void SetCameraTarget()
    {
        if (orbitCamera == null) return;
        targetYaw = Mathf.Atan2(-wallNormal.x, -wallNormal.z) * Mathf.Rad2Deg;
        smoothingCamera = true;
    }

    // ──────────────────────────────────────────────────────────
    //  GIZMOS
    // ──────────────────────────────────────────────────────────
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, 1.1f);
        if (!IsClimbing) return;
        Gizmos.color = Color.red;
        Gizmos.DrawRay(transform.position, -wallNormal * snapDistance);
        Gizmos.color = Color.green;
        Gizmos.DrawRay(transform.position, wallRight * (snapDistance + 0.4f));
        Gizmos.DrawRay(transform.position, -wallRight * (snapDistance + 0.4f));
    }
}