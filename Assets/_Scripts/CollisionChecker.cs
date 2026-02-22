using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class CollisionChecker : MonoBehaviour
{
    [Header("Detection")]
    public float detectionRadius = 1.0f;
    public LayerMask deathZoneLayer;
    public LayerMask checkpointLayer;

    private bool isDead = false;

    void OnEnable()
    {
        // Reset every time the player is re-enabled (respawn or scene reload)
        isDead = false;
    }

    void Update()
    {
        if (isDead) return;
        CheckDeathZones();
        CheckCheckpoints();
    }

    void CheckDeathZones()
    {
        Collider[] hits = Physics.OverlapSphere(
            transform.position, detectionRadius, deathZoneLayer);

        foreach (var hit in hits)
        {
            if (hit.GetComponent<DeathZone3D>() != null)
            {
                isDead = true;
                if (GameManager.Instance != null)
                    GameManager.Instance.PlayerDied();
                return;
            }
        }
    }

    void CheckCheckpoints()
    {
        Collider[] hits = Physics.OverlapSphere(
            transform.position, detectionRadius, checkpointLayer);

        foreach (var hit in hits)
        {
            Checkpoint3D cp = hit.GetComponent<Checkpoint3D>();
            if (cp != null && !cp.IsActivated)
                cp.Activate();
        }
    }

    // Called explicitly by GameManager after respawn as a safety net
    public void ResetDead() => isDead = false;

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}