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
            SoundManager.Instance.PlayClip(SoundManager.Instance.GetAudioClip("CardPlay"), 1f, false);
            Node spawnNode= NodeManager.GetRandomEmptyNodeOnRow(14);
            if (spawnNode == null) yield break;
            yield return new WaitForSeconds(1f);
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
        SoundManager.Instance.PlayClip(SoundManager.Instance.GetAudioClip("CardPlay"), 1f, false);
        Node spawnNode = NodeManager.GetRandomEmptyNodeOnRow(14);
        if (spawnNode == null) yield break;
        yield return new WaitForSeconds(1f);
        CardPlayManager.instance.GetcurrentUIcard = cardToPlay;
        CardPlayManager.instance.GetcurrentCardData = cardToPlay.GetComponent<UICard>().cardData;
        CardPlayManager.instance.GetselectedNode = spawnNode;
        CardPlayManager.instance.placingMode = true;
        CardPlayManager.instance.PlaceUnitAtNode();
        yield return new WaitUntil(() => !CardPlayManager.instance.placingMode);
        yield return new WaitForSeconds(2f);
    }
    public IEnumerator PlayOneCard_NearThreat(Units threat)
    {
        Transform hand = DeckManager.instance.enemyHand;
        CardInteraction cardToPlay = GetCardToPlay(hand);
        if (cardToPlay == null) yield break;
        SoundManager.Instance.PlayClip(SoundManager.Instance.GetAudioClip("CardPlay"), 1f, false);
        //nodo más cercano a la amenaza en fila 14
        Node spawnNode = GetClosestEmptyNodeOnRowToThreat(14, threat);
        if (spawnNode == null)
        {
            spawnNode = NodeManager.GetRandomEmptyNodeOnRow(14);
        }

        if (spawnNode == null)
        {
            Debug.LogWarning("IA: No hay nodos disponibles en fila 14");
            yield break;
        }
        yield return new WaitForSeconds(1f);
        CardPlayManager.instance.GetcurrentUIcard = cardToPlay;
        CardPlayManager.instance.GetcurrentCardData =cardToPlay.GetComponent<UICard>().cardData;
        CardPlayManager.instance.GetselectedNode = spawnNode;
        CardPlayManager.instance.placingMode = true;
        CardPlayManager.instance.PlaceUnitAtNode();
        yield return new WaitUntil(() => !CardPlayManager.instance.placingMode);
        yield return new WaitForSeconds(0.5f);
    }
    private Node GetClosestEmptyNodeOnRowToThreat(int row, Units threat)
    {
        if (threat == null) return null;
        Node bestNode = null;
        float minDist = float.MaxValue;
        foreach (Node node in NodeManager.GetAllNodes())
        {
            if (node == null) continue;
            if (node.gridIndex.x != row) continue;
            if (!node.IsEmpty() || node._isBlock) continue;
            float dist = Vector3.Distance(node.transform.position,threat.transform.position);
            if (dist < minDist)
            {
                minDist = dist;
                bestNode = node;
            }
        }
        return bestNode;
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
    private CardInteraction GetCardToPlay_PrioritizeRanger(Transform hand)
    {
        CardInteraction cardToPlay = null;
        // 1. Prioridad: Ranger
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
        if (cardToPlay == null)
        {
            Debug.Log("IA: No hay cartas de Rangers jugables disponibles.");
        }
        return cardToPlay;
    }
    public IEnumerator PlayOneCard_PrioritizeRanger()
    {
        Transform hand = DeckManager.instance.enemyHand;
        CardInteraction cardToPlay = GetCardToPlay_PrioritizeRanger(hand);
        if (cardToPlay == null) yield break;
        SoundManager.Instance.PlayClip(SoundManager.Instance.GetAudioClip("CardPlay"), 1f, false);
        Node spawnNode = NodeManager.GetRandomEmptyNodeOnRow(14);
        if (spawnNode == null) yield break;
        yield return new WaitForSeconds(1f);
        CardPlayManager.instance.GetcurrentUIcard = cardToPlay;
        CardPlayManager.instance.GetcurrentCardData = cardToPlay.GetComponent<UICard>().cardData;
        CardPlayManager.instance.GetselectedNode = spawnNode;
        CardPlayManager.instance.placingMode = true;
        CardPlayManager.instance.PlaceUnitAtNode();
        yield return new WaitUntil(() => !CardPlayManager.instance.placingMode);
        yield return new WaitForSeconds(2f);
    }
    public bool CanPlayRanger()
    {
        foreach (Transform c in DeckManager.instance.enemyHand)
        {
            UICard card = c.GetComponent<UICard>();
            if (card != null &&card.cardData.unitType == UnitType.Ranger &&card.cardData.cost <= EnergyManager.instance.enemyCurrentEnergy)
                return true;
        }
        Debug.Log("IA: No puede jugar cartas de Ranger porque no tiene.");
        return false;
    }
}