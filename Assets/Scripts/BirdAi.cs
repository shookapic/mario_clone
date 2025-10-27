using UnityEngine;

public class BirdAI : MonoBehaviour
{
    public Transform player;
    public Transform[] patrolPoints;

    [Header("Patrol Settings")]
    public float patrolSpeed = 2f;
    private int patrolIndex = 0;

    [Header("Dive Settings")]
    public float xTriggerRange = 0.5f;
    public float diveSpeed = 6f;
    public float returnSpeed = 3f;
    public float diveTargetY = -3f;
    private float originalY;

    private enum State { Patrol, Dive, Return }
    private State state = State.Patrol;

    private Rigidbody2D rb;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.isKinematic = true;
        if (patrolPoints.Length > 0)
            transform.position = patrolPoints[0].position;

        originalY = transform.position.y;
    }

    void Update()
    {
        switch (state)
        {
            case State.Patrol: UpdatePatrol(); CheckDiveTrigger(); break;
            case State.Dive: UpdateDive(); break;
            case State.Return: UpdateReturn(); break;
        }
    }

    void UpdatePatrol()
    {
        if (patrolPoints.Length == 0) return;

        Vector2 target = patrolPoints[patrolIndex].position;
        transform.position = Vector2.MoveTowards(transform.position, target, patrolSpeed * Time.deltaTime);

        if (Vector2.Distance(transform.position, target) < 0.05f)
            patrolIndex = (patrolIndex + 1) % patrolPoints.Length;
    }

    void CheckDiveTrigger()
    {
        if (player == null) return;

        float xDiff = Mathf.Abs(player.position.x - transform.position.x);
        if (xDiff < xTriggerRange)
        {
            state = State.Dive;
        }
    }

    void UpdateDive()
    {
        Vector2 pos = transform.position;

        pos.y = Mathf.MoveTowards(pos.y, diveTargetY, diveSpeed * Time.deltaTime);
        transform.position = pos;

        if (transform.position.y <= diveTargetY + 0.05f)
        {
            state = State.Return;
        }
    }

    void UpdateReturn()
    {
        Vector2 pos = transform.position;

        pos.y = Mathf.MoveTowards(pos.y, originalY, returnSpeed * Time.deltaTime);
        transform.position = pos;

        if (Mathf.Abs(transform.position.y - originalY) < 0.05f)
        {
            state = State.Patrol;
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(transform.position, new Vector3(transform.position.x, diveTargetY, 0));
        Gizmos.DrawWireCube(transform.position, new Vector3(xTriggerRange * 2, 0.5f, 0));
    }
}
