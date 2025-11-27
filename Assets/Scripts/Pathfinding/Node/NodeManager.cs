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
    public static List<Node> GetNodeCount()
    {
        return _totalNodes;
    }
}
