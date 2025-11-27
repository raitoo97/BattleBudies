using System.Collections.Generic;
using UnityEngine;
[RequireComponent(typeof(GlowUnit))]
public abstract class Units : MonoBehaviour
{
    public Node currentNode;
    private Node targetNode;
    private List<Node> path = new List<Node>();
    public float moveSpeed = 5f;
    private float arriveThreshold = 0.05f;
    private GlowUnit _glow;
    private float originalY;
    public CardInstance cardInstance;
    public int currentHealth;
    public int damage;
    protected virtual void Start()
    {
        currentNode = NodeManager.GetClosetNode(transform.position);
        targetNode = null;
        _glow = GetComponent<GlowUnit>();
        originalY = transform.position.y;
        if (cardInstance != null)
        {
            currentHealth = cardInstance.currentHealth;
            damage = cardInstance.GetDamage();
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
            if (EnergyManager.instance.TryConsumeEnergy(requiredEnergy))
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
    public void SetCard(CardInstance card)
    {
        cardInstance = card;
        currentHealth = card.currentHealth;
        damage = card.GetDamage();
    }
    public void TakeDamage(int amount)
    {
        currentHealth -= amount;
        if (cardInstance != null)
            cardInstance.TakeDamage(amount);

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
