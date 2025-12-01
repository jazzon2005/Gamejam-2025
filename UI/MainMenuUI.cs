using UnityEngine;
using TMPro;

public class MainMenuUI : MonoBehaviour
{

    //[Header("Visuals")]
    //public TextMeshProUGUI highscoreText;

    private void Start()
    {
        // Cargar Highscore si lo tienes...
    }

    // ==========================================
    // FUNCIONES PARA LOS BOTONES PROPIOS DE ESTA UI
    // ==========================================

    public void OnStoryModeClicked()
    {
        // Inicia con infiniteWaves = false
        Debug.Log("Iniciando Modo Historia...");
        GameManager.Instance.StartGame(false);
    }

    public void OnInfiniteModeClicked()
    {
        // Inicia con infiniteWaves = true
        Debug.Log("Iniciando Modo Infinito...");
        GameManager.Instance.StartGame(true);
    }
}