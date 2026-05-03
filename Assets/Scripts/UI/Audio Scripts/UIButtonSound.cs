using UnityEngine;

public class UIButtonSound : MonoBehaviour
{
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip clickClip;
    [SerializeField] private AudioClip backClip;
    [SerializeField] private AudioClip customGridClip;
    [SerializeField] private AudioClip startGameClip;
    [SerializeField] private AudioClip resetClip;
    [SerializeField] private AudioClip boardEditorClip;
    [SerializeField] private AudioClip ruleEditorClip;
    [SerializeField] private AudioClip duplicateClip;

    // button press sound
    public void PlayClick()
    {
        if (audioSource != null && clickClip != null)
            audioSource.PlayOneShot(clickClip);
    }

    // back button sound
    public void PlayBack()
    {
        if (audioSource != null && backClip != null)
            audioSource.PlayOneShot(backClip);
    }

    // open custom grid sound
    public void PlayCustomGrid()
    {
        if (audioSource != null && customGridClip != null)
            audioSource.PlayOneShot(customGridClip);
    }

    // start button sound
    public void startGame()
    {
        if (audioSource != null && startGameClip != null)
            audioSource.PlayOneShot(startGameClip);
    }

    /*
     * 
     *  Board Editor/Rule Editor Sounds
     *  
     */

    // reset button sound
    public void Reset()
    {
        if (audioSource != null && resetClip != null)
            audioSource.PlayOneShot(resetClip);
    }

    // open rule editor sound
    public void RuleEditor()
    {
        if (audioSource != null && ruleEditorClip != null)
            audioSource.PlayOneShot(ruleEditorClip);
    }

    // open board editor sound
    public void BoardEditor()
    {
        if (audioSource != null && boardEditorClip != null)
            audioSource.PlayOneShot(boardEditorClip);
    }

    public void Duplicate()
    {
        if (audioSource != null && duplicateClip != null)
            audioSource.PlayOneShot(duplicateClip);
    }

}