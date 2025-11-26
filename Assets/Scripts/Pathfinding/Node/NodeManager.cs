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
    public static List<Node> GetNodeCount()
    {
        return _totalNodes;
    }
}
