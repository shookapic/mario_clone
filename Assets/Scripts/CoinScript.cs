using UnityEngine;

public class Coin : MonoBehaviour
{
    private AudioSource audioSource;
    private bool collected = false;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            Debug.LogError("No AudioSource found on " + gameObject.name);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!collected && other.CompareTag("Player"))
        {
            GetComponent<SpriteRenderer>().enabled = false;
            collected = true;
            audioSource.Play();

            
            Destroy(gameObject, audioSource.clip.length);
        }
    }
}
