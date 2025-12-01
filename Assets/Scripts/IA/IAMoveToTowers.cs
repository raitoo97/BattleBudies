using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class IAMoveToTowers : MonoBehaviour
{
    public static IAMoveToTowers instance;
    [HideInInspector] public bool movedAnyUnit = false;
    private Dictionary<Units, Node> unitReservedNodes = new Dictionary<Units, Node>();
    private void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);
    }
    public IEnumerator MoveAllEnemyUnitsToTowers()
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
            Tower targetTower = null;
            Node targetNode = null;
            Tower[] allTowers = FindObjectsOfType<Tower>();
            List<Tower> candidateTowers = new List<Tower>();
            foreach (Tower t in allTowers)
                if (t.faction == Faction.Player && !t.isDestroyed)
                    candidateTowers.Add(t);
            candidateTowers.Sort((a, b) => a.currentHealth.CompareTo(b.currentHealth));
            foreach (Tower t in candidateTowers)
            {
                foreach (var nodeKey in TowerManager.instance.GetAttackNodes(t))
                {
                    string[] parts = nodeKey.Split('_');
                    if (parts.Length != 3) continue;
                    if (!int.TryParse(parts[1], out int x)) continue;
                    if (!int.TryParse(parts[2], out int y)) continue;

                    Node node = NodeManager.GetAllNodes().Find(n => n.gridIndex.x == x && n.gridIndex.y == y);
                    if (node != null && !IsNodeReserved(node) && node.unitOnNode == null)
                    {
                        targetTower = t;
                        targetNode = node;
                        break;
                    }
                }
                if (targetTower != null) break;
            }
            if (targetTower == null || targetNode == null) continue;
            List<Node> path = PathFinding.CalculateAstart(enemy.currentNode, targetNode);
            if (path == null || path.Count == 0) continue;
            int maxSteps = Mathf.FloorToInt(EnergyManager.instance.enemyCurrentEnergy);
            if (path.Count > maxSteps)
                path = path.GetRange(0, maxSteps);
            movedAnyUnit = true;
            foreach (Node step in path)
            {
                ReleaseReservedNode(enemy);
                enemy.SetPath(new List<Node> { step });
                yield return new WaitUntil(() => enemy.PathEmpty());
                enemy.SetCurrentNode(step);
                if (step == targetNode)
                    ReserveNode(enemy, step);
                if (TryGetPlayerNeighbor(enemy, out Units playerUnit))
                    yield return StartCoroutine(CombatManager.instance.StartCombatWithUnit_Coroutine(enemy, playerUnit));
            }
            if (TowerManager.instance.CanUnitAttackTower(enemy, targetTower))
            {
                yield return StartCoroutine(CombatManager.instance.StartCombatWithTowerAI_Coroutine(enemy, targetTower));
            }
            yield return new WaitForSeconds(0.2f);
        }
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
    private void ReserveNode(Units unit, Node node)
    {
        unitReservedNodes[unit] = node;
    }
    private void ReleaseReservedNode(Units unit)
    {
        if (unitReservedNodes.ContainsKey(unit))
            unitReservedNodes.Remove(unit);
    }
    public bool IsNodeReserved(Node node)
    {
        return unitReservedNodes.ContainsValue(node);
    }
    public void ReleaseNodeOnDeath(Units unit)
    {
        ReleaseReservedNode(unit);
    }
}