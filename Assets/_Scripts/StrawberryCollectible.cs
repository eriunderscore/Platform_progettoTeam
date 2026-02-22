using UnityEngine;

public class StrawberryCollectible : MonoBehaviour
{
    public float bobHeight = 0.3f;
    public float bobSpeed = 2f;
    public float rotateSpeed = 90f;

    private Vector3 startPos;
    public static int collected = 0;

    void Start() => startPos = transform.position;

    void Update()
    {
        // Bob & spin
        float y = startPos.y + Mathf.Sin(Time.time * bobSpeed) * bobHeight;
        transform.position = new Vector3(startPos.x, y, startPos.z);
        transform.Rotate(Vector3.up, rotateSpeed * Time.deltaTime);
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            collected++;
            Debug.Log($"Strawberries: {collected}");
            Destroy(gameObject);
        }
    }
}