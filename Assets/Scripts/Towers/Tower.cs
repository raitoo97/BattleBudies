using System.Collections.Generic;
using UnityEngine;
public enum teamTower
{
    player,
    enemy
}
public class Tower : MonoBehaviour
{
    public teamTower team;
    public int maxHealth;
    private int currentHealth;
    public List<Node> attackableNodes;
    void Start()
    {
        currentHealth = maxHealth;
    }
    public bool CanBeAttackedFrom(Node node)
    {
        return attackableNodes.Contains(node);
    }
    public void TakeDamage(int damage)
    {
        currentHealth -= damage;
        if (currentHealth <= 0)
        {
            currentHealth = 0;
            //VictoryManager.instance.TowerDestroyed(this);
            Destroy(gameObject);
        }
    }
}
