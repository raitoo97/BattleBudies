using UnityEngine;
using System.Collections;
public class GridVisualizer : MonoBehaviour
{
    [Header("Colors")]
    [SerializeField] private Color defaultColor = Color.white;
    [SerializeField] private Color hoverColor = new Color(1f, 0.9f, 0f, 1f) * 2f;
    [SerializeField] private Color selectedColor = new Color(0, 0.4f, 1f, 1f) * 3f;
    [SerializeField] private Color placementRowColor = new Color(0f, 1f, 0f, 1f) * 3f;
    [Header("Line Settings")]
    [SerializeField] private Material lineMaterial;
    [SerializeField] private float lineWidth = 0.05f;
    [SerializeField] private float _liftHeight = 0.05f;
    private LineRenderer[,] lines;
    private Node hoveredNode;
    private Node selectedNode;
    public bool placingMode = false;
    private Coroutine placementHighlightCoroutine;
    public static GridVisualizer instance;
    private void Awake()
    {
        if (instance == null)
            instance = this;
        else
            Destroy(gameObject);
    }
    void Start()
    {
        CreateAllLines();
    }
    void Update()
    {
        DetectHover();
        DetectClick();
        if (placingMode && placementHighlightCoroutine == null)
            placementHighlightCoroutine = StartCoroutine(PulsePlacementRow());
        else if (!placingMode && placementHighlightCoroutine != null)
        {
            StopCoroutine(placementHighlightCoroutine);
            placementHighlightCoroutine = null;
            ResetPlacementRowColor();
        }
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
    IEnumerator PulsePlacementRow()
    {
        var gm = GridManager.instance;
        int cols = gm.Columns;
        float duration = 0.5f;
        float t = 0f;
        bool toGreen = true;
        while (placingMode)
        {
            t += Time.deltaTime / duration;
            Color targetColor = toGreen ? placementRowColor : defaultColor;
            Color fromColor = toGreen ? defaultColor : placementRowColor;
            Color lerped = Color.Lerp(fromColor, targetColor, t);
            for (int c = 0; c < cols; c++)
            {
                Node node = gm.GetNode(0, c);
                if (node != null && node != hoveredNode && node != selectedNode)
                {
                    SetColor(node, lerped);
                    SetLinePosition(node, true);
                }
            }
            if (t >= 1f)
            {
                t = 0f;
                toGreen = !toGreen;
            }
            yield return null;
        }
    }
    void ResetPlacementRowColor()
    {
        var gm = GridManager.instance;
        int cols = gm.Columns;
        for (int c = 0; c < cols; c++)
        {
            Node node = gm.GetNode(0, c);
            if (node != null && node != hoveredNode && node != selectedNode)
            {
                SetColor(node, defaultColor);
                SetLinePosition(node, false);
            }
        }
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
        var idx = n.gridIndex;
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
        var idx = n.gridIndex;
        SetLinePosition(lines[idx.x, idx.y], lifted);
    }
    void SetLinePosition(LineRenderer lr, bool lifted)
    {
        float y = lifted ? _liftHeight : 0f;
        lr.transform.localPosition = new Vector3(0, y, 0);
    }
}