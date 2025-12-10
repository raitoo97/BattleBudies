using UnityEngine;
using System.Collections;
public class UpgradeManager : MonoBehaviour
{
    public static UpgradeManager instance;
    private bool CanUpgradePlayer;
    private bool isUpgradingPlayer = false;
    private void Awake()
    {
        if (instance == null)
            instance = this;
        else
            Destroy(this.gameObject);
        CanUpgradePlayer = false;
    }
    private void Update()
    {
        CanUpgradePlayer = ResourcesManager.instance.resourcesPlayer >= 10;
    }
    public void RequestPlayerUpgrade()
    {
        if (!CanUpgradePlayer || isUpgradingPlayer) return;
        StartCoroutine(PlayerUpgradeRoutine());
    }
    private IEnumerator PlayerUpgradeRoutine()
    {
        isUpgradingPlayer = true;
        Debug.Log("Elegí una unidad para mejorar...");
        CanvasManager.instance.UpgradeUnits.interactable = false;
        UnitController.instance.IsSelectingUpgradeUnit = true;
        GridVisualizer.instance.upgradeMode = true;
        UnitController.instance.StartUpgradeSelection();
        while (UnitController.instance.IsSelectingUpgradeUnit)yield return null;
        GridVisualizer.instance.upgradeMode = false;
        isUpgradingPlayer = false;
    }
    public void ApplyUpgradeToPlayerUnit(Units unit)
    {
        int cost = 10;
        if (ResourcesManager.instance.resourcesPlayer < cost) return;
        unit.currentHealth = unit.maxhealth;
        unit.diceCount += 1;
        ResourcesManager.instance.resourcesPlayer -= cost;
        CanvasManager.instance.playerResources.text = ResourcesManager.instance.resourcesPlayer.ToString();
    }
    public void UpgradeEnemyUnits()
    {
        int upgradeCost = 10;
        if (ResourcesManager.instance.resourcesEnemy < upgradeCost) return;
        Units unitToUpgrade = null;
        foreach (Attackers atk in FindObjectsOfType<Attackers>())
        {
            if (atk != null && atk.isPlayerUnit == false) // es enemigo
            {
                unitToUpgrade = atk;
                break;
            }
        }
        if (unitToUpgrade == null)
        {
            foreach (Defenders def in FindObjectsOfType<Defenders>())
            {
                if (def != null && def.isPlayerUnit == false) // es enemigo
                {
                    unitToUpgrade = def;
                    break;
                }
            }
        }
        if (unitToUpgrade == null) return;
        unitToUpgrade.currentHealth = unitToUpgrade.maxhealth;
        unitToUpgrade.diceCount += 1;
        ResourcesManager.instance.resourcesEnemy -= upgradeCost;
        Debug.Log($"[UPGRADE ENEMIGO] Se mejoró a {unitToUpgrade.name} | +1 dado | vida al máximo | -{upgradeCost} recursos");
    }
    public bool GetCanUpgradePlayer { get => CanUpgradePlayer; }
}
