using System.Collections;
using UnityEngine;
public class CombatManager : MonoBehaviour
{
    public static CombatManager instance;
    private Units attackerUnit;
    private Units defenderUnit;
    private DiceRoll attackerDice;
    private DiceRoll defenderDice;
    private int pendingDamage = 0;
    private bool combatActive = false;
    private void Awake()
    {
        if (instance == null)
            instance = this;
        else
            Destroy(gameObject);
    }
    public void AddDamage(int value)
    {
        pendingDamage += value;
    }
    public void StartCombat(Units unit1, Units unit2, bool attackerStartsTurn)
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
        attackerUnit = attackerStartsTurn ? unit1 : NodeManager.GetFirstUnitInAttackZone(unit1, unit2) ?? unit1;
        defenderUnit = (attackerUnit == unit1) ? unit2 : unit1;
        attackerDice = attackerUnit.diceInstance;
        defenderDice = defenderUnit.diceInstance;
        Debug.Log($"StartCombat: {attackerUnit.name} vs {defenderUnit.name}");
        CanvasManager.instance.ShowCombatUI(true, playerCanRoll: false);
        StartCoroutine(CombatFlow(attackerStartsTurn));
    }
    private IEnumerator CombatFlow(bool attackerStartsTurn)
    {
        bool attackerTurn = true;
        while (combatActive)
        {
            if (attackerTurn)
                yield return StartCoroutine(UnitRollDice(attackerUnit, defenderUnit, attackerDice));
            else
                yield return StartCoroutine(UnitRollDice(defenderUnit, attackerUnit, defenderDice));
            if (!combatActive) yield break;
            attackerTurn = !attackerTurn;
        }
    }
    private IEnumerator UnitRollDice(Units unit, Units target, DiceRoll dice)
    {
        int playerDice = unit.isPlayerUnit ? unit.diceCount : CanvasManager.instance.playerDiceRemaining;
        int enemyDice = !unit.isPlayerUnit ? unit.diceCount : CanvasManager.instance.enemyDiceRemaining;
        CanvasManager.instance.UpdateDiceRemaining(playerDice, enemyDice);
        for (int i = 0; i < unit.diceCount; i++)
        {
            dice.PrepareForRoll();
            if (unit.isPlayerUnit)
            {
                CanvasManager.instance.rollClicked = false;
                CanvasManager.instance.ShowCombatUI(true, playerCanRoll: true);
                yield return new WaitUntil(() => CanvasManager.instance.rollClicked);
                CanvasManager.instance.ShowCombatUI(true, playerCanRoll: false);
            }
            else
            {
                CanvasManager.instance.ShowCombatUI(true, playerCanRoll: false);
                yield return new WaitForSeconds(0.5f);
            }
            dice.RollDice();
            yield return new WaitUntil(() => dice.hasBeenThrown && dice.hasBeenCounted && dice.IsDiceStill());
            if (unit.isPlayerUnit)
                playerDice--;
            else
                enemyDice--;
            CanvasManager.instance.UpdateDiceRemaining(playerDice, enemyDice);
            if (pendingDamage > 0)
            {
                target.TakeDamage(pendingDamage);
                CanvasManager.instance.AddDamageToUI(unit, pendingDamage);
                Debug.Log($"{unit.name} inflige {pendingDamage} a {target.name}. Vida restante: {target.currentHealth}");
                if (target.currentHealth <= 0)
                {
                    Debug.Log($"{target.name} ha muerto. Terminando combate.");
                    combatActive = false;
                    CanvasManager.instance.ResetUI();
                    yield break;
                }
            }
            pendingDamage = 0;
            dice.ResetDicePosition();
            yield return new WaitForSeconds(0.3f);
        }
    }
    public bool GetCombatActive { get => combatActive; }
}
