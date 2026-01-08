using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
public class FadesAnimationsManager : MonoBehaviour
{
    public Color init_color;
    public Color final_color;
    public Image BackGroundImage;
    public static FadesAnimationsManager instance;
    private void Awake()
    {
        if (instance == null)
            instance = this;
        else
            Destroy(gameObject);
    }
    private void Start()
    {
        BackGroundImage.gameObject.SetActive(true);
        BackGroundImage.color = init_color;
    }
    public IEnumerator FadeOut()
    {
        yield return StartCoroutine(FadeOutCorutine());
    }
    public IEnumerator FadeIn(int scene)
    {
        yield return StartCoroutine(FadeInCorutine(scene));
    }
    private IEnumerator FadeOutCorutine()
    {
        float elapsedtime = 0f;
        float finishied_time = 1f;
        while (elapsedtime <= finishied_time)
        {
            BackGroundImage.color = Color.Lerp(init_color, final_color, elapsedtime / finishied_time);
            yield return null;
            elapsedtime += Time.deltaTime;
        }
        BackGroundImage.color = final_color;
        BackGroundImage.gameObject.SetActive(false);
    }
    private IEnumerator FadeInCorutine(int scene)
    {
        BackGroundImage.gameObject.SetActive(true);
        float elapsedtime = 0f;
        float finishied_time = 1f;
        while (elapsedtime <= finishied_time)
        {
            BackGroundImage.color = Color.Lerp(final_color, init_color, elapsedtime / finishied_time);
            yield return null;
            elapsedtime += Time.deltaTime;
        }
        BackGroundImage.color = init_color;
        SceneManager.LoadScene(scene);
    }
}
