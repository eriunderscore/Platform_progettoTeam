// ============================================================
//  PauseMenu.cs
//
//  Pause menu con 3 bottoni verticali: Return, Options, Quit.
//  Apri/chiudi: Escape (tastiera) o Menu Button Xbox (controller).
//
//  Quando aperto:
//  - Disattiva HUD
//  - Ferma il timer HUD
//  - Freezza il player
//  - Time.timeScale = 0 (ferma fisica e animazioni)
//
//  Quando chiuso (Return):
//  - Tutto viene ripristinato
//
//  SETUP HIERARCHY:
//
//  Canvas
//  └── PauseMenu                      ← questo script
//      ├── Overlay                    ← Image stretch-stretch, nero ~0.75 alpha
//      └── ButtonsContainer           ← RectTransform centrato verticalmente
//          ├── ReturnButton           ← Image arrotondata
//          │   └── ReturnText         ← TextMeshProUGUI
//          ├── OptionsButton          ← Image arrotondata
//          │   └── OptionsText        ← TextMeshProUGUI
//          └── QuitButton             ← Image arrotondata
//              └── QuitText           ← TextMeshProUGUI
//
//  SETUP INPUT ACTIONS:
//  Nel tuo PlayerInputActions, Action Map "UI":
//  - Aggiungi action "Pause": Button
//    → Escape (Keyboard)
//    → Start Button [Gamepad]  ← su Xbox è il tasto Menu (≡)
//  Poi in PlayerInputHandler aggiungi:
//    private InputAction pauseAction;
//    pauseAction = uiMap.FindAction("Pause", throwIfNotFound: true);
//    public bool PausePressed { get; private set; }
//    PausePressed = pauseAction.WasPressedThisFrame();
// ============================================================

using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Rendering;

public class PauseMenu : MonoBehaviour
{
    public static PauseMenu Instance { get; private set; }

    // ══════════════════════════════════════════════════════════
    //  INSPECTOR — Riferimenti
    // ══════════════════════════════════════════════════════════

    [Header("── Riferimenti UI ───────────────────────────────")]
    [Tooltip("Il CanvasGroup del pannello pause — usa CanvasGroup invece di SetActive\n" +
             "cosi lo script rimane sempre attivo e può ricevere input")]
    public CanvasGroup pauseCanvasGroup;

    [Tooltip("Le 3 Image dei bottoni: 0=Return, 1=Options, 2=Quit")]
    public Image[] buttonImages;

    [Tooltip("I 3 TextMeshPro dei bottoni: 0=Return, 1=Options, 2=Quit")]
    public TextMeshProUGUI[] buttonTexts;

    // ══════════════════════════════════════════════════════════
    //  INSPECTOR — Stile
    // ══════════════════════════════════════════════════════════

    [Header("── Stile Bottoni ─────────────────────────────────")]
    [Tooltip("Sprite con angoli arrotondati (opzionale, usa Sliced)")]
    public Sprite buttonSprite;

    public Color colorNormal = new Color(0.10f, 0.10f, 0.10f, 0.88f);
    public Color colorSelected = new Color(0.95f, 0.95f, 0.95f, 0.97f);
    public Color textNormal = new Color(1f, 1f, 1f, 1f);
    public Color textSelected = new Color(0f, 0f, 0f, 1f);

    public int fontSize = 28;

    [Tooltip("Font TMP per i bottoni (opzionale)")]
    public TMP_FontAsset buttonFont;

    [Header("── Etichette ────────────────────────────────────")]
    public string labelReturn = "Return";
    public string labelOptions = "Options";
    public string labelQuit = "Quit";

    [Header("── Defocus Overlay ─────────────────────────────")]
    [Tooltip("RawImage stretch-stretch sopra tutto")]
    public RawImage blurOverlay;
    [Tooltip("Quanti passaggi di downscale — più alto = più sfocato. Default 3")]
    [Range(1, 6)]
    public int defocusPasses = 3;

    // ══════════════════════════════════════════════════════════
    //  STATO INTERNO
    // ══════════════════════════════════════════════════════════

    public bool isPaused = false;
    private int selectedIndex = 0;
    private const int BTN_COUNT = 3;

    private float navCooldown = 0f;
    private const float NAV_DELAY = 0.18f;
    private int inputCooldownFrames = 0;

    private PlayerInputHandler input;
    private PlayerController3D playerController;
    private RenderTexture blurRT;
    private Texture2D blurTexture;

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
        playerController = FindObjectOfType<PlayerController3D>();

        ApplyLabelsAndFont();

        // Inizia nascosto (via CanvasGroup, non SetActive)
        SetVisible(false);

