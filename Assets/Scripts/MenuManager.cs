using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
public class MenuManager : MonoBehaviour
{
    public Button startGameButon;
    public Button returnMenuButon;
    public Button returnMenuButon2;
    public Button creditsButon;
    public Button ExitButton;
    public GameObject panelMain;
    public GameObject panelTutorial;
    public GameObject panelTutorial1;
    public GameObject panelTutorial2;
    public GameObject panelTutorial3;
    public GameObject panelCredits;
    [Header("Tutorial")]
    public Button tutorialButon;
    public Button tutorialButon2;
    public Button tutorialButon3;
    public Button backTutorial;
    public Button backTutorial2;
    void Start()
    {
        panelTutorial.SetActive(false);
        panelCredits.SetActive(false);
        panelMain.SetActive(true);
        startGameButon.onClick.AddListener(StartGame);
        returnMenuButon.onClick.AddListener(ReturnButon);
        returnMenuButon2.onClick.AddListener(ReturnButon);
        tutorialButon.onClick.AddListener(TutorialButon);
        tutorialButon2.onClick.AddListener(GoTutorialPanel2);
        tutorialButon3.onClick.AddListener(GoTutorialPanel3);
        backTutorial.onClick.AddListener(BackTutorialPanel1);
        backTutorial2.onClick.AddListener(BackTutorialPanel2);
        creditsButon.onClick.AddListener(CreditsButon);
        ExitButton.onClick.AddListener(QuitGame);
        SoundManager.Instance.PlayClip(SoundManager.Instance.GetAudioClip("MenuMusic"), 0.5f, true);
    }
    private void StartGame()
    {
        SceneManager.LoadScene(1);
    }
    private void ReturnButon()
    {
        SoundManager.Instance.PlayClip(SoundManager.Instance.GetAudioClip("SelectGrid"), 1f, false);
        panelTutorial.SetActive(false);
        panelCredits.SetActive(false);
        panelMain.SetActive(true);
    }
    private void TutorialButon()
    {
        SoundManager.Instance.PlayClip(SoundManager.Instance.GetAudioClip("SelectGrid"), 1f, false);
        panelTutorial.SetActive(true);
        panelTutorial1.SetActive(true);
        panelTutorial2.SetActive(false);
        panelTutorial3.SetActive(false);
        panelCredits.SetActive(false);
        panelMain.SetActive(false);
    }
    private void GoTutorialPanel2()
    {
        SoundManager.Instance.PlayClip(SoundManager.Instance.GetAudioClip("SelectGrid"), 1f, false);
        panelTutorial.SetActive(true);
        panelTutorial1.SetActive(false);
        panelTutorial2.SetActive(true);
        panelTutorial3.SetActive(false);
        panelCredits.SetActive(false);
        panelMain.SetActive(false);
    }
    private void GoTutorialPanel3()
    {
        SoundManager.Instance.PlayClip(SoundManager.Instance.GetAudioClip("SelectGrid"), 1f, false);
        panelTutorial.SetActive(true);
        panelTutorial1.SetActive(false);
        panelTutorial2.SetActive(false);
        panelTutorial3.SetActive(true);
        panelCredits.SetActive(false);
        panelMain.SetActive(false);
    }
    private void BackTutorialPanel2()
    {
        SoundManager.Instance.PlayClip(SoundManager.Instance.GetAudioClip("SelectGrid"), 1f, false);
        panelTutorial.SetActive(true);
        panelTutorial1.SetActive(false);
        panelTutorial2.SetActive(true);
        panelTutorial3.SetActive(false);
        panelCredits.SetActive(false);
        panelMain.SetActive(false);
    }
    private void BackTutorialPanel1()
    {
        SoundManager.Instance.PlayClip(SoundManager.Instance.GetAudioClip("SelectGrid"), 1f, false);
        panelTutorial.SetActive(true);
        panelTutorial1.SetActive(true);
        panelTutorial2.SetActive(false);
        panelTutorial3.SetActive(false);
        panelCredits.SetActive(false);
        panelMain.SetActive(false);
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
