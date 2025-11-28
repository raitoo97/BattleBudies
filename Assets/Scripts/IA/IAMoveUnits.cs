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
        {
            if (!u.isPlayerUnit) enemyUnits.Add(u);
        }
        foreach (Units enemy in enemyUnits)
        {
            if (enemy.currentNode == null) continue;
            if (EnergyManager.instance.enemyCurrentEnergy < 1f) continue;
            List<Node> emptyNodes = NodeManager.GetNodeCount().FindAll(n => n.IsEmpty());
            if (emptyNodes.Count == 0) continue;
            Node targetNode = null;
            List<Node> path = null;
            int maxAttempts = 20;
            int attempts = 0;
            while (attempts < maxAttempts)
            {
                attempts++;
                targetNode = emptyNodes[Random.Range(0, emptyNodes.Count)];
                if (targetNode == null) break;
                path = PathFinding.CalculateAstart(enemy.currentNode, targetNode);
                bool pathBlocked = path.Exists(n => n.unitOnNode != null);
                if (!pathBlocked && path.Count > 0)
                    break;
            }
            if (path == null || path.Count == 0) continue;
            int maxSteps = Mathf.FloorToInt(EnergyManager.instance.enemyCurrentEnergy);
            if (path.Count > maxSteps)
                path = path.GetRange(0, maxSteps);
            enemy.SetPath(path);
            movedAnyUnit = true;
            yield return new WaitUntil(() => enemy.PathEmpty());
            yield return new WaitForSeconds(0.3f);
        }
    }
}
