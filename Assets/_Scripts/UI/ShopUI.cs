// ============================================================
//  ShopUI.cs
//
//  Negozio orizzontale con slot quadrati che appare sopra l'NPC.
//  Navigazione Left/Right per scegliere, Confirm per comprare.
//
//  RICHIEDE: ShopItem.cs (ScriptableObject) per gli item.
//
//  SETUP:
//  Aggiungi questo script a un GameObject nella scena (es. "ShopUI").
// ============================================================

using UnityEngine;

public class ShopUI : MonoBehaviour
{
    public static ShopUI Instance { get; private set; }

    // ══════════════════════════════════════════════════════════
    //  INSPECTOR
    // ══════════════════════════════════════════════════════════

    [Header("── Slot ─────────────────────────────────────────")]
    public float slotSize      = 90f;
    public float slotSpacing   = 14f;
    [Tooltip("Altezza del pannello info sotto gli slot")]
    public float infoPanelH    = 80f;

    [Header("── Posizione (sopra l'NPC) ─────────────────────")]
    [Tooltip("Offset Y sopra la testa dell'NPC in screen space")]
    public float yOffsetAboveNPC = 160f;

    [Header("── Colori ───────────────────────────────────────")]
    public Color bgColor          = new Color(0.08f, 0.08f, 0.08f, 0.93f);
    public Color slotNormal       = new Color(0.18f, 0.18f, 0.18f, 1f);
    public Color slotSelected     = new Color(0.25f, 0.65f, 1f,    1f);
    public Color slotCantAfford   = new Color(0.6f,  0.15f, 0.15f, 1f);
    public Color textColor        = Color.white;
    public Color priceColor       = new Color(1f, 0.85f, 0.2f, 1f);
    public int   fontSize         = 15;

    // ══════════════════════════════════════════════════════════
    //  STATO INTERNO
    // ══════════════════════════════════════════════════════════

    private bool       isVisible     = false;
    private ShopItem[] items;
    private int        selectedIndex = 0;
    private Transform  npcTransform;

    // Contatore acquisti per item (per rispettare maxPurchases)
    private int[]      purchaseCount;

    // Callback per ottenere/spendere monete (assegnati da NPCInteraction)
    private System.Func<int>        getCoins;
    private System.Action<int>      spendCoins;
    private System.Action           onClose;

    // Input debounce
    private float navCooldown     = 0f;
    private const float NAV_DELAY = 0.18f;

    // Feedback acquisto
    private string feedbackMsg   = "";
    private float  feedbackTimer = 0f;

