using UnityEngine;
public class DeckManager : MonoBehaviour
{
    public static DeckManager instance;
    [Header("Deck Settings")]
    [SerializeField]private CardData[] allCards;      
    [SerializeField]public UICard cardPrefab;
    [SerializeField]public Transform playerHand;
    [SerializeField]public Transform enemyHand;
    [SerializeField]private int maxHandSize = 5;
    private void Awake()
    {
        if (instance == null)
            instance = this;
        else
            Destroy(gameObject);
    }
    public void DrawCard(Transform handParent)
    {
        if (handParent.childCount >= maxHandSize)
        {
            Debug.Log($"No se pueden robar más cartas, la mano de {handParent.name} está llena!");
            return;
        }
        if (allCards.Length == 0) return;
        int index = Random.Range(0, allCards.Length);
        CardData randomCard = allCards[index];
        UICard newCard = Instantiate(cardPrefab, handParent);
        newCard.Setup(randomCard);
    }
}
