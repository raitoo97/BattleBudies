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
        List<Units> enemyUnits = GetEnemyUnits(allUnits); //obtiene solo unidades enemigas
        foreach (Units enemy in enemyUnits)
        {
            if (!CanEnemyAct(enemy)) continue; //valida energía y nodo
            bool unitDidAction = false;
            //SELECCIÓN DE TORRE Y NODO
            Tower targetTower;
            Node targetNode;
            if (!GetClosestValidTowerNode(out targetTower, out targetNode)) continue; //busca torre y nodo libre
            //CAMINO HACIA LA TORRE
            Node previousStep = enemy.currentNode;
            List<Node> path = PreparePath(enemy, targetNode, ref unitDidAction); //calcula path y limita pasos según energy
                                                                                 //MOVIMIENTO PASO A PASO
            yield return StartCoroutine(ExecuteMovementPath(enemy, path, previousStep)); //mueve unidad paso a paso
            //ATAQUE A TORRE
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
    }//FUNCIONA MAIN
    private List<Units> GetEnemyUnits(Units[] allUnits)// devuelve lista de unidades enemigas
    {
        List<Units> enemyUnits = new List<Units>();
        foreach (Units u in allUnits)
            if (!u.isPlayerUnit) enemyUnits.Add(u);
        return enemyUnits;
    }
    private bool CanEnemyAct(Units enemy) // valida energía + referencias
    {
        return !(enemy == null || enemy.currentNode == null || EnergyManager.instance.enemyCurrentEnergy < 1f);
    }
    private bool GetClosestValidTowerNode(out Tower targetTower, out Node targetNode)// busca torre objetivo y nodo de ataque
    {
        targetTower = null;
        targetNode = null;
        Tower[] allTowers = FindObjectsOfType<Tower>();
        List<Tower> candidateTowers = new List<Tower>();
        foreach (Tower t in allTowers)
            if (t.faction == Faction.Player && !t.isDestroyed)
                candidateTowers.Add(t);
        candidateTowers.Sort((a, b) => a.currentHealth.CompareTo(b.currentHealth));
        foreach (Tower tower in candidateTowers)
        {
            foreach (var nodeKey in TowerManager.instance.GetAttackNodes(tower))
            {
                string[] parts = nodeKey.Split('_');
                if (parts.Length != 3) continue;
                if (!int.TryParse(parts[1], out int x)) continue;
                if (!int.TryParse(parts[2], out int y)) continue;
                Node node = NodeManager.GetAllNodes().Find(n => n.gridIndex.x == x && n.gridIndex.y == y);
                if (node != null && !IsNodeReserved(node) && node.IsEmpty())
                {
                    targetTower = tower;
                    targetNode = node;
                    return true;
                }
            }
        }
        return false;
    }
    private List<Node> PreparePath(Units enemy, Node targetNode, ref bool unitDidAction)// calcula path y lo limita por energía
    {
        List<Node> path = PathFinding.CalculateAstart(enemy.currentNode, targetNode);
        int maxStep = Mathf.FloorToInt(EnergyManager.instance.enemyCurrentEnergy);
        if (path.Count > maxStep)
            path = path.GetRange(0, maxStep);
        if(path.Count > 0)
            unitDidAction = true;
        return path;
    }
    IEnumerator ExecuteMovementPath(Units enemy, List<Node> path, Node previousStep)
    {
        for (int i = 0; i < path.Count; i++)
        {
            Node step = path[i];
            if (enemy == null) yield break;
            ReleaseReservedNode(enemy);
            enemy.SetPath(new List<Node> { step });
            yield return new WaitUntil(() => enemy != null && enemy.PathEmpty());
            if (enemy == null) yield break;
            if (IsDangerousNode(step))
            {
                Node lastSafe = previousStep != null ? previousStep : enemy.lastSafeNode;
                yield return StartCoroutine(HandleDangerAndRepath(enemy, lastSafe));
                yield break;
            }
            enemy.lastSafeNode = step;
            previousStep = step;
            Units playerUnit;
            if (TryGetPlayerNeighbor(enemy, out playerUnit))
            {
                yield return StartCoroutine(CombatManager.instance.StartCombatWithUnit_Coroutine(enemy, playerUnit));
                if (enemy == null) yield break;
            }
            if (i == path.Count - 1)
                ReserveNode(enemy, step);
        }
    }
    private bool IsDangerousNode(Node step)
    {
        return step.IsDangerous;
    }
    IEnumerator HandleDangerAndRepath(Units enemy, Node lastSafeNode)
    {
        if (enemy == null) yield break;
        enemy.lastSafeNode = lastSafeNode;
        SalvationManager.instance.StartSavingThrow(enemy);
        yield return new WaitUntil(() => enemy == null || !SalvationManager.instance.GetOnSavingThrow);
        if (enemy == null) yield break;
        Node startNode = enemy.lastSafeNode;
        if (startNode == null) yield break;
        Tower targetTower;
        Node targetNode;
        if (!GetClosestValidTowerNode(out targetTower, out targetNode))
            yield break;
        List<Node> fullPath = PathFinding.CalculateAstart(startNode, targetNode);
        if (fullPath.Count == 0) yield break;
        int maxSteps = Mathf.FloorToInt(EnergyManager.instance.enemyCurrentEnergy);
        if (fullPath.Count > maxSteps)
            fullPath = fullPath.GetRange(0, maxSteps);
        enemy.SetPath(fullPath);
        if (fullPath.Count > 0)
            ReserveNode(enemy, fullPath[fullPath.Count - 1]);
        yield return StartCoroutine(ExecuteMovementPath(enemy, fullPath, enemy.lastSafeNode));
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