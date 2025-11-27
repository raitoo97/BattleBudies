using System.Collections;
using UnityEngine;
public class IAPlayCards : MonoBehaviour
{
    public static IAPlayCards instance;

    private void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);
    }

    public IEnumerator PlayCards()
    {
        Transform hand = DeckManager.instance.enemyHand;

        while (hand.childCount > 0 && EnergyManager.instance.enemyCurrentEnergy > 0)
        {
            CardInteraction card = hand.GetChild(0).GetComponent<CardInteraction>();
            if (card == null) yield break;
            Node spawnNode = NodeManager.GetRandomEmptyNodeOnRow(14);
            if (spawnNode == null) yield break;
            CardPlayManager.instance.GetcurrentUIcard = card;
            CardPlayManager.instance.GetcurrentCardData = card.GetComponent<UICard>().cardData;
            CardPlayManager.instance.GetselectedNode = spawnNode;
            CardPlayManager.instance.PlaceUnitAtNode();
            yield return new WaitForSeconds(0.5f);
        }
    }
}
