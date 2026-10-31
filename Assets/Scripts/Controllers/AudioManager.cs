using UnityEngine;

[DisallowMultipleComponent]
public class AudioManager : MonoBehaviour
{
    private const int SampleRate = 44100;

    public static AudioManager Instance { get; private set; }

    private AudioSource source;
    private AudioClip correctClip;
    private AudioClip wrongClip;
    private WasteGameManager gameManager;

    // Buat sendiri AudioManager kalau scene AR belum dipasangi lewat menu editor.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureInstance()
    {
        if (Instance != null || FindAnyObjectByType<WasteGameManager>() == null)
        {
            return;
        }

        new GameObject("AudioManager").AddComponent<AudioManager>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        source = gameObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.spatialBlend = 0f; // SFX 2D: jangan teredam oleh jarak kamera AR.

        correctClip = BuildChime();
        wrongClip = BuildThud();

        if (correctClip == null || wrongClip == null || correctClip.samples == 0 || wrongClip.samples == 0)
        {
            Debug.LogError("[WasteWise] AudioManager: gagal membuat clip SFX prosedural.");
        }
    }

    private void Start()
    {
        gameManager = FindAnyObjectByType<WasteGameManager>();

        if (gameManager != null)
        {
            gameManager.OnAnswerEvaluated += HandleAnswer;
        }
        else
        {
            Debug.LogWarning("[WasteWise] AudioManager: WasteGameManager tidak ditemukan; SFX tidak akan berbunyi.");
        }
    }

    private void OnDestroy()
    {
        if (gameManager != null)
        {
            gameManager.OnAnswerEvaluated -= HandleAnswer;
        }

        if (Instance == this)
        {
            Instance = null;
        }
    }

    public void PlayCorrectSFX()
    {
        Play(correctClip);
    }

    public void PlayWrongSFX()
    {
        Play(wrongClip);
    }

    private void Play(AudioClip clip)
    {
        if (source != null && clip != null)
        {
            source.PlayOneShot(clip);
        }
    }

    private void HandleAnswer(WasteItem item, bool correct, string explanation)
    {
        if (correct)
        {
            PlayCorrectSFX();
        }
        else
        {
            PlayWrongSFX();
        }
    }

    // "Ding" ganda: nada tinggi C6 lalu E6 dengan harmonik jernih.
    private static AudioClip BuildChime()
    {
        float[] data = new float[Mathf.CeilToInt(SampleRate * 0.5f)];
        AddTone(data, 1046.5f, 0f, 0.22f, 0.6f);
        AddTone(data, 1318.5f, 0.16f, 0.3f, 0.6f);
        return CreateClip("sfx_correct", data);
    }

    // "Thud" pendek bernada rendah; dua frekuensi berdekatan memberi kesan buzz.
    private static AudioClip BuildThud()
    {
        float[] data = new float[Mathf.CeilToInt(SampleRate * 0.35f)];
        AddTone(data, 120f, 0f, 0.35f, 0.7f);
        AddTone(data, 173f, 0f, 0.3f, 0.35f);
        return CreateClip("sfx_wrong", data);
    }

    // Nada = fundamental + harmonik 2 & 3, dengan attack singkat lalu decay eksponensial.
    private static void AddTone(float[] data, float frequency, float startSeconds, float durationSeconds, float amplitude)
    {
        int start = Mathf.Clamp(Mathf.RoundToInt(startSeconds * SampleRate), 0, data.Length);
        int count = Mathf.Min(Mathf.RoundToInt(durationSeconds * SampleRate), data.Length - start);
        const float attack = 0.005f;
        const float release = 0.01f;

        for (int i = 0; i < count; i++)
        {
            float t = (float)i / SampleRate;
            float attackGain = t < attack ? t / attack : 1f;
            float releaseGain = Mathf.Clamp01((durationSeconds - t) / release);
            float envelope = Mathf.Exp(-6f * t) * attackGain * releaseGain;
            float wave = Mathf.Sin(2f * Mathf.PI * frequency * t)
                       + 0.35f * Mathf.Sin(2f * Mathf.PI * frequency * 2f * t)
                       + 0.15f * Mathf.Sin(2f * Mathf.PI * frequency * 3f * t);
            data[start + i] += amplitude * envelope * wave;
        }
    }

    private static AudioClip CreateClip(string name, float[] data)
    {
        float peak = 0f;

        foreach (float sample in data)
        {
            peak = Mathf.Max(peak, Mathf.Abs(sample));
        }

        if (peak > 0.0001f)
        {
            float scale = 0.9f / peak;

            for (int i = 0; i < data.Length; i++)
            {
                data[i] *= scale;
            }
        }

        AudioClip clip = AudioClip.Create(name, data.Length, 1, SampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }
}
