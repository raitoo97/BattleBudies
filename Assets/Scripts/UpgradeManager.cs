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
        CanUpgradePlayer = ResourcesManager.instance.resourcesPlayer >= 15;
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
        int cost = 15;
        int bonusHealth = 7;
        if (ResourcesManager.instance.resourcesPlayer < cost) return;
        unit.currentHealth = Mathf.Min(unit.currentHealth + bonusHealth, unit.maxHealth);
        unit.diceCount += 1;
        SoundManager.Instance.PlayClip(SoundManager.Instance.GetAudioClip("Upgrade"), 0.5f, false);
        ResourcesManager.instance.resourcesPlayer -= cost;
        CanvasManager.instance.playerResources.text = ResourcesManager.instance.resourcesPlayer.ToString();
    }
    public void UpgradeEnemyUnits()
    {
        int upgradeCost = 15;
        int bonusHealth = 7;
        while (ResourcesManager.instance.resourcesEnemy >= upgradeCost)
        {
            Units unitToUpgrade = null;
            int minDice = int.MaxValue;
            foreach (Attackers atk in FindObjectsOfType<Attackers>())
            {
                if (atk != null && !atk.isPlayerUnit)
                {
                    if (atk.diceCount < minDice)
                    {
                        minDice = atk.diceCount;
                        unitToUpgrade = atk;
                    }
                }
            }
            if (unitToUpgrade == null)
            {
                minDice = int.MaxValue;
                foreach (Defenders def in FindObjectsOfType<Defenders>())
                {
                    if (def != null && !def.isPlayerUnit)
                    {
                        if (def.diceCount < minDice)
                        {
                            minDice = def.diceCount;
                            unitToUpgrade = def;
                        }
                    }
                }
            }
            if (unitToUpgrade == null)return;
            unitToUpgrade.currentHealth = Mathf.Min(unitToUpgrade.currentHealth + bonusHealth, unitToUpgrade.maxHealth);
            unitToUpgrade.diceCount += 1;
            SoundManager.Instance.PlayClip(SoundManager.Instance.GetAudioClip("Upgrade"), 0.5f, false);
            ResourcesManager.instance.resourcesEnemy -= upgradeCost;
            CanvasManager.instance.enemyResources.text =ResourcesManager.instance.resourcesEnemy.ToString();
            Debug.Log($"[UPGRADE ENEMIGO] {unitToUpgrade.name} mejorado | +1 dado | vida al máximo | -10 recursos");
        }
    }
    public bool GetCanUpgradePlayer { get => CanUpgradePlayer; }
}
