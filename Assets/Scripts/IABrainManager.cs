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
        yield return StartCoroutine(InitializedTurn());
        List<Attackers> attackers = new List<Attackers>();
        List<Defenders> defenders = new List<Defenders>();
        List<Ranger> rangers = new List<Ranger>();
        GetEnemyUnitsByType(ref attackers, ref defenders, ref rangers);
        int totalUnits = attackers.Count + defenders.Count + rangers.Count;
        // ----------------- MOVIMIENTO POR TIPOS -----------------
        yield return StartCoroutine(MoveAttackers(attackers, totalUnits));
        yield return StartCoroutine(MoveDefenders(defenders, rangers));
        yield return StartCoroutine(MoveRangers(rangers));
        yield return new WaitForSeconds(1f);
        Debug.Log("Energía RESIDUAL restante al final del turno IA: " + EnergyManager.instance.enemyCurrentEnergy);
        // ----------------- ENERGÍA RESIDUAL -----------------
        List<Units> allUnits = new List<Units>();
        GetEnemyUnitsByType(ref attackers, ref defenders, ref rangers);
        allUnits.AddRange(attackers);
        allUnits.AddRange(defenders);
        allUnits.AddRange(rangers);
        yield return StartCoroutine(UseResidualEnergy(allUnits));
        // ----------------- JUEGO DE CARTAS FINAL -----------------
        GetEnemyUnitsByType(ref attackers, ref defenders, ref rangers);
        totalUnits = attackers.Count + defenders.Count + rangers.Count;
        if (EnergyManager.instance.enemyCurrentEnergy >= 1 && (totalUnits == 0 || Random.value < 0.7f))
            yield return StartCoroutine(IAPlayCards.instance.PlayCards());
        yield return new WaitUntil(() => isBusy());
        Debug.Log("Energía al final del turno IA: " + EnergyManager.instance.enemyCurrentEnergy);
        GameManager.instance.StartPlayerTurn();
    }
    IEnumerator InitializedTurn()
    {
        ClearAllPaths();
        CardPlayManager.instance.HideAllHandsAtAITurn();
        EnergyManager.instance.RefillEnemyEnergy();
        DeckManager.instance.DrawEnemyCard();
        CardPlayManager.instance.HideAllHandsAtAITurn();
        CanvasManager.instance.UpdateEnergyUI();
        yield return null;
        yield return new WaitForSeconds(0.5f);
    }
    private IEnumerator MoveAttackers(List<Attackers> attackers, int totalUnits)
    {
        if (attackers.Count == 0) yield break;
        int initialFairShare = (totalUnits > 0) ? EnergyManager.instance.enemyCurrentEnergy / totalUnits : 0;
        int energyPerUnit = Mathf.Max(1, Mathf.Min(initialFairShare, maxStepsPerUnit));
        foreach (Units u in attackers)
        {
            if (EnergyManager.instance.enemyCurrentEnergy < 1) break;
            Debug.Log($"IA moviendo Attackers {u.gameObject.name}");
            Debug.Log($"posicion de {u.gameObject.name} antes de moverse {u.currentNode}");
            int moveEnergy = Mathf.Min(energyPerUnit, EnergyManager.instance.enemyCurrentEnergy);
            yield return StartCoroutine(IAMoveToTowers.instance.MoveSingleUnit(u, moveEnergy));
            yield return new WaitForSeconds(0.2f);
            Debug.Log($"posicion de {u.gameObject.name} Despues de moverse {u.currentNode}");
        }

        if (Random.value < chanceToPlayCards && EnergyManager.instance.enemyCurrentEnergy >= 1)
        {
            Debug.Log("IA decide jugar carta tras mover Attackers");
            yield return StartCoroutine(IAPlayCards.instance.PlayOneCard());
        }
    }
    private IEnumerator MoveDefenders(List<Defenders> defenders, List<Ranger> rangers)
    {
        if (defenders.Count == 0) yield break;
        int remainingUnits = defenders.Count + rangers.Count;
        int currentEnergy = EnergyManager.instance.enemyCurrentEnergy;
        int energyPerUnit = Mathf.Max(1, Mathf.Min((remainingUnits > 0 ? currentEnergy / remainingUnits : 1), maxStepsPerUnit));
        foreach (Units u in defenders)
        {
            if (EnergyManager.instance.enemyCurrentEnergy < 1) break;
            if (NodeManager.GetHealthNodes().Contains(u.currentNode))
            {
                yield return StartCoroutine(IADefendTowers.instance.MoveSingleUnit(u, 0));
                continue;
            }
            Debug.Log($"IA moviendo Defenders {u.gameObject.name}");
            Debug.Log($"posicion de {u.gameObject.name} antes de moverse {u.currentNode}");
            int moveEnergy = Mathf.Min(energyPerUnit, EnergyManager.instance.enemyCurrentEnergy);
            yield return StartCoroutine(IADefendTowers.instance.MoveSingleUnit(u, moveEnergy));
            yield return new WaitUntil(() => !HealthTowerManager.instance.onColectedHealth);
            yield return new WaitForSeconds(0.2f);
            Debug.Log($"posicion de {u.gameObject.name} Despues de moverse {u.currentNode}");
        }
        if (Random.value < chanceToPlayCards && EnergyManager.instance.enemyCurrentEnergy >= 1)
        {
            Debug.Log("IA decide jugar carta tras mover Defender");
            yield return StartCoroutine(IAPlayCards.instance.PlayOneCard());
        }
    }
    private IEnumerator MoveRangers(List<Ranger> rangers)
    {
        if (rangers.Count == 0) yield break;
        int currentEnergy = EnergyManager.instance.enemyCurrentEnergy;
        int energyPerUnit = Mathf.Max(1, Mathf.Min((rangers.Count > 0 ? currentEnergy / rangers.Count : 1), maxStepsPerUnit));
        foreach (Units u in rangers)
        {
            if (EnergyManager.instance.enemyCurrentEnergy < 1) break;
            if (NodeManager.GetResourcesNode().Contains(u.currentNode))
            {
                yield return StartCoroutine(IAMoveToResources.instance.MoveSingleUnit(u, 0));
                continue;
            }
            Debug.Log($"IA moviendo Rangers {u.gameObject.name}");
            Debug.Log($"posicion de {u.gameObject.name} antes de moverse {u.currentNode}");
            int moveEnergy = Mathf.Min(energyPerUnit, EnergyManager.instance.enemyCurrentEnergy);
            yield return StartCoroutine(IAMoveToResources.instance.MoveSingleUnit(u, moveEnergy));
            yield return new WaitUntil(() => !ResourcesManager.instance.onColectedResources);
            yield return new WaitForSeconds(0.2f);
            Debug.Log($"posicion de {u.gameObject.name} Despues de moverse {u.currentNode}");
        }
        if (Random.value < chanceToPlayCards && EnergyManager.instance.enemyCurrentEnergy >= 1)
        {
            Debug.Log("IA decide jugar carta tras mover Ranger");
            yield return StartCoroutine(IAPlayCards.instance.PlayOneCard());
        }
    }
    private IEnumerator UseResidualEnergy(List<Units> allUnits)
    {
        bool anyUnitCanAct = false;
        foreach (Units Unit in allUnits)
        {
            if (Unit is Attackers || Unit is Ranger || (Unit is Defenders && !NodeManager.GetHealthNodes().Contains(Unit.currentNode)))
            {
                anyUnitCanAct = true;
                break;
            }
        }
        if (!anyUnitCanAct) yield break;
        bool anyMoved;
        int safetyCounter = 0;
        do
        {
            anyMoved = false;
            safetyCounter++;
            if (safetyCounter > 20) break;
            int residualEnergy = EnergyManager.instance.enemyCurrentEnergy;
            foreach (Units Unit in allUnits)
            {
                if (Unit == null) continue;
                // Saltar unidades que ya están en nodos de curación o recursos
                if (Unit is Defenders && NodeManager.GetHealthNodes().Contains(Unit.currentNode)) continue;
                if (Unit is Ranger && NodeManager.GetResourcesNode().Contains(Unit.currentNode)) continue;
                if (residualEnergy < 1) break;
                if (Unit is Attackers)
                {
                    Debug.Log($"IA usando energía residual para mover Attacker {Unit.gameObject.name}");
                    Debug.Log($"IA Nodo Anterior al movimiento del attacker {Unit.currentNode}");
                    yield return StartCoroutine(IAMoveToTowers.instance.MoveSingleUnit(Unit as Attackers, residualEnergy));
                    Debug.Log($"IA Nodo Posterior al movimiento del attacker {Unit.currentNode}");
                    anyMoved = true;
                }
                else if (Unit is Defenders)
                {
                    Debug.Log($"IA usando energía residual para mover Defender {Unit.gameObject.name}");
                    Debug.Log($"IA Nodo Anterior al movimiento del Defender {Unit.currentNode}");
                    yield return StartCoroutine(IADefendTowers.instance.MoveSingleUnit(Unit as Defenders, residualEnergy));
                    Debug.Log($"IA Nodo Posterior al movimiento del Defender {Unit.currentNode}");
                    yield return new WaitUntil(() => !HealthTowerManager.instance.onColectedHealth);
                    anyMoved = true;
                }
                else if (Unit is Ranger)
                {
                    Debug.Log($"IA usando energía residual para mover Ranger {Unit.gameObject.name}");
                    Debug.Log($"IA Nodo Anterior al movimiento del Ranger {Unit.currentNode}");
                    yield return StartCoroutine(IAMoveToResources.instance.MoveSingleUnit(Unit as Ranger, residualEnergy));
                    Debug.Log($"IA Nodo Posterior al movimiento del Ranger {Unit.currentNode}");
                    yield return new WaitUntil(() => !ResourcesManager.instance.onColectedResources);
                    anyMoved = true;
                }
                if (anyMoved) break;
            }
            if (!anyMoved && EnergyManager.instance.enemyCurrentEnergy >= 1)
            {
                Debug.Log("IA no puede mover unidades restantes, intenta jugar carta con energía residual.");
                yield return StartCoroutine(IAPlayCards.instance.PlayOneCard());
                anyMoved = true;
            }
        } while (EnergyManager.instance.enemyCurrentEnergy >= 1);
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