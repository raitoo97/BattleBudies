using System.Collections.Generic;
using UnityEngine;
[RequireComponent(typeof(GlowUnit))]
public abstract class Units : MonoBehaviour
{
    [Header("Stats")]
    public int currentHealth;
    public int diceCount = 1;
    public int damage;
    [Header("Movement")]
    public Node currentNode;
    private Node targetNode;
    private List<Node> path = new List<Node>();
    public float moveSpeed = 5f;
    private float arriveThreshold = 0.05f;
    private GlowUnit _glow;
    private float originalY;
    protected virtual void Start()
    {
        currentNode = NodeManager.GetClosetNode(transform.position);
        targetNode = null;
        _glow = GetComponent<GlowUnit>();
        originalY = transform.position.y;
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
                currentNode = path[0];
                path.RemoveAt(0);
            }
            else
            {
                path.Clear();
            }
        }
    }
    public int RollDamage()
    {
        int total = 0;
        for (int i = 0; i < diceCount; i++)
        {
            total += Random.Range(1, 7);
        }
        return total;
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
        Destroy(gameObject);
    }
}