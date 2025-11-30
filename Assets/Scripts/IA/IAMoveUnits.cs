using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class IAMoveUnits : MonoBehaviour
{
    public static IAMoveUnits instance;
    [HideInInspector]public bool movedAnyUnit = false;
    private void Awake()
    {
        if (instance == null)
            instance = this;
        else
            Destroy(gameObject);
    }
    public IEnumerator MoveAllEnemyUnits()
    {
        movedAnyUnit = false;
        Units[] allUnits = FindObjectsOfType<Units>();
        List<Units> enemyUnits = new List<Units>();
        foreach (Units u in allUnits)
            if (!u.isPlayerUnit) enemyUnits.Add(u);
        foreach (Units enemy in enemyUnits)
        {
            if (enemy.currentNode == null) continue;
            if (EnergyManager.instance.enemyCurrentEnergy < 1f) continue;
            List<Node> validNodes = NodeManager.GetNodeCount().FindAll(n => n.unitOnNode == null);
            if (validNodes.Count == 0) continue;
            Node targetNode = null;
            List<Node> path = null;
            int attempts = 0;
            int maxAttempts = 20;
            while (attempts < maxAttempts)
            {
                attempts++;
                targetNode = validNodes[Random.Range(0, validNodes.Count)];
                if (targetNode == null) break;

                path = PathFinding.CalculateAstart(enemy.currentNode, targetNode);
                if (path == null || path.Count == 0) continue;

                bool pathBlocked = path.Exists(n => n.unitOnNode != null);
                if (!pathBlocked) break;
            }
            if (path == null || path.Count == 0) continue;
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
            int maxSteps = Mathf.FloorToInt(EnergyManager.instance.enemyCurrentEnergy);
            if (path.Count > maxSteps)
                path = path.GetRange(0, maxSteps);
            enemy.SetPath(path);
            movedAnyUnit = true;
            yield return new WaitUntil(() => enemy.PathEmpty());
            if (path.Count > 0)
            {
                Node finalNode = path[path.Count - 1];
                enemy.SetCurrentNode(finalNode);
            }
            if (TryGetPlayerNeighbor(enemy, out Units playerUnit))
            {
                yield return StartCoroutine(StartCombatAfterMove(enemy, playerUnit));
            }
            yield return new WaitForSeconds(0.2f);
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
}