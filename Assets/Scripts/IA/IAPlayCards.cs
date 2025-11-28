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
            CardPlayManager.instance.PlaceUnitAtNode();
            yield return new WaitForSeconds(2f);
        }
    }
}
