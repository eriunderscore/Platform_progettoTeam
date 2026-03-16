// ============================================================
//  NPCInteraction.cs  (aggiornato — menu, dialogo, shop)
//
//  METTI QUESTO SCRIPT SU OGNI NPC.
//
//  SETUP:
//  1. Assegna "playerTransform" con il tuo Player
//  2. Scrivi le righe di dialogo nel campo "dialogueLines"
//     (ogni elemento = una riga; il nome speaker è "npcName")
//  3. Crea dei ShopItem (Create > Game > Shop Item) e
//     assegnali alla lista "shopItems"
//  4. Regola "interactionRadius"
//
//  RICHIEDE NELLA SCENA:
//  - DialogueCameraController
//  - NPCMenuUI
//  - DialogueUI
//  - ShopUI
//  - Un CoinManager (o qualsiasi script con GetCoins/SpendCoins)
//    → per ora usa il placeholder interno se non ce l'hai ancora
// ============================================================

using UnityEngine;

public class NPCInteraction : MonoBehaviour
{
    // ══════════════════════════════════════════════════════════
    //  INSPECTOR
    // ══════════════════════════════════════════════════════════

    [Header("── Riferimenti ──────────────────────────────────")]
    [Tooltip("Il Transform del personaggio giocante")]
    public Transform playerTransform;

    [Header("── Identità NPC ─────────────────────────────────")]
    public string npcName = "Personaggio";

    [Header("── Interazione ──────────────────────────────────")]
    public float interactionRadius = 3f;
    public KeyCode interactKey = KeyCode.E;

    [Header("── Dialogo ─────────────────────────────────────")]
    [Tooltip("Righe di testo mostrate in sequenza quando si preme Talk")]
    [TextArea(2, 4)]
    public string[] dialogueLines = { "Ciao avventuriero!", "Come posso aiutarti?" };

    [Header("── Shop ─────────────────────────────────────────")]
    [Tooltip("Se true, mostra il bottone Shop nel menu di questo NPC")]
    public bool hasShop = false;
    [Tooltip("Item venduti da questo NPC (crea con Create > Game > Shop Item)")]
    public ShopItem[] shopItems;

    [Header("── Uscita ───────────────────────────────────────")]
    [Tooltip("Secondi di delay prima che il player possa saltare dopo aver chiuso il menu NPC")]
    public float exitJumpDelay = 0.3f;

    // Monete gestite da ContenitoreMonete.Instance

    // ══════════════════════════════════════════════════════════
    //  STATO INTERNO
    // ══════════════════════════════════════════════════════════

    private bool isPlayerInRange = false;
    private bool isInteracting = false;
    // Cooldown: evita che il frame di apertura venga letto come Confirm dai menu
    private int interactCooldownFrames = 0;
    // Cooldown dopo chiusura: evita riapertura immediata
    private int closeCooldownFrames = 0;
    private int dialogueLine = 0;

    private PlayerController3D playerController;
    private PlayerInputHandler inputHandler;
    private bool isPausedByPauseMenu = false;

    // GUI stile per il prompt "Premi E"
    private GUIStyle promptStyle;
    private bool promptStyleReady = false;

    // ══════════════════════════════════════════════════════════
    //  UNITY LIFECYCLE
    // ══════════════════════════════════════════════════════════

    void Start()
    {
        if (playerTransform != null)
        {
            playerController = playerTransform.GetComponent<PlayerController3D>();
            inputHandler = playerTransform.GetComponent<PlayerInputHandler>();
        }
    }

    void Update()
    {
        if (playerTransform == null) return;

        // Se il pause menu si apre durante un'interazione → sospendi
        if (isInteracting && PauseMenu.Instance != null)
        {
            if (PauseMenu.Instance.isPaused && !isPausedByPauseMenu)
            {
                isPausedByPauseMenu = true;
                NPCMenuUI.Instance?.Hide();
                DialogueUI.Instance?.Hide();
                ShopUI.Instance?.Hide();
            }
            else if (!PauseMenu.Instance.isPaused && isPausedByPauseMenu)
            {
                isPausedByPauseMenu = false;
                // Riapri il menu NPC quando si chiude la pausa
                BackToMenu();
            }
        }

        if (isPausedByPauseMenu) return;

        CheckPlayerDistance();

        // Countdown cooldown (aspetta 2 frame dopo apertura prima di leggere input UI)
        if (interactCooldownFrames > 0)
        {
            interactCooldownFrames--;
            return;
        }

        // Cooldown dopo chiusura: impedisce riapertura immediata
        if (closeCooldownFrames > 0)
        {
            closeCooldownFrames--;
        }
        else if (!isInteracting)
        {
            // Apri con E (tastiera) oppure A/Cross (controller via UIConfirm)
            bool pressedE = Input.GetKeyDown(interactKey);
            bool pressedConfirm = inputHandler != null && inputHandler.UIConfirmPressed;

            if (isPlayerInRange && (pressedE || pressedConfirm))
                StartInteraction();
        }
        else
        {
            // Se il player si allontana → chiudi tutto
            if (!isPlayerInRange)
                CloseAll();
        }
    }

    // ══════════════════════════════════════════════════════════
    //  DISTANZA
    // ══════════════════════════════════════════════════════════

    void CheckPlayerDistance()
    {
        float dist = Vector3.Distance(playerTransform.position, transform.position);
        isPlayerInRange = dist <= interactionRadius;
    }

    // ══════════════════════════════════════════════════════════
    //  INTERAZIONE PRINCIPALE
    // ══════════════════════════════════════════════════════════

