using UnityEngine;

public class FlagPole : MonoBehaviour
{
    [Header("Win Settings")]
    public bool levelCompleted = false;
    
    private bool showWinMessage = false;
    
    void Start()
    {
    }

    void Update()
    {
        if (levelCompleted && Input.GetKeyDown(KeyCode.R))
        {
            RestartLevel();
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Player") && !levelCompleted)
        {
            CompleteLevel();
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player") && !levelCompleted)
        {
            CompleteLevel();
        }
    }

    private void CompleteLevel()
    {
        levelCompleted = true;
        showWinMessage = true;

    }
    void OnGUI()
    {
        if (!showWinMessage) return;

        GUIStyle winStyle = new GUIStyle(GUI.skin.label);
        winStyle.fontSize = 56;
        winStyle.fontStyle = FontStyle.Bold;
        winStyle.alignment = TextAnchor.MiddleCenter;
        winStyle.normal.textColor = Color.green;

        GUI.color = Color.black;
        GUI.Label(new Rect(Screen.width / 2 - 201, Screen.height / 2 - 81, 402, 162), "YOU WON!", winStyle);
        
        GUI.color = Color.green;
        GUI.Label(new Rect(Screen.width / 2 - 200, Screen.height / 2 - 80, 400, 160), "YOU WON!", winStyle);

        GUIStyle celebrationStyle = new GUIStyle(GUI.skin.label);
        celebrationStyle.fontSize = 20;
        celebrationStyle.alignment = TextAnchor.MiddleCenter;
        celebrationStyle.normal.textColor = Color.yellow;

        string celebrationText = "🎉 Congratulations! Level Complete! 🎉";
        GUI.color = Color.black;
        GUI.Label(new Rect(Screen.width / 2 - 199, Screen.height / 2 + 21, 402, 30), celebrationText, celebrationStyle);
        GUI.color = Color.yellow;
        GUI.Label(new Rect(Screen.width / 2 - 200, Screen.height / 2 + 20, 400, 30), celebrationText, celebrationStyle);

        GUIStyle restartStyle = new GUIStyle(GUI.skin.label);
        restartStyle.fontSize = 16;
        restartStyle.alignment = TextAnchor.MiddleCenter;
        restartStyle.normal.textColor = Color.white;
        
        GUI.color = Color.white;
    }

    private void RestartLevel()
    {
        levelCompleted = false;
        showWinMessage = false;
        
    }

    public bool IsLevelCompleted()
    {
        return levelCompleted;
    }

    public void ManualCompleteLevel()
    {
        CompleteLevel();
    }
}
