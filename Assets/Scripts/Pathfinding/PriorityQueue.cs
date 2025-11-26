using System.Collections.Generic;
using UnityEngine;
public class PriorityQueue <T>
{
    private Dictionary<T, float> _allElements = new Dictionary<T, float>();
    public int Count { get { return _allElements.Count; } }
    public void Enqueue(T element, float cost)
    {
        if (!_allElements.ContainsKey(element))
            _allElements.Add(element, cost);
        else
            _allElements[element] = cost;
    }
    public T Dequeue()
    {
        T minElement = default;
        float minCost = Mathf.Infinity;
        foreach (var item in _allElements)
        {
            if (item.Value < minCost)
            {
                minCost = item.Value;
                minElement = item.Key;
            }
        }
        _allElements.Remove(minElement);
        return minElement;
    }
}
