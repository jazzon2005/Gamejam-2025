using UnityEngine;
using UnityEngine.UI; // Necesario para Sliders y Toggles
using TMPro; // Necesario para TextMeshPro

public class SettingsUI : MonoBehaviour
{
    [Header("UI References")]
    public Slider masterVolumeSlider;
    public Slider musicVolumeSlider;
    public Slider sfxVolumeSlider;
    public Toggle fullscreenToggle;

    [Header("Gameplay UI References")]
    public TMP_Dropdown difficultyDropdown; // <--- NUEVO: Selector de Dificultad

    private void Start()
    {
        // Cargar valores guardados al iniciar
        LoadSettings();
    }

    // --- VOLUMEN (Conectado a AudioManager) ---

    public void OnMasterVolumeChanged(float value)
    {
        // Guardar
        PlayerPrefs.SetFloat("MasterVolume", value);

        // Aplicar
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetMasterVolume(value);
        }
    }

    public void OnMusicVolumeChanged(float value)
    {
        PlayerPrefs.SetFloat("MusicVolume", value);

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetMusicVolume(value);
        }
    }

    public void OnSFXVolumeChanged(float value)
    {
        PlayerPrefs.SetFloat("SFXVolume", value);

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetSFXVolume(value);
        }
    }

    // --- GRÁFICOS ---

    public void OnFullscreenToggled(bool isFullscreen)
    {
        // Convertir bool a int para guardar (0 = false, 1 = true)
        PlayerPrefs.SetInt("Fullscreen", isFullscreen ? 1 : 0);

        Screen.fullScreen = isFullscreen;
        Debug.Log($"Pantalla Completa: {isFullscreen}");
    }

    // --- DIFICULTAD (NUEVO) ---
    public void OnDifficultyChanged(int index)
    {
        // 0: Fácil, 1: Normal, 2: Difícil
        PlayerPrefs.SetInt("Difficulty", index);

        // Actualizar el GameManager inmediatamente para que aplique en la próxima partida
        if (GameManager.Instance != null)
        {
            GameManager.Instance.UpdateDifficulty(index);
        }

        Debug.Log($"Dificultad cambiada a: {index}");
    }


    // --- CARGA INICIAL ---
    private void LoadSettings()
    {
        float masterVol = PlayerPrefs.GetFloat("MasterVolume", 0.8f);
        float musicVol = PlayerPrefs.GetFloat("MusicVolume", 0.8f);
        float sfxVol = PlayerPrefs.GetFloat("SFXVolume", 0.8f);
        bool isFullscreen = PlayerPrefs.GetInt("Fullscreen", 1) == 1;

        // Cargar dificultad (Default 1 = Normal)
        int difficulty = PlayerPrefs.GetInt("Difficulty", 1);

        // Actualizar UI
        if (masterVolumeSlider != null) masterVolumeSlider.value = masterVol;
        if (musicVolumeSlider != null) musicVolumeSlider.value = musicVol;
        if (sfxVolumeSlider != null) sfxVolumeSlider.value = sfxVol;
        if (fullscreenToggle != null) fullscreenToggle.isOn = isFullscreen;
        if (difficultyDropdown != null) difficultyDropdown.value = difficulty;

        // Aplicar Audio
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetMasterVolume(masterVol);
            AudioManager.Instance.SetMusicVolume(musicVol);
            AudioManager.Instance.SetSFXVolume(sfxVol);
        }

        Screen.fullScreen = isFullscreen;

        // Aplicar Dificultad al iniciar
        if (GameManager.Instance != null)
        {
            GameManager.Instance.UpdateDifficulty(difficulty);
        }
    }

    // El botón "Cerrar" o "Guardar" llamará a UIManager.CloseSettings()
}