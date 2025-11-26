using System.Collections.Generic;
using UnityEngine;
public class Node : MonoBehaviour
{
    public Vector2Int gridIndex;
    [SerializeField]private List<Node> _neighbors = new List<Node>();
    [SerializeField]private float _cost;
    public void Initalize(Vector2Int xY)
    {
        gridIndex = xY;
        _cost = 1;
        NodeManager.RegisterNode(this);
    }
    private void OnDestroy()
    {
        NodeManager.UnregisterNode(this);
        _neighbors.Clear();
    }
    public List<Node> Neighbors
    {
        get
        {
            if (_neighbors.Count > 0)
                return _neighbors;
            Node right = GridManager.instance.GetNode(gridIndex.x + 1, gridIndex.y);
            if (right != null) _neighbors.Add(right);
            Node left = GridManager.instance.GetNode(gridIndex.x - 1, gridIndex.y);
            if (left != null) _neighbors.Add(left);
            Node up = GridManager.instance.GetNode(gridIndex.x, gridIndex.y + 1);
            if (up != null) _neighbors.Add(up);
            Node down = GridManager.instance.GetNode(gridIndex.x, gridIndex.y - 1);
            if (down != null) _neighbors.Add(down);
            Node upRight = GridManager.instance.GetNode(gridIndex.x + 1, gridIndex.y + 1);
            if (upRight != null) _neighbors.Add(upRight);
            Node upLeft = GridManager.instance.GetNode(gridIndex.x - 1, gridIndex.y + 1);
            if (upLeft != null) _neighbors.Add(upLeft);
            Node downRight = GridManager.instance.GetNode(gridIndex.x + 1, gridIndex.y - 1);
            if (downRight != null) _neighbors.Add(downRight);
            Node downLeft = GridManager.instance.GetNode(gridIndex.x - 1, gridIndex.y - 1);
            if (downLeft != null) _neighbors.Add(downLeft);
            return _neighbors;
        }
    }
    public float Cost { get => _cost; }
}
