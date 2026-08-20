using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIController : MonoBehaviour
{
    [Header("Selección de armas")]
    public GameObject weaponSelectPanel;
    public WeaponController weaponController;
    public GameObject weaponHintText;

    bool weaponMenuOpen;
    
    [Header("Paneles")]
    public GameObject startMenuPanel;
    public GameObject pauseMenuPanel;
    public GameObject endGamePanel;
    public GameObject gameOverPanel;

    [Header("Barra de vidas")]
    public Slider healthBar;
    public Slider bossHealthBar;

    public static bool skipStartMenu = false;


    void Start()
    {
        // Ocultar los paneles
        pauseMenuPanel.SetActive(false);
        gameOverPanel.SetActive(false);
        endGamePanel.SetActive(false);

        // Al entrar el juego, debe mostrar el panel de inicio
        // Cuando se reinicia, debe ignorar el panel de inicio
        if (skipStartMenu)
        {
            startMenuPanel.SetActive(false);
            Time.timeScale = 1f;
            MusicManager.Instance.PlayGameplayMusic();
        }
        else
        {
            startMenuPanel.SetActive(true);
            Time.timeScale = 0f;
            MusicManager.Instance.PlayMenuMusic();
        }
    }

    void Update()
    {
        // Tecla escape
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            TogglePause();
        }

        // Cuando el núcleo no está destruido, no se puede abrir el panel de arma
        if (!weaponMenuOpen)
            return;

        // Botón derecho para abrir el panel de arma
        if (Input.GetMouseButtonDown(1))
        {
            OpenWeaponMenu();
        }
    }

    // Empezar el juego al pulsar el botón "Start"
    public void StartGame()
    {
        MusicManager.Instance.PlayGameplayMusic();
        
        skipStartMenu = true;
        startMenuPanel.SetActive(false);
        Time.timeScale = 1f;
    }

    // Salir del juego
    public void ExitGame()
    {
    #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
    #else
        Application.Quit();
    #endif
    }

    // Pausar o reanudar con escape
    void TogglePause()
    {
        if (pauseMenuPanel.activeSelf)
            ResumeGame();
        else
            PauseGame();
    }

    // Activar el panel de pausa
    void PauseGame()
    {
        pauseMenuPanel.SetActive(true);
        weaponController.CancelEnergyCharge();
        Time.timeScale = 0f;
    }

    // Continuar el juego
    public void ResumeGame()
    {
        pauseMenuPanel.SetActive(false);
        Time.timeScale = 1f;
    }

    // Mostrar el panel de victoria
    public void ShowEndGame()
    {
        MusicManager.Instance.StopAllLoopSFX();
        MusicManager.Instance.PlayVictoryMusic();
        
        endGamePanel.SetActive(true);
        Time.timeScale = 0f;
    }

    // Mostrar el panel de GameOver
    public void ShowGameOver()
    {
        MusicManager.Instance.StopAllLoopSFX();
        MusicManager.Instance.PlayGameOverMusic();

        gameOverPanel.SetActive(true);
        Time.timeScale = 0f;
    }

    // Reiniciar el juego
    public void RestartGame()
    {
        skipStartMenu = true;
        Time.timeScale = 1f;

        UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex
        );
    }

    // Mostrar un tips cuando el núcleo fue destruido
    public void ShowWeaponHint()
    {
        weaponMenuOpen = true;
        weaponHintText.SetActive(true);
    }

    // Abrir el panel de armas
    void OpenWeaponMenu()
    {
        weaponHintText.SetActive(false);
        weaponSelectPanel.SetActive(true);
        Time.timeScale = 0f;
    }

    // Cerrar el panel de armas
    public void CloseWeaponMenu()
    {
        weaponMenuOpen = false;
        weaponSelectPanel.SetActive(false);
        Time.timeScale = 1f;
    }

    // Selección de armas
    public void SelectNormal()
    {
        weaponController.currentWeapon = WeaponController.WeaponType.NormalBullet;
        CloseWeaponMenu();
    }
    public void SelectEnergyBall()
    {
        weaponController.currentWeapon = WeaponController.WeaponType.EnergyBall;
        CloseWeaponMenu();
    }
    public void SelectSwords()
    {
        weaponController.currentWeapon = WeaponController.WeaponType.DualSwords;
        CloseWeaponMenu();
    }
    public void SelectMines()
    {
        weaponController.currentWeapon = WeaponController.WeaponType.Mines;
        CloseWeaponMenu();
    }

    // Actualizar la barra de vida del jugador
    public void UpdateHealth(float current, float max)
    {
        healthBar.maxValue = max;
        healthBar.value = current;
    }

    // Mostrar la barra de vida del Boss
    public void ShowBossHealth(float maxHealth)
    {
        bossHealthBar.gameObject.SetActive(true);
        bossHealthBar.maxValue = maxHealth;
        bossHealthBar.value = maxHealth;
    }

    // Actualizar la barra de vida del Boss
    public void UpdateBossHealth(float currentHealth)
    {
        bossHealthBar.value = currentHealth;
    }

    // Desactivar la barra de vida del Boss
    public void HideBossHealth()
    {
        bossHealthBar.gameObject.SetActive(false);
    }
}
