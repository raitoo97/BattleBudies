using System.Collections.Generic;
using UnityEngine;
[DefaultExecutionOrder(-100)]
public class GridManager : MonoBehaviour
{
    [SerializeField]private Node _node;
    private Node [,] _grid;
    [SerializeField]private int _rows, _columns;
    [Range(0.1f,10f)][SerializeField]private float _offset;
    [SerializeField] private LayerMask _groundMask;
    [SerializeField] private float _rayHeight = 30f;
    public static GridManager instance;
    private void Awake()
    {
        if (instance == null)
            instance = this;
        else
            Destroy(gameObject);
    }
    void Start()
    {
        CreateGrid();
        AddBridgeNeighbords();
        InitializeNeighbors();
    }
    private void CreateGrid()
    {
        _grid = new Node[_rows, _columns];
        float totalWidth = (_columns - 1) * _offset;
        float totalHeight = (_rows - 1) * _offset;
        Vector3 origin = transform.position - new Vector3(totalWidth / 2f, 0f, totalHeight / 2f);
        for (int r = 0; r < _rows; r++)
        {
            for (int c = 0; c < _columns; c++)
            {
                Vector3 basePos = origin + new Vector3(c * _offset, 0f, r * _offset);
                Vector3 rayStart = basePos + Vector3.up * _rayHeight;
                Vector3 finalPos = basePos;
                if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, _rayHeight * 2f, _groundMask))
                {
                    finalPos.y = hit.point.y;
                }
                else
                {
                    Debug.LogWarning($"Node {r},{c} no encontró suelo. Marcando como no walkable.");
                }
                Node node = Instantiate(_node, finalPos, Quaternion.identity, transform);
                node.Initalize(new Vector2Int(r, c));
                node.name = $"Node_{r}_{c}";
                _grid[r, c] = node;
            }
        }
    }
    private void AddBridgeNeighbords()
    {
        Dictionary<Vector2Int, Vector2Int> bridgeEdges = new Dictionary<Vector2Int, Vector2Int>();
        bridgeEdges.Add(new Vector2Int(3, 2), new Vector2Int(2, 2));
        bridgeEdges.Add(new Vector2Int(3, 7), new Vector2Int(2, 7));
        bridgeEdges.Add(new Vector2Int(3, 12), new Vector2Int(2, 12));
        bridgeEdges.Add(new Vector2Int(9, 2), new Vector2Int(10, 2));
        bridgeEdges.Add(new Vector2Int(9, 7), new Vector2Int(10, 7));
        bridgeEdges.Add(new Vector2Int(9, 12), new Vector2Int(10, 12));
        foreach (var kvp in bridgeEdges)
        {
            Node bridgeNode = GetNode(kvp.Key.x, kvp.Key.y);
            Node floorNode = GetNode(kvp.Value.x, kvp.Value.y);
            if (bridgeNode != null && floorNode != null)
            {
                bridgeNode.SetFloorNeighbor(floorNode);
                bridgeNode.maxStepUp = 2f;
                bridgeNode.maxStepDown = 2f;
                floorNode.maxStepUp = 2f;
                floorNode.maxStepDown = 2f;
            }
        }
    }
    private void InitializeNeighbors()
    {
        for (int r = 0; r < _rows; r++)
        {
            for (int c = 0; c < _columns; c++)
            {
                _grid[r, c].InitializeGridNeighbors();
            }
        }
    }
    public Node GetNode(int r, int c)
    {
        if (r < 0 || r >= _rows || c < 0 || c >= _columns) return null;
        return _grid[r, c];
    }
    public int Rows { get => _rows; }
    public int Columns { get => _columns; }
    public float Offset { get => _offset; }
}
