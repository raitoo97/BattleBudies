using System.Collections;
using UnityEngine;
public class GameManager : MonoBehaviour
{
    public static GameManager instance;
    public bool isPlayerTurn = true;
    private bool playerWantsToEndTurn = false;
    private void Awake()
    {
        if (instance == null)
            instance = this;
        else
            Destroy(gameObject);
    }
    private void Start()
    {
        DeckManager.instance.FillHandsAtStart();
        SoundManager.Instance.PlayClip(SoundManager.Instance.GetAudioClip("MusicBackground"), 0.5f, true);
        StartCoroutine(TurnLoop());
    }
    private IEnumerator TurnLoop()
    {
        yield return new WaitUntil(() => IABrainManager.instance != null && DeckManager.instance != null);
        while (true)
        {
            if (isPlayerTurn)
            {
                CardPlayManager.instance.ShowAllHandsAtPlayerTurn();
                EnergyManager.instance.RefillPlayerEnergy();
                CanvasManager.instance.UpdateEnergyUI();
                DeckManager.instance.DrawPlayerCard();
                yield return StartCoroutine(HandleUnitsOnSpecialNodes());
                yield return new WaitUntil(() => playerWantsToEndTurn && !IsAnyPlayerUnitMoving() && !CombatManager.instance.GetCombatActive);
                isPlayerTurn = false;
                playerWantsToEndTurn = false;
                CardPlayManager.instance.HideAllHandsAtAITurn();
                StartCoroutine(IABrainManager.instance.ExecuteTurn());
            }
            yield return null;
        }
    }
    public void StartPlayerTurn()
    {
        isPlayerTurn = true;
        CardPlayManager.instance.ShowAllHandsAtPlayerTurn();
        Units[] allUnits = FindObjectsOfType<Units>();
        foreach (var u in allUnits)
        {
            if (u.isPlayerUnit)
                u.ResetTurnFlags();
        }
    }
    public bool IsAnyPlayerUnitMoving()
    {
        Units[] allUnits = FindObjectsOfType<Units>();
        foreach (Units u in allUnits)
        {
            if (u.isPlayerUnit && !u.PathEmpty())
                return true;
        }
        return false;
    }
    public void PlayerRequestsEndTurn()
    {
        if (!isPlayerTurn) return;
        if (!IsAnyPlayerUnitMoving() && !CombatManager.instance.GetCombatActive)
            playerWantsToEndTurn = true;
    }
    public void SetEndGame(bool playerWon)
    {
        StopAllCoroutines();
        if (playerWon)
            Debug.Log("VICTORIA DEL JUGADOR");
        else
            Debug.Log("DERROTA DEL JUGADOR");
    }
    private IEnumerator HandleUnitsOnSpecialNodes()
    {
        Units[] allUnits = FindObjectsOfType<Units>();
        foreach (Units u in allUnits)
        {
            if (!u.isPlayerUnit) continue;
            if (u.currentNode == null) continue;
            if (u is Ranger ranger &&NodeManager.GetResourcesNode().Contains(u.currentNode) &&!ranger.hasCollectedThisTurn)
            {
                ranger.hasCollectedThisTurn = true;
                ResourcesManager.instance.StartRecolectedResources(ranger);
                yield return new WaitUntil(() => !ResourcesManager.instance.onColectedResources);
            }
            if (u is Defenders def && NodeManager.GetHealthNodes().Contains(u.currentNode) &&!u.hasHealthedTowerThisTurn)
            {
                HealthTowerManager.instance.StartRecolectedHealth(def);
                yield return new WaitUntil(() => !HealthTowerManager.instance.onColectedHealth);
                u.hasHealthedTowerThisTurn = true;
            }
            if (TowerManager.instance.TryGetTowerAtNode(u.currentNode, out Tower tower))
            {
                if (!u.hasAttackedTowerThisTurn &&TowerManager.instance.CanUnitAttackTower(u, tower))
                {
                    u.hasAttackedTowerThisTurn = true;
                    yield return StartCoroutine(CombatManager.instance.StartCombatWithTower_Coroutine(u, tower));
                }
            }
        }
    }
}
