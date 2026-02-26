using UnityEngine;

public class Checkpoint3D : MonoBehaviour
{
    private bool isActivated = false;
    public bool IsActivated => isActivated; // Per il CollisionChecker

    public void Activate() => isActivated = true; // Per il CollisionChecker

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerController3D player = other.GetComponent<PlayerController3D>();
            if (player != null)
            {
                // Dice al player: "Se muori, torna qui!"
                player.SetRespawnPoint(transform.position);

                if (!isActivated)
                {
                    isActivated = true;
                    Debug.Log("Checkpoint salvato in questa posizione!");
                    // Opzionale: cambia colore o fai un suono
                }
            }
        }
    }
}