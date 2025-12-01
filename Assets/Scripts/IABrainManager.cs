using System.Collections;
using System.Linq;
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
        CardPlayManager.instance.HideAllHandsAtAITurn();
        CanvasManager.instance.UpdateEnergyUI();
        yield return null;
        yield return new WaitForSeconds(0.5f);
        yield return StartCoroutine(IAMoveToTowers.instance.MoveAllEnemyUnitsToTowers());
        if (!IAMoveToTowers.instance.movedAnyUnit)
        {
            yield return StartCoroutine(IAPlayCards.instance.PlayCards());
        }
        yield return new WaitUntil(() => !CombatManager.instance.GetCombatActive);
        GameManager.instance.StartPlayerTurn();
    }
}