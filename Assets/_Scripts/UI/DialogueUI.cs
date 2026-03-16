// ============================================================
//  DialogueUI.cs  (Canvas UI + TextMeshPro)
//
//  Textbox con angoli arrotondati, font personalizzabile,
//  effetto macchina da scrivere, HUD nascosto durante dialogo.
//
//  SETUP HIERARCHY:
//
//  Canvas
//  └── DialoguePanel                    ← RectTransform, Image (bg arrotondato)
//      ├── SpeakerNamePanel             ← RectTransform, Image (bg nome)
//      │   └── SpeakerNameText          ← TextMeshProUGUI
//      ├── DialogueText                 ← TextMeshProUGUI
//      └── ContinueHint                 ← TextMeshProUGUI
//
//  PASSAGGI:
//  1. Crea la gerarchia sopra nel Canvas esistente
//  2. Su "DialoguePanel":
//     - Anchor: bottom-stretch (o top-stretch come preferisci)
//     - Aggiungi Image → assegna una sprite con angoli arrotondati
//       (usa la sprite "UISprite" built-in di Unity oppure una tua)
//       → Image Type: Sliced  (per mantenere gli angoli arrotondati)
//  3. Aggiungi questo script al GameObject "DialoguePanel"
//  4. Assegna tutti i riferimenti nell'Inspector
//
//  COME CREARE UNA SPRITE CON ANGOLI ARROTONDATI IN UNITY:
//  - Importa una PNG con angoli arrotondati come Sprite
//  - Oppure usa: Assets > Create > UI > Panel con Sprite arrotondata
//  - Nell'inspector della Sprite: imposta "Mesh Type: Full Rect"
//    e usa "Sprite Editor" per impostare i 9-slice borders
// ============================================================

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class DialogueUI : MonoBehaviour
{
    public static DialogueUI Instance { get; private set; }

    // ══════════════════════════════════════════════════════════
    //  INSPECTOR — Riferimenti UI
    // ══════════════════════════════════════════════════════════

    [Header("── Riferimenti UI ───────────────────────────────")]
    [Tooltip("Il pannello principale (Image con sfondo arrotondato)")]
    public RectTransform dialoguePanel;

    [Tooltip("Image del pannello — per cambiare colore/sprite sfondo")]
    public Image dialoguePanelImage;

    [Tooltip("Pannello del nome speaker (Image separata sopra la textbox)")]
    public Image speakerNamePanel;

    [Tooltip("Text TMP per il nome dell'NPC")]
    public TextMeshProUGUI speakerNameText;

    [Tooltip("Text TMP per il testo del dialogo")]
    public TextMeshProUGUI dialogueText;

    [Tooltip("Text TMP per l'hint 'Premi A per continuare'")]
    public TextMeshProUGUI continueHintText;

    // ══════════════════════════════════════════════════════════
    //  INSPECTOR — Stile sfondo
    // ══════════════════════════════════════════════════════════

    [Header("── Sfondo Textbox ────────────────────────────────")]
    [Tooltip("Sprite con angoli arrotondati per lo sfondo della textbox.\n" +
             "Usa Image Type: Sliced per mantenere gli angoli.\n" +
             "Lascia vuoto per usare il colore piatto.")]
    public Sprite dialogueBackgroundSprite;

    [Tooltip("Colore dello sfondo della textbox")]
    public Color dialogueBgColor = new Color(0.05f, 0.05f, 0.08f, 0.93f);

    [Tooltip("Sprite con angoli arrotondati per il pannello del nome")]
    public Sprite nameBackgroundSprite;

    [Tooltip("Colore del pannello del nome speaker")]
    public Color nameBgColor = new Color(0.1f, 0.3f, 0.6f, 0.95f);

    // ══════════════════════════════════════════════════════════
    //  INSPECTOR — Font
    // ══════════════════════════════════════════════════════════

    [Header("── Font ─────────────────────────────────────────")]
    [Tooltip("Font usato per il testo del dialogo.\n" +
             "Trascina qui un TMP_FontAsset dalla tua Project window.")]
    public TMP_FontAsset dialogueFont;

    [Tooltip("Font usato per il nome dell'NPC (può essere diverso)")]
    public TMP_FontAsset nameFont;

    [Tooltip("Font usato per l'hint 'Continua'")]
    public TMP_FontAsset hintFont;

    // ══════════════════════════════════════════════════════════
    //  INSPECTOR — Testo
    // ══════════════════════════════════════════════════════════

    [Header("── Testo ────────────────────────────────────────")]
    public int   dialogueFontSize = 22;
    public Color dialogueTextColor = Color.white;

    public int   nameFontSize = 20;
    public Color nameTextColor = new Color(1f, 1f, 1f, 1f);

    // ══════════════════════════════════════════════════════════
    //  INSPECTOR — Typewriter
    // ══════════════════════════════════════════════════════════

    [Header("── Typewriter ───────────────────────────────────")]
    [Tooltip("Caratteri al secondo")]
    public float typewriterSpeed = 40f;

    // ══════════════════════════════════════════════════════════
    //  INSPECTOR — Hint
    // ══════════════════════════════════════════════════════════

    [Header("── Hint Avanzamento ─────────────────────────────")]
    public string continueText = "▼  Continua";
    public string closeText    = "▼  Chiudi";
    public Color  hintColor    = new Color(1f, 1f, 1f, 0.55f);
    public int    hintFontSize = 14;

    // ══════════════════════════════════════════════════════════
    //  STATO INTERNO
    // ══════════════════════════════════════════════════════════

    private bool   isVisible    = false;
    private string fullText     = "";
    private bool   isTyping     = false;
    private bool   isLastLine   = false;
    private int    inputCooldownFrames = 0;

    private System.Action  onFinished;
    private Coroutine      typingCoroutine;
    private PlayerInputHandler input;

    // ══════════════════════════════════════════════════════════
    //  UNITY LIFECYCLE
    // ══════════════════════════════════════════════════════════

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        input = FindObjectOfType<PlayerInputHandler>();
        ApplyStyles();

        // Inizia nascosto
        SetPanelVisible(false);
    }

    void Update()
    {
        if (!isVisible || input == null) return;

        if (inputCooldownFrames > 0) { inputCooldownFrames--; return; }

        if (input.UIConfirmPressed)
        {
            if (isTyping)
            {
                // Salta animazione
                if (typingCoroutine != null) StopCoroutine(typingCoroutine);
                dialogueText.text = fullText;
                isTyping          = false;
                inputCooldownFrames = 2;
            }
            else
            {
                inputCooldownFrames = 3;
                onFinished?.Invoke();
            }
        }
    }

    // ══════════════════════════════════════════════════════════
    //  API PUBBLICA
    // ══════════════════════════════════════════════════════════

    /// <summary>Mostra una riga di dialogo.</summary>
    public void ShowLine(string speaker, string text, bool isLast = false,
                         System.Action onDone = null)
    {
        fullText   = text;
        isLastLine = isLast;
        onFinished = onDone;
        isVisible  = true;

        // Aggiorna nome speaker
        if (speakerNameText != null)
            speakerNameText.text = speaker;

        // Hint
        if (continueHintText != null)
            continueHintText.text = isLast ? closeText : continueText;

        SetPanelVisible(true);

        // Nascondi HUD
        HUDManager.Instance?.gameObject.SetActive(false);

        if (typingCoroutine != null) StopCoroutine(typingCoroutine);
        typingCoroutine = StartCoroutine(TypewriterRoutine());
    }

    /// <summary>Nasconde la textbox e ripristina il HUD.</summary>
    public void Hide()
    {
        isVisible = false;
        if (typingCoroutine != null) StopCoroutine(typingCoroutine);
        SetPanelVisible(false);

        // Ripristina HUD
        HUDManager.Instance?.gameObject.SetActive(true);
    }

    // ══════════════════════════════════════════════════════════
    //  TYPEWRITER
    // ══════════════════════════════════════════════════════════

    IEnumerator TypewriterRoutine()
    {
        isTyping = true;
        dialogueText.text = "";

        // Nascondi hint durante scrittura
        if (continueHintText != null)
            continueHintText.gameObject.SetActive(false);

        float delay = 1f / typewriterSpeed;
        foreach (char c in fullText)
        {
            dialogueText.text += c;
            yield return new WaitForSeconds(delay);
        }

        isTyping = false;

        // Mostra hint con blink
        if (continueHintText != null)
        {
            continueHintText.gameObject.SetActive(true);
            continueHintText.text = isLastLine ? closeText : continueText;
        }
    }

    // ══════════════════════════════════════════════════════════
    //  VISIBILITÀ PANNELLO
    // ══════════════════════════════════════════════════════════

    void SetPanelVisible(bool visible)
    {
        if (dialoguePanel != null)
            dialoguePanel.gameObject.SetActive(visible);
    }

    // ══════════════════════════════════════════════════════════
    //  APPLICA STILI (chiamato a Start e ogni volta che vuoi aggiornare)
    // ══════════════════════════════════════════════════════════

    /// <summary>
    /// Applica font, colori e sprite dall'Inspector.
    /// Puoi chiamarlo a runtime per aggiornare lo stile al volo.
    /// </summary>
    public void ApplyStyles()
    {
        // ── Sfondo textbox ────────────────────────────────────
        if (dialoguePanelImage != null)
        {
            dialoguePanelImage.color = dialogueBgColor;
            if (dialogueBackgroundSprite != null)
            {
                dialoguePanelImage.sprite    = dialogueBackgroundSprite;
                dialoguePanelImage.type      = Image.Type.Sliced; // mantiene angoli arrotondati
                dialoguePanelImage.pixelsPerUnitMultiplier = 1f;
            }
        }

        // ── Sfondo nome ───────────────────────────────────────
        if (speakerNamePanel != null)
        {
            speakerNamePanel.color = nameBgColor;
            if (nameBackgroundSprite != null)
            {
                speakerNamePanel.sprite = nameBackgroundSprite;
                speakerNamePanel.type   = Image.Type.Sliced;
            }
        }

        // ── Font testo dialogo ────────────────────────────────
        if (dialogueText != null)
        {
            if (dialogueFont != null) dialogueText.font = dialogueFont;
            dialogueText.fontSize  = dialogueFontSize;
            dialogueText.color     = dialogueTextColor;
        }

        // ── Font nome ─────────────────────────────────────────
        if (speakerNameText != null)
        {
            if (nameFont != null) speakerNameText.font = nameFont;
            speakerNameText.fontSize = nameFontSize;
            speakerNameText.color    = nameTextColor;
        }

        // ── Font hint ─────────────────────────────────────────
        if (continueHintText != null)
        {
            if (hintFont != null) continueHintText.font = hintFont;
            continueHintText.fontSize = hintFontSize;
            continueHintText.color    = hintColor;
        }
    }
}
