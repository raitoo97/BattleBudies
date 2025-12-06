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

        // CÁLCULO DE CUOTA MÁXIMA PARA ATTACKERS (LÍMITE CONSERVADOR)
        // 1. Calculamos la división justa (ej: 10/2 = 5)
        int initialFairShare = (totalUnits > 0) ? Mathf.FloorToInt(EnergyManager.instance.enemyCurrentEnergy / (float)totalUnits) : 0;
        int energyPerUnit = Mathf.Max(1, Mathf.Min(initialFairShare, maxStepsPerUnit));
        // ----------------- MOVIMIENTO DE ATTACKERS -----------------
        if (attackers.Count > 0)
        {
            foreach (Units u in attackers)
            {
                if (EnergyManager.instance.enemyCurrentEnergy < 1) break;
                // moveEnergy es la cuota limitada (energyPerUnit) o lo que quede disponible.
                int moveEnergy = Mathf.Min(energyPerUnit, Mathf.FloorToInt(EnergyManager.instance.enemyCurrentEnergy));
                yield return StartCoroutine(IAMoveToTowers.instance.MoveSingleUnit(u, moveEnergy));
            }
            if (Random.value < chanceToPlayCards && EnergyManager.instance.enemyCurrentEnergy >= 1)
            {
                yield return StartCoroutine(IAPlayCards.instance.PlayCards());
            }
        }

        // ----------------- MOVIMIENTO DE DEFENDERS -----------------
        if (defenders.Count > 0)
        {
            // Recalcula la energía promedio para las unidades restantes
            int remainingUnits = defenders.Count + rangers.Count;
            int currentEnergy = Mathf.FloorToInt(EnergyManager.instance.enemyCurrentEnergy);

            // CÁLCULO DE CUOTA MÁXIMA PARA DEFENDERS (Ajuste Dinámico + Límite)
            int defendersFairShare = (remainingUnits > 0) ? Mathf.FloorToInt((float)currentEnergy / remainingUnits) : 1;
            energyPerUnit = Mathf.Max(1, Mathf.Min(defendersFairShare, maxStepsPerUnit));

            foreach (Units u in defenders)
            {
                if (EnergyManager.instance.enemyCurrentEnergy < 1) break;

                int moveEnergy = Mathf.Min(energyPerUnit, Mathf.FloorToInt(EnergyManager.instance.enemyCurrentEnergy));
                yield return StartCoroutine(IADefendTowers.instance.MoveSingleUnit(u, moveEnergy));
            }

            // JUEGO DE CARTAS INTERMEDIO: Baja probabilidad.
            if (Random.value < chanceToPlayCards && EnergyManager.instance.enemyCurrentEnergy >= 1)
            {
                yield return StartCoroutine(IAPlayCards.instance.PlayCards());
            }
        }

        // ----------------- MOVIMIENTO DE RANGERS -----------------
        if (rangers.Count > 0)
        {
            // Recalcula la energía promedio (solo Rangers)
            int currentEnergy = Mathf.FloorToInt(EnergyManager.instance.enemyCurrentEnergy);

            // CÁLCULO DE CUOTA MÁXIMA PARA RANGERS (Ajuste Dinámico + Límite)
            int rangersFairShare = (rangers.Count > 0) ? Mathf.FloorToInt((float)currentEnergy / rangers.Count) : 1;
            energyPerUnit = Mathf.Max(1, Mathf.Min(rangersFairShare, maxStepsPerUnit));

            foreach (Units u in rangers)
            {
                if (EnergyManager.instance.enemyCurrentEnergy < 1) break;

                int moveEnergy = Mathf.Min(energyPerUnit, Mathf.FloorToInt(EnergyManager.instance.enemyCurrentEnergy));
                yield return StartCoroutine(IAMoveToResources.instance.MoveSingleUnit(u, moveEnergy));
            }

            // JUEGO DE CARTAS INTERMEDIO: Baja probabilidad.
            if (Random.value < chanceToPlayCards && EnergyManager.instance.enemyCurrentEnergy >= 1)
            {
                yield return StartCoroutine(IAPlayCards.instance.PlayCards());
            }
        }
        int residualEnergy = Mathf.FloorToInt(EnergyManager.instance.enemyCurrentEnergy);
        if (residualEnergy >= 1 && totalUnits > 0)
        {
            List<Units> allUnits = new List<Units>();
            allUnits.AddRange(attackers);
            allUnits.AddRange(defenders);
            allUnits.AddRange(rangers);
            foreach (Units u in allUnits)
            {
                // Doble chequeo de energía, por si la unidad anterior ya consumió todo.
                if (EnergyManager.instance.enemyCurrentEnergy < 1) break;
                // Usamos la energía residual para esa unidad
                if (u is Attackers)
                {
                    yield return StartCoroutine(IAMoveToTowers.instance.MoveSingleUnit(u as Attackers, residualEnergy));
                    break;
                }
                else if (u is Defenders)
                {
                    yield return StartCoroutine(IADefendTowers.instance.MoveSingleUnit(u as Defenders, residualEnergy));
                    break;
                }
                else if (u is Ranger)
                {
                    yield return StartCoroutine(IAMoveToResources.instance.MoveSingleUnit(u as Ranger, residualEnergy));
                    break;
                }
            }
        }
        // ----------------- JUEGO DE CARTAS FINAL (AGRESIVO) -----------------
        if (EnergyManager.instance.enemyCurrentEnergy >= 1)
        {
            // Alta probabilidad de jugar cartas si sobra energía o si no hay unidades en mesa (70%).
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