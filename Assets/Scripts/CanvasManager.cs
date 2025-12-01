using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
public class CanvasManager : MonoBehaviour
{
    //PreHover
    public static CanvasManager instance;
    [Header("Combat")]
    public Button rollButton;
    public TextMeshProUGUI playerDamageText;
    public TextMeshProUGUI enemyDamageText;
    public GameObject panelEnemy;
    public GameObject panelPlayer;
    public TextMeshProUGUI playerDiceRemainingText;
    public TextMeshProUGUI enemyDiceRemainingText;
    [HideInInspector]public bool rollClicked = false;
    [HideInInspector]public int playerDamageUI = 0;
    [HideInInspector]public int enemyDamageUI = 0;
    [HideInInspector]public int playerDiceRemaining = 0;
    [HideInInspector]public int enemyDiceRemaining = 0;
    [Header("Energy")]
    public List<GameObject> energyPlayer = new List<GameObject>();
    public List<GameObject> energyEnemy = new List<GameObject>();
    [Header("Stats")]
    public GameObject unitStatsPanel;
    public TextMeshProUGUI unitHealthText;
    public TextMeshProUGUI unitDiceText;
    private Units hoveredUnit = null;
    public Button towerPanel;
    private void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);
        rollButton.onClick.AddListener(() => rollClicked = true);
        ResetUI();
    }
    private void Update()
    {
        UpdateUnitStatsHover();
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
    public void ResetUI()
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
    public void ShowTowerCombatUI(bool show, int attackerDiceCount, bool playerCanRoll = false)
    {
        if (towerPanel != null) towerPanel.gameObject.SetActive(show);
        if (playerDiceRemainingText != null)
        {
            playerDiceRemainingText.gameObject.SetActive(show);
            playerDiceRemainingText.text = $"Remaining Dices: {attackerDiceCount}";
        }
        if (panelEnemy != null) panelEnemy.SetActive(false);
        if (panelPlayer != null) panelPlayer.SetActive(false);
        rollButton.gameObject.SetActive(playerCanRoll);
        playerDiceRemaining = attackerDiceCount;
        UpdateDiceRemaining(playerDiceRemaining, 0);
    }
}