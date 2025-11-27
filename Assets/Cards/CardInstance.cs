using UnityEngine;
public class CardInstance
{
    public Card data;
    public bool isPlayerCard;  
    public int currentHealth;
    public CardInstance(Card cardData, bool _isPlayercard)
    {
        data = cardData;
        isPlayerCard = _isPlayercard;
        currentHealth = cardData.health;
    }
    public void TakeDamage(int amount)
    {
        currentHealth -= amount;
        if (currentHealth <= 0)
        {
            currentHealth = 0;
            OnDeath();
        }
    }
    private void OnDeath()
    {
        Debug.Log($"{data.name} ha sido destruida.");
    }
    public int GetDamage()
    {
        return data.RollDamage();
    }
}
