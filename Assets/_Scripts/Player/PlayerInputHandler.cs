// ============================================================
//  PlayerInputHandler.cs  (aggiornato — controller + UI)
//
//  NUOVI BINDING DA AGGIUNGERE nel tuo file PlayerInputActions:
//
//  Action Map: "Player" (già esistente)
//  ├── Move          → WASD / Frecce / Left Stick (Gamepad)
//  ├── Jump          → Space / Button South (A/Cross)
//  ├── Dash          → Left Shift / X / Button West (X/Square)
//  ├── Climb         → E / Button North (Y/Triangle)
//  └── LookDelta  ← NUOVO → Mouse Delta / Right Stick (Gamepad)
//
//  Action Map: "UI" ← NUOVO (crea questo Action Map)
//  ├── Navigate      → Arrow Keys / D-Pad / Left Stick
//  ├── Confirm       → Return / Button South (A/Cross)
//  └── Cancel        → Escape / Button East (B/Circle)
//
//  COME AGGIUNGERE I BINDING:
//  1. Apri PlayerInputActions
//  2. Nel Action Map "Player" aggiungi l'action "LookDelta":
//     - Type: Value, Control Type: Vector2
//     - Binding 1: Mouse Delta
//     - Binding 2: Right Stick [Gamepad]
//  3. Crea un nuovo Action Map "UI" e aggiungi:
//     - Navigate: Value, Vector2
//       → Arrow Keys (2D Vector Composite) + D-Pad [Gamepad] + Left Stick [Gamepad]
//     - Confirm: Button → Return/Enter + Button South [Gamepad]
//     - Cancel:  Button → Escape + Button East [Gamepad]
//  4. Salva l'asset
// ============================================================

using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInputHandler : MonoBehaviour
{
    [Header("Input Actions Asset")]
    [Tooltip("Trascina qui il file .inputactions che hai creato")]
    public InputActionAsset inputActionsAsset;

    [Header("Controller Camera Sensitivity")]
    [Tooltip("Sensibilità dello stick destro per la camera (gradi/secondo)")]
    public float controllerLookSensitivity = 200f;

    // ── Actions ───────────────────────────────────────────────
    private InputAction moveAction;
    private InputAction jumpAction;
    private InputAction dashAction;
    private InputAction climbAction;
    private InputAction lookDeltaAction;
    private InputAction uiNavigateAction;
    private InputAction uiConfirmAction;
    private InputAction uiCancelAction;
    private InputAction uiPauseAction;

    // ══════════════════════════════════════════════════════════
    //  PROPRIETÀ PUBBLICHE — Player
    // ══════════════════════════════════════════════════════════

    /// <summary>Direzione di movimento, clampata a 1.</summary>
    public Vector2 MoveInput { get; private set; }

    /// <summary>
    /// Delta di look della camera.
    /// Mouse: pixel raw (usati direttamente da OrbitCamera).
    /// Controller: stick destro scalato per sensibilità e deltaTime.
    /// </summary>
    public Vector2 LookDelta { get; private set; }

    public bool JumpPressed { get; private set; }
    public bool JumpHeld { get; private set; }
    public bool JumpReleased { get; private set; }
    public bool DashPressed { get; private set; }
    public bool ClimbPressed { get; private set; }

    // ══════════════════════════════════════════════════════════
    //  PROPRIETÀ PUBBLICHE — UI
    // ══════════════════════════════════════════════════════════

    /// <summary>Direzione di navigazione UI (D-Pad / stick sinistro / frecce).</summary>
    public Vector2 UINavigate { get; private set; }

    /// <summary>True nel frame in cui Confirm viene premuto (A / Cross / Invio).</summary>
    public bool UIConfirmPressed { get; private set; }

    /// <summary>True nel frame in cui Cancel viene premuto (B / Circle / Escape).</summary>
    public bool UICancelPressed { get; private set; }

    /// <summary>True nel frame in cui Pause viene premuto (Escape / Menu Xbox).</summary>
    public bool PausePressed { get; private set; }

    // ══════════════════════════════════════════════════════════
    //  UNITY LIFECYCLE
    // ══════════════════════════════════════════════════════════

    void Awake()
    {
        if (inputActionsAsset == null)
        {
            Debug.LogError("[PlayerInputHandler] Nessun Input Actions Asset assegnato!");
            enabled = false;
            return;
        }

        var playerMap = inputActionsAsset.FindActionMap("Player", throwIfNotFound: true);
        moveAction = playerMap.FindAction("Move", throwIfNotFound: true);
        jumpAction = playerMap.FindAction("Jump", throwIfNotFound: true);
        dashAction = playerMap.FindAction("Dash", throwIfNotFound: true);
        climbAction = playerMap.FindAction("Climb", throwIfNotFound: true);
        lookDeltaAction = playerMap.FindAction("LookDelta", throwIfNotFound: true);
        playerMap.Enable();

        var uiMap = inputActionsAsset.FindActionMap("UI", throwIfNotFound: true);
        uiNavigateAction = uiMap.FindAction("Navigate", throwIfNotFound: true);
        uiConfirmAction = uiMap.FindAction("Confirm", throwIfNotFound: true);
        uiCancelAction = uiMap.FindAction("Cancel", throwIfNotFound: true);
        uiPauseAction = uiMap.FindAction("Pause", throwIfNotFound: true);
        uiMap.Enable();
    }

    void Update()
    {
        // ── Movimento ─────────────────────────────────────────
        MoveInput = Vector2.ClampMagnitude(moveAction.ReadValue<Vector2>(), 1f);

        // ── Camera look ───────────────────────────────────────
        // Se un gamepad è connesso e lo stick destro è attuato → siamo in controller mode
        bool usingController = Gamepad.current != null &&
                               Gamepad.current.rightStick.ReadValue().magnitude > 0.1f;

        Vector2 rawLook = lookDeltaAction.ReadValue<Vector2>();

        // Controller: stick in range -1..1, moltiplichiamo per sensibilità e deltaTime
        // Mouse: già in pixel/frame, OrbitCamera usa Input.GetAxis internamente
        // → qui passiamo solo il valore raw e lasciamo che OrbitCamera lo gestisca
        LookDelta = usingController
            ? rawLook * controllerLookSensitivity * Time.deltaTime
            : rawLook;

        // ── Jump ──────────────────────────────────────────────
        JumpPressed = jumpAction.WasPressedThisFrame();
        JumpHeld = jumpAction.IsPressed();
        JumpReleased = jumpAction.WasReleasedThisFrame();

        // ── Dash / Climb ──────────────────────────────────────
        DashPressed = dashAction.WasPressedThisFrame();
        ClimbPressed = climbAction.WasPressedThisFrame();

        // ── UI ────────────────────────────────────────────────
        UINavigate = uiNavigateAction.ReadValue<Vector2>();
        UIConfirmPressed = uiConfirmAction.WasPressedThisFrame();
        UICancelPressed = uiCancelAction.WasPressedThisFrame();
        PausePressed = uiPauseAction.WasPressedThisFrame();
    }

    void OnDisable()
    {
        inputActionsAsset?.FindActionMap("Player")?.Disable();
        inputActionsAsset?.FindActionMap("UI")?.Disable();
    }
}