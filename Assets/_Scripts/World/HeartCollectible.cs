// ============================================================
//  HeartCollectible.cs
//  Consumabile che aggiunge +1 vita (max 3).
//  Se vita già piena → 50 monete.
//
//  SETUP:
//  - Aggiungi al GameObject cuore
//  - Collider → Is Trigger ✅
//  - Player deve avere Tag "Player"
// ============================================================

using UnityEngine;

public class HeartCollectible : MonoBehaviour
{
    private bool collected = false;

    void OnTriggerEnter(Collider other)
    {
        if (collected) return;
        if (!other.CompareTag("Player")) return;

        collected = true;
        LivesManager.Instance?.AddLife();

        Destroy(gameObject);
    }
}
