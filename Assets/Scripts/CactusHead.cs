using UnityEngine;

public class EnemyHeadCollider : MonoBehaviour
{
    public CactusScript parentEnemy; // Assign in inspector

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            Debug.Log("Enemy head collider triggered by Player");
            Destroy(parentEnemy.gameObject);

            Rigidbody2D rb = collision.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.AddForce(Vector2.up * 10f, ForceMode2D.Impulse);
            }
        }
    }
}
