using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager instance;

    [Header("Sources")]
    public AudioSource bgmSource;
    public AudioSource[] sfxSources;

    private float masterVolume = 1f;
    private float bgmVolume = 1f;
    private float sfxVolume = 1f;

    private void Awake()
    {
        instance = this;
    }

    public void SetMasterVolume(float value)
    {
        masterVolume = Mathf.Clamp01(value);
        ApplyVolume();
    }

    public void SetBgmVolume(float value)
    {
        bgmVolume = Mathf.Clamp01(value);
        ApplyVolume();
    }

    public void SetSfxVolume(float value)
    {
        sfxVolume = Mathf.Clamp01(value);
        ApplyVolume();
    }

    private void ApplyVolume()
    {
        if (bgmSource != null)
            bgmSource.volume = masterVolume * bgmVolume;

        if (sfxSources != null)
        {
            for (int i = 0; i < sfxSources.Length; i++)
            {
                if (sfxSources[i] != null)
                    sfxSources[i].volume = masterVolume * sfxVolume;
            }
        }
    }

    public void PlaySfx(AudioClip clip)
    {
        if (clip == null)
            return;

        for (int i = 0; i < sfxSources.Length; i++)
        {
            if (sfxSources[i] != null && !sfxSources[i].isPlaying)
            {
                sfxSources[i].PlayOneShot(clip, masterVolume * sfxVolume);
                return;
            }
        }
    }
}