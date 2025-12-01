using UnityEngine;
using UnityEngine.UI; // Necesario para Image
using System.Collections; // Necesario para Coroutines
using System; // Necesario para Action

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("Paneles Principales")]
    public GameObject mainMenuPanel;
    public GameObject hudPanel;
    public GameObject shopPanel;
    public GameObject pausePanel;
    public GameObject gameOverPanel;
    public GameObject settingsPanel;
    public GameObject creditsPanel;
    public GameObject victoryPanel;
    public GameObject instructionsPanel;

    // =================================================================
    // ELEMENTOS DE FADE AÑADIDOS
    // =================================================================
    [Header("Transiciones (Fundido)")]
    [Tooltip("Panel Image que cubre toda la pantalla para el fundido a negro.")]
    public Image fadeImage;
    [Tooltip("Tiempo en segundos que dura el fundido (In y Out).")]
    public float fadeDuration = 0.35f;

    private bool isTransitioning = false;
    private GameManager.GameState targetState;

    // Rastreador del estado anterior para determinar si se necesita Fade
    private GameManager.GameState previousState;
    // =================================================================

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        // Configuración de la imagen de fundido (Inicialmente transparente)
        if (fadeImage != null)
        {
            Color c = fadeImage.color;
            fadeImage.color = new Color(c.r, c.g, c.b, 0f);
            // Desactivamos la imagen para que no bloquee clics
            fadeImage.gameObject.SetActive(false);
        }

        // Conexión con GameManager (LÓGICA ORIGINAL)
        if (GameManager.Instance != null)
        {
            GameManager.GameState initialState = GameManager.Instance.CurrentState;

            // Inicializamos el estado anterior con el estado actual al inicio
            previousState = initialState;

            // 1. Nos suscribimos al evento
            GameManager.Instance.OnStateChanged += HandleStateChange;

            // 2. Sincronizamos el estado inicial (Esto activa el menú principal)
            HandleStateChange(initialState);
        }

        // Inicialización de paneles secundarios
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (creditsPanel != null) creditsPanel.SetActive(false);
        if (instructionsPanel != null) instructionsPanel.SetActive(false);
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnStateChanged -= HandleStateChange;
        }
    }

    // ====================================================================
    // LÓGICA PRINCIPAL DE CAMBIO DE ESTADO (RESTAURADA Y MODIFICADA)
    // ====================================================================

    private void HandleStateChange(GameManager.GameState newState)
    {
        // 1. Determinamos si el nuevo estado requiere FADE
        bool needsFade = false;

        if (newState == GameManager.GameState.GameOver || newState == GameManager.GameState.Victory)
        {
            // Fade siempre para Game Over y Victory
            needsFade = true;
        }
        else if (newState == GameManager.GameState.Playing)
        {
            // El Fade SOLO se aplica si se transiciona desde el Menú Principal (Start Game)
            if (previousState == GameManager.GameState.Menu)
            {
                needsFade = true;
            }
            // Si viene de Paused o Shopping, needsFade es falso (transición instantánea)
        }
        // Estados como Menu, Paused, Shopping siempre usan SwitchPanelsInstant

        if (needsFade)
        {
            // Si requiere fade, iniciamos la coroutine de transición.
            StartCoroutine(FadeToState(newState));
        }
        else
        {
            // Si NO requiere fade (Menu, Pausa, Shop, o Playing desde Pausa/Shop), cambiamos los paneles instantáneamente.
            SwitchPanelsInstant(newState);
        }

        // Actualizar el estado anterior para la próxima transición
        previousState = newState;
    }

    // Función para el cambio instantáneo de paneles (Usado por Menu, Pausa, Shop y Playing sin Fade)
    private void SwitchPanelsInstant(GameManager.GameState newState)
    {
        // 1. Apagamos TODO primero (Limpieza)
        SetActivePanel(mainMenuPanel, false);
        SetActivePanel(hudPanel, false);
        SetActivePanel(shopPanel, false);
        SetActivePanel(pausePanel, false);
        SetActivePanel(gameOverPanel, false);
        SetActivePanel(victoryPanel, false);
        SetActivePanel(instructionsPanel, false);

        // NO TOCAMOS settingsPanel/creditsPanel/instructionsPanel aquí

        // 2. Prendemos solo lo necesario según el estado
        switch (newState)
        {
            case GameManager.GameState.Menu:
                SetActivePanel(mainMenuPanel, true);
                break;

            case GameManager.GameState.Playing:
                SetActivePanel(hudPanel, true);
                break;

            case GameManager.GameState.Paused:
                // Mantenemos el HUD de fondo para que se vea bonito, pero ponemos Pausa encima
                SetActivePanel(hudPanel, true);
                SetActivePanel(pausePanel, true);
                break;

            case GameManager.GameState.Shopping:
                // Opcional: Dejar HUD visible para ver el dinero
                SetActivePanel(hudPanel, true);
                SetActivePanel(shopPanel, true);
                break;

            case GameManager.GameState.GameOver:
                SetActivePanel(gameOverPanel, true);
                break;

            case GameManager.GameState.Victory:
                SetActivePanel(victoryPanel, true);
                break;
        }
    }

    // ====================================================================
    // LÓGICA DE FADE (AÑADIDA)
    // ====================================================================

    private IEnumerator FadeToState(GameManager.GameState newState)
    {
        if (isTransitioning || fadeImage == null)
        {
            // Si ya estamos transicionando o no hay imagen de fade, hacemos el cambio instantáneo
            SwitchPanelsInstant(newState);
            yield break;
        }

        isTransitioning = true;
        targetState = newState;

        // 1. FADE OUT (Oscurecer la pantalla)
        fadeImage.gameObject.SetActive(true);
        yield return StartCoroutine(RunFade(1f, fadeDuration)); // Ir de 0 a 1 (Opaco)

        // 2. CAMBIO INSTANTÁNEO (Oculto)
        SwitchPanelsInstant(newState);

        // 3. FADE IN (Aclarar la pantalla)
        yield return StartCoroutine(RunFade(0f, fadeDuration)); // Ir de 1 a 0 (Transparente)
        fadeImage.gameObject.SetActive(false); // Desactivar el panel negro al terminar

        isTransitioning = false;
    }

    // Coroutine genérica para fundir la imagen
    private IEnumerator RunFade(float targetAlpha, float duration)
    {
        Color startColor = fadeImage.color;
        Color targetColor = new Color(startColor.r, startColor.g, startColor.b, targetAlpha);
        float time = 0;

        while (time < duration)
        {
            time += Time.deltaTime;
            float t = time / duration;
            fadeImage.color = Color.Lerp(startColor, targetColor, t);
            yield return null;
        }

        fadeImage.color = targetColor; // Asegurar el valor final
    }

    // Helper para cambiar el estado de un panel de forma segura
    private void SetActivePanel(GameObject panel, bool active)
    {
        if (panel != null) panel.SetActive(active);
    }

    // ==================================================
    // FUNCIONES PARA BOTONES (On Click)
    // ==================================================

    public void ResumeGame()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ChangeState(GameManager.GameState.Playing);
        }
    }

    public void PauseGame()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ChangeState(GameManager.GameState.Paused);
        }
    }

    public void RestartGame()
    {
        if (GameManager.Instance != null)
        {
            Time.timeScale = 1f;
            GameManager.Instance.RestartGame();
        }
    }

    public void QuitGame()
    {
        Debug.Log("Saliendo del juego...");
        Application.Quit();
    }

    public void StartGameFromMenu()
    {
        if (GameManager.Instance != null)
        {
            Time.timeScale = 1f;
            GameManager.Instance.StartGame(GameManager.Instance.infiniteWaves);
        }
    }

    public void ReturnToMainMenu()
    {
        if (GameManager.Instance != null)
        {
            GameManager.AutoStartNextLoad = false;
            Time.timeScale = 1f;
            GameManager.Instance.RestartGameOnlySceneReload();
        }
    }

    // ==================================================
    // FUNCIONES PARA PANELES SECUNDARIOS (SIN AFECTAR GM STATE)
    // ==================================================

    // NOTA: Settings YA NO usa Fade. Credits e Instructions SÍ usan Fade.

    public void OnSettingsClicked()
    {
        // Settings: Cambio instantáneo
        if (settingsPanel != null) SetActivePanel(settingsPanel, true);
    }

    public void CloseSettings()
    {
        // Settings: Cambio instantáneo
        if (settingsPanel != null) SetActivePanel(settingsPanel, false);
    }

    public void OnCreditsClicked()
    {
        Time.timeScale = 1f;
        if (creditsPanel != null) StartCoroutine(TogglePanelWithFade(creditsPanel, true));
    }

    public void CloseCredits()
    {
        if (creditsPanel != null) StartCoroutine(TogglePanelWithFade(creditsPanel, false));
    }

    public void OnInstructionsClicked()
    {
        Time.timeScale = 1f;
        if (instructionsPanel != null) StartCoroutine(TogglePanelWithFade(instructionsPanel, true));
    }

    public void CloseInstructions()
    {
        if (instructionsPanel != null) StartCoroutine(TogglePanelWithFade(instructionsPanel, false));
    }

    // Coroutine para abrir/cerrar un panel secundario con fade (Usado por Credits e Instructions)
    private IEnumerator TogglePanelWithFade(GameObject panel, bool open)
    {
        if (isTransitioning || fadeImage == null)
        {
            // Fallback instantáneo
            SetActivePanel(panel, open);
            yield break;
        }

        isTransitioning = true;

        // 1. FADE OUT
        fadeImage.gameObject.SetActive(true);
        yield return StartCoroutine(RunFade(1f, fadeDuration));

        // 2. CAMBIO DE VISIBILIDAD
        SetActivePanel(panel, open);

        // 3. FADE IN
        yield return StartCoroutine(RunFade(0f, fadeDuration));
        fadeImage.gameObject.SetActive(false);

        isTransitioning = false;
    }
}