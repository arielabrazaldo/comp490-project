using UnityEngine;

public class MainMenuOpenSound : MonoBehaviour
{
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip menuOpenClip;

    private static bool hasPlayedOnce = false;

    private void OnEnable()
    {
        if (hasPlayedOnce)
            return;

        if (audioSource != null && menuOpenClip != null)
        {
            audioSource.PlayOneShot(menuOpenClip);
            hasPlayedOnce = true;
        }
    }
}