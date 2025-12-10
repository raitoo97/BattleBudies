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
        int bonusHealth = 15;
        if (ResourcesManager.instance.resourcesPlayer < cost) return;
        unit.currentHealth = Mathf.Min(unit.currentHealth + bonusHealth, unit.maxHealth);
        unit.diceCount += 1;
        ResourcesManager.instance.resourcesPlayer -= cost;
        CanvasManager.instance.playerResources.text = ResourcesManager.instance.resourcesPlayer.ToString();
    }
    public void UpgradeEnemyUnits()
    {
        int upgradeCost = 10;
        int bonusHealth = 15;
        while (ResourcesManager.instance.resourcesEnemy >= upgradeCost)
        {
            Units unitToUpgrade = null;
            foreach (Attackers atk in FindObjectsOfType<Attackers>())
            {
                if (atk != null && !atk.isPlayerUnit)
                {
                    unitToUpgrade = atk;
                    break;
                }
            }
            if (unitToUpgrade == null)
            {
                foreach (Defenders def in FindObjectsOfType<Defenders>())
                {
                    if (def != null && !def.isPlayerUnit)
                    {
                        unitToUpgrade = def;
                        break;
                    }
                }
            }
            if (unitToUpgrade == null)return;
            unitToUpgrade.currentHealth = Mathf.Min(unitToUpgrade.currentHealth + bonusHealth, unitToUpgrade.maxHealth);
            unitToUpgrade.diceCount += 1;
            ResourcesManager.instance.resourcesEnemy -= upgradeCost;
            CanvasManager.instance.enemyResources.text =ResourcesManager.instance.resourcesEnemy.ToString();
            Debug.Log($"[UPGRADE ENEMIGO] {unitToUpgrade.name} mejorado | +1 dado | vida al máximo | -10 recursos");
        }
    }
    public bool GetCanUpgradePlayer { get => CanUpgradePlayer; }
}
