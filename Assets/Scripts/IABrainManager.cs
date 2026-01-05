using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class IABrainManager : MonoBehaviour
{
    public static IABrainManager instance;
    private float chanceToPlayCards = 0.85f;
    private int maxStepsPerUnit = 3;
    [SerializeField]private float defendTriggerDistance;
    [SerializeField]private float attackTriggerDistance;
    private bool reactedToSpecialNodeThisTurn = false;
    private int maxEnemyUnits = 5;
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
            Debug.Log("IA no tiene unidades: solo invoca cartas, no chequea amenazas");
            yield return new WaitForSeconds(1f);
            if (CanInvokeMoreUnits())
            {
                yield return StartCoroutine(IAPlayCards.instance.PlayCards());
            }
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
        yield return new WaitForSeconds(1.5f);
        yield return StartCoroutine(IACheckEnemies());
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
        BellEndTurn.instance.RingFromIA();
    }
    IEnumerator InitializedTurn()
    {
        ClearPendingTargetsIfPlayerLeftSpecialNode();
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
        reactedToSpecialNodeThisTurn = false;
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
        Units rangerOnResource = null;
        Units defenderOnHealth = null;
        bool IaPossesRanger = false;
        foreach (Units u in FindObjectsOfType<Units>())
        {
            if (u == null || !u.isPlayerUnit || u.currentNode == null) continue;
            if (u is Ranger && NodeManager.GetResourcesNode().Contains(u.currentNode))
                rangerOnResource = u;
            else if (u is Defenders && NodeManager.GetHealthNodes().Contains(u.currentNode))
                defenderOnHealth = u;
        }
        foreach (Units u in FindObjectsOfType<Units>())
        {
            if (u != null && !u.isPlayerUnit && u is Ranger)
            {
                IaPossesRanger = true;
                break;
            }
        }
        if (rangerOnResource != null)
        {
            if (IAPlayCards.instance.CanPlayRanger() || IaPossesRanger)
            {
                Debug.Log("IA: Ranger del jugador en nodo de recursos objetivo detectado");
                target = rangerOnResource;
                return true;
            }
        }
        if (defenderOnHealth != null)
        {
            Debug.Log("IA: Defender del jugador en nodo de vida objetivo detectado");
            target = defenderOnHealth;
            return true;
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
        List<Node> resourceNodes = NodeManager.GetResourcesNode();
        List<Node> healthNodes = NodeManager.GetHealthNodes();
        bool targetOnResourceNode = target.currentNode != null && resourceNodes.Contains(target.currentNode);
        bool targetOnHealthNode = target.currentNode != null && healthNodes.Contains(target.currentNode);
        // 1. Buscar si ya hay alguna unidad enemiga con isPendingTarget = true
        Units unitToMove = null;
        Units[] enemyUnits = FindObjectsOfType<Units>();
        foreach (Units u in enemyUnits)
        {
            if (u != null && !u.isPlayerUnit && u.isPendingTarget)
            {
                if (u is Attackers && targetOnResourceNode)
                {
                    u.isPendingTarget = false;
                    continue;
                }
                unitToMove = u;
                break;
            }
        }
        // 2. Si no hay, elegir la unidad enemiga más cercana al target
        if (unitToMove == null)
        {
            Units bestUnit = null;
            float minDist = float.MaxValue;
            if (targetOnResourceNode) // Nodo de recolección
            {
                //solo  Rangers
                foreach (Units u in enemyUnits)
                {
                    if (u == null || u.isPlayerUnit || u.currentNode == null) continue;
                    if (u is Attackers || u is Defenders) continue; // Solo Rangers
                    float dist = Vector3.Distance(u.transform.position, target.transform.position);
                    if (dist < minDist)
                    {
                        minDist = dist;
                        bestUnit = u;
                    }
                }
                //Si no hay ranger no manda a ninguna unidad
                if (bestUnit == null)
                {
                    Debug.Log("IA: No hay Rangers disponibles para enviar al nodo de recursos");
                }
            }
            else if(targetOnHealthNode)
            {
                // Prioridad 1: Attackers
                foreach (Units u in enemyUnits)
                {
                    if (u == null || u.isPlayerUnit || u.currentNode == null) continue;
                    if (u is Ranger || u is Defenders) continue; // Solo Attackers
                    float dist = Vector3.Distance(u.transform.position, target.transform.position);
                    if (dist < minDist)
                    {
                        minDist = dist;
                        bestUnit = u;
                    }
                }
                // Prioridad 2: Defenders si no hay Attackers
                if (bestUnit == null)
                {
                    minDist = float.MaxValue;
                    foreach (Units u in enemyUnits)
                    {
                        if (u == null || u.isPlayerUnit || u.currentNode == null) continue;
                        if (u is Ranger || u is Attackers) continue; // Solo Defenders
                        float dist = Vector3.Distance(u.transform.position, target.transform.position);
                        if (dist < minDist)
                        {
                            minDist = dist;
                            bestUnit = u;
                        }
                    }
                }
                // Prioridad 3: no mando nada si no hay Attackers ni Defenders
                if (bestUnit == null)
                {
                    Debug.Log("IA: No hay Attackers o Defenders disponibles para enviar al nodo de salud");
                }
            }
            if (bestUnit != null)
            {
                unitToMove = bestUnit;
            }
        }
        // 3. Mover la unidad seleccionada al target
        if (unitToMove != null)
        {
            unitToMove.isLockedOnSpecialNode = false;
            unitToMove.isPendingTarget = true;
            yield return StartCoroutine(MoveUnitToTarget(unitToMove, target));
        }
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
        int pathOffset = (path.Count > 0 && path[0] == unit.currentNode) ? 1 : 0;
        int stepsToMove = Mathf.Min(energyForThisUnit, path.Count - pathOffset);
        if (stepsToMove <= 0)
        {
            Debug.LogWarning($"MoveUnitToTarget: {unit.name} no puede moverse con la energía restante");
            yield break;
        }

        List<Node> nodesToMove = path.GetRange(pathOffset, stepsToMove);
        Debug.Log($"IA: Moviendo {unit.name} hacia {target.name} con {stepsToMove} pasos");
        yield return StartCoroutine(IAMoveToTowers.instance.ExecuteMovementPathWithSavingThrows(unit, nodesToMove));
        if (target == null || target.currentNode == null)
        {
            unit.isPendingTarget = false;
            yield break;
        }
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
                if (u is Ranger) continue;
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
            unitToMove.isLockedOnSpecialNode = false;
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
        int pathOffset = (path.Count > 0 && path[0] == unit.currentNode) ? 1 : 0;
        int stepsToMove = Mathf.Min(EnergyManager.instance.enemyCurrentEnergy,path.Count - pathOffset);
        if (stepsToMove <= 0)
        {
            Debug.LogWarning($"MoveUnitToThreat: {unit.name} no puede moverse con la energía restante");
            yield break;
        }
        List<Node> nodesToMove = path.GetRange(pathOffset, stepsToMove);
        Debug.Log($"IA: Moviendo {unit.name} hacia amenaza {threat.name} ({stepsToMove} pasos)");
        yield return StartCoroutine(IAMoveToTowers.instance.ExecuteMovementPathWithSavingThrows(unit, nodesToMove));
    }
    private IEnumerator IACheckEnemies()
    {
        if (reactedToSpecialNodeThisTurn)yield break;
        Units threat;
        if (IsPlayerThreateningTower(out threat) && EnergyManager.instance.enemyCurrentEnergy > 0)
        {
            reactedToSpecialNodeThisTurn = true;
            if (EnergyManager.instance.enemyCurrentEnergy > 0)
            {
                yield return StartCoroutine(IAPlayCards.instance.PlayOneCard_NearThreat(threat));
            }
            yield return StartCoroutine(MoveAllUnitsToThreat(threat));
            yield break;
        }

        Units specialTarget;
        if (IsPlayerUsingSpecialNode(out specialTarget) && EnergyManager.instance.enemyCurrentEnergy > 0)
        {
            reactedToSpecialNodeThisTurn = true;
            int playerRangers = GetPlayerRangerCount();
            int enemyRangers = GetEnemyRangerCount();
            int toInvoke = playerRangers - enemyRangers;
            if (toInvoke > 0)
            {
                yield return StartCoroutine(InvokeRangersAsPossible(toInvoke, specialTarget));
            }
            yield return StartCoroutine(SendUnitToKillTarget(specialTarget));
            yield break;
        }
    }
    private IEnumerator HandleUnitsMoves(List<Attackers> attackers,List<Defenders> defenders,List<Ranger> rangers,int totalUnits)
    {
        float globalChance = Random.value;
        if (globalChance < 0.05f)
        {
            Debug.Log("Ataque Global de parte de la IA");
            yield return StartCoroutine(IAMoveToTowers.instance.MoveAllEnemyUnitsToTowers(attackers));
            yield break;
        }
        //MOVIMIENTOS INDIVIDUALES NORMALES
        float random = Random.value;
        if (random < chanceToPlayCards && EnergyManager.instance.enemyCurrentEnergy >= 1 && CanInvokeMoreUnits())
        {
            Debug.Log("IA decide jugar carta iniciar movimiento fichas");
            yield return StartCoroutine(IAPlayCards.instance?.PlayOneCard());
        }
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
        if (random < chanceToPlayCards && EnergyManager.instance.enemyCurrentEnergy >= 1 && CanInvokeMoreUnits())
        {
            Debug.Log("IA decide jugar carta tras mover Attackers");
            yield return StartCoroutine(IAPlayCards.instance?.PlayOneCard());
        }
    }
    private IEnumerator MoveDefenders(List<Defenders> defenders, List<Ranger> rangers)
    {
        if (defenders == null || defenders.Count == 0) yield break;
        bool anyDamagedTower = TowerManager.instance.enemyTowers.Exists(t => t.currentHealth < t.maxHealth);
        int remainingUnits = defenders.Count + rangers.Count;
        int currentEnergy = EnergyManager.instance.enemyCurrentEnergy;
        int energyPerUnit = Mathf.Max(1, Mathf.Min((remainingUnits > 0 ? currentEnergy / remainingUnits : 1), maxStepsPerUnit));
        foreach (Units u in defenders)
        {
            if (u == null || !u) continue;
            if (u.isPendingTarget) continue;
            if (u.isLockedOnSpecialNode) continue;
            // Si no hay energía no intento mover
            if (EnergyManager.instance.enemyCurrentEnergy < 1) break;
            Debug.Log($"IA moviendo Defenders {u.gameObject.name}");
            int moveEnergy = Mathf.Min(energyPerUnit, EnergyManager.instance.enemyCurrentEnergy);
            if (anyDamagedTower)
            {
                Debug.Log("IA: Hay torres dañadas, moviendo Defender para curar");
                // Mover normal para curar torres
                yield return StartCoroutine(IADefendTowers.instance.MoveSingleUnit(u, moveEnergy));
                yield return StartCoroutine(HandleSingleUnitOnSpecialNode(u));
            }
            else
            {
                Debug.Log("IA: NO Hay torres dañadas, moviendo Defender para atacar");
                // No hay torres para curar  mover como attacker
                yield return StartCoroutine(IAMoveToTowers.instance.MoveSingleUnit(u, moveEnergy));
            }
            yield return new WaitForSeconds(0.2f);
        }
        float random = Random.value;
        if (random < chanceToPlayCards && EnergyManager.instance.enemyCurrentEnergy >= 1 && CanInvokeMoreUnits())
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
            if (u.isLockedOnSpecialNode) continue;
            if (EnergyManager.instance.enemyCurrentEnergy < 1) break;
            Debug.Log($"IA moviendo Rangers {u.gameObject.name}");
            int moveEnergy = Mathf.Min(energyPerUnit, EnergyManager.instance.enemyCurrentEnergy);
            yield return StartCoroutine(IAMoveToResources.instance.MoveSingleUnit(u, moveEnergy));
            yield return StartCoroutine(HandleSingleUnitOnSpecialNode(u));
            yield return new WaitForSeconds(0.2f);
        }
        UpgradeManager.instance.UpgradeEnemyUnits();
        float random = Random.value;
        if (random < chanceToPlayCards && EnergyManager.instance.enemyCurrentEnergy >= 1 && CanInvokeMoreUnits())
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
        bool reactedToThreat = false;
        bool reactedToSpecialNode = false;
        do
        {
            anyMoved = false;
            safetyCounter++;
            if (safetyCounter > 50) break;
            Units threat;
            if (!reactedToThreat && EnergyManager.instance.enemyCurrentEnergy > 0 && IsPlayerThreateningTower(out threat))
            {
                yield return StartCoroutine(MoveAllUnitsToThreat(threat));
                reactedToThreat = true;
                anyMoved = true;
                continue;
            }
            Units specialTarget;
            if (!reactedToSpecialNode && EnergyManager.instance.enemyCurrentEnergy > 0 && IsPlayerUsingSpecialNode(out specialTarget))
            {
                yield return StartCoroutine(SendUnitToKillTarget(specialTarget));
                reactedToSpecialNode = true;
                anyMoved = true;
                continue;
            }
            foreach (Units u in allUnits)
            {
                if (u == null || !u) continue;
                if (u.isLockedOnSpecialNode)continue;
                int energyAvailable = EnergyManager.instance.enemyCurrentEnergy;
                if (energyAvailable <= 0) break;
                if (u is Attackers)
                {
                    yield return StartCoroutine(IAMoveToTowers.instance.MoveSingleUnit(u as Attackers, energyAvailable));
                    anyMoved = true;
                }
                else if (u is Defenders)
                {
                    bool anyDamagedTower = TowerManager.instance.enemyTowers.Exists(t => t.currentHealth < t.maxHealth);
                    if (anyDamagedTower)
                    {
                        Debug.Log("IA: Hay torres dañadas, moviendo Defender para curar");
                        // Mover normal para curar torres
                        yield return StartCoroutine(IADefendTowers.instance.MoveSingleUnit(u as Defenders, energyAvailable));
                        yield return new WaitUntil(() => !HealthTowerManager.instance.onColectedHealth);
                    }
                    else
                    {
                        Debug.Log("IA: NO Hay torres dañadas, moviendo Defender para atacar");
                        // No hay torres para curar  mover como attacker
                        yield return StartCoroutine(IAMoveToTowers.instance.MoveSingleUnit(u as Defenders, energyAvailable));
                    }
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
            if (!anyMoved && EnergyManager.instance.enemyCurrentEnergy > 0 &&CanInvokeMoreUnits())
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
                if (u.isLockedOnSpecialNode) continue;
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
        float random = Random.value;
        if (random < chanceToPlayCards && EnergyManager.instance.enemyCurrentEnergy >= 1 && CanInvokeMoreUnits())
        {
            Debug.Log("IA decide jugar carta tras mover Attackers");
            yield return StartCoroutine(IAPlayCards.instance?.PlayOneCard());
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
            r.isLockedOnSpecialNode = true;
            if (!r.hasCollectedThisTurn)
            {
                r.hasCollectedThisTurn = true;
                ResourcesManager.instance.StartRecolectedResources(r);
                yield return new WaitUntil(() => !ResourcesManager.instance.onColectedResources);
                UpgradeManager.instance.UpgradeEnemyUnits();
            }
        }
        if (u is Defenders d&& NodeManager.GetHealthNodes().Contains(u.currentNode)&& !d.hasHealthedTowerThisTurn)
        {
            d.isLockedOnSpecialNode = true;
            if (!d.hasHealthedTowerThisTurn)
            {
                d.hasHealthedTowerThisTurn = true;
                HealthTowerManager.instance.StartRecolectedHealth(d);
                yield return new WaitUntil(() => !HealthTowerManager.instance.onColectedHealth);
            }
        }
        if (TowerManager.instance.TryGetTowerAtNode(u.currentNode, out Tower tower)&& !u.hasAttackedTowerThisTurn&& TowerManager.instance.CanUnitAttackTower(u, tower))
        {
            u.hasAttackedTowerThisTurn = true;
            yield return StartCoroutine(CombatManager.instance.StartCombatWithTowerAI_Coroutine(u, tower));
        }
    }
    private void ClearPendingTargetsIfPlayerLeftSpecialNode()
    {
        bool playerStillOnSpecialNode = false;
        foreach (Units u in FindObjectsOfType<Units>())
        {
            if (u == null || !u.isPlayerUnit || u.currentNode == null) continue;
            // Check PASIVO: solo detectar, no reaccionar
            if ((u is Ranger && NodeManager.GetResourcesNode().Contains(u.currentNode)) ||(u is Defenders && NodeManager.GetHealthNodes().Contains(u.currentNode)))
            {
                playerStillOnSpecialNode = true;
                break;
            }
        }
        // Si el player sigue en un nodo especial, no limpiamos nada
        if (playerStillOnSpecialNode) return;
        // Limpieza segura de targets pendientes
        foreach (Units u in FindObjectsOfType<Units>())
        {
            if (u == null || u.isPlayerUnit) continue;

            if (u.isPendingTarget || u.isLockedOnSpecialNode)
            {
                u.isPendingTarget = false;
                u.isLockedOnSpecialNode = false;
            }
        }
        Debug.Log("IA: Inicio de turno — player NO está en nodo especial, se limpian pendingTarget");
    }
    #region UnitsCountHelpers
    int GetPlayerRangerCount()
    {
        int count = 0;
        foreach (Units u in FindObjectsOfType<Units>())
        {
            if (u != null && u.isPlayerUnit && u is Ranger)
                count++;
        }
        return count;
    }
    int GetEnemyRangerCount()
    {
        int count = 0;
        foreach (Units u in FindObjectsOfType<Units>())
        {
            if (u != null && !u.isPlayerUnit && u is Ranger)
                count++;
        }
        return count;
    }
    private bool CanInvokeMoreUnits()
    {
        int enemyCount = 0;
        foreach (Units u in FindObjectsOfType<Units>())
        {
            if (u != null && !u.isPlayerUnit)
                enemyCount++;
        }
        int playerCount = 0;
        foreach (Units u in FindObjectsOfType<Units>())
        {
            if (u != null && u.isPlayerUnit)
                playerCount++;
        }
        int targetCount = Mathf.Max(maxEnemyUnits, playerCount);
        return enemyCount < targetCount;
    }
    private IEnumerator InvokeRangersAsPossible(int amount, Units rangerTarget)
    {
        int maxRangersAllowed = 2;
        for (int i = 0; i < amount; i++)
        {
            if (GetEnemyRangerCount() >= maxRangersAllowed)
            {
                Debug.Log("IA: ya hay 2 Rangers enemigos, no invoca más");
                yield break;
            }
            if (!IAPlayCards.instance.CanPlayRanger()) yield break;
            yield return StartCoroutine(IAPlayCards.instance.PlayOneCard_PrioritizeRanger(rangerTarget));
        }
    }
    #endregion
    public bool isBusy()
    {
        return !CombatManager.instance.GetCombatActive
               && !SalvationManager.instance.GetOnSavingThrow
               && !ResourcesManager.instance.onColectedResources
               && !HealthTowerManager.instance.onColectedHealth
               && !Units.anyUnitMoving;
    }
}