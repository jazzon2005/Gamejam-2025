using UnityEngine;

public class FloatingTextManager : MonoBehaviour
{
    public static FloatingTextManager Instance { get; private set; }

    [Header("Referencias")]
    [Tooltip("Prefab con el script FloatingText y un TextMeshPro")]
    public GameObject textPrefab;

    [Header("Configuración")]
    public Color goldColor = Color.yellow;
    public Color scoreColor = Color.white;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void Show(string text, Vector3 position, Color color)
    {
        if (textPrefab == null) return;

        // Instanciar en la posición del mundo + un pequeño offset aleatorio para que no se solapen
        Vector3 randomOffset = new Vector3(Random.Range(-0.5f, 0.5f), Random.Range(0f, 0.5f), 0);
        GameObject go = Instantiate(textPrefab, position + randomOffset, Quaternion.identity);

        FloatingText ft = go.GetComponent<FloatingText>();
        if (ft != null)
        {
            ft.Setup(text, color);
        }
    }

    // Helpers rápidos
    public void ShowGold(int amount, Vector3 position)
    {
        Show($"+{amount} Gold", position, goldColor);
    }

    public void ShowScore(int amount, Vector3 position)
    {
        // Mostramos el puntaje un poco más arriba para que no tape el oro
        Show($"+{amount} Points", position + Vector3.up * 0.5f, scoreColor);
    }
}