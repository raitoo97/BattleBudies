using UnityEngine;
using UnityEngine.SceneManagement;
public class PauseManager : MonoBehaviour
{
    public bool on_pause;
    [SerializeField] private GameObject pause_menu;
    public static PauseManager instance;
    private void Awake()
    {
        if(instance == null)
            instance = this;
        else
            Destroy(gameObject);
    }
    private void Start()
    {
        Continue();
    }
    public void MenuState()
    {
        if (!on_pause)
        {
            Pause();
        }
        else
        {
            Continue();
        }
    }
    public void GoToMainMenu()
    {
        SceneManager.LoadScene(0);
    }
    public void ExitGame()
    {
        Application.Quit();
        Debug.Log("Funciona solo en build");
    }
    public void Continue()
    {
        Time.timeScale = 1.0f;
        pause_menu.SetActive(false);
        on_pause = false;
    }
    private void Pause()
    {
        Time.timeScale = 0.0f;
        pause_menu.SetActive(true);
        on_pause = true;
    }
}
