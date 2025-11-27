using UnityEngine;
public class GlowUnit : MonoBehaviour
{
    [Header("Glow Material Settings")]
    [SerializeField]private string glowMaterialName = "OutLine";
    [SerializeField]private Renderer rend;
    [SerializeField]private Material matInstance;
    private void Awake()
    {
        rend = GetComponentInChildren<Renderer>();
        if (rend == null) return;
        Material[] mats = rend.materials;
        int index = System.Array.FindIndex(mats, m => m.name.Contains(glowMaterialName));
        if (index == -1) return;
        matInstance = Instantiate(mats[index]);
        mats[index] = matInstance;
        rend.materials = mats;
        SetGlowOff();
    }
    public void SetGlowOff()
    {
        if (matInstance == null) return;
        matInstance.SetFloat("_GlowIntensity", 0f);
        matInstance.SetInt("_ActivateGlow", 0);
    }
    public void SetGlowHover()
    {
        if (matInstance == null) return;
        matInstance.SetInt("_ActivateGlow", 1);
        matInstance.SetFloat("_GlowIntensity", 1f);
        matInstance.SetColor("_GlowColor", Color.yellow);
    }
    public void SetGlowSelected()
    {
        if (matInstance == null) return;
        matInstance.SetInt("_ActivateGlow", 1);
        matInstance.SetFloat("_GlowIntensity", 1f);
        matInstance.SetColor("_GlowColor", Color.blue);
    }
}
