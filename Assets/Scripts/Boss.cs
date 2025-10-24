using System.Diagnostics;
using UnityEngine;

public class Boss : MonoBehaviour
{
    public int maxHealth = 10;
    private int currentHealth;
    private Animator animator;

    void Start()
    {
        currentHealth = maxHealth;
        animator = GetComponent<Animator>();
    }

    public void TakeDamage(int amount)
    {
        currentHealth -= amount;

        // Optional: play hit animation
        if (animator != null)
            animator.SetTrigger("Hit");

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        //Debug.Log("Boss defeated!");

        // Optional: play death animation
        if (animator != null)
            animator.SetTrigger("Die");

        // Destroy after short delay to allow animation
        Destroy(gameObject, 0.5f);

        // Optional: trigger victory UI
        // FindObjectOfType<UIManager>().ShowVictoryScreen();
    }
}
