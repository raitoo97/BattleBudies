using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class GridVisualizer : MonoBehaviour
{
    [Header("Colors")]
    [SerializeField] private Color defaultColor = Color.white;
    [SerializeField] private Color hoverColor = new Color(1f, 0.9f, 0f, 1f) * 2f;
    [SerializeField] private Color selectedColor = new Color(0, 0.4f, 1f, 1f) * 3f;
    [SerializeField] private Color placementRowColor = new Color(0f, 1f, 0f, 1f) * 3f;
    [SerializeField] private Color dangerColor = new Color(1f, 0f, 0f, 1f) * 3f;
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
    private HashSet<Node> dangerNodes = new HashSet<Node>();
    public bool upgradeMode = false;
    private Coroutine upgradeHighlightCoroutine;
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
        if (upgradeMode && upgradeHighlightCoroutine == null)
        {
            upgradeHighlightCoroutine = StartCoroutine(PulsePlayerUnits());
        }
        else if (!upgradeMode && upgradeHighlightCoroutine != null)
        {
            StopCoroutine(upgradeHighlightCoroutine);
            upgradeHighlightCoroutine = null;
            ResetPlayerUnitsColor();
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
                if (!dangerNodes.Contains(hoveredNode))
                {
                    SetColor(hoveredNode, defaultColor);
                    SetLinePosition(hoveredNode, false);
                }
            }
            hoveredNode = hitNode;
            if (hoveredNode != null)
            {
                if (hoveredNode != selectedNode)
                {
                    if (!dangerNodes.Contains(hoveredNode))
                    {
                        SetColor(hoveredNode, hoverColor);
                        SetLinePosition(hoveredNode, true);
                    }
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
            if (!dangerNodes.Contains(selectedNode))
            {
                SetColor(selectedNode, defaultColor);
                SetLinePosition(selectedNode, false);
            }
        }
        selectedNode = node;
        if (!dangerNodes.Contains(selectedNode))
        {
            SetColor(selectedNode, selectedColor);
            SetLinePosition(selectedNode, true);
        }
    }
    void ResetSelection()
    {
        if (selectedNode != null)
        {
            if (!dangerNodes.Contains(selectedNode))
            {
                SetColor(selectedNode, defaultColor);
                SetLinePosition(selectedNode, false);
            }
        }
        selectedNode = null;
        if (hoveredNode != null)
        {
            if (!dangerNodes.Contains(hoveredNode))
            {
                SetColor(hoveredNode, hoverColor);
                SetLinePosition(hoveredNode, true);
            }
        }
    }
    public void SetColor(Node n, Color c)
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
    public void SetLinePosition(Node n, bool lifted)
    {
        var idx = n.gridIndex;
        SetLinePosition(lines[idx.x, idx.y], lifted);
    }
    void SetLinePosition(LineRenderer lr, bool lifted)
    {
        float y = lifted ? _liftHeight : 0f;
        lr.transform.localPosition = new Vector3(0, y, 0);
    }
    public void SetDangerNodes(IEnumerable<Node> nodes)
    {
        ClearDangerNodes();
        foreach (Node n in nodes)
        {
            if (n == null) continue;
            dangerNodes.Add(n);
            SetColor(n, dangerColor);
            SetLinePosition(n, true);
        }
    }
    public void ClearDangerNodes()
    {
        if (dangerNodes.Count == 0) return;
        var toClear = new List<Node>(dangerNodes);
        dangerNodes.Clear();
        foreach (Node n in toClear)
        {
            if (n == null) continue;
            if (n == selectedNode)
            {
                SetColor(n, selectedColor);
                SetLinePosition(n, true);
            }
            else if (n == hoveredNode)
            {
                SetColor(n, hoverColor);
                SetLinePosition(n, true);
            }
            else
            {
                SetColor(n, defaultColor);
                SetLinePosition(n, false);
            }
        }
    }
    IEnumerator PulsePlayerUnits()
    {
        float duration = 0.5f;
        float t = 0f;
        bool toGreen = true;
        List<Node> playerNodes = NodeManager.PlayerUnits();
        while (upgradeMode)
        {
            t += Time.deltaTime / duration;
            Color targetColor = toGreen ? placementRowColor : defaultColor;
            Color fromColor = toGreen ? defaultColor : placementRowColor;
            Color lerped = Color.Lerp(fromColor, targetColor, t);
            foreach (var node in playerNodes)
            {
                if (node == null) continue;

                if (node != hoveredNode && node != selectedNode)
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
    void ResetPlayerUnitsColor()
    {
        List<Node> playerNodes = NodeManager.PlayerUnits();
        foreach (var node in playerNodes)
        {
            if (node == null) continue;

            if (node != hoveredNode && node != selectedNode)
            {
                SetColor(node, defaultColor);
                SetLinePosition(node, false);
            }
        }
    }
}