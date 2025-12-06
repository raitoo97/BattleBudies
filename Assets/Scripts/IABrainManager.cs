using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class IABrainManager : MonoBehaviour
{
    public static IABrainManager instance;
    private float chanceToPlayCards = 0.5f;
    [SerializeField] private int maxStepsPerUnit = 3;
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
        int totalUnits = attackers.Count + defenders.Count + rangers.Count;
        // ----------------- MOVIMIENTO POR TIPOS -----------------
        // Attackers
        if (attackers.Count > 0)
        {
            int initialFairShare = (totalUnits > 0) ? EnergyManager.instance.enemyCurrentEnergy / totalUnits : 0;
            int energyPerUnit = Mathf.Max(1, Mathf.Min(initialFairShare, maxStepsPerUnit));
            foreach (Units u in attackers)
            {
                if (EnergyManager.instance.enemyCurrentEnergy < 1) break;
                int moveEnergy = Mathf.Min(energyPerUnit, EnergyManager.instance.enemyCurrentEnergy);
                yield return StartCoroutine(IAMoveToTowers.instance.MoveSingleUnit(u, moveEnergy));
            }
            if (Random.value < chanceToPlayCards && EnergyManager.instance.enemyCurrentEnergy >= 1)
                yield return StartCoroutine(IAPlayCards.instance.PlayOneCard());
        }
        // Defenders
        if (defenders.Count > 0)
        {
            int remainingUnits = defenders.Count + rangers.Count;
            int currentEnergy = EnergyManager.instance.enemyCurrentEnergy;
            int defendersFairShare = (remainingUnits > 0) ? currentEnergy / remainingUnits : 1;
            int energyPerUnit = Mathf.Max(1, Mathf.Min(defendersFairShare, maxStepsPerUnit));
            foreach (Units u in defenders)
            {
                if (EnergyManager.instance.enemyCurrentEnergy < 1) break;
                // Si ya está sobre nodo de curación, curar y no gastar energía
                if (NodeManager.GetHealthNodes().Contains(u.currentNode))
                {
                    yield return StartCoroutine(IADefendTowers.instance.MoveSingleUnit(u, 0));
                    continue;
                }
                int moveEnergy = Mathf.Min(energyPerUnit, EnergyManager.instance.enemyCurrentEnergy);
                yield return StartCoroutine(IADefendTowers.instance.MoveSingleUnit(u, moveEnergy));
                yield return new WaitUntil(() => !HealthTowerManager.instance.onColectedHealth);
                yield return new WaitForSeconds(0.2f);
            }
            if (Random.value < chanceToPlayCards && EnergyManager.instance.enemyCurrentEnergy >= 1)
                yield return StartCoroutine(IAPlayCards.instance.PlayOneCard());
        }
        // Rangers
        if (rangers.Count > 0)
        {
            int currentEnergy = EnergyManager.instance.enemyCurrentEnergy;
            int rangersFairShare = (rangers.Count > 0) ? currentEnergy / rangers.Count : 1;
            int energyPerUnit = Mathf.Max(1, Mathf.Min(rangersFairShare, maxStepsPerUnit));
            foreach (Units u in rangers)
            {
                if (EnergyManager.instance.enemyCurrentEnergy < 1) break;
                int moveEnergy = Mathf.Min(energyPerUnit, EnergyManager.instance.enemyCurrentEnergy);
                yield return StartCoroutine(IAMoveToResources.instance.MoveSingleUnit(u, moveEnergy));
                yield return new WaitUntil(() => !ResourcesManager.instance.onColectedResources);
            }

            if (Random.value < chanceToPlayCards && EnergyManager.instance.enemyCurrentEnergy >= 1)
                yield return StartCoroutine(IAPlayCards.instance.PlayOneCard());
        }
        yield return new WaitForSeconds(1f);
        Debug.Log("Energía RESIDUAL restante al final del turno IA: " + EnergyManager.instance.enemyCurrentEnergy);
        // ----------------- ENERGÍA RESIDUAL -----------------
        List<Units> allUnits = new List<Units>();
        GetEnemyUnitsByType(ref attackers, ref defenders, ref rangers);
        allUnits.AddRange(attackers);
        allUnits.AddRange(defenders);
        allUnits.AddRange(rangers);
        bool anyUnitCanAct = false;
        foreach (Units u in allUnits)
        {
            if (u is Attackers || u is Ranger || (u is Defenders && !NodeManager.GetHealthNodes().Contains(u.currentNode)))
            {
                anyUnitCanAct = true;
                break;
            }
        }
        if (anyUnitCanAct)
        {
            bool anyMoved;
            int safetyCounter = 0;
            do
            {
                anyMoved = false;
                safetyCounter++;
                if (safetyCounter > 20) break;
                int residualEnergy = EnergyManager.instance.enemyCurrentEnergy;
                foreach (Units u in allUnits)
                {
                    if (u == null) continue;
                    // Saltar Defenders que ya están sobre nodos de curación
                    if (u is Defenders && NodeManager.GetHealthNodes().Contains(u.currentNode))
                        continue;
                    if (EnergyManager.instance.enemyCurrentEnergy < 1) break;

                    if (u is Attackers)
                    {
                        yield return StartCoroutine(IAMoveToTowers.instance.MoveSingleUnit(u as Attackers, residualEnergy));
                        anyMoved = true;
                    }
                    else if (u is Defenders)
                    {
                        yield return StartCoroutine(IADefendTowers.instance.MoveSingleUnit(u as Defenders, residualEnergy));
                        yield return new WaitUntil(() => !HealthTowerManager.instance.onColectedHealth);
                        anyMoved = true;
                    }
                    else if (u is Ranger)
                    {
                        yield return StartCoroutine(IAMoveToResources.instance.MoveSingleUnit(u as Ranger, residualEnergy));
                        yield return new WaitUntil(() => !ResourcesManager.instance.onColectedResources);
                        anyMoved = true;
                    }
                    if (anyMoved) break;
                }
                if (!anyMoved && EnergyManager.instance.enemyCurrentEnergy == residualEnergy)
                    break;
            } while (EnergyManager.instance.enemyCurrentEnergy >= 1);
        }
        // ----------------- JUEGO DE CARTAS FINAL -----------------
        GetEnemyUnitsByType(ref attackers, ref defenders, ref rangers);
        totalUnits = attackers.Count + defenders.Count + rangers.Count;
        if (EnergyManager.instance.enemyCurrentEnergy >= 1 && (totalUnits == 0 || Random.value < 0.7f))
            yield return StartCoroutine(IAPlayCards.instance.PlayCards());
        yield return new WaitUntil(() => isBusy());
        Debug.Log("Energía al final del turno IA: " + EnergyManager.instance.enemyCurrentEnergy);
        GameManager.instance.StartPlayerTurn();
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