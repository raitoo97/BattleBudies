using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class IABrainManager : MonoBehaviour
{
    public static IABrainManager instance;
    private float chanceToPlayCards = 0.6f;
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
        // CÁLCULO DE CUOTA MÁXIMA PARA ATTACKERS (LÍMITE CONSERVADOR)
        // 1. Calculamos la división justa (ej: 10/2 = 5)
        int initialFairShare = (totalUnits > 0) ? EnergyManager.instance.enemyCurrentEnergy / totalUnits : 0;
        int energyPerUnit = Mathf.Max(1, Mathf.Min(initialFairShare, maxStepsPerUnit));
        // ----------------- MOVIMIENTO DE ATTACKERS -----------------
        if (attackers.Count > 0)
        {
            foreach (Units u in attackers)
            {
                if (EnergyManager.instance.enemyCurrentEnergy < 1) break;
                // moveEnergy es la cuota limitada o lo que quede disponible (SIMPLIFICADO POR SER INT).
                int moveEnergy = Mathf.Min(energyPerUnit, EnergyManager.instance.enemyCurrentEnergy);
                yield return StartCoroutine(IAMoveToTowers.instance.MoveSingleUnit(u, moveEnergy));
            }
            if (Random.value < chanceToPlayCards && EnergyManager.instance.enemyCurrentEnergy >= 1)
            {
                yield return StartCoroutine(IAPlayCards.instance.PlayOneCard());
            }
        }
        // --- MOVIMIENTO DE DEFENDERS ---
        if (defenders.Count > 0)
        {
            int remainingUnits = defenders.Count + rangers.Count;
            int currentEnergy = EnergyManager.instance.enemyCurrentEnergy;
            int defendersFairShare = (remainingUnits > 0) ? currentEnergy / remainingUnits : 1;
            energyPerUnit = Mathf.Max(1, Mathf.Min(defendersFairShare, maxStepsPerUnit));
            foreach (Units u in defenders)
            {
                if (EnergyManager.instance.enemyCurrentEnergy < 1) break;
                int moveEnergy = Mathf.Min(energyPerUnit, EnergyManager.instance.enemyCurrentEnergy);
                yield return StartCoroutine(IADefendTowers.instance.MoveSingleUnit(u, moveEnergy));
            }
            if (Random.value < chanceToPlayCards && EnergyManager.instance.enemyCurrentEnergy >= 1)
            {
                yield return StartCoroutine(IAPlayCards.instance.PlayOneCard());
            }
        }
        // --- MOVIMIENTO DE RANGERS ---
        if (rangers.Count > 0)
        {
            int currentEnergy = EnergyManager.instance.enemyCurrentEnergy;
            int rangersFairShare = (rangers.Count > 0) ? currentEnergy / rangers.Count : 1;
            energyPerUnit = Mathf.Max(1, Mathf.Min(rangersFairShare, maxStepsPerUnit));
            foreach (Units u in rangers)
            {
                if (EnergyManager.instance.enemyCurrentEnergy < 1) break;
                int moveEnergy = Mathf.Min(energyPerUnit, EnergyManager.instance.enemyCurrentEnergy);
                yield return StartCoroutine(IAMoveToResources.instance.MoveSingleUnit(u, moveEnergy));
            }
            if (Random.value < chanceToPlayCards && EnergyManager.instance.enemyCurrentEnergy >= 1)
            {
                yield return StartCoroutine(IAPlayCards.instance.PlayOneCard());
            }
        }
        // --- USO DE ENERGÍA RESIDUAL ---
        if (totalUnits > 0)
        {
            // Volvemos a obtener las unidades por si se jugaron cartas que invocaron nuevas unidades.
            attackers.Clear(); defenders.Clear(); rangers.Clear();
            GetEnemyUnitsByType(ref attackers, ref defenders, ref rangers);
            List<Units> allUnits = new List<Units>();
            allUnits.AddRange(attackers);
            allUnits.AddRange(defenders);
            allUnits.AddRange(rangers);
            while (EnergyManager.instance.enemyCurrentEnergy >= 1)
            {
                int residualEnergy = EnergyManager.instance.enemyCurrentEnergy;
                bool unitMoved = false;
                foreach (Units u in allUnits)
                {
                    if (EnergyManager.instance.enemyCurrentEnergy < 1) break;
                    if (u == null) continue; // Protección para unidades destruidas
                    // Mover la unidad usando toda la energía residual disponible en ese momento
                    if (u is Attackers)
                    {
                        yield return StartCoroutine(IAMoveToTowers.instance.MoveSingleUnit(u as Attackers, residualEnergy));
                        unitMoved = true;
                    }
                    else if (u is Defenders)
                    {
                        yield return StartCoroutine(IADefendTowers.instance.MoveSingleUnit(u as Defenders, residualEnergy));
                        unitMoved = true;
                    }
                    else if (u is Ranger)
                    {
                        yield return StartCoroutine(IAMoveToResources.instance.MoveSingleUnit(u as Ranger, residualEnergy));
                        unitMoved = true;
                    }
                    if (unitMoved) break; // Solo una unidad por ciclo de WHILE
                }
                //Si no se pudo mover ninguna unidad, rompemos el bucle.
                if (!unitMoved && EnergyManager.instance.enemyCurrentEnergy == residualEnergy)
                {
                    break;
                }
            }
        }
        // --- JUEGO DE CARTAS FINAL ---
        if (EnergyManager.instance.enemyCurrentEnergy >= 1)
        {
            if (totalUnits == 0 || Random.value < 0.7f)
            {
                yield return StartCoroutine(IAPlayCards.instance.PlayCards());
            }
        }
        yield return new WaitUntil(() => isBusy());
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