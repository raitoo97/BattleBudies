using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IABrainManager : MonoBehaviour
{
    public static IABrainManager instance;

    private void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);
    }

    public IEnumerator ExecuteTurn()
    {
        CardPlayManager.instance.HideAllHandsAtAITurn();
        EnergyManager.instance.RefillEnemyEnergy();
        DeckManager.instance.DrawEnemyCard();
        yield return null;
        CardPlayManager.instance.HideAllHandsAtAITurn();
        yield return new WaitForSeconds(0.5f);
        yield return StartCoroutine(IAMoveUnits.instance.MoveAllEnemyUnits());
        if (!IAMoveUnits.instance.movedAnyUnit)
        {
            yield return StartCoroutine(IAPlayCards.instance.PlayCards());
        }
        GameManager.instance.StartPlayerTurn();
    }
}
