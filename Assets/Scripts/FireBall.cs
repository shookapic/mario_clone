using UnityEngine;

public class Fireball : MonoBehaviour
{
    public float speed = 10f;
    public float bounceForce = 5f;
    public float lifetime = 5f;

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
            // IMPORTANT: Preserve horizontal velocity when bouncing!
            float currentHorizontalVelocity = rb.linearVelocity.x;
            rb.linearVelocity = new Vector2(currentHorizontalVelocity, bounceForce);
        }

        if (collision.gameObject.CompareTag("Enemy"))
        {
            Destroy(collision.gameObject);
            Destroy(gameObject);
        }
    }
}
