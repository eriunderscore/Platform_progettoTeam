using UnityEngine;
using System;

public class NPCMenuUI : MonoBehaviour
{
    public static NPCMenuUI Instance { get; private set; }

    // ══════════════════════════════════════════════════════════
    //  INSPECTOR
    // ══════════════════════════════════════════════════════════

    [Header("── Posizione Bottoni ──────────────────────────────")]
    [Tooltip("Offset in pixel rispetto alla posizione screen dell'NPC\nX positivo = a destra, X negativo = a sinistra\nY positivo = in basso, Y negativo = in alto")]
    public Vector2 buttonOffset = new Vector2(20f, 0f);
    [Tooltip("Altezza world-space della testa dell'NPC (regola in base al modello)")]
    public float npcHeadHeight = 1.5f;

    [Header("── Dimensioni Bottoni ───────────────────────────")]
    public float buttonWidth = 200f;
    public float buttonHeight = 55f;
    public float buttonSpacing = 12f;

    [Header("── Colori Sfondo ──────────────────────────────────")]
    public Color colorNormal = new Color(0.12f, 0.12f, 0.12f, 0.88f);
    public Color colorHover = new Color(0.25f, 0.55f, 1f, 0.95f);
    public Color colorSelected = new Color(0.15f, 0.70f, 0.35f, 0.95f);

    [Header("── Colori Testo ────────────────────────────────────")]
    public Color colorTextNormal = Color.white;
    public Color colorTextHover = Color.white;
    public Color colorTextSelected = new Color(1f, 1f, 0.6f, 1f); // giallo chiaro di default
    public int fontSize = 18;

    [Header("── Etichette ────────────────────────────────────")]
    public string labelTalk = "💬  Parla";
    public string labelShop = "🛒  Negozio";
    public string labelExit = "✖   Esci";

    // ══════════════════════════════════════════════════════════
    //  STATO INTERNO
    // ══════════════════════════════════════════════════════════

    private bool isVisible = false;
    private int selectedIndex = 0;
    private const int BUTTON_COUNT_FULL = 3;
    private const int BUTTON_COUNT_NOSHOP = 2;
    private int ButtonCount => _showShop ? BUTTON_COUNT_FULL : BUTTON_COUNT_NOSHOP;

    private Action onTalk;
    private Action onShop;
    private Action onExit;

    private float navCooldown = 0f;
    private const float NAV_DELAY = 0.2f;
    private int inputCooldownFrames = 0;

    // Traccia quale bottone il mouse sta hovering (-1 = nessuno)
    private int hoveredIndex = -1;
    private bool _showShop = true;
    private Transform _npcTransform = null;

    private GUIStyle btnStyle;
    private bool styleReady = false;

    private PlayerInputHandler input;

