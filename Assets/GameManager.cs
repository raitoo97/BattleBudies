using System.Collections;
using UnityEngine;
public class GameManager : MonoBehaviour
{
    public static GameManager instance;
    public bool isPlayerTurn = true;
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
                yield return new WaitUntil(() => Input.GetKeyDown(KeyCode.K));
                isPlayerTurn = false;
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
}
