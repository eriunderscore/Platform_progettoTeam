using System.Collections;
using UnityEngine;

/// <summary>
/// Attach this to the platform GameObject.
/// Detection is handled by PlatformDetector on the Player.
/// Call TriggerFall() from PlatformDetector when player lands on top.
/// </summary>
public class FallingPlatform : MonoBehaviour
{
    [Header("Timing")]
    public float shakeDelay = 1.2f;
    public float shakeIntensity = 0.05f;
    public float shakeSpeed = 40f;

    [Header("Fall")]
    public float fallSpeed = 12f;
    public float fallDistance = 30f;

    // ── Private ───────────────────────────────────────────────
    private Vector3 startPosition;
    private bool triggered = false;
    private bool isFalling = false;
    private float fallProgress = 0f;

    private static System.Collections.Generic.List<FallingPlatform> all
        = new System.Collections.Generic.List<FallingPlatform>();

    // ──────────────────────────────────────────────────────────

    void Awake()
    {
        startPosition = transform.position;
        all.Add(this);
    }

    void OnDestroy() { all.Remove(this); }

    void Update()
    {
        if (!isFalling) return;

        transform.position += Vector3.down * fallSpeed * Time.deltaTime;
        fallProgress += fallSpeed * Time.deltaTime;

        if (fallProgress >= fallDistance)
            gameObject.SetActive(false);
    }

    // ──────────────────────────────────────────────────────────
    //  CALLED BY PlatformDetector ON THE PLAYER
    // ──────────────────────────────────────────────────────────
    public void TriggerFall()
    {
        if (triggered) return;
        triggered = true;
        StartCoroutine(ShakeAndFall());
    }

    IEnumerator ShakeAndFall()
    {
        float elapsed = 0f;

        while (elapsed < shakeDelay)
        {
            elapsed += Time.deltaTime;
            float ox = Mathf.Sin(elapsed * shakeSpeed) * shakeIntensity;
            float oz = Mathf.Sin(elapsed * shakeSpeed * 0.7f) * shakeIntensity;
            transform.position = startPosition + new Vector3(ox, 0f, oz);
            yield return null;
        }

        transform.position = startPosition;
        isFalling = true;
    }

    // ──────────────────────────────────────────────────────────
    //  RESET ON PLAYER DEATH
    // ──────────────────────────────────────────────────────────
    public static void ResetAll()
    {
        foreach (var p in all)
        {
            if (p == null) continue;
            p.gameObject.SetActive(true);
            p.transform.position = p.startPosition;
            p.triggered = false;
            p.isFalling = false;
            p.fallProgress = 0f;
            p.StopAllCoroutines();
        }
    }
}