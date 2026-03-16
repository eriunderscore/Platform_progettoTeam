// ============================================================
//  DialogueCameraController.cs
//
//  Gestisce il posizionamento cinematografico della camera
//  durante i dialoghi con gli NPC.
//
//  REGOLA DEI TERZI applicata:
//  ┌───────┬───────┬───────┐
//  │       │       │       │
//  │  [P]  │  [N]  │       │  ← P = Player (sinistra)
//  │       │       │       │     N = NPC   (centro)
//  └───────┴───────┴───────┘
//
//  La camera si posiziona lateralmente tra i due personaggi,
//  leggermente sopra e arretrata, guardando verso il centro
//  della scena tra player e NPC.
//
//  SETUP:
//  1. Aggiungi questo script a un GameObject vuoto chiamato
//     "DialogueCameraController" nella scena
//  2. Assegna la tua OrbitCamera al campo "orbitCamera"
//  3. Assegna il Transform del Player al campo "playerTransform"
//  4. Ogni NPC ha il suo NPCInteraction.cs che chiama
//     questo controller automaticamente
// ============================================================

using UnityEngine;

public class DialogueCameraController : MonoBehaviour
{
    // ── Singleton (facile da trovare dagli NPC) ───────────────
    public static DialogueCameraController Instance { get; private set; }

    [Header("── Riferimenti ──────────────────────────────────")]
    [Tooltip("La tua OrbitCamera")]
    public OrbitCamera orbitCamera;

    [Tooltip("Il Transform del personaggio giocante")]
    public Transform playerTransform;

    [Header("── Posizionamento Camera ────────────────────────")]
    [Tooltip("Distanza della camera dal punto medio tra player e NPC")]
    public float cameraDistance = 5f;

    [Tooltip("Altezza della camera rispetto al punto medio")]
    public float cameraHeight = 1.8f;

    [Tooltip("Offset laterale: sposta la camera di lato per la regola dei terzi")]
    [Range(-1f, 1f)]
    public float lateralOffset = -0.4f;

    [Tooltip("Velocità di transizione verso la posizione dialogo")]
    public float transitionSpeed = 5f;

    [Header("── Zoom ─────────────────────────────────────────")]
    [Tooltip("Field of View durante il dialogo (più basso = più zoom)")]
    public float dialogueFOV = 50f;

    [Tooltip("Field of View normale (quello che usi solitamente)")]
    public float normalFOV = 60f;

    [Tooltip("Velocità di transizione del FOV")]
    public float fovTransitionSpeed = 4f;

    // ── Stato interno ─────────────────────────────────────────
    private Camera cam;
    private bool   isActive = false;

    // ══════════════════════════════════════════════════════════

    void Awake()
    {
        // Singleton
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        cam = Camera.main;
        if (cam == null)
            Debug.LogError("[DialogueCameraController] Nessuna Main Camera trovata!");
    }

