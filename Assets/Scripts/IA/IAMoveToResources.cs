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
        List<Node> resourceNodes = GetResourcesNode(); 
        List<Node> validNodes = GetValidNodes();
        foreach (Units enemy in enemyUnits)
        {
            if (enemy == null || enemy.currentNode == null || EnergyManager.instance.enemyCurrentEnergy < 1f)
                continue;
            if (resourceNodes.Contains(enemy.currentNode))
            {
                print("Ya esta en un nodo de recursos");
                continue;
            }
            if (validNodes.Count == 0) continue;
            List<Node> path = GetPathToRandomNode(enemy, validNodes);
            if (path == null || path.Count == 0) continue;
            int maxSteps = Mathf.FloorToInt(EnergyManager.instance.enemyCurrentEnergy);
            if (path.Count > maxSteps)
                path = path.GetRange(0, maxSteps);
            yield return StartCoroutine(ExecuteMovementPathWithSavingThrows(enemy, path));
            movedAnyUnit = true;
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
        }
    }
}