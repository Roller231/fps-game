using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Меню паузы - ESC переключает, но если открыт магазин - закрывает магазин
/// </summary>
public class PauseMenu : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private WeaponShop weaponShop;

    public static bool IsPaused { get; set; } = false;
    private bool isPaused = false;

    private void Start()
    {
        if (pausePanel != null)
            pausePanel.SetActive(false);

        if (weaponShop == null)
            weaponShop = FindObjectOfType<WeaponShop>();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            HandleEscape();
        }
    }

    private void HandleEscape()
    {
        // Если открыт магазин - закрываем его (оригинальное поведение)
        if (weaponShop != null && weaponShop.IsOpen)
        {
            weaponShop.CloseShop();
            return;
        }

        // Иначе переключаем меню паузы
        if (isPaused)
            Resume();
        else
            Pause();
    }

    public void Pause()
    {
        isPaused = true;
        IsPaused = true;
        
        if (pausePanel != null)
            pausePanel.SetActive(true);

        // Показываем и разблокируем курсор для меню
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void Resume()
    {
        isPaused = false;
        IsPaused = false;
        
        if (pausePanel != null)
            pausePanel.SetActive(false);

        // Возвращаем курсор в игру
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void Toggle()
    {
        if (isPaused)
            Resume();
        else
            Pause();
    }


    public void QuitToMenu()
    {
        // Сбрасываем паузу перед выходом
        IsPaused = false;
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        
        SceneManager.LoadScene("MainMenu");
    }

    public void QuitGame()
    {
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }
}
