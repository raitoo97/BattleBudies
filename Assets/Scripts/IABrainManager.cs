using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class IABrainManager : MonoBehaviour
{
    public static IABrainManager instance;
    private float chanceToPlayCards = 0.65f;
    private int maxStepsPerUnit = 3;
    [SerializeField] private float defendTriggerDistance = 50f;
    private void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);
    }
    public IEnumerator ExecuteTurn()
    {
        // ----------------- INCIO DE TURNO DE IA -----------------
        if (!IAMoveToTowers.instance || !IADefendTowers.instance || !IAMoveToResources.instance || !IAPlayCards.instance)
        {
            Debug.LogWarning("IA no puede ejecutarse: faltan instancias necesarias");
            yield break;
        }
        yield return StartCoroutine(InitializedTurn());
        List<Attackers> attackers = new List<Attackers>();
        List<Defenders> defenders = new List<Defenders>();
        List<Ranger> rangers = new List<Ranger>();
        GetEnemyUnitsByType(ref attackers, ref defenders, ref rangers);
        attackers.RemoveAll(u => u == null || !u);
        defenders.RemoveAll(u => u == null || !u);
        rangers.RemoveAll(u => u == null || !u);
        int totalUnits = attackers.Count + defenders.Count + rangers.Count;
        // ----------------- DEFENSA -----------------
        Units threat = null;
        if (IsPlayerThreateningTower(out threat))
        {
            Debug.Log("IA: Amenaza detectada, activando defensa");
            yield return StartCoroutine(MoveAllUnitsToThreat(threat));
        }
        else
        {
            // ----------------- MOVIMIENTO POR TIPOS -----------------
            yield return StartCoroutine(HandleUnitsMoves(attackers, defenders, rangers, totalUnits));
        }
        yield return new WaitForSeconds(1f);

        // ----------------- ENERGÍA RESIDUAL -----------------
        List<Units> allUnits = new List<Units>();
        GetEnemyUnitsByType(ref attackers, ref defenders, ref rangers);
        allUnits.AddRange(attackers);
        allUnits.AddRange(defenders);
        allUnits.AddRange(rangers);
        allUnits.RemoveAll(u => u == null || !u);
        yield return StartCoroutine(UseResidualEnergy(allUnits));
        // ----------------- JUEGO DE CARTAS FINAL -----------------
        GetEnemyUnitsByType(ref attackers, ref defenders, ref rangers);
        attackers.RemoveAll(u => u == null || !u);
        defenders.RemoveAll(u => u == null || !u);
        rangers.RemoveAll(u => u == null || !u);
        totalUnits = attackers.Count + defenders.Count + rangers.Count;
        if (EnergyManager.instance.enemyCurrentEnergy >= 1 && (totalUnits == 0 || Random.value < 0.7f))
            yield return StartCoroutine(IAPlayCards.instance?.PlayCards());
        yield return new WaitUntil(() => isBusy());
        Debug.Log("Energía al final del turno IA: " + EnergyManager.instance.enemyCurrentEnergy);
        GameManager.instance.StartPlayerTurn();
    }
    IEnumerator InitializedTurn()
    {
        ClearAllPaths();
        CardPlayManager.instance?.HideAllHandsAtAITurn();
        EnergyManager.instance?.RefillEnemyEnergy();
        DeckManager.instance?.DrawEnemyCard();
        CardPlayManager.instance?.HideAllHandsAtAITurn();
        CanvasManager.instance?.UpdateEnergyUI();
        yield return null;
        yield return new WaitForSeconds(0.5f);
    }
    private bool IsPlayerThreateningTower(out Units threateningUnit)
    {
        threateningUnit = null;
        List<Node> enemyTowerNodes = NodeManager.GetEnemyTowerNodes();
        if (enemyTowerNodes == null || enemyTowerNodes.Count == 0) return false;
        Units[] allUnits = FindObjectsOfType<Units>();
        foreach (Units u in allUnits)
        {
            if (u == null || !u.isPlayerUnit || !u) continue;
            foreach (Node towerNode in enemyTowerNodes)
            {
                float dist = Vector3.Distance(u.transform.position,towerNode.transform.position);
                if (dist <= defendTriggerDistance)
                {
                    threateningUnit = u;
                    Debug.Log("La amenza se llama" + u.name);
                    return true;
                }
            }
        }
        return false;
    }
    private IEnumerator MoveAllUnitsToThreat(Units threat)
    {
        if (threat == null || !threat)
        {
            Debug.LogWarning("MoveAllUnitsToThreat: La amenaza es null");
            yield break;
        }
        Units[] allUnits = FindObjectsOfType<Units>();
        if(allUnits == null || allUnits.Length == 0) yield break;
        List<Units> candidateUnits = new List<Units>();
        int maxUnitsToSend = 2;
        // ----------------- RANGERS -----------------
        List<Ranger> rangers = new List<Ranger>();
        foreach (Units unit in allUnits)
            if (unit != null && !unit.isPlayerUnit && unit is Ranger)
                rangers.Add(unit as Ranger);
        if(threat != null)
        {
            rangers.Sort((a, b) => Vector3.Distance(a.transform.position, threat.transform.position).CompareTo(Vector3.Distance(b.transform.position, threat.transform.position)));
        }
        foreach (Ranger ranger in rangers)
        {
            if(ranger == null || !ranger) continue;
            candidateUnits.Add(ranger);
            if (candidateUnits.Count >= maxUnitsToSend) break;
        }
        // ----------------- DEFENDERS -----------------
        if (candidateUnits.Count < maxUnitsToSend)
        {
            List<Defenders> defenders = new List<Defenders>();
            foreach (Units unit in allUnits)
                if (unit != null && !unit.isPlayerUnit && unit is Defenders && !candidateUnits.Contains(unit))
                    defenders.Add(unit as Defenders);
            if(threat != null)
            {
                defenders.Sort((a, b) => Vector3.Distance(a.transform.position, threat.transform.position).CompareTo(Vector3.Distance(b.transform.position, threat.transform.position)));
            }
            foreach (Defenders defender in defenders)
            {
                if (defender == null || !defender) continue;
                candidateUnits.Add(defender);
                if (candidateUnits.Count >= maxUnitsToSend) break;
            }
        }
        // ----------------- ATTACKERS -----------------
        if (candidateUnits.Count < maxUnitsToSend)
        {
            List<Attackers> attackers = new List<Attackers>();
            foreach (Units unit in allUnits)
                if (unit != null && !unit.isPlayerUnit && unit is Attackers && !candidateUnits.Contains(unit))
                    attackers.Add(unit as Attackers);
            if (threat != null) 
            {
                attackers.Sort((a, b) => Vector3.Distance(a.transform.position, threat.transform.position).CompareTo(Vector3.Distance(b.transform.position, threat.transform.position)));
            }
            foreach (Attackers attacker in attackers)
            {
                if (attacker == null || !attacker) continue;
                candidateUnits.Add(attacker);
                if (candidateUnits.Count >= maxUnitsToSend) break;
            }
        }
        if (candidateUnits.Count == 0)
        {
            Debug.Log("MoveAllUnitsToThreat: No hay unidades para mover.");
            yield break;
        }
        Debug.Log($"IA: Moviendo {candidateUnits.Count} unidades hacia {threat.name}");
        Node playerNode = threat.currentNode;
        Node safeNeighbor = null;
        if (playerNode != null && playerNode.Neighbors != null)
        {
            foreach (Node node in playerNode.Neighbors)
            {
                if (node != null && node.IsEmpty())
                {
                    safeNeighbor = node;
                    break;
                }
            }
        }
        if (safeNeighbor == null)
        {
            safeNeighbor = NodeManager.GetClosetNode(threat.transform.position);
            if(safeNeighbor == null) 
            {
                Debug.LogError("MoveAllUnitsToThreat: No hay nodo seguro ni nodo más cercano. CANCELADO.");
                yield break;
            }
            Debug.LogWarning("MoveAllUnitsToThreat: Ningún vecino libre. Usando nodo más cercano.");
        }
        foreach (Units unit in candidateUnits)
        {
            if (unit == null || unit.currentNode == null)continue;
            if (EnergyManager.instance.enemyCurrentEnergy < 1)
            {
                Debug.Log("IA sin energía para mover más defensores.");
                break;
            }
            Node finalTarget = null;
            // Si el safeNeighbor aún está libre, usarlo
            if (safeNeighbor != null && safeNeighbor.IsEmpty())
            {
                finalTarget = safeNeighbor;
            }
            else
            {
                // Buscar otro vecino libre
                foreach (Node node in threat.currentNode.Neighbors)
                {
                    if (node != null && node.IsEmpty())
                    {
                        finalTarget = node;
                        break;
                    }
                }
            }
            // Fallback al más cercano si sigue sin haber
            if (finalTarget == null) 
            {
                finalTarget = NodeManager.GetClosetNode(threat.transform.position);
            }
            List<Node> path = PathFinding.CalculateAstart(unit.currentNode, finalTarget);
            if (path == null || path.Count == 0)
            {
                Debug.LogWarning($"IA: {unit.name} no encontró camino hacia el objetivo.");
                continue;
            }
            int stepsToMove = Mathf.Min(EnergyManager.instance.enemyCurrentEnergy, path.Count - 1);
            if (stepsToMove <= 0) continue;
            List<Node> nodesToMove = path.GetRange(0, stepsToMove);
            Debug.Log($"IA: Moviendo {unit.name} hacia zona de defensa ({stepsToMove} pasos).");
            yield return StartCoroutine(IAMoveToTowers.instance.ExecuteMovementPathWithSavingThrows(unit, nodesToMove));
            yield return new WaitForSeconds(0.15f);
        }
        Debug.Log("IA: Defensa completada.");
    }
    private IEnumerator HandleUnitsMoves(List<Attackers> attackers,List<Defenders> defenders,List<Ranger> rangers,int totalUnits)
    {
        float globalChance = Random.value;
        if (globalChance < 0.10f)
        {
            Debug.Log("IA impredecible: ATAQUE GLOBAL  TODOS los tipos van a las torres del player");
            yield return StartCoroutine(IAMoveToTowers.instance.MoveAllEnemyUnitsToTowers());
            yield break;
        }
        //MOVIMIENTOS INDIVIDUALES NORMALES
        Debug.Log("IA: movimientos individuales normales por tipo");
        yield return StartCoroutine(MoveAttackers(attackers, totalUnits));
        yield return StartCoroutine(MoveDefenders(defenders, rangers));
        yield return StartCoroutine(MoveRangers(rangers));
        yield return new WaitForSeconds(1f);
    }
    private IEnumerator MoveAttackers(List<Attackers> attackers, int totalUnits)
    {
        if (attackers == null || attackers.Count == 0) yield break;
        int initialFairShare = (totalUnits > 0) ? EnergyManager.instance.enemyCurrentEnergy / totalUnits : 0;
        int energyPerUnit = Mathf.Max(1, Mathf.Min(initialFairShare, maxStepsPerUnit));
        foreach (Units u in attackers)
        {
            if (u == null || !u) continue;
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
            yield return StartCoroutine(IAPlayCards.instance?.PlayOneCard());
        }
    }
    private IEnumerator MoveDefenders(List<Defenders> defenders, List<Ranger> rangers)
    {
        if (defenders == null || defenders.Count == 0) yield break;
        int remainingUnits = defenders.Count + rangers.Count;
        int currentEnergy = EnergyManager.instance.enemyCurrentEnergy;
        int energyPerUnit = Mathf.Max(1, Mathf.Min((remainingUnits > 0 ? currentEnergy / remainingUnits : 1), maxStepsPerUnit));
        foreach (Units u in defenders)
        {
            if (u == null || !u) continue;
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
            yield return StartCoroutine(IAPlayCards.instance?.PlayOneCard());
        }
    }
    private IEnumerator MoveRangers(List<Ranger> rangers)
    {
        if (rangers == null || rangers.Count == 0) yield break;
        int currentEnergy = EnergyManager.instance.enemyCurrentEnergy;
        int energyPerUnit = Mathf.Max(1, Mathf.Min((rangers.Count > 0 ? currentEnergy / rangers.Count : 1), maxStepsPerUnit));
        foreach (Units u in rangers)
        {
            if (u == null || !u) continue;
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
            yield return StartCoroutine(IAPlayCards.instance?.PlayOneCard());
        }
    }
    private IEnumerator UseResidualEnergy(List<Units> allUnits)
    {
        bool anyUnitCanAct = false;
        foreach (Units Unit in allUnits)
        {
            if (Unit == null || !Unit) continue;
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
                if (Unit == null || !Unit) continue;
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
                yield return StartCoroutine(IAPlayCards.instance?.PlayOneCard());
                anyMoved = true;
            }
        } while (EnergyManager.instance.enemyCurrentEnergy >= 1);
    }
    private void GetEnemyUnitsByType(ref List<Attackers> atk,ref List<Defenders> def,ref List<Ranger> rng)
    {
        Units[] allUnits = GameObject.FindObjectsOfType<Units>();
        foreach (Units u in allUnits)
        {
            if (u.isPlayerUnit || u == null || !u) continue;
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