using UnityEngine;

public class AudioManager : MonoBehaviour
{
    [SerializeField] private AudioSource sfx;
    [SerializeField] private AudioClip flip;
    [SerializeField] private AudioClip match;
    [SerializeField] private AudioClip mismatch;
    [SerializeField] private AudioClip gameOver;

    public void PlayFlip() => Play(flip);
    public void PlayMatch() => Play(match);
    public void PlayMismatch() => Play(mismatch);
    public void PlayGameOver() => Play(gameOver);

    private void Play(AudioClip clip)
    {
        if (clip == null || sfx == null) return;
        sfx.PlayOneShot(clip);
    }
}
