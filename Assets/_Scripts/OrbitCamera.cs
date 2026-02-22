using UnityEngine;

public class OrbitCamera : MonoBehaviour
{
    [Header("Target to Follow")]
    public Transform target;
    public Vector3 targetOffset = new Vector3(0, 1.5f, 0);

    [Header("Orbit Settings")]
    public float distance = 8f;
    public float mouseSensitivity = 3f;
    public float minPitch = -15f;
    public float maxPitch = 60f;
    public float smoothSpeed = 12f;

    [Header("Collision")]
    public LayerMask collisionMask;
    public float collisionRadius = 0.3f;
    public float minDistance = 2f;

    // Public so WallClimb can smoothly rotate camera on corner wrap
    public float Yaw { get; set; }
    public float Pitch { get; private set; }

    private float currentDistance;
    private Vector3 currentPosition;

    void Start()
    {
        if (target != null)
            Yaw = target.eulerAngles.y;

        Pitch = 20f;
        currentDistance = distance;
        currentPosition = transform.position;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void LateUpdate()
    {
        if (target == null)
        {
            Debug.LogWarning("OrbitCamera: No target assigned!");
            return;
        }

        HandleMouseInput();
        HandleCameraPosition();
    }

    void HandleMouseInput()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        Yaw += mouseX;
        Pitch -= mouseY;
        Pitch = Mathf.Clamp(Pitch, minPitch, maxPitch);
    }

    void HandleCameraPosition()
    {
        Vector3 pivot = target.position + targetOffset;
        Quaternion rotation = Quaternion.Euler(Pitch, Yaw, 0f);
        Vector3 desiredOffset = rotation * new Vector3(0f, 0f, -distance);
        Vector3 desiredPos = pivot + desiredOffset;

        // Collision — shorten distance if something is in the way
        float finalDistance = distance;
        Vector3 dirFromPivot = (desiredPos - pivot).normalized;

        if (Physics.SphereCast(pivot, collisionRadius, dirFromPivot,
            out RaycastHit hit, distance, collisionMask))
        {
            finalDistance = Mathf.Clamp(hit.distance - collisionRadius, minDistance, distance);
        }

        Vector3 finalPos = pivot + dirFromPivot * finalDistance;

        currentPosition = Vector3.Lerp(currentPosition, finalPos, smoothSpeed * Time.deltaTime);
        transform.position = currentPosition;
        transform.LookAt(pivot);
    }

    // Unlock / re-lock cursor
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        if (Input.GetMouseButtonDown(0) && Cursor.lockState == CursorLockMode.None)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }
}