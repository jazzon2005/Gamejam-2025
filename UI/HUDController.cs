using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;

public class HUDController : MonoBehaviour
{
    [Header("1. Decoración Superior")]
    public Animator topDeco1;
    public Animator topDeco2;

    [Header("2. Salud (Face System)")]
    public Image faceImage;
    public Sprite normalFaceSprite;
    public Sprite hurtFaceSprite;
    public float hurtFaceDuration = 0.5f;
    public TextMeshProUGUI healthText;

    [Header("3. Energía (Dash)")]
    public Image energyBarFill; // <--- FALTABA ESTO
    public TextMeshProUGUI energyText; // <--- FALTABA ESTO

    [Header("4. Arma y Munición")]
    public Image currentWeaponIcon;
    public Image ammoBarFill; // <--- FALTABA ESTO
    public Gradient ammoColorGradient; // <--- FALTABA ESTO
    public TextMeshProUGUI ammoText; // <--- FALTABA ESTO

    [Header("5. Estadísticas Generales")]
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI goldText;
    public TextMeshProUGUI waveText;
    public TextMeshProUGUI enemiesLeftText;

    [Header("7. MODO HISTORIA (Narrador)")]
    public GameObject storyPanel; // El contenedor (Panel + Imagen + Texto)
    public Image narratorImage;   // La cara del doctor/narrador
    public Animator narratorAnimator; // Para animar la boca o aparición
    public TextMeshProUGUI storyText; // El texto de la historia

    [Header("8. Crosshair (Mira)")] // <--- NUEVO
    public Image crosshairImage;    // Arrastra aquí la imagen de tu mira

    // Referencias internas
    private CharacterHealth playerHealth;
    private PlayerAttack playerAttack;
    private PlayerController playerController; // Necesario para leer la Stamina

    private bool isShowingHurtFace = false;

    void OnDisable()
    {
        // Cuando el HUD se apaga (Menú, Tienda, GameOver), mostramos el puntero normal
        Cursor.visible = true;
    }

    void Start()
    {
        // Conexiones Globales
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnScoreChanged += UpdateScore;
            GameManager.Instance.OnWaveStarted += UpdateWave;
            GameManager.Instance.OnWavePrepare += ShowStoryMessage; // <--- NUEVA SUSCRIPCIÓN
            UpdateScore(GameManager.Instance.Score);
            UpdateWave(GameManager.Instance.CurrentWaveIndex + 1);
        }

        if (CurrencySystem.Instance != null)
        {
            CurrencySystem.Instance.OnGoldChanged += UpdateGold;
            UpdateGold(CurrencySystem.Instance.CurrentGold);
        }

        // Conexiones Jugador
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerHealth = player.GetComponent<CharacterHealth>();
            playerAttack = player.GetComponent<PlayerAttack>();
            playerController = player.GetComponent<PlayerController>(); // Obtener el controller

            if (playerHealth != null)
            {
                playerHealth.OnHit += OnPlayerHit;
            }

