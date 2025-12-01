using UnityEngine;

public class CurrencySystem : MonoBehaviour
{
    // Singleton: Acceso global fácil (CurrencySystem.Instance)
    public static CurrencySystem Instance { get; private set; }

    [Header("Debug")]
    [SerializeField] private int currentGold = 0; // Serializado para ver en inspector, pero privado
    public int CurrentGold => currentGold; // Propiedad pública de solo lectura

    // Evento: Avisamos a la UI solo cuando el dinero cambia
    // int = cantidad total actual
    public System.Action<int> OnGoldChanged;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            // DontDestroyOnLoad(gameObject); // Descomentar si quieres mantener dinero entre escenas
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // Inicializar UI al arrancar
        OnGoldChanged?.Invoke(currentGold);
    }

    // Sumar dinero
    public void AddGold(int amount)
    {
        currentGold += amount;
        Debug.Log($"Se añadieron {amount} de oro. Total: {currentGold}");
        OnGoldChanged?.Invoke(currentGold);
    }

    // Intentar gastar dinero (retorna true si tuvo éxito)
    public bool TrySpendGold(int amount)
    {
        if (currentGold >= amount)
        {
            currentGold -= amount;
            Debug.Log($"Gastados {amount} de oro. Restante: {currentGold}");
            OnGoldChanged?.Invoke(currentGold);
            return true;
        }

        Debug.Log("No tienes suficiente oro.");
        return false;
    }

    public void ResetGold()
    {
        currentGold = 0;
    }

    // HERRAMIENTAS DE PRUEBA (Click derecho en el componente en Unity para usarlas)
    [ContextMenu("Debug: Add 100 Gold")]
    public void DebugAddGold()
    {
        AddGold(100);
    }

    [ContextMenu("Debug: Spend 50 Gold")]
    public void DebugSpendGold()
    {
        TrySpendGold(50);
    }
}