using UnityEngine;
public class GridVisualizer : MonoBehaviour
{
    [Header("Colors")]
    [SerializeField] private Color defaultColor = Color.white;
    [SerializeField] private Color hoverColor = new Color(1f, 0.9f, 0f, 1f) * 2f;
    [SerializeField] private Color selectedColor = new Color(0, 0.4f, 1f, 1f) * 3f; 
    [Header("Line Settings")]
    [SerializeField] private Material lineMaterial;
    [SerializeField] private float lineWidth = 0.05f;
    [SerializeField] private float _liftHeight = 0.05f;
    private LineRenderer[,] lines;
    private Node hoveredNode;
    private Node selectedNode;
    void Start()
    {
        CreateAllLines();
    }
    void Update()
    {
        DetectHover();
        DetectClick();
    }
    void CreateAllLines()
    {
        var gm = GridManager.instance;
        int rows = gm.Rows;
        int cols = gm.Columns;
        lines = new LineRenderer[rows, cols];
        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                Node node = gm.GetNode(r, c);
                lines[r, c] = CreateLine(node.transform, gm.Offset);
                lines[r, c].material = new Material(lineMaterial);
                SetColor(lines[r, c], defaultColor);
            }
        }
    }
    LineRenderer CreateLine(Transform parent, float size)
    {
        GameObject go = new GameObject("Line");
        go.transform.SetParent(parent);
        go.transform.localPosition = Vector3.zero;
        float half = size * 0.5f;
        LineRenderer lr = go.AddComponent<LineRenderer>();
        lr.loop = true;
        lr.useWorldSpace = false;
        lr.positionCount = 4;
        lr.widthMultiplier = lineWidth;
        lr.SetPosition(0, new Vector3(-half, 0, -half));
        lr.SetPosition(1, new Vector3(-half, 0, half));
        lr.SetPosition(2, new Vector3(half, 0, half));
        lr.SetPosition(3, new Vector3(half, 0, -half));
        return lr;
    }
    void DetectHover()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        Node hitNode = null;
        if (Physics.Raycast(ray, out RaycastHit hit))
            hitNode = hit.collider.GetComponent<Node>();
        if (hoveredNode != hitNode)
        {
            if (hoveredNode != null && hoveredNode != selectedNode)
            {
                SetColor(hoveredNode, defaultColor);
                SetLinePosition(hoveredNode, false);
            }
            hoveredNode = hitNode;
            if (hoveredNode != null)
            {
                if (hoveredNode != selectedNode)
                {
                    SetColor(hoveredNode, hoverColor);
                    SetLinePosition(hoveredNode, true);
                }
            }
        }
    }
    void DetectClick()
    {
        if (Input.GetMouseButtonDown(0))
        {
            if (hoveredNode == null)
            {
                ResetSelection();
                return;
            }
            SetSelection(hoveredNode);
        }
    }
    void SetSelection(Node node)
    {
        if (selectedNode != null)
        {
            SetColor(selectedNode, defaultColor);
            SetLinePosition(selectedNode, false);
        }
        selectedNode = node;
        SetColor(selectedNode, selectedColor);
        SetLinePosition(selectedNode, true);
    }
    void ResetSelection()
    {
        if (selectedNode != null)
        {
            SetColor(selectedNode, defaultColor);
            SetLinePosition(selectedNode, false);
        }
        selectedNode = null;
        if (hoveredNode != null)
        {
            SetColor(hoveredNode, hoverColor);
            SetLinePosition(hoveredNode, true);
        }
    }
    void SetColor(Node n, Color c)
    {
        var idx = n.GridIndex;
        SetColor(lines[idx.x, idx.y], c);
    }
    void SetColor(LineRenderer lr, Color c)
    {
        lr.startColor = c;
        lr.endColor = c;
        if (lr.material.HasProperty("_Color"))
            lr.material.SetColor("_Color", c);
    }
    void SetLinePosition(Node n, bool lifted)
    {
        var idx = n.GridIndex;
        SetLinePosition(lines[idx.x, idx.y], lifted);
    }
    void SetLinePosition(LineRenderer lr, bool lifted)
    {
        float y = lifted ? _liftHeight : 0f;
        lr.transform.localPosition = new Vector3(0, y, 0);
    }
}