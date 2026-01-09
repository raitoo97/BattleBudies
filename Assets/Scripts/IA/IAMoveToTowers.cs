using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class IAMoveToTowers : MonoBehaviour
{
    public static IAMoveToTowers instance;
    [HideInInspector] public bool movedAnyUnit = false;
    private bool actionInProgress = false;
    public Transform ReferencePoint;
    [SerializeField]private float maxDistanceFromReference;
    private void Awake()
    {
        if (instance == null)
            instance = this;
        else
            Destroy(gameObject);
    }
    public IEnumerator MoveSingleUnit(Units enemy, int maxEnergy)
    {
        if (enemy == null) yield break;
        yield return new WaitUntil(() => isBusy());
        movedAnyUnit = false;
        actionInProgress = true;
        if (enemy.currentNode == null || EnergyManager.instance.enemyCurrentEnergy < 1f)
        {
            actionInProgress = false;
            yield break;
        }
        if (NodeManager.GetAllPlayerTowersNodes().Contains(enemy.currentNode))
        {
            actionInProgress = false;
            yield break;
        }
        List<Node> validNodes = GetValidNodes();
        bool foundPath = GetClosestTowerPlayer(enemy, validNodes, out Node closestNode, out List<Node> path);
        if (!foundPath)
        {
            Debug.LogWarning($"IA: {enemy.gameObject.name} no encontró camino hacia torre, se moverá aleatoriamente.");
            GetRandomNodeNearReference(enemy, out closestNode, out path);
            if (path == null || path.Count == 0)
            {
                Debug.LogWarning($"IA: {enemy.gameObject.name} no encontró ningún nodo válido cerca del ReferencePoint. Se detiene.");
                actionInProgress = false;
                yield break; // No hay nodos válidos, terminar
            }
        }
        int pathOffset = (path.Count > 0 && path[0] == enemy.currentNode) ? 1 : 0;
        int stepsToMove = Mathf.Min(maxEnergy, path.Count - pathOffset);
        if (stepsToMove <= 0)
        {
            Debug.Log($"Atacker {enemy.gameObject.name} no puede moverse con la energía restante.");
            actionInProgress = false;
            yield break;
        }
        List<Node> nodesToMove = path.GetRange(pathOffset, stepsToMove);
        yield return StartCoroutine(ExecuteMovementPathWithSavingThrows(enemy, nodesToMove));
        movedAnyUnit = true;
        yield return StartCoroutine(IABrainManager.instance.HandleSingleUnitOnSpecialNode(enemy));
        if (TryGetPlayerNeighbor(enemy, out Units playerUnit))
            yield return StartCoroutine(StartCombatAfterMove(enemy, playerUnit));
        yield return new WaitForSeconds(0.2f);
        actionInProgress = false;
    }
    public IEnumerator MoveAllEnemyUnitsToTowers(List<Attackers> attackers)
    {
        movedAnyUnit = false;
        if (attackers == null || attackers.Count == 0)yield break;
        List<Node> validNodes = GetValidNodes();
        foreach (Units enemy in attackers)
        {
            yield return new WaitUntil(() => isBusy());
            actionInProgress = true;
            if (enemy == null || enemy.currentNode == null || EnergyManager.instance.enemyCurrentEnergy < 1f)
            {
                actionInProgress = false;
                continue;
            }
            List<Node> attackNodes = GetAttackNodes();
            if (attackNodes.Contains(enemy.currentNode))
            {
                Tower targetTower = GetClosestTowerToNode(enemy.currentNode);
                if (targetTower != null)
                {
                    Debug.Log($"IA: Unidad enemiga {enemy.gameObject.name} atacará torre {targetTower.name}.");
                    yield return StartCoroutine(CombatManager.instance.StartCombatWithTowerAI_Coroutine(enemy, targetTower));
                }
                actionInProgress = false;
                continue;
            }
            bool foundPath = GetClosestTowerPlayer(enemy, validNodes, out Node closestNode, out List<Node> path);
            if (!foundPath)
            {
                // Si no hay nodo alcanzable, moverse a un nodo random cerca del punto de referencia
                if (!GetRandomNodeNearReference(enemy, out closestNode, out path))
                {
                    actionInProgress = false;
                    continue;
                }
            }
            // Limitamos path por energía
            int maxSteps = Mathf.FloorToInt(EnergyManager.instance.enemyCurrentEnergy);
            if (path.Count > maxSteps)
                path = path.GetRange(0, maxSteps);
            // Movemos la unidad con tiradas de salvación
            yield return StartCoroutine(ExecuteMovementPathWithSavingThrows(enemy, path));
            movedAnyUnit = true;
            if (attackNodes.Contains(enemy.currentNode))
            {
                Tower targetTower = GetClosestTowerToNode(enemy.currentNode);
                if (targetTower != null)
                {
                    Debug.Log($"IA: Unidad enemiga {enemy.gameObject.name} atacará torre {targetTower.name} (después de moverse).");
                    yield return StartCoroutine(CombatManager.instance.StartCombatWithTowerAI_Coroutine(enemy, targetTower));
                }
                actionInProgress = false;
                continue;
            }
            // Ataque a unidad jugador si hay vecino
            if (TryGetPlayerNeighbor(enemy, out Units playerUnit))
                yield return StartCoroutine(StartCombatAfterMove(enemy, playerUnit));
            yield return new WaitForSeconds(0.2f);
            actionInProgress = false;
        }
    }
    private Tower GetClosestTowerToNode(Node node)
    {
        Tower closestTower = null;
        float minDistance = Mathf.Infinity;
        foreach (Tower t in TowerManager.instance.playerTowers)
        {
            if (t == null || t.isDestroyed) continue;

            float dist = Vector3.Distance(node.transform.position, t.transform.position);
            if (dist < minDistance)
            {
                minDistance = dist;
                closestTower = t;
            }
        }
        return closestTower;
    }
    private bool GetClosestTowerPlayer(Units enemy, List<Node> validNodes, out Node closestNode, out List<Node> pathToNode)
    {
        closestNode = null;
        pathToNode = null;
        if (enemy == null || validNodes.Count == 0) return false;
        validNodes.Sort((a, b) => Vector3.Distance(enemy.transform.position, a.transform.position).CompareTo(Vector3.Distance(enemy.transform.position, b.transform.position)));
        foreach (Node node in validNodes)
        {
            List<Node> path = PathFinding.CalculateAstart(enemy.currentNode, node);
            if (path != null && path.Count > 0)
            {
                closestNode = node;
                pathToNode = path;
                return true;
            }
        }
        return false;
    }
    private bool GetRandomNodeNearReference(Units enemy, out Node targetNode, out List<Node> path)
    {
        targetNode = null;
        path = null;
        if (ReferencePoint == null) 
        {
            Debug.Log($"IA: ReferencePoint es null.");
            return false;
        }
        if (enemy == null)
        {
            Debug.Log($"IA:  enemy es null.");
            return false;
        }
        List<Node> allValidNodes = GetAllValidNodes();
        if (allValidNodes.Count == 0) 
        {
            Debug.Log($"IA: No hay nodos válidos en el mapa.");
            return false;
        }
        List<Node> candidates = new List<Node>();
        foreach (Node n in allValidNodes)
        {
            float dist = Vector3.Distance(n.transform.position, ReferencePoint.position);
            if (dist <= maxDistanceFromReference)
            {
                candidates.Add(n);
            }
        }
        if (candidates.Count == 0)
        {
            Debug.Log($"IA: No hay nodos dentro de {maxDistanceFromReference} unidades del ReferencePoint");
            return false;
        }
        int attempts = Mathf.Min(10, candidates.Count); // máximo 10 intentos
        for (int i = 0; i < attempts; i++)
        {
            Node randomNode = candidates[Random.Range(0, candidates.Count)];
            List<Node> testPath = PathFinding.CalculateAstart(enemy.currentNode, randomNode);
            if (testPath != null && testPath.Count > 0)
            {
                targetNode = randomNode;
                Debug.Log($"IA: Nodo aleatorio accesible encontrado: {targetNode.gameObject.name}");
                path = testPath;
                return true;
            }
            else
            {
                candidates.Remove(randomNode); // no intentar otra vez con este nodo
            }
        }
        Debug.Log($"IA: No se encontró ningún nodo accesible cerca del ReferencePoint después de {attempts} intentos.");
        return false;
    }
    private IEnumerator StartCombatAfterMove(Units attacker, Units defender)
    {
        yield return new WaitUntil(() => attacker.PathEmpty());
        CombatManager.instance.StartCombat(attacker, defender, true);
        yield return new WaitUntil(() => !CombatManager.instance.GetCombatActive);
    }
    private bool TryGetPlayerNeighbor(Units enemy, out Units player)
    {
        player = null;
        if (enemy.currentNode == null) return false;
        foreach (Node neighbor in enemy.currentNode.Neighbors)
        {
            if (neighbor.unitOnNode != null)
            {
                Units unit = neighbor.unitOnNode.GetComponent<Units>();
                if (unit != null && unit.isPlayerUnit)
                {
                    player = unit;
                    return true;
                }
            }
        }
        return false;
    }
    private List<Node> GetAttackNodes()
    {
        return NodeManager.GetAllPlayerTowersNodes();
    }
    private List<Node> GetValidNodes()
    {
        return GetAttackNodes().FindAll(n => n.unitOnNode == null);
    }
    private List<Node> GetAllValidNodes()
    {
        return NodeManager.GetAllNodes().FindAll(n => n.IsEmpty());
    }
    public IEnumerator ExecuteMovementPathWithSavingThrows(Units enemy, List<Node> path)
    {
        foreach (Node step in path)
        {
            if (enemy == null) yield break;
            enemy.SetPath(new List<Node> { step });
            yield return new WaitUntil(() => enemy != null && enemy.PathEmpty());
            if (enemy == null) yield break;
            if (step.IsDangerous)
            {
                SalvationManager.instance.StartSavingThrow(enemy);
                yield return new WaitUntil(() => enemy == null || !SalvationManager.instance.GetOnSavingThrow);
                if (enemy == null || enemy.currentNode != step) yield break;
            }
            enemy.lastSafeNode = step;
            if (TryGetPlayerNeighbor(enemy, out Units playerUnit))
            {
                yield return StartCoroutine(StartCombatAfterMove(enemy, playerUnit));
                yield break; // Detener movimiento al pelear
            }
        }
    }
    private bool isBusy()
    {
        return !actionInProgress
        && !SalvationManager.instance.GetOnSavingThrow
        && !CombatManager.instance.GetCombatActive
        && !Units.anyUnitMoving;
    }
    private void OnDrawGizmos()
    {
        if (ReferencePoint == null) return;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(ReferencePoint.position, maxDistanceFromReference);//ERROR
    }
}