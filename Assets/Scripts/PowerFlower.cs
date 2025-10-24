using UnityEngine;

public class PowerFlower : MonoBehaviour
{
    public AudioClip pickupSound;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            // Grant power-up to player
            var player = collision.GetComponent<MyPlayerMovement>();
            if (player != null)
            {
                player.GiveFirePower(); // This method handles both fire power AND growth!
            }

            // Play pickup sound
            if (pickupSound != null)
            {
                AudioSource.PlayClipAtPoint(pickupSound, transform.position);
            }

            // Destroy flower after pickup
            Destroy(gameObject);
        }
    }
}
