using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TurnManager : MonoBehaviour
{
    public static TurnManager instance;

    public bool isPlayerTurn = true;   
    public float turnDelay = 1f;
    private void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);
    }
    private void Start()
    {
        //StartCoroutine(TurnLoop());
    }
    private IEnumerator TurnLoop()
    {
        while (true)
        {
            if (isPlayerTurn)
            {
                EnergyManager.instance.currentEnergy = EnergyManager.instance.maxEnergy;
                Debug.Log("Turno del Player: energía restaurada.");
                CardManager.instance.DrawCard(true);
                yield return new WaitUntil(() => Input.GetKeyDown(KeyCode.K));
                Debug.Log("Jugador pasó turno.");
                isPlayerTurn = false;
            }
            else
            {
                EnergyManager.instance.currentEnergy = EnergyManager.instance.maxEnergy;
                Debug.Log("Turno de la IA: energía restaurada.");
                CardManager.instance.DrawCard(false);

                yield return new WaitForSeconds(turnDelay);
                Debug.Log("IA pasa turno (sin lógica aún).");
                isPlayerTurn = true;
            }
            yield return null;
        }
    }
}
