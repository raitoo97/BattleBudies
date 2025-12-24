using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
public class MenuManager : MonoBehaviour
{
    public Button startGameButon;
    public Button returnMenuButon;
    public Button returnMenuButon2;
    public Button tutorialButon;
    public Button creditsButon;
    public Button ExitButton;
    public GameObject panelMain;
    public GameObject panelTutorial;
    public GameObject panelCredits;
    void Start()
    {
        panelTutorial.SetActive(false);
        panelCredits.SetActive(false);
        panelMain.SetActive(true);
        startGameButon.onClick.AddListener(StartGame);
        returnMenuButon.onClick.AddListener(ReturnButon);
        returnMenuButon2.onClick.AddListener(ReturnButon);
        tutorialButon.onClick.AddListener(TutorialButon);
        creditsButon.onClick.AddListener(CreditsButon);
        ExitButton.onClick.AddListener(QuitGame);
        SoundManager.Instance.PlayClip(SoundManager.Instance.GetAudioClip("MenuMusic"), 0.5f, true);
    }
    private void StartGame()
    {
        SceneManager.LoadScene(1);
    }
    private void TutorialButon()
    {
        SoundManager.Instance.PlayClip(SoundManager.Instance.GetAudioClip("SelectGrid"), 1f, false);
        panelTutorial.SetActive(true);
        panelCredits.SetActive(false);
        panelMain.SetActive(false);
    }
    private void ReturnButon()
    {
        SoundManager.Instance.PlayClip(SoundManager.Instance.GetAudioClip("SelectGrid"), 1f, false);
        panelTutorial.SetActive(false);
        panelCredits.SetActive(false);
        panelMain.SetActive(true);
    }
    private void CreditsButon()
    {
        SoundManager.Instance.PlayClip(SoundManager.Instance.GetAudioClip("SelectGrid"), 1f, false);
        panelTutorial.SetActive(false);
        panelCredits.SetActive(true);
        panelMain.SetActive(false);
    }
    private void QuitGame()
    {
        SoundManager.Instance.PlayClip(SoundManager.Instance.GetAudioClip("SelectGrid"), 1f, false);
        Application.Quit();
        print("No funciona en Editor");
    }
}
