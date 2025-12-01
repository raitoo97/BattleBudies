using System.Collections.Generic;
using UnityEngine;
[RequireComponent(typeof(LineRenderer))]
public class PathDrawer : MonoBehaviour
{
    private LineRenderer lineRenderer;
    [Header("Path colors")]
    [SerializeField] private Color safeColor = Color.blue;
    [SerializeField] private Color dangerColor = Color.red;
    void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.positionCount = 0;
        Material mat = new Material(Shader.Find("Sprites/Default"));
        mat.color = safeColor;
        lineRenderer.material = mat;
        lineRenderer.startColor = safeColor;
        lineRenderer.endColor = safeColor;
    }
    public void DrawPath(Node start, Node end)
    {
        if (start == null || end == null)
        {
            lineRenderer.positionCount = 0;
            GridVisualizer.instance.ClearDangerNodes();
            return;
        }
        List<Node> path = PathFinding.CalculateAstart(start, end);
        if (path == null || path.Count == 0)
        {
            lineRenderer.positionCount = 0;
            GridVisualizer.instance.ClearDangerNodes();
            return;
        }
        int energyAvailable = Mathf.FloorToInt(EnergyManager.instance.currentEnergy);
        int nodesToDraw = Mathf.Min(path.Count, energyAvailable + 1);
        if (nodesToDraw == 0)
        {
            lineRenderer.positionCount = 0;
            GridVisualizer.instance.ClearDangerNodes();
            return;
        }
        bool danger = false;
        List<Node> nodesToMark = null;
        if (NodeManager.PathTouchesUnitNeighbor(path, out nodesToMark))
        {
            if (nodesToMark != null && nodesToMark.Count > 0)
            {
                Node firstDangerNode = null;
                foreach (var node in path)
                {
                    if (nodesToMark.Contains(node))
                    {
                        firstDangerNode = node;
                        break;
                    }
                }
                if (firstDangerNode != null)
                {
                    int index = path.IndexOf(firstDangerNode);
                    if (index <= energyAvailable)
                        danger = true;
                }
            }
        }
        Node lastReachableNode = path[Mathf.Min(energyAvailable, path.Count - 1)];
        if (TowerManager.instance.TryGetTowerAtNode(lastReachableNode, out Tower tower))
        {
            if (tower.faction == Faction.Enemy)
            {
                danger = true;
                if (nodesToMark == null) nodesToMark = new List<Node>();
                if (!nodesToMark.Contains(lastReachableNode))
                    nodesToMark.Add(lastReachableNode);
            }
        }
        SetLineColor(danger ? dangerColor : safeColor);
        if (danger)
            GridVisualizer.instance.SetDangerNodes(nodesToMark);
        else
            GridVisualizer.instance.ClearDangerNodes();
        lineRenderer.positionCount = nodesToDraw;
        for (int i = 0; i < nodesToDraw; i++)
        {
            lineRenderer.SetPosition(i, path[i].transform.position + Vector3.up * 0.2f);
        }
    }
    public void ClearPath()
    {
        lineRenderer.positionCount = 0;
        GridVisualizer.instance.ClearDangerNodes();
    }
    void SetLineColor(Color c)
    {
        if (lineRenderer.material == null)
            lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        lineRenderer.material.color = c;
        lineRenderer.startColor = c;
        lineRenderer.endColor = c;
    }   
}
