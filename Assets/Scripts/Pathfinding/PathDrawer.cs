using System.Collections.Generic;
using UnityEngine;
[RequireComponent(typeof(LineRenderer))]
public class PathDrawer : MonoBehaviour
{
    private LineRenderer lineRenderer;
    void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.positionCount = 0;
        Material mat = new Material(Shader.Find("Sprites/Default"));
        mat.color = Color.blue;
        lineRenderer.material = mat;
        lineRenderer.startColor = Color.blue;
        lineRenderer.endColor = Color.blue;
    }
    public void DrawPath(Node start, Node end)
    {
        if (start == null || end == null)
        {
            lineRenderer.positionCount = 0;
            return;
        }
        List<Node> path = PathFinding.CalculateAstart(start, end);
        if (path == null || path.Count == 0)
        {
            lineRenderer.positionCount = 0;
            return;
        }
        lineRenderer.positionCount = path.Count;
        for (int i = 0; i < path.Count; i++)
        {
            lineRenderer.SetPosition(i, path[i].transform.position + Vector3.up * 0.2f);
        }
    }
    public void ClearPath()
    {
        lineRenderer.positionCount = 0;
    }
}
