using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.buildIndex == 0)
    {
        Osc songPlayer = FindObjectOfType<Osc>();

        if (songPlayer != null)
        {
            Debug.Log("Osc script found");
            songPlayer.PlaySong();
            Debug.Log("PlaySong() called");
        }
        else
        {
            Debug.LogWarning("Osc script not found");
        }
    }
    }
}
