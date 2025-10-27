using UnityEngine;

public class Fireball : MonoBehaviour
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
            // IMPORTANT: Preserve horizontal velocity when bouncing!
            float currentHorizontalVelocity = rb.linearVelocity.x;
            rb.linearVelocity = new Vector2(currentHorizontalVelocity, bounceForce);
        }

        if (collision.gameObject.CompareTag("Enemy"))
        {
            // Check if it's a boss
            Boss boss = collision.gameObject.GetComponent<Boss>();
            if (boss != null)
            {
                boss.TakeDamage(damage, DamageSource.Fireball);
            }
            else
            {
                // Regular enemy
                Enemy enemy = collision.gameObject.GetComponent<Enemy>();
                if (enemy != null)
                {
                    enemy.TakeDamage(damage);
                }
                else
                {
                    // Fallback - just destroy the enemy
                    Destroy(collision.gameObject);
                }
            }
            Destroy(gameObject);
        }
        
        // Handle Boss tag specifically
        if (collision.gameObject.CompareTag("Boss"))
        {
            Boss boss = collision.gameObject.GetComponent<Boss>();
            if (boss != null)
            {
                boss.TakeDamage(damage, DamageSource.Fireball);
            }
            Destroy(gameObject);
        }
    }
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        // Handle trigger-based collisions with Boss
        if (other.CompareTag("Boss") || other.CompareTag("Enemy"))
        {
            Boss boss = other.GetComponent<Boss>();
            if (boss != null)
            {
                boss.TakeDamage(damage, DamageSource.Fireball);
                Destroy(gameObject);
                return;
            }
            
            Enemy enemy = other.GetComponent<Enemy>();
            if (enemy != null)
            {
                enemy.TakeDamage(damage);
                Destroy(gameObject);
            }
        }
    }
}
