using UnityEngine;
public class GlowTower : MonoBehaviour
{
    [Header("Glow Material Settings")]
    [SerializeField] private string glowMaterialName = "OutLine";
    [SerializeField] private Renderer rend;
    [SerializeField] private Material matInstance;
    [Header("Glow Thickness")]
    [SerializeField] private float glowThickness = 1.05f;
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
    public void SetGlowHover(Faction faction)
    {
        if (matInstance == null) return;
        matInstance.SetInt("_ActivateGlow", 1);
        matInstance.SetFloat("_GlowIntensity", 1f);
        matInstance.SetColor("_GlowColor", faction == Faction.Player ? Color.green : Color.red);
        matInstance.SetFloat("_OutLineThickness", glowThickness);
    }
}
