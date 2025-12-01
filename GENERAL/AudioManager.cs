using UnityEngine;
using UnityEngine.Audio; // Necesario para el Mixer
using System.Collections; // Para Corrutinas

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Configuración")]
    public AudioMixer audioMixer; // Arrastra tu AudioMixer aquí (Master, Music, SFX)
    public AudioSource musicSource; // Fuente dedicada para música (Loop)
    public AudioSource sfxSource2D; // Fuente para UI (No espacial)

    [Header("Pooling (Optimización)")]
    [Tooltip("Prefab simple con un AudioSource configurado en 3D")]
    public GameObject soundEffectPrefab;

    [Header("Volumen Global")]
    [Range(0f, 2f)] public float sfxVolumeMultiplier = 1.2f; // Boost para SFX
    [Range(0f, 1f)] public float musicVolumeMultiplier = 0.6f; // Reducción para Música

    [Header("Música")]
    public AudioClip mainMenuMusic;
    public AudioClip inGameMusic;

    // Guardar volumen/pitch original para restaurar
    private float originalMusicVolume;
    private float originalMusicPitch;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Persiste entre escenas
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        if (musicSource != null)
        {
            musicSource.volume = 1f * musicVolumeMultiplier;
            originalMusicVolume = musicSource.volume;
        }
    }


    // --- MÚSICA ---
    public void PlayMusic(AudioClip clip)
    {
        if (musicSource.clip == clip && musicSource.isPlaying) return;
        musicSource.Stop();
        musicSource.clip = clip;
        musicSource.volume = 1f * musicVolumeMultiplier; // Asegurar volumen inicial
        musicSource.Play();
    }

    public void PlayMainMenuMusic() => PlayMusic(mainMenuMusic);
    public void PlayInGameMusic() => PlayMusic(inGameMusic);

    // --- SONIDOS UI (2D) ---
    public void PlayUISound(AudioClip clip)
    {
        if (clip != null)
        {
            // Usamos PlayOneShot con el multiplicador
            sfxSource2D.PlayOneShot(clip, 1f * sfxVolumeMultiplier);
        }
    }

    // --- SONIDOS MUNDO (3D) ---
    public void PlaySoundAtPosition(AudioClip clip, Vector3 position, float volume = 1f, float pitch = 1f)
    {
        if (clip == null) return;

        GameObject tempAudio = Instantiate(soundEffectPrefab, position, Quaternion.identity);
        AudioSource source = tempAudio.GetComponent<AudioSource>();

        source.clip = clip;
        source.volume = volume * sfxVolumeMultiplier; // Aplicar Boost
        source.pitch = pitch;
        source.Play();

        Destroy(tempAudio, clip.length / Mathf.Abs(pitch) + 0.1f); // +0.1s de seguridad
    }

    // --- EFECTOS DE MÚSICA (TIENDA) ---

    public void SetShopMusicState(bool isShopOpen)
    {
        if (musicSource == null) return;

        StopAllCoroutines(); // Detener transiciones anteriores
        StartCoroutine(FadeMusicState(isShopOpen));
    }

    private IEnumerator FadeMusicState(bool isShopOpen)
    {
        float targetPitch = isShopOpen ? 0.9f : 1.0f; // Más lento en tienda
        float targetVol = isShopOpen ? 0.3f : 1.0f;   // Más bajo en tienda (ajusta según tu volumen base)
        // Nota: Si usas AudioMixer para volumen, mejor usa mixer. Pero directo al source es más rápido para prototipo.

        float duration = 0.5f;
        float time = 0;
        float startVol = musicSource.volume;
        float startPitch = musicSource.pitch;

        while (time < duration)
        {
            time += Time.unscaledDeltaTime; // Usar unscaled porque el juego puede estar en pausa (Time.timeScale = 0)
            float t = time / duration;

            musicSource.volume = Mathf.Lerp(startVol, targetVol, t);
            musicSource.pitch = Mathf.Lerp(startPitch, targetPitch, t);
            yield return null;
        }

        musicSource.volume = targetVol;
        musicSource.pitch = targetPitch;
    }

    // --- SETTINGS (Sliders) ---
    public void SetMasterVolume(float volume)
    {
        float db = Mathf.Log10(Mathf.Max(0.0001f, volume)) * 20;
        audioMixer.SetFloat("MasterVol", db);
    }

    public void SetMusicVolume(float volume)
    {
        float db = Mathf.Log10(Mathf.Max(0.0001f, volume)) * 20;
        audioMixer.SetFloat("MusicVol", db);
    }

    public void SetSFXVolume(float volume)
    {
        float db = Mathf.Log10(Mathf.Max(0.0001f, volume)) * 20;
        audioMixer.SetFloat("SFXVol", db);
    }
}