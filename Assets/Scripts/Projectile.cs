using UnityEngine;

public class Projectile : MonoBehaviour
{
    public float speed = 10f;
    public float bounceForce = 5f;
    public float lifetime = 5f;
    public int damage = 1;

    private Rigidbody2D rb;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            // Ensure proper physics settings for bouncy material
            rb.freezeRotation = true;
        }
        Destroy(gameObject, lifetime);
    }

    // Called by player when shooting
    public void Shoot(Vector2 velocity)
    {
        if (rb != null)
        {
            // Use the velocity directly
            rb.linearVelocity = velocity;
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            float currentHorizontalVelocity = rb.linearVelocity.x;
            rb.linearVelocity = new Vector2(currentHorizontalVelocity, bounceForce);
            Debug.Log("Projectile bounced off ground!");
            return; // Don't destroy, just bounce
        }

        // FIXED: Properly damage player
        if (collision.gameObject.CompareTag("Player"))
        {
            MyPlayerMovement player = collision.gameObject.GetComponent<MyPlayerMovement>();
            if (player != null && !player.IsInvincible())
            {
                Debug.Log("Projectile hit player!");
            }
            Destroy(gameObject);
        }
    }
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            MyPlayerMovement player = other.GetComponent<MyPlayerMovement>();
            if (player != null && !player.IsInvincible())
            {
                Debug.Log("Projectile hit player via trigger!");
            }
            Destroy(gameObject);
        }
    }
}
