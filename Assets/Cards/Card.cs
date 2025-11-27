using UnityEngine;
[CreateAssetMenu(fileName = "NewCard", menuName = "Cards/CardData")]
public class Card : ScriptableObject
{
    [Header("Estadísticas de ficha")]
    public int health;        
    public int energyCost;
    [Header("Daño")]
    public int diceCount = 1; // Número de D6 que lanza la carta
    [Header("Prefab y lógica")]
    public GameObject fichaPrefab;
    [Header("Arte de la carta (512x1024)")]
    public Sprite artwork;
    public int RollDamage()
    {
        int total = 0;
        for (int i = 0; i < diceCount; i++)
            total += Random.Range(1, 7);
        return total;
    }
}
