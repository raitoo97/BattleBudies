using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class IAMoveToResources : MonoBehaviour
{
    public static IAMoveToResources instance;
    [HideInInspector]public bool movedAnyUnit = false;
    private void Awake()
    {
        if (instance == null)
            instance = this;
        else
            Destroy(gameObject);
    }
    public IEnumerator MoveAllEnemyUnitsToResorces()
    {
        movedAnyUnit = false;
        List<Units> enemyUnits = GetAllEnemyUnits();
        List<Node> resourceNodes = GetResourcesNode(); // todos los nodos de recurso
        List<Node> validNodes = GetValidNodes(); // nodos de recurso libres
        foreach (Units enemy in enemyUnits)
        {
            if (enemy.currentNode == null || EnergyManager.instance.enemyCurrentEnergy < 1f) continue;
            if (resourceNodes.Contains(enemy.currentNode))
            {
                print("Ya esta en un nodo de recursos no mover");
                continue;
            }
            if (validNodes.Count == 0) continue;
            List<Node> path = GetPathToRandomNode(enemy, validNodes);
            if (path == null || path.Count == 0) continue;
            path = TrimPathBeforeDanger(path);
            path = LimitPathByEnergy(path);
            enemy.SetPath(path);
            movedAnyUnit = true;
            yield return new WaitUntil(() => enemy.PathEmpty());
            UpdateEnemyFinalNode(enemy, path);
            if (TryGetPlayerNeighbor(enemy, out Units playerUnit))
                yield return StartCoroutine(StartCombatAfterMove(enemy, playerUnit));
            yield return new WaitForSeconds(0.2f);
        }
    }
    private List<Units> GetAllEnemyUnits()
    {
        Units[] allUnits = FindObjectsOfType<Units>();
        List<Units> enemies = new List<Units>();
        foreach (Units u in allUnits)
            if (!u.isPlayerUnit) enemies.Add(u);
        return enemies;
    }
    private List<Node> GetPathToRandomNode(Units enemy, List<Node> validNodes)
    {
        List<Node> path = null;
        int attempts = 0;
        int maxAttempts = 20;
        while (attempts < maxAttempts)
        {
            attempts++;
            Node targetNode = validNodes[Random.Range(0, validNodes.Count)];
            if (targetNode == null) break;
            path = PathFinding.CalculateAstart(enemy.currentNode, targetNode);
            if (path == null || path.Count == 0) continue;
            bool pathBlocked = path.Exists(n => n.unitOnNode != null);
            if (!pathBlocked) break;
        }
        return path;
    }
    private List<Node> TrimPathBeforeDanger(List<Node> path)
    {
        if (NodeManager.PathTouchesUnitNeighbor(path, out List<Node> dangerNodes))
        {
            Node firstDangerNode = null;
            foreach (Node node in path)
            {
                if (dangerNodes.Contains(node))
                {
                    firstDangerNode = node;
                    break;
                }
            }
            if (firstDangerNode != null)
            {
                int index = path.IndexOf(firstDangerNode);
                path = path.GetRange(0, index + 1);
            }
        }
        return path;
    }
    private List<Node> LimitPathByEnergy(List<Node> path)
    {
        int maxSteps = Mathf.FloorToInt(EnergyManager.instance.enemyCurrentEnergy);
        if (path.Count > maxSteps)
            path = path.GetRange(0, maxSteps);
        return path;
    }
    private void UpdateEnemyFinalNode(Units enemy, List<Node> path)
    {
        if (path.Count > 0)
        {
            Node finalNode = path[path.Count - 1];
            enemy.SetCurrentNode(finalNode);
        }
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
}