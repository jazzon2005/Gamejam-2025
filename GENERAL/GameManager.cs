using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Settings")]
    public bool gameStartsOnPlay = true;
    public bool infiniteWaves = true;
    public int maxWaves = 12;

    [Header("References")]
    public WaveManager waveManager;
    public GameObject player;
    public CameraFollow cameraFollow;

    // Banderas estáticas para controlar el flujo entre recargas de escena
    public static bool AutoStartNextLoad = false;
    public static bool ForceMenuNextLoad = true; // NUEVA: Para obligar a ir al menú
    // Variables públicas para que WaveManager las lea
    public float DifficultyHealthMultiplier { get; private set; } = 1f;
    public float DifficultyDamageMultiplier { get; private set; } = 1f;

    // Estados
    public enum GameState { Menu, Playing, Paused, GameOver, Shopping, Victory } // Añadido Shopping para futuro
    public GameState CurrentState { get; private set; }
    public float SurvivalTime { get; private set; }
    public int Score { get; private set; }
    public int CurrentWaveIndex { get; private set; } = 0;

    // Eventos
    // Este evento avisa al UIManager qué panel activar
    public System.Action<GameState> OnStateChanged;
    public System.Action<int> OnScoreChanged;
    public System.Action<float> OnTimeChanged;
    public System.Action OnGameOver;
    public System.Action<int> OnWaveStarted;
    public System.Action<int> OnWaveCompleted;
    // NUEVO EVENTO: Envía la configuración de la oleada (para leer la historia)
    public System.Action<WaveConfig> OnWavePrepare;

    private void Awake() { if (Instance == null) Instance = this; else Destroy(gameObject); }

    private void Start()
    {
        // 1. BÚSQUEDA ROBUSTA DEL JUGADOR (Incluso si está apagado)
        if (player == null)
        {
            // Intento 1: Por Tag (solo activos)
            player = GameObject.FindGameObjectWithTag("Player");

            // Intento 2: Por Componente (incluso inactivos) - MÁS SEGURO
            if (player == null)
            {
                var pc = FindFirstObjectByType<PlayerController>(FindObjectsInactive.Include);
                if (pc != null) player = pc.gameObject;
            }
        }

        // Búsquedas robustas de otros sistemas
        if (waveManager == null) waveManager = FindFirstObjectByType<WaveManager>(FindObjectsInactive.Include);
        if (cameraFollow == null) cameraFollow = FindFirstObjectByType<CameraFollow>();

        // Suscripciones
        if (player != null)
        {
            CharacterHealth health = player.GetComponent<CharacterHealth>();
            if (health != null) health.OnDeath += HandlePlayerDeath;
        }

        // CASO A: Forzamos ir al menú (Botón "Volver al Menú")
        if (ForceMenuNextLoad)
        {
            Debug.Log("GameManager: Forzando inicio en MENU");
            ForceMenuNextLoad = false; // Resetear bandera
            GoToMenu();
        }
        // CASO B: Forzamos reinicio rápido (Botón "Reintentar")
        else if (AutoStartNextLoad)
        {
            Debug.Log("GameManager: Auto-Start detectado. Iniciando JUEGO.");
            AutoStartNextLoad = false; // Resetear bandera
            StartGame(infiniteWaves);
        }
        // CASO C: Inicio normal (Play en editor o primera carga)
        else if (gameStartsOnPlay)
        {
            Debug.Log("GameManager: gameStartsOnPlay activo. Iniciando JUEGO.");
            StartGame(infiniteWaves);
        }
        else
        {
            Debug.Log("GameManager: Inicio normal. Yendo a MENU.");
            GoToMenu();
        }
    
        if (!gameStartsOnPlay)
        {
            if (AudioManager.Instance != null) AudioManager.Instance.PlayMainMenuMusic();
        }
    }

    private void GoToMenu()
    {
        // Apagar sistemas de juego para que no se muevan de fondo
        if (player != null) player.SetActive(false);
        if (waveManager != null) waveManager.enabled = false;

        ChangeState(GameState.Menu);
        Time.timeScale = 0f; // Congelar tiempo en el menú
    }

    private void Update()
    {
        if (CurrentState == GameState.GameOver) return;
        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.P))
        {
            if (CurrentState == GameState.Playing) ChangeState(GameState.Paused);
            else if (CurrentState == GameState.Paused) ChangeState(GameState.Playing);
            //else if (CurrentState == GameState.Shopping) { } // Futuro: manejar pausa desde tienda 
        }

        if (CurrentState == GameState.Playing)
        {
            SurvivalTime += Time.deltaTime;
            OnTimeChanged?.Invoke(SurvivalTime);
        }
    }

    public void ChangeState(GameState newState)
    {
        if (AudioManager.Instance != null)
        {
            if (newState == GameState.Shopping)
            {
                AudioManager.Instance.SetShopMusicState(true); // Bajar música
            }
            else if (newState == GameState.Playing)
            {
                AudioManager.Instance.SetShopMusicState(false); // Restaurar música
            }
        }

        CurrentState = newState;

        switch (newState)
        {
            case GameState.Menu:
                Time.timeScale = 0f;
                break;
            case GameState.Playing:
                Time.timeScale = 1f;
                break;
            case GameState.Paused:
                Time.timeScale = 0f;
                break;
            case GameState.Shopping:
                // Opcional: Pausar o dejar en cámara lenta
                Time.timeScale = 0f; // Pausamos para comprar tranquilos
                break;
            case GameState.GameOver:
                Time.timeScale = 1f; // O slow motion
                break;
            case GameState.Victory: 
                Time.timeScale = 1f; 
                break; // <--- NUEVO CASO
        }

        // ¡AVISAR A LA UI!
        OnStateChanged?.Invoke(newState);
    }

    public void StartGame(bool isInfiniteMode)
    {
        if (AudioManager.Instance != null) AudioManager.Instance.PlayInGameMusic();


        this.infiniteWaves = isInfiniteMode;

        Score = 0;
        SurvivalTime = 0f;
        CurrentWaveIndex = 0;

        if (CurrencySystem.Instance != null) CurrencySystem.Instance.ResetGold();

        // 1. ACTIVAR JUGADOR
        if (player != null)
        {
            player.SetActive(true);
            player.transform.position = Vector3.zero; // Reiniciar posición

            // 2. RESUCITAR JUGADOR (¡CRUCIAL!)
            CharacterHealth health = player.GetComponent<CharacterHealth>();
            if (health != null)
            {
                health.ResetHealth(); // Asegurar que tenga vida completa
            }

            // 3. ARREGLAR CÁMARA (Esto soluciona que se quede quieta)
            if (cameraFollow != null)
            {
                cameraFollow.target = player.transform;
            }
        }
        else
        {
            Debug.LogError("GameManager: Intento iniciar juego pero 'player' es NULL.");
        }

        if (waveManager != null)
        {
            waveManager.enabled = true;
        }

        ChangeState(GameState.Playing);
        StartCoroutine(GameLoop());        
    }

    public void RegisterEnemyKill(int scoreAmount, int goldAmount)
    {
        if (CurrentState != GameState.Playing) return;

        // 1. Aumentar Score (Prestigio)
        AddScore(scoreAmount);

        // 2. Aumentar Oro (Economía)
        if (CurrencySystem.Instance != null)
        {
            CurrencySystem.Instance.AddGold(goldAmount);
        }
    }

    private void AddScore(int amount)
    {
        Score += amount;
        OnScoreChanged?.Invoke(Score);
    }

    // ==================================================
    // EL CORAZÓN DEL JUEGO: LA MÁQUINA DE ESTADOS DE OLEADAS
    // ==================================================
    private IEnumerator GameLoop()
    {
        // Determinamos cuántas oleadas hay en total para saber cuándo dar la victoria en modo historia
        // Si es infinito, este número no importa tanto, pero sirve de referencia
        int totalStoryWaves = maxWaves;

        while (infiniteWaves || CurrentWaveIndex < totalStoryWaves)
        {
            // 1. OBTENER CONFIGURACIÓN
            WaveConfig config = waveManager.GetNextWaveConfig(CurrentWaveIndex);

            if (config == null)
            {
                Debug.LogWarning("GameManager: No hay configuración de oleada. Terminando.");
                break;
            }

            // --- HISTORIA / PREPARACIÓN ---
            // Este evento envía la config al HUD para que muestre el diálogo del doctor si existe
            OnWavePrepare?.Invoke(config);

            waveManager.PlayWaveWarning(true);
            yield return new WaitForSeconds(config.delayBeforeWave);
            waveManager.PlayWaveWarning(false);

            // --- COMBATE (Tensión Activa) ---
            OnWaveStarted?.Invoke(CurrentWaveIndex + 1);
            waveManager.StartSpawningWave(config);

            // Esperar hasta que el WaveManager diga que todos murieron (Victoria de Oleada)
            yield return new WaitUntil(() => waveManager.IsWaveFinished);

            // --- FINAL DE OLEADA ---
            OnWaveCompleted?.Invoke(CurrentWaveIndex + 1);
            Debug.Log($"Oleada {CurrentWaveIndex + 1} Completada.");

            // LÓGICA DE DECISIÓN: ¿Tienda o Victoria Final?
            // Si NO es modo infinito Y acabamos de terminar la última oleada -> Victoria
            bool isStoryVictory = !infiniteWaves && (CurrentWaveIndex + 1 >= totalStoryWaves);

            if (isStoryVictory)
            {
                Debug.Log("¡Modo Historia Completado!");
                yield return new WaitForSeconds(2f); // Pequeña pausa dramática antes de la pantalla
                break; // Salimos del bucle para ir al estado Victory
            }
            else
            {
                // Si seguimos jugando (Infinito o faltan oleadas), abrimos la TIENDA
                ChangeState(GameState.Shopping);

                // El juego se pausa aquí hasta que el jugador cierre la tienda
                // El botón "Continuar" de la tienda debe llamar a ChangeState(Playing)
                yield return new WaitUntil(() => CurrentState == GameState.Playing);

                // Descanso breve después de comprar y antes de la siguiente alerta
                yield return new WaitForSeconds(config.delayAfterWave);
            }

            CurrentWaveIndex++;
        }

        // FIN DEL JUEGO: ESTADO DE VICTORIA
        // Llegamos aquí si rompimos el bucle (Victoria de Historia) o si se acabaron las configs en infinito
        Debug.Log("ACTIVANDO PANTALLA DE VICTORIA");
        ChangeState(GameState.Victory);
    }

    public void HandlePlayerDeath()
    {
        if (CurrentState == GameState.GameOver) return;

        StopAllCoroutines(); // Detiene el GameLoop
        if (waveManager != null) waveManager.StopCurrentWave();
        if (cameraFollow != null) cameraFollow.target = null;

        ChangeState(GameState.GameOver);
    }

    // Reinicio rápido (Reintentar)
    public void RestartGame()
    {
        AutoStartNextLoad = true;
        ForceMenuNextLoad = false;
        ReloadScene();
    }

    // Volver al menú (Salir de partida)
    public void RestartGameOnlySceneReload()
    {
        AutoStartNextLoad = false;
        ForceMenuNextLoad = true; // ¡Obligamos a ir al menú!
        ReloadScene();
    }

    private void ReloadScene()
    {
        Time.timeScale = 1f; // CRUCIAL: Restaurar tiempo antes de cargar
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void UpdateDifficulty(int index)
    {
        switch (index)
        {
            case 0: // Fácil
                DifficultyHealthMultiplier = 0.3f; // 30% menos vida
                DifficultyDamageMultiplier = 0.5f; // 50% menos daño
                break;
            case 1: // Normal
                DifficultyHealthMultiplier = 1f;
                DifficultyDamageMultiplier = 1f;
                break;
            case 2: // Difícil
                DifficultyHealthMultiplier = 1.5f; // 50% más vida
                DifficultyDamageMultiplier = 1.5f; // 50% más daño
                break;
        }
    }

    private void OnDestroy()
    {
        if (player != null)
        {
            var h = player.GetComponent<CharacterHealth>();
            if (h != null) h.OnDeath -= HandlePlayerDeath;
        }
    }
}