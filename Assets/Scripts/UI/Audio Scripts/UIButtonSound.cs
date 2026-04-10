using UnityEngine;

public class UIButtonSound : MonoBehaviour
{
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip clickClip;
    [SerializeField] private AudioClip backClip;
    [SerializeField] private AudioClip customGridClip;
    [SerializeField] private AudioClip startGameClip;

    public void PlayClick()
    {
        if (audioSource != null && clickClip != null)
            audioSource.PlayOneShot(clickClip);
    }

    public void PlayBack()
    {
        if (audioSource != null && backClip != null)
            audioSource.PlayOneShot(backClip);
    }

    public void PlayCustomGrid()
    {
        if (audioSource != null && customGridClip != null)
            audioSource.PlayOneShot(customGridClip);
    }

    public void startGame()
    {
        if (audioSource != null && startGameClip != null)
            audioSource.PlayOneShot(startGameClip);
    }
}