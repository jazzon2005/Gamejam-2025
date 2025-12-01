using UnityEngine;
using TMPro;

public class GameOverUI : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI finalScoreText;
    public TextMeshProUGUI wavesSurvivedText;
    public GameObject newRecordLabel; // "¡Nuevo Récord!"

    // Se llama automáticamente cada vez que el objeto se activa (cuando UIManager prende el panel)
    private void OnEnable()
    {
        RefreshStats();
    }

    private void RefreshStats()
    {
        if (GameManager.Instance == null) return;

        int score = GameManager.Instance.Score;
        int wave = GameManager.Instance.CurrentWaveIndex; // O +1 si quieres mostrar "Oleada 5" en vez de 4 completadas

        if (finalScoreText != null)
            finalScoreText.text = $"{score}";

        if (wavesSurvivedText != null)
            wavesSurvivedText.text = $"{wave}";

        // Lógica de High Score (Ejemplo básico)
        /*
        int highScore = PlayerPrefs.GetInt("HighScore", 0);
        if (score > highScore)
        {
            PlayerPrefs.SetInt("HighScore", score);
            if (newRecordLabel != null) newRecordLabel.SetActive(true);
        }
        else
        {
            if (newRecordLabel != null) newRecordLabel.SetActive(false);
        }
        */
    }
}