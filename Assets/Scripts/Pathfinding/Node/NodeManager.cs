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
        foreach (var node in _totalNodes)
        {
            float currentDistance = Vector3.Distance(node.transform.position, pos);
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
        if (allNodes == null || allNodes.Count == 0) return false;
        bool detectPlayerUnits = !GameManager.instance.isPlayerTurn;
        foreach (var node in allNodes)
        {
            if (node == null || node.unitOnNode == null) continue;
            var unitsScript = node.unitOnNode.GetComponent<Units>();
            if (unitsScript == null) continue;
            if (detectPlayerUnits && !unitsScript.isPlayerUnit) continue;
            if (!detectPlayerUnits && unitsScript.isPlayerUnit) continue;
            if (path.Contains(node))
            {
                if (!nodesToMark.Contains(node)) nodesToMark.Add(node);
                foreach (var neigh in node.Neighbors)
                    if (neigh != null && !nodesToMark.Contains(neigh))
                        nodesToMark.Add(neigh);
                continue;
            }
            foreach (var p in path)
            {
                if (node.Neighbors.Contains(p))
                {
                    if (!nodesToMark.Contains(node)) nodesToMark.Add(node);
                    foreach (var neigh in node.Neighbors)
                        if (neigh != null && !nodesToMark.Contains(neigh))
                            nodesToMark.Add(neigh);
                    break;
                }
            }
        }
        return nodesToMark.Count > 0;
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
    public static List<Node> GetNodeCount()
    {
        return _totalNodes;
    }
}
