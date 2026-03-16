// ============================================================
//  DeathScreen.cs
//
//  SETUP HIERARCHY:
//  Canvas
//  └── DeathScreen                  ← CanvasGroup + questo script
//      ├── Overlay                  ← Image nera semi-trasparente
//      ├── TitleText                ← TextMeshProUGUI "GAME OVER"
//      ├── RetryButton              ← Button component + Image
//      │   └── RetryText            ← TextMeshProUGUI
//      └── MenuButton               ← Button component + Image
//          └── MenuText             ← TextMeshProUGUI
//
//  IMPORTANTE: usa il componente Button di Unity sui bottoni
//  e collega OnClick() nell'Inspector a Retry() e MainMenu()
// ============================================================

using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class DeathScreen : MonoBehaviour
{
    public static DeathScreen Instance { get; private set; }

    // ══════════════════════════════════════════════════════════
    //  INSPECTOR
    // ══════════════════════════════════════════════════════════

    [Header("── Riferimenti UI ───────────────────────────────")]
    public CanvasGroup deathCanvasGroup;

    [Header("── Stile Bottoni ────────────────────────────────")]
    public Color colorNormal = new Color(0.12f, 0.12f, 0.12f, 0.9f);
    public Color colorSelected = new Color(0.9f, 0.9f, 0.9f, 0.97f);
    public Color textNormal = Color.white;
    public Color textSelected = Color.black;

    [Tooltip("Immagini dei bottoni: 0=Retry, 1=MainMenu")]
    public Image[] buttonImages;
    [Tooltip("Testi dei bottoni: 0=Retry, 1=MainMenu")]
    public TextMeshProUGUI[] buttonTexts;

    [Header("── Scene ────────────────────────────────────────")]
    public string mainMenuScene = "MainMenu";

    // ══════════════════════════════════════════════════════════
    //  STATO
    // ══════════════════════════════════════════════════════════

    public static bool IsVisible { get; private set; } = false;

    private int selectedIndex = 0;

    // ══════════════════════════════════════════════════════════

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        SetVisible(false);
    }

    void Start()
    {
        // Assicura bottoni aggiornati
        RefreshButtons();
    }

    // ══════════════════════════════════════════════════════════
    //  UPDATE — controller input
    // ══════════════════════════════════════════════════════════

    private float navCooldown = 0f;
    private const float NAV_DELAY = 0.18f;
    private int inputCooldownFrames = 0;

    void Update()
    {
        if (!IsVisible) return;
        if (inputCooldownFrames > 0) { inputCooldownFrames--; return; }

        // Navigazione con controller/tastiera (unscaled perché timeScale=0)
        navCooldown -= Time.unscaledDeltaTime;

        if (navCooldown <= 0f)
        {
            float navY = 0f;

            // Legge input direttamente dall'InputSystem senza dipendere dal PlayerInputHandler
            if (UnityEngine.InputSystem.Gamepad.current != null)
                navY = -UnityEngine.InputSystem.Gamepad.current.leftStick.ReadValue().y;

            if (UnityEngine.InputSystem.Keyboard.current != null)
            {
                if (UnityEngine.InputSystem.Keyboard.current.downArrowKey.wasPressedThisFrame) navY = 1f;
                if (UnityEngine.InputSystem.Keyboard.current.upArrowKey.wasPressedThisFrame) navY = -1f;
            }

            if (navY > 0.5f)
            {
                selectedIndex = (selectedIndex + 1) % 2;
                navCooldown = NAV_DELAY;
                RefreshButtons();
            }
            else if (navY < -0.5f)
            {
                selectedIndex = (selectedIndex - 1 + 2) % 2;
                navCooldown = NAV_DELAY;
                RefreshButtons();
            }
        }

        // Conferma con controller/tastiera
        bool confirm = false;
        if (UnityEngine.InputSystem.Gamepad.current != null)
            confirm = UnityEngine.InputSystem.Gamepad.current.buttonSouth.wasPressedThisFrame;
        if (UnityEngine.InputSystem.Keyboard.current != null)
            confirm |= UnityEngine.InputSystem.Keyboard.current.enterKey.wasPressedThisFrame;

        if (confirm)
        {
            inputCooldownFrames = 3;
            if (selectedIndex == 0) Retry();
            else MainMenu();
        }
    }

    // ══════════════════════════════════════════════════════════
    //  SHOW / HIDE
    // ══════════════════════════════════════════════════════════

    public void Show()
    {
        IsVisible = true;
        selectedIndex = 0;
        inputCooldownFrames = 10;
        navCooldown = 0.5f;

        Time.timeScale = 0f;
        HUDManager.Instance?.gameObject.SetActive(false);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        SetVisible(true);
        RefreshButtons();
    }

    void SetVisible(bool visible)
    {
        if (deathCanvasGroup == null) return;
        deathCanvasGroup.alpha = visible ? 1f : 0f;
        deathCanvasGroup.interactable = visible;
        deathCanvasGroup.blocksRaycasts = visible;
    }

    // ══════════════════════════════════════════════════════════
    //  BOTTONI — chiamati da Button.OnClick() nell'Inspector
    // ══════════════════════════════════════════════════════════

    public void Retry()
    {
        IsVisible = false;
        Time.timeScale = 1f;
        LivesManager.Instance?.ResetLives();
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void MainMenu()
    {
        IsVisible = false;
        Time.timeScale = 1f;
        LivesManager.Instance?.ResetLives();
        SceneManager.LoadScene(mainMenuScene);
    }

    // ══════════════════════════════════════════════════════════
    //  HOVER / SELEZIONE
    // ══════════════════════════════════════════════════════════

    public void OnRetryHover() { selectedIndex = 0; RefreshButtons(); }
    public void OnMenuHover() { selectedIndex = 1; RefreshButtons(); }

    void RefreshButtons()
    {
        for (int i = 0; i < 2; i++)
        {
            bool sel = (i == selectedIndex);
            if (buttonImages != null && i < buttonImages.Length && buttonImages[i] != null)
                buttonImages[i].color = sel ? colorSelected : colorNormal;
            if (buttonTexts != null && i < buttonTexts.Length && buttonTexts[i] != null)
                buttonTexts[i].color = sel ? textSelected : textNormal;
        }
    }
}