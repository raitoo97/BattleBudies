using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
public class CanvasManager : MonoBehaviour
{
    //PreHover
    public static CanvasManager instance;
    [Header("Energy")]
    public List<GameObject> energyPlayer = new List<GameObject>();
    public List<GameObject> energyEnemy = new List<GameObject>();
    [Header("General")]
    public GameObject panelEnemy;
    public GameObject panelPlayer;
    public TextMeshProUGUI playerDiceRemainingText;
    public TextMeshProUGUI enemyDiceRemainingText;
    [Header("Combat")]
    public Button rollButton;
    public TextMeshProUGUI playerDamageText;
    public TextMeshProUGUI enemyDamageText;
    [HideInInspector]public bool rollClicked = false;
    [HideInInspector]public int playerDamageUI = 0;
    [HideInInspector]public int enemyDamageUI = 0;
    [HideInInspector]public int playerDiceRemaining = 0;
    [HideInInspector]public int enemyDiceRemaining = 0;
    [Header("Stats")]
    public GameObject unitStatsPanel;
    public TextMeshProUGUI unitHealthText;
    public TextMeshProUGUI unitDiceText;
    private Units hoveredUnit = null;
    [Header("StatsTower")]
    public GameObject towerStatsPanel;
    public TextMeshProUGUI towerHealthText;
    private Tower hoveredTower = null;
    [Header("Resources")]
    public TextMeshProUGUI enemyResources;
    public TextMeshProUGUI playerResources;
    private void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);
        rollButton.onClick.AddListener(() => rollClicked = true);
        ResetUICombat();
    }
    private void Update()
    {
        UpdateUnitStatsHover();
        UpdateTowerStatsHover();
    }
    public void UpdateDiceRemaining(int playerRemaining, int enemyRemaining)
    {
        if (playerDiceRemainingText != null)
            playerDiceRemainingText.text = $"Remaining Dices: {playerRemaining}";
        if (enemyDiceRemainingText != null)
            enemyDiceRemainingText.text = $"Remaining Dices: {enemyRemaining}";
    }
    public void UpdateEnergyUI()
    {
        int playerEnergy = Mathf.RoundToInt(EnergyManager.instance.currentEnergy);
        int enemyEnergy = Mathf.RoundToInt(EnergyManager.instance.enemyCurrentEnergy);
        for (int i = 0; i < energyPlayer.Count; i++)
        {
            int index = energyPlayer.Count - 1 - i;
            energyPlayer[index].SetActive(i < playerEnergy);
        }
        for (int i = 0; i < energyEnemy.Count; i++)
        {
            int index = energyEnemy.Count - 1 - i;
            energyEnemy[index].SetActive(i < enemyEnergy);
        }
    }
    private void UpdateUnitStatsHover()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            Units unit = hit.collider.GetComponent<Units>();
            if (unit != null)
            {
                if (hoveredUnit != unit)
                {
                    hoveredUnit = unit;
                    unitStatsPanel.SetActive(true);
                }
                unitHealthText.text = $"{unit.currentHealth}";
                unitDiceText.text = $"{unit.diceCount}D6";
                Vector3 panelPos = Input.mousePosition + new Vector3(15, -15, 0);
                unitStatsPanel.transform.position = panelPos;
            }
            else
            {
                hoveredUnit = null;
                unitStatsPanel.SetActive(false);
            }
        }
        else
        {
            hoveredUnit = null;
            unitStatsPanel.SetActive(false);
        }
    }
    private void UpdateTowerStatsHover()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            Tower tower = hit.collider.GetComponent<Tower>();
            if (tower != null)
            {
                if (hoveredTower != tower)
                {
                    hoveredTower = tower;
                    towerStatsPanel.SetActive(true);
                }
                towerHealthText.text = $"{tower.currentHealth}";
                Vector3 panelPos = Input.mousePosition + new Vector3(15, -15, 0);
                towerStatsPanel.transform.position = panelPos;
            }
            else
            {
                hoveredTower = null;
                towerStatsPanel.SetActive(false);
            }
        }
        else
        {
            hoveredTower = null;
            towerStatsPanel.SetActive(false);
        }
    }
    #region Combat
    public void ShowCombatUI(bool show, bool playerCanRoll = false)
    {
        if (playerDamageText != null) playerDamageText.gameObject.SetActive(show);
        if (enemyDamageText != null) enemyDamageText.gameObject.SetActive(show);
        if (panelEnemy != null) panelEnemy.gameObject.SetActive(show);
        if (panelPlayer != null) panelPlayer.gameObject.SetActive(show);
        if (playerDiceRemainingText != null) playerDiceRemainingText.gameObject.SetActive(show);
        if (enemyDiceRemainingText != null) enemyDiceRemainingText.gameObject.SetActive(show);
        rollButton.gameObject.SetActive(playerCanRoll);
    }
    public void TryShowCombatUI(bool playerCanRoll = false)
    {
        ShowCombatUI(true, playerCanRoll);
        if (SalvationManager.instance.GetOnSavingThrow && !GameManager.instance.isPlayerTurn)
        {
            rollButton.gameObject.SetActive(false);
        }
    }
    public void AddDamageToUI(Units attacker, int value)
    {
        if (attacker.isPlayerUnit) playerDamageUI += value;
        else enemyDamageUI += value;
        UpdateDamageUI();
    }
    public void UpdateDamageUI()
    {
        if (playerDamageText != null) playerDamageText.text = $"Player Damage: {playerDamageUI}";
        if (enemyDamageText != null) enemyDamageText.text = $"Enemy Damage: {enemyDamageUI}";
    }
    public void ResetUICombat()
    {
        playerDamageUI = 0;
        enemyDamageUI = 0;
        UpdateDamageUI();
        rollButton.gameObject.SetActive(false);
        if (playerDamageText != null) playerDamageText.gameObject.SetActive(false);
        if (enemyDamageText != null) enemyDamageText.gameObject.SetActive(false);
        if (panelEnemy != null) panelEnemy.gameObject.SetActive(false);
        if (panelPlayer != null) panelPlayer.gameObject.SetActive(false);
        if (playerDiceRemainingText != null) playerDiceRemainingText.gameObject.SetActive(false);
        if (enemyDiceRemainingText != null) enemyDiceRemainingText.gameObject.SetActive(false);
    }
    #endregion
    #region TowersAtack
    public void ShowTowerCombat(bool show, Units attacker, int playerDiceRemaining, int enemyDiceRemaining)
    {
        if (!show || attacker == null)
        {
            panelEnemy.SetActive(false);
            panelPlayer.SetActive(false);
            playerDamageText.gameObject.SetActive(false);
            enemyDamageText.gameObject.SetActive(false);
            rollButton.gameObject.SetActive(false);
            playerDiceRemainingText.gameObject.SetActive(false);
            enemyDiceRemainingText.gameObject.SetActive(false);
            return;
        }
        panelEnemy.SetActive(!attacker.isPlayerUnit);
        panelPlayer.SetActive(attacker.isPlayerUnit);
        if (attacker.isPlayerUnit)
        {
            playerDamageText.text = $"Damage to Tower: {playerDamageUI}";
            playerDamageText.gameObject.SetActive(true);
            playerDiceRemainingText.gameObject.SetActive(true);
            playerDiceRemainingText.text = $"Remaining Dices: {playerDiceRemaining}";
        }
        else
        {
            enemyDamageText.text = $"Damage to Tower: {enemyDamageUI}";
            enemyDamageText.gameObject.SetActive(true);
            enemyDiceRemainingText.gameObject.SetActive(true);
            enemyDiceRemainingText.text = $"Remaining Dices: {enemyDiceRemaining}";
            rollButton.gameObject.SetActive(false); // enemigo no puede usar botón
        }
    }
    #endregion
    #region Salvations
    public void ShowSalvationUI(bool show, Units unit, bool playerCanRoll = false)
    {
        if (!show || unit == null)
        {
            panelEnemy.SetActive(false);
            panelPlayer.SetActive(false);
            playerDamageText.gameObject.SetActive(false);
            enemyDamageText.gameObject.SetActive(false);
            rollButton.gameObject.SetActive(false);
            playerDiceRemainingText.gameObject.SetActive(false);
            enemyDiceRemainingText.gameObject.SetActive(false);
            return;
        }
        panelPlayer.SetActive(unit.isPlayerUnit);
        panelEnemy.SetActive(!unit.isPlayerUnit);
        rollButton.gameObject.SetActive(playerCanRoll);
        if (unit.isPlayerUnit)
        {
            playerDamageText.text = $"Saving Throw: 3";
            playerDamageText.gameObject.SetActive(true);
            playerDiceRemainingText.gameObject.SetActive(true);
            playerDiceRemainingText.text = $"Remaining Dices: 1"; // solo un intento
        }
        else
        {
            enemyDamageText.text = $"Saving Throw: 3";
            enemyDamageText.gameObject.SetActive(true);

            enemyDiceRemainingText.gameObject.SetActive(true);
            enemyDiceRemainingText.text = $"Remaining Dices: 1";
        }
    }
    #endregion
    #region HealingTower
    public void HealingTowerUI(bool show, Defenders defender, bool playerCanRoll = false, int result = -1, int dicesLeft = -1)
    {
        if (!show || defender == null)
        {
            panelPlayer.SetActive(false);
            panelEnemy.SetActive(false);
            playerDamageText.gameObject.SetActive(false);
            enemyDamageText.gameObject.SetActive(false);
            rollButton.gameObject.SetActive(false);
            playerDiceRemainingText.gameObject.SetActive(false);
            enemyDiceRemainingText.gameObject.SetActive(false);
            return;
        }
        panelPlayer.SetActive(defender.isPlayerUnit);
        panelEnemy.SetActive(!defender.isPlayerUnit);
        rollButton.gameObject.SetActive(playerCanRoll);
        if (defender.isPlayerUnit)
        {
            playerDamageText.text = result >= 0 ? $"Healing Tower: + {result}" : "Healing Tower:";
            playerDamageText.gameObject.SetActive(true);
            playerDiceRemainingText.gameObject.SetActive(true);
            playerDiceRemainingText.text = dicesLeft >= 0 ? $"Remaining Dices: {dicesLeft}" : $"Remaining Dices: {defender.healthTowerDice}";
        }
        else
        {
            enemyDamageText.text = result >= 0 ? $"Healing Tower: + {result}" : "Healing Tower:";
            enemyDamageText.gameObject.SetActive(true);
            enemyDiceRemainingText.gameObject.SetActive(true);
            enemyDiceRemainingText.text = dicesLeft >= 0 ? $"Remaining Dices: {dicesLeft}" : $"Remaining Dices: {defender.healthTowerDice}";
        }
    }
    #endregion
    public void RecolectResourcesUI()
    {

    }
}