            if (playerAttack != null)
            {
                playerAttack.OnWeaponChanged += UpdateWeaponIcon;
                UpdateWeaponIcon(playerAttack.CurrentEquippedWeapon);
            }
        }

        // Inicializar cara
        if (faceImage != null && normalFaceSprite != null)
        {
            faceImage.sprite = normalFaceSprite;
        }

        if (storyPanel != null) storyPanel.SetActive(false);
    }

    void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnScoreChanged -= UpdateScore;
            GameManager.Instance.OnWaveStarted -= UpdateWave;
            GameManager.Instance.OnWavePrepare -= ShowStoryMessage;
        }
        if (CurrencySystem.Instance != null) CurrencySystem.Instance.OnGoldChanged -= UpdateGold;
        if (playerHealth != null) playerHealth.OnHit -= OnPlayerHit;
        if (playerAttack != null) playerAttack.OnWeaponChanged -= UpdateWeaponIcon;
    }

    void Update()
    {
        // --- LÓGICA DE CROSSHAIR DINÁMICA ---
        UpdateCursorState();
        // ------------------------------------


        // 1. VIDA
        if (playerHealth != null && healthText != null)
        {
            healthText.text = $"{playerHealth.currentHealth}";
        }

        if (GameManager.Instance != null && GameManager.Instance.waveManager != null)
        {
            if (enemiesLeftText != null)
                enemiesLeftText.text = $"{GameManager.Instance.waveManager.EnemiesAlive}";
        }

        // 2. ENERGÍA (Dash)
        if (playerController != null && energyBarFill != null)
        {
            float currentEnergy = playerController.CurrentStamina;
            // Usamos stats del controller. Si no tiene, asumimos 100
            float maxEnergy = (playerController.stats != null) ? playerController.stats.maxStamina : 100f;
            float fillAmount = currentEnergy / maxEnergy;

            energyBarFill.fillAmount = fillAmount;
            if (energyText != null) energyText.text = $"{Mathf.FloorToInt(currentEnergy)}";
        }

        // 3. MUNICIÓN (Color y Barra)
        if (playerAttack != null && ammoBarFill != null)
        {
            PlayerAttackType weapon = playerAttack.CurrentEquippedWeapon;

            if (weapon != null && weapon.useAmmo)
            {
                float currentAmmo = playerAttack.CurrentAmmo;
                float maxAmmo = weapon.maxAmmo;
                float ammoPercent = currentAmmo / maxAmmo;

                ammoBarFill.fillAmount = ammoPercent;

                if (ammoColorGradient != null)
                    ammoBarFill.color = ammoColorGradient.Evaluate(ammoPercent);

                if (ammoText != null) ammoText.text = $"{Mathf.FloorToInt(currentAmmo)}";
            }
            else
            {
                // Munición infinita (Melee)
                ammoBarFill.fillAmount = 1f;
                ammoBarFill.color = Color.white;
                if (ammoText != null) ammoText.text = "∞";
            }
        }
    }

    // Método dedicado para decidir qué cursor mostrar
    private void UpdateCursorState()
    {
        if (crosshairImage == null) return;

        bool isPlaying = false;

        if (GameManager.Instance != null)
        {
            // Solo mostramos la mira si estamos JUGANDO.
            // Si estamos en Pausa, Tienda, Historia o GameOver, queremos el puntero normal.
            isPlaying = GameManager.Instance.CurrentState == GameManager.GameState.Playing;
        }

        if (isPlaying)
        {
            // Modo Juego: Ocultar ratón, Mostrar mira, Mover mira
            Cursor.visible = false;
            crosshairImage.enabled = true;
            crosshairImage.transform.position = Input.mousePosition;
        }
        else
        {
            // Modo Menús: Mostrar ratón, Ocultar mira (para que no estorbe)
            Cursor.visible = true;
            crosshairImage.enabled = false;
        }
    }


    // --- LÓGICA VISUAL ---

    private void OnPlayerHit()
    {
        if (faceImage != null && hurtFaceSprite != null)
        {
            if (!isShowingHurtFace)
            {
                StartCoroutine(ShowHurtFaceRoutine());
            }
        }
    }

    private IEnumerator ShowHurtFaceRoutine()
    {
        isShowingHurtFace = true;
        faceImage.sprite = hurtFaceSprite;

        yield return new WaitForSeconds(hurtFaceDuration);

        faceImage.sprite = normalFaceSprite;
        isShowingHurtFace = false;
    }

    private void UpdateWeaponIcon(PlayerAttackType weapon)
    {
        if (currentWeaponIcon != null)
        {
            if (weapon != null && weapon.icon != null)
            {
                currentWeaponIcon.sprite = weapon.icon;
                currentWeaponIcon.enabled = true;
            }
            else
            {
                // Si no hay icono, podrías poner uno default o ocultar
                // currentWeaponIcon.enabled = false; 
            }
        }
    }

    private void ShowStoryMessage(WaveConfig config)
    {
        // Solo mostrar si hay mensaje y el panel está asignado
        if (!GameManager.Instance.infiniteWaves)
        {
            if (storyPanel != null && !string.IsNullOrEmpty(config.storyMessage))
            {
                storyPanel.SetActive(true);
                storyText.text = config.storyMessage;

                if (narratorImage != null && config.narratorSprite != null)
                {
                    narratorImage.sprite = config.narratorSprite;
                    narratorAnimator.SetTrigger("Talk");
                }

                // --- NUEVO: REPRODUCIR VOZ ---
                if (AudioManager.Instance != null && config.narratorVoice != null)
                {
                    // Usamos PlayUISound porque es una voz en la interfaz, no en el mundo 3D
                    AudioManager.Instance.PlayUISound(config.narratorVoice);
                }

                // Ocultar automáticamente después del tiempo configurado
                if (config.messageDuration > 0)
                {
                    StopCoroutine("HideStoryRoutine");
                    StartCoroutine(HideStoryRoutine(config.messageDuration));
                }
            }
            else if (storyPanel != null)
            {
                // Si no hay mensaje en esta oleada, asegurarnos de ocultarlo
                storyPanel.SetActive(false);
            }
        }       
    }

    private IEnumerator HideStoryRoutine(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (storyPanel != null) storyPanel.SetActive(false);
    }

    // Callbacks simples
    void UpdateScore(int score) { if (scoreText) scoreText.text = $"{score}"; }
    void UpdateWave(int wave) { if (waveText) waveText.text = $"WAVE: {wave}"; }
    void UpdateGold(int gold) { if (goldText) goldText.text = $"{gold}"; }
}