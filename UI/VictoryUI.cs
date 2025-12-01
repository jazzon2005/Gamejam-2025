using UnityEngine;
using TMPro;

public class VictoryUI : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI finalScoreText;
    public TextMeshProUGUI goldText;
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
        int gold = CurrencySystem.Instance.CurrentGold;

        if (finalScoreText != null)
            finalScoreText.text = $"{score}";

        if (goldText != null)
            goldText.text = $"{gold}";

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