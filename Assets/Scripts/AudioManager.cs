using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [SerializeField] private bool testOnPlay = false; 
    private AudioClip sliceClip;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        sliceClip = CreateSliceClip();

        if (testOnPlay && sliceClip != null)
            PlayAtPoint(sliceClip, Vector3.zero, 0.8f);
    }

    public void PlaySliceSound(AudioSource source, float volume = 0.9f)
    {
        if (source == null) return;
        if (sliceClip == null) sliceClip = CreateSliceClip();
        source.PlayOneShot(sliceClip, volume);
    }

    private AudioClip CreateSliceClip()
    {
        int sampleRate = 44100;
        float duration = 0.12f;
        int samples = Mathf.CeilToInt(sampleRate * duration);

        float[] data = new float[samples];
        float fStart = 1100f, fEnd = 500f;
        for (int i = 0; i < samples; i++)
        {
            float t = (float)i / samples;
            float freq = Mathf.Lerp(fStart, fEnd, t);
            float env = Mathf.Exp(-6f * t);
            data[i] = Mathf.Sin(2f * Mathf.PI * freq * i / sampleRate) * env * 0.6f;
        }

        var clip = AudioClip.Create("SlicePew", samples, 1, sampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }

    public static void PlayAtPoint(AudioClip clip, Vector3 pos, float volume = 1f)
    {
        if (clip == null) return;
        var go = new GameObject("OneShotAudio");
        var src = go.AddComponent<AudioSource>();
        src.spatialBlend = 0f;
        src.clip = clip;
        src.volume = volume;
        src.Play();
        Object.Destroy(go, clip.length + 0.05f);
    }
}