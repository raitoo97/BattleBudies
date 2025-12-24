using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class IABrainManager : MonoBehaviour
{
    public static IABrainManager instance;
    private float chanceToPlayCards = 0.85f;
    private int maxStepsPerUnit = 3;
    [SerializeField]private float defendTriggerDistance;
    private float arriveToTarget = 23;
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
        yield return StartCoroutine(HandleEnemyUnitsOnSpecialNodes());
        List<Attackers> attackers = new List<Attackers>();
        List<Defenders> defenders = new List<Defenders>();
        List<Ranger> rangers = new List<Ranger>();
        GetEnemyUnitsByType(ref attackers, ref defenders, ref rangers);
        attackers.RemoveAll(u => u == null || !u);
        defenders.RemoveAll(u => u == null || !u);
        rangers.RemoveAll(u => u == null || !u);
        int totalUnits = attackers.Count + defenders.Count + rangers.Count;
        //  Si no tiene unidades, jugar cartas primero
        if (totalUnits == 0 && EnergyManager.instance.enemyCurrentEnergy >= 1)
        {
            Debug.Log("IA no tiene unidades: juega cartas iniciales");
            yield return StartCoroutine(IAPlayCards.instance?.PlayCards());
            // Actualizar lista de unidades recién invocadas
            GetEnemyUnitsByType(ref attackers, ref defenders, ref rangers);
            List<Units> newlySpawnedUnits = new List<Units>();
            newlySpawnedUnits.AddRange(attackers);
            newlySpawnedUnits.AddRange(defenders);
            newlySpawnedUnits.AddRange(rangers);
            newlySpawnedUnits.RemoveAll(u => u == null || !u);
            if (newlySpawnedUnits.Count > 0)
            {
                yield return StartCoroutine(UseResidualEnergy(newlySpawnedUnits));
            }
        }
        Units threat;
        if (IsPlayerThreateningTower(out threat) && EnergyManager.instance.enemyCurrentEnergy > 0)
        {
            yield return StartCoroutine(MoveAllUnitsToThreat(threat));
        }
        Units specialTarget;
        if (IsPlayerUsingSpecialNode(out specialTarget) && EnergyManager.instance.enemyCurrentEnergy > 0)
        {
            yield return StartCoroutine(SendUnitToKillTarget(specialTarget));
        }
        yield return StartCoroutine(HandleUnitsMoves(attackers, defenders, rangers, totalUnits));
        yield return new WaitForSeconds(0.5f);
        List<Units> allUnits = new List<Units>();
        GetEnemyUnitsByType(ref attackers, ref defenders, ref rangers);
        allUnits.AddRange(attackers);
        allUnits.AddRange(defenders);
        allUnits.AddRange(rangers);
        allUnits.RemoveAll(u => u == null || !u);
        yield return StartCoroutine(UseResidualEnergy(allUnits));
        yield return new WaitUntil(() => isBusy());
        GameManager.instance.StartPlayerTurn();
    }
    IEnumerator InitializedTurn()
    {
        ClearAllPaths();
        foreach (Ranger r in FindObjectsOfType<Ranger>())
        {
            if (r != null && !r.isPlayerUnit)
                r.hasCollectedThisTurn = false;
        }
        foreach (Units u in FindObjectsOfType<Units>())
        {
            if (u == null || u.isPlayerUnit) continue;
            u.hasHealthedTowerThisTurn = false;
            u.hasAttackedTowerThisTurn = false;
        }
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
    private bool IsPlayerUsingSpecialNode(out Units target)
    {
        target = null;
        List<Node> resourceNodes = NodeManager.GetResourcesNode();
        List<Node> healthNodes = NodeManager.GetHealthNodes();
        Units[] allUnits = FindObjectsOfType<Units>();
        foreach (Units u in allUnits)
        {
            if (u == null || !u.isPlayerUnit || u.currentNode == null)
                continue;
            if (u is Ranger && resourceNodes.Contains(u.currentNode))
            {
                print("Ranger detectado en nodo de recursos: " + u.name);
                target = u;
                return true;
            }
            if (u is Defenders && healthNodes.Contains(u.currentNode))
            {
                print("Defender detectado en nodo de recursos: " + u.name);
                target = u;
                return true;
            }
        }
        return false;
    }
    private IEnumerator SendUnitToKillTarget(Units target)
    {
        if (target == null)
        {
            // Liberar todas las unidades pendientes si el target ya no existe
            Units[] allUnits = FindObjectsOfType<Units>();
            foreach (Units u in allUnits)
            {
                if (!u.isPlayerUnit)
                    u.isPendingTarget = false;
            }
            yield break;
        }
        // 1. Buscar si ya hay alguna unidad enemiga con isPendingTarget = true
        Units unitToMove = null;
        Units[] enemyUnits = FindObjectsOfType<Units>();
        foreach (Units u in enemyUnits)
        {
            if (u != null && !u.isPlayerUnit && u.isPendingTarget)
            {
                unitToMove = u;
                break;
            }
        }
        // 2. Si no hay, elegir la unidad enemiga más cercana al target
        if (unitToMove == null)
        {
            float minDist = float.MaxValue;
            foreach (Units u in enemyUnits)
            {
                if (u != null && !u.isPlayerUnit)
                {
                    float dist = Vector3.Distance(u.transform.position, target.transform.position);
                    if (dist < minDist)
                    {
                        minDist = dist;
                        unitToMove = u;
                    }
                }
            }
            if (unitToMove != null)
                unitToMove.isPendingTarget = true;
        }
        // 3. Mover la unidad seleccionada al target
        if (unitToMove != null)
            yield return StartCoroutine(MoveUnitToTarget(unitToMove, target));
    }
    private IEnumerator MoveUnitToTarget(Units unit, Units target)
    {
        if (unit == null)
        {
            Debug.LogWarning("MoveUnitToTarget: unit es null");
            yield break;
        }
        if (target == null)
        {
            Debug.LogWarning("MoveUnitToTarget: target es null");
            yield break;
        }
        if (unit.currentNode == null)
        {
            Debug.LogWarning($"MoveUnitToTarget: {unit.name} no tiene currentNode");
            yield break;
        }
        int huntMaxSteps = 5;
        int energyForThisUnit = Mathf.Min(EnergyManager.instance.enemyCurrentEnergy, huntMaxSteps);
        Node targetNode = target.currentNode;
        Node finalTarget = null;
        // Buscar un vecino libre del target
        foreach (Node n in targetNode.Neighbors)
        {
            if (n != null && n.IsEmpty() && !n._isBlock)
            {
                finalTarget = n;
                break;
            }
        }
        // Si ningún vecino está libre, usar el nodo del target si está libre y no bloqueado
        if (finalTarget == null)
        {
            if (targetNode.IsEmpty() && !targetNode._isBlock)
            {
                finalTarget = targetNode;
                Debug.Log($"IA: Nodo objetivo {targetNode.name} está libre y no bloqueado, se usará como finalTarget");
            }
            else
            {
                Node closest = NodeManager.GetClosetNode(target.transform.position);
                if (closest != null && closest.IsEmpty() && !closest._isBlock)
                {
                    finalTarget = closest;
                    Debug.Log($"IA: Usando nodo más cercano válido: {finalTarget.name}");
                }
                else
                {
                    Debug.LogWarning(
                        $"MoveUnitToTarget: Nodo más cercano inválido. " +
                        $"IsEmpty={closest?.IsEmpty()} _isBlock={closest?._isBlock}"
                    );
                    yield break;
                }
            }
        }
        if (finalTarget == null)
        {
            Debug.LogWarning("MoveUnitToTarget: No se encontró nodo final válido");
            yield break;
        }
        List<Node> path = PathFinding.CalculateAstart(unit.currentNode, finalTarget);
        if (path == null || path.Count == 0)
        {
            Debug.LogWarning($"MoveUnitToTarget: No se encontró path de {unit.name} a {finalTarget.name}");
            yield break;
        }
        int steps = Mathf.Min(path.Count, energyForThisUnit);
        if (steps <= 0)
        {
            Debug.LogWarning($"MoveUnitToTarget: pasos calculados <= 0 para {unit.name}");
            yield break;
        }
        List<Node> nodesToMove = path.GetRange(0, steps);
        Debug.Log($"IA: Moviendo {unit.name} hacia {target.name} con {steps} pasos");
        yield return StartCoroutine(IAMoveToTowers.instance.ExecuteMovementPathWithSavingThrows(unit, nodesToMove));
        if (Vector3.Distance(unit.transform.position, target.transform.position) < arriveToTarget || target==null)
            unit.isPendingTarget = false;
    }
    private IEnumerator MoveAllUnitsToThreat(Units threat)
    {
        if (threat == null)
        {
            Debug.LogWarning("MoveAllUnitsToThreat: La amenaza es null");
            yield break;
        }
        Units unitToMove = null;
        float minDist = float.MaxValue;
        Units[] allUnits = FindObjectsOfType<Units>();
        foreach (Units u in allUnits)
        {
            if (u != null && !u.isPlayerUnit)
            {
                float dist = Vector3.Distance(u.transform.position, threat.transform.position);
                if (dist < minDist)
                {
                    minDist = dist;
                    unitToMove = u;
                }
            }
        }
        if (unitToMove != null)
        {
            Debug.Log($"IA: Unidad más cercana al threat {threat.name} seleccionada: {unitToMove.name}, distancia: {minDist}");
            yield return StartCoroutine(MoveUnitToThreat(unitToMove, threat));
        }
        else
        {
            Debug.Log("IA: No hay unidades disponibles para mover hacia la amenaza");
        }
    }
    private IEnumerator MoveUnitToThreat(Units unit, Units threat)
    {
        if (unit == null || threat == null || unit.currentNode == null) yield break;
        Node threatNode = threat.currentNode;
        Node finalTarget = null;
        if (threatNode != null)
        {
            foreach (Node n in threatNode.Neighbors)
            {
                if (n != null && n.IsEmpty() && !n._isBlock)
                {
                    finalTarget = n;
                    break;
                }
            }
        }
        if (finalTarget == null)
        {
            if (threatNode != null && threatNode.IsEmpty() && !threatNode._isBlock)
            {
                finalTarget = threatNode;
                Debug.Log($"IA: Nodo de amenaza {threatNode.name} está libre y no bloqueado");
            }
            else
            {
                Node closest = NodeManager.GetClosetNode(threat.transform.position);
                if (closest != null && closest.IsEmpty() && !closest._isBlock)
                {
                    finalTarget = closest;
                    Debug.Log($"IA: Usando nodo más cercano válido a la amenaza: {finalTarget.name}");
                }
                else
                {
                    Debug.LogWarning($"MoveUnitToThreat: Nodo más cercano inválido. " +$"IsEmpty={closest?.IsEmpty()} _isBlock={closest?._isBlock}");
                    yield break;
                }
            }
        }
        if (finalTarget == null)
        {
            Debug.LogWarning("MoveUnitToThreat: No se encontró nodo final válido");
            yield break;
        }
        List<Node> path = PathFinding.CalculateAstart(unit.currentNode, finalTarget);
        if (path == null || path.Count == 0) yield break;
        int stepsToMove = Mathf.Min(EnergyManager.instance.enemyCurrentEnergy, path.Count);
        if (stepsToMove <= 0) yield break;
        List<Node> nodesToMove = path.GetRange(0, stepsToMove);
        Debug.Log($"IA: Moviendo {unit.name} hacia amenaza {threat.name} ({stepsToMove} pasos)");
        yield return StartCoroutine(IAMoveToTowers.instance.ExecuteMovementPathWithSavingThrows(unit, nodesToMove));
    }
    private IEnumerator HandleUnitsMoves(List<Attackers> attackers,List<Defenders> defenders,List<Ranger> rangers,int totalUnits)
    {
        float globalChance = Random.value;
        if (globalChance < 0.1f)
        {
            Debug.Log("Ataque Global de parte de la IA");
            yield return StartCoroutine(IAMoveToTowers.instance.MoveAllEnemyUnitsToTowers(attackers));
            yield break;
        }
        //MOVIMIENTOS INDIVIDUALES NORMALES
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
            if (u.isPendingTarget) continue;
            if (EnergyManager.instance.enemyCurrentEnergy < 1) break;
            Debug.Log($"IA moviendo Attackers {u.gameObject.name}");
            int moveEnergy = Mathf.Min(energyPerUnit, EnergyManager.instance.enemyCurrentEnergy);
            yield return StartCoroutine(IAMoveToTowers.instance.MoveSingleUnit(u, moveEnergy));
            yield return new WaitForSeconds(0.2f);
        }
        float random = Random.value;
        if (random < chanceToPlayCards && EnergyManager.instance.enemyCurrentEnergy >= 1)
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
            if (u.isPendingTarget) continue;
            // Si no hay energía no intento mover
            if (EnergyManager.instance.enemyCurrentEnergy < 1) break;
            Debug.Log($"IA moviendo Defenders {u.gameObject.name}");
            int moveEnergy = Mathf.Min(energyPerUnit, EnergyManager.instance.enemyCurrentEnergy);
            yield return StartCoroutine(IADefendTowers.instance.MoveSingleUnit(u, moveEnergy));
            yield return StartCoroutine(HandleSingleUnitOnSpecialNode(u));
            yield return new WaitForSeconds(0.2f);
        }
        float random = Random.value;
        if (random < chanceToPlayCards && EnergyManager.instance.enemyCurrentEnergy >= 1)
        {
            Debug.Log("IA decide jugar carta tras mover Defender");
            yield return StartCoroutine(IAPlayCards.instance?.PlayOneCard());
        }
    }
    private IEnumerator MoveRangers(List<Ranger> rangers)
    {
        if (rangers == null || rangers.Count == 0) yield break;
        UpgradeManager.instance.UpgradeEnemyUnits();
        List<Node> resourceNodes = NodeManager.GetResourcesNode();
        int currentEnergy = EnergyManager.instance.enemyCurrentEnergy;
        int energyPerUnit = Mathf.Max(1, Mathf.Min((rangers.Count > 0 ? currentEnergy / rangers.Count : 1), maxStepsPerUnit));
        foreach (Units u in rangers)
        {
            if (u == null || !u) continue;
            if (u.isPendingTarget) continue;
            if (EnergyManager.instance.enemyCurrentEnergy < 1) break;
            Debug.Log($"IA moviendo Rangers {u.gameObject.name}");
            int moveEnergy = Mathf.Min(energyPerUnit, EnergyManager.instance.enemyCurrentEnergy);
            yield return StartCoroutine(IAMoveToResources.instance.MoveSingleUnit(u, moveEnergy));
            yield return StartCoroutine(HandleSingleUnitOnSpecialNode(u));
            yield return new WaitForSeconds(0.2f);
        }
        UpgradeManager.instance.UpgradeEnemyUnits();
        float random = Random.value;
        if (random < chanceToPlayCards && EnergyManager.instance.enemyCurrentEnergy >= 1)
        {
            Debug.Log("IA decide jugar carta tras mover Ranger");
            yield return StartCoroutine(IAPlayCards.instance?.PlayOneCard());
        }
    }
    private IEnumerator UseResidualEnergy(List<Units> allUnits)
    {
        if (allUnits == null || allUnits.Count == 0) yield break;
        int safetyCounter = 0;
        bool anyMoved;
        do
        {
            anyMoved = false;
            safetyCounter++;
            if (safetyCounter > 50) break;
            Units threat;
            if (EnergyManager.instance.enemyCurrentEnergy > 0 && IsPlayerThreateningTower(out threat))
            {
                yield return StartCoroutine(MoveAllUnitsToThreat(threat));
                anyMoved = true;
                continue;
            }
            Units specialTarget;
            if (EnergyManager.instance.enemyCurrentEnergy > 0 && IsPlayerUsingSpecialNode(out specialTarget))
            {
                yield return StartCoroutine(SendUnitToKillTarget(specialTarget));
                anyMoved = true;
                continue;
            }
            foreach (Units u in allUnits)
            {
                if (u == null || !u) continue;
                if (u is Defenders && NodeManager.GetHealthNodes().Contains(u.currentNode)) continue;
                if (u is Ranger && NodeManager.GetResourcesNode().Contains(u.currentNode)) continue;
                int energyAvailable = EnergyManager.instance.enemyCurrentEnergy;
                if (energyAvailable <= 0) break;
                if (u is Attackers)
                {
                    yield return StartCoroutine(IAMoveToTowers.instance.MoveSingleUnit(u as Attackers, energyAvailable));
                    anyMoved = true;
                }
                else if (u is Defenders)
                {
                    yield return StartCoroutine(IADefendTowers.instance.MoveSingleUnit(u as Defenders, energyAvailable));
                    yield return new WaitUntil(() => !HealthTowerManager.instance.onColectedHealth);
                    anyMoved = true;
                }
                else if (u is Ranger)
                {
                    yield return StartCoroutine(IAMoveToResources.instance.MoveSingleUnit(u as Ranger, energyAvailable));
                    yield return new WaitUntil(() => !ResourcesManager.instance.onColectedResources);
                    anyMoved = true;
                }
                u.isPendingTarget = false;
                if (anyMoved) break;
            }
            if (!anyMoved && EnergyManager.instance.enemyCurrentEnergy > 0)
            {
                yield return StartCoroutine(IAPlayCards.instance?.PlayOneCard());
                anyMoved = true;
            }
            if (!anyMoved && EnergyManager.instance.enemyCurrentEnergy <= 0)
                break;

        } while (EnergyManager.instance.enemyCurrentEnergy > 0);
        while (EnergyManager.instance.enemyCurrentEnergy > 0)
        {
            foreach (Units u in allUnits)
            {
                if (u == null || !u) continue;
                int moveEnergy = Mathf.Min(EnergyManager.instance.enemyCurrentEnergy, 1); // 1 paso a la vez
                if (moveEnergy <= 0) continue;
                if (u is Attackers)
                    yield return StartCoroutine(IAMoveToTowers.instance.MoveSingleUnit(u as Attackers, moveEnergy));
                else if (u is Defenders)
                    yield return StartCoroutine(IADefendTowers.instance.MoveSingleUnit(u as Defenders, moveEnergy));
                else if (u is Ranger)
                    yield return StartCoroutine(IAMoveToResources.instance.MoveSingleUnit(u as Ranger, moveEnergy));
                if (EnergyManager.instance.enemyCurrentEnergy <= 0) break;
            }
        }
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
    private IEnumerator HandleEnemyUnitsOnSpecialNodes()
    {
        Units[] allUnits = FindObjectsOfType<Units>();
        foreach (Units u in allUnits)
        {
            if (u.isPlayerUnit || u.currentNode == null) continue;
            if (u is Ranger ranger && NodeManager.GetResourcesNode().Contains(u.currentNode) && !ranger.hasCollectedThisTurn)
            {
                ranger.hasCollectedThisTurn = true;
                ResourcesManager.instance.StartRecolectedResources(ranger);
                yield return new WaitUntil(() => !ResourcesManager.instance.onColectedResources);
                UpgradeManager.instance.UpgradeEnemyUnits();
            }
            if (u is Defenders def && NodeManager.GetHealthNodes().Contains(u.currentNode) && !u.hasHealthedTowerThisTurn)
            {
                HealthTowerManager.instance.StartRecolectedHealth(def);
                yield return new WaitUntil(() => !HealthTowerManager.instance.onColectedHealth);
                u.hasHealthedTowerThisTurn = true;
            }
            if (TowerManager.instance.TryGetTowerAtNode(u.currentNode, out Tower tower))
            {
                if (!u.hasAttackedTowerThisTurn && TowerManager.instance.CanUnitAttackTower(u, tower))
                {
                    u.hasAttackedTowerThisTurn = true;
                    yield return StartCoroutine(CombatManager.instance.StartCombatWithTowerAI_Coroutine(u, tower));
                }
            }
        }
    }
    public IEnumerator HandleSingleUnitOnSpecialNode(Units u)
    {
        if (u == null || u.currentNode == null) yield break;
        if (u is Ranger r&& NodeManager.GetResourcesNode().Contains(u.currentNode)&& !r.hasCollectedThisTurn)
        {
            r.hasCollectedThisTurn = true;
            ResourcesManager.instance.StartRecolectedResources(r);
            yield return new WaitUntil(() => !ResourcesManager.instance.onColectedResources);
            UpgradeManager.instance.UpgradeEnemyUnits();
        }
        if (u is Defenders d&& NodeManager.GetHealthNodes().Contains(u.currentNode)&& !d.hasHealthedTowerThisTurn)
        {
            d.hasHealthedTowerThisTurn = true;
            HealthTowerManager.instance.StartRecolectedHealth(d);
            yield return new WaitUntil(() => !HealthTowerManager.instance.onColectedHealth);
        }
        if (TowerManager.instance.TryGetTowerAtNode(u.currentNode, out Tower tower)&& !u.hasAttackedTowerThisTurn&& TowerManager.instance.CanUnitAttackTower(u, tower))
        {
            u.hasAttackedTowerThisTurn = true;
            yield return StartCoroutine(CombatManager.instance.StartCombatWithTowerAI_Coroutine(u, tower));
        }
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