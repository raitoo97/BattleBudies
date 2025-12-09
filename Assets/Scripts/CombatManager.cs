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
        if (SalvationManager.instance.GetOnSavingThrow) return;
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
        CanvasManager.instance.TryShowCombatUI(playerCanRoll: false);
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
                CanvasManager.instance.TryShowCombatUI(playerCanRoll: true);
                yield return new WaitUntil(() => CanvasManager.instance.rollClicked);
                CanvasManager.instance.TryShowCombatUI(playerCanRoll: false);
            }
            else
            {
                CanvasManager.instance.TryShowCombatUI(playerCanRoll: false);
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
                if (target.currentHealth <= 0)
                {
                    combatActive = false;
                    CanvasManager.instance.ResetUICombat();
                    yield break;
                }
            }
            pendingDamage = 0;
            dice.ResetDicePosition();
            yield return new WaitForSeconds(0.3f);
        }
    }
    #region//TowersPlayer
    public void StartCombatWithTower(Units attacker, Tower tower)
    {
        if (combatActive) return;
        if (SalvationManager.instance.GetOnSavingThrow) return;
        if (attacker == null || tower == null) return;
        if (attacker.isPlayerUnit && tower.faction == Faction.Player)
        {
            return;
        }
        combatActive = true;
        attackerUnit = attacker;
        defenderUnit = null;
        attackerDice = attacker.diceInstance;
        Debug.Log($"StartCombatWithTower: {attackerUnit.name} ataca torre {tower.name}");
        StartCoroutine(TowerCombatFlow(attackerUnit, tower));
    }
    private IEnumerator TowerCombatFlow(Units attacker, Tower tower)
    {
        int remainingDice = attacker.diceCount;
        while (remainingDice > 0)
        {
            attackerDice.PrepareForRoll();
            CanvasManager.instance.rollClicked = false;
            CanvasManager.instance.ShowTowerCombat(true, attacker,attacker.isPlayerUnit ? remainingDice : 0,!attacker.isPlayerUnit ? remainingDice : 0);
            CanvasManager.instance.rollButton.gameObject.SetActive(true);
            yield return new WaitUntil(() => CanvasManager.instance.rollClicked);
            CanvasManager.instance.rollButton.gameObject.SetActive(false);
            attackerDice.RollDice();
            yield return new WaitUntil(() => attackerDice.hasBeenThrown && attackerDice.hasBeenCounted && attackerDice.IsDiceStill());
            if (pendingDamage > 0)
            {
                tower.TakeDamage(pendingDamage);
                CanvasManager.instance.AddDamageToUI(attacker, pendingDamage);
                CanvasManager.instance.ShowTowerCombat(true, attacker,attacker.isPlayerUnit ? remainingDice : 0,!attacker.isPlayerUnit ? remainingDice : 0);
                Debug.Log($"{attacker.name} inflige {pendingDamage} a torre {tower.name}. Vida restante: {tower.currentHealth}");
                if (tower.currentHealth <= 0)
                {
                    Debug.Log($"Torre {tower.name} destruida.");
                    TowerManager.instance.NotifyTowerDestroyed(tower);
                    break;
                }
            }
            pendingDamage = 0;
            attackerDice.ResetDicePosition();
            remainingDice--;
            if (attacker.isPlayerUnit)
                CanvasManager.instance.UpdateDiceRemaining(remainingDice, 0);
            else
                CanvasManager.instance.UpdateDiceRemaining(0, remainingDice);
            CanvasManager.instance.ShowTowerCombat(true, attacker,attacker.isPlayerUnit ? remainingDice : 0,!attacker.isPlayerUnit ? remainingDice : 0);
            yield return new WaitForSeconds(0.2f);
        }
        attacker.hasAttackedTowerThisTurn = true;
        CanvasManager.instance.ResetUICombat();
        combatActive = false;
    }
    public IEnumerator StartCombatWithTower_Coroutine(Units attacker, Tower tower)
    {
        StartCombatWithTower(attacker, tower);
        yield return new WaitUntil(() => !combatActive);
    }
    #endregion
    #region//TowersAI
    public IEnumerator StartCombatWithTowerAI_Coroutine(Units attacker, Tower tower)
    {
        StartCombatWithTowerAI(attacker, tower);
        yield return new WaitUntil(() => !combatActive);
    }
    private void StartCombatWithTowerAI(Units attacker, Tower tower)
    {
        if (combatActive) return;
        if (SalvationManager.instance.GetOnSavingThrow) return;
        if (attacker == null || tower == null) return;
        if (!attacker.isPlayerUnit && tower.faction == Faction.Enemy)
        {
            return;
        }
        combatActive = true;
        attackerUnit = attacker;
        defenderUnit = null;
        attackerDice = attacker.diceInstance;
        Debug.Log($"StartCombatWithTowerAI: {attackerUnit.name} ataca torre {tower.name}");
        StartCoroutine(TowerCombatFlowAI(attackerUnit, tower));
    }
    private IEnumerator TowerCombatFlowAI(Units attacker, Tower tower)
    {
        int remainingDice = attacker.diceCount;
        while (remainingDice > 0)
        {
            attackerDice.PrepareForRoll();
            CanvasManager.instance.ShowTowerCombat(true, attacker,attacker.isPlayerUnit ? remainingDice : 0,!attacker.isPlayerUnit ? remainingDice : 0);
            attackerDice.RollDice();
            yield return new WaitUntil(() => attackerDice.hasBeenThrown && attackerDice.hasBeenCounted && attackerDice.IsDiceStill());
            if (pendingDamage > 0)
            {
                tower.TakeDamage(pendingDamage);
                CanvasManager.instance.AddDamageToUI(attacker, pendingDamage);
                CanvasManager.instance.ShowTowerCombat(true, attacker,attacker.isPlayerUnit ? remainingDice : 0,!attacker.isPlayerUnit ? remainingDice : 0);
                if (tower.currentHealth <= 0)
                {
                    TowerManager.instance.NotifyTowerDestroyed(tower);
                    break;
                }
            }
            pendingDamage = 0;
            attackerDice.ResetDicePosition();
            remainingDice--;
            CanvasManager.instance.ShowTowerCombat(true, attacker,attacker.isPlayerUnit ? remainingDice : 0,!attacker.isPlayerUnit ? remainingDice : 0);
            yield return new WaitForSeconds(0.2f);
        }
        attacker.hasAttackedTowerThisTurn = true;
        CanvasManager.instance.ResetUICombat();
        combatActive = false;
    }
    #endregion
    public bool GetCombatActive { get => combatActive; }
}