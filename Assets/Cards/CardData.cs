using UnityEngine;
[CreateAssetMenu(fileName = "NewCard", menuName = "Cards/Card Data")]
public class CardData : ScriptableObject
{
    public Sprite artwork;
    public int health;
    public int diceCount;
    public int cost;          
    public GameObject unitPrefab;
    public int RollDamage()
    {
        int total = 0;
        for (int i = 0; i < diceCount; i++)
        {
            total += Random.Range(1, 7);
        }
        return total;
    }
}
