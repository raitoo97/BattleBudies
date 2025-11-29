using UnityEngine;
using UnityEngine.UI;
public class CanvasManager : MonoBehaviour
{
    public static CanvasManager instance;
    [Header("Combat")]
    public Button rollButton;
    public Text playerDamageText;
    public Text enemyDamageText;
    [HideInInspector] public bool rollClicked = false;
    [HideInInspector] public int playerDamageUI = 0;
    [HideInInspector] public int enemyDamageUI = 0;
    private void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);

        rollButton.onClick.AddListener(() => rollClicked = true);
        ResetDamageUI();
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
    public void ResetDamageUI()
    {
        playerDamageUI = 0;
        enemyDamageUI = 0;
        UpdateDamageUI();
        if (playerDamageText != null) playerDamageText.gameObject.SetActive(false);
        if (enemyDamageText != null) enemyDamageText.gameObject.SetActive(false);
        rollButton.gameObject.SetActive(false);
    }
    public void ShowCombatUI(bool show, bool playerCanRoll = false)
    {
        if (playerDamageText != null) playerDamageText.gameObject.SetActive(show);
        if (enemyDamageText != null) enemyDamageText.gameObject.SetActive(show);
        rollButton.gameObject.SetActive(playerCanRoll);
    }
}