using UnityEngine;
using UnityEngine.SceneManagement;

public class PipeScript : MonoBehaviour
{
    [Tooltip("Name of the scene to load. Make sure the scene is added to Build Settings.")]
    public string sceneToLoad;

    [Tooltip("Tag used to identify the player GameObject (default: Player).")]
    public string playerTag = "Player";

    // tracks whether the player is inside the pipe trigger
    private bool playerInside = false;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        // If the player is inside the trigger and presses S or DownArrow, load the target scene
        if (playerInside && !string.IsNullOrEmpty(sceneToLoad))
        {
            if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow))
            {
                SceneManager.LoadScene(sceneToLoad);
            }
        }
    }

    // Called when another collider enters this trigger (requires BoxCollider2D with "Is Trigger" checked)
    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag(playerTag))
            playerInside = true;
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag(playerTag))
            playerInside = false;
    }
}
