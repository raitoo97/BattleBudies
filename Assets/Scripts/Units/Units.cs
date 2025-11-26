using System.Collections.Generic;
using UnityEngine;
public abstract class Units : MonoBehaviour
{
    public Node currentNode;
    private Node targetNode;
    private List<Node> path = new List<Node>();
    public float moveSpeed = 5f;
    private float arriveThreshold = 0.05f;
    protected virtual void Start()
    {
        currentNode = NodeManager.GetClosetNode(transform.position);
        targetNode = null;
    }

    protected virtual void Update()
    {
        FollowPath();
    }
    public void SetPath(List<Node> newPath)
    {
        if (newPath == null || newPath.Count == 0) return;
        path = new List<Node>(newPath);
        targetNode = path[0];
    }
    private void FollowPath()
    {
        if (path == null || path.Count == 0) return;
        Vector3 targetPos = path[0].transform.position;
        transform.position = Vector3.MoveTowards(transform.position, targetPos, moveSpeed * Time.deltaTime);
        if (Vector3.Distance(transform.position, targetPos) <= arriveThreshold)
        {
            currentNode = path[0];
            path.RemoveAt(0);
        }
    }
}
