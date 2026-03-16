using UnityEngine;

/// <summary>
/// Attach to any pickup item for a Minecraft-style floating animation:
/// constant 360° Y rotation + smooth up/down bobbing.
/// </summary>
public class FloatingItem : MonoBehaviour
{
    [Header("── Rotation ────────────────────────────────────")]
    [Tooltip("Degrees per second")]
    public float rotationSpeed = 180f;

    [Header("── Bobbing ─────────────────────────────────────")]
    [Tooltip("How high it bobs up and down")]
    public float bobHeight = 0.15f;
    [Tooltip("How fast it bobs")]
    public float bobSpeed  = 2f;

    private Vector3 startPosition;

    void Start()
    {
        startPosition = transform.position;
    }

    void Update()
    {
        // 360 rotation
        transform.Rotate(0f, rotationSpeed * Time.deltaTime, 0f, Space.World);

        // Up/down bob
        float newY = startPosition.y + Mathf.Sin(Time.time * bobSpeed) * bobHeight;
        transform.position = new Vector3(transform.position.x, newY, transform.position.z);
    }
}