    void Update()
    {
        // Interpolazione del FOV ogni frame
        if (cam == null) return;

        float targetFOV = isActive ? dialogueFOV : normalFOV;
        cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, targetFOV, fovTransitionSpeed * Time.deltaTime);
    }

    // ══════════════════════════════════════════════════════════
    //  API PUBBLICA — chiamata da NPCInteraction
    // ══════════════════════════════════════════════════════════

    /// <summary>
    /// Attiva la camera cinematografica del dialogo.
    /// </summary>
    /// <param name="npcTransform">Il Transform dell'NPC con cui si sta parlando</param>
    public void StartDialogueCamera(Transform npcTransform)
    {
        if (orbitCamera == null || playerTransform == null || npcTransform == null)
        {
            Debug.LogError("[DialogueCameraController] Riferimenti mancanti! Controlla Inspector.");
            return;
        }

        isActive = true;

        Vector3    camPos = CalculateCameraPosition(npcTransform);
        Quaternion camRot = CalculateCameraRotation(npcTransform, camPos);

        orbitCamera.EnterDialogueMode(camPos, camRot, transitionSpeed);
    }

    /// <summary>
    /// Disattiva la camera cinematografica e torna all'orbita normale.
    /// </summary>
    public void StopDialogueCamera()
    {
        isActive = false;
        orbitCamera?.ExitDialogueMode();
    }

    /// <summary>
    /// Aggiorna la posizione durante il dialogo (se i personaggi si muovono).
    /// Chiamata ogni frame da NPCInteraction mentre il dialogo è attivo.
    /// </summary>
    public void UpdateDialogueCamera(Transform npcTransform)
    {
        if (!isActive || npcTransform == null) return;

        Vector3    camPos = CalculateCameraPosition(npcTransform);
        Quaternion camRot = CalculateCameraRotation(npcTransform, camPos);

        orbitCamera?.UpdateDialogueTarget(camPos, camRot);
    }

    // ══════════════════════════════════════════════════════════
    //  CALCOLO POSIZIONE CINEMATOGRAFICA
    // ══════════════════════════════════════════════════════════

    /// <summary>
    /// Calcola la posizione della camera applicando la regola dei terzi.
    ///
    /// Logica:
    /// 1. Trova il punto medio tra player e NPC
    /// 2. Calcola la direzione perpendicolare alla linea player→NPC
    /// 3. Sposta la camera lateralmente (lateralOffset) e in altezza
    /// 4. Arretra la camera della distanza impostata
    ///
    ///  Vista dall'alto:
    ///
    ///      Player ←────── midPoint ──────→ NPC
    ///                         │
    ///                    (perp dir)
    ///                         │
    ///                       [CAM]  ← si posiziona qui, leggermente a sinistra del midPoint
    /// </summary>
    Vector3 CalculateCameraPosition(Transform npcTransform)
    {
        Vector3 playerPos = playerTransform.position;
        Vector3 npcPos    = npcTransform.position;

        // Punto medio tra i due personaggi
        Vector3 midPoint = (playerPos + npcPos) * 0.5f;

        // Direzione dalla camera verso il midpoint (sul piano orizzontale)
        Vector3 playerToNpc = (npcPos - playerPos);
        playerToNpc.y = 0f;
        playerToNpc.Normalize();

        // Direzione perpendicolare (a destra della linea player→NPC)
        Vector3 perpendicular = Vector3.Cross(playerToNpc, Vector3.up).normalized;

        // Posizione base: arretrata rispetto al midpoint
        // La camera guarda lungo playerToNpc, quindi si posiziona "di lato"
        Vector3 cameraPos = midPoint
            - playerToNpc * cameraDistance              // arretra
            + perpendicular * (cameraDistance * lateralOffset)  // offset laterale regola dei terzi
            + Vector3.up * cameraHeight;                // altezza

        return cameraPos;
    }

    /// <summary>
    /// Calcola la rotazione della camera in modo che guardi
    /// verso il punto di interesse (leggermente verso l'NPC,
    /// che deve stare al centro dell'inquadratura).
    /// </summary>
    Quaternion CalculateCameraRotation(Transform npcTransform, Vector3 cameraPos)
    {
        // Il punto verso cui la camera guarda:
        // - leggermente spostato verso l'NPC (centro inquadratura)
        // - con un offset in altezza per inquadrare le teste
        Vector3 playerPos = playerTransform.position + Vector3.up * 1.5f;
        Vector3 npcPos    = npcTransform.position    + Vector3.up * 1.5f;

        // Punto di focus: 60% verso l'NPC (che deve essere al centro)
        // 40% verso il player (che deve essere a sinistra)
        Vector3 focusPoint = Vector3.Lerp(playerPos, npcPos, 0.6f);

        Vector3 lookDir = (focusPoint - cameraPos).normalized;
        if (lookDir == Vector3.zero) lookDir = Vector3.forward;

        return Quaternion.LookRotation(lookDir, Vector3.up);
    }

    // ══════════════════════════════════════════════════════════
    //  GIZMOS
    // ══════════════════════════════════════════════════════════

    void OnDrawGizmos()
    {
        if (playerTransform == null) return;

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(playerTransform.position, 0.3f);

#if UNITY_EDITOR
        UnityEditor.Handles.Label(playerTransform.position + Vector3.up * 2f, "Player");
#endif
    }
}
