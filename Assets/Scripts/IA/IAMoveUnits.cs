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
                    enemy.SetPath(path);
                    movedAnyUnit = true;
                    yield return new WaitUntil(() => enemy.PathEmpty());
                    Units playerUnit = firstDangerNode.unitOnNode?.GetComponent<Units>();
                    if (playerUnit != null && playerUnit.isPlayerUnit)
                    {
                        CombatManager.instance.StartCombat(enemy, playerUnit, true);
                    }
                    continue;
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
            yield return new WaitForSeconds(0.3f);
        }
    }
}