    void StartInteraction()
    {
        isInteracting = true;

        // Aspetta 2 frame prima di leggere input UI — evita che lo stesso
        // frame in cui si apre l'interazione venga letto come "Confirm" nei menu
        interactCooldownFrames = 2;

        // Blocca il player
        playerController?.Freeze();

        // Gira l'NPC verso il player
        FacePlayer();

        // Avvia camera cinematografica
        DialogueCameraController.Instance?.StartDialogueCamera(transform);

        // Pausa timer HUD durante il dialogo
        HUDManager.Instance?.OnDialogueStart();

        // Abbassa musica durante interazione NPC
        MusicManager.Instance?.OnNPCInteractionStart();

        // Mostra il menu con i 3 bottoni
        NPCMenuUI.Instance?.Show(
            talkCallback: OpenDialogue,
            shopCallback: hasShop ? OpenShop : (System.Action)null,
            exitCallback: CloseAll,
            showShop: hasShop,
            npcTransform: transform
        );
    }

    // ── Talk ──────────────────────────────────────────────────

    void OpenDialogue()
    {
        NPCMenuUI.Instance?.Hide();
        dialogueLine = 0;
        ShowNextDialogueLine();
    }

    void ShowNextDialogueLine()
    {
        if (dialogueLines == null || dialogueLines.Length == 0)
        {
            BackToMenu();
            return;
        }

        bool isLast = dialogueLine >= dialogueLines.Length - 1;

        DialogueUI.Instance?.ShowLine(
            speaker: npcName,
            text: dialogueLines[dialogueLine],
            isLast: isLast,
            onDone: () =>
            {
                if (isLast)
                {
                    DialogueUI.Instance?.Hide();
                    CloseAll(); // fine dialogo → esce automaticamente
                }
                else
                {
                    dialogueLine++;
                    ShowNextDialogueLine();
                }
            }
        );
    }

    // ── Shop ──────────────────────────────────────────────────

    void OpenShop()
    {
        if (shopItems == null || shopItems.Length == 0)
        {
            Debug.Log($"[NPCInteraction] {npcName} non ha item nel negozio.");
            return;
        }

        NPCMenuUI.Instance?.Hide();

        ShopUI.Instance?.Show(
            shopItems: shopItems,
            npc: transform,
            coinsGetter: () => ContenitoreMonete.Instance != null ? ContenitoreMonete.Instance.Monete : 0,
            coinsSpender: (cost) => ContenitoreMonete.Instance?.SpendMonete(cost),
            closeCallback: BackToMenu
        );
    }

    // ── Torna al menu ─────────────────────────────────────────

    void BackToMenu()
    {
        DialogueUI.Instance?.Hide();
        ShopUI.Instance?.Hide();

        NPCMenuUI.Instance?.Show(
            talkCallback: OpenDialogue,
            shopCallback: hasShop ? OpenShop : (System.Action)null,
            exitCallback: CloseAll,
            showShop: hasShop,
            npcTransform: transform
        );
    }

    // ── Chiudi tutto ──────────────────────────────────────────

    void CloseAll()
    {
        isInteracting = false;
        isPausedByPauseMenu = false;
        closeCooldownFrames = 5;

        NPCMenuUI.Instance?.Hide();
        DialogueUI.Instance?.Hide();
        ShopUI.Instance?.Hide();

        DialogueCameraController.Instance?.StopDialogueCamera();

        // Unfreeze con piccolo delay per evitare salto accidentale
        StartCoroutine(UnfreezeWithDelay());

        // Riprende timer HUD
        HUDManager.Instance?.OnDialogueEnd();

        // Ripristina volume musica
        MusicManager.Instance?.OnNPCInteractionEnd();
    }

    System.Collections.IEnumerator UnfreezeWithDelay()
    {
        yield return new WaitForSeconds(exitJumpDelay);
        playerController?.Unfreeze();
    }

    // ══════════════════════════════════════════════════════════
    //  HELPERS
    // ══════════════════════════════════════════════════════════

    void FacePlayer()
    {
        Vector3 dir = playerTransform.position - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude > 0.001f)
            transform.rotation = Quaternion.LookRotation(dir);
    }

    // ══════════════════════════════════════════════════════════
    //  GUI — Prompt "Premi E per parlare"
    // ══════════════════════════════════════════════════════════

    void OnGUI()
    {
        if (!isPlayerInRange || isInteracting || Camera.main == null) return;

        if (!promptStyleReady)
        {
            promptStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 18,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.white }
            };
            promptStyleReady = true;
        }

        Vector3 screenPos = Camera.main.WorldToScreenPoint(transform.position + Vector3.up * 2.5f);
        if (screenPos.z < 0f) return;

        float guiX = screenPos.x;
        float guiY = Screen.height - screenPos.y;

        float w = 240f, h = 30f;

        // Ombra
        GUI.color = new Color(0, 0, 0, 0.6f);
        GUI.Label(new Rect(guiX - w / 2f + 1, guiY - h / 2f + 1, w, h),
                  $"[{interactKey} / A]  Parla con {npcName}", promptStyle);

        GUI.color = Color.white;
        GUI.Label(new Rect(guiX - w / 2f, guiY - h / 2f, w, h),
                  $"[{interactKey} / A]  Parla con {npcName}", promptStyle);
    }

    // ══════════════════════════════════════════════════════════
    //  GIZMOS
    // ══════════════════════════════════════════════════════════

    void OnDrawGizmosSelected()
    {
        Gizmos.color = isPlayerInRange ? Color.green : Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactionRadius);
#if UNITY_EDITOR
        UnityEditor.Handles.Label(transform.position + Vector3.up * 2f, $"NPC: {npcName}");
#endif
    }
}