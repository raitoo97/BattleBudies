using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class IABrainManager : MonoBehaviour
{
    public static IABrainManager instance;
    private void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);
    }
    public IEnumerator ExecuteTurn()
    {
        ClearAllPaths();
        CardPlayManager.instance.HideAllHandsAtAITurn();
        EnergyManager.instance.RefillEnemyEnergy();
        DeckManager.instance.DrawEnemyCard();
        CardPlayManager.instance.HideAllHandsAtAITurn();
        CanvasManager.instance.UpdateEnergyUI();
        yield return null;
        yield return new WaitForSeconds(0.5f);
        int enemyUnits = CountEnemyUnits();
        int playerUnits = CountPlayerUnits();
        if (enemyUnits <= playerUnits)
        {
            yield return StartCoroutine(IAPlayCards.instance.PlayCards());
        }
        List<Attackers> attackers = new List<Attackers>();
        List<Defenders> defenders = new List<Defenders>();
        List<Ranger> rangers = new List<Ranger>();
        GetEnemyUnitsByType(ref attackers, ref defenders, ref rangers);
        int totalEnergy = Mathf.FloorToInt(EnergyManager.instance.enemyCurrentEnergy);
        int energyForAttackers = Mathf.FloorToInt(totalEnergy * 0.5f);
        int energyForDefenders = Mathf.FloorToInt(totalEnergy * 0.3f);
        int energyForRangers = Mathf.FloorToInt(totalEnergy * 0.2f);
        // ----------------- MOVIMIENTO DE ATTACKERS -----------------
        if (attackers.Count > 0)
        {
            int energyPerUnit = Mathf.Max(1, Mathf.FloorToInt((float)energyForAttackers / attackers.Count));
            foreach (Units u in attackers)
            {
                if (EnergyManager.instance.enemyCurrentEnergy < 1) break;
                int moveEnergy = Mathf.Min(energyPerUnit, Mathf.FloorToInt(EnergyManager.instance.enemyCurrentEnergy));
                yield return StartCoroutine(IAMoveToTowers.instance.MoveSingleUnit(u, moveEnergy));
            }
            if (Random.value < 0.3f && EnergyManager.instance.enemyCurrentEnergy >= 1)
            {
                yield return StartCoroutine(IAPlayCards.instance.PlayCards());
            }
        }
        // ----------------- MOVIMIENTO DE DEFENDERS -----------------
        if (defenders.Count > 0)
        {
            int energyPerUnit = Mathf.Max(1, Mathf.FloorToInt((float)energyForDefenders / defenders.Count));
            foreach (Units u in defenders)
            {
                if (EnergyManager.instance.enemyCurrentEnergy < 1) break;
                int moveEnergy = Mathf.Min(energyPerUnit, Mathf.FloorToInt(EnergyManager.instance.enemyCurrentEnergy));
                yield return StartCoroutine(IADefendTowers.instance.MoveSingleUnit(u, moveEnergy));
            }
            if (Random.value < 0.3f && EnergyManager.instance.enemyCurrentEnergy >= 1)
            {
                yield return StartCoroutine(IAPlayCards.instance.PlayCards());
            }
        }
        // ----------------- MOVIMIENTO DE RANGERS -----------------
        if (rangers.Count > 0)
        {
            int energyPerUnit = Mathf.Max(1, Mathf.FloorToInt((float)energyForRangers / rangers.Count));
            foreach (Units u in rangers)
            {
                if (EnergyManager.instance.enemyCurrentEnergy < 1) break;
                int moveEnergy = Mathf.Min(energyPerUnit, Mathf.FloorToInt(EnergyManager.instance.enemyCurrentEnergy));
                yield return StartCoroutine(IAMoveToResources.instance.MoveSingleUnit(u, moveEnergy));
            }
            if (Random.value < 0.3f && EnergyManager.instance.enemyCurrentEnergy >= 1)
            {
                yield return StartCoroutine(IAPlayCards.instance.PlayCards());
            }
        }
        if (attackers.Count + defenders.Count + rangers.Count == 0 && EnergyManager.instance.enemyCurrentEnergy >= 1)
        {
            yield return StartCoroutine(IAPlayCards.instance.PlayCards());
        }
        yield return new WaitUntil(() => isBusy());
        GameManager.instance.StartPlayerTurn();
    }
    private int CountEnemyUnits()
    {
        int count = 0;
        foreach (Units u in FindObjectsOfType<Units>())
            if (!u.isPlayerUnit)
                count++;
        return count;
    }
    private int CountPlayerUnits()
    {
        int count = 0;
        foreach (Units u in FindObjectsOfType<Units>())
            if (u.isPlayerUnit)
                count++;
        return count;
    }
    private void GetEnemyUnitsByType(ref List<Attackers> atk,ref List<Defenders> def,ref List<Ranger> rng)
    {
        Units[] allUnits = GameObject.FindObjectsOfType<Units>();
        foreach (Units u in allUnits)
        {
            if (u.isPlayerUnit) continue;
            if (u is Attackers) atk.Add(u as Attackers);
            else if (u is Defenders) def.Add(u as Defenders);
            else if (u is Ranger) rng.Add(u as Ranger);
        }
    }
    private void ClearAllPaths()
    {
        foreach (Units u in FindObjectsOfType<Units>())
            u.ClearPath();
    }
    public bool isBusy()
    {
        return !CombatManager.instance.GetCombatActive
               && !SalvationManager.instance.GetOnSavingThrow
               && !ResourcesManager.instance.onColectedResources
               && !HealthTowerManager.instance.onColectedHealth
               && !Units.anyUnitMoving;
    }
}