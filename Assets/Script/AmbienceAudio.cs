using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class AmbienceAudio : MonoBehaviour
{
    [Header("Ambience")]
    public AudioClip ambienceClip;
    [Range(0f, 1f)] public float volume = 0.6f;
    public bool playOnStart = true;
    public bool loop = true;

    [Header("Fade")]
    public bool fadeIn = true;
    public float fadeInDuration = 1.5f;

    AudioSource source;
    float targetVolume;

    void Awake()
    {
        source = GetComponent<AudioSource>();
        source.clip = ambienceClip;
        source.loop = loop;
        source.playOnAwake = false;
        source.spatialBlend = 0f; // 2D sound
        source.outputAudioMixerGroup = null; // assign a mixer group if you have one

        targetVolume = Mathf.Clamp01(volume);
        source.volume = fadeIn ? 0f : targetVolume;
    }

    void Start()
    {
        if (playOnStart && ambienceClip != null)
        {
            source.volume = fadeIn ? 0f : targetVolume;
            source.Play();
            if (fadeIn && targetVolume > 0f)
                StartCoroutine(FadeTo(targetVolume, fadeInDuration));
        }
    }

    System.Collections.IEnumerator FadeTo(float to, float duration)
    {
        float from = source.volume;
        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            source.volume = Mathf.Lerp(from, to, t / duration);
            yield return null;
        }
        source.volume = to;
    }

    // Optional: public methods
    public void SetVolume(float v) { volume = Mathf.Clamp01(v); source.volume = volume; }
    public void Play() { if (ambienceClip != null) source.Play(); }
    public void Stop() { source.Stop(); }
}
