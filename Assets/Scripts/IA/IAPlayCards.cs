using System.Collections;
using UnityEngine;
public class IAPlayCards : MonoBehaviour
{
    public static IAPlayCards instance;
    private void Awake()
    {
        if (instance == null)
            instance = this;
        else
            Destroy(gameObject);
    }
    private bool CanPlayAnyCard()
    {
        foreach (Transform c in DeckManager.instance.enemyHand)
        {
            CardInteraction card = c.GetComponent<CardInteraction>();
            if (card != null && card.GetComponent<UICard>().cardData.cost <= EnergyManager.instance.enemyCurrentEnergy)
                return true;
        }
        return false;
    }
    public IEnumerator PlayCards()
    {
        Transform hand = DeckManager.instance.enemyHand;
        while (hand.childCount > 0 && CanPlayAnyCard())
        {
            CardInteraction cardToPlay = null;
            foreach (Transform c in hand)
            {
                CardInteraction card = c.GetComponent<CardInteraction>();
                if (card != null && card.GetComponent<UICard>().cardData.cost <= EnergyManager.instance.enemyCurrentEnergy)
                {
                    cardToPlay = card;
                    break;
                }
            }
            if (cardToPlay == null) yield break;
            Node spawnNode = NodeManager.GetRandomEmptyNodeOnRow(14);
            if (spawnNode == null) yield break;
            CardPlayManager.instance.GetcurrentUIcard = cardToPlay;
            CardPlayManager.instance.GetcurrentCardData = cardToPlay.GetComponent<UICard>().cardData;
            CardPlayManager.instance.GetselectedNode = spawnNode;
            CardPlayManager.instance.placingMode = true;
            CardPlayManager.instance.PlaceUnitAtNode();
            yield return new WaitUntil(() => !CardPlayManager.instance.placingMode);
            yield return new WaitForSeconds(2f);
        }
    }
    public IEnumerator PlayOneCard()
    {
        Transform hand = DeckManager.instance.enemyHand;
        CardInteraction cardToPlay = null;
        // Buscar la primera carta que se pueda pagar.
        foreach (Transform c in hand)
        {
            CardInteraction card = c.GetComponent<CardInteraction>();
            if (card != null && card.GetComponent<UICard>().cardData.cost <= EnergyManager.instance.enemyCurrentEnergy)
            {
                cardToPlay = card;
                break; // Rompe el foreach: solo encontramos UNA carta.
            }
        }
        if (cardToPlay == null) yield break;
        Node spawnNode = NodeManager.GetRandomEmptyNodeOnRow(14);
        if (spawnNode == null) yield break;
        CardPlayManager.instance.GetcurrentUIcard = cardToPlay;
        CardPlayManager.instance.GetcurrentCardData = cardToPlay.GetComponent<UICard>().cardData;
        CardPlayManager.instance.GetselectedNode = spawnNode;
        CardPlayManager.instance.placingMode = true;
        CardPlayManager.instance.PlaceUnitAtNode(); // Consume la energía de la carta.
        yield return new WaitUntil(() => !CardPlayManager.instance.placingMode);
        yield return new WaitForSeconds(2f);
    }
}