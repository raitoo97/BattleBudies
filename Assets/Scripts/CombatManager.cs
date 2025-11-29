using System.Collections;
using UnityEngine;
using UnityEngine.UI;
public class CombatManager : MonoBehaviour
{
    public static CombatManager instance;
    [Header("UI")]
    public Button rollButton;
    public Text playerDamageText;
    public Text enemyDamageText;
    private Units attackerUnit;
    private Units defenderUnit;
    private DiceRoll attackerDice;
    private DiceRoll defenderDice;
    private int pendingDamage = 0;
    private bool rollClicked = false;
    private bool combatActive = false;
    private void Awake()
    {
        if (instance == null)
            instance = this;
        else
            Destroy(gameObject);
        rollButton.onClick.AddListener(() => rollClicked = true);
        rollButton.gameObject.SetActive(false);
    }
    public void AddDamage(int value)
    {
        pendingDamage += value;
        UpdateDamageUI();
        Debug.Log($"Daño detectado: {value}, total acumulado: {pendingDamage}");
    }
    private void UpdateDamageUI()
    {
        if (attackerUnit == null) return;
        if (attackerUnit.isPlayerUnit)
        {
            if (playerDamageText != null) playerDamageText.text = $"Damage: {pendingDamage}";
            if (enemyDamageText != null) enemyDamageText.text = $"Damage: 0";
        }
        else
        {
            if (enemyDamageText != null) enemyDamageText.text = $"Damage: {pendingDamage}";
            if (playerDamageText != null) playerDamageText.text = $"Damage: 0";
        }
    }
    private void ResetDamageUI()
    {
        if (playerDamageText != null) playerDamageText.text = "Damage: 0";
        if (enemyDamageText != null) enemyDamageText.text = "Damage: 0";
    }
    public void StartCombat(Units unit1, Units unit2)
    {
        if (combatActive) return;
        if (unit1 == null || unit2 == null)
        {
            Debug.LogError("StartCombat: alguna unidad es null.");
            return;
        }
        if (unit1.currentNode == null || unit2.currentNode == null)
        {
            Debug.LogError("StartCombat: alguna unidad no tiene currentNode asignado.");
            return;
        }
        combatActive = true;
        attackerUnit = NodeManager.GetFirstUnitInAttackZone(unit1, unit2) ?? unit1;
        defenderUnit = (attackerUnit == unit1) ? unit2 : unit1;
        attackerDice = attackerUnit.diceInstance;
        defenderDice = defenderUnit.diceInstance;
        Debug.Log($"StartCombat: {attackerUnit.name} vs {defenderUnit.name}");
        StartCoroutine(CombatFlow());
    }
    private IEnumerator CombatFlow()
    {
        while (attackerUnit.currentHealth > 0 && defenderUnit.currentHealth > 0)
        {
            yield return StartCoroutine(UnitRollDice(attackerUnit, defenderUnit, attackerDice));
            if (defenderUnit.currentHealth <= 0) break;
            yield return StartCoroutine(UnitRollDice(defenderUnit, attackerUnit, defenderDice));
        }
        Debug.Log(attackerUnit.currentHealth <= 0 ? $"{attackerUnit.name} muere." : $"{defenderUnit.name} muere.");
        combatActive = false;
        ResetDamageUI();
    }
    private IEnumerator UnitRollDice(Units unit, Units target, DiceRoll dice)
    {
        for (int i = 0; i < unit.diceCount; i++)
        {
            pendingDamage = 0;
            UpdateDamageUI();
            dice.PrepareForRoll();
            if (unit.isPlayerUnit)
            {
                rollClicked = false;
                rollButton.gameObject.SetActive(true);
                yield return new WaitUntil(() => rollClicked);
                rollButton.gameObject.SetActive(false);
            }
            else
            {
                yield return new WaitForSeconds(0.5f);
            }
            dice.RollDice();
            yield return new WaitUntil(() => dice.hasBeenThrown && dice.hasBeenCounted && dice.IsDiceStill());
            if (pendingDamage > 0)
            {
                target.TakeDamage(pendingDamage);
                Debug.Log($"{unit.name} inflige {pendingDamage} a {target.name}. Vida restante: {target.currentHealth}");
            }
            pendingDamage = 0;
            UpdateDamageUI();
            dice.ResetDicePosition();
            yield return new WaitForSeconds(0.3f);
        }
    }
}