using UnityEngine;

public class Enemy : MonoBehaviour
{
    public int health = 1;

    public void TakeDamage(int damage)
    {
        health -= damage;
        if (health <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        // Optional: play animation or sound here
        Destroy(gameObject);
    }
}
