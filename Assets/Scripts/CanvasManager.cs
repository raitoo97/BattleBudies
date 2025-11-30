using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
public class CanvasManager : MonoBehaviour
{
    //InstanciarUI energia y UI daño y vida undiades.
    public static CanvasManager instance;
    [Header("Combat")]
    public Button rollButton;
    public TextMeshProUGUI playerDamageText;
    public TextMeshProUGUI enemyDamageText;
    public TextMeshProUGUI playerDiceRemainingText;
    public TextMeshProUGUI enemyDiceRemainingText;
    [HideInInspector]public bool rollClicked = false;
    [HideInInspector]public int playerDamageUI = 0;
    [HideInInspector]public int enemyDamageUI = 0;
    [HideInInspector]public int playerDiceRemaining = 0;
    [HideInInspector]public int enemyDiceRemaining = 0;
    [Header("Energy")]
    public List<GameManager> energyPlayer = new List<GameManager>();
    public List<GameManager> energyEnemy = new List<GameManager>();
    private void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);
        rollButton.onClick.AddListener(() => rollClicked = true);
        ResetUI();
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
        if (playerDiceRemainingText != null) playerDiceRemainingText.gameObject.SetActive(false);
        if (enemyDiceRemainingText != null) enemyDiceRemainingText.gameObject.SetActive(false);
    }
    public void ShowCombatUI(bool show, bool playerCanRoll = false)
    {
        if (playerDamageText != null) playerDamageText.gameObject.SetActive(show);
        if (enemyDamageText != null) enemyDamageText.gameObject.SetActive(show);
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
}