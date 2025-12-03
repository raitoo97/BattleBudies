using System.Collections.Generic;
using UnityEngine;
public static class NodeManager
{
    private static List<Node> _totalNodes = new List<Node>();
    public static void RegisterNode(Node node)
    {
        if (!_totalNodes.Contains(node))
            _totalNodes.Add(node);
    }
    public static void UnregisterNode(Node node)
    {
        if (_totalNodes.Contains(node))
            _totalNodes.Remove(node);
    }
    public static Node GetClosetNode(Vector3 pos)
    {
        Node closestNode = null;
        float closestDistance = Mathf.Infinity;
        Vector2 posXZ = new Vector2(pos.x, pos.z);
        foreach (var node in _totalNodes)
        {
            Vector2 nodeXZ = new Vector2(node.transform.position.x, node.transform.position.z);
            float currentDistance = Vector2.Distance(nodeXZ, posXZ);

            if (currentDistance < closestDistance)
            {
                closestDistance = currentDistance;
                closestNode = node;
            }
        }
        return closestNode;
    }
    public static Node GetRandomEmptyNodeOnRow(int row)
    {
        List<Node> emptyNodes = new List<Node>();
        foreach (Node node in _totalNodes)
        {
            if (node.gridIndex.x == row && node.IsEmpty())
            {
                emptyNodes.Add(node);
            }
        }
        if (emptyNodes.Count == 0) return null;
        int index = Random.Range(0, emptyNodes.Count);
        return emptyNodes[index];
    }
    public static bool PathTouchesUnitNeighbor(List<Node> path, out List<Node> nodesToMark)
    {
        nodesToMark = new List<Node>();
        var allNodes = GetNodeCount();
        if (allNodes == null || allNodes.Count == 0)
            return false;
        bool detectPlayerUnits = !GameManager.instance.isPlayerTurn;
        foreach (var node in allNodes)
        {
            if (node == null) continue;
            if (node.unitOnNode == null)
                continue;
            Units unit = node.unitOnNode.GetComponent<Units>();
            if (unit == null) continue;
            if (detectPlayerUnits && !unit.isPlayerUnit) continue;
            if (!detectPlayerUnits && unit.isPlayerUnit) continue;
            if (path.Contains(node))
            {
                AddNodeSafe(nodesToMark, node);

                foreach (var neigh in node.Neighbors)
                    AddNodeSafe(nodesToMark, neigh);

                continue;
            }
            foreach (var p in path)
            {
                if (node.Neighbors.Contains(p))
                {
                    AddNodeSafe(nodesToMark, node);

                    foreach (var neigh in node.Neighbors)
                        AddNodeSafe(nodesToMark, neigh);

                    break;
                }
            }
        }
        return nodesToMark.Count > 0;
    }
    private static void AddNodeSafe(List<Node> list, Node n)
    {
        if (n != null && !list.Contains(n))
            list.Add(n);
    }
    public static Units GetFirstUnitInAttackZone(Units unit1, Units unit2)
    {
        List<Node> attackZone = new List<Node>();
        if (unit1.currentNode != null)
        {
            attackZone.Add(unit1.currentNode);
            attackZone.AddRange(unit1.currentNode.Neighbors);
        }
        if (unit2.currentNode != null && attackZone.Contains(unit2.currentNode))
        {
            return unit2;
        }
        foreach (var node in attackZone)
        {
            if (node == null || node.unitOnNode == null) continue;

            Units unit = node.unitOnNode.GetComponent<Units>();
            if (unit == null) continue;

            if (unit.isPlayerUnit != unit1.isPlayerUnit)
                return unit;
        }
        return null;
    }
    public static Node FindFirstDangerStep(List<Node> path, List<Node> dangerNodes)
    {
        foreach (Node step in path)
        {
            if (dangerNodes.Contains(step))
                return step;
        }
        return null;
    }
    public static List<Node> GetNodeCount()
    {
        return _totalNodes;
    }
    public static List<Node> TouchDangerousNodes()
    {
        var dangerNodes = new List<Node>();
        foreach (var node in _totalNodes)
        {
            if (node == null) continue;
            if (node.IsDangerous)
                dangerNodes.Add(node);
        }
        return dangerNodes;
    }
    public static Node FindFirstDangerousNode(List<Node> path)
    {
        var dangerous = TouchDangerousNodes();
        foreach (var step in path)
        {
            if (dangerous.Contains(step))
                return step;
        }
        return null;
    }
    public static Node GetForwardSafeNode(Node safeNode, Node currentNode)
    {
        if(safeNode == null || currentNode == null)
        return null;
        int dirX = Mathf.Clamp(currentNode.gridIndex.x - safeNode.gridIndex.x, -1, 1);
        int dirY = Mathf.Clamp(currentNode.gridIndex.y - safeNode.gridIndex.y, -1, 1);
        int targetX = currentNode.gridIndex.x + dirX;
        int targetY = currentNode.gridIndex.y + dirY;
        foreach (var node in NodeManager.GetAllNodes())
        {
            if (node == null) continue;
            if (node.gridIndex.x != targetX) continue;
            if (node.gridIndex.y != targetY) continue;
            if (node.IsDangerous) continue;
            if (!node.IsEmpty()) continue;

            return node;
        }
        return null;
    }
    public static List<Node> GetAllNodes()
    {
        return _totalNodes;
    }
}