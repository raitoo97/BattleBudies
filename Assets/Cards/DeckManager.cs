using UnityEngine;
public class DeckManager : MonoBehaviour
{
    public static DeckManager instance;
    [Header("Deck Settings")]
    [SerializeField]private CardData[] allCards;
    [SerializeField]private CardData[] enemyCards;
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
    private void DrawCard(Transform handParent, bool isPlayer = true)
    {
        if (handParent.childCount >= maxHandSize)
        {
            Debug.Log($"No se pueden robar más cartas, la mano de {handParent.name} está llena!");
            return;
        }
        CardData[] deck = isPlayer ? allCards : enemyCards;
        if (deck.Length == 0) return;
        int index = Random.Range(0, deck.Length);
        CardData randomCard = deck[index];
        UICard newCard = Instantiate(cardPrefab, handParent);
        newCard.Setup(randomCard);
        CardInteraction cardInteraction = newCard.GetComponent<CardInteraction>();
        if (isPlayer && cardInteraction != null)
        {
            cardInteraction.isPlayerCard = true;
            cardInteraction.UpdateTint();
        }
    }
    public void DrawPlayerCard()
    {
        DrawCard(playerHand, true);
    }
    public void DrawEnemyCard()
    {
        DrawCard(enemyHand, false);
    }
    public void FillHandsAtStart()
    {
        while (playerHand.childCount < maxHandSize)
        {
            DrawPlayerCard();
        }
        while (enemyHand.childCount < maxHandSize)
        {
            DrawEnemyCard();
        }
    }
}
