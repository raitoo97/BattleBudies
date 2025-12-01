using UnityEngine;
public enum Faction
{
    Player,
    Enemy
}
public class Tower : MonoBehaviour
{
    public Faction faction;
    public int maxHealth;
    public int currentHealth;
    public bool isDestroyed = false;
    private void Start()
    {
        currentHealth = maxHealth;
        TowerManager.instance.RegisterTower(this);
    }
    public void TakeDamage(int amount)
    {
        currentHealth -= amount;
        Debug.Log($"{name} recibió {amount} daño. Vida restante: {currentHealth}");

        if (currentHealth <= 0 && !isDestroyed)
        {
            isDestroyed = true;
            Debug.Log($"{name} ha sido destruida");
            TowerManager.instance.NotifyTowerDestroyed(this);
            Destroy(gameObject);
        }
    }
}
