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
        List<Attackers> attackers = new List<Attackers>();
        List<Defenders> defenders = new List<Defenders>();
        List<Ranger> rangers = new List<Ranger>();
        GetEnemyUnitsByType(ref attackers, ref defenders, ref rangers);
        if (EnergyManager.instance.enemyCurrentEnergy > 0f)
            yield return StartCoroutine(IAMoveToResources.instance.MoveAllEnemyUnitsToResorces());
        if (!IAMoveToTowers.instance.movedAnyUnit)
            yield return StartCoroutine(IAPlayCards.instance.PlayCards());
        yield return new WaitUntil(() => !CombatManager.instance.GetCombatActive && !SalvationManager.instance.GetOnSavingThrow&&!ResourcesManager.instance.onColectedResources&&!HealthTowerManager.instance.onColectedHealth);
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
}