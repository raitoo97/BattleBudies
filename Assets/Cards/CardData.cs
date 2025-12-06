using UnityEngine;
public enum UnitType
{
    Attacker,
    Defender,
    Ranger,
    None
}
[CreateAssetMenu(fileName = "NewCard", menuName = "Cards/Card Data")]
public class CardData : ScriptableObject
{
    public Sprite artwork;
    public int cost;          
    public GameObject unitPrefab;
    public UnitType unitType;
}
