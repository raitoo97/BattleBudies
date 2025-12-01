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
    }
    private bool IsAnyPlayerUnitMoving()
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
}
