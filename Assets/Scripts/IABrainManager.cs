using System.Collections;
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
        ClearAllPaths();
        CardPlayManager.instance.HideAllHandsAtAITurn();
        EnergyManager.instance.RefillEnemyEnergy();
        DeckManager.instance.DrawEnemyCard();
        CardPlayManager.instance.HideAllHandsAtAITurn();
        CanvasManager.instance.UpdateEnergyUI();
        yield return null;
        yield return new WaitForSeconds(0.5f);
        if (EnergyManager.instance.enemyCurrentEnergy > 0f)
            yield return StartCoroutine(IADefendTowers.instance.MoveAllEnemyUnitsToDefend());
        if (!IAMoveToTowers.instance.movedAnyUnit)
            yield return StartCoroutine(IAPlayCards.instance.PlayCards());
        yield return new WaitUntil(() => !CombatManager.instance.GetCombatActive);
        GameManager.instance.StartPlayerTurn();
    }
    private void ClearAllPaths()
    {
        foreach (Units u in FindObjectsOfType<Units>())
            u.ClearPath();
    }
}