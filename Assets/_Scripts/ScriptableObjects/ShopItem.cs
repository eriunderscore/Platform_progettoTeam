// ============================================================
//  ShopItem.cs  —  ScriptableObject
//
//  Definisce un oggetto vendibile dal negozio di un NPC.
//
//  COME CREARE UN ITEM:
//  1. Nella Project window: tasto destro
//     → Create > Game > Shop Item
//  2. Dagli un nome, icona, prezzo e descrizione
//  3. Assegnalo alla lista "shopItems" del componente NPCInteraction
// ============================================================

using UnityEngine;

[CreateAssetMenu(fileName = "NewShopItem", menuName = "Game/Shop Item")]
public class ShopItem : ScriptableObject
{
    [Header("── Identità ─────────────────────────────────────")]
    [Tooltip("Nome mostrato nello shop")]
    public string itemName = "Item";

    [Tooltip("Icona mostrata nello slot dello shop")]
    public Sprite icon;

    [Tooltip("Descrizione mostrata quando è selezionato")]
    [TextArea(2, 4)]
    public string description = "Descrizione dell'oggetto.";

    [Header("── Prezzo ───────────────────────────────────────")]
    [Tooltip("Costo in monete")]
    public int price = 10;

    [Header("── Disponibilità ────────────────────────────────")]
    [Tooltip("Quanti se ne possono acquistare (-1 = illimitato)")]
    public int maxPurchases = -1;
}
