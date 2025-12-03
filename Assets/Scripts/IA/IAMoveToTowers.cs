using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class IAMoveToTowers : MonoBehaviour
{
    //hace q si el nodo seguro esta ocupado, el enemigo recalcule el camino a otra luga
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
            bool unitDidAction = false;
            if (enemy == null || enemy.currentNode == null || EnergyManager.instance.enemyCurrentEnergy < 1f) continue;
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
            if (targetTower == null || targetNode == null)continue;
            Node previousStep = enemy.currentNode;
            List<Node> path = PathFinding.CalculateAstart(enemy.currentNode, targetNode);
            int maxSteps = Mathf.FloorToInt(EnergyManager.instance.enemyCurrentEnergy);
            if (path.Count > maxSteps)
                path = path.GetRange(0, maxSteps);
            if (path.Count > 0)
                unitDidAction = true;
            for (int i = 0; i < path.Count; i++)
            {
                Node step = path[i];
                if (enemy == null) break;
                ReleaseReservedNode(enemy);
                enemy.SetPath(new List<Node> { step });
                yield return new WaitUntil(() => enemy != null && enemy.PathEmpty());
                if (enemy == null) break;
                enemy.SetCurrentNode(step);
                Units playerUnit = null;
                if (step.IsDangerous)
                {
                    if (previousStep != null)
                        enemy.lastSafeNode = previousStep;
                    SalvationManager.instance.StartSavingThrow(enemy);
                    yield return new WaitUntil(() => enemy == null || !SalvationManager.instance.GetOnSavingThrow);
                    yield return new WaitForSeconds(1f);
                    if (enemy == null) break;
                    Node startNode = (enemy.currentNode != previousStep) ? enemy.currentNode : enemy.lastSafeNode;
                    path = PathFinding.CalculateAstart(startNode, targetNode);
                    maxSteps = Mathf.FloorToInt(EnergyManager.instance.enemyCurrentEnergy);
                    if (path.Count > maxSteps)
                        path = path.GetRange(0, maxSteps);
                    i = (path.Count > 0 && path[0] == enemy.currentNode) ? 0 : -1;
                    if (TryGetPlayerNeighbor(enemy, out playerUnit))
                    {
                        if (enemy == null) break;
                        yield return StartCoroutine(CombatManager.instance.StartCombatWithUnit_Coroutine(enemy, playerUnit));
                        if (enemy == null) break;
                    }
                    continue;
                }
                else
                {
                    enemy.lastSafeNode = step;
                }
                previousStep = step;
                if (step == targetNode)
                    ReserveNode(enemy, step);
                if (TryGetPlayerNeighbor(enemy,out playerUnit))
                {
                    if (enemy == null) break;
                    yield return StartCoroutine(CombatManager.instance.StartCombatWithUnit_Coroutine(enemy, playerUnit));
                    if (enemy == null) break;
                }
            }
            if (enemy != null && TowerManager.instance.CanUnitAttackTower(enemy, targetTower))
            {
                unitDidAction = true;
                yield return StartCoroutine(CombatManager.instance.StartCombatWithTowerAI_Coroutine(enemy, targetTower));
            }
            if (enemy != null)
            {
                enemy.hasAttackedTowerThisTurn = true;
                if (unitDidAction)
                    movedAnyUnit = true;
            }
        }
        unitReservedNodes.Clear();
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