// ============================================================
//  OrbitCamera.cs  (aggiornato — controller + dialogo)
//
//  CAMBIAMENTI rispetto alla versione precedente:
//  - Rimosso Input.GetAxis("Mouse X/Y") → ora legge da
//    PlayerInputHandler.LookDelta (supporta controller)
//  - Il resto del comportamento è identico
// ============================================================

using UnityEngine;

public class OrbitCamera : MonoBehaviour
{
    [Header("Target to Follow")]
    public Transform target;
    public Vector3 targetOffset = new Vector3(0, 1.5f, 0);

    [Header("Orbit Settings")]
    public float distance = 8f;
    public float mouseSensitivity = 3f;
    [Tooltip("Il controller ha già la sensibilità applicata in PlayerInputHandler, questo è un moltiplicatore fine")]
    public float controllerSensitivityMultiplier = 1f;
    public float minPitch = -15f;
    public float maxPitch = 60f;
    public float smoothSpeed = 12f;

    [Header("Collision")]
    public LayerMask collisionMask;
    public float collisionRadius = 0.3f;
    public float minDistance = 2f;

    public float Yaw { get; set; }
    public float Pitch { get; private set; }

    // ── Stato dialogo ─────────────────────────────────────────
    private bool isInDialogueMode;
    private Vector3 dialogueTargetPosition;
    private Quaternion dialogueTargetRotation;
    private float dialogueTransitionSpeed = 5f;

    // ── Stato interno ─────────────────────────────────────────
    private float currentDistance;
    private Vector3 currentPosition;
    private PlayerInputHandler inputHandler;

    // ══════════════════════════════════════════════════════════

    void Start()
    {
        if (target != null)
            Yaw = target.eulerAngles.y;

        Pitch = 20f;
        currentDistance = distance;
        currentPosition = transform.position;

        // Cerca PlayerInputHandler automaticamente sul Player
        if (target != null)
            inputHandler = target.GetComponent<PlayerInputHandler>();

        if (inputHandler == null)
            Debug.LogWarning("[OrbitCamera] PlayerInputHandler non trovato sul target. " +
                             "Assicurati che il target sia il Player.");

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void LateUpdate()
    {
        if (target == null) return;

        if (isInDialogueMode)
        {
            transform.position = Vector3.Lerp(
                transform.position, dialogueTargetPosition,
                dialogueTransitionSpeed * Time.deltaTime);

            transform.rotation = Quaternion.Slerp(
                transform.rotation, dialogueTargetRotation,
                dialogueTransitionSpeed * Time.deltaTime);
        }
        else
        {
            HandleLookInput();
            HandleCameraPosition();
        }
    }

    void HandleLookInput()
    {
        if (inputHandler == null) return;

        // LookDelta contiene già il valore corretto sia per mouse che per controller
        // (vedi PlayerInputHandler per i dettagli)
        Vector2 look = inputHandler.LookDelta;

        // Per il mouse applichiamo la sensibilità qui (come prima)
        // Per il controller la sensibilità è già applicata in PlayerInputHandler,
        // quindi usiamo solo il moltiplicatore fine
        bool usingController = UnityEngine.InputSystem.Gamepad.current != null &&
                               UnityEngine.InputSystem.Gamepad.current.rightStick.ReadValue().magnitude > 0.1f;

        if (usingController)
        {
            Yaw += look.x * controllerSensitivityMultiplier;
            Pitch -= look.y * controllerSensitivityMultiplier;
        }
        else
        {
            Yaw += look.x * mouseSensitivity;
            Pitch -= look.y * mouseSensitivity;
        }

        Pitch = Mathf.Clamp(Pitch, minPitch, maxPitch);
    }

    void HandleCameraPosition()
    {
        Vector3 pivot = target.position + targetOffset;
        Quaternion rotation = Quaternion.Euler(Pitch, Yaw, 0f);
        Vector3 desiredPos = pivot + rotation * new Vector3(0f, 0f, -distance);

        float finalDistance = distance;
        Vector3 dirFromPivot = (desiredPos - pivot).normalized;

        if (Physics.SphereCast(pivot, collisionRadius, dirFromPivot,
            out RaycastHit hit, distance, collisionMask))
        {
            finalDistance = Mathf.Clamp(hit.distance - collisionRadius, minDistance, distance);
        }

        Vector3 finalPos = pivot + dirFromPivot * finalDistance;
        currentPosition = Vector3.Lerp(currentPosition, finalPos, smoothSpeed * Time.deltaTime);
        transform.position = currentPosition;
        transform.LookAt(pivot);
    }

    // ══════════════════════════════════════════════════════════
    //  DIALOGUE MODE API
    // ══════════════════════════════════════════════════════════

    public void EnterDialogueMode(Vector3 position, Quaternion rotation, float transitionSpeed = 5f)
    {
        isInDialogueMode = true;
        dialogueTargetPosition = position;
        dialogueTargetRotation = rotation;
        dialogueTransitionSpeed = transitionSpeed;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void UpdateDialogueTarget(Vector3 position, Quaternion rotation)
    {
        dialogueTargetPosition = position;
        dialogueTargetRotation = rotation;
    }

    public void ExitDialogueMode()
    {
        isInDialogueMode = false;

        Vector3 euler = transform.rotation.eulerAngles;
        Yaw = euler.y;
        Pitch = euler.x > 180f ? euler.x - 360f : euler.x;

        currentPosition = transform.position;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    // ══════════════════════════════════════════════════════════
    //  CURSOR (solo quando non in dialogo)
    // ══════════════════════════════════════════════════════════

    void Update()
    {
        if (isInDialogueMode) return;

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        if (Input.GetMouseButtonDown(0) && Cursor.lockState == CursorLockMode.None)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }
}
