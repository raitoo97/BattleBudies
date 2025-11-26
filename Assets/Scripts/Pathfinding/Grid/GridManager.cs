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
    }
    void CreateGrid()
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
                node.GridIndex = new Vector2Int(r, c);
                node.name = $"Node_{r}_{c}";
                _grid[r, c] = node;
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
    void OnDrawGizmos()
    {
        if (!Application.isPlaying)
        {
            //DrawGridRays();DebugGrilla
        }
    }
    void DrawGridRays()
    {
        Gizmos.color = Color.yellow;
        float totalWidth = (_columns - 1) * _offset;
        float totalHeight = (_rows - 1) * _offset;
        Vector3 origin = transform.position - new Vector3(totalWidth / 2f, 0f, totalHeight / 2f);
        for (int r = 0; r < _rows; r++)
        {
            for (int c = 0; c < _columns; c++)
            {
                Vector3 basePos = origin + new Vector3(c * _offset, 0f, r * _offset);
                Vector3 rayStart = basePos + Vector3.up * _rayHeight;
                Vector3 rayEnd = basePos + Vector3.down * _rayHeight;
                Gizmos.DrawLine(rayStart, rayEnd);
                Gizmos.DrawSphere(rayStart, 0.1f);
            }
        }
    }
}