        // Nascondi blur overlay subito
        if (blurOverlay != null)
        {
            blurOverlay.color = new Color(1f, 1f, 1f, 0f);
            blurOverlay.texture = null;
        }
    }

    void Update()
    {
        // ── Toggle pause ──────────────────────────────────────
        bool escPressed = Input.GetKeyDown(KeyCode.Escape);
        bool menuBtnPressed = UnityEngine.InputSystem.Gamepad.current != null &&
                              UnityEngine.InputSystem.Gamepad.current.startButton.wasPressedThisFrame;

        if (escPressed || menuBtnPressed)
        {
            // Blocca pausa se la death screen è attiva
            if (DeathScreen.IsVisible) return;

            if (isPaused) Resume();
            else OpenPause();
            return;
        }

        if (!isPaused || input == null) return;

        // ── Cooldown input ────────────────────────────────────
        if (inputCooldownFrames > 0)
        {
            inputCooldownFrames--;
            return;
        }

        // ── Navigazione verticale (unscaledDeltaTime perché timeScale=0) ──
        navCooldown -= Time.unscaledDeltaTime;
        float navY = input.UINavigate.y;

        if (navCooldown <= 0f)
        {
            if (navY < -0.5f)
            {
                selectedIndex = (selectedIndex + 1) % BTN_COUNT;
                navCooldown = NAV_DELAY;
                RefreshButtons();
            }
            else if (navY > 0.5f)
            {
                selectedIndex = (selectedIndex - 1 + BTN_COUNT) % BTN_COUNT;
                navCooldown = NAV_DELAY;
                RefreshButtons();
            }
        }

        // ── Conferma ──────────────────────────────────────────
        if (input.UIConfirmPressed)
        {
            inputCooldownFrames = 3;
            ActivateSelected();
        }
    }

    // ══════════════════════════════════════════════════════════
    //  OPEN / CLOSE
    // ══════════════════════════════════════════════════════════

    public void OpenPause()
    {
        isPaused = true;
        selectedIndex = 0;
        inputCooldownFrames = 3;

        // Cattura lo schermo PRIMA di fermare il tempo
        CaptureBlur();

        // Ferma tutto
        Time.timeScale = 0f;
        HUDManager.Instance?.PauseTimer();
        HUDManager.Instance?.gameObject.SetActive(false);
        playerController?.Freeze();

        // Sblocca cursore
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        SetVisible(true);
        RefreshButtons();
    }

    public void Resume()
    {
        isPaused = false;

        // Riprendi tutto
        Time.timeScale = 1f;
        HUDManager.Instance?.ResumeTimer();
        HUDManager.Instance?.gameObject.SetActive(true);
        playerController?.Unfreeze();

        // Riblocca cursore
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        SetVisible(false);

        // Nascondi e libera blur overlay
        if (blurOverlay != null) { blurOverlay.color = new Color(1f, 1f, 1f, 0f); blurOverlay.texture = null; }
        if (blurTexture != null) { Destroy(blurTexture); blurTexture = null; }
    }

    // ══════════════════════════════════════════════════════════
    //  BOTTONI
    // ══════════════════════════════════════════════════════════

    void ActivateSelected()
    {
        switch (selectedIndex)
        {
            case 0: Resume(); break; // Return
            case 1: OnOptions(); break; // Options (placeholder)
            case 2: OnQuit(); break; // Quit
        }
    }

    void OnOptions()
    {
        // Placeholder — implementeremo dopo
        Debug.Log("[PauseMenu] Options — da implementare.");
    }

    void OnQuit()
    {
        // Ripristina timeScale prima di uscire
        Time.timeScale = 1f;

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // ══════════════════════════════════════════════════════════
    //  BLUR
    // ══════════════════════════════════════════════════════════

    void CaptureBlur()
    {
        StartCoroutine(CaptureBlurCoroutine());
    }

    System.Collections.IEnumerator CaptureBlurCoroutine()
    {
        yield return new WaitForEndOfFrame();

        if (blurOverlay == null) yield break;

        int w = Screen.width;
        int h = Screen.height;

        // 1. Cattura a piena risoluzione
        Texture2D screenshot = new Texture2D(w, h, TextureFormat.RGB24, false);
        screenshot.ReadPixels(new Rect(0, 0, w, h), 0, 0);
        screenshot.Apply();

        // 2. Downscale basato su defocusPasses
        int divisor = (int)Mathf.Pow(2, defocusPasses);
        int bw = Mathf.Max(1, w / divisor);
        int bh = Mathf.Max(1, h / divisor);
        RenderTexture rt1 = RenderTexture.GetTemporary(bw, bh, 0, RenderTextureFormat.Default);
        rt1.filterMode = FilterMode.Bilinear;
        Graphics.Blit(screenshot, rt1);

        // 3. Blur orizzontale
        RenderTexture rt2 = RenderTexture.GetTemporary(bw, bh, 0);
        rt2.filterMode = FilterMode.Bilinear;
        Graphics.Blit(rt1, rt2);
        RenderTexture.ReleaseTemporary(rt1);

        // 4. Upscale a piena risoluzione con bilinear → smooth defocus
        RenderTexture rtFull = RenderTexture.GetTemporary(w, h, 0);
        rtFull.filterMode = FilterMode.Bilinear;
        Graphics.Blit(rt2, rtFull);
        RenderTexture.ReleaseTemporary(rt2);

        // 5. Passa un'altra volta a meta risoluzione e poi ancora su
        //    Questo crea un effetto bokeh morbido senza pixelatura
        RenderTexture rtMid = RenderTexture.GetTemporary(w / 2, h / 2, 0);
        rtMid.filterMode = FilterMode.Bilinear;
        Graphics.Blit(rtFull, rtMid);
        RenderTexture.ReleaseTemporary(rtFull);

        RenderTexture rtFinal = RenderTexture.GetTemporary(w, h, 0);
        rtFinal.filterMode = FilterMode.Bilinear;
        Graphics.Blit(rtMid, rtFinal);
        RenderTexture.ReleaseTemporary(rtMid);

        // 6. Leggi il risultato finale in una Texture2D
        Texture2D result = new Texture2D(w, h, TextureFormat.RGB24, false);
        result.filterMode = FilterMode.Bilinear;
        RenderTexture.active = rtFinal;
        result.ReadPixels(new Rect(0, 0, w, h), 0, 0);
        result.Apply();
        RenderTexture.active = null;
        RenderTexture.ReleaseTemporary(rtFinal);

        Destroy(screenshot);
        if (blurTexture != null) Destroy(blurTexture);
        blurTexture = result;
        blurOverlay.texture = blurTexture;
        blurOverlay.color = new Color(1f, 1f, 1f, 1f);
    }

    // ══════════════════════════════════════════════════════════
    //  VISUALE
    // ══════════════════════════════════════════════════════════

    void SetVisible(bool visible)
    {
        if (pauseCanvasGroup == null) return;

        pauseCanvasGroup.alpha = visible ? 1f : 0f;
        pauseCanvasGroup.interactable = visible;
        pauseCanvasGroup.blocksRaycasts = visible;
    }

    void RefreshButtons()
    {
        if (buttonImages == null || buttonTexts == null) return;

        for (int i = 0; i < BTN_COUNT; i++)
        {
            bool sel = (i == selectedIndex);

            if (i < buttonImages.Length && buttonImages[i] != null)
            {
                buttonImages[i].color = sel ? colorSelected : colorNormal;

                if (buttonSprite != null)
                {
                    buttonImages[i].sprite = buttonSprite;
                    buttonImages[i].type = Image.Type.Sliced;
                }
            }

            if (i < buttonTexts.Length && buttonTexts[i] != null)
                buttonTexts[i].color = sel ? textSelected : textNormal;
        }
    }

    // ══════════════════════════════════════════════════════════
    //  SETUP ETICHETTE E FONT
    // ══════════════════════════════════════════════════════════

    void ApplyLabelsAndFont()
    {
        if (buttonTexts == null) return;

        string[] labels = { labelReturn, labelOptions, labelQuit };

        for (int i = 0; i < BTN_COUNT; i++)
        {
            if (i >= buttonTexts.Length || buttonTexts[i] == null) continue;

            buttonTexts[i].text = labels[i];
            buttonTexts[i].fontSize = fontSize;

            if (buttonFont != null)
                buttonTexts[i].font = buttonFont;
        }
    }

    // ══════════════════════════════════════════════════════════
    //  MOUSE SUPPORT
    // ══════════════════════════════════════════════════════════

    void OnGUI()
    {
        if (!isPaused || buttonImages == null) return;

        // Hover con mouse
        for (int i = 0; i < BTN_COUNT; i++)
        {
            if (i >= buttonImages.Length || buttonImages[i] == null) continue;

            // Converti rect Canvas in screen rect
            RectTransform rt = buttonImages[i].rectTransform;
            Vector3[] corners = new Vector3[4];
            rt.GetWorldCorners(corners);

            Rect screenRect = new Rect(
                corners[0].x,
                corners[0].y,
                corners[2].x - corners[0].x,
                corners[2].y - corners[0].y);

            Vector2 mousePos = new Vector2(Input.mousePosition.x, Input.mousePosition.y);

            if (screenRect.Contains(mousePos))
            {
                if (selectedIndex != i)
                {
                    selectedIndex = i;
                    RefreshButtons();
                }

                if (Input.GetMouseButtonDown(0))
                {
                    inputCooldownFrames = 3;
                    ActivateSelected();
                }
            }
        }
    }
}