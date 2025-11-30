using System.Collections.Generic;
using UnityEngine;
[RequireComponent(typeof(GlowUnit))]
public abstract class Units : MonoBehaviour
{
    [Header("Stats")]
    public int currentHealth;
    public int diceCount = 1;
    [Header("Ownership")]
    public bool isPlayerUnit = true;
    [Header("Movement")]
    public Node currentNode;
    private Node targetNode;
    private List<Node> path = new List<Node>();
    public float moveSpeed = 5f;
    private float arriveThreshold = 0.05f;
    private GlowUnit _glow;
    private float originalY;
    [Header("Dice")]
    public DiceRoll diceInstance;
    protected virtual void Start()
    {
        currentNode = NodeManager.GetClosetNode(transform.position);
        targetNode = null;
        _glow = GetComponent<GlowUnit>();
        originalY = transform.position.y;
        if (currentNode != null)
        {
            Vector3 snap = currentNode.transform.position;
            snap.y = originalY;
            transform.position = snap;
        }
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
        if (targetPos.y < originalY)
            targetPos.y = originalY;
        transform.position = Vector3.MoveTowards(transform.position, targetPos, moveSpeed * Time.deltaTime);
        if (Vector3.Distance(transform.position, targetPos) <= arriveThreshold)
        {
            float requiredEnergy = 1f;
            bool isPlayerTurn = GameManager.instance.isPlayerTurn;
            if (EnergyManager.instance.TryConsumeEnergy(requiredEnergy, isPlayerTurn))
            {
                SetCurrentNode(path[0]);
                transform.position = GetSnappedPosition(currentNode);
                path.RemoveAt(0);
                if (path.Count > 0)
                    targetNode = path[0];
                else
                    targetNode = null;
            }
            else
            {
                path.Clear();
            }
        }
    }
    private Vector3 GetSnappedPosition(Node node)
    {
        Vector3 pos = node.transform.position;
        if (pos.y < originalY)
            pos.y = originalY;
        return pos;
    }
    public void SetCurrentNode(Node newNode)
    {
        if (currentNode != null && currentNode.unitOnNode == this.gameObject)
            currentNode.unitOnNode = null;
        currentNode = newNode;
        if (currentNode != null)
        {
            currentNode.unitOnNode = this.gameObject;
        }
    }
    public bool PathEmpty()
    {
        return path == null || path.Count == 0;
    }
    public void TakeDamage(int amount)
    {
        currentHealth -= amount;
        if (currentHealth <= 0)
        {
            currentHealth = 0;
            Die();
        }
    }
    private void Die()
    {
        Debug.Log($"{gameObject.name} ha muerto.");
        if (currentNode != null && currentNode.unitOnNode == this.gameObject)
            currentNode.unitOnNode = null;
        Destroy(gameObject);
    }
}