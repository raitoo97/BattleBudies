using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CardManager : MonoBehaviour
{
    public List<Card> deck;                
    public List<CardInstance> hand;         
    public int maxHandSize = 5;
    public int playerEnergy = 3;
    public static CardManager instance;
    void Start()
    {
        hand = new List<CardInstance>();
    }
    public void DrawCard(bool isPlayer)
    {
        if (deck.Count == 0) return;
        Card drawn = deck[Random.Range(0, deck.Count)];
        deck.Remove(drawn);
        CardInstance instance = new CardInstance(drawn, isPlayer);
        hand.Add(instance);

        Debug.Log($"Carta robada: {drawn.name}");
    }
    public void PlayCard(CardInstance card, Vector3 spawnPosition)
    {
        if (!EnergyManager.instance.TryConsumeEnergy(card.data.energyCost))
        {
            Debug.Log("No tienes energía suficiente");
            return;
        }
        GameObject unitObj = Instantiate(card.data.fichaPrefab, spawnPosition, Quaternion.identity);
        Units unitScript = unitObj.GetComponent<Units>();
        if (unitScript != null)
        {
            unitScript.SetCard(card);
        }
        hand.Remove(card);
    }
}
