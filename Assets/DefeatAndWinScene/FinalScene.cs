using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
public class FinalScene : MonoBehaviour
{
    public Button menuButton;
    public Button exitButton;
    void Start()
    {
        menuButton.onClick.AddListener(OnMenuButtonClick);
        exitButton.onClick.AddListener(OnExitButtonClick);
    }
    public void OnMenuButtonClick()
    {
        SoundManager.Instance?.PlayClip(SoundManager.Instance?.GetAudioClip("SelectGrid"), 1f, false);
        SceneManager.LoadScene(0);
    }
    public void OnExitButtonClick()
    {
        SoundManager.Instance?.PlayClip(SoundManager.Instance?.GetAudioClip("SelectGrid"), 1f, false);
        Debug.Log("Exiting the game.");
        Application.Quit();
    }
}
