// ============================================================
//  MushroomBounce.cs
//
//  SETUP:
//  1. Aggiungi questo script alla testa del fungo
//  2. Il Collider della testa deve essere sul layer "Platform"
//     (lo stesso usato dal PlatformDetector)
//  3. Player tag = "Player"
//
//  FUNZIONAMENTO:
//  Il rilevamento avviene tramite OnControllerColliderHit
//  aggiunto al PlayerController3D — CharacterController
//  non trigga OnCollisionEnter quindi usiamo questo metodo.
// ============================================================

using UnityEngine;

public class MushroomBounce : MonoBehaviour
{
    [Header("── Rimbalzo ────────────────────────────────────")]
    [Tooltip("Forza verso l'alto applicata al player")]
    public float bounceForce = 18f;
    [Tooltip("Mantieni velocità orizzontale durante il rimbalzo")]
    public bool preserveHorizontalVelocity = true;

    [Header("── Animazione Fungo ────────────────────────────")]
    public float squishAmount = 0.3f;
    public float squishSpeed = 15f;
    public float recoverySpeed = 10f;

    private Vector3 originalScale;
    private Vector3 targetScale;

    // ──────────────────────────────────────────────────────────

    void Awake()
    {
        originalScale = transform.localScale;
        targetScale = originalScale;
    }

    void Update()
    {
        transform.localScale = Vector3.Lerp(
            transform.localScale, targetScale, squishSpeed * Time.deltaTime);
        targetScale = Vector3.Lerp(targetScale, originalScale, recoverySpeed * Time.deltaTime);
    }

    // ── Chiamato da PlayerController3D.OnControllerColliderHit ──
    public void TryBounce(PlayerController3D player, Vector3 hitNormal)
    {
        // OnControllerColliderHit: la normale punta VERSO il player
        // quindi quando atterra sopra, hitNormal.y è POSITIVO (> 0.5)
        if (hitNormal.y < 0.5f) return;

        Vector3 current = player.GetVelocity();

        // Solo se il player sta scendendo o è fermo verticalmente
        if (current.y > 1f) return;

        Vector3 newVel = preserveHorizontalVelocity
            ? new Vector3(current.x, bounceForce, current.z)
            : new Vector3(0f, bounceForce, 0f);

        player.SetVelocity(newVel);
        TriggerSquish();

        Debug.Log($"[MushroomBounce] Rimbalzo applicato! Forza: {bounceForce}");
    }

    void TriggerSquish()
    {
        targetScale = new Vector3(
            originalScale.x * (1f + squishAmount),
            originalScale.y * (1f - squishAmount),
            originalScale.z * (1f + squishAmount));
    }
}