using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class IADefendTowers : MonoBehaviour
{
    public static IADefendTowers instance;
    [HideInInspector]public bool movedAnyUnit = false;
    public Transform ReferenceTower;
    public float maxDistanceFromReference = 10f;
    private bool actionInProgress = false;
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
        if (enemy is Defenders def &&enemy.currentNode != null &&NodeManager.GetHealthNodes().Contains(enemy.currentNode))
        {
            def.isLockedOnSpecialNode = true;
            actionInProgress = false;
            yield break;
        }
        yield return new WaitUntil(() => isBusy());
        movedAnyUnit = false;
        actionInProgress = true;
        if (enemy.currentNode == null)
        {
            actionInProgress = false;
            yield break;
        }
        List<Node> resourceNodes = GetHealtNodes();
        if (resourceNodes.Contains(enemy.currentNode) && enemy is Defenders defender && !defender.hasHealthedTowerThisTurn)
        {
            Debug.Log($"IA: Unidad enemiga es un Defensor va a tirar de nombre {enemy.gameObject.name}.");
            defender.hasHealthedTowerThisTurn = true;
            HealthTowerManager.instance.StartRecolectedHealth(enemy as Defenders);
            yield return new WaitUntil(() => !HealthTowerManager.instance.onColectedHealth);
            actionInProgress = false;
            yield break;
        }
        if (EnergyManager.instance.enemyCurrentEnergy < 1f)
        {
            actionInProgress = false;
            yield break;
        }
        List<Node> validNodes = GetValidNodes();
        bool foundPath = GetClosestFreeHealthNode(enemy, validNodes, out Node closestNode, out List<Node> path);
        if (!foundPath)
        {
            MoveToRandomNode(enemy, ref closestNode, ref path, ref foundPath);
            if (!foundPath) // Si todavía no encontró path, salimos
            {
                actionInProgress = false;
                yield break;
            }
            // Si encontró path, continuamos con el movimiento
        }
        int pathOffset = (path.Count > 0 && path[0] == enemy.currentNode) ? 1 : 0;
        int stepsToMove = Mathf.Min(maxEnergy, path.Count - pathOffset);
        if (stepsToMove <= 0)
        {
            Debug.Log($"Defender {enemy.gameObject.name} no puede moverse con la energía restante.");
            actionInProgress = false;
            yield break;
        }
        List<Node> nodesToMove = path.GetRange(pathOffset, stepsToMove);
        yield return StartCoroutine(ExecuteMovementPathWithSavingThrows(enemy, nodesToMove));
        movedAnyUnit = true;
        if (TryGetPlayerNeighbor(enemy, out Units playerUnit))
            yield return StartCoroutine(StartCombatAfterMove(enemy, playerUnit));
        yield return new WaitForSeconds(0.2f);
        actionInProgress = false;
    }
    private void MoveToRandomNode(Units enemy, ref Node closestNode, ref List<Node> path, ref bool foundPath)
    {
        if (ReferenceTower == null) return;
        List<Node> allFreeNodes = GetAllNodes();
        if(allFreeNodes.Count == 0) return;
        List<Node> filteredNodes = new List<Node>();
        foreach (Node node in allFreeNodes)
        {
            float distanceToReference = Vector3.Distance(node.transform.position, ReferenceTower.position);
            if (distanceToReference <= maxDistanceFromReference)
            {
                filteredNodes.Add(node);
            }
        }
        if (filteredNodes.Count == 0) return;
        Node randomNode = filteredNodes[Random.Range(0, filteredNodes.Count)];
        List<Node> randomPath = PathFinding.CalculateAstart(enemy.currentNode, randomNode);
        if (randomPath != null && randomPath.Count > 0)
        {
            closestNode = randomNode;
            path = randomPath;
            foundPath = true;
        }
    }
    private bool GetClosestFreeHealthNode(Units enemy, List<Node> validNodes, out Node closestNode, out List<Node> pathToNode)
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
    private List<Node> GetHealtNodes()
    {
        return NodeManager.GetHealthNodes();
    }
    private List<Node> GetValidNodes()
    {
        return GetHealtNodes().FindAll(n => n.IsEmpty());
    }
    private List<Node> GetAllNodes()
    {
        var nodes = NodeManager.GetAllNodes();
        return nodes.FindAll(n => n.IsEmpty());

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
    private void OnDrawGizmos()
    {
        if (ReferenceTower == null) return;
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(ReferenceTower.position, maxDistanceFromReference);
    }
    private bool isBusy()
    {
        return !actionInProgress
        && !SalvationManager.instance.GetOnSavingThrow
        && !HealthTowerManager.instance.onColectedHealth
        && !CombatManager.instance.GetCombatActive
        && !Units.anyUnitMoving;
    }
}
