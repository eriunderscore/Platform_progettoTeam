using System.Collections;
using UnityEngine;

/// <summary>
/// One-time collectible gem. On pickup:
///  - Plays a small pop animation then disappears
///  - Resets the player's dash (via PlayerController3D.RefillDash)
///  - Refills stamina fully (via WallClimb.CurrentStamina + StaminaUI)
/// </summary>
public class DashGem : MonoBehaviour
{
    [Header("─── References ───")]
    [Tooltip("Tag used to identify the player")]
    public string playerTag = "Player";

    [Header("─── Pop Animation ───")]
    [Tooltip("How long the pop scale animation lasts before destroying")]
    public float popDuration = 0.25f;
    [Tooltip("How much the gem scales up during the pop")]
    public float popScaleMultiplier = 1.6f;
   

    // ── Private ──
    private Vector3 _startPosition;
    private Vector3 _originalScale;
    private bool _collected = false;

    private void OnTriggerEnter(Collider other)
    {
        // Only collect once, only by the player
        if (_collected) return;
        if (!other.CompareTag(playerTag)) return;

        _collected = true;

        // Grab the components we need from the player
        PlayerController3D playerController = other.GetComponent<PlayerController3D>();
        WallClimb wallClimb = other.GetComponent<WallClimb>();

        // ── Refill Dash ──
        if (playerController != null)
            playerController.RefillDash();
        else
            Debug.LogWarning("DashGem: Could not find PlayerController3D on player!");

        // ── Refill Stamina ──
        if (wallClimb != null)
        {
            // Use reflection to set the private-set property CurrentStamina
            // We do this cleanly via a new public method we'll add to WallClimb
            wallClimb.RefillStamina();
        }
        else
            Debug.LogWarning("DashGem: Could not find WallClimb on player!");

        // ── Play pop then destroy ──
        StartCoroutine(PopAndDestroy());
    }

    private IEnumerator PopAndDestroy()
    {
        // Disable the collider immediately so it can't be collected twice
        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false;

        float elapsed = 0f;
        Vector3 targetScale = _originalScale * popScaleMultiplier;

        // Phase 1: Scale UP quickly
        while (elapsed < popDuration * 0.4f)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / (popDuration * 0.4f);
            transform.localScale = Vector3.Lerp(_originalScale, targetScale, t);
            yield return null;
        }

        // Phase 2: Scale DOWN to zero (disappear)
        elapsed = 0f;
        while (elapsed < popDuration * 0.6f)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / (popDuration * 0.6f);
            transform.localScale = Vector3.Lerp(targetScale, Vector3.zero, t);
            yield return null;
        }

        Destroy(gameObject);
    }
}