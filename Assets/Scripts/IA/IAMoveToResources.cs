using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class IAMoveToResources : MonoBehaviour
{
    public static IAMoveToResources instance;
    [HideInInspector] public bool movedAnyUnit = false;
    private bool actionInProgress = false;
    public Transform ReferencePoint;
    public float maxDistanceFromReference = 10f;
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
        if (enemy.isLockedOnSpecialNode)
        {
            actionInProgress = false;
            yield break;
        }
        if (enemy is Ranger r &&enemy.currentNode != null &&NodeManager.GetResourcesNode().Contains(enemy.currentNode))
        {
            r.isLockedOnSpecialNode = true;
            actionInProgress = false;
            yield break;
        }
        yield return new WaitUntil(() => isBusy());
        movedAnyUnit = false;
        actionInProgress = true;
        if (enemy.currentNode == null || EnergyManager.instance.enemyCurrentEnergy < 1f)
        {
            actionInProgress = false;
            yield break;
        }
        yield return StartCoroutine(IABrainManager.instance.HandleSingleUnitOnSpecialNode(enemy));
        List<Node> validNodes = GetValidNodes();
        bool foundPath = GetClosestFreeResourceNode(enemy, validNodes, out Node closestNode, out List<Node> path);
        if (!foundPath)
        {
            if (!GetRandomNodeNearReference(enemy, out closestNode, out path))
            {
                actionInProgress = false;
                yield break;
            }
        }
        int pathOffset = (path.Count > 0 && path[0] == enemy.currentNode) ? 1 : 0;
        int stepsToMove = Mathf.Min(maxEnergy, path.Count - pathOffset);
        if (stepsToMove <= 0)
        {
            Debug.Log($"Ranger {enemy.gameObject.name} no puede moverse con la energía restante.");
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
    private bool GetClosestFreeResourceNode(Units enemy, List<Node> validNodes, out Node closestNode, out List<Node> pathToNode)
    {
        closestNode = null;
        pathToNode = null;
        if (validNodes.Count == 0 || enemy == null) return false;
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
        if (ReferencePoint == null) return false;
        List<Node> allValidNodes = GetAllValidNodes();
        if (allValidNodes.Count == 0) return false;
        List<Node> candidates = new List<Node>();
        foreach (Node n in allValidNodes)
        {
            float dist = Vector3.Distance(n.transform.position, ReferencePoint.position);
            if (dist <= maxDistanceFromReference)
                candidates.Add(n);
        }
        if (candidates.Count == 0)
            return false;
        targetNode = candidates[Random.Range(0, candidates.Count)];
        path = PathFinding.CalculateAstart(enemy.currentNode, targetNode);
        if (path == null || path.Count == 0) return false;
        return true;
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
    private List<Node> GetResourcesNode()
    {
        return NodeManager.GetResourcesNode();
    }
    private List<Node> GetValidNodes()
    {
        return GetResourcesNode().FindAll(n => n.unitOnNode == null);
    }
    private List<Node> GetAllValidNodes()
    {
        return NodeManager.GetAllNodes().FindAll(n => n.IsEmpty());
    }
    private IEnumerator ExecuteMovementPathWithSavingThrows(Units enemy, List<Node> path)
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
        && !ResourcesManager.instance.onColectedResources
        && !CombatManager.instance.GetCombatActive
        && !Units.anyUnitMoving;
    }
    private void OnDrawGizmos()
    {
        if (ReferencePoint == null) return;
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(ReferencePoint.position, maxDistanceFromReference);
    }
}