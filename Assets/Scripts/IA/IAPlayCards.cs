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
            CardInteraction cardToPlay = GetCardToPlay(hand);
            if (cardToPlay == null) yield break;
            Node spawnNode= NodeManager.GetRandomEmptyNodeOnRow(14);
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
        CardInteraction cardToPlay = GetCardToPlay(hand);
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
    private CardInteraction GetCardToPlay(Transform hand)
    {
        CardInteraction cardToPlay = null;
        foreach (Transform c in hand)
        {
            CardInteraction card = c.GetComponent<CardInteraction>();
            if (card != null && card.GetComponent<UICard>().cardData.cost <= EnergyManager.instance.enemyCurrentEnergy &&
                card.GetComponent<UICard>().cardData.unitType == UnitType.Attacker)
            {
                cardToPlay = card;
                break;
            }
        }
        if (cardToPlay == null)
        {
            Debug.Log("IA: No hay cartas Attackers disponibles, buscando Defenders...");
            foreach (Transform c in hand)
            {
                CardInteraction card = c.GetComponent<CardInteraction>();
                if (card != null && card.GetComponent<UICard>().cardData.cost <= EnergyManager.instance.enemyCurrentEnergy &&
                    card.GetComponent<UICard>().cardData.unitType == UnitType.Defender)
                {
                    cardToPlay = card;
                    break;
                }
            }
        }
        if (cardToPlay == null)
        {
            Debug.Log("IA: No hay cartas Defenders disponibles, buscando Rangers...");
            foreach (Transform c in hand)
            {
                CardInteraction card = c.GetComponent<CardInteraction>();
                if (card != null && card.GetComponent<UICard>().cardData.cost <= EnergyManager.instance.enemyCurrentEnergy &&
                    card.GetComponent<UICard>().cardData.unitType == UnitType.Ranger)
                {
                    cardToPlay = card;
                    break;
                }
            }
        }
        if (cardToPlay == null)
        {
            Debug.Log("IA: No hay cartas de prioridad, se jugará cualquier carta disponible si hay energía.");
            foreach (Transform c in hand)
            {
                CardInteraction card = c.GetComponent<CardInteraction>();
                if (card != null && card.GetComponent<UICard>().cardData.cost <= EnergyManager.instance.enemyCurrentEnergy)
                {
                    cardToPlay = card;
                    break;
                }
            }
        }
        if (cardToPlay == null)
        {
            Debug.Log("IA: No hay cartas jugables disponibles.");
        }
        return cardToPlay;
    }
}