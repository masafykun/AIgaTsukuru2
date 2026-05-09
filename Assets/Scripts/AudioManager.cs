using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    AudioSource bgmSource;
    AudioSource sfxSource;

    AudioClip clipGoal;
    AudioClip clipWin;

    void Awake()
    {
        if (Instance) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        var src = GetComponents<AudioSource>();
        bgmSource = src[0];
        sfxSource = src[1];

        clipGoal = BuildGoalChime();
        clipWin  = BuildWinFanfare();

        if (bgmSource.clip != null) { bgmSource.loop = true; bgmSource.Play(); }
        else
        {
            // Procedural 8-bit BGM loop
            bgmSource.clip = BuildBGMLoop();
            bgmSource.loop = true;
            bgmSource.volume = 0.25f;
            bgmSource.Play();
        }
    }

    public void PlayGoal() => sfxSource.PlayOneShot(clipGoal, 0.85f);
    public void PlayWin()  => sfxSource.PlayOneShot(clipWin,  1f);

    // ── Procedural audio helpers ──────────────────────────────────────

    static float[] SineNote(float freq, float dur, int sr, float amp = 0.45f, float decay = 5f)
    {
        int n = (int)(sr * dur);
        var d = new float[n];
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)sr;
            d[i] = (Mathf.Sin(2f * Mathf.PI * freq * t)
                  + Mathf.Sin(2f * Mathf.PI * freq * 2f * t) * 0.3f)
                  * Mathf.Exp(-t * decay) * amp;
        }
        return d;
    }

    static AudioClip Concat(string name, float[][] segs, int sr)
    {
        int total = 0;
        foreach (var s in segs) total += s.Length;
        var data = new float[total];
        int pos = 0;
        foreach (var s in segs) { System.Array.Copy(s, 0, data, pos, s.Length); pos += s.Length; }
        var clip = AudioClip.Create(name, total, 1, sr, false);
        clip.SetData(data, 0);
        return clip;
    }

    static AudioClip BuildGoalChime()
    {
        int sr = 44100;
        return Concat("Goal", new[] {
            SineNote(523f,  0.22f, sr),
            SineNote(659f,  0.22f, sr),
            SineNote(784f,  0.22f, sr),
            SineNote(1047f, 0.55f, sr, 0.55f, 2.5f),
        }, sr);
    }

    static AudioClip BuildWinFanfare()
    {
        int sr = 44100;
        return Concat("Win", new[] {
            SineNote(523f,  0.15f, sr),
            SineNote(659f,  0.15f, sr),
            SineNote(784f,  0.15f, sr),
            SineNote(1047f, 0.15f, sr),
            SineNote(784f,  0.15f, sr),
            SineNote(1047f, 0.9f,  sr, 0.65f, 1.8f),
        }, sr);
    }

    static AudioClip BuildBGMLoop()
    {
        int sr = 44100;
        float bpm = 138f;
        float eighth = 60f / bpm / 2f;
        // Simple pentatonic melody (0 = rest)
        float[] notes = {
            261f, 330f, 392f, 440f, 523f, 440f, 392f, 330f,
            261f, 330f, 392f, 440f, 523f, 0f,   523f, 0f
        };
        int noteLen = (int)(sr * eighth);
        int total   = noteLen * notes.Length;
        var data    = new float[total];

        for (int n = 0; n < notes.Length; n++)
        {
            float freq = notes[n];
            if (freq == 0f) continue;
            int start = n * noteLen;
            for (int i = 0; i < noteLen && start + i < total; i++)
            {
                float t   = i / (float)sr;
                float env = i < (int)(noteLen * 0.75f)
                    ? 1f
                    : Mathf.Lerp(1f, 0f, (i - noteLen * 0.75f) / (noteLen * 0.25f));
                float sq  = Mathf.Sin(2f * Mathf.PI * freq * t) >= 0 ? 1f : -1f;
                data[start + i] = sq * env * 0.13f;
            }
        }

        var clip = AudioClip.Create("BGM", total, 1, sr, false);
        clip.SetData(data, 0);
        return clip;
    }
}
