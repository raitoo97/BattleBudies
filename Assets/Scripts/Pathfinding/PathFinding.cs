using System.Collections.Generic;
using UnityEngine;
public static class PathFinding
{
    public static List<Node> CalculateAstart(Node start, Node end)
    {
        if (start == null || end == null)
        {
            Debug.Log("Start o end null");
            return new List<Node>();
        }
        var frontier = new PriorityQueue<Node>();
        frontier.Enqueue(start, 0);
        var cameFrom = new Dictionary<Node, Node>();
        cameFrom.Add(start, null);
        var costSoFar = new Dictionary<Node, float>();
        costSoFar.Add(start, 0);
        while (frontier.Count > 0)
        {
            var current = frontier.Dequeue();
            if (current == end)
            {
                var path = new List<Node>();
                while (current != null)
                {
                    path.Add(current);
                    current = cameFrom[current];
                }
                path.Reverse();
                return path;
            }
            foreach (var node in current.Neighbors)
            {
                if (!node.IsEmpty()) continue;
                if (node._isBlock) continue;
                float newCost = costSoFar[current] + node.Cost;
                float distance = Vector3.Distance(node.transform.position, end.transform.position);
                float priority = newCost + distance;
                if (!cameFrom.ContainsKey(node))
                {
                    frontier.Enqueue(node, priority);
                    cameFrom.Add(node, current);
                    costSoFar.Add(node, newCost);
                }
                else if (costSoFar[node] > newCost)
                {
                    cameFrom[node] = current;
                    costSoFar[node] = newCost;
                    frontier.Enqueue(node, priority);
                }
            }
        }
        return new List<Node>();
    }
}

