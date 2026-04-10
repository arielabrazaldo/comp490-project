using UnityEngine;
using TMPro;

public class MuteAllAudio : MonoBehaviour
{
    [SerializeField] private TMP_Text buttonText;

    private bool isMuted = false;

    private void Start()
    {
        UpdateButtonText();
    }

    public void ToggleMute()
    {
        isMuted = !isMuted;
        AudioListener.volume = isMuted ? 0f : 1f;
        UpdateButtonText();
    }

    private void UpdateButtonText()
    {
        if (buttonText != null)
        {
            buttonText.text = isMuted ? "Sound - OFF" : "Sound - ON";
            buttonText.color = isMuted ? Color.red : Color.green;
        }
    }
}