    private GUIStyle labelStyle;
    private GUIStyle priceStyle;
    private GUIStyle feedbackStyle;
    private bool     stylesReady = false;

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
    }

    // ── API pubblica ──────────────────────────────────────────

    /// <summary>Apre lo shop per l'NPC specificato.</summary>
    public void Show(ShopItem[] shopItems, Transform npc,
                     System.Func<int> coinsGetter, System.Action<int> coinsSpender,
                     System.Action closeCallback)
    {
        items          = shopItems;
        npcTransform   = npc;
        getCoins       = coinsGetter;
        spendCoins     = coinsSpender;
        onClose        = closeCallback;
        selectedIndex  = 0;
        purchaseCount  = new int[shopItems.Length];
        feedbackMsg    = "";
        isVisible      = true;
    }

    public void Hide()
    {
        isVisible = false;
    }

    // ══════════════════════════════════════════════════════════
    //  UPDATE
    // ══════════════════════════════════════════════════════════

    void Update()
    {
        if (!isVisible || input == null || items == null) return;

        navCooldown -= Time.deltaTime;
        feedbackTimer = Mathf.Max(0f, feedbackTimer - Time.deltaTime);

        float navX = input.UINavigate.x;

        if (navCooldown <= 0f)
        {
            if (navX > 0.5f)
            {
                selectedIndex = (selectedIndex + 1) % items.Length;
                navCooldown = NAV_DELAY;
            }
            else if (navX < -0.5f)
            {
                selectedIndex = (selectedIndex - 1 + items.Length) % items.Length;
                navCooldown = NAV_DELAY;
            }
        }

        // Acquista
        if (input.UIConfirmPressed)
            TryPurchase();

        // Chiudi
        if (input.UICancelPressed)
            onClose?.Invoke();
    }

    void TryPurchase()
    {
        if (items == null || selectedIndex >= items.Length) return;

        ShopItem item = items[selectedIndex];

        // Controlla limite acquisti
        if (item.maxPurchases >= 0 && purchaseCount[selectedIndex] >= item.maxPurchases)
        {
            ShowFeedback("Hai già acquistato il massimo!");
            return;
        }

        int coins = getCoins?.Invoke() ?? 0;
        if (coins < item.price)
        {
            ShowFeedback("Non hai abbastanza monete!");
            return;
        }

        spendCoins?.Invoke(item.price);
        purchaseCount[selectedIndex]++;
        ShowFeedback($"Acquistato: {item.itemName}!");

        // Qui puoi aggiungere la logica per dare l'item al player
        // Es: PlayerInventory.Instance.AddItem(item);
        Debug.Log($"[ShopUI] Acquistato {item.itemName} per {item.price} monete.");
    }

    void ShowFeedback(string msg)
    {
        feedbackMsg   = msg;
        feedbackTimer = 2f;
    }

    // ══════════════════════════════════════════════════════════
    //  RENDERING
    // ══════════════════════════════════════════════════════════

    void OnGUI()
    {
        if (!isVisible || items == null || Camera.main == null) return;

        InitStyles();

        // Calcola posizione in screen space sopra l'NPC
        Vector3 npcScreen = npcTransform != null
            ? Camera.main.WorldToScreenPoint(npcTransform.position + Vector3.up * 2.2f)
            : new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 1f);

        if (npcScreen.z < 0f) return; // NPC dietro la camera

        float totalSlotW = items.Length * slotSize + (items.Length - 1) * slotSpacing;
        float panelW     = totalSlotW + 24f;
        float panelH     = slotSize + infoPanelH + 20f;

        // Converti Y (screen space → GUI space)
        float guiCenterX = npcScreen.x;
        float guiY       = (Screen.height - npcScreen.y) - yOffsetAboveNPC - panelH;

        Rect panelRect = new Rect(guiCenterX - panelW * 0.5f, guiY, panelW, panelH);

        // Sfondo pannello
        DrawBox(panelRect, bgColor);

        // Slot
        float slotStartX = panelRect.x + 12f;
        float slotY      = panelRect.y + 10f;

        for (int i = 0; i < items.Length; i++)
        {
            Rect slotRect = new Rect(slotStartX + i * (slotSize + slotSpacing), slotY,
                                     slotSize, slotSize);

            bool canAfford = (getCoins?.Invoke() ?? 0) >= items[i].price;
            bool maxed     = items[i].maxPurchases >= 0 &&
                             purchaseCount[i] >= items[i].maxPurchases;

            Color slotColor = (i == selectedIndex) ? slotSelected
                            : (!canAfford || maxed)   ? slotCantAfford
                            :                           slotNormal;

            DrawBox(slotRect, slotColor);

            // Icona
            if (items[i].icon != null)
            {
                Rect iconRect = new Rect(slotRect.x + 8f, slotRect.y + 8f,
                                         slotSize - 16f, slotSize - 32f);
                GUI.DrawTexture(iconRect, items[i].icon.texture, ScaleMode.ScaleToFit);
            }
            else
            {
                // Placeholder testo se non c'è icona
                Rect nameRect = new Rect(slotRect.x + 4f, slotRect.y + slotSize * 0.3f,
                                         slotSize - 8f, slotSize * 0.4f);
                GUI.Label(nameRect, items[i].itemName, labelStyle);
            }

            // Prezzo
            Rect priceRect = new Rect(slotRect.x, slotRect.y + slotSize - 24f,
                                      slotSize, 22f);
            GUI.Label(priceRect, $"🪙 {items[i].price}", priceStyle);

            // Click mouse
            if (slotRect.Contains(Event.current.mousePosition))
            {
                if (Event.current.type == EventType.MouseMove)
                    selectedIndex = i;
                if (Event.current.type == EventType.MouseUp && Event.current.button == 0)
                {
                    selectedIndex = i;
                    TryPurchase();
                }
            }
        }

        // Pannello info item selezionato
        if (selectedIndex < items.Length)
        {
            ShopItem sel = items[selectedIndex];
            Rect infoRect = new Rect(panelRect.x + 12f, slotY + slotSize + 8f,
                                     panelW - 24f, infoPanelH - 12f);

            string infoText = feedbackTimer > 0f
                ? feedbackMsg
                : $"<b>{sel.itemName}</b>\n{sel.description}";

            GUI.Label(infoRect, infoText, labelStyle);
        }

        // Hint navigazione
        Rect hintRect = new Rect(panelRect.x, panelRect.y + panelH + 4f, panelW, 22f);
        GUI.Label(hintRect, "◀ ▶  Naviga   |   A / Invio  Acquista   |   B / Esc  Chiudi",
                  new GUIStyle(GUI.skin.label)
                  {
                      fontSize  = 11,
                      alignment = TextAnchor.MiddleCenter,
                      normal    = { textColor = new Color(1, 1, 1, 0.5f) }
                  });
    }

    void DrawBox(Rect rect, Color color)
    {
        Color old = GUI.color;
        GUI.color = color;
        GUI.DrawTexture(rect, Texture2D.whiteTexture);
        GUI.color = old;
    }

    void InitStyles()
    {
        if (stylesReady) return;

        labelStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize  = fontSize,
            wordWrap  = true,
            alignment = TextAnchor.MiddleCenter,
            richText  = true,
            normal    = { textColor = textColor }
        };

        priceStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize  = fontSize - 2,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            normal    = { textColor = priceColor }
        };

        feedbackStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize  = fontSize,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            normal    = { textColor = new Color(1f, 0.6f, 0.2f, 1f) }
        };

        stylesReady = true;
    }
}