    // ══════════════════════════════════════════════════════════

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        input = FindObjectOfType<PlayerInputHandler>();
        if (input == null)
            Debug.LogWarning("[NPCMenuUI] PlayerInputHandler non trovato nella scena.");
    }

    // ── API pubblica ──────────────────────────────────────────

    public void Show(Action talkCallback, Action shopCallback, Action exitCallback,
                     bool showShop = true, Transform npcTransform = null)
    {
        onTalk = talkCallback;
        onShop = shopCallback;
        onExit = exitCallback;
        selectedIndex = 0;
        isVisible = true;
        inputCooldownFrames = 3;
        _showShop = showShop;
        _npcTransform = npcTransform;
    }

    public void Hide() => isVisible = false;

    // ══════════════════════════════════════════════════════════
    //  UPDATE
    // ══════════════════════════════════════════════════════════

    void Update()
    {
        if (!isVisible || input == null) return;

        if (inputCooldownFrames > 0) { inputCooldownFrames--; return; }

        navCooldown -= Time.deltaTime;
        float navY = input.UINavigate.y;

        if (navCooldown <= 0f)
        {
            if (navY < -0.5f)
            {
                selectedIndex = (selectedIndex + 1) % ButtonCount;
                navCooldown = NAV_DELAY;
                hoveredIndex = -1; // reset hover quando si naviga con controller
            }
            else if (navY > 0.5f)
            {
                selectedIndex = (selectedIndex - 1 + ButtonCount) % ButtonCount;
                navCooldown = NAV_DELAY;
                hoveredIndex = -1;
            }
        }

        if (input.UIConfirmPressed)
        {
            inputCooldownFrames = 3;
            ActivateSelected();
        }

        if (input.UICancelPressed)
            onExit?.Invoke();
    }

    void ActivateSelected()
    {
        int toActivate = hoveredIndex >= 0 ? hoveredIndex : selectedIndex;

        if (_showShop)
        {
            switch (toActivate)
            {
                case 0: onTalk?.Invoke(); break;
                case 1: onShop?.Invoke(); break;
                case 2: onExit?.Invoke(); break;
            }
        }
        else
        {
            // Senza shop: 0=Talk, 1=Exit
            switch (toActivate)
            {
                case 0: onTalk?.Invoke(); break;
                case 1: onExit?.Invoke(); break;
            }
        }
    }

    // ══════════════════════════════════════════════════════════
    //  RENDERING
    // ══════════════════════════════════════════════════════════

    void OnGUI()
    {
        if (!isVisible) return;

        InitStyle();

        float totalH = ButtonCount * buttonHeight + (ButtonCount - 1) * buttonSpacing;

        float startX, startY;

        if (_npcTransform != null && Camera.main != null)
        {
            // Converti posizione NPC in screen space
            Vector3 worldPos = _npcTransform.position + Vector3.up * npcHeadHeight;
            Vector3 screenPos = Camera.main.WorldToScreenPoint(worldPos);

            // Se l'NPC è dietro la camera → nascondi
            if (screenPos.z < 0f) return;

            // Converti Y (screen space → GUI space)
            float guiY = Screen.height - screenPos.y;

            // Posiziona a destra dell'NPC, centrato verticalmente
            startX = screenPos.x + buttonOffset.x;
            startY = guiY - totalH * 0.5f + buttonOffset.y;
        }
        else
        {
            // Fallback: angolo in basso a sinistra
            startX = 30f;
            startY = Screen.height - totalH - 40f;
        }

        string[] labels = _showShop
            ? new string[] { labelTalk, labelShop, labelExit }
            : new string[] { labelTalk, labelExit };

        // Prima passata: aggiorna hoveredIndex e selectedIndex
        hoveredIndex = -1;
        for (int i = 0; i < ButtonCount; i++)
        {
            Rect rect = new Rect(startX, startY + i * (buttonHeight + buttonSpacing),
                                 buttonWidth, buttonHeight);
            if (rect.Contains(Event.current.mousePosition))
            {
                hoveredIndex = i;
                selectedIndex = i; // il selected diventa sempre l'ultimo hovered
            }
        }

        // Seconda passata: disegna
        for (int i = 0; i < ButtonCount; i++)
        {
            Rect rect = new Rect(startX, startY + i * (buttonHeight + buttonSpacing),
                                 buttonWidth, buttonHeight);

            bool isHovered = (hoveredIndex == i);
            bool isSelected = (i == selectedIndex) && !isHovered;

            // Colore sfondo
            Color bg = isHovered ? colorHover
                     : isSelected ? colorSelected
                     : colorNormal;

            // Colore testo
            Color tc = isHovered ? colorTextHover
                     : isSelected ? colorTextSelected
                     : colorTextNormal;

            DrawRoundedBox(rect, bg);

            btnStyle.normal.textColor = tc;
            GUI.Label(rect, labels[i], btnStyle);

            // Click mouse
            if (isHovered && Event.current.type == EventType.MouseUp &&
                Event.current.button == 0)
            {
                selectedIndex = i;
                ActivateSelected();
            }
        }
    }

    void DrawRoundedBox(Rect rect, Color color)
    {
        Color old = GUI.color;
        GUI.color = color;
        GUI.DrawTexture(rect, Texture2D.whiteTexture);
        GUI.color = old;
    }

    void InitStyle()
    {
        if (styleReady) return;
        btnStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = fontSize,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };
        styleReady = true;
    }
}