using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Простой загрузчик сцен - вызывай LoadScene("SceneName") из UnityEvent
/// </summary>
public class SceneLoader : MonoBehaviour
{
    /// <summary>
    /// Загрузить сцену по названию. Назначь на кнопку и введи имя сцены в параметр.
    /// </summary>
    public void LoadScene(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogError("[SceneLoader] Scene name is empty!");
            return;
        }

        Debug.Log($"[SceneLoader] Loading scene: {sceneName}");
        SceneManager.LoadScene(sceneName);
    }
}
