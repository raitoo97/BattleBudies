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
            int maxAttempts = 20;
            int attempts = 0;
            while (attempts < maxAttempts)
            {
                attempts++;
                targetNode = validNodes[Random.Range(0, validNodes.Count)];
                if (targetNode == null) break;
                path = PathFinding.CalculateAstart(enemy.currentNode, targetNode);
                bool pathBlocked = path.Exists(n => n.unitOnNode != null);
                if (!pathBlocked && path.Count > 0) break;
            }
            if (path == null || path.Count == 0) continue;
            if (NodeManager.PathTouchesUnitNeighbor(path, out List<Node> dangerNodes))
            {
                int index = path.FindIndex(n => dangerNodes.Contains(n));
                if (index >= 0)
                {
                    path = path.GetRange(0, index + 1);
                    Node combatNode = path[path.Count - 1];
                    Debug.Log($"IA: {enemy.name} se mueve hasta nodo {combatNode.name} (vecino al jugador)");
                    enemy.SetPath(path);
                    movedAnyUnit = true;
                    yield return new WaitUntil(() => enemy.PathEmpty());
                    foreach (Node danger in dangerNodes)
                    {
                        if (danger.unitOnNode != null)
                        {
                            Units playerUnit = danger.unitOnNode.GetComponent<Units>();
                            if (playerUnit != null && playerUnit.isPlayerUnit)
                            {
                                Debug.Log($"IA inicia combate: {enemy.name} vs {playerUnit.name}");
                                CombatManager.instance.StartCombat(enemy, playerUnit);
                                yield break;
                            }
                        }
                    }
                }
            }
            else
            {
                int maxSteps = Mathf.FloorToInt(EnergyManager.instance.enemyCurrentEnergy);
                if (path.Count > maxSteps)
                    path = path.GetRange(0, maxSteps);
                enemy.SetPath(path);
                movedAnyUnit = true;
                yield return new WaitUntil(() => enemy.PathEmpty());
                Node finalNode = path[path.Count - 1];
                finalNode.unitOnNode = enemy.gameObject;
            }
            yield return new WaitForSeconds(0.3f);
        }
    }
}

