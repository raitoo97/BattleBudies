using System.Collections.Generic;
using UnityEngine;
public class Node : MonoBehaviour
{
    public Vector2Int gridIndex;
    [SerializeField] private List<Node> _gridNeighbors = new List<Node>();
    [SerializeField] private List<Node> _extraNeighbors = new List<Node>();
    [SerializeField] private float _cost;
    public float maxStepUp = 1f;
    public float maxStepDown = 1f;
    public GameObject unitOnNode;
    [SerializeField]private bool _isDangerous;
    [SerializeField]public bool _isBlock;
    public bool _isResourceNode;
    public bool _isDefendTowerNode;
    public void Initalize(Vector2Int xY)
    {
        gridIndex = xY;
        _cost = 1;
        _isDangerous = false;
        _isBlock = false;
        _isResourceNode = false;
        _isDefendTowerNode = false;
        NodeManager.RegisterNode(this);
    }
    private void OnDestroy()
    {
        NodeManager.UnregisterNode(this);
        _gridNeighbors.Clear();
        _extraNeighbors.Clear();
    }
    public void SetFloorNeighbor(Node floorNode)
    {
        if (floorNode == null) return;
        if (!_extraNeighbors.Contains(floorNode))
            _extraNeighbors.Add(floorNode);
        if (!floorNode._extraNeighbors.Contains(this))
            floorNode._extraNeighbors.Add(this);
    }
    public List<Node> Neighbors
    {
        get
        {
            if (_gridNeighbors.Count == 0)
                InitializeGridNeighbors();
            List<Node> allNeighbors = new List<Node>(_gridNeighbors);
            allNeighbors.AddRange(_extraNeighbors);
            return allNeighbors;
        }
    }
    public void InitializeGridNeighbors()
    {
        Node[] possibleNeighbors = {
            GridManager.instance.GetNode(gridIndex.x + 1, gridIndex.y),
            GridManager.instance.GetNode(gridIndex.x - 1, gridIndex.y),
            GridManager.instance.GetNode(gridIndex.x, gridIndex.y + 1),
            GridManager.instance.GetNode(gridIndex.x, gridIndex.y - 1),
            GridManager.instance.GetNode(gridIndex.x + 1, gridIndex.y + 1),
            GridManager.instance.GetNode(gridIndex.x - 1, gridIndex.y + 1),
            GridManager.instance.GetNode(gridIndex.x + 1, gridIndex.y - 1),
            GridManager.instance.GetNode(gridIndex.x - 1, gridIndex.y - 1)
        };
        foreach (var node in possibleNeighbors)
        {
            if (node == null) continue;
            float heightDiff = Mathf.Abs(node.transform.position.y - transform.position.y);
            if (heightDiff <= 1f)
                _gridNeighbors.Add(node);
        }
    }
    private void OnMouseDown()
    {
        if (Input.GetMouseButtonDown(0) && CardPlayManager.instance != null)
        {
            CardPlayManager.instance.NodeClicked(this);
        }
    }
    public bool IsEmpty()
    {
        return unitOnNode == null;
    }
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.layer == 8)//trampas
        {
            _cost+= 30;
            _isDangerous = true;
            this.gameObject.layer = 13;// DangerNodeText
        }
        if (collision.gameObject.layer == 9)//resources
        {
            _isBlock = true;
            _isResourceNode = true;
            foreach(var neighboard in _gridNeighbors)
            {
                neighboard.gameObject.layer = 11;// ResourceNodeText
            }
        }
        if (collision.gameObject.layer == 10)//resources
        {
            _isDefendTowerNode = true;
            this.gameObject.layer = 12;// HelathNodeText
        }
    }
    public float Cost { get => _cost; }
    public bool IsDangerous { get => _isDangerous; }
}
